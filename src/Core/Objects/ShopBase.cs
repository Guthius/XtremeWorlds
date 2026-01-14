using Core.Globals;

namespace Core.Objects;

public class ShopBase
{
    public static bool[] IsStreaming { get; set; } = new bool[Core.Globals.Variables.MaxShops];
    public static bool[] IsChanged { get; set; } = new bool[Core.Globals.Variables.MaxShops];

    public string Name { get; set; } = "";
    public int BuyRate { get; set; }
    public Core.Globals.Type.TradeItem[] TradeItem { get; set; }

    public static List<ShopBase> Instance { get; private set; } = new();

    public ShopBase()
    {
        TradeItem = new Core.Globals.Type.TradeItem[Core.Globals.Variables.MaxTrades];
        for (var i = 0; i < Core.Globals.Variables.MaxTrades; i++)
        {
            TradeItem[i].Item = -1;
            TradeItem[i].CostItem = -1;
        }
    }

    public static void OnClearChanged()
    {
        IsChanged = new bool[Core.Globals.Variables.MaxShops];
        IsStreaming = new bool[Core.Globals.Variables.MaxShops];
    }

    public static void OnClear(int index)
    {
        if (Instance.Count > index)
            Instance[index] = new ShopBase();
        IsChanged[index] = false;
        IsStreaming[index] = false;
    }

    public static void OnClear()
    {
        for (var i = 0; i < Core.Globals.Variables.MaxShops; i++)
            OnClear(i);
    }
}
