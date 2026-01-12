using Client.Net;
using Core.Globals;
using System.IO;
using static Core.Globals.Commands;

namespace Client.Game.UI.Windows;

public class WinCharacter
{
    private static readonly Equipment[] EquipmentTypes = Enum.GetValues<Equipment>();

    public static void Update()
    {
        if (GameState.MyIndex < 0 || GameState.MyIndex >= Player.Instance.Count)
        {
            return;
        }
        UpdateBars();

        var winCharacter = WindowManager.GetWindowByName("winCharacter");
        if (winCharacter is null)
        {
            return;
        }

        winCharacter.GetChild("lblHealth").Text = "Health";
        winCharacter.GetChild("lblSpirit").Text = "Spirit";
        winCharacter.GetChild("lblExperience").Text = "Exp";
        winCharacter.GetChild("lblHealth2").Text = GetPlayerVital(GameState.MyIndex, Core.Globals.Vital.Health) + "/" + GetPlayerMaxVital(GameState.MyIndex, Core.Globals.Vital.Health);
        winCharacter.GetChild("lblSpirit2").Text = GetPlayerVital(GameState.MyIndex, Core.Globals.Vital.Stamina) + "/" + GetPlayerMaxVital(GameState.MyIndex, Core.Globals.Vital.Stamina);
        winCharacter.GetChild("lblExperience2").Text = Player.Instance[GameState.MyIndex].Experience + "/" + GameState.NextlevelExp;
    }

    private static void UpdateBars()
    {
        if (GameState.MyIndex < 0 || GameState.MyIndex >= Player.Instance.Count)
        {
            return;
        }
        var winBars = WindowManager.GetWindowByName("winBars");
        if (winBars is null)
        {
            return;
        }

        winBars.GetChild("lblHP").Text = GetPlayerVital(GameState.MyIndex, Core.Globals.Vital.Health) + "/" + GetPlayerMaxVital(GameState.MyIndex, Core.Globals.Vital.Health);
        winBars.GetChild("lblMP").Text = GetPlayerVital(GameState.MyIndex, Core.Globals.Vital.Mana) + "/" + GetPlayerMaxVital(GameState.MyIndex, Core.Globals.Vital.Mana);
        winBars.GetChild("lblEXP").Text = GetPlayerExperience(GameState.MyIndex) + "/" + GameState.NextlevelExp;
    }

    public static void OnDrawCharacter()
    {
        if (GameState.MyIndex < 0 || GameState.MyIndex > Variables.MaxPlayers)
        {
            return;
        }

        var winCharacter = WindowManager.GetWindowByName("winCharacter");
        if (winCharacter is null)
        {
            return;
        }

        var x = winCharacter.X;
        var y = winCharacter.Y;

        // Render bottom
        var argPath = Path.Combine(DataPath.Gui, "37");
        GameClient.RenderTexture(ref argPath, x + 4, y + 314, 0, 0, 40, 38, 40, 38);
        GameClient.RenderTexture(ref argPath, x + 44, y + 314, 0, 0, 40, 38, 40, 38);
        GameClient.RenderTexture(ref argPath, x + 84, y + 314, 0, 0, 40, 38, 40, 38);
        GameClient.RenderTexture(ref argPath, x + 124, y + 314, 0, 0, 46, 38, 46, 38);

        // render top wood
        var argPath4 = Path.Combine(DataPath.Gui, "1");
        GameClient.RenderTexture(ref argPath4, x + 4, y + 23, 100, 100, 166, 291, 166, 291);

        for (var i = 0; i < EquipmentTypes.Length; i++)
        {
            var item = GetPlayerPaperdoll(GameState.MyIndex, EquipmentTypes[i]);
            if (item < 0)
            {
                continue;
            }

            Item.OnStream(item);

            var itemIcon = Item.Instance[item].Icon;
            if (itemIcon <= 0 || itemIcon >= GameState.NumItems)
            {
                continue;
            }

            x = winCharacter.X + GameState.EqLeft + (GameState.EqOffsetX + 32) * (i % GameState.EqColumns);
            y = winCharacter.Y + GameState.EqTop;

            var path = Path.Combine(DataPath.Items, itemIcon.ToString());

            GameClient.RenderTexture(ref path, x, y, 0, 0, 32, 32, 32, 32);
        }
    }

    public static void OnDoubleClick()
    {
        var winCharacter = WindowManager.GetWindowByName("winCharacter");
        if (winCharacter is null)
        {
            return;
        }

        var slot = General.IsEq(winCharacter.X, winCharacter.Y);
        if (slot >= 0)
        {
            Sender.SendUnequip(slot);
        }

        OnMouseMove();
    }

    public static void OnMouseDown()
    {
        var winCharacter = WindowManager.GetWindowByName("winCharacter");
        if (winCharacter is null)
        {
            return;
        }

        var slot = General.IsEq(winCharacter.X, winCharacter.Y);
        if (slot < 0 || slot >= EquipmentTypes.Length)
        {
            OnMouseMove();
            return;
        }

        var item = GetPlayerPaperdoll(GameState.MyIndex, EquipmentTypes[slot]);
        if (item < 0 || item >= Item.Instance.Count)
        {
            OnMouseMove();
            return;
        }

        ref var dragBox = ref WindowManager.DragBox;

        dragBox.Type = DraggablePartType.Item;
        dragBox.Value = item;
        dragBox.Origin = PartOrigin.Character;
        dragBox.Slot = slot;

        var windowIndex = WindowManager.GetWindow("winDragBox");
        var winDragBox = WindowManager.Windows[windowIndex];

        winDragBox.X = GameState.CurMouseX;
        winDragBox.Y = GameState.CurMouseY;
        winDragBox.MovedX = GameState.CurMouseX - winDragBox.X;
        winDragBox.MovedY = GameState.CurMouseY - winDragBox.Y;

        WindowManager.ShowWindow(windowIndex, resetPosition: false);

        winCharacter.State = ControlState.Normal;

        OnMouseMove();
    }

    public static void OnMouseMove()
    {
        if (WindowManager.DragBox.Type != DraggablePartType.None)
        {
            return;
        }

        var winCharacter = WindowManager.GetWindowByName("winCharacter");
        if (winCharacter is null)
        {
            return;
        }

        var winDescription = WindowManager.GetWindowByName("winDescription");
        if (winDescription is null)
        {
            return;
        }

        var slot = General.IsEq(winCharacter.X, winCharacter.Y);
        if (slot < 0)
        {
            winDescription.Visible = false;
            return;
        }

        var x = winCharacter.X - winDescription.Width;
        if (x < 0)
        {
            x = winCharacter.X + winCharacter.Width;
        }

        var y = winCharacter.Y - 6;

        GameState.DescOwnerWindow = "winCharacter";
        GameLogic.ShowEqDesc(x, y, slot);
    }

    public static void OnSpendPoint1()
    {
        Sender.SendTrainStat(0);
    }

    public static void OnSpendPoint2()
    {
        Sender.SendTrainStat(1);
    }

    public static void OnSpendPoint3()
    {
        Sender.SendTrainStat(2);
    }

    public static void OnSpendPoint4()
    {
        Sender.SendTrainStat(3);
    }

    public static void OnSpendPoint5()
    {
        Sender.SendTrainStat(4);
    }
}