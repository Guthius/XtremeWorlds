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
    public class Job : JobBase, IAsyncData
    {
        private static void EnsureSize(int size)
        {
            if (size <= 0)
            {
                return;
            }

            if (Job.Instance.Count >= size)
            {
                return;
            }

            lock (Job.Instance)
            {
                while (Job.Instance.Count < size)
                {
                    Job.Instance.Add(new Job());
                }
            }
        }

        public static Task OnLoadAllAsync()
        {
            EnsureSize(Core.Globals.Variables.MaxJobs);
            return Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxJobs), OnLoadAsync);
        }

        public static void OnSave(int index)
        {
            string json = JsonConvert.SerializeObject(Job.Instance[index]).ToString();

            if (Database.RowExists(index, "job"))
            {
                Database.UpdateRow(index, json, "job", "data");
            }
            else
            {
                Database.InsertRow(index, json, "job");
            }
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

            var jobData = JObject.FromObject(data).ToObject<Job>();

            EnsureSize(index + 1);
            Job.Instance[index] = jobData ?? new Job();
        }
    }
}
