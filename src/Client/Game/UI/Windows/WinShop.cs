using Client.Net;
using Core.Globals;
using System.IO;
using static Core.Globals.Commands;

namespace Client.Game.UI.Windows;

public class WinShop
{
    public static void OnDraw()
    {
        var winShop = WindowManager.GetWindowByName("winShop");
        if (winShop is null)
        {
            return;
        }

        if (GameState.InShop < 0 || GameState.InShop > Variables.MaxShops)
        {
            return;
        }

        Shop.OnStream(GameState.InShop);

        if (GameState.ShopIsSelling)
        {
            DrawSelling(winShop);
        }
        else
        {
            DrawBuying(winShop);
        }
    }

    public static void OnDrawBackground()
    {
        var winShop = WindowManager.GetWindowByName("winShop");
        if (winShop is null)
        {
            return;
        }

        var xo = winShop.X;
        var yo = winShop.Y;
        var width = winShop.Width;
        var height = winShop.Height;

        // render green
        var argPath = Path.Combine(DataPath.Gui, "34");

        GameClient.RenderTexture(ref argPath, xo + 4, yo + 23, 0, 0, width - 8, height - 27, 4, 4);

        width = 76;
        height = 76;

        var y = yo + 23;
        for (var i = 0; i < 3; i++)
        {
            if (i == 3)
            {
                height = 42;
            }

            var argPath1 = Path.Combine(DataPath.Gui, "35");

            GameClient.RenderTexture(ref argPath1, xo + 4, y, 0, 0, width, height, width, height);
            GameClient.RenderTexture(ref argPath1, xo + 80, y, 0, 0, width, height, width, height);
            GameClient.RenderTexture(ref argPath1, xo + 156, y, 0, 0, width, height, width, height);
            GameClient.RenderTexture(ref argPath1, xo + 232, y, 0, 0, 42, height, 42, height);

            y += 76;
        }

        var argPath5 = Path.Combine(DataPath.Gui, "1");

        GameClient.RenderTexture(ref argPath5, xo + 4, y - 34, 0, 0, 270, 72, 270, 72);
    }

    public static void OnClose()
    {
        Shop.OnClose();
    }

    public static void OnBuyingChecked()
    {
        var winShop = WindowManager.GetWindowByName("winShop");
        if (winShop is null)
        {
            return;
        }

        var checkBoxBuying = winShop.GetChild("CheckboxBuying");
        var checkBoxSelling = winShop.GetChild("CheckboxSelling");

        if (checkBoxBuying.Value == 0)
        {
            checkBoxSelling.Value = 0;
        }
        else
        {
            checkBoxSelling.Value = 0;
            checkBoxBuying.Value = 0;
            return;
        }

        var buttonBuy = winShop.GetChild("btnBuy");
        var buttonSell = winShop.GetChild("btnSell");

        buttonSell.Visible = false;
        buttonBuy.Visible = true;

        GameState.ShopIsSelling = false;
        GameState.ShopSelectedSlot = 0;

        UpdateShop();
    }

    public static void OnSellingChecked()
    {
        var winShop = WindowManager.GetWindowByName("winShop");
        if (winShop is null)
        {
            return;
        }

        var checkBoxBuying = winShop.GetChild("CheckboxBuying");
        var checkBoxSelling = winShop.GetChild("CheckboxSelling");

        if (checkBoxSelling.Value == 0)
        {
            checkBoxBuying.Value = 0;
        }
        else
        {
            checkBoxBuying.Value = 0;
            checkBoxSelling.Value = 0;
            return;
        }

        var buttonBuy = winShop.GetChild("btnBuy");
        var buttonSell = winShop.GetChild("btnSell");

        buttonBuy.Visible = false;
        buttonSell.Visible = true;

        GameState.ShopIsSelling = true;
        GameState.ShopSelectedSlot = 0;

        UpdateShop();
    }

    public static void OnBuy()
    {
        Sender.SendBuyItem(GameState.ShopSelectedSlot);
    }

