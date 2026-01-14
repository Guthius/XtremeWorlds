using System.Diagnostics.CodeAnalysis;
using System.IO;
using Client.Net;
using Core.Globals;
using static Core.Globals.Commands;
using Type = Core.Globals.Type;

namespace Client.Game.UI.Windows;

public class WinDragBox
{
    public static void OnDraw()
    {
        var winDragBox = WindowManager.GetWindowByName("winDragBox");
        if (winDragBox is null)
        {
            return;
        }

        var x = winDragBox.X;
        var y = winDragBox.Y;

        if (WindowManager.DragBox.Type == DraggablePartType.None)
        {
            return;
        }

        ref var dragBox = ref WindowManager.DragBox;
        switch (dragBox.Type)
        {
            case DraggablePartType.Item:
                if (dragBox.Value >= 0)
                {
                    var icon = Item.Instance[dragBox.Value].Icon;
                    if (icon <= 0 || icon > GameState.NumItems)
                    {
                        return;
                    }

                    var iconPath = Path.Combine(DataPath.Items, icon.ToString());

                    GameClient.RenderTexture(ref iconPath, x, y, 0, 0, 32, 32, 32, 32);
                }

                break;

            case DraggablePartType.Skill:
                if (dragBox.Value >= 0)
                {
                    var icon = Skill.Instance[dragBox.Value].Icon;
                    if (icon <= 0 || icon > GameState.NumSkills)
                    {
                        return;
                    }
                    var iconPath = Path.Combine(DataPath.Skills, icon.ToString());

                    GameClient.RenderTexture(ref iconPath, x, y, 0, 0, 32, 32, 32, 32);
                }

                break;
        }
    }

    public static void DragBox_Check()
    {
        Window? targetWindow = null;

        var winDragBox = WindowManager.GetWindowByName("winDragBox");
        if (winDragBox is null)
        {
            return;
        }

        if (WindowManager.DragBox.Type == DraggablePartType.None)
        {
            return;
        }

        foreach (var window in WindowManager.Windows.Values)
        {
            if (!window.Visible || window.Name == "winDragBox")
            {
                continue;
            }

            if (GameState.CurMouseX < window.X ||
                GameState.CurMouseX > window.X + window.Width ||
                GameState.CurMouseY < window.Y ||
                GameState.CurMouseY > window.Y + window.Height)
            {
                continue;
            }

            targetWindow ??= window;

            if (window.ZOrder > targetWindow.ZOrder)
            {
                targetWindow = window;
            }
        }

        if (targetWindow is not null)
        {
            switch (targetWindow.Name)
            {
                case "winBank":
                    DropOnBank(targetWindow);
                    break;

                case "winInventory":
                    DropOnInventory(targetWindow);
                    break;

                case "winCharacter":
                    DropOnCharacter(targetWindow);
                    break;

                case "winSkills":
                    DropOnSkills(targetWindow);
                    break;

                case "winHotbar":
                    DropOnHotBar(targetWindow);
                    break;

                case "winTrade":
                    DropOnTrade(targetWindow);
                    break;
            }
        }
        else
        {
            DropWithoutTarget();
        }

        WindowManager.HideWindow("winDragBox");

        ref var dragBox = ref WindowManager.DragBox;

        dragBox.Type = DraggablePartType.None;
        dragBox.Slot = 0;
        dragBox.Origin = PartOrigin.None;
        dragBox.Value = 0;
    }

