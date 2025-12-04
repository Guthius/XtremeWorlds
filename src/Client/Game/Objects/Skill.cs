using Client;
using Client.Net;
using Core.Globals;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    public class Skill : Content
    {
        public Data Data { get; set; } = Data.Skill;

        public void OnReset()
        {
            int i;

            for (i = 0; i < Variables.MaxSkills; i++)
                OnClear(i);

        }

        public void OnClear(int index)
        {
            Data.Skill[index] = default;
            Data.Skill[index].Name = "";
            Data.Skill[index].JobReq = -1;
            Data.Skill[index].SkillAnim = -1;
            GameState.SkillLoaded[index] = 0;
        }

        public void OnStream(int index)
        {
            if (index >= 0 && string.IsNullOrEmpty(Data.Skill[index].Name) && GameState.SkillLoaded[index] == 0)
            {
                GameState.SkillLoaded[index] = 1;
                Sender.SendRequestSkill(index);
            }
        }
    }
}
