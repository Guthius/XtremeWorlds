using Core.Globals;
using System.IO;

namespace Client.Game.UI.Windows;

public class WinBars
{
    public static void OnDraw()
    {
        var winBars = WindowManager.GetWindowByName("winBars");
        if (winBars is null)
        {
            return;
        }

        var x = winBars.X;
        var y = winBars.Y;

        var hpBarTexturePath = Path.Combine(DataPath.Gui, "27");
        var mpBarTexturePath = Path.Combine(DataPath.Gui, "28");
        var xpBarTexturePath = Path.Combine(DataPath.Gui, "29");

        GameClient.RenderTexture(ref hpBarTexturePath,
            x + 15, y + 15, 0, 0,
            GameState.BarWidthGuiHP, 13,
            GameState.BarWidthGuiHP, 13);

        GameClient.RenderTexture(ref mpBarTexturePath,
            x + 15, y + 32, 0, 0,
            GameState.BarWidthGuiMP, 13,
            GameState.BarWidthGuiMP, 13);

        GameClient.RenderTexture(ref xpBarTexturePath,
            x + 15, y + 49, 0, 0,
            GameState.BarWidthGuiExp, 13,
            GameState.BarWidthGuiExp, 13);
    }
}