    private static void DropOnBank(Window window)
    {
        switch (WindowManager.DragBox.Origin)
        {
            case PartOrigin.Bank:
                if (WindowManager.DragBox.Type == DraggablePartType.Item)
                {
                    for (var slot = 0; slot <= Core.Globals.Variables.MaxBank; slot++)
                    {
                        Type.Rect rect;

                        rect.Top = window.Y + GameState.BankTop + (GameState.BankOffsetY + 32) * (slot / GameState.BankColumns);
                        rect.Bottom = rect.Top + 32;
                        rect.Left = window.X + GameState.BankLeft + (GameState.BankOffsetX + 32) * (slot % GameState.BankColumns);
                        rect.Right = rect.Left + 32;

                        if (GameState.CurMouseX < rect.Left ||
                            GameState.CurMouseX > rect.Right ||
                            GameState.CurMouseY < rect.Top ||
                            GameState.CurMouseY > rect.Bottom)
                        {
                            continue;
                        }

                        if (WindowManager.DragBox.Slot == slot)
                        {
                            continue;
                        }

                        Sender.ChangeBankSlots(WindowManager.DragBox.Slot, slot);
                        break;
                    }
                }

                break;

            case PartOrigin.Inventory:
                if (WindowManager.DragBox.Type == DraggablePartType.Item)
                {
                    if (Item.Instance[GetPlayerInv(GameState.MyIndex, WindowManager.DragBox.Slot)].Type != (byte) ItemCategory.Currency)
                    {
                        Sender.DepositItem(WindowManager.DragBox.Slot, 1);
                    }
                    else
                    {
                        GameLogic.Dialogue("Deposit Item", "Enter the deposit quantity.", "", DialogueType.DepositItem, DialogueStyle.Input, WindowManager.DragBox.Slot);
                    }
                }

                break;
        }
    }

    private static void DropOnInventory(Window window)
    {
        switch (WindowManager.DragBox.Origin)
        {
            case PartOrigin.Inventory:
                if (WindowManager.DragBox.Type == DraggablePartType.Item)
                {
                    for (var slot = 0; slot < Core.Globals.Variables.MaxInventory; slot++)
                    {
                        Type.Rect rect;

                        rect.Top = window.Y + GameState.InvTop + (GameState.InvOffsetY + 32) * (slot / GameState.InvColumns);
                        rect.Bottom = rect.Top + 32;
                        rect.Left = window.X + GameState.InvLeft + (GameState.InvOffsetX + 32) * (slot % GameState.InvColumns);
                        rect.Right = rect.Left + 32;

                        if (GameState.CurMouseX < rect.Left ||
                            GameState.CurMouseX > rect.Right ||
                            GameState.CurMouseY < rect.Top ||
                            GameState.CurMouseY > rect.Bottom)
                        {
                            continue;
                        }

                        if (WindowManager.DragBox.Slot != slot)
                        {
                            Sender.ChangeInvSlots(WindowManager.DragBox.Slot, slot);
                        }

                        break;
                    }
                }

                break;

            case PartOrigin.Character:
                if (WindowManager.DragBox.Type == DraggablePartType.Item)
                {
                    for (var slot = 0; slot < Core.Globals.Variables.MaxInventory; slot++)
                    {
                        Type.Rect rect;

                        rect.Top = window.Y + GameState.InvTop + (GameState.InvOffsetY + 32) * (slot / GameState.InvColumns);
                        rect.Bottom = rect.Top + 32;
                        rect.Left = window.X + GameState.InvLeft + (GameState.InvOffsetX + 32) * (slot % GameState.InvColumns);
                        rect.Right = rect.Left + 32;

                        if (GameState.CurMouseX < rect.Left ||
                            GameState.CurMouseX > rect.Right ||
                            GameState.CurMouseY < rect.Top ||
                            GameState.CurMouseY > rect.Bottom)
                        {
                            continue;
                        }

                        Sender.Unequip(WindowManager.DragBox.Slot);
                        break;
                    }
                }

                break;

            case PartOrigin.Bank:
                if (WindowManager.DragBox.Type == DraggablePartType.Item)
                {
                    if (Item.Instance[GetBank(GameState.MyIndex, (byte) WindowManager.DragBox.Slot)].Type != (byte) ItemCategory.Currency)
                    {
                        Sender.WithdrawItem((byte) WindowManager.DragBox.Slot, 0);
                    }
                    else
                    {
                        GameLogic.Dialogue("Withdraw Item", "Enter the amount you wish to withdraw.", "", DialogueType.WithdrawItem, DialogueStyle.Input, WindowManager.DragBox.Slot);
                    }
                }

                break;
        }
    }

