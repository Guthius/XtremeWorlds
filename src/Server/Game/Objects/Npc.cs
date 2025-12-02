using Core;
using Core.Globals;
using Core.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server.Game;
using Server.Game.Net;
using Server.Net;
using static Core.Globals.Command;
using static Core.Net.Packets;

namespace Server;

public static class Npc
{
    // Tracks remaining pixels to finish the current tile step for each npc on each map.
    private static readonly int[,] _stepRemaining = new int[Core.Globals.Variables.MaxMaps, Core.Globals.Variables.MaxMapNpcs];

    // Planned multi-tile movement route (as directions) per npc on each map.
    private static readonly System.Collections.Generic.Queue<byte>?[,] _route = new System.Collections.Generic.Queue<byte>?[Core.Globals.Variables.MaxMaps, Core.Globals.Variables.MaxMapNpcs];

    public static async System.Threading.Tasks.Task OnLoadAllAsync()
    {
        var tasks = Enumerable.Range(0, Core.Globals.Variables.MaxNpcs).Select(i => System.Threading.Tasks.Task.Run(() => OnLoadAsync(i)));
        await System.Threading.Tasks.Task.WhenAll(tasks);
    }

    public static void Save(int npcNum)
    {
        string json = JsonConvert.SerializeObject(Data.Npc[(int)npcNum]).ToString();

        if (Database.RowExists(npcNum, "npc"))
        {
            Database.UpdateRow(npcNum, json, "npc", "data");
        }
        else
        {
            Database.InsertRow(npcNum, json, "npc");
        }
    }

    public static async System.Threading.Tasks.Task OnLoadAsync(int npcNum)
    {
        JObject data;

        data = await Database.SelectRowAsync(npcNum, "npc", "data");

        if (data is null)
        {
            Npc.Clear(npcNum);
            return;
        }

        var npcData = JObject.FromObject(data).ToObject<Core.Globals.Type.Npc>();
        Data.Npc[(int)npcNum] = npcData;
    }

    public static void Clear(int index)
    {
        Data.Npc[index].Name = "";
        Data.Npc[index].AttackSay = "";
        int statCount = Enum.GetValues(typeof(Stat)).Length;
        Data.Npc[index].Stat = new byte[statCount];

        for (int i = 0, loopTo = Core.Globals.Variables.MaxDropItems; i < loopTo; i++)
        {
            Data.Npc[index].DropChance = new int[Core.Globals.Variables.MaxDropItems];
            Data.Npc[index].DropItem = new int[Core.Globals.Variables.MaxDropItems];
            Data.Npc[index].DropItemValue = new int[Core.Globals.Variables.MaxDropItems];
            Data.Npc[index].Skill = new byte[Core.Globals.Variables.MaxNpcSkills];
        }
    }


    public static async Task SpawnAllMapNpcs()
    {
        await Task.WhenAll(Enumerable
            .Range(0, Core.Globals.Variables.MaxMapNpcs)
            .Select(SpawnMapNpcs));
    }

    public static async Task SpawnMapNpcs(int mapNum)
    {
        await Task.WhenAll(Enumerable
            .Range(0, Core.Globals.Variables.MaxMapNpcs)
            .Select(mapNpcNum => Task.Run(() =>
                SpawnNpc(mapNpcNum, mapNum))));
    }

