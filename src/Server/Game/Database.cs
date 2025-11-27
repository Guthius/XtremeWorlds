using Core;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Core.Common;
using Core.Configurations;
using Core.Globals;
using Core.Net;
using Server.Net;
using static Core.Globals.Command;
using static Core.Globals.Type;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using File=System.IO.File;
using Path = System.IO.Path;
using SdMapLayer = Core.Globals.SdMapLayer;
using Type = Core.Globals.Type;
using System.Reflection.Metadata;

namespace Server
{
    public class Database
    {
        private static readonly int StatCount = Enum.GetValues<Stat>().Length;

        private static readonly SemaphoreSlim ConnectionSemaphore = new SemaphoreSlim(Variables.MaxSqlClients, Variables.MaxSqlClients);

        public static string ConnectionString { get; set; } = string.Empty;

        public static async System.Threading.Tasks.Task CreateDatabaseAsync(string databaseName)
        {
            await ConnectionSemaphore.WaitAsync();
            try
            {
                string checkDbExistsSql = $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'";
                string createDbSql = $"CREATE DATABASE {databaseName}";

                using (var connection = new NpgsqlConnection(ConnectionString.Replace("Database=mirage", "Database=postgres")))
                {
                    await connection.OpenAsync();

                    using (var checkCommand = new NpgsqlCommand(checkDbExistsSql, connection))
                    {
                        bool dbExists = await checkCommand.ExecuteScalarAsync() is not null;

                        if (!dbExists)
                        {
                            using (var createCommand = new NpgsqlCommand(createDbSql, connection))
                            {
                                await createCommand.ExecuteNonQueryAsync();

                                using (var dbConnection = new NpgsqlConnection(ConnectionString))
                                {
                                    await dbConnection.CloseAsync();
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                ConnectionSemaphore.Release();
            }
        }

        public static async System.Threading.Tasks.Task<bool> RowExistsByColumnAsync(string columnName, long value, string tableName)
        {
            await ConnectionSemaphore.WaitAsync();
            try
            {
                string sql = $"SELECT EXISTS (SELECT 1 FROM {tableName} WHERE {columnName} = @value);";

                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@value", value);

                        var result = await command.ExecuteScalarAsync();
                        return result is bool b && b;
                    }
                }
            }
            finally
            {
                ConnectionSemaphore.Release();
            }
        }

        public static async System.Threading.Tasks.Task UpdateRowAsync(long id, string data, string table, string columnName)
        {
            await ConnectionSemaphore.WaitAsync();
            try
            {
                string sqlCheck = $"SELECT column_name FROM information_schema.columns WHERE table_name='{table}' AND column_name='{columnName}';";

                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    // Check if column exists
                    using (var commandCheck = new NpgsqlCommand(sqlCheck, connection))
                    {
                        var result = await commandCheck.ExecuteScalarAsync();

                        // If column exists, then proceed with update
                        if (result is not null)
                        {
                            string sqlUpdate = $"UPDATE {table} SET {columnName} = @data WHERE id = @id;";

                            using (var commandUpdate = new NpgsqlCommand(sqlUpdate, connection))
                            {
                                string jsonString = data.ToString();
                                commandUpdate.Parameters.AddWithValue("@data", NpgsqlDbType.Jsonb, jsonString);
                                commandUpdate.Parameters.AddWithValue("@id", id);

                                await commandUpdate.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Column '{columnName}' does not exist in table {table}.");
                        }
                    }
                }
            }
            finally
            {
                ConnectionSemaphore.Release();
            }
        }

        public static async System.Threading.Tasks.Task UpdateRowByColumnAsync(string columnName, long value, string targetColumn, string newValue, string tableName)
        {
            await ConnectionSemaphore.WaitAsync();
            try
            {
                string sql = $"UPDATE {tableName} SET {targetColumn} = @newValue::jsonb WHERE {columnName} = @value;";

                newValue = newValue.Replace(@"\u0000", "");

                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@value", value);
                        command.Parameters.AddWithValue("@newValue", newValue);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            finally
            {
                ConnectionSemaphore.Release();
            }
        }

        public static async System.Threading.Tasks.Task CreateTablesAsync()
        {
            await ConnectionSemaphore.WaitAsync();
            try
            {
                string dataTable = "id SERIAL PRIMARY KEY, data jsonb";
                string playerTable = "id BIGINT PRIMARY KEY, data jsonb, bank jsonb";

                for (int i = 1, loopTo = Core.Globals.Variables.MaxChars; i <= loopTo; i++)
                    playerTable += $", character{i} jsonb";

                string[] tableNames = new[] { "job", "item", "map", "npc", "shop", "skill", "resource", "animation", "projectile", "moral" };

                var tasks = tableNames.Select(tableName => CreateTableAsync(tableName, dataTable));
                await System.Threading.Tasks.Task.WhenAll(tasks);

                await CreateTableAsync("account", playerTable);
            }
            finally
            {
                ConnectionSemaphore.Release();
            }
        }

        public static async System.Threading.Tasks.Task CreateTableAsync(string tableName, string layout)
        {
            await ConnectionSemaphore.WaitAsync();
            try
            {
                using (var conn = new NpgsqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();

                    using (var cmd = new NpgsqlCommand($"CREATE TABLE IF NOT EXISTS {tableName} ({layout});", conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            finally
            {
                ConnectionSemaphore.Release();
            }
        }

        public static async System.Threading.Tasks.Task<List<long>> GetDataAsync(string tableName)
        {
            var ids = new List<long>();

            await ConnectionSemaphore.WaitAsync();
            try
            {
                using (var conn = new NpgsqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();

                    // Define a query
                    var cmd = new NpgsqlCommand($"SELECT id FROM {tableName}", conn);

                    // Execute a query
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        // Read all rows and output the first column in each row
                        while (await reader.ReadAsync())
                        {
                            long id = await reader.GetFieldValueAsync<long>(0);
                            ids.Add(id);
                        }
                    }
                }
            }
            finally
            {
                ConnectionSemaphore.Release();
            }

            return ids;
        }

        public static async System.Threading.Tasks.Task<bool> RowExistsAsync(long id, string table)
        {
            await ConnectionSemaphore.WaitAsync();
            try
            {
                string sql = $"SELECT EXISTS (SELECT 1 FROM {table} WHERE id = @id);";

                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return reader.GetBoolean(0);
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            finally
            {
                ConnectionSemaphore.Release();
            }
        }

        public static async System.Threading.Tasks.Task InsertRowAsync(long id, string data, string tableName)
        {
            await ConnectionSemaphore.WaitAsync();
            try
            {
                using (var conn = new NpgsqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();

                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = $"INSERT INTO {tableName} (id, data) VALUES (@id, @data::jsonb);";
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@data", data); // Convert JObject back to string

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            finally
            {
                ConnectionSemaphore.Release();
            }
        }

        public static async System.Threading.Tasks.Task InsertRowAsync(long id, string data, string tableName, string columnName)
        {
            await ConnectionSemaphore.WaitAsync();
            try
            {
                using (var conn = new NpgsqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();

                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = $"INSERT INTO {tableName} (id, data) VALUES (@id, @data::jsonb);";
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@" + columnName, data); // Convert JObject back to string

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            finally
            {
                ConnectionSemaphore.Release();
            }
        }

        public static async System.Threading.Tasks.Task InsertRowByColumnAsync(long id, string data, string tableName, string dataColumn, string idColumn)
        {
            await ConnectionSemaphore.WaitAsync();
            try
            {
                // Sanitize the data string
                data = data.Replace("\\u0000", "");

                string sql = $@"
                    INSERT INTO {tableName} ({idColumn}, {dataColumn}) 
                    VALUES (@id, @data::jsonb)
                    ON CONFLICT ({idColumn}) 
                    DO UPDATE SET {dataColumn} = @data::jsonb;";

                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@data", data); // Ensure this is properly serialized JSON

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            finally
            {
                ConnectionSemaphore.Release();
            }
        }

        public static async System.Threading.Tasks.Task<JObject?> SelectRowAsync(long id, string tableName, string columnName)
        {
            await ConnectionSemaphore.WaitAsync();
            try
            {
                string sql = $"SELECT {columnName} FROM {tableName} WHERE id = @id;";

                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string jsonbData = reader.GetString(0);
                                var jsonObject = JObject.Parse(jsonbData);
                                return jsonObject;
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
            }
            finally
            {
                ConnectionSemaphore.Release();
            }
        }

        public static async System.Threading.Tasks.Task<JObject?> SelectRowByColumnAsync(string columnName, long value, string tableName, string dataColumn)
        {
            await ConnectionSemaphore.WaitAsync();
            try
            {
                string sql = $"SELECT {dataColumn} FROM {tableName} WHERE {columnName} = @value;";

                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@value", Math.Abs(value));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // Check if the first column is not null
                                if (!reader.IsDBNull(0))
                                {
                                    string jsonbData = reader.GetString(0);
                                    var jsonObject = JObject.Parse(jsonbData);
                                    return jsonObject;
                                }
                                else
                                {
                                    // Handle null value or return null JObject...
                                    return null;
                                }
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
            }
            finally
            {
                ConnectionSemaphore.Release();
            }
        }

        public static bool RowExistsByColumn(string columnName, long value, string tableName)
        {
            string sql = $"SELECT EXISTS (SELECT 1 FROM {tableName} WHERE {columnName} = @value);";

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@value", value);

                    bool exists = Convert.ToBoolean(command.ExecuteScalar());
                    return exists;
                }
            }
        }

        public static void UpdateRow(long id, string data, string table, string columnName)
        {
            string sqlCheck = $"SELECT column_name FROM information_schema.columns WHERE table_name='{table}' AND column_name='{columnName}';";

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                // Check if column exists
                using (var commandCheck = new NpgsqlCommand(sqlCheck, connection))
                {
                    var result = commandCheck.ExecuteScalar();

                    // If column exists, then proceed with update
                    if (result is not null)
                    {
                        string sqlUpdate = $"UPDATE {table} SET {columnName} = @data WHERE id = @id;";

                        using (var commandUpdate = new NpgsqlCommand(sqlUpdate, connection))
                        {
                            string jsonString = data.ToString();
                            commandUpdate.Parameters.AddWithValue("@data", NpgsqlDbType.Jsonb, jsonString);
                            commandUpdate.Parameters.AddWithValue("@id", id);

                            commandUpdate.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Column '{columnName}' does not exist in table {table}.");
                    }
                }
            }
        }

        public static string StringToHex(string input)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(input);
            var hex = new StringBuilder(byteArray.Length * 2);

            foreach (byte b in byteArray)
                hex.AppendFormat("{0:x2}", b);

            return hex.ToString();
        }

        public static long GetStringHash(string input)
        {
            using (var sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Convert byte array to a long
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }

                // Use only the first 8 bytes (64 bits) to fit a Long
                return Math.Abs((BitConverter.ToInt64(bytes, 0)));
            }
        }

        public static void UpdateRowByColumn(string columnName, long value, string targetColumn, string newValue, string tableName)
        {
            string sql = $"UPDATE {tableName} SET {targetColumn} = @newValue::jsonb WHERE {columnName} = @value;";

            newValue = newValue.Replace(@"\u0000", "");

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@value", value);
                    command.Parameters.AddWithValue("@newValue", newValue);

                    command.ExecuteNonQuery();
                }
            }
        }

        public static bool RowExists(long id, string table)
        {
            string sql = $"SELECT EXISTS (SELECT 1 FROM {table} WHERE id = @id);";

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetBoolean(0);
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }

        public static void InsertRow(long id, string data, string tableName)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = $"INSERT INTO {tableName} (id, data) VALUES (@id, @data::jsonb);";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@data", data); // Convert JObject back to string

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void InsertRow(long id, string data, string tableName, string columnName)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = $"INSERT INTO {tableName} (id, data) VALUES (@id, @data::jsonb);";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@" + columnName, data); // Convert JObject back to string

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void InsertRowByColumn(long id, string data, string tableName, string dataColumn, string idColumn)
        {
            // Sanitize the data string
            data = data.Replace("\\u0000", "");

            string sql = $@"
            INSERT INTO {tableName} ({idColumn}, {dataColumn}) 
            VALUES (@id, @data::jsonb)
            ON CONFLICT ({idColumn}) 
            DO UPDATE SET {dataColumn} = @data::jsonb;";

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@data", data); // Ensure this is properly serialized JSON

                    command.ExecuteNonQuery();
                }
            }
        }
        public static JObject? SelectRowByColumn(string columnName, long value, string tableName, string dataColumn)
        {
            string sql = $"SELECT {dataColumn} FROM {tableName} WHERE {columnName} = @value;";

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@value", Math.Abs(value));

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Check if the first column is not null
                            if (!reader.IsDBNull(0))
                            {
                                string jsonbData = reader.GetString(0);
                                var jsonObject = JObject.Parse(jsonbData);
                                return jsonObject;
                            }
                            else
                            {
                                // Handle null value or return null JObject...
                                return null;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        #region Var

        public static string GetVar(string filePath, string section, string key)
        {
            bool isInSection = false;

            foreach (string line in System.IO.File.ReadAllLines(filePath))
            {
                if (line.Equals("[" + section + "]", StringComparison.OrdinalIgnoreCase))
                {
                    isInSection = true;
                }
                else if (line.StartsWith("[") & line.EndsWith("]"))
                {
                    isInSection = false;
                }
                else if (isInSection & line.Contains("="))
                {
                    string[] parts = line.Split(new char[] { '=' }, 2);
                    if (parts[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        return parts[1];
                    }

                }
            }

            return string.Empty; // Key not found
        }

        public static void PutVar(string filePath, string section, string key, string value)
        {
            var lines = new List<string>(System.IO.File.ReadAllLines(filePath));
            bool updated = false;
            int i = 0;

            while (i < lines.Count)
            {
                if (lines[i].Equals("[" + section + "]", StringComparison.OrdinalIgnoreCase))
                {
                    i += 0;
                    while (i < lines.Count & !lines[i].StartsWith("["))
                    {
                        if (lines[i].Contains("="))
                        {
                            string[] parts = lines[i].Split(new char[] { '=' }, 2);
                            if (parts[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                            {
                                lines[i] = key + "=" + value;
                                updated = true;
                                break;
                            }
                        }
                        i += 0;
                    }
                    break;
                }
                i += 0;
            }

            if (!updated)
            {
                // Key not found, add it to the section
                lines.Add("[" + section + "]");
                lines.Add(key + "=" + value);
            }

            System.IO.File.WriteAllLines(filePath, lines);
        }


        #endregion

        #region Job

        public static void ClearJob(int jobNum)
        {
            int statCount = Enum.GetValues(typeof(Stat)).Length;
            Data.Job[jobNum].Stat = new int[statCount];
            Data.Job[jobNum].StartItem = new int[Core.Globals.Variables.MaxStartItems];
            Data.Job[jobNum].StartValue = new int[Core.Globals.Variables.MaxStartItems];
            Data.Job[jobNum].StartSkill = new int[Core.Globals.Variables.MaxStartSkills];

            Data.Job[jobNum].Name = "";
            Data.Job[jobNum].Desc = "";
            Data.Job[jobNum].StartMap = 1;
            Data.Job[jobNum].MaleSprite = 0;
            Data.Job[jobNum].FemaleSprite = 0;

            for (int i = 0; i < Core.Globals.Variables.MaxStartItems; i++)
            {
                Data.Job[jobNum].StartItem[i] = -1;
                Data.Job[jobNum].StartValue[i] = 0;
            }

            for (int i = 0; i < Core.Globals.Variables.MaxStartItems; i++)
            {
                Data.Job[jobNum].StartSkill[i] = -1;
            }
        }

        public static async System.Threading.Tasks.Task LoadJobAsync(int jobNum)
        {
            JObject data;

            data = await SelectRowAsync(jobNum, "job", "data");

            if (data is null)
            {
                ClearJob(jobNum);
                return;
            }

            var jobData = JObject.FromObject(data).ToObject<Job>();
            Data.Job[jobNum] = jobData;
        }

        public static async System.Threading.Tasks.Task LoadJobsAsync()
        {
            var tasks = Enumerable.Range(0, Core.Globals.Variables.MaxJobs).Select(i => System.Threading.Tasks.Task.Run(() => LoadJobAsync(i)));
            await System.Threading.Tasks.Task.WhenAll(tasks);
        }

        public static void SaveJob(int jobNum)
        {
            string json = JsonConvert.SerializeObject(Data.Job[jobNum]).ToString();

            if (RowExists(jobNum, "job"))
            {
                UpdateRow(jobNum, json, "job", "data");
            }
            else
            {
                InsertRow(jobNum, json, "job");
            }
        }

        public static void ClearMapItem(int index, int mapNum)
        {
            Data.MapItem[mapNum, index].PlayerName = "";
            Data.MapItem[mapNum, index].Num = -1;
        }

        #region Players

        public static async System.Threading.Tasks.Task SaveAllPlayersOnlineAsync()
        {
            foreach (var i in PlayerService.Instance.PlayerIds)
            {
                if (!NetworkConfig.IsPlaying(i))
                    continue;

                await SaveCharacterAsync(i, Data.TempPlayer[i].Slot);
                await SaveBankAsync(i);
            }
        }

        public static async System.Threading.Tasks.Task SaveCharacterAsync(int index, int slot)
        {
            await System.Threading.Tasks.Task.Run(() => SaveCharacter(index, slot));
        }

        public static async System.Threading.Tasks.Task SaveBankAsync(int index)
        {
            await System.Threading.Tasks.Task.Run(() => SaveBank(index));
        }

        public static async System.Threading.Tasks.Task SaveAccountAsync(int index)
        {
            string json = JsonConvert.SerializeObject(Data.Account[index]).ToString();
            string username = GetAccountLogin(index);
            long id = GetStringHash(username);

            if (await RowExistsAsync(id, "account"))
            {
                await UpdateRowByColumnAsync("id", id, "data", json, "account");
            }
            else
            {
                await InsertRowByColumnAsync(id, json, "account", "data", "id");
            }
        }

        public static void RegisterAccount(int index, string username, string password)
        {
            SetPlayerLogin(index, username);
            SetPlayerPassword(index, password);

            string json = JsonConvert.SerializeObject(Data.Account[index]).ToString();

            long id = GetStringHash(username);

            InsertRowByColumn(id, json, "account", "data", "id");
        }

        public static bool LoadAccount(int index, string username)
        {
            JObject data;
            data = SelectRowByColumn("id", GetStringHash(username), "account", "data");

            if (data is null)
            {
                return false;
            }

            var accountData = JObject.FromObject(data).ToObject<Account>();
            Data.Account[index] = accountData;
            return true;
        }

        public static void ClearAccount(int index)
        {
            SetPlayerLogin(index, "");
            SetPlayerPassword(index, "");
        }

        public static void ClearPlayer(int index)
        {
            ClearAccount(index);
            ClearBank(index);

            Data.TempPlayer[index].SkillCd = new int[Core.Globals.Variables.MaxPlayerSkills];
            Data.TempPlayer[index].TradeOffer = new PlayerInv[Core.Globals.Variables.MaxInv];

            Data.TempPlayer[index].SkillCd = new int[Core.Globals.Variables.MaxPlayerSkills];
            Data.TempPlayer[index].Editor = EditorType.None;
            Data.TempPlayer[index].SkillBuffer = -1;
            Data.TempPlayer[index].InShop = -1;
            Data.TempPlayer[index].InTrade = -1;
            Data.TempPlayer[index].InParty = -1;

            for (int i = 0, loopTo = Data.TempPlayer[index].EventProcessingCount; i < loopTo; i++)
                Data.TempPlayer[index].EventProcessing[i].EventId = -1;

            ClearCharacter(index);
        }

        #endregion

        public static void LoadBank(int index)
        {
            JObject data;
            data = SelectRowByColumn("id", GetStringHash(GetAccountLogin(index)), "account", "bank");

            if (data is null)
            {
                ClearBank(index);
                return;
            }

            var bankData = JObject.FromObject(data).ToObject<Bank>();
            Data.Bank[index] = bankData;
        }

        public static void SaveBank(int index)
        {
            string json = JsonConvert.SerializeObject(Data.Bank[index]);
            string username = GetAccountLogin(index);
            long id = GetStringHash(username);

            if (RowExistsByColumn("id", id, "account"))
            {
                UpdateRowByColumn("id", id, "bank", json, "account");
            }
            else
            {
                InsertRowByColumn(id, json, "account", "bank", "id");
            }
        }

        public static void ClearBank(int index)
        {
            Data.Bank[index].Item = new PlayerInv[global::Script.MaxBank + 1];
            for (int i = 0; i < global::Script.MaxBank; i++)
            {
                Data.Bank[index].Item[i].Num = -1;
                Data.Bank[index].Item[i].Value = 0;
            }
        }

        public static void ClearCharacter(int index)
        {
            Data.Player[index].Name = "";
            Data.Player[index].Job = 0;
            Data.Player[index].Dir = 0;
            Data.Player[index].Access = (byte)AccessLevel.Player;

            Data.Player[index].Equipment = new PlayerEq[Enum.GetValues(typeof(Equipment)).Length];
            for (int i = 0, loopTo = Enum.GetValues(typeof(Equipment)).Length; i < loopTo; i++)
            {
                Data.Player[index].Equipment[i] = new PlayerEq();
                Data.Player[index].Equipment[i].Num = -1;
            }

            Data.Player[index].Inv = new PlayerInv[global::Script.MaxInv];
            for (int i = 0, loopTo1 = global::Script.MaxInv; i < loopTo1; i++)
            {
                Data.Player[index].Inv[i].Num = -1;
                Data.Player[index].Inv[i].Value = 0;
            }

            Data.Player[index].Exp = 0;
            Data.Player[index].Level = 0;
            Data.Player[index].Map = 0;
            Data.Player[index].Name = "";
            Data.Player[index].Pk = false;
            Data.Player[index].Points = 0;
            Data.Player[index].Sex = 0;

            Data.Player[index].Skill = new Type.PlayerSkill[global::Script.MaxPlayerSkills];
            for (int i = 0, loopTo2 = global::Script.MaxPlayerSkills; i < loopTo2; i++)
            {
                Data.Player[index].Skill[i].Num = -1;
                Data.Player[index].Skill[i].Cd = 0;
            }

            Data.Player[index].Sprite = 0;

            Data.Player[index].Stat = new byte[Enum.GetValues(typeof(Stat)).Length];
            for (int i = 0, loopTo3 = Enum.GetValues(typeof(Stat)).Length; i < loopTo3; i++)
                Data.Player[index].Stat[i] = 0;

            var count = Enum.GetValues(typeof(Vital)).Length;
            Data.Player[index].Vital = new int[count];
            Data.Player[index].MaxVital = new int[count];
            for (int i = 0, loopTo4 = count; i < loopTo4; i++)
            {
                Data.Player[index].Vital[i] = 0;
                Data.Player[index].MaxVital[i] = 0;
            }
            Data.Player[index].X = 0;
            Data.Player[index].Y = 0;

            Data.Player[index].Hotbar = new Type.Hotbar[global::Script.MaxHotbar];
            for (int i = 0, loopTo5 = global::Script.MaxHotbar; i < loopTo5; i++)
            {
                Data.Player[index].Hotbar[i].Slot = -1;
                Data.Player[index].Hotbar[i].SlotType = 0;
            }

            Data.Player[index].Switches = new byte[global::Script.MaxSwitches];
            for (int i = 0, loopTo6 = global::Script.MaxSwitches; i < loopTo6; i++)
                Data.Player[index].Switches[i] = 0;

            Data.Player[index].Variables = new int[global::Script.MaxVariables];
            for (int i = 0, loopTo7 = global::Script.MaxVariables; i < loopTo7; i++)
                Data.Player[index].Variables[i] = 0;

            var resoruceCount = Enum.GetValues(typeof(ResourceSkill)).Length;
            Data.Player[index].GatherSkills = new Type.ResourceType[resoruceCount];
            for (int i = 0, loopTo8 = resoruceCount; i < loopTo8; i++)
            {
                Data.Player[index].GatherSkills[i].SkillLevel = 1;
                Data.Player[index].GatherSkills[i].SkillCurExp = 0;
                SetPlayerGatherSkillMaxExp(index, i, (int)GetSkillNextLevel(index, i));
            }

            for (int i = 0, loopTo9 = Enum.GetValues(typeof(Equipment)).Length; i < loopTo9; i++)
                Data.Player[index].Equipment[i] = new PlayerEq();
        }

        public static bool LoadCharacter(int index, int charNum)
        {
            JObject data;
            data = SelectRowByColumn("id", GetStringHash(GetAccountLogin(index)), "account", "character" + charNum.ToString());

            if (data is null)
            {
                return false;
            }

            var characterData = data.ToObject<Type.Player>();

            if (characterData.Name == "")
            {
                return false;
            }

            Data.Player[index] = characterData;
            Data.TempPlayer[index].Slot = (byte)charNum;
            return true;
        }

        public static void SaveCharacter(int index, int slot)
        {
            string json = JsonConvert.SerializeObject(Data.Player[index]).ToString();
            long id = GetStringHash(GetAccountLogin(index));

            if (slot < 1 | slot > global::Script.MaxChars)
                return;

            if (RowExistsByColumn("id", id, "account"))
            {
                UpdateRowByColumn("id", id, "character" + slot.ToString(), json, "account");
            }
            else
            {
                InsertRowByColumn(id, json, "account", "character" + slot.ToString(), "id");
            }
        }

        public static void AddChar(int index, int slot, string name, byte sex, byte jobNum, int sprite)
        {
            int n;
            int i;

            if (Data.Player[index].Name == "")
            {
                Data.Player[index].Name = name;
                Data.Player[index].Sex = sex;
                Data.Player[index].Job = jobNum;
                Data.Player[index].Sprite = sprite;
                Data.Player[index].Level = 1;

                var statCount = Enum.GetValues(typeof(Stat)).Length;
                for (n = 0; n < statCount; n++)
                    Data.Player[index].Stat[n] = (byte)Data.Job[jobNum].Stat[n];

                Data.Player[index].Dir = (byte)Direction.Down;
                Data.Player[index].Map = Data.Job[jobNum].StartMap;

                if (Data.Player[index].Map == 0)
                    Data.Player[index].Map = 1;

                Data.Player[index].X = Data.Job[jobNum].StartX;
                Data.Player[index].Y = Data.Job[jobNum].StartY;
                Data.Player[index].Dir = (byte)Direction.Down;

                var vitalCount = Enum.GetValues(typeof(Vital)).Length;
                for (i = 0; i < vitalCount; i++)
                {
                    int value = Script.Instance?.GetPlayerMaxVital(index, (Vital)i) ?? 0;
                    SetPlayerVital(index, (Vital)i, value);
                }

                // set starter items
                for (n = 0; n < Core.Globals.Variables.MaxStartItems; n++)
                {
                    if (Data.Job[jobNum].StartItem[n] >= 0)
                    {
                        Data.Player[index].Inv[n].Num = Data.Job[jobNum].StartItem[n];
                        Data.Player[index].Inv[n].Value = Data.Job[jobNum].StartValue[n];
                    }
                }

                // set start skills
                for (n = 0; n < Core.Globals.Variables.MaxStartSkills; n++)
                {
                    if (Data.Job[jobNum].StartSkill[n] >= 0)
                    {
                        Data.Player[index].Skill[n].Num = Data.Job[jobNum].StartSkill[n];
                        Data.Player[index].Skill[n].Cd = 0;
                    }
                }

                for (n = 0; n < Enum.GetValues(typeof(Equipment)).Length; n++)
                {
                    Data.Player[index].Equipment[n] = new PlayerEq();
                    Data.Player[index].Equipment[n].Num = -1;
                }

                // set gathering skills defaults
                var resourceCount = Enum.GetValues(typeof(ResourceSkill)).Length;
                for (i = 0; i < resourceCount; i++)
                {
                    Data.Player[index].GatherSkills[i].SkillLevel = 1;
                    Data.Player[index].GatherSkills[i].SkillCurExp = 0;
                    SetPlayerGatherSkillMaxExp(index, i, (int)GetSkillNextLevel(index, i));
                }
                SaveCharacter(index, slot);
            }

        }

        public static bool IsBanned(int index, string ip)
        {
            bool isBanned = default;
            string filename;
            string line;
            int i;

            for (i = ip.Length; i >= 0; i -= 1)
            {
                if (ip.Substring(i - 1, 1) == ".")
                {
                    ip = ip.Substring(i - 1, 1);
                    break;
                }
            }

            filename = Path.Combine(DataPath.Database, "banlist.txt");

            // Check if file exists
            if (!System.IO.File.Exists(filename))
            {
                return false;
            }

            var sr = new StreamReader(filename);

            while (sr.Peek() >= 0)
            {
                // Is banned?
                line = sr.ReadLine();
                if ((line?.ToLower() ?? "") == (ip.Substring(0, Math.Min(line?.Length ?? 0, ip.Length)).ToLower() ?? ""))
                {
                    isBanned = true;
                }
            }

            sr.Close();

            if (Data.Account[index].Banned)
            {
                isBanned = true;
            }

            return isBanned;

        }

        public static void BanPlayer(int banPlayerIndex, int bannedByIndex)
        {
            string filename = Path.Combine(DataPath.Database, "banlist.txt");
            string ip;
            int i;

            // Make sure the file exists
            if (!System.IO.File.Exists(filename))
                System.IO.File.Create(filename).Dispose();

            // Cut off last portion of ip
            ip = PlayerService.Instance.ClientIp(banPlayerIndex);

            for (i = ip.Length; i >= 0; i -= 1)
            {
                if (ip.Substring(i - 1, 1) == ".")
                {
                    break;
                }
            }

            Data.Account[banPlayerIndex].Banned = true;

            ip = ip.Substring(0, i);
            Log.Add(ip, "banlist.txt");
            NetworkSend.GlobalMsg(GetPlayerName(banPlayerIndex) + " has been banned from " + SettingsManager.Instance.GameName + " by " + GetPlayerName(bannedByIndex) + "!");
            Log.Add(GetPlayerName(bannedByIndex) + " has banned " + GetPlayerName(banPlayerIndex) + ".", Constant.AdminLog);
            var task = Server.Player.LeftGame(banPlayerIndex);
            task.Wait();
        }

        public static void WriteJobDataToPacket(int jobNum, PacketWriter packetWriter)
        {
            packetWriter.WriteString(Data.Job[jobNum].Name);
            packetWriter.WriteString(Data.Job[jobNum].Desc);
            packetWriter.WriteInt32(Data.Job[jobNum].MaleSprite);
            packetWriter.WriteInt32(Data.Job[jobNum].FemaleSprite);

            for (var i = 0; i < StatCount; i++)
            {
                packetWriter.WriteInt32(Data.Job[jobNum].Stat[i]);
            }

            for (var q = 0; q < Core.Globals.Variables.MaxStartItems; q++)
            {
                packetWriter.WriteInt32(Data.Job[jobNum].StartItem[q]);
                packetWriter.WriteInt32(Data.Job[jobNum].StartValue[q]);
            }

            for (var q = 0; q < Core.Globals.Variables.MaxStartSkills; q++)
            {
                packetWriter.WriteInt32(Data.Job[jobNum].StartSkill[q]);
            }

            packetWriter.WriteInt32(Data.Job[jobNum].StartMap);
            packetWriter.WriteByte(Data.Job[jobNum].StartX);
            packetWriter.WriteByte(Data.Job[jobNum].StartY);
            packetWriter.WriteInt32(Data.Job[jobNum].BaseExp);
        }
    }
    #endregion
}