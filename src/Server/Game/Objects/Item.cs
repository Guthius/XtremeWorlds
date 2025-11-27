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

public static class Item
{
    public static void Save(int itemNum)
    {
        var json = JsonConvert.SerializeObject(Data.Item[itemNum]);

        if (Database.RowExists(itemNum, "item"))
        {
            Database.UpdateRow(itemNum, json, "item", "data");
        }
        else
        {
            Database.InsertRow(itemNum, json, "item");
        }
    }

    public static async Task LoadAllAsync()
    {
        await Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxItems), LoadAsync);
    }

    private static async ValueTask LoadAsync(int itemNum, CancellationToken cancellationToken)
    {
        var data = await Database.SelectRowAsync(itemNum, "item", "data");
        if (data is null)
        {
            Clear(itemNum);
            return;
        }

        var itemData = JObject.FromObject(data).ToObject<Type.Item>();

        Data.Item[itemNum] = itemData;
    }

    private static void Clear(int itemNum)
    {
        Data.Item[itemNum].Name = "";
        Data.Item[itemNum].Description = "";
        Data.Item[itemNum].Ammo = -1;
        Data.Item[itemNum].Stackable = 1;
    }
}