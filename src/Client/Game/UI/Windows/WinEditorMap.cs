using System.IO;
using Client.Game.UI;
using Client.Game.UI.Controls;
using Core.Globals;
using Microsoft.Xna.Framework;

namespace Client.Game.UI.Windows;

public static class WinEditors
{
    private static bool _isDraggingTileset = false;
    public static int NpcSelectedSlot = 0;
    private const int WheelTileStep = 3; // scroll by 3 tiles per wheel notch
    
    public static void OnFillLayerClick()
    {
        var layer = (MapLayer)GameState.CurLayer;
        byte autotile = (byte)GameState.CurAutotileType;
        byte tileX = (byte)GameState.EditorTileX;
        byte tileY = (byte)GameState.EditorTileY;
        int tileset = GameState.CurTileset > 0 ? GameState.CurTileset : Data.MyMap.Tileset;
        GameLogic.Dialogue("Map Editor", $"Fill Layer: {layer}", "Are you sure you wish to fill this layer?", DialogueType.FillLayer, DialogueStyle.YesNo, GameState.CurLayer, autotile, tileX, tileY, tileset);
    }
    
    // Draw the tileset preview into the picTileset PictureBox area
    public static void OnDrawTileset()
    {
        var win = WindowManager.GetWindowByName("winEditors");
        if (win is null) return;
        if (!WindowManager.TryGetControl("winEditors", "picTileset", out var ctrl)) return;

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
        if (WindowManager.TryGetControl("winEditors", "sldTilesetH", out var sbhCtrl) && sbhCtrl is ScrollBar sbh)
        {
            var maxX = System.Math.Max(0, srcW - viewW);
            sbh.Max = maxX;
            sbh.Min = 0;
            sbhCtrl.Value = System.Math.Clamp(sbhCtrl.Value, sbh.Min, sbh.Max);
            scrollX = sbhCtrl.Value;
        }
        if (WindowManager.TryGetControl("winEditors", "sldTilesetV", out var sbCtrl) && sbCtrl is ScrollBar sb)
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

        // Draw selection rectangle (red outline) if in view
        int selXpx = GameState.EditorTileX * GameState.SizeX;
        int selYpx = GameState.EditorTileY * GameState.SizeY;
        int selWpx = System.Math.Max(1, GameState.EditorTileWidth) * GameState.SizeX;
        int selHpx = System.Math.Max(1, GameState.EditorTileHeight) * GameState.SizeY;

        // Compute intersection with visible src rect (sX,sY,sW,sH)
        int interLeft = System.Math.Max(selXpx, sX);
        int interTop = System.Math.Max(selYpx, sY);
        int interRight = System.Math.Min(selXpx + selWpx, sX + sW);
        int interBottom = System.Math.Min(selYpx + selHpx, sY + sH);
        int interW = interRight - interLeft;
        int interH = interBottom - interTop;
        if (interW > 0 && interH > 0)
        {
            int drawX = destX + (interLeft - sX);
            int drawY = destY + (interTop - sY);
            GameClient.DrawOutlineRectangle(drawX, drawY, interW, interH, Color.Red, 2f);
        }
    }

