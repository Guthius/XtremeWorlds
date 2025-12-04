using Core.Globals;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    public class Job : IContent
    {
        public Data Data { get; set; } = Data.Job;
        
        public void OnReset()
        {
            for (int i = 0; i < Variables.MaxJobs; i++)
                OnClear(i);
        }

        public void OnClear(int index)
        {
            var statCount = System.Enum.GetValues(typeof(Stat)).Length;
            Data.Job[index] = default;
            Data.Job[index].Stat = new int[statCount];
            Data.Job[index].Name = "";
            Data.Job[index].Desc = "";
            Data.Job[index].StartItem = new int[Variables.MaxStartItems];
            Data.Job[index].StartValue = new int[Variables.MaxStartItems];
            Data.Job[index].StartSkill = new int[Variables.MaxStartSkills];
            Data.Job[index].MaleSprite = 1;
            Data.Job[index].FemaleSprite = 1;
            for (int i = 0; i < Variables.MaxStartItems; i++)
            {
                Data.Job[index].StartItem[i] = -1;
                Data.Job[index].StartValue[i] = 0;
            }
            for (int i = 0; i < Variables.MaxStartSkills; i++)
            {
                Data.Job[index].StartSkill[i] = -1;
            }
        }
    }
}
