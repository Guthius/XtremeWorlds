using Client;
using Client.Net;
using Core.Globals;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    public class Skill : IData
    {

        public static void OnReset()
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

        public static void OnDraw(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnLoad(int index)
        {
            throw new NotImplementedException();
        }
    }
}