    // Click to choose a tile from the tileset viewport
    public static void OnTilesetMouseDown()
    {
        var win = WindowManager.GetWindowByName("winEditors");
        if (win is null) return;
        if (!WindowManager.TryGetControl("winEditors", "picTileset", out var ctrl)) return;

        int relX = GameState.CurMouseX - (win.X + ctrl.X);
        int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
        if (relX < 0 || relY < 0 || relX >= ctrl.Width || relY >= ctrl.Height) return;

        // Compute current tileset source rect and horizontal centering offset
        int tilesetIndex = GameState.CurTileset;
        if (tilesetIndex <= 0) tilesetIndex = Data.MyMap.Tileset;
        if (tilesetIndex <= 0) tilesetIndex = 1;
        var path = System.IO.Path.Combine(DataPath.Tilesets, tilesetIndex.ToString());
        var info = GameClient.GetGfxInfo(path);
        if (info is null || info.Width <= 0 || info.Height <= 0) return;
        if (_isDraggingTileset) return; // already dragging; ignore spurious repeats

        int srcW = info.Width;
        int srcH = info.Height;
        int viewW = ctrl.Width;
        int viewH = ctrl.Height;

        // Read scrollbars
        int sX = 0, sY = 0;
        if (WindowManager.TryGetControl("winEditors", "sldTilesetH", out var sbhCtrl))
            sX = System.Math.Clamp(sbhCtrl.Value, 0, System.Math.Max(0, srcW - viewW));
        if (WindowManager.TryGetControl("winEditors", "sldTilesetV", out var sbvCtrl))
            sY = System.Math.Clamp(sbvCtrl.Value, 0, System.Math.Max(0, srcH - viewH));

        int sW = System.Math.Min(viewW, srcW - sX);
        int sH = System.Math.Min(viewH, srcH - sY);
        int offsetX = (viewW - sW) / 2; // horizontal centering only

        // Map to tileset pixel coordinates (clamp inside visible image area)
        int localX = System.Math.Clamp(relX - offsetX, 0, System.Math.Max(0, sW - 1));
        int localY = System.Math.Clamp(relY, 0, System.Math.Max(0, sH - 1));
        int px = sX + localX;
        int py = sY + localY;

        // Translate to tile indices
        int tileX = System.Math.Clamp(px / GameState.SizeX, 0, int.MaxValue);
        int tileY = System.Math.Clamp(py / GameState.SizeY, 0, int.MaxValue);

                _isDraggingTileset = true;
        // Initialize selection
        GameState.EditorTileX = tileX;
        GameState.EditorTileY = tileY;
        if (GameState.CurAutotileType > 0)
        {
            switch (GameState.CurAutotileType)
            {
                case 1: GameState.EditorTileWidth = 2; GameState.EditorTileHeight = 3; break; // autotile
                case 2: GameState.EditorTileWidth = 1; GameState.EditorTileHeight = 1; break; // fake autotile
                case 3: GameState.EditorTileWidth = 6; GameState.EditorTileHeight = 3; break; // animated
                case 4: GameState.EditorTileWidth = 2; GameState.EditorTileHeight = 2; break; // cliff
                case 5: GameState.EditorTileWidth = 2; GameState.EditorTileHeight = 3; break; // waterfall
                default: GameState.EditorTileWidth = 1; GameState.EditorTileHeight = 1; break;
            }
        }
        else
        {
            GameState.EditorTileWidth = 1;
            GameState.EditorTileHeight = 1;
        }
        GameState.EditorTileSelStart = new Microsoft.Xna.Framework.Point(tileX, tileY);
        GameState.EditorTileSelEnd = new Microsoft.Xna.Framework.Point(tileX + GameState.EditorTileWidth, tileY + GameState.EditorTileHeight);
    }

