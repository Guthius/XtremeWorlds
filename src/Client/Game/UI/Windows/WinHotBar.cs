using Client.Net;
using Core.Globals;
using Microsoft.Xna.Framework;
using System.IO;
using static Core.Globals.Commands;

namespace Client.Game.UI.Windows;

public class WinHotBar
{
    public static void OnDraw()
    {
        if (GameState.MyIndex < 0 || GameState.MyIndex > Core.Globals.Variables.MaxPlayers)
        {
            return;
        }

        var winHotbar = WindowManager.GetWindowByName("winHotbar");
        if (winHotbar is null)
        {
            return;
        }

        if (GameState.PlayerData == false)
        {
            return;
        }

        var argPath = Path.Combine(DataPath.Gui, "31");

        GameClient.RenderTexture(ref argPath, winHotbar.X - 1, winHotbar.Y + 3, 0, 0, 11, 26, 11, 26);
        GameClient.RenderTexture(ref argPath, winHotbar.X + 407, winHotbar.Y + 3, 0, 0, 11, 26, 11, 26);

        for (var slot = 0; slot < Core.Globals.Variables.MaxHotbar; slot++)
        {
            var x = winHotbar.X + GameState.HotbarLeft + slot * GameState.HotbarOffsetX;
            var y = winHotbar.Y + GameState.HotbarTop;

            if (slot != Core.Globals.Variables.MaxHotbar - 1)
            {
                var argPath2 = Path.Combine(DataPath.Gui, "32");

                GameClient.RenderTexture(ref argPath2, x + 30, y + 3, 0, 0, 13, 26, 13, 26);
            }

            var argPath3 = Path.Combine(DataPath.Gui, "30");

            GameClient.RenderTexture(ref argPath3, x - 2, y - 2, 0, 0, 36, 36, 36, 36);

            if (WindowManager.DragBox.Origin != PartOrigin.Hotbar || WindowManager.DragBox.Slot != slot)
            {
                switch (Player.Instance[GameState.MyIndex].Hotbar[slot].SlotType)
                {
                    case (byte) PartOrigin.Inventory:
                        DrawInventorySlot(slot, x, y);
                        break;

                    case (byte) PartOrigin.SkillTree:
                        DrawSkillTreeSlot(slot, x, y);
                        break;
                }
            }

            var hotbar = slot + 1;
            if (hotbar > 9)
            {
                hotbar = 0;
            }

            var str = hotbar.ToString();

            TextRenderer.Render(str, x + 4, y + 19, Color.White, Color.White, winHotbar.Font);
        }
    }

    public static void OnMouseDown()
    {
        var winHotbar = WindowManager.GetWindowByName("winHotbar");
        if (winHotbar is null)
        {
            return;
        }

        var slot = GameLogic.IsHotbar(winHotbar.X, winHotbar.Y);
        // Right-click: remove hotbar slot
        if (slot >= 0 && GameClient.IsMouseButtonDown(MouseButton.Right))
        {
            Sender.DeleteHotbar(slot);
            return;
        }

        if (slot >= 0)
        {
            ref var dragBox = ref WindowManager.DragBox;

            dragBox.Type = Player.Instance[GameState.MyIndex].Hotbar[slot].SlotType switch
            {
                1 => (DraggablePartType) PartOrigin.Inventory,
                2 => (DraggablePartType) PartOrigin.SkillTree,
                _ => dragBox.Type
            };

            dragBox.Value = Player.Instance[GameState.MyIndex].Hotbar[slot].Slot;
            dragBox.Origin = PartOrigin.Hotbar;
            dragBox.Slot = slot;

            var windowIndex = WindowManager.GetWindow("winDragBox");
            var winDragBox = WindowManager.Windows[windowIndex];

            winDragBox.X = GameState.CurMouseX;
            winDragBox.Y = GameState.CurMouseY;
            winDragBox.MovedX = GameState.CurMouseX - winDragBox.X;
            winDragBox.MovedY = GameState.CurMouseY - winDragBox.Y;

            WindowManager.ShowWindow(windowIndex, resetPosition: false);

            winHotbar.State = ControlState.Normal;
        }

        OnMouseMove();
    }

    public static void OnDoubleClick()
    {
        var winHotbar = WindowManager.GetWindowByName("winHotbar");
        if (winHotbar is null)
        {
            return;
        }

        var slot = GameLogic.IsHotbar(winHotbar.X, winHotbar.Y);
        if (slot >= 0)
        {
            Sender.UseHotbarSlot(slot);
        }

        OnMouseMove();
    }

    public static void OnMouseMove()
    {
        if (WindowManager.DragBox.Type != (int) PartOrigin.None)
        {
            return;
        }

        var winHotbar = WindowManager.GetWindowByName("winHotbar");
        if (winHotbar is null)
        {
            return;
        }

        var winDescription = WindowManager.GetWindowByName("winDescription");
        if (winDescription is null)
        {
            return;
        }

        var slot = GameLogic.IsHotbar(winHotbar.X, winHotbar.Y);
        if (slot < 0)
        {
            winDescription.Visible = false;
            return;
        }

        if (WindowManager.DragBox.Origin == PartOrigin.Hotbar &&
            WindowManager.DragBox.Slot == slot)
        {
            return;
        }

        var x = winHotbar.X - winDescription.Width;
        if (x < 0)
        {
            x = winHotbar.X + winHotbar.Width;
        }

        var y = winHotbar.Y - 6;

        GameState.DescOwnerWindow = "winHotbar";
        switch (Player.Instance[GameState.MyIndex].Hotbar[slot].SlotType)
        {
            case 1: // Inventory
                GameLogic.ShowItemDesc(x, y, Player.Instance[GameState.MyIndex].Hotbar[slot].Slot);
                break;

            case 2: // Skill
                GameLogic.ShowSkillDesc(x, y, Player.Instance[GameState.MyIndex].Hotbar[slot].Slot, 0L);
                break;
        }
    }

    private static void DrawInventorySlot(int slot, int x, int y)
    {
        var item = Player.Instance[GameState.MyIndex].Hotbar[slot].Slot;

        Item.OnStream(item);

        if (Item.Instance.Count <= item)
        {
            return;
        }

        if (Item.Instance[item].Name.Length <= 0 || Item.Instance[item].Icon <= 0)
        {
            return;
        }

        var path = Path.Combine(DataPath.Items, Item.Instance[item].Icon.ToString());

        GameClient.RenderTexture(ref path, x, y, 0, 0, 32, 32, 32, 32);
    }

    private static void DrawSkillTreeSlot(int slot, int x, int y)
    {
        var skill = Player.Instance[GameState.MyIndex].Hotbar[slot].Slot;

        Skill.OnStream(skill);

        if (Skill.Instance.Count <= skill)
        {
            return;
        }
        
        if (Skill.Instance[skill].Name.Length == 0 ||
            Skill.Instance[skill].Icon <= 0)
        {
            return;
        }

        var path = Path.Combine(DataPath.Skills, Skill.Instance[skill].Icon.ToString());

        GameClient.RenderTexture(ref path, x, y, 0, 0, 32, 32, 32, 32);

        for (var i = 0; i < Core.Globals.Variables.MaxPlayerSkills; i++)
        {
            if (GetPlayerSkill(GameState.MyIndex, i) < 0 ||
                GetPlayerSkill(GameState.MyIndex, i) != skill ||
                GetPlayerSkillCd(GameState.MyIndex, i) <= 0)
            {
                continue;
            }

            GameClient.RenderTexture(ref path, x, y, 0, 0, 32, 32, 32, 32, 255, 100, 100, 100);
        }
    }
}