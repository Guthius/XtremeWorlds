using Core.Globals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    internal class Job
    {

        public static void Clear(int jobNum)
        {
            int statCount = Enum.GetValues(typeof(Stat)).Length;
            Data.Job[jobNum].Stat = new int[statCount];
            Data.Job[jobNum].StartItem = new int[Core.Globals.Variables.MaxStartItems];
            Data.Job[jobNum].StartValue = new int[Core.Globals.Variables.MaxStartItems];
            Data.Job[jobNum].StartSkill = new int[Core.Globals.Variables.MaxStartSkills];

            Data.Job[jobNum].Name = "";
            Data.Job[jobNum].Desc = "";
            Data.Job[jobNum].StartMap = 1;
            Data.Job[jobNum].MaleSprite = 0;
            Data.Job[jobNum].FemaleSprite = 0;

            for (int i = 0; i < Core.Globals.Variables.MaxStartItems; i++)
            {
                Data.Job[jobNum].StartItem[i] = -1;
                Data.Job[jobNum].StartValue[i] = 0;
            }

            for (int i = 0; i < Core.Globals.Variables.MaxStartItems; i++)
            {
                Data.Job[jobNum].StartSkill[i] = -1;
            }
        }

        public static async System.Threading.Tasks.Task OnLoadAsync(int jobNum)
        {
            JObject data;

            data = await Database.SelectRowAsync(jobNum, "job", "data");

            if (data is null)
            {
                Clear(jobNum);
                return;
            }

            var jobData = JObject.FromObject(data).ToObject<Core.Globals.Type.Job>();
            Data.Job[jobNum] = jobData;
        }

        public static async System.Threading.Tasks.Task OnLoadAllAsync()
        {
            var tasks = Enumerable.Range(0, Core.Globals.Variables.MaxJobs).Select(i => System.Threading.Tasks.Task.Run(() => OnLoadAsync(i)));
            await System.Threading.Tasks.Task.WhenAll(tasks);
        }

        public static void Save(int jobNum)
        {
            string json = JsonConvert.SerializeObject(Data.Job[jobNum]).ToString();

            if (Database.RowExists(jobNum, "job"))
            {
                Database.UpdateRow(jobNum, json, "job", "data");
            }
            else
            {
                Database.InsertRow(jobNum, json, "job");
            }
        }
    }
}
