using Core.Globals;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public static class MapNpc
    {
        public static void Clear(int index, int mapNum)
        {
            var count = Enum.GetValues(typeof(Vital)).Length;
            Data.MapNpc[mapNum].Npc[index].Vital = new int[count];
            Data.MapNpc[mapNum].Npc[index].SkillCd = new int[Core.Globals.Variables.MaxNpcSkills];
            Data.MapNpc[mapNum].Npc[index].Num = -1;
            Data.MapNpc[mapNum].Npc[index].SkillBuffer = -1;
        }
    }
}
