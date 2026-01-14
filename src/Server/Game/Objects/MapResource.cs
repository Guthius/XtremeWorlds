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
        public static MapResourceCacheData[] Instance { get; private set; } = new MapResourceCacheData[Core.Globals.Variables.MaxMaps];
        public static void OnUpdate(int map)
        {
            var resourceCount = 0;

            for (var x = 0; x < Server.Map.Instance[map].MaxX; x++)
            {
                for (var y = 0; y < Server.Map.Instance[map].MaxY; y++)
                {
                    if (Server.Map.Instance[map].Tile[x, y].Type != TileType.Resource &&
                        Server.Map.Instance[map].Tile[x, y].Type2 != TileType.Resource)
                    {
                        continue;
                    }

                    resourceCount++;

                    Array.Resize(ref MapResource.Instance[map].ResourceData, resourceCount);

                    MapResource.Instance[map].ResourceData[resourceCount - 1].X = x;
                    MapResource.Instance[map].ResourceData[resourceCount - 1].Y = y;
                    MapResource.Instance[map].ResourceData[resourceCount - 1].Health = (byte)Resource.Instance[Server.Map.Instance[map].Tile[x, y].Data1].Health;
                }
            }

            MapResource.Instance[map].ResourceCount = resourceCount;
        }

        public static void OnLevel(int playerId, int skillSlot)
        {
            var count = 0;

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

                count++;
            }

            if (count == 0)
            {
                return;
            }

            NetworkSend.PlayerMessage(playerId, count == 1
                ? $"Your {GetResourceSkillName((ResourceSkill)skillSlot)} has gained a level!"
                : $"Your {GetResourceSkillName((ResourceSkill)skillSlot)} has gained {count} levels!", (int)ColorName.BrightGreen);

            NetworkSend.PlayerData(playerId);
        }

        public static void OnUpdate(int playerId, int x, int y)
        {
            var map = GetPlayerMap(playerId);

            if (x < 0 || y < 0 || x >= Server.Map.Instance[map].MaxX || y >= Server.Map.Instance[map].MaxY)
            {
                return;
            }

            if (Server.Map.Instance[map].Tile[x, y].Type != TileType.Resource &&
                Server.Map.Instance[map].Tile[x, y].Type2 != TileType.Resource)
            {
                return;
            }

            var resourceNum = 0;
            var resourceIndex = Server.Map.Instance[map].Tile[x, y].Data1;
            var resourceType = (byte)Resource.Instance[resourceIndex].ResourceType;

            for (var i = 0; i < MapResource.Instance[map].ResourceCount; i++)
            {
                if (MapResource.Instance[map].ResourceData[i].X == x &&
                    MapResource.Instance[map].ResourceData[i].Y == y)
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
                NetworkSend.PlayerMessage(playerId, "You need a tool to gather this resource.", (int)ColorName.Yellow);
                return;
            }

            if (Item.Instance[GetPlayerPaperdoll(playerId, Equipment.Weapon)].Data3 != Resource.Instance[resourceIndex].ToolRequired)
            {
                NetworkSend.PlayerMessage(playerId, "You have the wrong type of tool equiped.", (int)ColorName.Yellow);
                return;
            }

            if (Resource.Instance[resourceIndex].ItemReward > 0)
            {
                if (Player.FindOpenInvSlot(playerId, Resource.Instance[resourceIndex].ItemReward) == 0)
                {
                    NetworkSend.PlayerMessage(playerId, "You have no inventory space.", (int)ColorName.Yellow);
                    return;
                }
            }

            if (Resource.Instance[resourceIndex].LvlRequired > GetPlayerGatherSkillLevel(playerId, resourceType))
            {
                NetworkSend.PlayerMessage(playerId, "Your level is too low!", (int)ColorName.Yellow);
                return;
            }

            if (MapResource.Instance[map].ResourceData[resourceNum].State != 0)
            {
                NetworkSend.ActionMessage(map, Resource.Instance[resourceIndex].EmptyMessage, (int)ColorName.BrightRed, 1, GetPlayerX(playerId) * 32, GetPlayerY(playerId) * 32);
                return;
            }

            var resourceX = MapResource.Instance[map].ResourceData[resourceNum].X;
            var resourceY = MapResource.Instance[map].ResourceData[resourceNum].Y;

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
                NetworkSend.ActionMessage(map, "Miss!", (int)ColorName.BrightRed, 1, resourceX * 32, resourceY * 32);
                return;
            }

            if (MapResource.Instance[map].ResourceData[resourceNum].Health - damage >= 0)
            {
                MapResource.Instance[map].ResourceData[resourceNum].Health = (byte)(MapResource.Instance[map].ResourceData[resourceNum].Health - damage);
                NetworkSend.ActionMessage(map, "-" + damage, (int)ColorName.BrightRed, 1, resourceX * 32, resourceY * 32);
                NetworkSend.PlayAnimation(map, Resource.Instance[resourceIndex].Animation, resourceX, resourceY);

                return;
            }

            MapResource.Instance[map].ResourceData[resourceNum].State = 0; // Cut
            MapResource.Instance[map].ResourceData[resourceNum].Timer = General.GetTime();

            NetworkSend.MapResourceToMap(map);

            NetworkSend.ActionMessage(map, Resource.Instance[resourceIndex].SuccessMessage, (int)ColorName.BrightGreen, 1, GetPlayerX(playerId) * 32, GetPlayerY(playerId) * 32);
            Player.GiveInv(playerId, Resource.Instance[resourceIndex].ItemReward, 1);
            NetworkSend.PlayAnimation(map, Resource.Instance[resourceIndex].Animation, resourceX, resourceY);

            SetPlayerGatherSkillExperience(playerId, resourceType, GetPlayerGatherSkillExperience(playerId, resourceType) + Resource.Instance[resourceIndex].ExperienceReward);

            NetworkSend.PlayerMessage(playerId, $"Your {GetResourceSkillName((ResourceSkill)resourceType)} has earned {Resource.Instance[resourceIndex].ExperienceReward} experience. ({GetPlayerGatherSkillExperience(playerId, resourceType)}/{GetPlayerGatherSkillMaxExperience(playerId, resourceType)})", (int)ColorName.BrightGreen);
            NetworkSend.PlayerData(playerId);

            MapResource.OnLevel(playerId, resourceType);
        }
    }
}
