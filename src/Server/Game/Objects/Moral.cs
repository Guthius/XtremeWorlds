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
using static Core.Globals.Command;
using Type = Core.Globals.Type;
using Core.Interfaces;

namespace Server;

public class Moral : IData, IAsyncData
{
    public static void OnClear(int index)
    {
        Data.Moral[index].Name = "";
        Data.Moral[index].Color = 0;
        Data.Moral[index].CanCast = false;
        Data.Moral[index].CanDropItem = false;
        Data.Moral[index].CanPk = false;
        Data.Moral[index].CanPickupItem = false;
        Data.Moral[index].CanUseItem = false;
        Data.Moral[index].DropItems = false;
        Data.Moral[index].LoseExp = false;
        Data.Moral[index].NpcBlock = false;
        Data.Moral[index].PlayerBlock = false;
    }

    public static async ValueTask OnLoadAsync(int index, CancellationToken cancellationToken)
    {
        var data = await Database.SelectRowAsync(index, "moral", "data");
        if (data is null)
        {
            OnClear(index);
            return;
        }

        var moralData = JObject.FromObject(data).ToObject<Type.Moral>();

        Data.Moral[index] = moralData;
    }

    public static async Task OnLoadAllAsync()
    {
        await Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxMorals), OnLoadAsync);
    }

    public static void OnSave(int index)
    {
        var json = JsonConvert.SerializeObject(Data.Moral[index]);

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
}