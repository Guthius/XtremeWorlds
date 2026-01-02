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

public class Moral : MoralBase, IAsyncData
{
    private static void EnsureSize(int size)
    {
        if (size <= 0)
        {
            return;
        }

        if (Moral.Instance.Count >= size)
        {
            return;
        }

        lock (Moral.Instance)
        {
            while (Moral.Instance.Count < size)
            {
                Moral.Instance.Add(new Moral());
            }
        }
    }

    public static async ValueTask OnLoadAsync(int index, CancellationToken cancellationToken)
    {
        EnsureSize(Core.Globals.Variables.MaxMorals);

        var data = await Database.SelectRowAsync(index, "moral", "data");
        if (data is null)
        {
            lock (Moral.Instance)
            {
                Moral.Instance[index] = new Moral();
            }
            return;
        }

        var moralData = JObject.FromObject(data).ToObject<Moral>();

        lock (Moral.Instance)
        {
            Moral.Instance[index] = moralData ?? new Moral();
        }
    }

    public static async System.Threading.Tasks.Task OnLoadAllAsync()
    {
        EnsureSize(Core.Globals.Variables.MaxMorals);
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
}