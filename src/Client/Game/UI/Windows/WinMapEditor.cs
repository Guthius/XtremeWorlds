using System.IO;
using Client.Game.UI;
using Client.Game.UI.Controls;
using Core.Globals;
using Microsoft.Xna.Framework;
using static Core.Globals.Commands;

namespace Client.Game.UI.Windows;

public class WinMapEditor
{
    private static bool _isDraggingTileset = false;
    public static int NpcSelectedSlot = 0;
    private const int WheelTileStep = 3; // scroll by 3 tiles per wheel notch

    public static void InitNpcList()
    {
        if (WindowManager.TryGetControl("winMapEditor", "cmbNpcList", out var npcCtrl) && npcCtrl is ComboBox cmbNpc)
        {
            int prev = cmbNpc.Value;
            cmbNpc.Items.Clear();
            cmbNpc.Items.Add("None");
            var npcArr = Npc.Instance;
            if (npcArr != null)
            {
                for (int i = 0; i < npcArr.Count; i++)
                {
                    var raw = npcArr[i].Name ?? string.Empty;
                    var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                    cmbNpc.Items.Add($"{i + 1}: {name}");
                }
            }
            cmbNpc.Value = (prev >= 0 && prev < cmbNpc.Items.Count) ? prev : 0;

            // Repopulate items when the dropdown is first clicked open
            cmbNpc.CallBack[(int)ControlState.MouseDown] = () => InitNpcList();

            // When selection actually changes (mouse move over list), write to map data + list
            cmbNpc.CallBack[(int)ControlState.MouseMove] = () =>
            {
                int slotIndex = NpcSelectedSlot;
                if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Npc != null && slotIndex >= 0 && slotIndex < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Npc.Length)
                {
                    int npcIndex = cmbNpc.Value - 1; // 0 = None
                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Npc[slotIndex] = npcIndex;

                    if (WindowManager.TryGetControl("winMapEditor", "lstIndex", out var lstIndex) && lstIndex is ListBox lst)
                    {
                        string name = "None";
                        if (npcIndex >= 0 && npcIndex < (Npc.Instance?.Count ?? 0))
                        {
                            var rawName = Npc.Instance?[npcIndex].Name ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(rawName)) name = rawName.Trim();
                        }
                        if (slotIndex >= 0 && slotIndex < lst.Items.Count)
                        {
                            lst.Items[slotIndex] = $"{slotIndex + 1}: {name}";
                        }
                    }
                }
            };
        }
    }

    // Rebuild Map Editor NPC slot list items once and preserve selection/scroll
    public static void RefreshMapNpcList()
    {
        if (!WindowManager.TryGetControl("winMapEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list)
            return;

        int prevIndex = Math.Clamp(NpcSelectedSlot, -1, Core.Globals.Variables.MaxMapNpcs - 1);
        int prevScroll = list.ScrollOffset;

        list.Clear();
        int total = Core.Globals.Variables.MaxMapNpcs;
        for (int slot = 0; slot < total; slot++)
        {
            string name = "None";
            try
            {
                int npcIndex = (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Npc != null && slot < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Npc.Length) ? Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Npc[slot] : -1;
                if (npcIndex >= 0 && npcIndex < Core.Globals.Variables.MaxNpcs && npcIndex < (Npc.Instance?.Count ?? 0))
                {
                    var raw = Npc.Instance[npcIndex].Name ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(raw)) name = raw.Trim();
                }
            }
            catch { /* ignore */ }

            list.AddItem($"{slot + 1}: {name}");
        }
    }

    public static void OnFillLayerClick()
    {
        var layer = (MapLayer)GameState.CurLayer;
        byte autotile = (byte)GameState.CurAutotileType;
        byte tileX = (byte)GameState.EditorTileX;
        byte tileY = (byte)GameState.EditorTileY;
        int tileset = GameState.CurTileset > 0 ? GameState.CurTileset : Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tileset;
        GameLogic.Dialogue("Map Editor", $"Fill Layer: {layer}", "Are you sure you wish to fill this layer?", DialogueType.FillLayer, DialogueStyle.YesNo, GameState.CurLayer, autotile, tileX, tileY, tileset);
    }

    // Draw the tileset preview into the picTileset PictureBox area
    public static void OnDrawTileset()
    {
        var win = WindowManager.GetWindowByName("winMapEditor");
        if (win is null) return;
        if (!WindowManager.TryGetControl("winMapEditor", "picTileset", out var ctrl)) return;

        int tilesetIndex = GameState.CurTileset;
        if (tilesetIndex <= 0) tilesetIndex = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tileset;
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
        if (WindowManager.TryGetControl("winMapEditor", "sldTilesetH", out var sbhCtrl) && sbhCtrl is ScrollBar sbh)
        {
            var maxX = System.Math.Max(0, srcW - viewW);
            sbh.Max = maxX;
            sbh.Min = 0;
            sbhCtrl.Value = System.Math.Clamp(sbhCtrl.Value, sbh.Min, sbh.Max);
            scrollX = sbhCtrl.Value;
        }
        if (WindowManager.TryGetControl("winMapEditor", "sldTilesetV", out var sbCtrl) && sbCtrl is ScrollBar sb)
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
        int selXpx = GameState.EditorTileX * Constants.TileSize;
        int selYpx = GameState.EditorTileY * Constants.TileSize;
        int selWpx = System.Math.Max(1, GameState.EditorTileWidth) * Constants.TileSize;
        int selHpx = System.Math.Max(1, GameState.EditorTileHeight) * Constants.TileSize;

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
        var win = WindowManager.GetWindowByName("winMapEditor");
        if (win is null) return;
        if (!WindowManager.TryGetControl("winMapEditor", "picTileset", out var ctrl)) return;

        int relX = GameState.CurMouseX - (win.X + ctrl.X);
        int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
        if (relX < 0 || relY < 0 || relX >= ctrl.Width || relY >= ctrl.Height) return;

        // Compute current tileset source rect and horizontal centering offset
        int tilesetIndex = GameState.CurTileset;
        if (tilesetIndex <= 0) tilesetIndex = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tileset;
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
        if (WindowManager.TryGetControl("winMapEditor", "sldTilesetH", out var sbhCtrl))
            sX = System.Math.Clamp(sbhCtrl.Value, 0, System.Math.Max(0, srcW - viewW));
        if (WindowManager.TryGetControl("winMapEditor", "sldTilesetV", out var sbvCtrl))
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
        int tileX = System.Math.Clamp(px / Constants.TileSize, 0, int.MaxValue);
        int tileY = System.Math.Clamp(py / Constants.TileSize, 0, int.MaxValue);

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
        var win = WindowManager.GetWindowByName("winMapEditor");
        if (win is null) return;
        if (!WindowManager.TryGetControl("winMapEditor", "picTileset", out var ctrl)) return;

        int relX = GameState.CurMouseX - (win.X + ctrl.X);
        int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
        if (relX < 0 || relY < 0 || relX >= ctrl.Width || relY >= ctrl.Height) return;

        int tilesetIndex = GameState.CurTileset;
        if (tilesetIndex <= 0) tilesetIndex = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tileset;
        if (tilesetIndex <= 0) tilesetIndex = 1;
        var path = System.IO.Path.Combine(DataPath.Tilesets, tilesetIndex.ToString());
        var info = GameClient.GetGfxInfo(path);
        if (info is null || info.Width <= 0 || info.Height <= 0) return;

        int srcW = info.Width;
        int srcH = info.Height;
        int viewW = ctrl.Width;
        int viewH = ctrl.Height;

        int sX = 0, sY = 0;
        if (WindowManager.TryGetControl("winMapEditor", "sldTilesetH", out var sbhCtrl))
            sX = System.Math.Clamp(sbhCtrl.Value, 0, System.Math.Max(0, srcW - viewW));
        if (WindowManager.TryGetControl("winMapEditor", "sldTilesetV", out var sbvCtrl))
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
        int curX = System.Math.Clamp(px / Constants.TileSize, 0, int.MaxValue);
        int curY = System.Math.Clamp(py / Constants.TileSize, 0, int.MaxValue);

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
        if (!WindowManager.TryGetControl("winMapEditor", "picTileset", out var ctrl)) return;
        var win = WindowManager.GetWindowByName("winMapEditor");
        if (win is null) return;

        int tilesetIndex = GameState.CurTileset;
        if (tilesetIndex <= 0) tilesetIndex = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tileset;
        if (tilesetIndex <= 0) return;

        var path = System.IO.Path.Combine(DataPath.Tilesets, tilesetIndex.ToString());
        var info = GameClient.GetGfxInfo(path);
        if (info is null || info.Width <= 0 || info.Height <= 0) return;

        int viewW = ctrl.Width;
        int viewH = ctrl.Height;
        int maxY = System.Math.Max(0, info.Height - viewH);
        int maxX = System.Math.Max(0, info.Width - viewW);

        int delta = GameClient.GetMouseScrollDelta();
        int stepPx = Constants.TileSize * (delta > 0 ? -WheelTileStep : WheelTileStep);

        // If Shift is held, scroll horizontally; otherwise scroll vertically
        bool shift = GameState.VbKeyShift;
        if (shift)
        {
            if (WindowManager.TryGetControl("winMapEditor", "sldTilesetH", out var sbhCtrl) && sbhCtrl is ScrollBar sbh)
            {
                sbh.Min = 0; sbh.Max = maxX;
                int newVal = System.Math.Clamp(sbhCtrl.Value + stepPx, sbh.Min, sbh.Max);
                sbhCtrl.Value = newVal;
            }
        }
        else
        {
            if (WindowManager.TryGetControl("winMapEditor", "sldTilesetV", out var sbCtrl) && sbCtrl is ScrollBar sb)
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

    public static void OnLoad()
    {
        var map = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)];

        GameState.CurTileset = 1;
        map.Tileset = 1;

        // Name
        if (WindowManager.TryGetControl("winMapEditor", "txtName", out var nameCtrl) && nameCtrl is TextBox txtName)
            txtName.Text = map.Name ?? string.Empty;

        // Music combo
        if (WindowManager.TryGetControl("winMapEditor", "cmbMusic", out var musicCtrl) && musicCtrl is ComboBox cmbMusic)
        {
            // find the index of the current map music in the combo
            int id = 0;
            for (int i = 0; i < cmbMusic.Items.Count; i++)
            {
                if (string.Equals(cmbMusic.Items[i], map.Music, StringComparison.OrdinalIgnoreCase))
                {
                    id = i;
                    break;
                }
            }
            cmbMusic.Value = id;
        }

        // Shop / Moral combos
        if (WindowManager.TryGetControl("winMapEditor", "lstShop", out var shopCtrl) && shopCtrl is ComboBox cmbShop)
            cmbShop.Value = Math.Clamp(map.Shop, 0, Math.Max(0, cmbShop.Items.Count - 1));

        if (WindowManager.TryGetControl("winMapEditor", "lstMoral", out var moralCtrl) && moralCtrl is ComboBox cmbMoral)
            cmbMoral.Value = Math.Clamp(map.Moral, 0, Math.Max(0, cmbMoral.Items.Count - 1));

        // Links
        int maxMaps = Variables.MaxMaps - 1;
        if (WindowManager.TryGetControl("winMapEditor", "txtUp", out var txtUp))
            txtUp.Text = map.Up.ToString();
        if (WindowManager.TryGetControl("winMapEditor", "txtDown", out var txtDown))
            txtDown.Text = map.Down.ToString();
        if (WindowManager.TryGetControl("winMapEditor", "txtLeft", out var txtLeft))
            txtLeft.Text = map.Left.ToString();
        if (WindowManager.TryGetControl("winMapEditor", "txtRight", out var txtRight))
            txtRight.Text = map.Right.ToString();

        // Boot map/coords
        if (WindowManager.TryGetControl("winMapEditor", "txtBootMap", out var txtBootMap))
            txtBootMap.Text = map.BootMap.ToString();
        if (WindowManager.TryGetControl("winMapEditor", "txtBootX", out var txtBootX))
            txtBootX.Text = map.BootX.ToString();
        if (WindowManager.TryGetControl("winMapEditor", "txtBootY", out var txtBootY))
            txtBootY.Text = map.BootY.ToString();

        // Flags
        if (WindowManager.TryGetControl("winMapEditor", "chkNoMapRespawn", out var chkNoMapRespawn))
            chkNoMapRespawn.Value = map.NoRespawn ? 1 : 0;
        if (WindowManager.TryGetControl("winMapEditor", "chkIndoors", out var chkIndoors))
            chkIndoors.Value = map.Indoors ? 1 : 0;

        // Size
        if (WindowManager.TryGetControl("winMapEditor", "txtMaxX", out var txtMaxX))
            txtMaxX.Text = map.MaxX.ToString();
        if (WindowManager.TryGetControl("winMapEditor", "txtMaxY", out var txtMaxY))
            txtMaxY.Text = map.MaxY.ToString();

        // Tileset state + NPC list
        GameState.CurTileset = map.Tileset;
    }

    public static void OnNpcListMouseDown()
    {
        if (!WindowManager.TryGetControl("winMapEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list) return;
        var win = WindowManager.GetWindowByName("winMapEditor");
        if (win is null) return;

        int relY = GameState.CurMouseY - (win.Y + list.Y);
        int index = list.GetItemIndexAtPosition(relY);
        if (index < 0 || index >= Core.Globals.Variables.MaxMapNpcs) return;

        NpcSelectedSlot = index;
        list.SelectedIndex = index;
        list.EnsureVisible(index);

        if (WindowManager.TryGetControl("winMapEditor", "cmbNpcList", out var npcCtrl) && npcCtrl is ComboBox cmbNpc)
        {
            int assigned = -1;
            if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Npc != null && index < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Npc.Length)
                assigned = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Npc[index];
            int desired = (assigned >= 0) ? assigned + 1 : 0;
            desired = Math.Clamp(desired, 0, cmbNpc.Items.Count - 1);
            cmbNpc.Value = desired;
        }
    }

    public static void OnNpcListMouseWheel()
    {
        if (!WindowManager.TryGetControl("winMapEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list) return;
        int visible = list.GetVisibleCount();
        int max = Math.Max(0, list.Items.Count - visible);
        int delta = GameClient.GetMouseScrollDelta();
        int step = (delta > 0) ? -1 : 1;
        list.ScrollOffset = Math.Clamp(list.ScrollOffset + step, 0, max);
        if (WindowManager.TryGetControl("winMapEditor", "sldNpcList", out var sld)) sld.Value = list.ScrollOffset;
    }

    public static void OnNpcScrollBarMove()
    {
        if (!WindowManager.TryGetControl("winMapEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list) return;

        if (WindowManager.TryGetControl("winMapEditor", "sldNpcList", out var sldCtrl))
        {
            int visible = list.GetVisibleCount();
            int max = Math.Max(0, list.Items.Count - visible);

            if (sldCtrl is ScrollBar sb)
            {
                sb.Min = 0;
                sb.Max = max;
                list.ScrollOffset = Math.Clamp(sb.Value, sb.Min, sb.Max);
            }
            else
            {
                // Fallback if control isn't a ScrollBar: clamp using 0..max
                list.ScrollOffset = Math.Clamp(sldCtrl.Value, 0, max);
            }
        }
    }

    public static void OnCancel()
    {
        Editors.MapEditorCancel();
        WindowManager.HideWindow("winMapEditor");
    }
}
