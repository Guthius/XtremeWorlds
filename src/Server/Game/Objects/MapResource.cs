using Core.Globals;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public static class MapResource
    {

        public static void Cache(int mapNum)
        {
            var resourceCount = 0;

            for (var x = 0; x < Data.Map[mapNum].MaxX; x++)
            {
                for (var y = 0; y < Data.Map[mapNum].MaxY; y++)
                {
                    if (Data.Map[mapNum].Tile[x, y].Type != TileType.Resource &&
                        Data.Map[mapNum].Tile[x, y].Type2 != TileType.Resource)
                    {
                        continue;
                    }

                    resourceCount++;

                    Array.Resize(ref Data.MapResource[mapNum].ResourceData, resourceCount);

                    Data.MapResource[mapNum].ResourceData[resourceCount - 1].X = x;
                    Data.MapResource[mapNum].ResourceData[resourceCount - 1].Y = y;
                    Data.MapResource[mapNum].ResourceData[resourceCount - 1].Health = (byte)Data.Resource[Data.Map[mapNum].Tile[x, y].Data1].Health;
                }
            }

            Data.MapResource[mapNum].ResourceCount = resourceCount;
        }

        public static void CheckLevelUp(int playerId, int skillSlot)
        {
            var levels = 0;

            if (Command.GetPlayerGatherSkillLevel(playerId, skillSlot) == Script.Instance?.MaxLevel)
            {
                return;
            }

            while (Command.GetPlayerGatherSkillExp(playerId, skillSlot) >= Command.GetPlayerGatherSkillMaxExp(playerId, skillSlot))
            {
                var expRollover = Command.GetPlayerGatherSkillExp(playerId, skillSlot) - Command.GetPlayerGatherSkillMaxExp(playerId, skillSlot);

                Command.SetPlayerGatherSkillLevel(playerId, skillSlot, Command.GetPlayerGatherSkillLevel(playerId, skillSlot) + 1);
                Command.SetPlayerGatherSkillExp(playerId, skillSlot, expRollover);
                Command.SetPlayerGatherSkillMaxExp(playerId, skillSlot, (int)Command.GetSkillNextLevel(playerId, skillSlot));

                levels++;
            }

            if (levels == 0)
            {
                return;
            }

            NetworkSend.PlayerMsg(playerId, levels == 1
                ? $"Your {Command.GetResourceSkillName((ResourceSkill)skillSlot)} has gone up a level!"
                : $"Your {Command.GetResourceSkillName((ResourceSkill)skillSlot)} has gone up by {levels} levels!", (int)ColorName.BrightGreen);

            NetworkSend.SendPlayerData(playerId);
        }
    }
}
