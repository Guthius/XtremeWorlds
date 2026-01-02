using Core.Globals;

namespace Core.Objects;

public class ShopBase
{
    public static bool[] IsChanged { get; set; } = new bool[Variables.MaxShops];

    public string Name { get; set; } = "";
    public int BuyRate { get; set; }
    public Core.Globals.Type.TradeItem[] TradeItem { get; set; }

    public static List<ShopBase> Instance { get; private set; } = new();

    public ShopBase()
    {
        TradeItem = new Core.Globals.Type.TradeItem[Variables.MaxTrades];
        for (var i = 0; i < Variables.MaxTrades; i++)
        {
            TradeItem[i].Item = -1;
            TradeItem[i].CostItem = -1;
        }
    }

    public static void OnClearChanged()
    {
        IsChanged = new bool[Variables.MaxShops];
    }

    public static void OnClear(int index)
    {
        if (Instance.Count > index)
            Instance[index] = new ShopBase();
    }

    public static void OnReset()
    {
        for (var i = 0; i < Variables.MaxShops; i++)
        {
            OnClear(i);
        }
    }

}
