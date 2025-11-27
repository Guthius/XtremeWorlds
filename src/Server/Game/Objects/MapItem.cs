using Core.Globals;
using Core.Net;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using static Core.Net.Packets;

namespace Server
{
    public static class MapItem
    {
        public static void GetItem(int playerId)
        {
            var mapNum = Command.GetPlayerMap(playerId);

            for (var mapItemNum = 0; mapItemNum < Core.Globals.Variables.MaxMapItems; mapItemNum++)
            {
                if (Data.MapItem[mapNum, mapItemNum].Num < 0 ||
                    Data.MapItem[mapNum, mapItemNum].Num >= Core.Globals.Variables.MaxItems)
                {
                    continue;
                }

                if (Math.Floor((double)Data.MapItem[mapNum, mapItemNum].X / 32) != Command.GetPlayerX(playerId) || Math.Floor((double)Data.MapItem[mapNum, mapItemNum].Y / 32) != Command.GetPlayerY(playerId))
                {
                    continue;
                }

                var slot = Player.FindOpenInvSlot(playerId, Data.MapItem[mapNum, mapItemNum].Num);
                if (slot == -1)
                {
                    NetworkSend.PlayerMsg(playerId, "Your inventory is full.", (int)ColorName.BrightRed);
                    break;
                }

                if (!Player.CanPickup(playerId, mapItemNum))
                {
                    break;
                }

                try
                {
                    Script.Instance?.MapGetItem(playerId, mapNum, mapItemNum, slot);
                }
                catch (Exception ex)
                {
                    General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(GetItem));
                }

                break;
            }
        }
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

            if (Data.Map[mapNum].NoRespawn)
            {
                return;
            }

            for (var x = 0; x < Data.Map[mapNum].MaxX; x++)
            {
                for (var y = 0; y < Data.Map[mapNum].MaxY; y++)
                {
                    if (Data.Map[mapNum].Tile[x, y].Type == TileType.Item)
                    {
                        if (Data.Item[Data.Map[mapNum].Tile[x, y].Data1].Type == (byte)ItemCategory.Currency ||
                            Data.Item[Data.Map[mapNum].Tile[x, y].Data1].Stackable == 1)
                        {
                            var value = Data.Map[mapNum].Tile[x, y].Data2 < 1 ? 1 : Data.Map[mapNum].Tile[x, y].Data2;

                            Spawn(Data.Map[mapNum].Tile[x, y].Data1, value, mapNum, x, y);
                        }
                        else
                        {
                            Spawn(Data.Map[mapNum].Tile[x, y].Data1, Data.Map[mapNum].Tile[x, y].Data2, mapNum, x, y);
                        }
                    }

                    if (Data.Map[mapNum].Tile[x, y].Type2 == TileType.Item)
                    {
                        if (Data.Item[Data.Map[mapNum].Tile[x, y].Data1_2].Type == (byte)ItemCategory.Currency ||
                            Data.Item[Data.Map[mapNum].Tile[x, y].Data1_2].Stackable == 1)
                        {
                            var value = Data.Map[mapNum].Tile[x, y].Data2_2 < 1 ? 1 : Data.Map[mapNum].Tile[x, y].Data2_2;

                            Spawn(Data.Map[mapNum].Tile[x, y].Data1_2, value, mapNum, x, y);
                        }
                        else
                        {
                            Spawn(Data.Map[mapNum].Tile[x, y].Data1_2, Data.Map[mapNum].Tile[x, y].Data2_2, mapNum, x, y);
                        }
                    }
                }
            }
        }


        public static void Spawn(int itemNum, int itemVal, int mapNum, int x, int y)
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

            if (Data.Item[itemNum].Type != (byte)ItemCategory.Currency && Data.Item[itemNum].Stackable != 1)
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

        public static void Clear(int index, int mapNum)
        {
            Data.MapItem[mapNum, index].PlayerName = "";
            Data.MapItem[mapNum, index].Num = -1;
        }
    }
}
