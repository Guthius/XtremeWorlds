using Client.Net;
using Core.Globals;
using System.IO;
using static Core.Globals.Commands;

namespace Client.Game.UI.Windows;

public class WinInventory
{
    public static void OnDraw()
    {
        if (GameState.MyIndex < 0 || GameState.MyIndex > Variables.MaxPlayers)
        {
            return;
        }

        var winInventory = WindowManager.GetWindowByName("winInventory");
        if (winInventory is null)
        {
            return;
        }

        var xO = winInventory.X;
        var yO = winInventory.Y;
        var width = winInventory.Width;
        var height = winInventory.Height;

        // render green
        var greenTexturePath = Path.Combine(DataPath.Gui, "34");

        GameClient.RenderTexture(ref greenTexturePath, xO + 4, yO + 23, 0, 0, width - 8, height - 27, 4, 4);

        width = 76;
        height = 76;

        var y = yO + 23;

        for (var i = 0; i < 4; i++)
        {
            if (i == 3)
            {
                height = 38;
            }

            var slotTexturePath = Path.Combine(DataPath.Gui, "35");

            GameClient.RenderTexture(ref slotTexturePath, xO + 4, y, 0, 0, width, height, width, height);
            GameClient.RenderTexture(ref slotTexturePath, xO + 80, y, 0, 0, width, height, width, height);
            GameClient.RenderTexture(ref slotTexturePath, xO + 156, y, 0, 0, 42, height, 42, height);

            y += 76;
        }

        var woodTexturePath = Path.Combine(DataPath.Gui, "1");

        GameClient.RenderTexture(ref woodTexturePath, xO + 4, yO + 289, 100, 100, 194, 26, 194, 26);

        var skipItem = false;

        for (var slot = 0; slot < Variables.MaxInventory; slot++)
        {
            var item = GetPlayerInv(GameState.MyIndex, slot);
            if (item < 0 || item >= Core.Globals.Variables.MaxItems)
            {
                continue;
            }

            Item.OnStream(item);

            if (WindowManager.DragBox.Origin == PartOrigin.Inventory &&
                WindowManager.DragBox.Slot == slot)
            {
                continue;
            }

            if (Item.Instance.Count <= item)
            {
                continue;
            }

            var icon = Item.Instance[item].Icon;

            if (icon <= 0 || icon > GameState.NumItems)
            {
                return;
            }

            // exit out if we're offering item in a trade.
            var amountModifier = 0;
            if (Trade.InTrade > 0)
            {
                for (var tradeSlot = 0; tradeSlot < Variables.MaxInventory; tradeSlot++)
                {
                    if (Data.TradeYourOffer[tradeSlot].Num < 0)
                    {
                        continue;
                    }

                    if (Data.TradeYourOffer[tradeSlot].Num != slot)
                    {
                        continue;
                    }

                    var tempItem = GetPlayerInv(GameState.MyIndex, Data.TradeYourOffer[tradeSlot].Num);

                    if (Item.Instance[tempItem].Type != (byte) ItemCategory.Currency ||
                        Data.TradeYourOffer[tradeSlot].Value == GetPlayerInvValue(GameState.MyIndex, slot))
                    {
                        skipItem = true;
                    }
                    else
                    {
                        amountModifier = Data.TradeYourOffer[tradeSlot].Value;
                    }
                }
            }

            if (!skipItem)
            {
                if (icon > 0 && icon <= GameState.NumItems)
                {
                    var top = yO + GameState.InvTop + (GameState.InvOffsetY + 32) * (slot / GameState.InvColumns);
                    var left = xO + GameState.InvLeft + (GameState.InvOffsetX + 32) * (slot % GameState.InvColumns);

                    var iconPath = Path.Combine(DataPath.Items, icon.ToString());

                    GameClient.RenderTexture(ref iconPath, left, top, 0, 0, 32, 32, 32, 32);

                    if (GetPlayerInvValue(GameState.MyIndex, slot) > 1)
                    {
                        y = top + 20;

                        var x = left + 1;
                        var amount = GetPlayerInvValue(GameState.MyIndex, slot) - amountModifier;
                        var amountColor = TextRenderer.GetColorForAmount(amount);

                        TextRenderer.Render(GameLogic.ConvertCurrency(amount), x, y, amountColor, amountColor, winInventory.Font);
                    }
                }
            }

            skipItem = false;
        }
    }

    public static void OnMouseDown()
    {
        var winInventory = WindowManager.GetWindowByName("winInventory");
        if (winInventory is null)
        {
            return;
        }
        var slot = General.IsInv(winInventory.X, winInventory.Y);
        if (slot >= 0)
        {
            ref var dragBox = ref WindowManager.DragBox;

            dragBox.Type = DraggablePartType.Item;
            dragBox.Value = GetPlayerInv(GameState.MyIndex, slot);
            dragBox.Origin = PartOrigin.Inventory;
            dragBox.Slot = slot;

            var windowIndex = WindowManager.GetWindow("winDragBox");
            var window = WindowManager.Windows[windowIndex];

            window.X = GameState.CurMouseX;
            window.Y = GameState.CurMouseY;
            window.MovedX = GameState.CurMouseX - window.X;
            window.MovedY = GameState.CurMouseY - window.Y;

            WindowManager.ShowWindow(windowIndex, resetPosition: false);

            winInventory.State = ControlState.Normal;
        }

        OnMouseMove();
    }

    public static void OnDoubleClick()
    {
        var winInventory = WindowManager.GetWindowByName("winInventory");
        if (winInventory is null)
        {
            return;
        }

        var slot = General.IsInv(winInventory.X, winInventory.Y);
        if (slot >= 0)
        {
            Sender.SendUseItem(slot);
        }

        OnMouseMove();
    }

    public static void OnMouseMove()
    {
        if (WindowManager.DragBox.Type != DraggablePartType.None)
        {
            return;
        }

        var winInventory = WindowManager.GetWindowByName("winInventory");
        if (winInventory is null)
        {
            return;
        }

        var winDescription = WindowManager.GetWindowByName("winDescription");
        if (winDescription is null)
        {
            return;
        }

        var slot = General.IsInv(winInventory.X, winInventory.Y);
        if (slot < 0)
        {
            winDescription.Visible = false;
            return;
        }

        if (Trade.InTrade > 0)
        {
            for (var i = 0; i < Variables.MaxInventory; i++)
            {
                if (Data.TradeYourOffer[i].Num != slot)
                {
                    continue;
                }

                if (Item.Instance[GetPlayerInv(GameState.MyIndex, Data.TradeYourOffer[i].Num)].Type != (byte) ItemCategory.Currency)
                {
                    return;
                }

                if (Data.TradeYourOffer[i].Value == GetPlayerInvValue(GameState.MyIndex, Data.TradeYourOffer[i].Num))
                {
                    return;
                }
            }
        }

        if (WindowManager.DragBox.Type == DraggablePartType.Item &&
            WindowManager.DragBox.Value == slot)
        {
            return;
        }

        var x = winInventory.X - winDescription.Width;
        if (x < 0)
        {
            x = winInventory.X + winInventory.Width;
        }

        var y = winInventory.Y - 6;

        GameState.DescOwnerWindow = "winInventory";
        GameLogic.ShowInvDesc(x, y, slot);
    }
}