using Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using Core.Globals;
using Core.Net;
using Server.Game;
using Server.Game.Net;
using Server.Net;
using static Core.Globals.Commands;
using static Core.Net.Packets;
using static Core.Globals.Type;
using EventCommand = Core.Globals.EventCommand;
using Type = Core.Globals.Type;

namespace Server
{
    public class Event
    {
        #region Globals

        public static GlobalEvents[] TempEventMap = new GlobalEvents[Core.Globals.Variables.MaxMaps + 1];
        public static string[] Switches = new string[Core.Globals.Variables.MaxSwitches];
        public static string[] Variables = new string[Core.Globals.Variables.MaxVariables];
        private static readonly ConcurrentBag<ScheduledEvent> ScheduledEvents = new ConcurrentBag<ScheduledEvent>();
        private static readonly object TempEventLock = new object();

        internal const int PathfindingType = 0; // 0: None, 1: Random, 2: BFS (existing), 3: A* (new)

        // Effect Constants
        public const int EffectTypeFadeIn = 2;
        public const int EffectTypeFadeOut = 0;
        public const int EffectTypeFlash = 3;
        public const int EffectTypeFog = 4;
        public const int EffectTypeWeather = 5;
        public const int EffectTypeTint = 6;
        public const int EffectTypeScreenShake = 7; // New effect

        #endregion

        #region Database

        private static readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static void CreateSwitches()
        {
            Switches = new string[Core.Globals.Variables.MaxSwitches];
            Array.Fill(Switches, string.Empty);
            SaveSwitches();
            General.Logger.LogInformation("Switches initialized and saved.");
        }

        public static void CreateVariables()
        {
            Variables = new string[Core.Globals.Variables.MaxVariables];
            Array.Fill(Variables, string.Empty);
            SaveVariables();
            General.Logger.LogInformation("Variables initialized and saved.");
        }

        public static void SaveSwitches()
        {
            try
            {
                var jsonPath = System.IO.Path.Combine(DataPath.Database, "Switches.json");
                var json = JsonSerializer.Serialize(Switches, options);
                
                File.WriteAllText(jsonPath, json);
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "Failed to save Switches.");
                throw;
            }
        }

