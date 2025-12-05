using Core.Globals;
using Core.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server;

public class Shop : IData, IAsyncData
{
    public static async System.Threading.Tasks.Task OnLoadAllAsync()
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

        Core.Globals.Type.Shop shopData = JObject.FromObject(data).ToObject<Core.Globals.Type.Shop>();
        Data.Shop[index] = shopData;
    }

    public static void OnSave(int index)
    {
        string json = JsonConvert.SerializeObject(Data.Shop[index]).ToString();

        if (Database.RowExists(index, "shop"))
        {
            Database.UpdateRow(index, json, "shop", "data");
        }
        else
        {
            Database.InsertRow(index, json, "shop");
        }
    }

    public static void OnClear(int index)
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

    public static void OnReset()
    {
        for (int i = 0, loopTo = Core.Globals.Variables.MaxShops; i < loopTo; i++)
            OnClear(i);
    }

    public static void OnDraw(int index)
    {
        throw new NotImplementedException();
    }

    public static void OnStream(int index)
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