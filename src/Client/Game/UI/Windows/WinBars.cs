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

        var hpBlank = winBars.GetChild("picHP_Blank");
        var mpBlank = winBars.GetChild("picMP_Blank");
        var expBlank = winBars.GetChild("picEXP_Blank");

        // Draw the fill 3px inset from the frame (matches the old hard-coded offsets).
        // Clamp to the blank frame width/height so we don't overdraw beyond the frame.
        var hpWidth = Math.Clamp(GameState.BarWidthGuiHP, 0, hpBlank.Width);
        var mpWidth = Math.Clamp(GameState.BarWidthGuiMP, 0, mpBlank.Width);
        var expWidth = Math.Clamp(GameState.BarWidthGuiExp, 0, expBlank.Width);

        var hpX = winBars.X + hpBlank.X;
        var hpY = winBars.Y + hpBlank.Y;
        var mpX = winBars.X + mpBlank.X;
        var mpY = winBars.Y + mpBlank.Y;
        var expX = winBars.X + expBlank.X;
        var expY = winBars.Y + expBlank.Y;

        var hpBarTexturePath = Path.Combine(DataPath.Gui, "27");
        var mpBarTexturePath = Path.Combine(DataPath.Gui, "28");
        var xpBarTexturePath = Path.Combine(DataPath.Gui, "29");

        GameClient.RenderTexture(ref hpBarTexturePath,
            hpX, hpY, 0, 0,
            hpWidth, hpBlank.Height,
            hpWidth, hpBlank.Height);

        GameClient.RenderTexture(ref mpBarTexturePath,
            mpX, mpY, 0, 0,
            mpWidth, mpBlank.Height,
            mpWidth, mpBlank.Height);

        GameClient.RenderTexture(ref xpBarTexturePath,
            expX, expY, 0, 0,
            expWidth, expBlank.Height,
            expWidth, expBlank.Height);
    }
}