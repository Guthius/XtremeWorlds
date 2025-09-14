using Core;
using Core.Globals;
using Core.Net;
using Microsoft.Extensions.Logging;
using Server.Game;
using Server.Game.Net;
using Server.Net;
using static Core.Globals.Command;
using static Core.Net.Packets;

namespace Server;

public static class Npc
{
    // Tracks remaining pixels to finish the current tile step for each NPC on each map.
    private static readonly int[,] _stepRemaining = new int[Core.Globals.Constant.MaxMaps, Core.Globals.Constant.MaxMapNpcs];
    public static async Task SpawnAllMapNpcs()
    {
        await Task.WhenAll(Enumerable
            .Range(0, Core.Globals.Constant.MaxMapNpcs)
            .Select(SpawnMapNpcs));
    }

    public static async Task SpawnMapNpcs(int mapNum)
    {
        await Task.WhenAll(Enumerable
            .Range(0, Core.Globals.Constant.MaxMapNpcs)
            .Select(mapNpcNum => Task.Run(() =>
                SpawnNpc(mapNpcNum, mapNum))));
    }

    public static void SpawnNpc(int mapNpcNum, int mapNum)
    {
        var spawned = false;

        if (Data.Map[mapNum].NoRespawn)
        {
            return;
        }

        var npcNum = Data.Map[mapNum].Npc[mapNpcNum];
        if (mapNpcNum == 0 || npcNum < 0 || npcNum > Core.Globals.Constant.MaxNpcs)
        {
            return;
        }

        if (Data.Npc[npcNum].SpawnTime != (byte) Clock.Instance.TimeOfDay && Data.Npc[npcNum].SpawnTime != 0)
        {
            Database.ClearMapNpc(mapNpcNum, mapNum);

            SendMapNpcsToMap(mapNum);

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
                if (Data.Map[mapNum].Tile[x, y].Type != TileType.NpcSpawn ||
                    Data.Map[mapNum].Tile[x, y].Data1 != mapNpcNum)
                {
                    continue;
                }

                Data.MapNpc[mapNum].Npc[mapNpcNum].X = x * 32;
                Data.MapNpc[mapNum].Npc[mapNpcNum].Dir = (byte) Data.Map[mapNum].Tile[x, y].Data2;

                spawned = true;
                break;
            }
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

        SendMapNpcVitals(mapNum, (byte) mapNpcNum);
    }