        public static void SaveVariables()
        {
            try
            {
                var jsonPath = System.IO.Path.Combine(DataPath.Database, "Variables.json");
                var json = JsonSerializer.Serialize(Variables, options);
                
                File.WriteAllText(jsonPath, json);
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "Failed to save Variables.");
                throw;
            }
        }

        public static async System.Threading.Tasks.Task LoadSwitchesAsync()
        {
            try
            {
                var jsonPath = System.IO.Path.Combine(DataPath.Database, "Switches.json");
                var json = await File.ReadAllTextAsync(jsonPath);
                
                Switches = JsonSerializer.Deserialize<string[]>(json, options) ?? [];
                
                if (Switches.Length != Core.Globals.Variables.MaxSwitches)
                {
                    General.Logger.LogWarning("Switches.json not found or invalid. Creating new switches.");
                    CreateSwitches();
                }
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "Failed to load Switches.json. Creating new switches.");
                
                CreateSwitches();
            }
        }

        public static async System.Threading.Tasks.Task LoadVariablesAsync()
        {
            try
            {
                var jsonPath = System.IO.Path.Combine(DataPath.Database, "Variables.json");
                var json = await File.ReadAllTextAsync(jsonPath);
                
                Variables = JsonSerializer.Deserialize<string[]>(json, options) ?? [];

                if (Variables.Length != Core.Globals.Variables.MaxVariables)
                {
                    General.Logger.LogWarning("Variables.json not found or invalid. Creating new variables.");
                    CreateVariables();
                }
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "Failed to load Variables.json. Creating new variables.");
                CreateVariables();
            }
        }

        #endregion

        #region Movement

        private sealed class ActiveEventMove
        {
            public int RemainingPixels;
            public byte Dir;
            public int Speed;
        }

        // Tracks active 1-tile event steps so we can send a stop packet after exactly 32px.
        // Key: (map, eventId, playerKey, globalEvent)
        private static readonly ConcurrentDictionary<(int map, int eventId, int playerKey, bool globalEvent), ActiveEventMove>
            ActiveMoves = new();

        private static (int map, int eventId, int playerKey, bool globalEvent) GetMoveKey(int map, int eventId, int index, bool globalEvent) =>
            (map, eventId, globalEvent ? -1 : index, globalEvent);

        public static bool IsMoving(int map, int eventId, int index, bool globalEvent)
        {
            if (map < 0 || map >= Core.Globals.Variables.MaxMaps) return false;
            var key = GetMoveKey(map, eventId, index, globalEvent);
            return ActiveMoves.TryGetValue(key, out var m) && m.RemainingPixels > 0;
        }

        public static void CancelMove(int map, int eventId, int index, bool globalEvent)
        {
            if (map < 0 || map >= Core.Globals.Variables.MaxMaps) return;
            var key = GetMoveKey(map, eventId, index, globalEvent);
            ActiveMoves.TryRemove(key, out _);
        }

        private static bool IsTileWalkable(int map, int x, int y)
        {
            if (map < 0 || map >= Core.Globals.Variables.MaxMaps)
                return false;

            var mapInstance = Server.Map.Instance[map];
            if (mapInstance?.Tile == null)
                return false;

            // Prefer actual tile array bounds (more robust than MaxX/MaxY if they ever diverge).
            int maxX = mapInstance.Tile.GetLength(0) - 1;
            int maxY = mapInstance.Tile.GetLength(1) - 1;
            if (x < 0 || x > maxX || y < 0 || y > maxY)
                return false;

            var tile = mapInstance.Tile[x, y];

            // Any non-blocked tile is walkable. Special tiles (warp, item, npc spawn, etc.) remain walkable.
            if (tile.Type == TileType.Blocked || tile.Type2 == TileType.Blocked) return false;

            return true;
        }

        private static bool IsPlayerBlocking(int index, int map, int x, int y, int eventId)
        {
            foreach (var i in PlayerService.Instance.PlayerIds)
            {
                if (NetworkConfig.IsPlaying(i) && GetPlayerMap(i) == map && GetPlayerX(i) == x && GetPlayerY(i) == y)
                {
                    if (Server.Map.Instance[map].Event[eventId].Pages[Data.TempPlayer[index].EventMap.EventPages[eventId].PageId].Trigger == 1)
                    {
                        StartEventProcessing(index, eventId, map);
                    }

                    return true;
                }
            }

            return false;
        }

        private static void StartEventProcessing(int index, int eventId, int map)
        {
            var pageId = Data.TempPlayer[index].EventMap.EventPages[eventId].PageId;
            if (Server.Map.Instance[map].Event[eventId].Pages[pageId].CommandListCount <= 0) return;

            ref var processing = ref Data.TempPlayer[index].EventProcessing[eventId];
            processing.Active = 1;
            processing.ActionTimer = General.GetTime();
            processing.CurList = 0;
            processing.CurSlot = 0;
            processing.EventId = eventId;
            processing.PageId = pageId;
            processing.WaitingForResponse = 0;
            processing.ListLeftOff = new int[Server.Map.Instance[map].Event[eventId].Pages[pageId].CommandListCount];
            Array.Fill(processing.ListLeftOff, -1);
        }

        private static bool IsNpcBlocking(int map, int x, int y)
        {
            for (var i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
            {
                if (MapNpc.Instance[map, i].Num >= 0 &&
                    (int)Math.Floor((double)MapNpc.Instance[map, i].X / Constants.TileSize) == x &&
                    (int)Math.Floor((double)MapNpc.Instance[map, i].Y / Constants.TileSize) == y)
                    return true;
            }

            return false;
        }

        private static bool IsDirectionBlocked(int map, int x, int y, byte dir) =>
            IsDirBlocked(Server.Map.Instance[map].Tile[x, y].DirBlock, (Direction)dir);

        public static bool CanMove(int index, int map, int x, int y, int eventId, int walkThrough, byte dir, bool globalEvent = false)
        {
            if (!IsValidMapAndDirection(map, dir)) return false;

            int targetX = x, targetY = y;
            switch (dir)
            {
                case (byte)Direction.Up: targetY--; break;
                case (byte)Direction.Down: targetY++; break;
                case (byte)Direction.Left: targetX--; break;
                case (byte)Direction.Right: targetX++; break;
                default: return false;
            }

            // Event X/Y are tile coordinates, not pixels.
            int realX = targetX;
            int realY = targetY;

            if (realX < 0 || realX > Server.Map.Instance[map].MaxX || realY < 0 || realY > Server.Map.Instance[map].MaxY) return false;
            if (walkThrough == 1) return true;

            bool walkable = IsTileWalkable(map, realX, realY);
            bool playerBlocking = IsPlayerBlocking(index, map, realX, realY, eventId);
            bool npcBlocking = IsNpcBlocking(map, realX, realY);
            bool directionBlocked = IsDirectionBlocked(map, realX, realY, dir);

            return walkable &&
                   !playerBlocking &&
                   !npcBlocking &&
                   !directionBlocked;
        }

        private static bool IsValidMapAndDirection(int map, byte dir) =>
            map >= 0 && map < Core.Globals.Variables.MaxMaps && dir >= 0 && dir <= System.Enum.GetValues(typeof(Direction)).Length;

        public static void Dir(int playerIndex, int map, int eventId, byte dir, bool globalEvent = false)
        {
            if (!IsValidMapAndDirection(map, dir)) return;

            var eventIndex = GetEventIndex(playerIndex, eventId, globalEvent);
            if (eventIndex == -1) return;

            lock (TempEventLock)
            {
                if (globalEvent)
                {
                    if (Server.Map.Instance[map].Event[eventId].Pages[0].DirFix == 0)
                        TempEventMap[map].Event[eventId].Dir = dir;
                }
                else if (Server.Map.Instance[map].Event[eventId].Pages[Data.TempPlayer[playerIndex].EventMap.EventPages[eventIndex].PageId].DirFix == 0)
                    Data.TempPlayer[playerIndex].EventMap.EventPages[eventIndex].Dir = dir;
            }

            if (globalEvent)
            {
                SendEventDirection(map, eventId, TempEventMap[map].Event[eventId].Dir);
            }
            else
            {
                SendEventDirection(map, eventId, Data.TempPlayer[playerIndex].EventMap.EventPages[eventIndex].Dir, playerIndex);
            }
        }

        private static int GetEventIndex(int playerIndex, int eventId, bool globalEvent)
        {
            if (globalEvent) return eventId;
            if (Data.TempPlayer[playerIndex].EventMap.CurrentEvents <= 0) return -1;

            for (var i = 0; i < Data.TempPlayer[playerIndex].EventMap.CurrentEvents; i++)
            {
                if (eventId == i)
                    return i;
            }

            return -1;
        }

        private static void SendEventDirection(int map, int eventId, int currentDir, int index = -1)
        {
            var packetWriter = new PacketWriter(12);

            packetWriter.WriteEnum(ServerPackets.SEventDir);
            packetWriter.WriteInt32(eventId);
            packetWriter.WriteInt32(currentDir);

            if (index == -1)
            {
                NetworkConfig.SendDataToMap(map, packetWriter.GetBytes());
            }
            else
            {
                PlayerService.Instance.SendDataTo(index, packetWriter.GetBytes());
            }
        }

        public static void OnMove(int index, int map, int eventId, byte dir, int movementSpeed, bool globalEvent = false)
        {
            if (!IsValidMapAndDirection(map, dir)) return;

            var eventIndex = GetEventIndex(index, eventId, globalEvent);
            if (eventIndex == -1) return;

            // Prevent issuing a new step while we're already mid-step.
            var moveKey = GetMoveKey(map, eventId, index, globalEvent);
            if (ActiveMoves.TryGetValue(moveKey, out var existing) && existing.RemainingPixels > 0)
            {
                return;
            }

            lock (TempEventLock)
            {
                if (globalEvent)
                {
                    ref var eventData = ref TempEventMap[map].Event[eventIndex];
                    if (Server.Map.Instance[map].Event[eventId].Pages[0].DirFix == 0)
                        eventData.Dir = dir;

                    // Start-of-step: send current tile coords (client will pixel-step 32px).
                    SendEventMove(map, eventId, eventData.X, eventData.Y, dir, eventData.Dir, movementSpeed);
                }
                else
                {
                    ref var eventData = ref Data.TempPlayer[index].EventMap.EventPages[eventIndex];
                    if (Server.Map.Instance[map].Event[eventId].Pages[Data.TempPlayer[index].EventMap.EventPages[eventIndex].PageId].DirFix == 0)
                        eventData.Dir = dir;

                    // Start-of-step: send current tile coords to the owning player.
                    SendEventMove(map, eventId, eventData.X, eventData.Y, dir, eventData.Dir, movementSpeed, index);
                }
            }

            // Begin a new 1-tile move (32px). Completion is handled by ProcessActiveEventMovement.
            var next = new ActiveEventMove { RemainingPixels = Constants.TileSize, Dir = dir, Speed = movementSpeed };
            ActiveMoves[moveKey] = next;
        }

        /// <summary>
        /// Advances active event movement by 1px per tick; when a tile step completes (32px),
        /// commits the tile coordinate and sends SEventDir to stop client movement.
        /// </summary>
        public static void OnMove()
        {
            foreach (var kvp in ActiveMoves)
            {
                var key = kvp.Key;
                var state = kvp.Value;
                if (state.RemainingPixels <= 0)
                {
                    ActiveMoves.TryRemove(key, out _);
                    continue;
                }

                state.RemainingPixels -= 1;
                if (state.RemainingPixels > 0)
                {
                    continue;
                }

                // Step finished.
                ActiveMoves.TryRemove(key, out _);

                int map = key.map;
                int eventId = key.eventId;
                int playerKey = key.playerKey;
                bool globalEvent = key.globalEvent;
                int dir = state.Dir;

                if (map < 0 || map >= Core.Globals.Variables.MaxMaps) continue;

                lock (TempEventLock)
                {
                    if (globalEvent)
                    {
                        if (TempEventMap[map].EventCount <= eventId) continue;
                        ref var ev = ref TempEventMap[map].Event[eventId];
                        ApplyDirDelta(ref ev.X, ref ev.Y, map, dir);
                        SendEventDirection(map, eventId, ev.Dir);
                    }
                    else
                    {
                        var playerId = playerKey;
                        if (!NetworkConfig.IsPlaying(playerId)) continue;
                        var eventIndex = GetEventIndex(playerId, eventId, false);
                        if (eventIndex == -1) continue;

                        ref var ev = ref Data.TempPlayer[playerId].EventMap.EventPages[eventIndex];
                        ApplyDirDelta(ref ev.X, ref ev.Y, map, dir);
                        SendEventDirection(map, eventId, ev.Dir, playerId);
                    }
                }
            }
        }

        private static void ApplyDirDelta(ref int tileX, ref int tileY, int map, int dir)
        {
            int x = tileX;
            int y = tileY;
            switch ((Direction)dir)
            {
                case Direction.Up: y--; break;
                case Direction.Down: y++; break;
                case Direction.Left: x--; break;
                case Direction.Right: x++; break;
            }

            // Clamp within map bounds.
            x = Math.Max(0, Math.Min(x, Server.Map.Instance[map].MaxX));
            y = Math.Max(0, Math.Min(y, Server.Map.Instance[map].MaxY));
            tileX = x;
            tileY = y;
        }

        private static void SendEventMove(int map, int eventId, int x, int y, byte dir, byte currentDir, int speed, int index = -1)
        {
            var packetWriter = new PacketWriter(24);

            packetWriter.WriteEnum(ServerPackets.SEventMove);
            packetWriter.WriteInt32(eventId);
            packetWriter.WriteInt32(x);
            packetWriter.WriteInt32(y);
            packetWriter.WriteByte(dir);
            packetWriter.WriteByte(currentDir);
            packetWriter.WriteInt32(speed);

            if (index == -1)
            {
                NetworkConfig.SendDataToMap(map, packetWriter.GetBytes());
            }
            else
            {
                PlayerService.Instance.SendDataTo(index, packetWriter.GetBytes());
            }
        }

        public static bool IsOneBlockAway(int x1, int y1, int x2, int y2) =>
            (x1 == x2 && (y1 == y2 - 1 || y1 == y2 + 1)) || (y1 == y2 && (x1 == x2 - 1 || x1 == x2 + 1));

        public static byte GetNpcDir(int x, int y, int x1, int y1)
        {
            byte direction = (byte)Direction.Right;
            var maxDistance = 0;
            UpdateDirectionAndDistance(x - x1, (int) Direction.Right, (int) Direction.Left, ref direction, ref maxDistance);
            UpdateDirectionAndDistance(y - y1, (int) Direction.Down, (int) Direction.Up, ref direction, ref maxDistance);
            return direction;
        }

        private static void UpdateDirectionAndDistance(int diff, int posDir, int negDir, ref byte direction, ref int maxDistance)
        {
            var absDiff = Math.Abs(diff);
            if (absDiff > maxDistance)
            {
                direction = (byte) (diff > 0 ? posDir : negDir);
                maxDistance = absDiff;
            }
        }

        public static byte CanMoveTowardsPlayer(int playerId, int map, int eventId)
        {
            if (!IsValidPlayerEvent(playerId, map, eventId)) return 4; // Invalid direction as failure

            var (px, py, ex, ey, walkThrough) = GetPlayerAndEventPositions(playerId, map, eventId);
            var dir = PathfindingType switch
            {
                1 => RandomMoveTowardsPlayer(playerId, map, eventId, ex, ey, px, py, walkThrough),
                2 => BfsMoveTowardsPlayer(playerId, map, eventId, ex, ey, px, py, walkThrough),
                3 => AStarMoveTowardsPlayer(playerId, map, eventId, ex, ey, px, py, walkThrough), // New A* pathfinding
                _ => RandomDirection()
            };

            return (byte)dir;
        }

        private static bool IsValidPlayerEvent(int playerId, int map, int eventId) =>
            playerId >= 0 && playerId < Core.Globals.Variables.MaxPlayers &&
            map >= 0 && map < Core.Globals.Variables.MaxMaps &&
            eventId >= 0 && eventId < Data.TempPlayer[playerId].EventMap.CurrentEvents;

        private static (int px, int py, int ex, int ey, int walkThrough) GetPlayerAndEventPositions(int playerId, int map, int eventId)
        {
            int px = GetPlayerX(playerId), py = GetPlayerY(playerId);
            var eventPage = Data.TempPlayer[playerId].EventMap.EventPages[eventId];
            return (px, py, eventPage.X, eventPage.Y,
                Server.Map.Instance[map].Event[eventPage.EventId].Pages[eventPage.PageId].WalkThrough);
        }

        private static int RandomMoveTowardsPlayer(int playerId, int map, int eventId, int ex, int ey, int px, int py, int walkThrough)
        {
            var i = Random.Shared.Next(0, 4);
            foreach (var dir in GetDirectionOrder(i))
            {
                if (ShouldMoveTowards(ex, ey, px, py, dir) && CanMove(playerId, map, ex, ey, eventId, walkThrough, (byte)dir, false))
                {
                    return dir;
                }
            }

            return RandomDirection();
        }

        private static IEnumerable<int> GetDirectionOrder(int start) =>
            Enumerable.Range(0, 4).Select(i => (start + i) % 4);

        private static bool ShouldMoveTowards(int ex, int ey, int px, int py, int dir) =>
            dir switch
            {
                (int) Direction.Up => ey > py,
                (int) Direction.Down => ey < py,
                (int) Direction.Left => ex > px,
                (int) Direction.Right => ex < px,
                _ => false
            };

        private static int BfsMoveTowardsPlayer(int playerId, int map, int eventId, int ex, int ey, int px, int py, int walkThrough)
        {
            // Existing BFS implementation (simplified here for brevity)
            var queue = new Queue<(int x, int y)>();
            var visited = new HashSet<(int, int)>();
            var parent = new Dictionary<(int, int), (int, int)>();
            queue.Enqueue((ex, ey));
            visited.Add((ex, ey));

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                if (x == px && y == py)
                {
                    var current = (x, y);
                    while (parent[current] != (ex, ey))
                        current = parent[current];
                    return GetDirectionFromStep(ex, ey, current.Item1, current.Item2);
                }

                foreach (var (dx, dy, dir) in new[] {(0, -1, (int) Direction.Up), (0, 1, (int) Direction.Down), (-1, 0, (int) Direction.Left), (1, 0, (int) Direction.Right)})
                {
                    int nx = x + dx, ny = y + dy;
                    if (IsValidMove(playerId, map, eventId, nx, ny, walkThrough, visited))
                    {
                        queue.Enqueue((nx, ny));
                        visited.Add((nx, ny));
                        parent[(nx, ny)] = (x, y);
                    }
                }
            }

            return 4; // No path found
        }

        private static bool IsValidMove(int playerId, int map, int eventId, int x, int y, int walkThrough, HashSet<(int, int)> visited) =>
            x >= 0 && x <= Server.Map.Instance[map].MaxX && y >= 0 && y <= Server.Map.Instance[map].MaxY &&
            !visited.Contains((x, y)) && CanMove(playerId, map, x, y, eventId, walkThrough, 0, false);

        private static int GetDirectionFromStep(int ex, int ey, int nx, int ny) =>
            nx > ex ? (int) Direction.Right : nx < ex ? (int) Direction.Left : ny > ey ? (int) Direction.Down : (int) Direction.Up;

        // New A* Pathfinding
        private static int AStarMoveTowardsPlayer(int playerId, int map, int eventId, int ex, int ey, int px, int py, int walkThrough)
        {
            var openSet = new PriorityQueue<(int x, int y, int fScore)>(Comparer<(int x, int y, int fScore)>.Create((a, b) => a.fScore.CompareTo(b.fScore)));
            var cameFrom = new Dictionary<(int, int), (int, int)>();
            var gScore = new Dictionary<(int, int), int> {[(ex, ey)] = 0};
            var fScore = new Dictionary<(int, int), int> {[(ex, ey)] = Heuristic(ex, ey, px, py)};
            openSet.Enqueue((ex, ey, fScore[(ex, ey)]));

            while (openSet.Count > 0)
            {
                var (x, y, _) = openSet.Dequeue();
                if (x == px && y == py)
                {
                    var current = (x, y);
                    while (cameFrom[current] != (ex, ey))
                        current = cameFrom[current];
                    return GetDirectionFromStep(ex, ey, current.Item1, current.Item2);
                }

                foreach (var (dx, dy, dir) in new[] {(0, -1, (int) Direction.Up), (0, 1, (int) Direction.Down), (-1, 0, (int) Direction.Left), (1, 0, (int) Direction.Right)})
                {
                    int nx = x + dx, ny = y + dy;
                    if (!IsWithinMapBounds(map, nx, ny) || !CanMove(playerId, map, x, y, eventId, walkThrough, (byte) dir, false)) continue;

                    var tentativeGScore = gScore[(x, y)] + 1;
                    if (!gScore.ContainsKey((nx, ny)) || tentativeGScore < gScore[(nx, ny)])
                    {
                        cameFrom[(nx, ny)] = (x, y);
                        gScore[(nx, ny)] = tentativeGScore;
                        fScore[(nx, ny)] = gScore[(nx, ny)] + Heuristic(nx, ny, px, py);
                        openSet.Enqueue((nx, ny, fScore[(nx, ny)]));
                    }
                }
            }

            return 4; // No path found
        }

        private static bool IsWithinMapBounds(int map, int x, int y) =>
            x >= 0 && x <= Server.Map.Instance[map].MaxX && y >= 0 && y <= Server.Map.Instance[map].MaxY;

        private static int Heuristic(int x1, int y1, int x2, int y2) => Math.Abs(x1 - x2) + Math.Abs(y1 - y2);

    // Returns a random cardinal direction (0-3). NextInt upper bound is inclusive, so use 3.
    private static int RandomDirection() => General.GetRandom.NextInt(0, 3);

        public static int CanMoveAwayFromPlayer(int playerId, int map, int eventId)
        {
            if (!IsValidPlayerEvent(playerId, map, eventId)) return 5;

            var (px, py, ex, ey, walkThrough) = GetPlayerAndEventPositions(playerId, map, eventId);
            // Seed selection for direction ordering (0-3 only).
            var i = General.GetRandom.NextInt(0, 3);
            foreach (var dir in GetDirectionOrder(i))
            {
                if (ShouldMoveAway(ex, ey, px, py, dir) && CanMove(playerId, map, ex, ey, eventId, walkThrough, (byte) dir, false))
                    return dir;
            }

            return RandomDirection();
        }

        private static bool ShouldMoveAway(int ex, int ey, int px, int py, int dir) =>
            dir switch
            {
                (int) Direction.Up => ey < py,
                (int) Direction.Down => ey > py,
                (int) Direction.Left => ex < px,
                (int) Direction.Right => ex > px,
                _ => false
            };

        public static int GetDirToPlayer(int playerId, int map, int eventId)
        {
            if (!IsValidPlayerEvent(playerId, map, eventId)) return (int) Direction.Right;
            var (px, py, ex, ey, _) = GetPlayerAndEventPositions(playerId, map, eventId);
            return GetNpcDir(ex, ey, px, py);
        }

        public static int GetDirAwayFromPlayer(int playerId, int map, int eventId)
        {
            if (!IsValidPlayerEvent(playerId, map, eventId)) return (int) Direction.Right;
            var (px, py, ex, ey, _) = GetPlayerAndEventPositions(playerId, map, eventId);
            byte direction = (byte)Direction.Right;
            var maxDistance = 0;
            UpdateDirectionAndDistance(px - ex, (int) Direction.Left, (int) Direction.Right, ref direction, ref maxDistance);
            UpdateDirectionAndDistance(py - ey, (int) Direction.Up, (int) Direction.Down, ref direction, ref maxDistance);
            return direction;
        }

        // New Movement Behaviors
        public static void PatrolEvent(int index, int map, int eventId, List<(int x, int y)> patrolPath, int speed, bool globalEvent = false)
        {
            if (!patrolPath.Any()) return;
            var currentStep = TempEventMap[map].Event[eventId].PatrolStep % patrolPath.Count;
            var (targetX, targetY) = patrolPath[currentStep];
            var dir = GetDirectionToTarget(TempEventMap[map].Event[eventId].X, TempEventMap[map].Event[eventId].Y, targetX, targetY);
            if (CanMove(index, map, TempEventMap[map].Event[eventId].X, TempEventMap[map].Event[eventId].Y, eventId, 0, (byte) dir, globalEvent))
            {
                OnMove(index, map, eventId, (byte)dir, speed, globalEvent);
                if (TempEventMap[map].Event[eventId].X == targetX && TempEventMap[map].Event[eventId].Y == targetY)
                    TempEventMap[map].Event[eventId].PatrolStep++;
            }
        }

        private static int GetDirectionToTarget(int x, int y, int tx, int ty) =>
            tx > x ? (int) Direction.Right : tx < x ? (int) Direction.Left : ty > y ? (int) Direction.Down : (int) Direction.Up;

        public static void FollowPlayer(int index, int map, int eventId, int targetPlayerId, int speed, bool globalEvent = false)
        {
            var dir = CanMoveTowardsPlayer(targetPlayerId, map, eventId);
            if (dir != 4)
                OnMove(index, map, eventId, (byte)dir, speed, globalEvent);
        }

        #endregion
   
        public static void ProcessEventReply(int index, int eventId, int pageId, int reply)
        {
            for (var i = 0; i <= Data.TempPlayer[index].EventProcessingCount; i++)
            {
                ref var proc = ref Data.TempPlayer[index].EventProcessing[i];
                if (proc.EventId != eventId || proc.PageId != pageId || proc.WaitingForResponse != 1) continue;

                // Treat a negative reply as an explicit cancel/abort.
                if (reply < 0)
                {
                    AbortEventProcessing(index, i);
                    break;
                }

                // Resolve the command we were waiting on. CurSlot is advanced after executing the command,
                // so the "prompt" command is typically at CurSlot-1. Clamp defensively.
                var map = GetPlayerMap(index);
                var commandList = Server.Map.Instance[map].Event[eventId].Pages[pageId].CommandList;
                if (proc.CurList < 0 || proc.CurList >= commandList.Length)
                {
                    proc.WaitingForResponse = 0;
                    break;
                }

                var commands = commandList[proc.CurList].Commands;
                if (commands == null || commands.Length == 0)
                {
                    proc.WaitingForResponse = 0;
                    break;
                }

                var promptSlot = Math.Clamp(proc.CurSlot - 1, 0, commands.Length - 1);
                var cmd = commands[promptSlot];

                if (cmd.Index == (byte)EventCommand.ShowText)
                {
                    // Any reply means "continue".
                    proc.WaitingForResponse = 0;
                    proc.ActionTimer = General.GetTime();
                    break;
                }

                if (cmd.Index == (byte)EventCommand.ShowChoices)
                {
                    if (reply is >= 1 and <= 4)
                    {
                        UpdateEventProcessing(index, i, reply, cmd);
                        proc.ActionTimer = General.GetTime();
                    }
                    else
                    {
                        proc.WaitingForResponse = 0;
                    }

                    if (proc.CurList == 0)
                    {
                        // Invalid reply; abort processing.
                        AbortEventProcessing(index, i);
                    }

                    break;
                }

                // Unknown prompt type; unblock to avoid soft-lock.
                proc.WaitingForResponse = 0;
                break;
            }
        }

        private static void AbortEventProcessing(int player, int procIndex)
        {
            if (player < 0 || player >= Core.Globals.Variables.MaxPlayers)
            {
                return;
            }

            // Best-effort: release any client-side hold so movement can't stay stuck.
            try
            {
                var buffer = new PacketWriter(8);
                buffer.WriteEnum(ServerPackets.SHoldPlayer);
                buffer.WriteInt32(1); // Release
                PlayerService.Instance.SendDataTo(player, buffer.GetBytes());
            }
            catch
            {
                // Ignore send failures; we still clear server-side processing below.
            }

            ref var proc = ref Data.TempPlayer[player].EventProcessing[procIndex];
            proc.WaitingForResponse = 0;
            proc.Active = 0;
            proc.EventId = -1;
            proc.PageId = -1;
            proc.CurList = 0;
            proc.CurSlot = 0;
            proc.ActionTimer = 0;
        }

        private static void UpdateEventProcessing(int index, int procIndex, int reply, Type.EventCommand cmd)
        {
            ref var proc = ref Data.TempPlayer[index].EventProcessing[procIndex];
            proc.ListLeftOff[proc.CurList] = proc.CurSlot - 1;
            proc.CurList = reply switch
            {
                1 => cmd.Data1,
                2 => cmd.Data2,
                3 => cmd.Data3,
                4 => cmd.Data4,
                _ => proc.CurList
            };
            proc.CurSlot = 0;
            proc.WaitingForResponse = 0;
        }
        
        public static void SerializeMapEvents(PacketWriter buffer, int map)
        {
            for (var i = 0; i < Server.Map.Instance[map].EventCount; i++)
            {
                var ev = Server.Map.Instance[map].Event[i];

                buffer.WriteString(ev.Name);
                buffer.WriteByte(ev.Globals);
                buffer.WriteInt32(ev.X);
                buffer.WriteInt32(ev.Y);
                buffer.WriteInt32(ev.PageCount);

                if (ev.PageCount > 0)
                    SerializeEventPages(buffer, map, i, ev.PageCount);
            }
        }

        private static void SerializeEventPages(PacketWriter buffer, int map, int eventIndex, int pageCount)
        {
            for (var x = 0; x < pageCount; x++)
            {
                var page = Server.Map.Instance[map].Event[eventIndex].Pages[x];
                SerializePageConditions(buffer, page);
                SerializePageGraphics(buffer, page);
                SerializePageMovement(buffer, page);
                SerializePageCommands(buffer, map, eventIndex, x, page);
            }
        }

        private static void SerializePageConditions(PacketWriter buffer, EventPage page)
        {
            buffer.WriteInt32(page.ChkVariable);
            buffer.WriteInt32(page.VariableIndex);
            buffer.WriteInt32(page.VariableCondition);
            buffer.WriteInt32(page.VariableCompare);
            buffer.WriteInt32(page.ChkSwitch);
            buffer.WriteInt32(page.SwitchIndex);
            buffer.WriteInt32(page.SwitchCompare);
            buffer.WriteInt32(page.ChkHasItem);
            buffer.WriteInt32(page.HasItemIndex);
            buffer.WriteInt32(page.HasItemAmount);
            buffer.WriteInt32(page.ChkSelfSwitch);
            buffer.WriteInt32(page.SelfSwitchIndex);
            buffer.WriteInt32(page.SelfSwitchCompare);
        }

        private static void SerializePageGraphics(PacketWriter packetWriter, EventPage page)
        {
            packetWriter.WriteByte(page.GraphicType);
            packetWriter.WriteInt32(page.Graphic);
            packetWriter.WriteInt32(page.GraphicX);
            packetWriter.WriteInt32(page.GraphicY);
            packetWriter.WriteInt32(page.GraphicX2);
            packetWriter.WriteInt32(page.GraphicY2);
        }

        private static void SerializePageMovement(PacketWriter packetWriter, EventPage page)
        {
            packetWriter.WriteByte(page.MoveType);
            packetWriter.WriteByte(page.MoveSpeed);
            packetWriter.WriteByte(page.MoveFreq);
            packetWriter.WriteInt32(page.MoveRouteCount);
            packetWriter.WriteInt32(page.IgnoreMoveRoute);
            packetWriter.WriteInt32(page.RepeatMoveRoute);

            if (page.MoveRouteCount > 0)
            {
                for (var y = 0; y < page.MoveRouteCount; y++)
                {
                    ref var route = ref page.MoveRoute[y];

                    packetWriter.WriteInt32(route.Index);
                    packetWriter.WriteInt32(route.Data1);
                    packetWriter.WriteInt32(route.Data2);
                    packetWriter.WriteInt32(route.Data3);
                    packetWriter.WriteInt32(route.Data4);
                    packetWriter.WriteInt32(route.Data5);
                    packetWriter.WriteInt32(route.Data6);
                }
            }

            packetWriter.WriteByte(page.IdleAnim);
            packetWriter.WriteByte(page.DirFix);
            packetWriter.WriteInt32(page.WalkThrough);
            packetWriter.WriteInt32(page.ShowName);
            packetWriter.WriteByte(page.Trigger);
            packetWriter.WriteInt32(page.CommandListCount);
            packetWriter.WriteByte(page.Position);
        }

        private static void SerializePageCommands(PacketWriter buffer, int map, int eventIndex, int pageIndex, EventPage page)
        {
            if (page.CommandListCount <= 0) return;
            for (var y = 0; y < page.CommandListCount; y++)
            {
                var cmdList = Server.Map.Instance[map].Event[eventIndex].Pages[pageIndex].CommandList[y];
                buffer.WriteInt32(cmdList.CommandCount);
                buffer.WriteInt32(cmdList.ParentList);
                if (cmdList.CommandCount > 0)
                {
                    for (var z = 0; z < cmdList.CommandCount; z++)
                    {
                        var cmd = cmdList.Commands[z];

                        SerializeCommand(buffer, cmd);
                    }
                }
            }
        }

        private static void SerializeCommand(PacketWriter buffer, Type.EventCommand cmd)
        {
            buffer.WriteInt32(cmd.Index);
            buffer.WriteString(cmd.Text1);
            buffer.WriteString(cmd.Text2);
            buffer.WriteString(cmd.Text3);
            buffer.WriteString(cmd.Text4);
            buffer.WriteString(cmd.Text5);
            buffer.WriteInt32(cmd.Data1);
            buffer.WriteInt32(cmd.Data2);
            buffer.WriteInt32(cmd.Data3);
            buffer.WriteInt32(cmd.Data4);
            buffer.WriteInt32(cmd.Data5);
            buffer.WriteInt32(cmd.Data6);
            buffer.WriteInt32(cmd.ConditionalBranch.CommandList);
            buffer.WriteInt32(cmd.ConditionalBranch.Condition);
            buffer.WriteInt32(cmd.ConditionalBranch.Data1);
            buffer.WriteInt32(cmd.ConditionalBranch.Data2);
            buffer.WriteInt32(cmd.ConditionalBranch.Data3);
            buffer.WriteInt32(cmd.ConditionalBranch.ElseCommandList);
            buffer.WriteInt32(cmd.MoveRouteCount);
            if (cmd.MoveRouteCount > 0)
            {
                for (var w = 0; w < cmd.MoveRouteCount; w++)
                {
                    var route = cmd.MoveRoute[w];
                    buffer.WriteInt32(route.Index);
                    buffer.WriteInt32(route.Data1);
                    buffer.WriteInt32(route.Data2);
                    buffer.WriteInt32(route.Data3);
                    buffer.WriteInt32(route.Data4);
                    buffer.WriteInt32(route.Data5);
                    buffer.WriteInt32(route.Data6);
                }
            }
        }

        #region New Features

        // Scheduled Events
        public struct ScheduledEvent
        {
            public int EventId;
            public DateTime TriggerTime;
            public int map;
        }

        public static void ScheduleEvent(int eventId, DateTime triggerTime, int map)
        {
            ScheduledEvents.Add(new ScheduledEvent {EventId = eventId, TriggerTime = triggerTime, map = map});
            General.Logger.LogInformation($"Scheduled event {eventId} on map {map} for {triggerTime}");
        }

        public static void CheckScheduledEvents()
        {
            var now = DateTime.Now;
            foreach (var ev in ScheduledEvents.ToList())
            {
                if (now >= ev.TriggerTime)
                {
                    TriggerScheduledEvent(ev);
                    ScheduledEvents.TryTake(out _);
                }
            }
        }

        private static void TriggerScheduledEvent(ScheduledEvent ev)
        {
            foreach (var i in PlayerService.Instance.PlayerIds)
            {
                if (NetworkConfig.IsPlaying(i) && GetPlayerMap(i) == ev.map)
                    EventLogic.TriggerEvent(i, ev.EventId, 0, TempEventMap[ev.map].Event[ev.EventId].X, TempEventMap[ev.map].Event[ev.EventId].Y);
            }

            General.Logger.LogInformation($"Triggered scheduled event {ev.EventId} on map {ev.map}");
        }

        // Action-Based Triggers
        public static void TriggerOnPlayerAction(int index, string actionType, int value)
        {
            var map = GetPlayerMap(index);
            for (var i = 0; i < Server.Map.Instance[map].EventCount; i++)
            {
                var page = Server.Map.Instance[map].Event[i].Pages[Data.TempPlayer[index].EventMap.EventPages[i].PageId];
                if (page.ChkVariable == 1 && page.VariableIndex == GetActionVariableIndex(actionType) && page.VariableCompare == value)
                    EventLogic.TriggerEvent(index, i, 0, GetPlayerX(index), GetPlayerY(index));
            }
        }

        private static int GetActionVariableIndex(string actionType) =>
            actionType switch
            {
                "Kills" => 1,
                "ItemsCollected" => 2,
                _ => 0
            };

        // Environment Effects
        public static void ChangeMapWeather(int map, int weatherType, int intensity)
        {
            foreach (var i in PlayerService.Instance.PlayerIds)
            {
                if (NetworkConfig.IsPlaying(i) && GetPlayerMap(i) == map)
                    NetworkSend.SpecialEffect(i, EffectTypeWeather, weatherType, intensity);
            }
        }

        #endregion

        #region Helper Classes

        // Simple Priority Queue for A* Pathfinding
        private class PriorityQueue<T>
        {
            private readonly List<T> _items = new List<T>();
            private readonly IComparer<T> _comparer;

            public PriorityQueue(IComparer<T> comparer) => this._comparer = comparer;
            public int Count => _items.Count;

            public void Enqueue(T item)
            {
                _items.Add(item);
                _items.Sort(_comparer);
            }

            public T Dequeue()
            {
                if (_items.Count == 0) throw new InvalidOperationException("Queue is empty");
                var item = _items[0];
                _items.RemoveAt(0);
                return item;
            }
        }

        #endregion
    }
}