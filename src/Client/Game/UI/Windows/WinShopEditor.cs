using Client.Game.UI;
using Client.Game.UI.Controls;
using Core.Globals;
using System;
using System.IO;

namespace Client.Game.UI.Windows;

public static class WinShopEditor
{
    public static int SelectedIndex = 0;

    private static Core.Globals.Type.Shop? _history = null;

    public static void Init()
    {
        if (!WindowManager.TryGetControl("winShopEditor", "lstIndex", out _))
            return; // window not present yet

        SelectedIndex = Math.Clamp(SelectedIndex, 0, Variables.MaxShops - 1);
        RefreshList();
        PopulateCombos();
        OnLoad(SelectedIndex);
    }

    public static void OnListMouseDown()
    {
        if (!WindowManager.TryGetControl("winShopEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list) return;
        var win = WindowManager.GetWindowByName("winShopEditor");
        if (win is null) return;
        int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
        int index = list.GetItemIndexAtPosition(relY);
        if (index < 0 || index >= Variables.MaxShops) return;
        SelectedIndex = index;
        GameState.EditorIndex = index;
        list.SelectedIndex = index;
        list.EnsureVisible(index);
        OnLoad(index);
    }

    public static void OnTradeListMouseDown()
    {
        if (!WindowManager.TryGetControl("winShopEditor", "lstTradeItem", out var ctrl) || ctrl is not ListBox list) return;
        var win = WindowManager.GetWindowByName("winShopEditor");
        if (win is null) return;
        int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
        int index = list.GetItemIndexAtPosition(relY);
        if (index < 0 || index >= Variables.MaxTrades) return;
        list.SelectedIndex = index;
        list.EnsureVisible(index);
        LoadTradeFieldsForSelected();
    }

    public static void LoadTradeFieldsForSelected()
    {
        if (!WindowManager.TryGetControl("winShopEditor", "lstTradeItem", out var ctrl) || ctrl is not ListBox lst) return;
        int idx = lst.SelectedIndex;
        if (idx < 0 || SelectedIndex < 0 || SelectedIndex >= Variables.MaxShops) return;
        ref var trade = ref Data.Shop[SelectedIndex].TradeItem[idx];
        if (WindowManager.TryGetControl("winShopEditor", "cmbItem", out var itCtrl) && itCtrl is ComboBox ci)
            ci.Value = Math.Clamp(trade.Item, 0, Math.Max(0, ci.Items.Count - 1));
        if (WindowManager.TryGetControl("winShopEditor", "cmbCostItem", out var cCtrl) && cCtrl is ComboBox cc)
            cc.Value = Math.Clamp(trade.CostItem, 0, Math.Max(0, cc.Items.Count - 1));
        if (WindowManager.TryGetControl("winShopEditor", "txtItemValue", out var iqCtrl) && iqCtrl is TextBox txtIQ)
            txtIQ.Text = trade.ItemValue.ToString();
        if (WindowManager.TryGetControl("winShopEditor", "txtCostValue", out var cqCtrl) && cqCtrl is TextBox txtCQ)
            txtCQ.Text = trade.CostValue.ToString();
    }

    public static void RefreshList()
    {
        if (!WindowManager.TryGetControl("winShopEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list)
            return;

        int prevIndex = SelectedIndex;
        int prevScroll = list.ScrollOffset;

        list.Clear();
        for (int i = 0; i < Variables.MaxShops; i++)
        {
            string name = Strings.Trim(Data.Shop[i].Name);
            if (string.IsNullOrWhiteSpace(name)) name = "None";
            list.AddItem($"{i + 1}: {name}");
        }

        if (prevIndex >= 0 && prevIndex < list.Items.Count)
        {
            list.SelectedIndex = prevIndex;
            list.EnsureVisible(prevIndex);
        }

        if (WindowManager.TryGetControl("winShopEditor", "sldList", out var sldCtrl) && sldCtrl is ScrollBar sb)
        {
            int visible = list.GetVisibleCount();
            int max = Math.Max(0, list.Items.Count - visible);
            sb.Min = 0;
            sb.Max = max;
            sb.Value = Math.Clamp(prevScroll, sb.Min, sb.Max);
        }

        // sync trade scrollbar range too
        if (WindowManager.TryGetControl("winShopEditor", "sldTradeList", out var sldTradeCtrl) && sldTradeCtrl is ScrollBar sbTrade && WindowManager.TryGetControl("winShopEditor", "lstTradeItem", out var tCtrl) && tCtrl is ListBox tl)
        {
            int visibleT = tl.GetVisibleCount();
            int maxT = Math.Max(0, tl.Items.Count - visibleT);
            sbTrade.Min = 0;
            sbTrade.Max = maxT;
            sbTrade.Value = Math.Clamp(tl.ScrollOffset, sbTrade.Min, sbTrade.Max);
        }
    }

    public static void PopulateCombos()
    {
        if (WindowManager.TryGetControl("winShopEditor", "cmbItem", out var itemCtrl) && itemCtrl is ComboBox cmbItem)
        {
            cmbItem.Items.Clear();
            for (int i = 0; i < Variables.MaxItems; i++)
                cmbItem.Items.Add($"{i + 1}: {Data.Item[i].Name}");
            cmbItem.Value = 0;
        }
        if (WindowManager.TryGetControl("winShopEditor", "cmbCostItem", out var costCtrl) && costCtrl is ComboBox cmbCost)
        {
            cmbCost.Items.Clear();
            for (int i = 0; i < Variables.MaxItems; i++)
                cmbCost.Items.Add($"{i + 1}: {Data.Item[i].Name}");
            cmbCost.Value = 0;
        }
    }

    public static void OnLoad(int index)
    {
        if (index < 0 || index >= Variables.MaxShops) return;
        SelectedIndex = index;
        GameState.EditorIndex = index;
        var shop = Data.Shop[index];

        if (WindowManager.TryGetControl("winShopEditor", "txtName", out var nameCtrl) && nameCtrl is TextBox txtName)
            txtName.Text = shop.Name ?? string.Empty;
        if (WindowManager.TryGetControl("winShopEditor", "txtBuy", out var buyCtrl) && buyCtrl is TextBox txtBuy)
            txtBuy.Text = shop.BuyRate.ToString();

        if (WindowManager.TryGetControl("winShopEditor", "lstTradeItem", out var tradeCtrl) && tradeCtrl is ListBox tradeList)
        {
            tradeList.Clear();
            for (int i = 0; i < Variables.MaxTrades; i++)
            {
                ref var t = ref shop.TradeItem[i];
                string itemName = t.Item >= 0 ? Data.Item[t.Item].Name : "None";
                string costName = t.CostItem >= 0 ? Data.Item[t.CostItem].Name : "None";
                tradeList.AddItem($"{i + 1}: {itemName} x{t.ItemValue} for {costName} x{t.CostValue}");
            }
        }

        if (WindowManager.TryGetControl("winShopEditor", "cmbItem", out var itemCombo) && itemCombo is ComboBox cmbItem)
            cmbItem.Value = 0;
        if (WindowManager.TryGetControl("winShopEditor", "cmbCostItem", out var costCombo) && costCombo is ComboBox cmbCost)
            cmbCost.Value = 0;
        if (WindowManager.TryGetControl("winShopEditor", "txtItemValue", out var itemQtyCtrl) && itemQtyCtrl is TextBox txtIQ)
            txtIQ.Text = "0";
        if (WindowManager.TryGetControl("winShopEditor", "txtCostValue", out var costQtyCtrl) && costQtyCtrl is TextBox txtCQ)
            txtCQ.Text = "0";

        // Update trade scrollbar range
        if (WindowManager.TryGetControl("winShopEditor", "sldTradeList", out var sldTradeCtrl) && sldTradeCtrl is ScrollBar sbTrade && WindowManager.TryGetControl("winShopEditor", "lstTradeItem", out var tradeListCtrl) && tradeListCtrl is ListBox tl)
        {
            int visible = tl.GetVisibleCount();
            int max = Math.Max(0, tl.Items.Count - visible);
            sbTrade.Min = 0;
            sbTrade.Max = max;
            sbTrade.Value = Math.Clamp(tl.ScrollOffset, sbTrade.Min, sbTrade.Max);
        }
    }

    // Toggle Copy -> Paste on subsequent clicks. Paste overwrites current SelectedIndex.
    public static void OnCopyOrPaste()
    {
        if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxShops) return;
        if (_history is null)
        {
            // Copy current Shop (deep copy for arrays)
            var s = Data.Shop[SelectedIndex];
            var n = s; // struct copy
            if (s.TradeItem != null)
            {
                n.TradeItem = new Core.Globals.Type.TradeItem[s.TradeItem.Length];
                Array.Copy(s.TradeItem, n.TradeItem, s.TradeItem.Length);
            }
            _history = n;
            if (WindowManager.TryGetControl("winShopEditor", "btnCopy", out var btn)) btn.Text = "Paste";
            return;
        }

        // Paste clipboard into current index
        var pasted = _history.Value;
        Data.Shop[SelectedIndex] = pasted;
        GameState.ShopChanged[SelectedIndex] = true;
        // Refresh UI to reflect pasted data
        OnLoad(SelectedIndex);
        RefreshList();
        // Keep clipboard for further pastes; update button text accordingly
        if (WindowManager.TryGetControl("winShopEditor", "btnCopy", out var btn2)) btn2.Text = "Paste";
    }

    // Update name from text box (called by Crystalshire wiring).
    public static void UpdateName(string newName)
    {
        if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxShops) return;
        Data.Shop[SelectedIndex].Name = Strings.Trim(newName ?? string.Empty);
        GameState.ShopChanged[SelectedIndex] = true;
        RefreshList();
    }

    public static void OnDelete()
    {
        if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxShops) return;
        if (!WindowManager.TryGetControl("winShopEditor", "lstTradeItem", out var tradeListCtrl) || tradeListCtrl is not ListBox lst) return;
        int idx = lst.SelectedIndex;
        if (idx < 0 || idx >= Variables.MaxTrades) return;
        ref var trade = ref Data.Shop[SelectedIndex].TradeItem[idx];
        trade.Item = -1; trade.ItemValue = 0; trade.CostItem = -1; trade.CostValue = 0;
        GameState.ShopChanged[SelectedIndex] = true;
        OnLoad(SelectedIndex);
    }

    public static void OnClear()
    {
        Shop.OnClear(SelectedIndex);
        GameState.ShopChanged[SelectedIndex] = true;
        OnLoad(SelectedIndex);
        RefreshList();
    }

    public static void OnCopy()
    {
        OnCopyOrPaste();
    }

    public static void OnSave()
    {
        Editors.ShopEditorOK();
        WindowManager.HideWindow("winShopEditor");
    }

    public static void OnCancel()
    {
        Editors.ShopEditorCancel();
        WindowManager.HideWindow("winShopEditor");
    }
}