    public static void OnSell()
    {
        Sender.SendSellItem(GameState.ShopSelectedSlot);
    }

    public static void OnMouseDown()
    {
        var winShop = WindowManager.GetWindowByName("winShop");
        if (winShop is null)
        {
            return;
        }

        var slot = General.IsShop(winShop.X, winShop.Y);
        if (slot >= 0)
        {
            if (GameState.ShopIsSelling)
            {
                if (GetPlayerInventory(GameState.MyIndex, slot) >= 0)
                {
                    GameState.ShopSelectedSlot = slot;

                    UpdateShop();
                }
            }
            else
            {
                if (Shop.Instance[GameState.InShop].TradeItem[slot].Item >= 0)
                {
                    GameState.ShopSelectedSlot = slot;

                    UpdateShop();
                }
            }
        }

        OnMouseMove();
    }

    public static void OnMouseMove()
    {
        var winShop = WindowManager.GetWindowByName("winShop");
        if (winShop is null)
        {
            return;
        }

        var winDescription = WindowManager.GetWindowByName("winDescription");
        if (winDescription is null)
        {
            return;
        }

        if (GameState.InShop < 0 || GameState.InShop > Variables.MaxShops)
        {
            return;
        }

        var slot = General.IsShop(winShop.X, winShop.Y);
        if (slot < 0)
        {
            winDescription.Visible = false;
            return;
        }

        var x = winShop.X - winDescription.Width;
        if (x < 0)
        {
            x = winShop.X + winShop.Width;
        }

        var y = winShop.Y - 6;

        var itemNum = !GameState.ShopIsSelling
            ? Shop.Instance[GameState.InShop].TradeItem[slot].Item
            : GetPlayerInventory(GameState.MyIndex, slot);

        if (itemNum == -1)
        {
            return;
        }

        GameLogic.ShowShopDesc(x, y, itemNum);
    }

    public static void UpdateShop()
    {
        var winShop = WindowManager.GetWindowByName("winShop");
        if (winShop is null)
        {
            return;
        }

        if (GameState.InShop < 0)
        {
            return;
        }

        var labelName = winShop.GetChild("lblName");
        var labelCost = winShop.GetChild("lblCost");

        var picItem = winShop.GetChild("picItem");

        if (!GameState.ShopIsSelling)
        {
            GameState.ShopSelectedItem = Shop.Instance[GameState.InShop].TradeItem[GameState.ShopSelectedSlot].Item;
            if (GameState.ShopSelectedItem >= 0)
            {
                labelName.Text = Item.Instance[GameState.ShopSelectedItem].Name;
                if (Shop.Instance[GameState.InShop].TradeItem[GameState.ShopSelectedSlot].CostItem == 0)
                {
                    labelCost.Text = Shop.Instance[GameState.InShop].TradeItem[GameState.ShopSelectedSlot].CostValue + "g";
                }
                else if (Shop.Instance[GameState.InShop].TradeItem[GameState.ShopSelectedSlot].CostValue == 1)
                {
                    labelCost.Text = Item.Instance[Shop.Instance[GameState.InShop].TradeItem[GameState.ShopSelectedSlot].CostItem].Name;
                }
                else
                {
                    labelCost.Text = Shop.Instance[GameState.InShop].TradeItem[GameState.ShopSelectedSlot].CostValue + " " + Item.Instance[Shop.Instance[GameState.InShop].TradeItem[GameState.ShopSelectedSlot].CostItem].Name;
                }

                picItem.Image = Item.Instance[GameState.ShopSelectedItem].Icon;

                for (var i = 0; i < 5; i++)
                {
                    picItem.Texture[i] = DataPath.Items;
                }
            }
            else
            {
                labelName.Text = "Empty Slot";
                labelCost.Text = "";

                picItem.Image = null;

                for (var i = 0; i < 5; i++)
                {
                    picItem.Texture[i] = string.Empty;
                }
            }
        }
        else
        {
            GameState.ShopSelectedItem = GetPlayerInventory(GameState.MyIndex, GameState.ShopSelectedSlot);

            if (GameState.ShopSelectedItem >= 0)
            {
                var cost = (long) Math.Round(Item.Instance[GameState.ShopSelectedItem].Price / 100d * Shop.Instance[GameState.InShop].BuyRate);

                labelName.Text = Item.Instance[GameState.ShopSelectedItem].Name;
                labelCost.Text = cost + "g";

                picItem.Image = Item.Instance[GameState.ShopSelectedItem].Icon;
                for (var i = 0; i < 5; i++)
                {
                    picItem.Texture[i] = DataPath.Items;
                }
            }
            else
            {
                labelName.Text = "Empty Slot";
                labelCost.Text = "";

                picItem.Image = null;
                for (var i = 0; i < 5; i++)
                {
                    picItem.Texture[i] = string.Empty;
                }
            }
        }
    }

