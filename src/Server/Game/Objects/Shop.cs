using Core.Globals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server;

public static class Shop
{
    public static async System.Threading.Tasks.Task LoadAllAsync()
    {
        var tasks = Enumerable.Range(0, Core.Globals.Variables.MaxShops).Select(i => System.Threading.Tasks.Task.Run(() => LoadAsync(i)));
        await System.Threading.Tasks.Task.WhenAll(tasks);

    }

    public static async System.Threading.Tasks.Task LoadAsync(int shopNum)
    {
        JObject data;

        data = await Database.SelectRowAsync(shopNum, "shop", "data");

        if (data is null)
        {
            Clear(shopNum);
            return;
        }

        Core.Globals.Type.Shop shopData = JObject.FromObject(data).ToObject<Core.Globals.Type.Shop>();
        Data.Shop[shopNum] = shopData;
    }


    public static void Save(int shopNum)
    {
        string json = JsonConvert.SerializeObject(Data.Shop[shopNum]).ToString();

        if (Database.RowExists(shopNum, "shop"))
        {
            Database.UpdateRow(shopNum, json, "shop", "data");
        }
        else
        {
            Database.InsertRow(shopNum, json, "shop");
        }
    }

    public static void Load(int shopNum)
    {
        LoadAsync(shopNum);
    }

    public static void LoadAll()
    {
        int i;

        var loopTo = Core.Globals.Variables.MaxShops;
        for (i = 0; i < loopTo; i++)
            _ = LoadAsync(i);

    }

    public static void Clear(int index)
    {
        Data.Shop[index] = default;
        Data.Shop[index].Name = "";

        Data.Shop[index].TradeItem = new Core.Globals.Type.TradeItem[Core.Globals.Variables.MaxTrades];
        for (int i = 0, loopTo = Core.Globals.Variables.MaxTrades; i < loopTo; i++)
        {
            Data.Shop[index].TradeItem[i].Item = -1;
            Data.Shop[index].TradeItem[i].CostItem = -1;
        }
    }

}