    public static void SpawnNpc(int mapNpcNum, int mapNum)
    {
        var spawned = false;

        // Validate map
        if (mapNum < 0 || mapNum >= Core.Globals.Variables.MaxMaps)
        {
            return;
        }

        if (Data.Map[mapNum].NoRespawn)
        {
            return;
        }

        var npcNum = Data.Map[mapNum].Npc[mapNpcNum];
        
        // Validate slot and npc index; allow slot 0
        if (mapNpcNum < 0 || mapNpcNum >= Core.Globals.Variables.MaxMapNpcs || npcNum < 0 || npcNum >= Core.Globals.Variables.MaxNpcs)
        {
            return;
        }

        if (Data.Npc[npcNum].SpawnTime != (byte) Clock.Instance.TimeOfDay && Data.Npc[npcNum].SpawnTime != 0)
        {
            MapNpc.Clear(mapNpcNum, mapNum);

            NetworkSend.SendMapNpcsToMap(mapNum);

            return;
        }

        Data.MapNpc[mapNum].Npc[mapNpcNum].Num = npcNum;
        Data.MapNpc[mapNum].Npc[mapNpcNum].Target = 0;
        Data.MapNpc[mapNum].Npc[mapNpcNum].TargetType = 0; // Clear

        var vitals = Enum.GetValues<Vital>();
        foreach (var vital in vitals)
        {
            Data.MapNpc[mapNum].Npc[mapNpcNum].Vital[(int) vital] = GameLogic.GetNpcMaxVital(npcNum, vital);
        }
        
        Data.MapNpc[mapNum].Npc[mapNpcNum].Dir = (byte) (Random.Shared.NextDouble() * 4f);

        for (var x = 0; x < Data.Map[mapNum].MaxX; x++)
        {
            for (var y = 0; y < Data.Map[mapNum].MaxY; y++)
            {
                var tile = Data.Map[mapNum].Tile[x, y];
                bool isPrimaryMatch = tile.Type == TileType.NpcSpawn && tile.Data1 == mapNpcNum;
                bool isSecondaryMatch = tile.Type2 == TileType.NpcSpawn && tile.Data1_2 == mapNpcNum;
                if (!isPrimaryMatch && !isSecondaryMatch)
                    continue;

                Data.MapNpc[mapNum].Npc[mapNpcNum].X = x * 32;
                Data.MapNpc[mapNum].Npc[mapNpcNum].Y = y * 32;
                Data.MapNpc[mapNum].Npc[mapNpcNum].Dir = (byte)(isPrimaryMatch ? tile.Data2 : tile.Data2_2);

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
                var x = (int) Math.Round(General.GetRandom.NextDouble(0d, Data.Map[mapNum].MaxX - 1));
                var y = (int) Math.Round(General.GetRandom.NextDouble(0d, Data.Map[mapNum].MaxY - 1));

                if (x > Data.Map[mapNum].MaxX) x = Data.Map[mapNum].MaxX - 1;
                if (y > Data.Map[mapNum].MaxY) y = Data.Map[mapNum].MaxY - 1;

                if (NpcTileIsOpen(mapNum, x, y))
                {
                    Data.MapNpc[mapNum].Npc[mapNpcNum].X = x * 32;
                    Data.MapNpc[mapNum].Npc[mapNpcNum].Y = y * 32;

                    spawned = true;
                    break;
                }

                ++i;
            }
        }

        // Didn't spawn, so now we'll just try to find a free tile
        if (!spawned)
        {
            for (var x = 0; x < Data.Map[mapNum].MaxX; x++)
            {
                for (var y = 0; y < Data.Map[mapNum].MaxY; y++)
                {
                    if (!NpcTileIsOpen(mapNum, x, y))
                    {
                        continue;
                    }

                    Data.MapNpc[mapNum].Npc[mapNpcNum].X = x * 32;
                    Data.MapNpc[mapNum].Npc[mapNpcNum].Y = y * 32;

                    spawned = true;
                }
            }
        }

        // If we suceeded in spawning then send it to everyone
        if (spawned)
        {
            var packet = new PacketWriter();

            packet.WriteInt32((int) ServerPackets.SSpawnNpc);
            packet.WriteInt32(mapNpcNum);
            packet.WriteInt32(Data.MapNpc[mapNum].Npc[mapNpcNum].Num);
            packet.WriteInt32(Data.MapNpc[mapNum].Npc[mapNpcNum].X);
            packet.WriteInt32(Data.MapNpc[mapNum].Npc[mapNpcNum].Y);
            packet.WriteByte(Data.MapNpc[mapNum].Npc[mapNpcNum].Dir);

            var vitalCount = Enum.GetValues<Vital>().Length;
            for (var i = 0; i < vitalCount; i++)
            {
                packet.WriteInt32(Data.MapNpc[mapNum].Npc[mapNpcNum].Vital[i]);
            }

            NetworkConfig.SendDataToMap(mapNum, packet.GetBytes());
        }

        NetworkSend.SendMapNpcVitals(mapNum, (byte) mapNpcNum);
    }

