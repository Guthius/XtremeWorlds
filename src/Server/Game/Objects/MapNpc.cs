using Core;
using Core.Globals;
using Core.Net;
using System;
using System.Collections.Generic;
using System.Text;
using static Core.Net.Packets;
using static Core.Globals.Commands;
using Server.Game;
using MapNpcData = Core.Globals.Type.MapNpc;
using Microsoft.Extensions.Logging;

namespace Server
{
    public class MapNpc
    {
        public static MapNpcData[,] Instance { get; } = new MapNpcData[Core.Globals.Variables.MaxMaps, Core.Globals.Variables.MaxMapNpcs];

        // Tracks remaining pixels to finish the current tile step for each npc on each map.
        private static readonly int[,] _stepRemaining = new int[Core.Globals.Variables.MaxMaps, Core.Globals.Variables.MaxMapNpcs];

        // Planned multi-tile movement route (as directions) per npc on each map.
        private static readonly System.Collections.Generic.Queue<byte>?[,] _route = new System.Collections.Generic.Queue<byte>?[Core.Globals.Variables.MaxMaps, Core.Globals.Variables.MaxMapNpcs];

        public static void OnClear(int index, int map)
        {
            var count = Enum.GetValues(typeof(Vital)).Length;
            Instance[map, index].Vital = new int[count];
            Instance[map, index].SkillCd = new int[Core.Globals.Variables.MaxNpcSkills];
            Instance[map, index].Num = -1;
            Instance[map, index].SkillBuffer = -1;
            Instance[map, index].DeathTimer = 0;
        }

        public static async System.Threading.Tasks.Task OnSpawnAll()
        {
            await Task.WhenAll(Enumerable
                .Range(0, Core.Globals.Variables.MaxMapNpcs)
                .Select(OnSpawn));
        }

        public static async System.Threading.Tasks.Task OnSpawn(int map)
        {
            await Task.WhenAll(Enumerable
                .Range(0, Core.Globals.Variables.MaxMapNpcs)
                .Select(npc => Task.Run(() =>
                    OnSpawn(npc, map))));
        }

        public static void OnSpawn(int npc, int map)
        {
            var spawned = false;

            // Validate map
            if (map < 0 || map >= Core.Globals.Variables.MaxMaps)
            {
                return;
            }

            if (Server.Map.Instance[map].NoRespawn)
            {
                return;
            }

            var npcNum = Server.Map.Instance[map].Npc[npc];
            
            // Validate slot and npc index; allow slot 0
            if (npc < 0 || npc >= Core.Globals.Variables.MaxMapNpcs || npcNum < 0 || npcNum >= Core.Globals.Variables.MaxNpcs)
            {
                return;
            }

            if (Npc.Instance[npcNum].SpawnTime != (byte) Clock.Instance.TimeOfDay && Npc.Instance[npcNum].SpawnTime != 0)
            {
                MapNpc.OnClear(npc, map);

                Network.MapNpcsToMap(map);

                return;
            }

            Instance[map, npc].Num = npcNum;
            Instance[map, npc].Target = 0;
            Instance[map, npc].TargetType = 0;
            Instance[map, npc].DeathTimer = 0;

            var vitals = Enum.GetValues<Vital>();
            foreach (var vital in vitals)
            {
                Instance[map, npc].Vital[(int) vital] = GameLogic.GetNpcMaxVital(npcNum, vital);
            }
            
            Instance[map, npc].Dir = (byte) (Random.Shared.NextDouble() * 4f);

            for (var x = 0; x < Server.Map.Instance[map].MaxX; x++)
            {
                for (var y = 0; y < Server.Map.Instance[map].MaxY; y++)
                {
                    var tile = Server.Map.Instance[map].Tile[x, y];
                    bool isPrimaryMatch = tile.Type == TileType.NpcSpawn && tile.Data1 == npc;
                    bool isSecondaryMatch = tile.Type2 == TileType.NpcSpawn && tile.Data1_2 == npc;
                    if (!isPrimaryMatch && !isSecondaryMatch)
                        continue;

                    Instance[map, npc].X = x * Variables.TileSize;
                    Instance[map, npc].Y = y * Variables.TileSize;
                    Instance[map, npc].Dir = (byte)(isPrimaryMatch ? tile.Data2 : tile.Data2_2);

                    spawned = true;
                    break;
                }
                if (spawned) break;
            }

            if (!spawned)
            {
                var i = 0;
                while (i < 1000)
                {
                    var x = (int) Math.Round(General.GetRandom.NextDouble(0d, Server.Map.Instance[map].MaxX - 1));
                    var y = (int) Math.Round(General.GetRandom.NextDouble(0d, Server.Map.Instance[map].MaxY - 1));

                    if (x > Server.Map.Instance[map].MaxX) x = Server.Map.Instance[map].MaxX - 1;
                    if (y > Server.Map.Instance[map].MaxY) y = Server.Map.Instance[map].MaxY - 1;

                    if (TileIsOpen(map, x, y))
                    {
                        Instance[map, npc].X = x * Variables.TileSize;
                        Instance[map, npc].Y = y * Variables.TileSize;

                        spawned = true;
                        break;
                    }

                    ++i;
                }
            }

            // Didn't spawn, so now we'll just try to find a free tile
            if (!spawned)
            {
                for (var x = 0; x < Server.Map.Instance[map].MaxX; x++)
                {
                    for (var y = 0; y < Server.Map.Instance[map].MaxY; y++)
                    {
                        if (TileIsOpen(map, x, y))
                        {
                            continue;
                        }

                        Instance[map, npc].X = x * Variables.TileSize;
                        Instance[map, npc].Y = y * Variables.TileSize;

                        spawned = true;
                    }
                }
            }

            // If we suceeded in spawning then send it to everyone
            if (spawned)
            {
                try
                {
                    Script.Instance?.OnSpawnNpc();
                }
                catch (Exception ex)
                {
                    General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnSpawn));
                }

