using Core.Globals;
using System;
using System.Collections.Generic;
using System.Text;
using static Core.Globals.Commands;
using MapResourceCacheData = Core.Globals.Type.MapResource;

namespace Server
{
    public static class MapResource
    {
        public static MapResourceCacheData[] Instance { get; private set; } = new MapResourceCacheData[Variables.MaxResources];
        public static void OnUpdate(int mapNum)
        {
            var resourceCount = 0;

            for (var x = 0; x < Server.Map.Instance[mapNum].MaxX; x++)
            {
                for (var y = 0; y < Server.Map.Instance[mapNum].MaxY; y++)
                {
                    if (Server.Map.Instance[mapNum].Tile[x, y].Type != TileType.Resource &&
                        Server.Map.Instance[mapNum].Tile[x, y].Type2 != TileType.Resource)
                    {
                        continue;
                    }

                    resourceCount++;

                    Array.Resize(ref MapResource.Instance[mapNum].ResourceData, resourceCount);

                    MapResource.Instance[mapNum].ResourceData[resourceCount - 1].X = x;
                    MapResource.Instance[mapNum].ResourceData[resourceCount - 1].Y = y;
                    MapResource.Instance[mapNum].ResourceData[resourceCount - 1].Health = (byte)Resource.Instance[Server.Map.Instance[mapNum].Tile[x, y].Data1].Health;
                }
            }

            MapResource.Instance[mapNum].ResourceCount = resourceCount;
        }

        public static void OnLevel(int playerId, int skillSlot)
        {
            var levels = 0;

            if (GetPlayerGatherSkillLevel(playerId, skillSlot) == Script.Instance?.MaxLevel)
            {
                return;
            }

            while (GetPlayerGatherSkillExperience(playerId, skillSlot) >= GetPlayerGatherSkillMaxExperience(playerId, skillSlot))
            {
                var expRollover = GetPlayerGatherSkillExperience(playerId, skillSlot) - GetPlayerGatherSkillMaxExperience(playerId, skillSlot);

                SetPlayerGatherSkillLevel(playerId, skillSlot, GetPlayerGatherSkillLevel(playerId, skillSlot) + 1);
                SetPlayerGatherSkillExperience(playerId, skillSlot, expRollover);
                SetPlayerGatherSkillMaxExperience(playerId, skillSlot, (int)GetSkillNextLevel(playerId, skillSlot));

                levels++;
            }

            if (levels == 0)
            {
                return;
            }

            NetworkSend.SendPlayerMessage(playerId, levels == 1
                ? $"Your {GetResourceSkillName((ResourceSkill)skillSlot)} has gone up a level!"
                : $"Your {GetResourceSkillName((ResourceSkill)skillSlot)} has gone up by {levels} levels!", (int)ColorName.BrightGreen);

            NetworkSend.SendPlayerData(playerId);
        }

