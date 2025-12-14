using Core.Globals;
using Core.Interfaces;
using Core.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Server;

public class Shop : ShopBase, IAsyncData
{
    public static async Task OnLoadAllAsync()
    {
        await Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxShops), OnLoadAsync);
    }

    public static async ValueTask OnLoadAsync(int index, CancellationToken cancellationToken)
    {
        JObject data;

        data = await Database.SelectRowAsync(index, "shop", "data");

        if (data is null)
        {
            OnClear(index);
            return;
        }

        var shopData = JObject.FromObject(data).ToObject<Shop>();

        Shop.Instance.Add(shopData ?? new Shop());
    }

    public static void OnSave(int index)
    {
        string json = JsonConvert.SerializeObject(Shop.Instance[index]);

        if (Database.RowExists(index, "shop"))
        {
            Database.UpdateRow(index, json, "shop", "data");
        }
        else
        {
            Database.InsertRow(index, json, "shop");
        }
    }
}