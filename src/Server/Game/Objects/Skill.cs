using Core.Globals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public static class Skill
    {
        public static async System.Threading.Tasks.Task LoadAllAsync()
        {
            var tasks = Enumerable.Range(0, Core.Globals.Variables.MaxSkills).Select(i => System.Threading.Tasks.Task.Run(() => LoadAsync(i)));
            await System.Threading.Tasks.Task.WhenAll(tasks);
        }

        public static void Save(int skillNum)
        {
            string json = JsonConvert.SerializeObject(Data.Skill[skillNum]).ToString();

            if (Database.RowExists(skillNum, "skill"))
            {
                Database.UpdateRow(skillNum, json, "skill", "data");
            }
            else
            {
                Database.InsertRow(skillNum, json, "skill");
            }
        }

        public static async System.Threading.Tasks.Task LoadAsync(int skillNum)
        {
            JObject data;

            data = await Database.SelectRowAsync(skillNum, "skill", "data");

            if (data is null)
            {
                Clear(skillNum);
                return;
            }

            var skillData = JObject.FromObject(data).ToObject<Core.Globals.Type.Skill>();
            Data.Skill[skillNum] = skillData;
        }

        public static void Clear(int index)
        {
            Data.Skill[index].Name = "";
            Data.Skill[index].LevelReq = 0;
        }

    }
}