    public static bool NpcTileIsOpen(int mapNum, int x, int y)
    {
        foreach (var playerId in PlayerService.Instance.PlayerIds)
        {
            if (GetPlayerMap(playerId) == mapNum &&
                GetPlayerX(playerId) == x &&
                GetPlayerY(playerId) == y)
            {
                return false;
            }
        }

        for (var mapNpcNum = 0; mapNpcNum < Core.Globals.Variables.MaxMapNpcs; mapNpcNum++)
        {
            if (Data.MapNpc[mapNum].Npc[mapNpcNum].Num >= 0 &&
                Data.MapNpc[mapNum].Npc[mapNpcNum].X == x &&
                Data.MapNpc[mapNum].Npc[mapNpcNum].Y == y)
            {
                return false;
            }
        }

        if (Data.Map[mapNum].Tile[x, y].Type != TileType.NpcSpawn &&
            Data.Map[mapNum].Tile[x, y].Type != TileType.Item &&
            Data.Map[mapNum].Tile[x, y].Type != TileType.None &&
            Data.Map[mapNum].Tile[x, y].Type2 != TileType.NpcSpawn &&
            Data.Map[mapNum].Tile[x, y].Type2 != TileType.Item &&
            Data.Map[mapNum].Tile[x, y].Type2 != TileType.None)
        {
            return false;
        }

        return true;
    }

    public static bool CanNpcMove(int mapNum, int mapNpcNum, byte dir)
    {
        int count = System.Enum.GetValues(typeof(Direction)).Length;
        if (mapNum < 0 || mapNum >= Core.Globals.Variables.MaxMaps || mapNpcNum < 0 || mapNpcNum >= Core.Globals.Variables.MaxMapNpcs || dir > count)
        {
            return false;
        }

        var x = Data.MapNpc[mapNum].Npc[mapNpcNum].X;
        var y = Data.MapNpc[mapNum].Npc[mapNpcNum].Y;
        // If already in mid-move, don't allow a new tile move.
        if (Data.MapNpc[mapNum].Npc[mapNpcNum].Moving == (byte)MovementState.Walking && _stepRemaining[mapNum, mapNpcNum] > 0)
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
        if (nextTileX < 0 || nextTileY < 0 || nextTileX >= Data.Map[mapNum].MaxX || nextTileY >= Data.Map[mapNum].MaxY)
            return false;

        // Check tile walkability
        int n = (int)Data.Map[mapNum].Tile[nextTileX, nextTileY].Type;
        int n2 = (int)Data.Map[mapNum].Tile[nextTileX, nextTileY].Type2;
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
            if (GetPlayerMap(playerId) == mapNum &&
                GetPlayerX(playerId) == nextTileX &&
                GetPlayerY(playerId) == nextTileY)
            {
                return false;
            }
        }

        // Check for other NPC collision (using tile grid)
        for (var i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
        {
            if (i == mapNpcNum) continue;
            if (Data.MapNpc[mapNum].Npc[i].Num < 0) continue;
            int npcTileX = (int)Math.Floor((double)Data.MapNpc[mapNum].Npc[i].X / Constants.TileSize);
            int npcTileY = (int)Math.Floor((double)Data.MapNpc[mapNum].Npc[i].Y / Constants.TileSize);
            if (npcTileX == nextTileX && npcTileY == nextTileY)
            {
                return false;
            }
        }

        // Prevent movement if skill buffer is active
        if (Data.MapNpc[mapNum].Npc[mapNpcNum].SkillBuffer >= 0)
        {
            return false;
        }

        return true;
    }

    public static void NpcMove(int mapNum, int mapNpcNum, byte dir, int movement)
    {
        var count = System.Enum.GetValues(typeof(MovementState)).Length;
        int count2 = System.Enum.GetValues(typeof(Direction)).Length;
        if (mapNum < 0 || mapNum >= Core.Globals.Variables.MaxMaps || mapNpcNum < 0 || mapNpcNum >= Core.Globals.Variables.MaxMapNpcs || dir > count2 || movement < 0 || movement > count)
        {
            return;
        }
        // If already walking mid-step, ignore duplicate start.
        if (Data.MapNpc[mapNum].Npc[mapNpcNum].Moving == (byte)MovementState.Walking && _stepRemaining[mapNum, mapNpcNum] > 0)
            return;

        // Begin a new tile movement: set dir, movement state, step counter (32px)
        Data.MapNpc[mapNum].Npc[mapNpcNum].Dir = dir;
        Data.MapNpc[mapNum].Npc[mapNpcNum].Moving = (byte)MovementState.Walking;
        _stepRemaining[mapNum, mapNpcNum] = 32; // pixels to travel

        // Send start-of-move packet (position unchanged); client will animate pixel stepping locally.
        var buffer = new PacketWriter(4);
        buffer.WriteEnum(ServerPackets.SNpcMove);
        buffer.WriteInt32(mapNpcNum);
        buffer.WriteInt32(Data.MapNpc[mapNum].Npc[mapNpcNum].X);
        buffer.WriteInt32(Data.MapNpc[mapNum].Npc[mapNpcNum].Y);
        buffer.WriteByte(Data.MapNpc[mapNum].Npc[mapNpcNum].Dir);
        buffer.WriteInt32((int)MovementState.Walking);
        NetworkConfig.SendDataToMap(mapNum, buffer.GetBytes());
    }

