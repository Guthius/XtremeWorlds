using Client;
using Client.Net;
using Core.Globals;
using Core.Interfaces;
using Core.Objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    public class Skill : SkillBase, IStreamable
    {        
        public static void OnStream(int index)
        {
            if (index < 0 || index >= Variables.MaxSkills) return;
            if (Skill.Instance.Count <= index)
            {
                Sender.SendRequestSkill(index);
            }
        }
    }
}
