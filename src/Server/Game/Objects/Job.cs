using Core.Globals;
using Core.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class Job : IData, IAsyncData
    {
        public static void OnClear(int index)
        {
            int statCount = Enum.GetValues(typeof(Stat)).Length;
            Data.Job[index].Stat = new int[statCount];
            Data.Job[index].StartItem = new int[Core.Globals.Variables.MaxStartItems];
            Data.Job[index].StartValue = new int[Core.Globals.Variables.MaxStartItems];
            Data.Job[index].StartSkill = new int[Core.Globals.Variables.MaxStartSkills];

            Data.Job[index].Name = "";
            Data.Job[index].Desc = "";
            Data.Job[index].StartMap = 1;
            Data.Job[index].MaleSprite = 0;
            Data.Job[index].FemaleSprite = 0;

            for (int i = 0; i < Core.Globals.Variables.MaxStartItems; i++)
            {
                Data.Job[index].StartItem[i] = -1;
                Data.Job[index].StartValue[i] = 0;
            }

            for (int i = 0; i < Core.Globals.Variables.MaxStartItems; i++)
            {
                Data.Job[index].StartSkill[i] = -1;
            }
        }

        public static Task OnLoadAllAsync()
        {
            return Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxJobs), OnLoadAsync);
        }

        public static void OnSave(int index)
        {
            string json = JsonConvert.SerializeObject(Data.Job[index]).ToString();

            if (Database.RowExists(index, "job"))
            {
                Database.UpdateRow(index, json, "job", "data");
            }
            else
            {
                Database.InsertRow(index, json, "job");
            }
        }

        public static void OnDraw(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnStream(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnReset()
        {
            throw new NotImplementedException();
        }

        public static void OnLoad(int index)
        {
            throw new NotImplementedException();
        }

        public static async ValueTask OnLoadAsync(int index, CancellationToken cancellationToken)
        {
            JObject data;

            data = await Database.SelectRowAsync(index, "job", "data");
            if (data is null)
            {
                OnClear(index);
                return;
            }

            var jobData = JObject.FromObject(data).ToObject<Core.Globals.Type.Job>();
            Data.Job[index] = jobData;
        }
    }
}
