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
        await Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxResources), Resource.OnLoadAsync);
    }

    public static async ValueTask OnLoadAsync(int index, CancellationToken cancellationToken)
    {
        var data = await Database.SelectRowAsync(index, "resource", "data");
        if (data is null)
        {
            OnClear(index);
            return;
        }

        var resourceData = JObject.FromObject(data).ToObject<Resource>();

        Resource.Instance.Add(resourceData ?? new Resource());
    }
}