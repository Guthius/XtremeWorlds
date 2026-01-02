using Core;
using Core.Globals;
using Core.Interfaces;
using Core.Net;
using Core.Objects;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server.Game;
using Server.Game.Net;
using Server.Net;
using static Core.Globals.Commands;
using static Core.Net.Packets;
using Type = Core.Globals.Type;

namespace Server;

public class Resource : ResourceBase, IAsyncData
{
    private static void EnsureSize(int size)
    {
        if (size <= 0)
        {
            return;
        }

        if (Resource.Instance.Count >= size)
        {
            return;
        }

        lock (Resource.Instance)
        {
            while (Resource.Instance.Count < size)
            {
                Resource.Instance.Add(new Resource());
            }
        }
    }

    public static void OnSave(int index)
    {
        var json = JsonConvert.SerializeObject(Resource.Instance[index]);

        if (Database.RowExists(index, "resource"))
        {
            Database.UpdateRow(index, json, "resource", "data");
        }
        else
        {
            Database.InsertRow(index, json, "resource");
        }
    }

    public static async System.Threading.Tasks.Task OnLoadAllAsync()
    {
        EnsureSize(Core.Globals.Variables.MaxResources);
        await Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxResources), Resource.OnLoadAsync);
    }

    public static async ValueTask OnLoadAsync(int index, CancellationToken cancellationToken)
    {
        EnsureSize(Core.Globals.Variables.MaxResources);
        var data = await Database.SelectRowAsync(index, "resource", "data");
        if (data is null)
        {
            OnClear(index);
            return;
        }

        var resourceData = JObject.FromObject(data).ToObject<Resource>();

        EnsureSize(index + 1);
        Resource.Instance[index] = resourceData ?? new Resource();
    }
}