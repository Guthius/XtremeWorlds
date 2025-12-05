using System.Security.Cryptography;
using System.Text;
using Core.Common;
using Core.Globals;
using Core.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using Server.Game;
using XtremeWorlds.Server.Configuration;
using static Core.Globals.Command;
using static Core.Globals.Type;
using Path = System.IO.Path;
using Type = Core.Globals.Type;

namespace Server;

public static class Database
{
    private static readonly int StatCount = Enum.GetValues<Stat>().Length;

    private static readonly SemaphoreSlim ConnectionSemaphore = new(Variables.MaxSqlClients, Variables.MaxSqlClients);

    public static string ConnectionString { get; set; } = string.Empty;

    public static async Task CreateDatabaseAsync(string databaseName)
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

    private static async Task UpdateRowByColumnAsync(string columnName, long value, string targetColumn, string newValue, string tableName)
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

    public static async Task CreateTablesAsync()
    {
        await ConnectionSemaphore.WaitAsync();
        try
        {
            var dataTable = "id SERIAL PRIMARY KEY, data jsonb";
            var playerTable = "id BIGINT PRIMARY KEY, data jsonb, bank jsonb";

            for (int i = 1, loopTo = Variables.MaxChars; i <= loopTo; i++)
                playerTable += $", character{i} jsonb";

            var tableNames = new[] { "job", "item", "map", "npc", "shop", "skill", "resource", "animation", "projectile", "moral" };

            var tasks = tableNames.Select(tableName => CreateTableAsync(tableName, dataTable));
            await Task.WhenAll(tasks);

            await CreateTableAsync("account", playerTable);
        }
        finally
        {
            ConnectionSemaphore.Release();
        }
    }

    private static async Task CreateTableAsync(string tableName, string layout)
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

    public static async Task<List<long>> GetDataAsync(string tableName)
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

    private static async Task<bool> RowExistsAsync(long id, string table)
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

    private static async Task InsertRowByColumnAsync(long id, string data, string tableName, string dataColumn, string idColumn)
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

    public static async Task<JObject?> SelectRowAsync(long id, string tableName, string columnName)
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

    public static async Task<JObject?> SelectRowByColumnAsync(string columnName, long value, string tableName, string dataColumn)
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

    private static bool RowExistsByColumn(string columnName, long value, string tableName)
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

    private static void UpdateRowByColumn(string columnName, long value, string targetColumn, string newValue, string tableName)
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

    private static void InsertRowByColumn(long id, string data, string tableName, string dataColumn, string idColumn)
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

    public static async Task SaveAllPlayersOnlineAsync()
    {
        foreach (var i in PlayerService.Instance.PlayerIds)
        {
            if (!NetworkConfig.IsPlaying(i))
                continue;

            await SaveCharacterAsync(i, Data.TempPlayer[i].Slot);
            await SaveBankAsync(i);
        }
    }

    public static async Task SaveCharacterAsync(int index, int slot)
    {
        await Task.Run(() => SaveCharacter(index, slot));
    }

    public static async Task SaveBankAsync(int index)
    {
        await Task.Run(() => SaveBank(index));
    }

    public static async Task SaveAccountAsync(int index)
    {
        var json = JsonConvert.SerializeObject(Data.Account[index]).ToString();
        var username = GetAccountLogin(index);
        var id = GetStringHash(username);

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

        var json = JsonConvert.SerializeObject(Data.Account[index]).ToString();

        var id = GetStringHash(username);

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

    private static void ClearAccount(int index)
    {
        SetPlayerLogin(index, "");
        SetPlayerPassword(index, "");
    }

    public static void ClearPlayer(int index)
    {
        ClearAccount(index);
        ClearBank(index);

        Data.TempPlayer[index].SkillCd = new int[Variables.MaxPlayerSkills];
        Data.TempPlayer[index].TradeOffer = new PlayerInv[Variables.MaxInv];

        Data.TempPlayer[index].SkillCd = new int[Variables.MaxPlayerSkills];
        Data.TempPlayer[index].Editor = EditorType.None;
        Data.TempPlayer[index].SkillBuffer = -1;
        Data.TempPlayer[index].InShop = -1;
        Data.TempPlayer[index].InTrade = -1;
        Data.TempPlayer[index].InParty = -1;

        for (int i = 0, loopTo = Data.TempPlayer[index].EventProcessingCount; i < loopTo; i++)
            Data.TempPlayer[index].EventProcessing[i].EventId = -1;

        ClearCharacter(index);
    }

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
        var json = JsonConvert.SerializeObject(Data.Bank[index]);
        var username = GetAccountLogin(index);
        var id = GetStringHash(username);

        if (RowExistsByColumn("id", id, "account"))
        {
            UpdateRowByColumn("id", id, "bank", json, "account");
        }
        else
        {
            InsertRowByColumn(id, json, "account", "bank", "id");
        }
    }

    private static void ClearBank(int index)
    {
        Data.Bank[index].Item = new PlayerInv[global::Script.MaxBank + 1];
        for (var i = 0; i < global::Script.MaxBank; i++)
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

        Data.Player[index].Skill = new PlayerSkill[global::Script.MaxPlayerSkills];
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

        Data.Player[index].Hotbar = new Hotbar[global::Script.MaxHotbar];
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
        Data.Player[index].GatherSkills = new ResourceType[resoruceCount];
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
        var json = JsonConvert.SerializeObject(Data.Player[index]).ToString();
        var id = GetStringHash(GetAccountLogin(index));

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
            for (n = 0; n < Variables.MaxStartItems; n++)
            {
                if (Data.Job[jobNum].StartItem[n] >= 0)
                {
                    Data.Player[index].Inv[n].Num = Data.Job[jobNum].StartItem[n];
                    Data.Player[index].Inv[n].Value = Data.Job[jobNum].StartValue[n];
                }
            }

            // set start skills
            for (n = 0; n < Variables.MaxStartSkills; n++)
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

        if (Data.Account[index].Banned)
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

        Data.Account[banPlayerIndex].Banned = true;

        ip = ip.Substring(0, i);
        Log.Add(ip, "banlist.txt");
        NetworkSend.GlobalMsg(GetPlayerName(banPlayerIndex) + " has been banned from " + SettingsManager.Instance.GameName + " by " + GetPlayerName(bannedByIndex) + "!");
        Log.Add(GetPlayerName(bannedByIndex) + " has banned " + GetPlayerName(banPlayerIndex) + ".", Constant.AdminLog);
        var task = Player.LeftGame(banPlayerIndex);
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
            if (Data.Job[jobNum].Stat == null)
                return;
            packetWriter.WriteInt32(Data.Job[jobNum].Stat[i]);
        }

        for (var q = 0; q < Variables.MaxStartItems; q++)
        {
            packetWriter.WriteInt32(Data.Job[jobNum].StartItem[q]);
            packetWriter.WriteInt32(Data.Job[jobNum].StartValue[q]);
        }

        for (var q = 0; q < Variables.MaxStartSkills; q++)
        {
            packetWriter.WriteInt32(Data.Job[jobNum].StartSkill[q]);
        }

        packetWriter.WriteInt32(Data.Job[jobNum].StartMap);
        packetWriter.WriteByte(Data.Job[jobNum].StartX);
        packetWriter.WriteByte(Data.Job[jobNum].StartY);
        packetWriter.WriteInt32(Data.Job[jobNum].BaseExp);
    }
}