    // Drag to select multiple tiles from the tileset viewport
    public static void OnTilesetMouseMove()
    {
        if (!GameClient.IsMouseButtonDown(MouseButton.Left))
        {
            // If the button was released anywhere, end the drag
            _isDraggingTileset = false;
            return;
        }
        if (!_isDraggingTileset) return;
        var win = WindowManager.GetWindowByName("winEditors");
        if (win is null) return;
        if (!WindowManager.TryGetControl("winEditors", "picTileset", out var ctrl)) return;

        int relX = GameState.CurMouseX - (win.X + ctrl.X);
        int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
        if (relX < 0 || relY < 0 || relX >= ctrl.Width || relY >= ctrl.Height) return;

        int tilesetIndex = GameState.CurTileset;
        if (tilesetIndex <= 0) tilesetIndex = Data.MyMap.Tileset;
        if (tilesetIndex <= 0) tilesetIndex = 1;
        var path = System.IO.Path.Combine(DataPath.Tilesets, tilesetIndex.ToString());
        var info = GameClient.GetGfxInfo(path);
        if (info is null || info.Width <= 0 || info.Height <= 0) return;

        int srcW = info.Width;
        int srcH = info.Height;
        int viewW = ctrl.Width;
        int viewH = ctrl.Height;

        int sX = 0, sY = 0;
        if (WindowManager.TryGetControl("winEditors", "sldTilesetH", out var sbhCtrl))
            sX = System.Math.Clamp(sbhCtrl.Value, 0, System.Math.Max(0, srcW - viewW));
        if (WindowManager.TryGetControl("winEditors", "sldTilesetV", out var sbvCtrl))
            sY = System.Math.Clamp(sbvCtrl.Value, 0, System.Math.Max(0, srcH - viewH));

        int sW = System.Math.Min(viewW, srcW - sX);
        int sH = System.Math.Min(viewH, srcH - sY);
        int offsetX = (viewW - sW) / 2;

        int localX = System.Math.Clamp(relX - offsetX, 0, System.Math.Max(0, sW - 1));
        int localY = System.Math.Clamp(relY, 0, System.Math.Max(0, sH - 1));
        int px = sX + localX;
        int py = sY + localY;
        if (GameState.CurAutotileType > 0) return; // fixed-size selections

        int startX = GameState.EditorTileSelStart.X;
        int startY = GameState.EditorTileSelStart.Y;
        int curX = System.Math.Clamp(px / GameState.SizeX, 0, int.MaxValue);
        int curY = System.Math.Clamp(py / GameState.SizeY, 0, int.MaxValue);

        int minX = System.Math.Min(startX, curX);
        int minY = System.Math.Min(startY, curY);
        int width = System.Math.Abs(curX - startX) + 1;
        int height = System.Math.Abs(curY - startY) + 1;

        GameState.EditorTileX = minX;
        GameState.EditorTileY = minY;
        GameState.EditorTileWidth = width;
        GameState.EditorTileHeight = height;
        GameState.EditorTileSelEnd = new Microsoft.Xna.Framework.Point(minX + width, minY + height);
    }

    public static void OnTilesetMouseUp()
    {
        // No-op for now; selection finalized via MapEditorDrag updates
        if (_isDraggingTileset)
        {
            _isDraggingTileset = false;
        }
    }

    // Mouse wheel support for tileset viewport: scroll vertically by tile rows
    public static void OnTilesetMouseWheel()
    {
        if (!WindowManager.TryGetControl("winEditors", "picTileset", out var ctrl)) return;
        var win = WindowManager.GetWindowByName("winEditors");
        if (win is null) return;

        int tilesetIndex = GameState.CurTileset;
        if (tilesetIndex <= 0) tilesetIndex = Data.MyMap.Tileset;
        if (tilesetIndex <= 0) return;

        var path = System.IO.Path.Combine(DataPath.Tilesets, tilesetIndex.ToString());
        var info = GameClient.GetGfxInfo(path);
        if (info is null || info.Width <= 0 || info.Height <= 0) return;

        int viewW = ctrl.Width;
        int viewH = ctrl.Height;
        int maxY = System.Math.Max(0, info.Height - viewH);
        int maxX = System.Math.Max(0, info.Width - viewW);

        int delta = GameClient.GetMouseScrollDelta();
        int stepPx = GameState.SizeY * (delta > 0 ? -WheelTileStep : WheelTileStep);

        // If Shift is held, scroll horizontally; otherwise scroll vertically
        bool shift = GameState.VbKeyShift;
        if (shift)
        {
            if (WindowManager.TryGetControl("winEditors", "sldTilesetH", out var sbhCtrl) && sbhCtrl is ScrollBar sbh)
            {
                sbh.Min = 0; sbh.Max = maxX;
                int newVal = System.Math.Clamp(sbhCtrl.Value + stepPx, sbh.Min, sbh.Max);
                sbhCtrl.Value = newVal;
            }
        }
        else
        {
            if (WindowManager.TryGetControl("winEditors", "sldTilesetV", out var sbCtrl) && sbCtrl is ScrollBar sb)
            {
                // Ensure range reflects current image size
                sb.Min = 0; sb.Max = maxY;
                int newVal = System.Math.Clamp(sbCtrl.Value + stepPx, sb.Min, sb.Max);
                sbCtrl.Value = newVal;
            }
        }
    }