                Network.NpcSpawn(map, npc);
            }

            Network.MapNpcVitals(map, npc);
        }

        public static bool TileIsOpen(int map, int x, int y)
        {
            foreach (var playerId in PlayerService.Instance.PlayerIds)
            {
                if (GetMap(playerId) == map &&
                    GetX(playerId) == x &&
                    GetY(playerId) == y)
                {
                    return false;
                }
            }

            for (var i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
            {
                if (Instance[map, i].Num >= 0 &&
                    Instance[map, i].X == x &&
                    Instance[map, i].Y == y)
                {
                    return false;
                }
            }

            if (Server.Map.Instance[map].Tile[x, y].Type != TileType.NpcSpawn &&
                Server.Map.Instance[map].Tile[x, y].Type != TileType.Item &&
                Server.Map.Instance[map].Tile[x, y].Type != TileType.None &&
                Server.Map.Instance[map].Tile[x, y].Type2 != TileType.NpcSpawn &&
                Server.Map.Instance[map].Tile[x, y].Type2 != TileType.Item &&
                Server.Map.Instance[map].Tile[x, y].Type2 != TileType.None)
            {
                return false;
            }

            return true;
        }

        public static bool CanMove(int map, int npc, byte dir)
        {
            int count = System.Enum.GetValues(typeof(Direction)).Length;
            if (map < 0 || map >= Server.Map.Instance.Count || npc < 0 || npc >= Core.Globals.Variables.MaxMapNpcs || dir > count)
            {
                return false;
            }

            static bool IsEventBlockingTile(int mapId, int tileX, int tileY)
            {
                // Global events (authoritative server-side position)
                if (Event.TempEventMap != null && mapId >= 0 && mapId < Event.TempEventMap.Length)
                {
                    var globalEvents = Event.TempEventMap[mapId];
                    if (globalEvents.Event != null && globalEvents.EventCount > 0)
                    {
                        for (var i = 0; i < globalEvents.EventCount && i < globalEvents.Event.Length; i++)
                        {
                            var ge = globalEvents.Event[i];
                            if (ge.WalkThrough != 0)
                                continue;

                            // Global events use pixel coordinates; movement/collision is tile-based.
                            var gx = (int)Math.Floor((double)ge.X / Constants.TileSize);
                            var gy = (int)Math.Floor((double)ge.Y / Constants.TileSize);
                            if (gx == tileX && gy == tileY)
                                return true;
                        }
                    }
                }

                // Non-global map events: fall back to the map-defined position and first page WalkThrough.
                if (Server.Map.Instance[mapId].EventCount > 0 && Server.Map.Instance[mapId].Event != null)
                {
                    for (var i = 0; i < Server.Map.Instance[mapId].EventCount && i < Server.Map.Instance[mapId].Event.Length; i++)
                    {
                        var ev = Server.Map.Instance[mapId].Event[i];
                        if (ev.Globals == 1)
                            continue;
                        if (ev.PageCount <= 0 || ev.Pages == null)
                            continue;

                        if (ev.Pages[0].WalkThrough != 0)
                            continue;

                        // Event.X/Y are tile coordinates.
                        if (ev.X == tileX && ev.Y == tileY)
                            return true;
                    }
                }

                return false;
            }

            var x = Instance[map, npc].X;
            var y = Instance[map, npc].Y;
            
            // If already in mid-move, don't allow a new tile move.
            if (Instance[map, npc].Moving == (byte)MovementState.Walking && _stepRemaining[map, npc] > 0)
                return false;

            int tileX = x / Constants.TileSize;
            int tileY = y / Constants.TileSize;
            int nextTileX = tileX;
            int nextTileY = tileY;
            switch (dir)
            {
                case (byte)Direction.Up: nextTileY -= 1; break;
                case (byte)Direction.Down: nextTileY += 1; break;
                case (byte)Direction.Left: nextTileX -= 1; break;
                case (byte)Direction.Right: nextTileX += 1; break;
            }

            // Check map bounds
            if (nextTileX < 0 || nextTileY < 0 || nextTileX >= Server.Map.Instance[map].MaxX || nextTileY >= Server.Map.Instance[map].MaxY)
                return false;

            // Block by events with WalkThrough disabled
            if (IsEventBlockingTile(map, nextTileX, nextTileY))
                return false;

            // Check tile walkability
            int n = (int)Server.Map.Instance[map].Tile[nextTileX, nextTileY].Type;
            int n2 = (int)Server.Map.Instance[map].Tile[nextTileX, nextTileY].Type2;
            if (n != (byte)TileType.None &&
                n != (byte)TileType.Item &&
                n != (byte)TileType.NpcSpawn &&
                n2 != (byte)TileType.None &&
                n2 != (byte)TileType.Item &&
                n2 != (byte)TileType.NpcSpawn)
            {
                return false;
            }

            // Check for player collision (using tile grid)
            foreach (var playerId in PlayerService.Instance.PlayerIds)
            {
                if (GetMap(playerId) == map &&
                    GetX(playerId) == nextTileX &&
                    GetY(playerId) == nextTileY)
                {
                    return false;
                }
            }

            // Check for other NPC collision (using tile grid)
            for (var i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
            {
                if (i == npc) continue;
                if (Instance[map, i].Num < 0) continue;
                int npcTileX = (int)Math.Floor((double)Instance[map, i].X / Constants.TileSize);
                int npcTileY = (int)Math.Floor((double)Instance[map, i].Y / Constants.TileSize);
                if (npcTileX == nextTileX && npcTileY == nextTileY)
                {
                    return false;
                }
            }

            // Prevent movement if skill buffer is active
            if (Instance[map, npc].SkillBuffer >= 0)
            {
                return false;
            }

            return true;
        }

        public static void OnMove(int map, int npc, byte dir, int movement)
        {
            var count = System.Enum.GetValues(typeof(MovementState)).Length;
            int count2 = System.Enum.GetValues(typeof(Direction)).Length;
            if (map < 0 || map >= Core.Globals.Variables.MaxMaps || npc < 0 || npc >= Core.Globals.Variables.MaxMapNpcs || dir > count2 || movement < 0 || movement > count)
            {
                return;
            }
            // If already walking mid-step, ignore duplicate start.
            if (Instance[map, npc].Moving == (byte)MovementState.Walking && _stepRemaining[map, npc] > 0)
                return;

            // Begin a new tile movement: set dir, movement state, step counter (32px)
            Instance[map, npc].Dir = dir;
            Instance[map, npc].Moving = (byte)MovementState.Walking;
            _stepRemaining[map, npc] = 32; // pixels to travel

            // Send start-of-move packet (position unchanged); client will animate pixel stepping locally.
            var buffer = new PacketWriter(4);
            buffer.WriteEnum(ServerPackets.SNpcMove);
            buffer.WriteInt32(npc);
            buffer.WriteInt32(Instance[map, npc].X);
            buffer.WriteInt32(Instance[map, npc].Y);
            buffer.WriteByte(Instance[map, npc].Dir);
            buffer.WriteByte((byte)MovementState.Walking);
            NetworkConfig.SendDataToMap(map, buffer.GetBytes());
        }

        /// <summary>
        /// Advances active NPC pixel movement. Called frequently (e.g., every walk tick) from the main loop.
        /// Sends an SNpcDir packet when a tile step is completed to stop client movement exactly on tile.
        /// </summary>
        public static void OnMove()
        {
            const int TileSize = 32;
            for (int map = 0; map < Core.Globals.Variables.MaxMaps; map++)
            {
                for (int i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
                {
                    ref var npc = ref Instance[map, i];
                    if (npc.Num < 0) continue;
                    if (npc.Moving != (byte)MovementState.Walking) continue;
                    if (_stepRemaining[map, i] <= 0) continue;

                    // Move one pixel
                    switch ((Direction)npc.Dir)
                    {
                        case Direction.Up: npc.Y -= 1; break;
                        case Direction.Down: npc.Y += 1; break;
                        case Direction.Left: npc.X -= 1; break;
                        case Direction.Right: npc.X += 1; break;
                    }
                    _stepRemaining[map, i]--;

                    if (_stepRemaining[map, i] <= 0)
                    {
                        // Clamp to tile grid alignment just in case
                        npc.X = Math.Max(0, Math.Min(npc.X, (Server.Map.Instance[map].MaxX - 1) * TileSize));
                        npc.Y = Math.Max(0, Math.Min(npc.Y, (Server.Map.Instance[map].MaxY - 1) * TileSize));
                        npc.Moving = 0;
                        _stepRemaining[map, i] = 0;

                        // If there is a planned route, immediately continue with the next step.
                        if (TryDequeueNextStep(map, i))
                        {
                            // Movement continued; don't send stop packet.
                            continue;
                        }

                        // Route finished; send stop so clients end the walk animation exactly on tile.
                        var stopPacket = new PacketWriter(9);
                        stopPacket.WriteEnum(ServerPackets.SNpcDir);
                        stopPacket.WriteInt32(i);
                        stopPacket.WriteByte(npc.Dir);
                        NetworkConfig.SendDataToMap(map, stopPacket.GetBytes());
                    }
                }
            }
        }

        public static void NpcDir(int map, int npc, byte dir)
        {
            int count = System.Enum.GetValues(typeof(Direction)).Length;
            if (map < 0 || map >= Core.Globals.Variables.MaxMaps || npc < 0 || npc >= Core.Globals.Variables.MaxMapNpcs || dir > count)
            {
                return;
            }

            Instance[map, npc].Dir = dir;

            var packet = new PacketWriter(9);

            packet.WriteEnum(ServerPackets.SNpcDir);
            packet.WriteInt32(npc);
            packet.WriteByte(dir);

            NetworkConfig.SendDataToMap(map, packet.GetBytes());
        }

        /// <summary>
        /// Replace the NPC's route with the provided sequence of directions.
        /// </summary>
        public static void SetRoute(int map, int npc, System.Collections.Generic.IEnumerable<byte> directions)
        {
            if (map < 0 || map >= Core.Globals.Variables.MaxMaps || npc < 0 || npc >= Core.Globals.Variables.MaxMapNpcs) return;
            var q = new System.Collections.Generic.Queue<byte>();
            foreach (var d in directions) q.Enqueue(d);
            _route[map, npc] = q;
        }

        /// <summary>
        /// Clears any planned route for the NPC.
        /// </summary>
        public static void ClearRoute(int map, int npc)
        {
            if (map < 0 || map >= Core.Globals.Variables.MaxMaps || npc < 0 || npc >= Core.Globals.Variables.MaxMapNpcs) return;
            _route[map, npc] = null;
        }

        /// <summary>
        /// If there is a pending route and the NPC is not currently mid-step, dequeue the next step and start moving.
        /// </summary>
        public static bool TryStartNextStepNow(int map, int npc)
        {
            if (map < 0 || map >= Core.Globals.Variables.MaxMaps || npc < 0 || npc >= Core.Globals.Variables.MaxMapNpcs) return false;
            ref var mapNpc = ref Instance[map, npc];
            if (mapNpc.Moving == (byte)MovementState.Walking && _stepRemaining[map, npc] > 0) return false;
            return TryDequeueNextStep(map, npc);
        }

        /// <summary>
        /// Pops the next planned direction and begins a new tile step if possible.
        /// </summary>
        private static bool TryDequeueNextStep(int map, int npc)
        {
            var route = _route[map, npc];
            if (route == null || route.Count == 0) return false;
            
            // Validate and attempt the next step
            var nextDir = route.Peek();
            if (CanMove(map, npc, nextDir))
            {
                route.Dequeue();
                OnMove(map, npc, nextDir, (int)MovementState.Walking);
                // If route is finished after this step, clear it now; ProcessActiveNpcMovement will send stop when the step ends.
                if (route.Count == 0)
                {
                    _route[map, npc] = null;
                }
                return true;
            }
            // Cannot move as planned; drop the route so callers can recompute.
            _route[map, npc] = null;
            return false;
        }
    }
}
