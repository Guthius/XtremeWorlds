using System.Diagnostics.CodeAnalysis;
using System.IO;
using Client.Net;
using Core.Globals;
using static Core.Globals.Command;
using Type = Core.Globals.Type;

namespace Client.Game.UI.Windows;

[SuppressMessage("ReSharper", "PossibleLossOfFraction")]
public static class WinDragBox
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
                    var icon = Data.Item[dragBox.Value].Icon;
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
                    var icon = Data.Skill[dragBox.Value].Icon;
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

                case "winSkills":
                    DropOnSkills(targetWindow);
                    break;

                case "winHotbar":
                    DropOnHotBar(targetWindow);
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
                    for (var slot = 0; slot <= Variables.MaxBank; slot++)
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

                        Sender.SendChangeBankSlots(WindowManager.DragBox.Slot, slot);
                        break;
                    }
                }

                break;

            case PartOrigin.Inventory:
                if (WindowManager.DragBox.Type == DraggablePartType.Item)
                {
                    if (Data.Item[GetPlayerInv(GameState.MyIndex, WindowManager.DragBox.Slot)].Type != (byte) ItemCategory.Currency)
                    {
                        Sender.SendDepositItem(WindowManager.DragBox.Slot, 1);
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
                    for (var slot = 0; slot < Variables.MaxInv; slot++)
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
                            Sender.SendChangeInvSlots(WindowManager.DragBox.Slot, slot);
                        }

                        break;
                    }
                }

                break;

            case PartOrigin.Bank:
                if (WindowManager.DragBox.Type == DraggablePartType.Item)
                {
                    if (Data.Item[GetBank(GameState.MyIndex, (byte) WindowManager.DragBox.Slot)].Type != (byte) ItemCategory.Currency)
                    {
                        Sender.SendWithdrawItem((byte) WindowManager.DragBox.Slot, 0);
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

        for (var slot = 0; slot < Variables.MaxPlayerSkills; slot++)
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
                Sender.SendChangeSkillSlots(WindowManager.DragBox.Slot, slot);
            }

            break;
        }
    }

    private static void DropOnHotBar(Window window)
    {
        if (WindowManager.DragBox.Origin == PartOrigin.None ||
            WindowManager.DragBox.Type == DraggablePartType.None)
        {
            return;
        }

        for (var slot = 0; slot < Variables.MaxHotbar; slot++)
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
                        Sender.SendSetHotbarSlot((int) PartOrigin.Inventory, slot, WindowManager.DragBox.Slot, WindowManager.DragBox.Value);
                        break;

                    case DraggablePartType.Skill:
                        Sender.SendSetHotbarSlot((int) PartOrigin.SkillTree, slot, WindowManager.DragBox.Slot, WindowManager.DragBox.Value);
                        break;
                }
            }
            else if (WindowManager.DragBox.Slot != slot)
            {
                Sender.SendSetHotbarSlot((int) PartOrigin.Hotbar, slot, WindowManager.DragBox.Slot, WindowManager.DragBox.Value);
            }

            break;
        }
    }

    private static void DropWithoutTarget()
    {
        switch (WindowManager.DragBox.Origin)
        {
            case PartOrigin.Inventory:
                if (Data.Item[GetPlayerInv(GameState.MyIndex, WindowManager.DragBox.Slot)].Type != (byte) ItemCategory.Currency)
                {
                    Sender.SendDropItem(WindowManager.DragBox.Slot, GetPlayerInv(GameState.MyIndex, WindowManager.DragBox.Slot));
                }
                else
                {
                    GameLogic.Dialogue("Drop Item", "Please choose how many to drop.", "", DialogueType.DropItem, DialogueStyle.Input, WindowManager.DragBox.Slot);
                }

                break;

            case PartOrigin.SkillTree:
                Sender.SendForgetSkill(WindowManager.DragBox.Slot);
                break;

            case PartOrigin.Hotbar:
                Sender.SendSetHotbarSlot((int) WindowManager.DragBox.Origin, WindowManager.DragBox.Slot, WindowManager.DragBox.Slot, 0);
                break;
        }
    }
}