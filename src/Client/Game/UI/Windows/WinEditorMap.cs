using System.IO;
using Client.Game.UI;
using Client.Game.UI.Controls;
using Core.Globals;

namespace Client.Game.UI.Windows;

public static class WinEditorMap
{
    // Draw the tileset preview into the picTileset PictureBox area
    public static void OnDrawTileset()
    {
        var win = WindowManager.GetWindowByName("winEditorMap");
        if (win is null) return;
        if (!WindowManager.TryGetControl("winEditorMap", "picTileset", out var ctrl)) return;

        int tilesetIndex = GameState.CurTileset;
        if (tilesetIndex <= 0) tilesetIndex = Data.MyMap.Tileset;
        if (tilesetIndex <= 0) return;

        // Build tileset path (extension added by GetGfxInfo / RenderTexture)
        var path = Path.Combine(DataPath.Tilesets, tilesetIndex.ToString());
        var info = GameClient.GetGfxInfo(path);
        if (info is null || info.Width <= 0 || info.Height <= 0) return;

        int srcW = info.Width;
        int srcH = info.Height;

        // Viewport equals the PictureBox size (targeting 512x512 by layout)
        int viewW = ctrl.Width;
        int viewH = ctrl.Height;
        if (viewW <= 0 || viewH <= 0) return;

        // Horizontal/Vertical scroll bars determine source X/Y offsets
        int scrollX = 0;
        int scrollY = 0;
        if (WindowManager.TryGetControl("winEditorMap", "sldTilesetH", out var sbhCtrl) && sbhCtrl is ScrollBar sbh)
        {
            var maxX = System.Math.Max(0, srcW - viewW);
            sbh.Max = maxX;
            sbh.Min = 0;
            sbhCtrl.Value = System.Math.Clamp(sbhCtrl.Value, sbh.Min, sbh.Max);
            scrollX = sbhCtrl.Value;
        }
        if (WindowManager.TryGetControl("winEditorMap", "sldTilesetV", out var sbCtrl) && sbCtrl is ScrollBar sb)
        {
            // Update range based on current tileset height
            var max = System.Math.Max(0, srcH - viewH);
            sb.Max = max;
            sb.Min = 0;
            // Clamp the current value
            sbCtrl.Value = System.Math.Clamp(sbCtrl.Value, sb.Min, sb.Max);
            scrollY = sbCtrl.Value;
        }

        // Clamp source region
        int sX = System.Math.Clamp(scrollX, 0, System.Math.Max(0, srcW - viewW));
        int sY = System.Math.Clamp(scrollY, 0, System.Math.Max(0, srcH - viewH));
        int sW = System.Math.Min(viewW, srcW - sX);
        int sH = System.Math.Min(viewH, srcH - sY);

        // Center horizontally if the tileset is narrower than the viewport
        int destX = win.X + ctrl.X + (viewW - sW) / 2;
        int destY = win.Y + ctrl.Y;

        // Draw 1:1 cropped region
        GameClient.RenderTexture(ref path, destX, destY, sX, sY, sW, sH, sW, sH);
    }
}