    public static bool NpcTileIsOpen(int mapNum, int x, int y)
    {
        foreach (var playerId in PlayerService.Instance.PlayerIds)
        {
            if (GetPlayerMap(playerId) == mapNum &&
                GetPlayerX(playerId) == x * 32 &&
                GetPlayerY(playerId) == y * 32)
            {
                return false;
            }
        }

        for (var mapNpcNum = 0; mapNpcNum < Core.Globals.Constant.MaxMapNpcs; mapNpcNum++)
        {
            if (Data.MapNpc[mapNum].Npc[mapNpcNum].Num >= 0 &&
                Data.MapNpc[mapNum].Npc[mapNpcNum].X == x * 32 &&
                Data.MapNpc[mapNum].Npc[mapNpcNum].Y == y  * 32)
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
        if (mapNum < 0 || mapNum >= Core.Globals.Constant.MaxMaps || mapNpcNum < 0 || mapNpcNum >= Core.Globals.Constant.MaxMapNpcs || dir > count)
        {
            return false;
        }

        var x = Data.MapNpc[mapNum].Npc[mapNpcNum].X;
        var y = Data.MapNpc[mapNum].Npc[mapNpcNum].Y;
        // If already in mid-move, don't allow a new tile move.
        if (Data.MapNpc[mapNum].Npc[mapNpcNum].Moving == (byte)MovementState.Walking && _stepRemaining[mapNum, mapNpcNum] > 0)
            return false;

        int tileX = x / 32;
        int tileY = y / 32;
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
        for (var i = 0; i < Core.Globals.Constant.MaxMapNpcs; i++)
        {
            if (i == mapNpcNum) continue;
            if (Data.MapNpc[mapNum].Npc[i].Num < 0) continue;
            int npcTileX = (int)Math.Floor((double)Data.MapNpc[mapNum].Npc[i].X / 32);
            int npcTileY = (int)Math.Floor((double)Data.MapNpc[mapNum].Npc[i].Y / 32);
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
        if (mapNum < 0 || mapNum >= Core.Globals.Constant.MaxMaps || mapNpcNum < 0 || mapNpcNum >= Core.Globals.Constant.MaxMapNpcs || dir > count2 || movement < 0 || movement > count)
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
        for (int map = 0; map < Core.Globals.Constant.MaxMaps; map++)
        {
            for (int i = 0; i < Core.Globals.Constant.MaxMapNpcs; i++)
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

                    // Send stop (direction) packet so clients stop animating exactly on tile
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
        if (mapNum < 0 || mapNum >= Core.Globals.Constant.MaxMaps || mapNpcNum < 0 || mapNpcNum >= Core.Globals.Constant.MaxMapNpcs || dir > count)
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

    private static void SendMapNpcsToMap(int mapNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SMapNpcData);

        for (var mapNpcNum = 0; mapNpcNum < Core.Globals.Constant.MaxMapNpcs; mapNpcNum++)
        {
            packet.WriteInt32(Data.MapNpc[mapNum].Npc[mapNpcNum].Num);
            packet.WriteInt32(Data.MapNpc[mapNum].Npc[mapNpcNum].X);
            packet.WriteInt32(Data.MapNpc[mapNum].Npc[mapNpcNum].Y);
            packet.WriteByte(Data.MapNpc[mapNum].Npc[mapNpcNum].Dir);
        }

        NetworkConfig.SendDataToMap(mapNum, packet.GetBytes());
    }

    public static void HandleRequestEditNpc(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetPlayerAccess(session.Id) < (byte) AccessLevel.Developer)
        {
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Npc);
        if (!string.IsNullOrEmpty(user))
        {
            NetworkSend.PlayerMsg(session.Id, "The game editor is locked and being used by " + user + ".", (int) ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Npc;

        Item.SendItems(session.Id);
        Animation.SendAnimations(session.Id);
        NetworkSend.SendSkills(session.Id);

        SendNpcs(session.Id);

        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SNpcEditor);

        PlayerService.Instance.SendDataTo(session.Id, packet.GetBytes());
    }

    public static void HandleSaveNpc(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetPlayerAccess(session.Id) < (byte) AccessLevel.Developer)
        {
            return;
        }

        var npcNum = packetReader.ReadInt32();
        if (npcNum < 0 | npcNum > Core.Globals.Constant.MaxNpcs)
        {
            return;
        }

        Data.Npc[npcNum].Animation = packetReader.ReadInt32();
        Data.Npc[npcNum].AttackSay = packetReader.ReadString();
        Data.Npc[npcNum].Behaviour = packetReader.ReadByte();

        for (var i = 0; i < Core.Globals.Constant.MaxDropItems; i++)
        {
            Data.Npc[npcNum].DropChance[i] = packetReader.ReadInt32();
            Data.Npc[npcNum].DropItem[i] = packetReader.ReadInt32();
            Data.Npc[npcNum].DropItemValue[i] = packetReader.ReadInt32();
        }

        Data.Npc[npcNum].Exp = packetReader.ReadInt32();
        Data.Npc[npcNum].Faction = packetReader.ReadByte();
        Data.Npc[npcNum].Hp = packetReader.ReadInt32();
        Data.Npc[npcNum].Name = packetReader.ReadString();
        Data.Npc[npcNum].Range = packetReader.ReadByte();
        Data.Npc[npcNum].SpawnTime = packetReader.ReadByte();
        Data.Npc[npcNum].SpawnSecs = packetReader.ReadInt32();
        Data.Npc[npcNum].Sprite = packetReader.ReadInt32();

        var statCount = Enum.GetValues<Stat>().Length;
        for (var i = 0; i < statCount; i++)
        {
            Data.Npc[npcNum].Stat[i] = packetReader.ReadByte();
        }

        for (var i = 0; i < Core.Globals.Constant.MaxNpcSkills; i++)
        {
            Data.Npc[npcNum].Skill[i] = packetReader.ReadByte();
        }

        Data.Npc[npcNum].Level = packetReader.ReadByte();
        Data.Npc[npcNum].Damage = packetReader.ReadInt32();

        Database.SaveNpc(npcNum);

        General.Logger.LogInformation("{AccountName} saved NPC #{NpcNum}",
            GetAccountLogin(session.Id), npcNum);

        SendUpdateNpcToAll(npcNum);
    }

    public static void SendNpcs(int playerId)
    {
        for (var npcNum = 0; npcNum < Core.Globals.Constant.MaxNpcs; npcNum++)
        {
            if (Data.Npc[npcNum].Name.Length > 0)
            {
                SendUpdateNpcTo(playerId, npcNum);
            }
        }
    }

    public static void SendUpdateNpcTo(int playerId, int npcNum)
    {
        var buffer = new PacketWriter();

        buffer.WriteEnum(ServerPackets.SUpdateNpc);
        buffer.WriteInt32(npcNum);
        buffer.WriteInt32(Data.Npc[npcNum].Animation);
        buffer.WriteString(Data.Npc[npcNum].AttackSay);
        buffer.WriteByte(Data.Npc[npcNum].Behaviour);

        for (var i = 0; i < Core.Globals.Constant.MaxDropItems; i++)
        {
            buffer.WriteInt32(Data.Npc[npcNum].DropChance[i]);
            buffer.WriteInt32(Data.Npc[npcNum].DropItem[i]);
            buffer.WriteInt32(Data.Npc[npcNum].DropItemValue[i]);
        }

        buffer.WriteInt32(Data.Npc[npcNum].Exp);
        buffer.WriteByte(Data.Npc[npcNum].Faction);
        buffer.WriteInt32(Data.Npc[npcNum].Hp);
        buffer.WriteString(Data.Npc[npcNum].Name);
        buffer.WriteByte(Data.Npc[npcNum].Range);
        buffer.WriteByte(Data.Npc[npcNum].SpawnTime);
        buffer.WriteInt32(Data.Npc[npcNum].SpawnSecs);
        buffer.WriteInt32(Data.Npc[npcNum].Sprite);

        var statCount = Enum.GetValues<Stat>().Length;
        for (var i = 0; i < statCount; i++)
        {
            buffer.WriteByte(Data.Npc[npcNum].Stat[i]);
        }

        for (var i = 0; i < Core.Globals.Constant.MaxNpcSkills; i++)
        {
            buffer.WriteByte(Data.Npc[npcNum].Skill[i]);
        }

        buffer.WriteInt32(Data.Npc[npcNum].Level);
        buffer.WriteInt32(Data.Npc[npcNum].Damage);

        PlayerService.Instance.SendDataTo(playerId, buffer.GetBytes());
    }

    public static void SendUpdateNpcToAll(int npcNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateNpc);
        packet.WriteInt32(npcNum);
        packet.WriteInt32(Data.Npc[npcNum].Animation);
        packet.WriteString(Data.Npc[npcNum].AttackSay);
        packet.WriteByte(Data.Npc[npcNum].Behaviour);

        for (var i = 0; i < Core.Globals.Constant.MaxDropItems; i++)
        {
            packet.WriteInt32(Data.Npc[npcNum].DropChance[i]);
            packet.WriteInt32(Data.Npc[npcNum].DropItem[i]);
            packet.WriteInt32(Data.Npc[npcNum].DropItemValue[i]);
        }

        packet.WriteInt32(Data.Npc[npcNum].Exp);
        packet.WriteByte(Data.Npc[npcNum].Faction);
        packet.WriteInt32(Data.Npc[npcNum].Hp);
        packet.WriteString(Data.Npc[npcNum].Name);
        packet.WriteByte(Data.Npc[npcNum].Range);
        packet.WriteByte(Data.Npc[npcNum].SpawnTime);
        packet.WriteInt32(Data.Npc[npcNum].SpawnSecs);
        packet.WriteInt32(Data.Npc[npcNum].Sprite);

        var statCount = Enum.GetValues<Stat>().Length;
        for (var i = 0; i < statCount; i++)
        {
            packet.WriteByte(Data.Npc[npcNum].Stat[i]);
        }

        for (var i = 0; i < Core.Globals.Constant.MaxNpcSkills; i++)
        {
            packet.WriteByte(Data.Npc[npcNum].Skill[i]);
        }

        packet.WriteInt32(Data.Npc[npcNum].Level);
        packet.WriteInt32(Data.Npc[npcNum].Damage);

        PlayerService.Instance.SendDataToAll(packet.GetBytes());
    }

    
    public static void SendMapNpcVitals(int mapNum, byte mapNpcNum)
    {
        var packet = new PacketWriter(4);

        packet.WriteInt32((int)ServerPackets.SMapNpcVitals);
        packet.WriteInt32(mapNpcNum);

        var vitalCount = Enum.GetValues<Vital>().Length;
        for (var i = 0; i < vitalCount; i++)
        {
            packet.WriteInt32(Data.MapNpc[mapNum].Npc[mapNpcNum].Vital[i]);
        }

        NetworkConfig.SendDataToMap(mapNum, packet.GetBytes());
    }
}