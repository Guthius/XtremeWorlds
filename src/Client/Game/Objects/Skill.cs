using Client;
using Client.Net;
using Core.Globals;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    public class Skill
    {

        public static void OnClearAll()
        {
            int i;

            for (i = 0; i < Variables.MaxSkills; i++)
                OnClear(i);

        }

        public static void OnClear(int index)
        {
            Data.Skill[index] = default;
            Data.Skill[index].Name = "";
            Data.Skill[index].JobReq = -1;
            Data.Skill[index].SkillAnim = -1;
            GameState.SkillLoaded[index] = 0;
        }

        public static void OnStream(int skillNum)
        {
            if (skillNum >= 0 && string.IsNullOrEmpty(Data.Skill[skillNum].Name) && GameState.SkillLoaded[skillNum] == 0)
            {
                GameState.SkillLoaded[skillNum] = 1;
                Sender.SendRequestSkill(skillNum);
            }
        }
    }
}
