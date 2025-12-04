using Core.Globals;
using Core.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class Skill : IData, IAsyncData
    {
        public static Task OnLoadAllAsync()
        {
            return Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxSkills), OnLoadAsync);
        }

        public static void OnSave(int index)
        {
            string json = JsonConvert.SerializeObject(Data.Skill[index]).ToString();

            if (Database.RowExists(index, "skill"))
            {
                Database.UpdateRow(index, json, "skill", "data");
            }
            else
            {
                Database.InsertRow(index, json, "skill");
            }
        }

        public static async ValueTask OnLoadAsync(int index, System.Threading.CancellationToken cancellationToken)
        {
            JObject data;

            data = await Database.SelectRowAsync(index, "skill", "data");
            if (data is null)
            {
                OnClear(index);
                return;
            }

            var skillData = JObject.FromObject(data).ToObject<Core.Globals.Type.Skill>();
            Data.Skill[index] = skillData;
        }

        public static void OnClear(int index)
        {
            Data.Skill[index].Name = "";
            Data.Skill[index].LevelReq = 0;
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
    }
}