    private static void DrawBuying(Window winShop)
    {
        // NOTE: Buying grid previously had X/Y swapped (using winShop.Y for X and winShop.X for Y) which caused
        // the selection highlight and icons to render in incorrect positions after toggling buying mode.
        // Align with DrawSelling logic: left uses window X, top uses window Y.
        for (var i = 0; i < Variables.MaxTrades; i++)
        {
            var top = winShop.Y + GameState.ShopTop + (GameState.ShopOffsetY + 32) * (i / GameState.ShopColumns);
            var left = winShop.X + GameState.ShopLeft + (GameState.ShopOffsetX + 32) * (i % GameState.ShopColumns);

            if (GameState.ShopSelectedSlot == i)
            {
                var selectedSlotTexturePath = Path.Combine(DataPath.Gui, "61");
                GameClient.RenderTexture(ref selectedSlotTexturePath, left, top, 0, 0, 32, 32, 32, 32);
            }

            var itemNum = Shop.Instance[GameState.InShop].TradeItem[i].Item;
            if (itemNum < 0 || itemNum >= Core.Globals.Variables.MaxItems)
            {
                continue;
            }

            Item.OnStream(itemNum);

            var itemIcon = Item.Instance[itemNum].Icon;
            if (itemIcon <= 0 || itemIcon > GameState.NumItems)
            {
                continue;
            }

            var path = Path.Combine(DataPath.Items, itemIcon.ToString());
            GameClient.RenderTexture(ref path, left, top, 0, 0, 32, 32, 32, 32);
        }
    }

    private static void DrawSelling(Window winShop)
    {
        for (var i = 0; i < Variables.MaxTrades; i++)
        {
            var top = winShop.Y + GameState.ShopTop + (GameState.ShopOffsetY + 32) * (i / GameState.ShopColumns);
            var left = winShop.X + GameState.ShopLeft + (GameState.ShopOffsetX + 32) * (i % GameState.ShopColumns);

            if (GameState.ShopSelectedSlot == i)
            {
                var selectedSlotTexturePath = Path.Combine(DataPath.Gui, "61");

                GameClient.RenderTexture(ref selectedSlotTexturePath, left, top, 0, 0, 32, 32, 32, 32);
            }

            var itemNum = GetPlayerInventory(GameState.MyIndex, i);
            if (itemNum < 0 || itemNum >= Core.Globals.Variables.MaxItems)
            {
                continue;
            }

            Item.OnStream(itemNum);

            var itemIcon = Item.Instance[itemNum].Icon;
            if (itemIcon <= 0 || itemIcon > GameState.NumItems)
            {
                continue;
            }

            var path = Path.Combine(DataPath.Items, itemIcon.ToString());

            GameClient.RenderTexture(ref path, left, top, 0, 0, 32, 32, 32, 32);

            if (GetPlayerInventoryValue(GameState.MyIndex, i) <= 1)
            {
                continue;
            }

            var y = top + 20;
            var x = left + 1;

            var amount = GetPlayerInventoryValue(GameState.MyIndex, i);
            var amountColor = TextRenderer.GetColorForAmount(amount);

            TextRenderer.OnDraw(GameLogic.ConvertCurrency(amount), x, y, amountColor, amountColor, winShop.Font);
        }
    }
}