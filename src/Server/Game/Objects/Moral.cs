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

namespace Server;

public static class Moral
{
    private static void Clear(int moralNum)
    {
        Data.Moral[moralNum].Name = "";
        Data.Moral[moralNum].Color = 0;
        Data.Moral[moralNum].CanCast = false;
        Data.Moral[moralNum].CanDropItem = false;
        Data.Moral[moralNum].CanPk = false;
        Data.Moral[moralNum].CanPickupItem = false;
        Data.Moral[moralNum].CanUseItem = false;
        Data.Moral[moralNum].DropItems = false;
        Data.Moral[moralNum].LoseExp = false;
        Data.Moral[moralNum].NpcBlock = false;
        Data.Moral[moralNum].PlayerBlock = false;
    }

    private static async ValueTask OnLoadAsync(int moralNum, CancellationToken cancellationToken)
    {
        var data = await Database.SelectRowAsync(moralNum, "moral", "data");
        if (data is null)
        {
            Clear(moralNum);
            return;
        }

        var moralData = JObject.FromObject(data).ToObject<Type.Moral>();

        Data.Moral[moralNum] = moralData;
    }

    public static async Task OnLoadAllAsync()
    {
        await Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxMorals), OnLoadAsync);
    }

    public static void Save(int moralNum)
    {
        var json = JsonConvert.SerializeObject(Data.Moral[moralNum]);

        if (Database.RowExists(moralNum, "moral"))
        {
            Database.UpdateRow(moralNum, json, "moral", "data");
        }
        else
        {
            Database.InsertRow(moralNum, json, "moral");
        }
    }
}