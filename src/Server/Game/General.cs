using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Core;
using Core.Common;
using Core.Globals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Server.Game;
using XtremeWorlds.Server.Configuration;
using XtremeWorlds.Server.Database;
using static Core.Globals.Type;
using static Core.Globals.Commands;
using Type = Core.Globals.Type;

namespace Server;

public static class General
{
    private static bool _serverDestroyed;
    private static string _myIpAddress = string.Empty;
    private static readonly Stopwatch MyStopwatch = new();
    public static ILogger Logger = NullLogger.Instance;
    private static readonly Lock SyncLock = new();
    private static readonly CancellationTokenSource Cts = new();
    private static Timer? _saveTimer;
    private static int _shutDownLastTimer;
    
    private static void SetConsoleTitleSafe(string title)
    {
        try
        {
            Console.Title = title;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Unable to set console title (no console attached)");
        }
    }
    
    public static Stopwatch GetShutDownTimer { get; } = new();
    public static RandomUtility GetRandom { get; } = new();

    public static int GetTimeMs() => (int)MyStopwatch.ElapsedMilliseconds;

    private static string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        return host.AddressList
            .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
            ?.ToString() ?? "127.0.0.1";
    }

    public static int IsValidLogin(string login)
    {
        if (string.IsNullOrWhiteSpace(login))
            return -1;

        if (login.Length < Variables.MinimumNameLength || login.Length > Variables.NameLength)
            return 0;

        return Regex.IsMatch(login, @"^[a-zA-Z0-9_ ]+$") ? 1 : -1;
    }

    public static async System.Threading.Tasks.Task InitServerAsync(IConfiguration configuration)
    {
        if (!Debugger.IsAttached)
        {
            try
            {
                await ServerStartAsync(configuration);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Server initialization failed");
                await HandleCriticalErrorAsync(ex);
            }
        }
        else
        {
            await ServerStartAsync(configuration);
        }
    }

    private static async System.Threading.Tasks.Task ServerStartAsync(IConfiguration configuration)
    {
        MyStopwatch.Start();
        var startTime = GetTimeMs();

        await InitializeCoreComponentsAsync(configuration);
        await LoadGameDataAsync();
        await StartGameLoopAsync(startTime);
    }

    private static async System.Threading.Tasks.Task InitializeCoreComponentsAsync(IConfiguration configuration)
    {
        await System.Threading.Tasks.Task.WhenAll(LoadConfigurationAsync(), InitializeNetworkAsync(), InitializeChatSystemAsync());
        await InitializeDatabaseWithRetryAsync(configuration);
    }

    public static void InitalizeCoreData()
    {
        for (var i = 0; i < Variables.MaxMaps; i++)
        {
            for (var x = 0; x < Variables.MaxMapNpcs; x++)
            {
                MapNpc.Instance[i, x].Vital = new int[Enum.GetValues(typeof(Vital)).Length];
                MapNpc.Instance[i, x].SkillCd = new int[Variables.MaxNpcSkills];
                MapNpc.Instance[i, x].Num = -1;
                MapNpc.Instance[i, x].SkillBuffer = -1;
            }

            for (var x = 0; x < Variables.MaxMapItems; x++)
            {
                MapItem.Instance[i, x].Num = -1;
            }
        }
        
        for (var i = 0; i < Player.Instance.Count; i++)
        {
            Account.OnClear(i);
        }

        for (var i = 0; i < Variables.MaxPartyMembers; i++)
        {
            Party.OnClear(i);
        }

        Event.TempEventMap = new GlobalEvents[Variables.MaxMaps];
    }

    private static async System.Threading.Tasks.Task LoadGameDataAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        await Script.OnLoadAsync(0);           
        Logger.LogInformation($"Game data loaded in {stopwatch.ElapsedMilliseconds}ms");
    }

    private static async System.Threading.Tasks.Task StartGameLoopAsync(int startTime)
    {
        InitializeSaveTimer();
        DisplayServerBanner(startTime);
        UpdateCaption();

        if (!Debugger.IsAttached)
        {
            try
            {
                await Loop.ServerAsync();
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Server loop crashed");
                await HandleCriticalErrorAsync(ex);
            }
        }
        else
        {
            await Loop.ServerAsync();
        }

    }

    private static async System.Threading.Tasks.Task DestroyServerAsync()
    {
        if (_serverDestroyed) return;
        _serverDestroyed = true;
        await Cts.CancelAsync();
        _saveTimer?.Dispose();

        Logger.LogInformation("Server shutdown initiated...");

        await Database.SaveAllPlayersOnlineAsync();

        try
        {
            await Parallel.ForEachAsync(Enumerable.Range(0, Variables.MaxPlayers), Cts.Token, async (i, _) =>
            {
                NetworkSend.SendLeftGame(i);
                await Player.OnExit(i);
            });
        }
        catch (TaskCanceledException)
        {
            Logger.LogWarning("Server shutdown tasks were canceled.");
        }
            
        Logger.LogInformation("Server shutdown completed.");
        Environment.Exit(0);
    }

    private static async System.Threading.Tasks.Task LoadConfigurationAsync()
    {
        await System.Threading.Tasks.Task.Run(() =>
        {
            ValidateConfiguration();
            Clock.Instance.GameSpeed = SettingsManager.Instance.TimeSpeed;
            SetConsoleTitleSafe("XtremeWorlds Server");
            _myIpAddress = GetLocalIpAddress();
        });
    }

    private static void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(SettingsManager.Instance.GameName))
            throw new InvalidOperationException("GameName is not set in configuration");

        if (SettingsManager.Instance.Port <= 0 || SettingsManager.Instance.Port > 65535)
            throw new InvalidOperationException("Invalid Port number in configuration");
    }

    private static System.Threading.Tasks.Task InitializeNetworkAsync()
    {
        return System.Threading.Tasks.Task.CompletedTask;
    }

    private static async System.Threading.Tasks.Task InitializeDatabaseWithRetryAsync(IConfiguration configuration)
    {
        var maxRetries = configuration.GetValue("Database:MaxRetries", 3);
        var retryDelayMs = configuration.GetValue("Database:RetryDelayMs", 1000);
        Logger.LogInformation("Initializing database...");
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var name = GetDatabaseName(configuration);
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new InvalidOperationException("Database name is missing from configuration (Database:ConnectionString).");
                }
                await Database.CreateDatabaseAsync(name);
                await Database.CreateTablesAsync();
                await LoadCharacterListAsync();
                Logger.LogInformation("Database initialized successfully.");
                return;
            }
            catch (Exception ex)
            {
                if (attempt == maxRetries)
                {
                    Logger.LogCritical(ex, "Failed to initialize database after multiple attempts");
                    throw;
                }
                Logger.LogWarning(ex, $"Database initialization failed, attempt {attempt} of {maxRetries}");
                await System.Threading.Tasks.Task.Delay(retryDelayMs * attempt, Cts.Token);
            }
        }
    }

    private static async System.Threading.Tasks.Task LoadCharacterListAsync()
    {
        var ids = await Database.GetDataAsync("account");
        Database.CharacterList = new CharacterNameList();
        const int maxConcurrency = 4;
        using var semaphore = new SemaphoreSlim(maxConcurrency);

        var tasks = ids.Select(async id =>
        {
            await semaphore.WaitAsync(Cts.Token);
            try
            {
                for (var i = 0; i < Variables.MaxCharacters; i++)
                {
                    var data = await Database.SelectRowByColumnAsync("id", id, "account", $"character{i + 1}");
                    if (data != null)
                    {
                        var name = (string?)data["Name"];
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            Database.CharacterList.Add(name);
                        }
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await System.Threading.Tasks.Task.WhenAll(tasks);
        Logger.LogInformation($"Loaded {Database.CharacterList.Count} character(s).");
    }

    public static async System.Threading.Tasks.Task LoadGameContentAsync()
    {
        const int maxConcurrency = 10;
        using var semaphore = new SemaphoreSlim(maxConcurrency);

        var tasks = new[]
        {
            LoadWithSemaphoreAsync(semaphore, async () => { Logger.LogInformation("Loading jobs..."); await Job.OnLoadAllAsync(); Logger.LogInformation("Jobs loaded."); }),
            LoadWithSemaphoreAsync(semaphore, async () => { Logger.LogInformation("Loading morals..."); await Moral.OnLoadAllAsync(); Logger.LogInformation("Morals loaded."); }),
            LoadWithSemaphoreAsync(semaphore, async () => { Logger.LogInformation("Loading npcs..."); await Npc.OnLoadAllAsync(); Logger.LogInformation("Npcs loaded."); }),
            LoadWithSemaphoreAsync(semaphore, async () => { Logger.LogInformation("Loading maps..."); await Map.OnLoadAllAsync(); Logger.LogInformation("Maps loaded."); }),
            LoadWithSemaphoreAsync(semaphore, async () => { Logger.LogInformation("Loading items..."); await Item.OnLoadAllAsync(); Logger.LogInformation("Items loaded."); }),          
            LoadWithSemaphoreAsync(semaphore, async () => { Logger.LogInformation("Loading resources..."); await Resource.OnLoadAllAsync(); Logger.LogInformation("Resources loaded."); }),
            LoadWithSemaphoreAsync(semaphore, async () => { Logger.LogInformation("Loading shops..."); await Shop.OnLoadAllAsync(); Logger.LogInformation("Shops loaded."); }),
            LoadWithSemaphoreAsync(semaphore, async () => { Logger.LogInformation("Loading skills..."); await Skill.OnLoadAllAsync(); Logger.LogInformation("Skills loaded."); }),
            LoadWithSemaphoreAsync(semaphore, async () => { Logger.LogInformation("Loading animations..."); await Animation.OnLoadAllAsync(); Logger.LogInformation("Animations loaded."); }),
            LoadWithSemaphoreAsync(semaphore, async () => { Logger.LogInformation("Loading switches..."); await Event.LoadSwitchesAsync(); Logger.LogInformation("Switches loaded."); }),
            LoadWithSemaphoreAsync(semaphore, async () => { Logger.LogInformation("Loading variables..."); await Event.LoadVariablesAsync(); Logger.LogInformation("Variables loaded."); }),
            LoadWithSemaphoreAsync(semaphore, async () => { Logger.LogInformation("Loading projectiles..."); await Projectile.OnLoadAllAsync(); Logger.LogInformation("Projectiles loaded."); }),
        };

        await System.Threading.Tasks.Task.WhenAll(tasks);
        Logger.LogInformation("Game content loaded successfully.");
    }

    private static async System.Threading.Tasks.Task LoadWithSemaphoreAsync(SemaphoreSlim semaphore, Func<System.Threading.Tasks.Task> loadFunc)
    {
        await semaphore.WaitAsync(Cts.Token);
        try
        {
            await loadFunc();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static async System.Threading.Tasks.Task SpawnGameObjectsAsync()
    {
        await System.Threading.Tasks.Task.WhenAll(
            System.Threading.Tasks.Task.Run(MapItem.SpawnAll),
            MapNpc.OnSpawnAll(),
            EventLogic.SpawnAllMapGlobalEvents()
        );
        Logger.LogInformation("Game objects spawned.");
    }

    private static void DisplayServerBanner(int startTime)
    {
        try { Console.Clear(); }
        catch
        {
            // ignored
        }

        string[] banner =
        [
            " __   ___                        __          __        _     _     ",
            @" \ \ / / |                       \ \        / /       | |   | |",
            @"  \ V /| |_ _ __ ___ _ __ ___   __\ \  /\  / /__  _ __| | __| |___ ",
            @"  > < | __| '__/ _ \ '_ ` _ \ / _ \ \/  \/ / _ \| '__| |/ _` / __|",
            @" / . \| |_| | |  __/ | | | | |  __/\  /\  / (_) | |  | | (_| \__ \",
            @"/_/ \_\\__|_|  \___|_| |_| |_|\___| \/  \/ \___/|_|  |_|\__,_|___/"
        ];

        foreach (var line in banner) Console.WriteLine(line);
        Console.WriteLine($"Initialization complete. Server loaded in {GetTimeMs() - startTime}ms.");
        Console.WriteLine("Use /help for available commands.");
    }

    private static int CountPlayersOnline()
    {
        lock (SyncLock)
        {
            return PlayerService.Instance.PlayerIds.Count(NetworkConfig.IsPlaying);
        }
    }

    public static void UpdateCaption()
    {
        try
        {
            var title = $"{SettingsManager.Instance.GameName} <IP {_myIpAddress}:{SettingsManager.Instance.Port}> " +
                        $"({CountPlayersOnline()} Players Online) - Errors: {Global.ErrorCount} - Time: {Clock.Instance}";
            SetConsoleTitleSafe(title);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update console title");
            SetConsoleTitleSafe(SettingsManager.Instance.GameName);
        }
    }

    private static void InitializeSaveTimer()
    {
        int intervalMinutes = Variables.SaveInterval;
        _saveTimer = new Timer(async void (_) =>
            {
                try
                {
                    await SavePlayersPeriodicallyAsync();
                }
                catch
                {
                    // ignored
                }
            }, null,
            TimeSpan.FromMinutes(intervalMinutes), 
            TimeSpan.FromMinutes(intervalMinutes));
    }

    private static async System.Threading.Tasks.Task SavePlayersPeriodicallyAsync()
    {
        try
        {
            await Database.SaveAllPlayersOnlineAsync();
            Logger.LogInformation("Periodic player save completed.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Periodic player save failed");
        }
    }

    private static async System.Threading.Tasks.Task SendServerAnnouncementAsync(string message)
    {
        await Parallel.ForEachAsync(Enumerable.Range(0, Variables.MaxPlayers), Cts.Token, (i, _) =>
        {
            if (NetworkConfig.IsPlaying(i))
                NetworkSend.SendPlayerMessage(i, message, (int)ColorName.Yellow);
            return new ValueTask();
        });
        Logger.LogInformation("Server announcement sent.");
    }

    private static System.Threading.Tasks.Task InitializeChatSystemAsync()
    {
        Logger.LogInformation("Chat system initialized.");
        // Additional initialization logic can be added here if needed
        return System.Threading.Tasks.Task.CompletedTask;
    }

    public static async System.Threading.Tasks.Task HandlePlayerCommandAsync(string[] command)
    {
        // Defensive: command[1] may not exist for some commands
        var player = -1;
        if (command.Length > 1)
            player = GameLogic.FindPlayer(command[1]);

        if (command is ["/help"]) await SendHelpMessageAsync();

        if (player == -1 || !NetworkConfig.IsPlaying(player))
        {
            return;
        }
        switch (command[0].ToLower())
        {
            case "/teleport":
                if (int.TryParse(command[2], out var x) && int.TryParse(command[3], out var y))
                    await TeleportPlayerAsync(player, x, y);
                break;

            case "/kick":               
                await KickPlayerAsync(player);
                break;

            case "/broadcast":
                await BroadcastMessageAsync(player, string.Join(" ", command[2..]));
                break;

            case "/status":
                await SendServerStatusAsync(player);
                break;

            case "/whisper":
                await SendWhisperAsync(player, "Server", string.Join(" ", command[2..]));
                break;
            
            case "/save":
                await SavePlayerDataAsync(player);
                break;

            case "/shutdown":
            {
                if (GetShutDownTimer.IsRunning)
                {
                    GetShutDownTimer.Stop();
                    Console.WriteLine("Server shutdown has been cancelled!");
                    NetworkSend.SendGlobalMessage("Server shutdown has been cancelled!");
                }
                else
                {
                    if (GetShutDownTimer.ElapsedTicks > 0)
                    {
                        GetShutDownTimer.Restart();
                    }
                    else
                    {
                        GetShutDownTimer.Start();
                    }

                    Console.WriteLine("Server shutdown in " + Variables.ServerShutdown + " seconds!");
                    NetworkSend.SendGlobalMessage("Server shutdown in " + Variables.ServerShutdown + " seconds!");
                }
                break;
            }

            case "/exit":
            {
                await DestroyServerAsync();
                break;
            }

            case "/access":
            {
                if (!byte.TryParse(command[2], out var access))
                {
                    Console.WriteLine("Invalid access level.");
                    break;
                }

                switch (access)
                {
                    case (byte)AccessLevel.Player:
                        SetPlayerAccess(player, access);
                        NetworkSend.SendPlayerData(player);
                        await Account.OnSave(player);
                        NetworkSend.SendPlayerMessage(player, "Your access has been set to Player!", (int)ColorName.Yellow);
                        Console.WriteLine("Successfully set the access level to " + access + " for player " + GetPlayerName(player));
                        break;
                    case (byte)AccessLevel.Moderator:
                        SetPlayerAccess(player, access);
                        NetworkSend.SendPlayerData(player);
                        await Account.OnSave(player);
                        NetworkSend.SendPlayerMessage(player, "Your access has been set to Moderator!", (int)ColorName.Yellow);
                        Console.WriteLine("Successfully set the access level to " + access + " for player " + GetPlayerName(player));
                        break;
                    case (byte)AccessLevel.Mapper:
                        SetPlayerAccess(player, access);
                        NetworkSend.SendPlayerData(player);
                        await Account.OnSave(player);
                        NetworkSend.SendPlayerMessage(player, "Your access has been set to Mapper!", (int)ColorName.Yellow);
                        Console.WriteLine("Successfully set the access level to " + access + " for player " + GetPlayerName(player));
                        break;
                    case (byte)AccessLevel.Developer:
                        SetPlayerAccess(player, access);
                        NetworkSend.SendPlayerData(player);
                        await Account.OnSave(player);
                        NetworkSend.SendPlayerMessage(player, "Your access has been set to Developer!", (int)ColorName.Yellow);
                        Console.WriteLine("Successfully set the access level to " + access + " for player " + GetPlayerName(player));
                        break;
                    case (byte)AccessLevel.Owner:
                        SetPlayerAccess(player, access);
                        NetworkSend.SendPlayerData(player);
                        await Account.OnSave(player);
                        NetworkSend.SendPlayerMessage(player, "Your access has been set to Owner!", (int)ColorName.Yellow);
                        Console.WriteLine("Successfully set the access level to " + access + " for player " + GetPlayerName(player));
                        break;
                    default:
                        Console.WriteLine("Failed to set the access level to " + access + " for player " + GetPlayerName(player));
                        break;
                            
                }
                break;
            }

            case "/ban":
            {
                Account.Instance[player].Banned = true;
                var task = Player.OnExit(player);
                task.Wait();
                Console.WriteLine($"Player {GetPlayerName(player)} has been banned by the server.");
                        
                break;
            }

            case "/timespeed":
            {
                if (!double.TryParse(command[1], out var speed))
                {
                    Console.WriteLine("Invalid speed value.");
                    break;
                }
                Clock.Instance.GameSpeed = speed;
                SettingsManager.Instance.TimeSpeed = speed;
                SettingsManager.Save();
                Console.WriteLine("Set GameSpeed to " + Clock.Instance.GameSpeed + " secs per seconds");
                break;
            }

            default:
                Console.WriteLine("Unknown command. Use /help for assistance.");
                break;
        }
    }

    private static System.Threading.Tasks.Task TeleportPlayerAsync(int playerIndex, int x, int y)
    {
        try
        {               
            var player = Player.Instance[playerIndex];

            if (x < 0 || x >= Server.Map.Instance[player.Map].MaxX || y < 0 || y >= Server.Map.Instance[player.Map].MaxY)
            {
                NetworkSend.SendPlayerMessage(playerIndex, "Invalid coordinates for teleportation.", (int)ColorName.BrightRed);
                return System.Threading.Tasks.Task.CompletedTask;
            }

            player.X = x;
            player.Y = y;
            NetworkSend.SendPlayerXYToMap(playerIndex);
            Logger.LogInformation($"Player {playerIndex} teleported to ({x}, {y})");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to teleport player {playerIndex}");
            NetworkSend.SendPlayerMessage(playerIndex, "Teleport failed.", (int)ColorName.BrightRed);
        }
        return System.Threading.Tasks.Task.CompletedTask;
    }

    private static async System.Threading.Tasks.Task KickPlayerAsync(int playerIndex)
    {
        try
        {
            if (NetworkConfig.IsPlaying(playerIndex))
            {
                NetworkSend.SendLeftGame(playerIndex);
                await Player.OnExit(playerIndex);
                Logger.LogInformation($"Player {playerIndex} kicked by server!");
                NetworkSend.SendPlayerMessage(playerIndex, $"Player {playerIndex} has been kicked.", (int)ColorName.BrightGreen);
            }
            else
            {
                NetworkSend.SendPlayerMessage(playerIndex, "Target player is not online.", (int)ColorName.BrightRed);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to kick player {playerIndex}");
            NetworkSend.SendPlayerMessage(playerIndex, "Kick operation failed.", (int)ColorName.BrightRed);
        }
    }

    private static async System.Threading.Tasks.Task BroadcastMessageAsync(int playerIndex, string message)
    {
        try
        {
            if (!await IsAdminAsync(playerIndex))
            {
                NetworkSend.SendPlayerMessage(playerIndex, "You are not authorized to broadcast.", (int)ColorName.BrightRed);
                return;
            }

            await SendChatMessageAsync(playerIndex, "global", $"[Broadcast] {message}", ColorName.BrightGreen);
            Logger.LogInformation($"Broadcast by {playerIndex}: {message}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Broadcast failed");
            NetworkSend.SendPlayerMessage(playerIndex, "Broadcast failed.", (int)ColorName.BrightRed);
        }
    }

    private static System.Threading.Tasks.Task SendServerStatusAsync(int playerIndex)
    {
        try
        {
            var status = $"Players Online: {CountPlayersOnline()}\n" +
                         $"Uptime: {MyStopwatch.Elapsed}\n" +
                         $"Errors: {Global.ErrorCount}";
            NetworkSend.SendPlayerMessage(playerIndex, status, (int)ColorName.BrightGreen);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send server status");
            NetworkSend.SendPlayerMessage(playerIndex, "Unable to retrieve server status.", (int)ColorName.BrightRed);
        }
        return System.Threading.Tasks.Task.CompletedTask;
    }

    private static System.Threading.Tasks.Task SendHelpMessageAsync()
    {
        var help = "Available Commands:\n" +
                   "/teleport <x> <y> - Teleport to coordinates\n" +
                   "/kick <player> - Kick a player (admin only)\n" +
                   "/broadcast <message> - Send a message to all players (admin only)\n" +
                   "/status - View server status\n" +
                   "/whisper <player> <message> - Send a private message\n" +
                   "/exit - Shutdown the server\n" +
                   "/ban <player> - Ban a player\n" +
                   "/shutdown - Initiate server shutdown\n" +
                   "/stats - View player statistics\n" +
                   "/access <player> <level> - Set player access level (1-5)\n" +
                   "/save - Manually save player data\n" +
                   "/help - Show this message";
        Console.WriteLine(help);
        return System.Threading.Tasks.Task.CompletedTask;
    }

    private static async System.Threading.Tasks.Task SendWhisperAsync(int senderIndex, string targetName, string message)
    {
        try
        {
            var targetIndex = await FindPlayerByNameAsync(targetName);
            if (targetIndex == -1)
            {
                NetworkSend.SendPlayerMessage(senderIndex, $"Player '{targetName}' not found.", (int)ColorName.BrightRed);
                return;
            }

            await SendChatMessageAsync(senderIndex, $"private:{targetIndex}", $"[Whisper] {message}", ColorName.BrightCyan);
            Logger.LogInformation($"Whisper from {senderIndex} to {targetIndex}: {message}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to send whisper from {senderIndex} to {targetName}");
            NetworkSend.SendPlayerMessage(senderIndex, "Failed to send whisper.", (int)ColorName.BrightRed);
        }
    }

    private static async System.Threading.Tasks.Task SavePlayerDataAsync(int playerIndex)
    {
        try
        {
            await Account.OnSave(playerIndex); // Assuming this method exists
            NetworkSend.SendPlayerMessage(playerIndex, "Your data has been saved.", (int)ColorName.BrightGreen);
            Logger.LogInformation($"Player {playerIndex} data saved manually.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to save data for player {playerIndex}");
            NetworkSend.SendPlayerMessage(playerIndex, "Failed to save data.", (int)ColorName.BrightRed);
        }
    }

    private static Task<int> FindPlayerByNameAsync(string name)
    {
        for (var i = 0; i < Player.Instance.Count; i++)
        {
            if (NetworkConfig.IsPlaying(i) && Player.Instance[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return System.Threading.Tasks.Task.FromResult(i);
            }
        }
        return System.Threading.Tasks.Task.FromResult(-1);
    }

    private static async System.Threading.Tasks.Task SendChatMessageAsync(int senderIndex, string channel, string message, ColorName color)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 200) // Basic filtering
            {
                NetworkSend.SendPlayerMessage(senderIndex, "Invalid message.", (int)ColorName.BrightRed);
                return;
            }

            if (channel.StartsWith("private:"))
            {
                var targetIndex = int.Parse(channel.Split(':')[1]);
                NetworkSend.SendPlayerMessage(targetIndex, $"[From {Player.Instance[senderIndex].Name}] {message}", (int)color);
                NetworkSend.SendPlayerMessage(senderIndex, $"[To {Player.Instance[targetIndex].Name}] {message}", (int)color);
            }
            else if (channel == "party" && Data.TempPlayer[senderIndex].InParty != 0)
            {
                await Parallel.ForEachAsync(Enumerable.Range(0, Variables.MaxPlayers), Cts.Token, (i, _) =>
                {
                    if (NetworkConfig.IsPlaying(i) && Data.TempPlayer[i].InParty == Data.TempPlayer[senderIndex].InParty)
                        NetworkSend.SendPlayerMessage(i, $"[Party] {Player.Instance[senderIndex].Name}: {message}", (int)color);
                    return new ValueTask();
                });
            }
            else if (channel == "global")
            {
                await Parallel.ForEachAsync(Enumerable.Range(0, Variables.MaxPlayers), Cts.Token, (i, _) =>
                {
                    if (NetworkConfig.IsPlaying(i))
                        NetworkSend.SendPlayerMessage(i, message, (int)color);
                    return new ValueTask();
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to send chat message from {senderIndex} to {channel}");
            NetworkSend.SendPlayerMessage(senderIndex, "Failed to send message.", (int)ColorName.BrightRed);
        }
    }

    private static Task<bool> IsAdminAsync(int playerIndex) =>
        System.Threading.Tasks.Task.FromResult(playerIndex == 0);

    private static async System.Threading.Tasks.Task BackupDatabaseAsync()
    {
        try
        {
            var backupDir = DataPath.Database;
            Directory.CreateDirectory(backupDir);
            var backupPath = Path.Combine(backupDir, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
            //await Database.BackupAsync(backupPath); // Assuming this method exists
            Logger.LogInformation($"Database backup created: {backupPath}");

            var backups = Directory.GetFiles(backupDir, "backup_*.bak")
                .OrderByDescending(f => f)
                .Skip(Variables.MaxBackups)
                .ToList();
            foreach (var oldBackup in backups)
            {
                await System.Threading.Tasks.Task.Run(() => File.Delete(oldBackup), Cts.Token);
                Logger.LogInformation($"Deleted old backup: {oldBackup}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Database backup failed");
        }
    }
        
    private static async System.Threading.Tasks.Task HandleCriticalErrorAsync(Exception ex)
    {
        await BackupDatabaseAsync();
        Logger.LogCritical(ex, "Critical error occurred. Initiating emergency shutdown");
        await SendServerAnnouncementAsync("Server shutting down due to critical error.");
        await DestroyServerAsync();
    }

    public static async System.Threading.Tasks.Task CheckShutDownCountDownAsync()
    {
        if (GetShutDownTimer.ElapsedTicks <= 0) return;

        var time = GetShutDownTimer.Elapsed.Seconds;
        if (_shutDownLastTimer != time)
        {
            if (Variables.ServerShutdown - time <= 10)
            {
                NetworkSend.SendGlobalMessage($"Server shutdown in {Variables.ServerShutdown - time} seconds!");
                Console.WriteLine($"Server shutdown in {Variables.ServerShutdown - time} seconds!");
                if (Variables.ServerShutdown - time <= 1)
                {
                    await DestroyServerAsync();
                }
            }
            _shutDownLastTimer = time;
        }
    }

    private static string? GetDatabaseName(IConfiguration configuration)
    {
        var connStr = configuration["Database:ConnectionString"];
        if (string.IsNullOrEmpty(connStr))
            return null;
        var builder = new DbConnectionStringBuilder { ConnectionString = connStr };
        return builder.TryGetValue("Database", out var dbNameObj) ? dbNameObj.ToString() : null;
    }
}