using Core.Globals;
using Core.Interfaces;
using Core.Net;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using static Core.Net.Packets;

namespace Server
{
    public class MapItem : IData
    {
        public static void SpawnAll()
        {
            for (var mapNum = 0; mapNum < Core.Globals.Variables.MaxMaps; mapNum++)
            {
                Spawn(mapNum);
            }
        }

        public static void Spawn(int mapNum)
        {
            if (mapNum < 0 || mapNum >= Core.Globals.Variables.MaxMaps)
            {
                return;
            }

            if (Server.Map.Instance[mapNum].NoRespawn)
            {
                return;
            }

            for (var x = 0; x < Server.Map.Instance[mapNum].MaxX; x++)
            {
                for (var y = 0; y < Server.Map.Instance[mapNum].MaxY; y++)
                {
                    if (Server.Map.Instance[mapNum].Tile[x, y].Type == TileType.Item)
                    {
                        if (Item.Instance[Server.Map.Instance[mapNum].Tile[x, y].Data1].Type == (byte)ItemCategory.Currency ||
                            Item.Instance[Server.Map.Instance[mapNum].Tile[x, y].Data1].Stackable == 1)
                        {
                            var value = Server.Map.Instance[mapNum].Tile[x, y].Data2 < 1 ? 1 : Server.Map.Instance[mapNum].Tile[x, y].Data2;

                            OnSpawn(Server.Map.Instance[mapNum].Tile[x, y].Data1, value, mapNum, x, y);
                        }
                        else
                        {
                            OnSpawn(Server.Map.Instance[mapNum].Tile[x, y].Data1, Server.Map.Instance[mapNum].Tile[x, y].Data2, mapNum, x, y);
                        }
                    }

                    if (Server.Map.Instance[mapNum].Tile[x, y].Type2 == TileType.Item)
                    {
                        if (Item.Instance[Server.Map.Instance[mapNum].Tile[x, y].Data1_2].Type == (byte)ItemCategory.Currency ||
                            Item.Instance[Server.Map.Instance[mapNum].Tile[x, y].Data1_2].Stackable == 1)
                        {
                            var value = Server.Map.Instance[mapNum].Tile[x, y].Data2_2 < 1 ? 1 : Server.Map.Instance[mapNum].Tile[x, y].Data2_2;

                            OnSpawn(Server.Map.Instance[mapNum].Tile[x, y].Data1_2, value, mapNum, x, y);
                        }
                        else
                        {
                            OnSpawn(Server.Map.Instance[mapNum].Tile[x, y].Data1_2, Server.Map.Instance[mapNum].Tile[x, y].Data2_2, mapNum, x, y);
                        }
                    }
                }
            }
        }


        public static void OnSpawn(int itemNum, int itemVal, int mapNum, int x, int y)
        {
            if (itemNum < 0 || itemNum > Core.Globals.Variables.MaxItems || mapNum < 0 || mapNum >= Core.Globals.Variables.MaxMaps)
            {
                return;
            }

            var slot = FindOpenSlot(mapNum);
            if (slot == -1)
            {
                return;
            }

            if (Item.Instance[itemNum].Type != (byte)ItemCategory.Currency && Item.Instance[itemNum].Stackable != 1)
            {
                for (var i = 0; i < itemVal; i++)
                {
                    slot = FindOpenSlot(mapNum);
                    if (slot == -1)
                    {
                        return;
                    }

                    SpawnSlot(slot, itemNum, 1, mapNum, x, y);
                }
            }
            else
            {
                SpawnSlot(slot, itemNum, itemVal, mapNum, x, y);
            }
        }

        public static void SpawnSlot(int mapItemSlot, int itemNum, int itemVal, int mapNum, int x, int y)
        {
            if (mapItemSlot < 0 || mapItemSlot > Core.Globals.Variables.MaxMapItems || itemNum < 0 || itemNum > Core.Globals.Variables.MaxItems || mapNum < 0 || mapNum >= Core.Globals.Variables.MaxMaps)
            {
                return;
            }

            x *= 32;
            y *= 32;

            Data.MapItem[mapNum, mapItemSlot].Num = itemNum;
            Data.MapItem[mapNum, mapItemSlot].Value = itemVal;
            Data.MapItem[mapNum, mapItemSlot].X = x;
            Data.MapItem[mapNum, mapItemSlot].Y = y;

            var packet = new PacketWriter();

            packet.WriteEnum(ServerPackets.SSpawnItem);
            packet.WriteInt32(mapItemSlot);
            packet.WriteInt32(itemNum);
            packet.WriteInt32(itemVal);
            packet.WriteInt32(x);
            packet.WriteInt32(y);

            NetworkConfig.SendDataToMap(mapNum, packet.GetBytes());
        }

        public static int FindOpenSlot(int mapNum)
        {
            if (mapNum < 0 || mapNum >= Core.Globals.Variables.MaxMaps)
            {
                return -1;
            }

            for (var mapItemNum = 0; mapItemNum < Core.Globals.Variables.MaxMapItems; mapItemNum++)
            {
                if (Data.MapItem[mapNum, mapItemNum].Num == -1)
                {
                    return mapItemNum;
                }
            }

            return -1;
        }

        public static void OnClear(int index, int mapNum)
        {
            Data.MapItem[mapNum, index].PlayerName = "";
            Data.MapItem[mapNum, index].Num = -1;
        }

        public static void OnDraw(int index)
        {
            throw new NotImplementedException();
        }
        
        public static void OnStream(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnReset()
        {
            for (int mapNum = 0; mapNum < Core.Globals.Variables.MaxMaps; mapNum++)
            {
                for (int i = 0; i < Core.Globals.Variables.MaxMapItems; i++)
                {
                    OnClear(i, mapNum);
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