    private static void DropOnSkills(Window window)
    {
        if (WindowManager.DragBox.Origin != PartOrigin.SkillTree ||
            WindowManager.DragBox.Type != DraggablePartType.Skill)
        {
            return;
        }

        for (var slot = 0; slot < Core.Globals.Variables.MaxPlayerSkills; slot++)
        {
            Type.Rect rect;

            rect.Top = window.Y + GameState.SkillTop + (GameState.SkillOffsetY + 32) * (slot / GameState.SkillColumns);
            rect.Bottom = rect.Top + 32;
            rect.Left = window.X + GameState.SkillLeft + (GameState.SkillOffsetX + 32) * (slot % GameState.SkillColumns);
            rect.Right = rect.Left + 32;

            if (GameState.CurMouseX < rect.Left ||
                GameState.CurMouseX > rect.Right ||
                GameState.CurMouseY < rect.Top ||
                GameState.CurMouseY > rect.Bottom)
            {
                continue;
            }

            if (WindowManager.DragBox.Slot != slot)
            {
                Sender.ChangeSkillSlots(WindowManager.DragBox.Slot, slot);
            }

            break;
        }
    }

    private static void DropOnCharacter(Window window)
    {
        if (WindowManager.DragBox.Type != DraggablePartType.Item)
        {
            return;
        }

        var origin = WindowManager.DragBox.Origin;
        if (origin != PartOrigin.Inventory && origin != PartOrigin.Hotbar)
        {
            return;
        }

        static int FindInventorySlotForItem(int itemNum)
        {
            if (itemNum < 0 || itemNum >= Item.Instance.Count)
            {
                return -1;
            }

            for (var slot = 0; slot < Core.Globals.Variables.MaxInventory; slot++)
            {
                if (GetPlayerInv(GameState.MyIndex, slot) == itemNum)
                {
                    return slot;
                }
            }

            return -1;
        }

        int inv;
        int item;
        if (origin == PartOrigin.Inventory)
        {
            inv = WindowManager.DragBox.Slot;
            if (inv < 0 || inv >= Core.Globals.Variables.MaxInventory)
            {
                return;
            }

            item = GetPlayerInv(GameState.MyIndex, inv);
        }
        else
        {
            // Hotbar drags store item id in Value; we must resolve it to an inventory slot to equip.
            item = WindowManager.DragBox.Value;
            inv = FindInventorySlotForItem(item);
        }

        if (inv < 0 || inv >= Core.Globals.Variables.MaxInventory)
        {
            return;
        }

        if (item < 0 || item >= Item.Instance.Count)
        {
            return;
        }

        // Only allow drag->equip for equipment items. (Avoid accidentally consuming potions, etc.)
        if (Item.Instance[item].Type != (byte)ItemCategory.Equipment)
        {
            return;
        }

        var equipmentCount = Enum.GetValues<Equipment>().Length;
        for (var slot = 0; slot < equipmentCount; slot++)
        {
            Type.Rect rect;

            rect.Top = window.Y + GameState.EqTop + (GameState.EqOffsetY + Constants.TileSize) * (slot / GameState.EqColumns);
            rect.Bottom = rect.Top + Constants.TileSize;
            rect.Left = window.X + GameState.EqLeft + (GameState.EqOffsetX + Constants.TileSize) * (slot % GameState.EqColumns);
            rect.Right = rect.Left + Constants.TileSize;

            if (GameState.CurMouseX < rect.Left ||
                GameState.CurMouseX > rect.Right ||
                GameState.CurMouseY < rect.Top ||
                GameState.CurMouseY > rect.Bottom)
            {
                continue;
            }

            // Require the item subtype to match the equipment slot we dropped onto.
            // The server will still validate, but this prevents confusing “nothing happens” drops.
            if ((Equipment)Item.Instance[item].SubType != (Equipment)slot)
            {
                return;
            }

            Sender.UseItem(inv);
            return;
        }
    }

