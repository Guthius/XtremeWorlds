using Core;
using Core.Globals;
using Core.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server.Game;
using Server.Game.Net;
using Server.Net;
using static Core.Globals.Command;
using static Core.Net.Packets;
using Type = Core.Globals.Type;

namespace Server;

public static class Resource
{
    public static void Save(int resourceNum)
    {
        var json = JsonConvert.SerializeObject(Data.Resource[resourceNum]);

        if (Database.RowExists(resourceNum, "resource"))
        {
            Database.UpdateRow(resourceNum, json, "resource", "data");
        }
        else
        {
            Database.InsertRow(resourceNum, json, "resource");
        }
    }

    public static async System.Threading.Tasks.Task OnLoadAllAsync()
    {
        await Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxResources), Resource.OnLoadAsync);
    }

    public static async ValueTask OnLoadAsync(int resourceNum, CancellationToken cancellationToken)
    {
        var data = await Database.SelectRowAsync(resourceNum, "resource", "data");
        if (data is null)
        {
            Clear(resourceNum);
            return;
        }

        var resourceData = JObject.FromObject(data).ToObject<Type.Resource>();

        Data.Resource[resourceNum] = resourceData;
    }

    public static void Clear(int resourceNum)
    {
        Data.Resource[resourceNum].Name = "";
        Data.Resource[resourceNum].EmptyMessage = "";
        Data.Resource[resourceNum].SuccessMessage = "";
    }
}