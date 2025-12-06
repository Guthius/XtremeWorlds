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
        public static Task OnLoadAllAsync()
        {
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
            Job.Instance.Add(jobData ?? new Job());
        }
    }
}
