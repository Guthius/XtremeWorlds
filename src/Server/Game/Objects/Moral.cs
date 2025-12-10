using Core;
using Core.Globals;
using Core.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server.Game;
using Server.Game.Net;
using Server.Net;
using static Core.Net.Packets;
using static Core.Globals.Commands;
using Type = Core.Globals.Type;
using Core.Interfaces;
using Core.Objects;

namespace Server;

public class Moral : MoralBase, IData, IAsyncData
{
    public static async ValueTask OnLoadAsync(int index, CancellationToken cancellationToken)
    {
        var data = await Database.SelectRowAsync(index, "moral", "data");
        if (data is null)
        {
            OnClear(index);
            return;
        }

        var moralData = JObject.FromObject(data).ToObject<Moral>();

        Moral.Instance.Add(moralData ?? new Moral());
    }

    public static async System.Threading.Tasks.Task OnLoadAllAsync()
    {
        await Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxMorals), OnLoadAsync);
    }

    public static void OnSave(int index)
    {
        var json = JsonConvert.SerializeObject(Moral.Instance[index]);

        if (Database.RowExists(index, "moral"))
        {
            Database.UpdateRow(index, json, "moral", "data");
        }
        else
        {
            Database.InsertRow(index, json, "moral");
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

    public static void OnUpdate(int index)
    {
        throw new NotImplementedException();
    }
}