    /// <summary>
    /// Advances active NPC pixel movement. Called frequently (e.g., every walk tick) from the main loop.
    /// Sends an SNpcDir packet when a tile step is completed to stop client movement exactly on tile.
    /// </summary>
    public static void ProcessActiveNpcMovement()
    {
        const int TileSize = 32;
        for (int map = 0; map < Core.Globals.Variables.MaxMaps; map++)
        {
            for (int i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
            {
                ref var npc = ref Data.MapNpc[map].Npc[i];
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
                    npc.X = Math.Max(0, Math.Min(npc.X, (Data.Map[map].MaxX - 1) * TileSize));
                    npc.Y = Math.Max(0, Math.Min(npc.Y, (Data.Map[map].MaxY - 1) * TileSize));
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

    public static void NpcDir(int mapNum, int mapNpcNum, byte dir)
    {
        int count = System.Enum.GetValues(typeof(Direction)).Length;
        if (mapNum < 0 || mapNum >= Core.Globals.Variables.MaxMaps || mapNpcNum < 0 || mapNpcNum >= Core.Globals.Variables.MaxMapNpcs || dir > count)
        {
            return;
        }

        Data.MapNpc[mapNum].Npc[mapNpcNum].Dir = dir;

        var packet = new PacketWriter(9);

        packet.WriteEnum(ServerPackets.SNpcDir);
        packet.WriteInt32(mapNpcNum);
        packet.WriteByte(dir);

        NetworkConfig.SendDataToMap(mapNum, packet.GetBytes());
    }

    /// <summary>
    /// Replace the NPC's route with the provided sequence of directions.
    /// </summary>
    public static void SetRoute(int mapNum, int mapNpcNum, System.Collections.Generic.IEnumerable<byte> directions)
    {
        if (mapNum < 0 || mapNum >= Core.Globals.Variables.MaxMaps || mapNpcNum < 0 || mapNpcNum >= Core.Globals.Variables.MaxMapNpcs) return;
        var q = new System.Collections.Generic.Queue<byte>();
        foreach (var d in directions) q.Enqueue(d);
        _route[mapNum, mapNpcNum] = q;
    }

    /// <summary>
    /// Clears any planned route for the NPC.
    /// </summary>
    public static void ClearRoute(int mapNum, int mapNpcNum)
    {
        if (mapNum < 0 || mapNum >= Core.Globals.Variables.MaxMaps || mapNpcNum < 0 || mapNpcNum >= Core.Globals.Variables.MaxMapNpcs) return;
        _route[mapNum, mapNpcNum] = null;
    }

    /// <summary>
    /// If there is a pending route and the NPC is not currently mid-step, dequeue the next step and start moving.
    /// </summary>
    public static bool TryStartNextStepNow(int mapNum, int mapNpcNum)
    {
        if (mapNum < 0 || mapNum >= Core.Globals.Variables.MaxMaps || mapNpcNum < 0 || mapNpcNum >= Core.Globals.Variables.MaxMapNpcs) return false;
        ref var npc = ref Data.MapNpc[mapNum].Npc[mapNpcNum];
        if (npc.Moving == (byte)MovementState.Walking && _stepRemaining[mapNum, mapNpcNum] > 0) return false;
        return TryDequeueNextStep(mapNum, mapNpcNum);
    }

    /// <summary>
    /// Pops the next planned direction and begins a new tile step if possible.
    /// </summary>
    private static bool TryDequeueNextStep(int mapNum, int mapNpcNum)
    {
        var route = _route[mapNum, mapNpcNum];
        if (route == null || route.Count == 0) return false;
        // Validate and attempt the next step
        var nextDir = route.Peek();
        if (CanNpcMove(mapNum, mapNpcNum, nextDir))
        {
            route.Dequeue();
            NpcMove(mapNum, mapNpcNum, nextDir, (int)MovementState.Walking);
            // If route is finished after this step, clear it now; ProcessActiveNpcMovement will send stop when the step ends.
            if (route.Count == 0)
            {
                _route[mapNum, mapNpcNum] = null;
            }
            return true;
        }
        // Cannot move as planned; drop the route so callers can recompute.
        _route[mapNum, mapNpcNum] = null;
        return false;
    }
}