    private static void DropOnHotBar(Window window)
    {
        if (WindowManager.DragBox.Origin == PartOrigin.None ||
            WindowManager.DragBox.Type == DraggablePartType.None)
        {
            return;
        }

        for (var slot = 0; slot < Core.Globals.Variables.MaxHotbar; slot++)
        {
            Type.Rect rect;

            rect.Top = window.Y + GameState.HotbarTop;
            rect.Bottom = rect.Top + 32;
            rect.Left = window.X + GameState.HotbarLeft + slot * GameState.HotbarOffsetX;
            rect.Right = rect.Left + 32;

            if (GameState.CurMouseX < rect.Left ||
                GameState.CurMouseX > rect.Right ||
                GameState.CurMouseY < rect.Top ||
                GameState.CurMouseY > rect.Bottom)
            {
                continue;
            }

            if (WindowManager.DragBox.Origin != PartOrigin.Hotbar)
            {
                switch (WindowManager.DragBox.Type)
                {
                    case DraggablePartType.Item:
                        Sender.SetHotbarSlot((int) PartOrigin.Inventory, slot, WindowManager.DragBox.Slot, WindowManager.DragBox.Value);
                        break;

                    case DraggablePartType.Skill:
                        Sender.SetHotbarSlot((int) PartOrigin.SkillTree, slot, WindowManager.DragBox.Slot, WindowManager.DragBox.Value);
                        break;
                }
            }
            else if (WindowManager.DragBox.Slot != slot)
            {
                Sender.SetHotbarSlot((int) PartOrigin.Hotbar, slot, WindowManager.DragBox.Slot, WindowManager.DragBox.Value);
            }

            break;
        }
    }

    private static void DropWithoutTarget()
    {
        switch (WindowManager.DragBox.Origin)
        {
            case PartOrigin.Inventory:
                {
                    var inv = WindowManager.DragBox.Slot;
                    var item = GetPlayerInv(GameState.MyIndex, inv);
                    if (item < 0 || item >= Item.Instance.Count)
                    {
                        break;
                    }

                    var isCurrency = Item.Instance[item].Type == (byte)ItemCategory.Currency;
                    var isStackable = Item.Instance[item].Stackable == 1;
                    if (isCurrency || isStackable)
                    {
                        GameLogic.Dialogue(
                            "Drop Item",
                            "Please choose how many to drop.",
                            "",
                            DialogueType.DropItem,
                            DialogueStyle.Input,
                            inv);
                    }
                    else
                    {
                        Sender.DropItem(inv, 1);
                    }

                    break;
                }

            case PartOrigin.SkillTree:
                Sender.ForgetSkill(WindowManager.DragBox.Slot);
                break;

            case PartOrigin.Hotbar:
                Sender.DeleteHotbar(WindowManager.DragBox.Slot);
                break;
        }
    }

    private static void DropOnTrade(Window window)
    {
        if (WindowManager.DragBox.Origin != PartOrigin.Inventory ||
            WindowManager.DragBox.Type != DraggablePartType.Item)
        {
            return;
        }

        var picYour = window.GetChild("picYour");
        if (picYour is null)
        {
            return;
        }

        var slotX = window.X + picYour.X;
        var slotY = window.Y + picYour.Y;

        var slot = General.IsTrade(slotX, slotY);
        if (slot < 0)
        {
            return;
        }

        var inv = WindowManager.DragBox.Slot;
        if (inv < 0 || inv >= Core.Globals.Variables.MaxInventory)
        {
            return;
        }

        // Match the same offer rules as the old inventory double-click trade behavior.
        for (var i = 0; i < Core.Globals.Variables.MaxInventory; i++)
        {
            if (Data.TradeYourOffer[i].Num != inv)
            {
                continue;
            }

            if (Item.Instance[GetPlayerInv(GameState.MyIndex, Data.TradeYourOffer[i].Num)].Type != (byte)ItemCategory.Currency)
            {
                return;
            }

            if (Data.TradeYourOffer[i].Value == GetPlayerInvValue(GameState.MyIndex, Data.TradeYourOffer[i].Num))
            {
                return;
            }
        }

        if (Item.Instance[GetPlayerInv(GameState.MyIndex, inv)].Type == (byte)ItemCategory.Currency)
        {
            GameLogic.Dialogue(
                "Select Amount",
                "Please choose how many to offer.",
                "",
                DialogueType.TradeAmount,
                DialogueStyle.Input,
                inv);

            return;
        }

        Sender.TradeItem(inv, 0);
    }
}