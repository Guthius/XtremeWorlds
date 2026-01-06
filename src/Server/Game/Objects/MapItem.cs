using Core.Globals;
using Core.Interfaces;
using Core.Net;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using static Core.Net.Packets;
using MapItemData = Core.Globals.Type.MapItem;

namespace Server
{
    public class MapItem : IData
    {
        public static MapItemData[,] Instance { get; } = new MapItemData[Core.Globals.Variables.MaxMaps, Core.Globals.Variables.MaxMapItems];

        public static void SpawnAll()
        {
            for (var map = 0; map < Core.Globals.Variables.MaxMaps; map++)
            {
                Spawn(map);
            }
        }

        public static void Spawn(int map)
        {
            if (map < 0 || map >= Core.Globals.Variables.MaxMaps)
            {
                return;
            }

            var items = Item.Instance;

            if (Server.Map.Instance.Count <= map)
            {
                return;
            }

            if (Server.Map.Instance[map].NoRespawn)
            {
                return;
            }

            for (var x = 0; x < Server.Map.Instance[map].MaxX; x++)
            {
                for (var y = 0; y < Server.Map.Instance[map].MaxY; y++)
                {
                    if (Server.Map.Instance[map].Tile[x, y].Type == TileType.Item)
                    {
                        var itemId = Server.Map.Instance[map].Tile[x, y].Data1;
                        if (itemId < 0 || itemId >= items.Count)
                        {
                            continue;
                        }

                        var itemTemplate = items[itemId];

                        if (itemTemplate.Type == (byte)ItemCategory.Currency ||
                            itemTemplate.Stackable == 1)
                        {
                            var value = Server.Map.Instance[map].Tile[x, y].Data2 < 1 ? 1 : Server.Map.Instance[map].Tile[x, y].Data2;

                            OnSpawn(itemId, value, map, x, y);
                        }
                        else
                        {
                            OnSpawn(itemId, Server.Map.Instance[map].Tile[x, y].Data2, map, x, y);
                        }
                    }

                    if (Server.Map.Instance[map].Tile[x, y].Type2 == TileType.Item)
                    {
                        var itemId = Server.Map.Instance[map].Tile[x, y].Data1_2;
                        if (itemId < 0 || itemId >= items.Count)
                        {
                            continue;
                        }

                        var itemTemplate = items[itemId];

                        if (itemTemplate.Type == (byte)ItemCategory.Currency ||
                            itemTemplate.Stackable == 1)
                        {
                            var value = Server.Map.Instance[map].Tile[x, y].Data2_2 < 1 ? 1 : Server.Map.Instance[map].Tile[x, y].Data2_2;

                            OnSpawn(itemId, value, map, x, y);
                        }
                        else
                        {
                            OnSpawn(itemId, Server.Map.Instance[map].Tile[x, y].Data2_2, map, x, y);
                        }
                    }
                }
            }
        }


        public static void OnSpawn(int id, int val, int map, int x, int y)
        {
            if (id < 0 || id >= Core.Globals.Variables.MaxItems || map < 0 || map >= Core.Globals.Variables.MaxMaps)
            {
                return;
            }

            if (id >= Item.Instance.Count)
            {
                return;
            }

            var item = Item.Instance[id];

            var slot = FindOpenSlot(map);
            if (slot == -1)
            {
                return;
            }

            if (item.Type != (byte)ItemCategory.Currency && item.Stackable != 1)
            {
                for (var i = 0; i < val; i++)
                {
                    slot = FindOpenSlot(map);
                    if (slot == -1)
                    {
                        return;
                    }

                    SpawnSlot(slot, id, 1, map, x, y);
                }
            }
            else
            {
                SpawnSlot(slot, id, val, map, x, y);
            }

            try
            {
                Script.Instance?.OnSpawnItem();
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnSpawn));
            }
        }

        public static void SpawnSlot(int mapItemSlot, int id, int val, int map, int x, int y)
        {
            if (mapItemSlot < 0 || mapItemSlot > Core.Globals.Variables.MaxMapItems || id < 0 || id > Core.Globals.Variables.MaxItems || map < 0 || map >= Core.Globals.Variables.MaxMaps)
            {
                return;
            }

            x *= 32;
            y *= 32;

            Instance[map, mapItemSlot].Num = id;
            Instance[map, mapItemSlot].Value = val;
            Instance[map, mapItemSlot].X = x;
            Instance[map, mapItemSlot].Y = y;

            var packet = new PacketWriter();

            packet.WriteEnum(ServerPackets.SSpawnItem);
            packet.WriteInt32(mapItemSlot);
            packet.WriteInt32(id);
            packet.WriteInt32(val);
            packet.WriteInt32(x);
            packet.WriteInt32(y);

            NetworkConfig.SendDataToMap(map, packet.GetBytes());
        }

        public static int FindOpenSlot(int map)
        {
            if (map < 0 || map >= Core.Globals.Variables.MaxMaps)
            {
                return -1;
            }

            for (var mapItem = 0; mapItem < Core.Globals.Variables.MaxMapItems; mapItem++)
            {
                if (Instance[map, mapItem].Num == -1)
                {
                    return mapItem;
                }
            }

            return -1;
        }

        public static void OnClear(int index, int map)
        {
            Instance[map, index].PlayerName = "";
            Instance[map, index].Num = -1;
        }

        public static void OnDraw(int index)
        {
            throw new NotImplementedException();
        }
        
        public static void OnStream(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnClear()
        {
            for (int map = 0; map < Core.Globals.Variables.MaxMaps; map++)
            {
                for (int i = 0; i < Core.Globals.Variables.MaxMapItems; i++)
                {
                    OnClear(i, map);
                }
            }
        }

        public static void OnLoad(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnSave(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnClear(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnUpdate(int index)
        {
            throw new NotImplementedException();
        }
    }
}