        public static void OnUpdate(int playerId, int x, int y)
        {
            var mapNum = GetPlayerMap(playerId);

            if (x < 0 || y < 0 || x >= Server.Map.Instance[mapNum].MaxX || y >= Server.Map.Instance[mapNum].MaxY)
            {
                return;
            }

            if (Server.Map.Instance[mapNum].Tile[x, y].Type != TileType.Resource &&
                Server.Map.Instance[mapNum].Tile[x, y].Type2 != TileType.Resource)
            {
                return;
            }

            var resourceNum = 0;
            var resourceIndex = Server.Map.Instance[mapNum].Tile[x, y].Data1;
            var resourceType = (byte)Resource.Instance[resourceIndex].ResourceType;

            for (var i = 0; i < MapResource.Instance[mapNum].ResourceCount; i++)
            {
                if (MapResource.Instance[mapNum].ResourceData[i].X == x &&
                    MapResource.Instance[mapNum].ResourceData[i].Y == y)
                {
                    resourceNum = i;
                }
            }

            if (resourceNum < 0)
            {
                return;
            }

            if (GetPlayerPaperdoll(playerId, Equipment.Weapon) < 0 && Resource.Instance[resourceIndex].ToolRequired != 0)
            {
                NetworkSend.SendPlayerMessage(playerId, "You need a tool to gather this resource.", (int)ColorName.Yellow);
                return;
            }

            if (Item.Instance[GetPlayerPaperdoll(playerId, Equipment.Weapon)].Data3 != Resource.Instance[resourceIndex].ToolRequired)
            {
                NetworkSend.SendPlayerMessage(playerId, "You have the wrong type of tool equiped.", (int)ColorName.Yellow);
                return;
            }

            if (Resource.Instance[resourceIndex].ItemReward > 0)
            {
                if (Player.FindOpenInvSlot(playerId, Resource.Instance[resourceIndex].ItemReward) == 0)
                {
                    NetworkSend.SendPlayerMessage(playerId, "You have no inventory space.", (int)ColorName.Yellow);
                    return;
                }
            }

            if (Resource.Instance[resourceIndex].LvlRequired > GetPlayerGatherSkillLevel(playerId, resourceType))
            {
                NetworkSend.SendPlayerMessage(playerId, "Your level is too low!", (int)ColorName.Yellow);
                return;
            }

            if (MapResource.Instance[mapNum].ResourceData[resourceNum].State != 0)
            {
                NetworkSend.SendActionMessage(mapNum, Resource.Instance[resourceIndex].EmptyMessage, (int)ColorName.BrightRed, 1, GetPlayerX(playerId) * 32, GetPlayerY(playerId) * 32);
                return;
            }

            var resourceX = MapResource.Instance[mapNum].ResourceData[resourceNum].X;
            var resourceY = MapResource.Instance[mapNum].ResourceData[resourceNum].Y;

            int damage;
            if (Resource.Instance[resourceIndex].ToolRequired == 0)
            {
                damage = 1 * GetPlayerGatherSkillLevel(playerId, resourceType);
            }
            else
            {
                damage = Item.Instance[GetPlayerPaperdoll(playerId, Equipment.Weapon)].Data2;
            }

            if (damage <= 0)
            {
                NetworkSend.SendActionMessage(mapNum, "Miss!", (int)ColorName.BrightRed, 1, resourceX * 32, resourceY * 32);
                return;
            }

            if (MapResource.Instance[mapNum].ResourceData[resourceNum].Health - damage >= 0)
            {
                MapResource.Instance[mapNum].ResourceData[resourceNum].Health = (byte)(MapResource.Instance[mapNum].ResourceData[resourceNum].Health - damage);
                NetworkSend.SendActionMessage(mapNum, "-" + damage, (int)ColorName.BrightRed, 1, resourceX * 32, resourceY * 32);
                NetworkSend.SendAnimation(mapNum, Resource.Instance[resourceIndex].Animation, resourceX, resourceY);

                return;
            }

            MapResource.Instance[mapNum].ResourceData[resourceNum].State = 0; // Cut
            MapResource.Instance[mapNum].ResourceData[resourceNum].Timer = General.GetTimeMs();

            NetworkSend.SendMapResourceToMap(mapNum);

            NetworkSend.SendActionMessage(mapNum, Resource.Instance[resourceIndex].SuccessMessage, (int)ColorName.BrightGreen, 1, GetPlayerX(playerId) * 32, GetPlayerY(playerId) * 32);
            Player.GiveInv(playerId, Resource.Instance[resourceIndex].ItemReward, 1);
            NetworkSend.SendAnimation(mapNum, Resource.Instance[resourceIndex].Animation, resourceX, resourceY);

            SetPlayerGatherSkillExperience(playerId, resourceType, GetPlayerGatherSkillExperience(playerId, resourceType) + Resource.Instance[resourceIndex].ExperienceReward);

            NetworkSend.SendPlayerMessage(playerId, $"Your {GetResourceSkillName((ResourceSkill)resourceType)} has earned {Resource.Instance[resourceIndex].ExperienceReward} experience. ({GetPlayerGatherSkillExperience(playerId, resourceType)}/{GetPlayerGatherSkillMaxExperience(playerId, resourceType)})", (int)ColorName.BrightGreen);
            NetworkSend.SendPlayerData(playerId);

            MapResource.OnLevel(playerId, resourceType);
        }
    }
}
