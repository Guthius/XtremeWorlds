using Core.Common;
using Core.Globals;
using Core.Net;
using Core.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using Microsoft.Extensions.Logging;
using Server.Game;
using System.Security.Cryptography;
using System.Text;
using XtremeWorlds.Server.Configuration;
using XtremeWorlds.Server.Database;
using static Core.Globals.Commands;
using static Core.Globals.Type;
using Path = System.IO.Path;
using Type = Core.Globals.Type;
using static Server.Globals.Commands;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace Server;

public static class Database
{
    public static CharacterNameList? CharacterList;

    private static readonly int StatCount = Enum.GetValues<Stat>().Length;

    private static readonly SemaphoreSlim ConnectionSemaphore = new(Core.Globals.Variables.MaxSqlClients, Core.Globals.Variables.MaxSqlClients);

    public static string ConnectionString { get; set; } = string.Empty;

    public static async System.Threading.Tasks.Task CreateDatabaseAsync(string databaseName)
    {
        await ConnectionSemaphore.WaitAsync();
        try
        {
            var checkDbExistsSql = $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'";
            var createDbSql = $"CREATE DATABASE {databaseName}";

            using (var connection = new NpgsqlConnection(ConnectionString.Replace("Database=mirage", "Database=postgres")))
            {
                await connection.OpenAsync();

                using (var checkCommand = new NpgsqlCommand(checkDbExistsSql, connection))
                {
                    var dbExists = await checkCommand.ExecuteScalarAsync() is not null;

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

    public static async System.Threading.Tasks.Task UpdateRowByColumnAsync(string columnName, long value, string targetColumn, string newValue, string tableName)
    {
        await ConnectionSemaphore.WaitAsync();
        try
        {
            var sql = $"UPDATE {tableName} SET {targetColumn} = @newValue::jsonb WHERE {columnName} = @value;";

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
            var dataTable = "id SERIAL PRIMARY KEY, data jsonb";
            var playerTable = "id BIGINT PRIMARY KEY, data jsonb, bank jsonb";

            for (int i = 1, count = Core.Globals.Variables.MaxCharacters; i <= count; i++)
                playerTable += $", character{i} jsonb";

            var tableNames = new[] { "job", "item", "map", "npc", "shop", "skill", "resource", "animation", "projectile", "moral" };

            var tasks = tableNames.Select(tableName => CreateTableAsync(tableName, dataTable));
            await System.Threading.Tasks.Task.WhenAll(tasks);

            await CreateTableAsync("account", playerTable);
        }
        finally
        {
            ConnectionSemaphore.Release();
        }
    }

    private static async System.Threading.Tasks.Task CreateTableAsync(string tableName, string layout)
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
                        var id = await reader.GetFieldValueAsync<long>(0);
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
            var sql = $"SELECT EXISTS (SELECT 1 FROM {table} WHERE id = @id);";

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

    public static async System.Threading.Tasks.Task InsertRowByColumnAsync(long id, string data, string tableName, string dataColumn, string idColumn)
    {
        await ConnectionSemaphore.WaitAsync();
        try
        {
            // Sanitize the data string
            data = data.Replace("\\u0000", "");

            var sql = $@"
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
            var sql = $"SELECT {columnName} FROM {tableName} WHERE id = @id;";

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
                            var jsonbData = reader.GetString(0);
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
            var sql = $"SELECT {dataColumn} FROM {tableName} WHERE {columnName} = @value;";

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
                                var jsonbData = reader.GetString(0);
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
        var sql = $"SELECT EXISTS (SELECT 1 FROM {tableName} WHERE {columnName} = @value);";

        using (var connection = new NpgsqlConnection(ConnectionString))
        {
            connection.Open();

            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@value", value);

                var exists = Convert.ToBoolean(command.ExecuteScalar());
                return exists;
            }
        }
    }

    public static void UpdateRow(long id, string data, string table, string columnName)
    {
        var sqlCheck = $"SELECT column_name FROM information_schema.columns WHERE table_name='{table}' AND column_name='{columnName}';";

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
                    var sqlUpdate = $"UPDATE {table} SET {columnName} = @data WHERE id = @id;";

                    using (var commandUpdate = new NpgsqlCommand(sqlUpdate, connection))
                    {
                        var jsonString = data.ToString();
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

    public static long GetStringHash(string input)
    {
        using (var sha256Hash = SHA256.Create())
        {
            if (input == null || input == "")
                return -1;

            // ComputeHash - returns byte array
            var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

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
        var sql = $"UPDATE {tableName} SET {targetColumn} = @newValue::jsonb WHERE {columnName} = @value;";

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
        var sql = $"SELECT EXISTS (SELECT 1 FROM {table} WHERE id = @id);";

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

    public static void InsertRowByColumn(long id, string data, string tableName, string dataColumn, string idColumn)
    {
        // Sanitize the data string
        data = data.Replace("\\u0000", "");

        var sql = $@"
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
        var sql = $"SELECT {dataColumn} FROM {tableName} WHERE {columnName} = @value;";

        if (value == -1)
            return null;

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
                            var jsonbData = reader.GetString(0);
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
    
    public static string GetVar(string filePath, string section, string key)
    {
        var isInSection = false;

        foreach (var line in File.ReadAllLines(filePath))
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
                var parts = line.Split(new char[] { '=' }, 2);
                if (parts[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return parts[1];
                }

            }
        }

        return string.Empty; // Key not found
    }

    public static async System.Threading.Tasks.Task SaveAllPlayersOnlineAsync()
    {
        foreach (var i in PlayerService.Instance.PlayerIds)
        {
            if (!NetworkConfig.IsPlaying(i))
                continue;

            await Account.OnSave(i);
        }
    }

    public static async System.Threading.Tasks.Task AddChar(int index, int slot, string name, byte sex, byte job, int sprite)
    {
        int n;
        int i;

        // Ensure backing collections are sized to accommodate the player index.
        // This prevents List<T>.set_Item out-of-range when assigning PlayerBase.Instance[index].
        Account.EnsureSize(index + 1);
        Core.Objects.PlayerBase.EnsureSize(index + 1);

        // Validate character slot against the player's character array bounds.
        if (slot < 0 || slot >= Account.Instance[index].Player.Length)
        {
            return;
        }

        if (Account.Instance[index].Player[slot].Name == "")
        {
            PlayerBase.Instance[index] = Account.Instance[index].Player[slot];
            
            Account.Instance[index].Player[slot].Name = name;
            Account.Instance[index].Player[slot].Sex = sex;
            Account.Instance[index].Player[slot].Job = job;
            Account.Instance[index].Player[slot].Sprite = sprite;
            Account.Instance[index].Player[slot].Level = 1;

            var statCount = Enum.GetValues(typeof(Stat)).Length;
            for (n = 0; n < statCount; n++)
                Account.Instance[index].Player[slot].Stat[n] = Job.Instance[job].Stat[n];

            Account.Instance[index].Player[slot].Dir = (byte)Direction.Down;
            Account.Instance[index].Player[slot].Map = Job.Instance[job].StartMap;

            if (Account.Instance[index].Player[slot].Map == 0)
                Account.Instance[index].Player[slot].Map = 1;

            Account.Instance[index].Player[slot].X = Job.Instance[job].StartX;
            Account.Instance[index].Player[slot].Y = Job.Instance[job].StartY;
            Account.Instance[index].Player[slot].Dir = (byte)Direction.Down;

            var vitalCount = Enum.GetValues(typeof(Vital)).Length;
            for (i = 0; i < vitalCount; i++)
            {
                int value = Script.Instance?.GetPlayerMaxVital(index, (Vital)i) ?? 0;
                SetVital(index, (Vital)i, value);
            }

            // set starter items
            for (n = 0; n < Core.Globals.Variables.MaxStartItems; n++)
            {
                if (Job.Instance[job].StartItem[n] >= 0)
                {
                    var startItemId = Job.Instance[job].StartItem[n];
                    Account.Instance[index].Player[slot].Inventory[n].Num = startItemId;
                    Account.Instance[index].Player[slot].Inventory[n].Value = Job.Instance[job].StartValue[n];

                    // Default starter items to full durability when applicable.
                    if (startItemId >= 0 && startItemId < Server.Item.Instance.Count)
                    {
                        var itemTemplate = Server.Item.Instance[startItemId];
                        var isStackable = itemTemplate.Type == (byte)ItemCategory.Currency || itemTemplate.Stackable == 1;
                        if (isStackable)
                        {
                            Account.Instance[index].Player[slot].Inventory[n].Durability = 0;
                        }
                        else
                        {
                            var maxDurability = Math.Max(0, itemTemplate.MaxDurability);
                            Account.Instance[index].Player[slot].Inventory[n].Durability = maxDurability > 0 ? maxDurability : 0;
                        }
                    }
                }            
            }

            // set start skills
            for (n = 0; n < Core.Globals.Variables.MaxStartSkills; n++)
            {
                if (Job.Instance[job].StartSkill[n] >= 0)
                {
                    Account.Instance[index].Player[slot].Skill[n].Num = Job.Instance[job].StartSkill[n];
                    Account.Instance[index].Player[slot].Skill[n].Cd = 0;
                }
            }

            for (n = 0; n < Enum.GetValues(typeof(Equipment)).Length; n++)
            {
                Account.Instance[index].Player[slot].Paperdoll[n] = new Paperdoll();
                Account.Instance[index].Player[slot].Paperdoll[n].Num = -1;
            }

            // set gathering skills defaults
            var resourceCount = Enum.GetValues(typeof(ResourceSkill)).Length;
            for (i = 0; i < resourceCount; i++)
            {
                Account.Instance[index].Player[slot].GatherSkills[i].Level = 1;
                Account.Instance[index].Player[slot].GatherSkills[i].Exp = 0;
                SetSkillMaxExp(index, i, (int)GetSkillMaxExp(index, i));
            }

            if (Database.CharacterList?.Count == 1)
                SetAccess(index, (int)Access.Owner);
            
            await Account.OnSave(index);
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
        if (!File.Exists(filename))
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

        if (Account.Instance[index].Banned)
        {
            isBanned = true;
        }

        return isBanned;

    }

    public static void BanPlayer(int banPlayerIndex, int bannedByIndex)
    {
        var filename = Path.Combine(DataPath.Database, "banlist.txt");
        string ip;
        int i;

        // Make sure the file exists
        if (!File.Exists(filename))
            File.Create(filename).Dispose();

        // Cut off last portion of ip
        ip = PlayerService.Instance.ClientIp(banPlayerIndex);

        for (i = ip.Length; i >= 0; i -= 1)
        {
            if (ip.Substring(i - 1, 1) == ".")
            {
                break;
            }
        }

        Account.Instance[banPlayerIndex].Banned = true;

        ip = ip.Substring(0, i);
        Log.Add(ip, "banlist.txt");
        Network.GlobalMessage(GetName(banPlayerIndex) + " has been banned from " + SettingsManager.Instance.GameName + " by " + GetName(bannedByIndex) + "!");
        Log.Add(GetName(bannedByIndex) + " has banned " + GetName(banPlayerIndex) + ".", Constant.AdminLog);
        _ = Player.OnExit(banPlayerIndex).ContinueWith(
            t => General.Logger.LogError(t.Exception, "Unhandled error during forced logout"),
            TaskContinuationOptions.OnlyOnFaulted
        );
    }
}