    public static void OnDirClearClick()
    {
        GameLogic.Dialogue("Map Editor", "Clear Directional Blocks", "Are you sure you want to clear all directional blocks?", DialogueType.ClearDirBlocks, DialogueStyle.YesNo);
    }
    
    public static void OnDrawNpcList()
    {
        if (!WindowManager.TryGetControl("winEditors", "lstNpcs", out var ctrl) || ctrl is not ListBox list) return;

        list.Clear();

        int total = Core.Globals.Variables.MaxMapNpcs;
        for (int slot = 0; slot < total; slot++)
        {
            string name = "None";
            try
            {
                int npcIndex = (Data.MyMap.Npc != null && slot < Data.MyMap.Npc.Length) ? Data.MyMap.Npc[slot] : -1;
                if (npcIndex >= 0 && npcIndex < Core.Globals.Variables.MaxNpcs && npcIndex < (Data.Npc?.Length ?? 0))
                {
                    var n = Data.Npc?[npcIndex].Name ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(n)) name = n.Trim();
                }
            }
            catch { /* ignore */ }

            list.AddItem($"{slot + 1}: {name}");
        }

        list.SelectedIndex = Math.Clamp(NpcSelectedSlot, -1, total - 1);
        list.EnsureVisible(list.SelectedIndex);

        // Sync scrollbar from ListBox scroll offset
        if (WindowManager.TryGetControl("winEditors", "sldNpcList", out var sbCtrl) && sbCtrl is ScrollBar sb)
        {
            int visible = list.GetVisibleCount();
            int maxScroll = Math.Max(0, total - visible);
            sb.Min = 0;
            sb.Max = maxScroll;
            sbCtrl.Value = Math.Clamp(list.ScrollOffset, sb.Min, sb.Max);
        }
    }

    public static void OnNpcListMouseDown()
    {
        if (!WindowManager.TryGetControl("winEditors", "lstNpcs", out var ctrl) || ctrl is not ListBox list) return;
        var win = WindowManager.GetWindowByName("winEditors");
        if (win is null) return;

        int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
        int index = list.GetItemIndexAtPosition(relY);
        int total = Core.Globals.Variables.MaxMapNpcs;
        if (index < 0 || index >= total) return;

        NpcSelectedSlot = index;
        list.SelectedIndex = index;
        list.EnsureVisible(index);

        if (WindowManager.TryGetControl("winEditors", "cmbNpcList", out var npcCtrl) && npcCtrl is ComboBox cmbNpc)
        {
            int assigned = -1;
            if (Data.MyMap.Npc != null && index < Data.MyMap.Npc.Length)
                assigned = Data.MyMap.Npc[index];
            int desired = (assigned >= 0) ? assigned + 1 : 0;
            desired = Math.Clamp(desired, 0, cmbNpc.Items.Count - 1);
            cmbNpc.Value = desired;
        }
    }

    public static void OnNpcListMouseWheel()
    {
        if (!WindowManager.TryGetControl("winEditors", "lstNpcs", out var ctrl) || ctrl is not ListBox list) return;

        int total = Core.Globals.Variables.MaxMapNpcs;
        int visible = list.GetVisibleCount();
        int maxScroll = Math.Max(0, total - visible);
        int delta = GameClient.GetMouseScrollDelta();
        int step = (delta > 0) ? -3 : 3;
        list.ScrollOffset = Math.Clamp(list.ScrollOffset + step, 0, maxScroll);

        if (WindowManager.TryGetControl("winEditors", "sldNpcList", out var sbCtrl))
        {
            sbCtrl.Value = list.ScrollOffset;
        }
    }

    public static void OnNpcScrollBarMove()
    {
        if (!WindowManager.TryGetControl("winEditors", "lstNpcs", out var ctrl) || ctrl is not ListBox list) return;
        if (WindowManager.TryGetControl("winEditors", "sldNpcList", out var sbCtrl))
        {
            list.ScrollOffset = sbCtrl.Value;
        }
    }
}
