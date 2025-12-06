using Core;
using Core.Globals;
using Core.Interfaces;
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
using Core.Objects;

namespace Server;

public class Item : ItemBase, IData, IAsyncData
{
    public static void OnSave(int index)
    {
        var json = JsonConvert.SerializeObject(Item.Instance[index]);

        if (Database.RowExists(index, "item"))
        {
            Database.UpdateRow(index, json, "item", "data");
        }
        else
        {
            Database.InsertRow(index, json, "item");
        }
    }

    public static Task OnLoadAllAsync()
    {
        return Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxItems), OnLoadAsync);
    }

    public static async ValueTask OnLoadAsync(int index, CancellationToken cancellationToken)
    {
        var data = await Database.SelectRowAsync(index, "item", "data");
        if (data is null)
        {
            OnClear(index);
            return;
        }

        var itemData = JObject.FromObject(data).ToObject<Item>();

        Item.Instance.Add(itemData ?? new Item());
    }

    public static void OnClear(int index)
    {
        if (Item.Instance.Count <= index)
            Item.Instance.Add(new Item());
        else
            Item.Instance[index] = new Item();
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