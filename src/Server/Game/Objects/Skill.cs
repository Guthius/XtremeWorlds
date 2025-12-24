using Core.Globals;
using Core.Interfaces;
using Core.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class Skill : SkillBase, IAsyncData
    {
        public static Task OnLoadAllAsync()
        {
            EnsureSize(Core.Globals.Variables.MaxSkills);
            return Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxSkills), OnLoadAsync);
        }

        public new static void OnSave(int index)
        {
            if (index < 0 || index >= Core.Globals.Variables.MaxSkills)
            {
                return;
            }

            EnsureSize(index + 1);
            SyncToData(index);

            string json = JsonConvert.SerializeObject(Skill.Instance[index]).ToString();

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
            var data = await Database.SelectRowAsync(index, "skill", "data");
            if (data is null)
            {
                OnClear(index);
                return;
            }

            EnsureSize(index + 1);

            var skillData = JObject.FromObject(data).ToObject<Skill>();
            Skill.Instance[index] = skillData ?? new Skill();

            if (Skill.Instance[index].Name is null)
            {
                Skill.Instance[index].Name = string.Empty;
            }

            SyncToData(index);
        }
    }
}
