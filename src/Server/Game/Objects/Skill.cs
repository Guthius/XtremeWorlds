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
        private static void EnsureSize(int size)
        {
            if (size <= 0)
            {
                return;
            }

            if (Skill.Instance.Count >= size)
            {
                return;
            }

            lock (Skill.Instance)
            {
                while (Skill.Instance.Count < size)
                {
                    Skill.Instance.Add(new Skill());
                }
            }
        }

        public static Task OnLoadAllAsync()
        {
            EnsureSize(Core.Globals.Variables.MaxSkills);
            return Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxSkills), OnLoadAsync);
        }

        public static void OnSave(int index)
        {
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
            EnsureSize(Core.Globals.Variables.MaxSkills);
            var data = await Database.SelectRowAsync(index, "skill", "data");
            if (data is null)
            {
                OnClear(index);
                return;
            }

            var skillData = JObject.FromObject(data).ToObject<Skill>();

            EnsureSize(index + 1);
            Skill.Instance[index] = skillData ?? new Skill();
        }
    }
}
