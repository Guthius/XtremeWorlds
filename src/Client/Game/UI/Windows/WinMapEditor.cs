using System.IO;
using Client;
using Client.Game.UI;
using Client.Game.UI.Controls;
using Client.Net;
using Core;
using Core.Globals;
using Core.Net;
using Microsoft.Xna.Framework;
using static Core.Globals.Commands;
using Type = Core.Globals.Type;

namespace Client.Game.UI.Windows;

public class WinMapEditor
{
    private static bool _isDraggingTileset = false;
    private static float tilesetOffsetX = 0;
    private static float tilesetOffsetY = 0;
    public static int NpcSelectedSlot = 0;
    private const int WheelTileStep = 3; // scroll by 3 tiles per wheel notch

    public static void OnSelect(float X, float Y)
    {
        if (GameClient.IsMouseButtonDown(MouseButton.Left)) // Primary (Left) Mouse Button
        {
            // Choosing from the tileset palette uses the contiguous-tileset stamping path.
            // Clear any previously captured map-stamp.
            GameState.EditorStampActive = false;
            GameState.EditorStampWidth = 0;
            GameState.EditorStampHeight = 0;
            GameState.EditorStampTileset = null;
            GameState.EditorStampX = null;
            GameState.EditorStampY = null;
            GameState.EditorStampAutoTile = null;

            GameState.EditorTileWidth = 1;
            GameState.EditorTileHeight = 1;

            if (GameState.CurAutotileType > 0)
            {
                switch (GameState.CurAutotileType)
                {
                    case 1: // autotile
                        GameState.EditorTileWidth = 2;
                        GameState.EditorTileHeight = 3;
                        break;
                    case 2: // fake autotile
                        GameState.EditorTileWidth = 1;
                        GameState.EditorTileHeight = 1;
                        break;
                    case 3: // animated
                        GameState.EditorTileWidth = 6;
                        GameState.EditorTileHeight = 3;
                        break;
                    case 4: // cliff
                        GameState.EditorTileWidth = 2;
                        GameState.EditorTileHeight = 2;
                        break;
                    case 5: // waterfall
                        GameState.EditorTileWidth = 2;
                        GameState.EditorTileHeight = 3;
                        break;
                }
            }

            // Corrected: Use integer division to get the tile index, not Math.Round
            GameState.EditorTileX = (int)((X + tilesetOffsetX) / Constants.TileSize);
            GameState.EditorTileY = (int)((Y + tilesetOffsetY) / Constants.TileSize);

            GameState.EditorTileSelStart = new Microsoft.Xna.Framework.Point(GameState.EditorTileX, GameState.EditorTileY);
            GameState.EditorTileSelEnd = new Microsoft.Xna.Framework.Point(
                GameState.EditorTileX + GameState.EditorTileWidth,
                GameState.EditorTileY + GameState.EditorTileHeight
            );
        }
    }

    public static void OnCancel()
    {
        if (GameState.MyEditorType != EditorType.Map)
        {
            return;
        }

        // Discard any queued resize changes when cancelling.
        GameState.MapResizePending = false;

        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CNeedMap);
        packetWriter.WriteInt32(1);

        Network.Send(packetWriter);

        GameState.MyEditorType = EditorType.None;
        GameState.GettingMap = true;

        Sender.CloseEditor();

        // show gui
        WindowManager.ShowWindow("winHotbar", resetPosition: false);
        WindowManager.ShowWindow("winMenu", resetPosition: false);
        WindowManager.ShowWindow("winBars", resetPosition: false);
        WinChat.Hide();

        GameState.TileHistoryHighIndex = 0;
        GameState.TileHistoryIndex = 0;
    }

    public static void OnSend()
    {
        // Apply queued resize (if any) only at save time.
        if (GameState.MyEditorType == EditorType.Map && GameState.MapResizePending)
        {
            var nx = (byte)Math.Clamp(GameState.MapResizePendingX, 1, byte.MaxValue);
            var ny = (byte)Math.Clamp(GameState.MapResizePendingY, 1, byte.MaxValue);
            GameState.MapResizePending = false;
            ResizeMap(nx, ny, updateControls: false);
        }

        // Send the edited map to the server
        Sender.Map();

        GameState.MyEditorType = EditorType.None;
        // Request the refreshed map data immediately so we don't linger on a black screen
        try
        {
            var packetWriter = new PacketWriter(8);
            packetWriter.WriteEnum(Packets.ClientPackets.CNeedMap);
            packetWriter.WriteInt32(1);
            Network.Send(packetWriter);
        }
        catch { }

        GameState.GettingMap = true;
        Sender.CloseEditor();

        // show gui
        WindowManager.ShowWindow("winHotbar", resetPosition: false);
        WindowManager.ShowWindow("winMenu", resetPosition: false);
        WindowManager.ShowWindow("winBars", resetPosition: false);
        WinChat.Hide();

        GameState.TileHistoryHighIndex = 0;
        GameState.TileHistoryIndex = 0;
    }

    public static void OnSet(int x, int y, int CurLayer, bool multitile = false, byte theAutotile = 0, byte eraseTile = 0)
    {
        int x2;
        int y2;
        int newTileX;
        int newTileY;

        // If we have a captured stamp (eyedropper), paint exact tile data.
        // Only applies to normal tiles (no forced autotile mode) and when not erasing.
        if (GameState.EditorStampActive && theAutotile == 0 && GameState.CurAutotileType == 0 && !Conversions.ToBoolean(eraseTile))
        {
            int stampW = Math.Max(1, GameState.EditorStampWidth);
            int stampH = Math.Max(1, GameState.EditorStampHeight);

            if (GameState.EditorStampTileset is not null && GameState.EditorStampX is not null && GameState.EditorStampY is not null && GameState.EditorStampAutoTile is not null)
            {
                bool anyAutotile = false;
                int mapIndex = GetPlayerMap(GameState.MyIndex);
                var map = Client.Map.Instance[mapIndex];

                for (int dy = 0; dy < stampH; dy++)
                {
                    for (int dx = 0; dx < stampW; dx++)
                    {
                        int tx = x + dx;
                        int ty = y + dy;
                        if (tx < 0 || ty < 0 || tx >= map.MaxX || ty >= map.MaxY) continue;

                        ref var tile = ref map.Tile[tx, ty];
                        if (tile.Layer == null || tile.Layer.Length <= CurLayer) continue;

                        tile.Layer[CurLayer].Tileset = GameState.EditorStampTileset[dx, dy];
                        tile.Layer[CurLayer].X = GameState.EditorStampX[dx, dy];
                        tile.Layer[CurLayer].Y = GameState.EditorStampY[dx, dy];
                        tile.Layer[CurLayer].AutoTile = GameState.EditorStampAutoTile[dx, dy];

                        if (tile.Layer[CurLayer].AutoTile > 0) anyAutotile = true;
                        Autotile.CacheRenderState(tx, ty, CurLayer);
                    }
                }

                if (anyAutotile)
                {
                    Autotile.InitAutotiles();
                }

                return;
            }
        }

        if (multitile)
        {
            newTileX = GameState.EditorTileX;
            newTileY = GameState.EditorTileY;
        }
        else
        {
            newTileX = GameState.EditorTileX;
            newTileY = GameState.EditorTileY;
        }

        if (theAutotile > 0)
        {
            newTileX = GameState.EditorTileX;
            newTileY = GameState.EditorTileY;
        }

        // see if the tileset is valid
        if (GameState.CurTileset < 0)
        {
            GameState.CurTileset = 0;
        }

        // calculate the tile's x/y on the tileset
        x2 = 0;
        y2 = 0;

        // loop through the selection
        var count = GameState.CurY + GameState.EditorTileHeight;
        for (int y3 = GameState.CurY; y3 < count; y3++)
        {
            if (y3 >= 0 & y3 < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY)
            {
                y2 = y3 - GameState.CurY;
                x2 = 0;

                var count2 = GameState.CurX + GameState.EditorTileWidth;
                for (x = GameState.CurX; x < count2; x++)
                {
                    if (x >= 0 & x < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX)
                    {
                        if (y3 >= 0 & y3 < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY)
                        {
                            ref var instance2 = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y3];
                            instance2.Layer[CurLayer].X = newTileX + x2;
                            instance2.Layer[CurLayer].Y = newTileY + y2;
                            if (Conversions.ToBoolean(eraseTile))
                            {
                                instance2.Layer[CurLayer].Tileset = 0;
                            }
                            else
                            {
                                instance2.Layer[CurLayer].Tileset = GameState.CurTileset;
                            }
                            instance2.Layer[CurLayer].AutoTile = 0;
                            Autotile.CacheRenderState(x, y3, CurLayer);
                        }
                    }
                    x2 += 1;
                }
                y2 += 1;
            }
        }
    }

    public static void OnHistory()
    {
        if (GameState.TileHistoryIndex <= 0)
            GameState.TileHistoryIndex = 0;

        if (GameState.TileHistoryIndex >= GameState.MaxTileHistory - 1)
        {
            for (int i = 0; i < GameState.TileHistoryIndex; i++)
            {
                Data.TileHistory![(int)i] = Data.TileHistory![(int)(i + 1)];
            }
        }
        else
        {
            GameState.TileHistoryIndex++;
            GameState.TileHistoryHighIndex++;

            if (GameState.TileHistoryHighIndex > GameState.MaxTileHistory)
                GameState.TileHistoryHighIndex = GameState.MaxTileHistory;

        }

    }

    public static void OnClear(MapLayer layer)
    {
        GameLogic.Dialogue("Map Editor", "Clear Layer: " + layer.ToString(), "Are you sure you wish to clear this layer?", DialogueType.ClearLayer, DialogueStyle.YesNo, GameState.CurLayer, GameState.CurAutotileType);
    }

    public static void OnFill(MapLayer layer, byte theAutotile = 0, byte tileX = 0, byte tileY = 0)
    {
        GameLogic.Dialogue("Map Editor", "Fill Layer: " + layer.ToString(), "Are you sure you wish to fill this layer?", DialogueType.FillLayer, DialogueStyle.YesNo, GameState.CurLayer, GameState.CurAutotileType, tileX, tileY, GameState.CurTileset);
    }

    public static void OnEyeDropper()
    {
        int CurLayer;

        CurLayer = GameState.CurLayer;

        {
            ref var instance = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[GameState.CurX, GameState.CurY];
            // Set tileset and directly apply the picked tile indices without invoking tileset-offset logic
            GameState.CurTileset = instance.Layer[CurLayer].Tileset;
            GameState.EditorTileX = instance.Layer[CurLayer].X;
            GameState.EditorTileY = instance.Layer[CurLayer].Y;
            GameState.EditorTileWidth = 1;
            GameState.EditorTileHeight = 1;
            GameState.EditorTileSelStart = new Microsoft.Xna.Framework.Point(GameState.EditorTileX, GameState.EditorTileY);
            GameState.EditorTileSelEnd = new Microsoft.Xna.Framework.Point(GameState.EditorTileX + 1, GameState.EditorTileY + 1);

            // Track the map selection for red outline feedback.
            GameState.EyeDropperSelStart = new Microsoft.Xna.Framework.Point(GameState.CurX, GameState.CurY);
            GameState.EyeDropperSelEnd = GameState.EyeDropperSelStart;

            // Also populate the stamp buffer for single-tile paint.
            GameState.EditorStampActive = true;
            GameState.EditorStampWidth = 1;
            GameState.EditorStampHeight = 1;
            GameState.EditorStampTileset = new int[1, 1];
            GameState.EditorStampX = new int[1, 1];
            GameState.EditorStampY = new int[1, 1];
            GameState.EditorStampAutoTile = new byte[1, 1];
            GameState.EditorStampTileset[0, 0] = instance.Layer[CurLayer].Tileset;
            GameState.EditorStampX[0, 0] = instance.Layer[CurLayer].X;
            GameState.EditorStampY[0, 0] = instance.Layer[CurLayer].Y;
            GameState.EditorStampAutoTile[0, 0] = (byte)instance.Layer[CurLayer].AutoTile;

            // After capture, switch back to painting.
            GameState.EyeDropperSelecting = false;
            GameState.EyeDropper = false;
        }
    }

    public static void OnEyeDropper(int startX, int startY, int endX, int endY)
    {
        int layer = GameState.CurLayer;
        int mapIndex = GetPlayerMap(GameState.MyIndex);
        var map = Client.Map.Instance[mapIndex];

        int minX = Math.Min(startX, endX);
        int minY = Math.Min(startY, endY);
        int maxX = Math.Max(startX, endX);
        int maxY = Math.Max(startY, endY);

        // Clamp to map bounds
        minX = Math.Clamp(minX, 0, map.MaxX - 1);
        minY = Math.Clamp(minY, 0, map.MaxY - 1);
        maxX = Math.Clamp(maxX, 0, map.MaxX - 1);
        maxY = Math.Clamp(maxY, 0, map.MaxY - 1);

        int width = (maxX - minX) + 1;
        int height = (maxY - minY) + 1;
        // Capture exact tiles as a stamp buffer (supports mixed tilesets and arbitrary atlas coords).
        GameState.EditorStampActive = true;
        GameState.EditorStampWidth = width;
        GameState.EditorStampHeight = height;
        GameState.EditorStampTileset = new int[width, height];
        GameState.EditorStampX = new int[width, height];
        GameState.EditorStampY = new int[width, height];
        GameState.EditorStampAutoTile = new byte[width, height];

        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                ref var t = ref map.Tile[minX + dx, minY + dy];
                var l = t.Layer[layer];
                GameState.EditorStampTileset[dx, dy] = l.Tileset;
                GameState.EditorStampX[dx, dy] = l.X;
                GameState.EditorStampY[dx, dy] = l.Y;
                GameState.EditorStampAutoTile[dx, dy] = (byte)l.AutoTile;
            }
        }

        // Also set selection indicator to the top-left tile of the stamp.
        ref var first = ref map.Tile[minX, minY];
        GameState.CurTileset = first.Layer[layer].Tileset;
        GameState.EditorTileX = first.Layer[layer].X;
        GameState.EditorTileY = first.Layer[layer].Y;
        GameState.EditorTileWidth = 1;
        GameState.EditorTileHeight = 1;
        GameState.EditorTileSelStart = new Microsoft.Xna.Framework.Point(GameState.EditorTileX, GameState.EditorTileY);
        GameState.EditorTileSelEnd = new Microsoft.Xna.Framework.Point(GameState.EditorTileX + 1, GameState.EditorTileY + 1);

        // Track selection for red outline feedback.
        GameState.EyeDropperSelStart = new Microsoft.Xna.Framework.Point(minX, minY);
        GameState.EyeDropperSelEnd = new Microsoft.Xna.Framework.Point(maxX, maxY);

        GameState.EyeDropperSelecting = false;
        GameState.EyeDropper = false;
    }

    public static void OnCopy()
    {
        int i;
        int x;
        int y;
        // Get the number of layers from the MapLayer enum
        int layerCount = Enum.GetValues(typeof(MapLayer)).Length;

        // Always copy (no implicit paste on second click)
        Data.TempTile = new Type.Tile[Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX, Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY];
        GameState.TmpMaxX = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX;
        GameState.TmpMaxY = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY;

        var count = (int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX;
        for (x = 0; x < count; x++)
        {
            var count2 = (int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY;
            for (y = 0; y < count2; y++)
            {
                ref var instance = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y];
                Data.TempTile[x, y].Layer = new Type.Layer[layerCount];

                Data.TempTile[x, y].Data1 = instance.Data1;
                Data.TempTile[x, y].Data2 = instance.Data2;
                Data.TempTile[x, y].Data3 = instance.Data3;
                Data.TempTile[x, y].Type = instance.Type;
                Data.TempTile[x, y].Data1_2 = instance.Data1_2;
                Data.TempTile[x, y].Data2_2 = instance.Data2_2;
                Data.TempTile[x, y].Data3_2 = instance.Data3_2;
                Data.TempTile[x, y].Type2 = instance.Type2;
                Data.TempTile[x, y].DirBlock = instance.DirBlock;

                for (i = 0; i < layerCount; i++)
                {
                    Data.TempTile[x, y].Layer[i].X = instance.Layer[i].X;
                    Data.TempTile[x, y].Layer[i].Y = instance.Layer[i].Y;
                    Data.TempTile[x, y].Layer[i].Tileset = instance.Layer[i].Tileset;
                    Data.TempTile[x, y].Layer[i].AutoTile = instance.Layer[i].AutoTile;
                }
            }
        }

        GameState.CopyMap = true;
        GameLogic.Dialogue("Map Editor", "Map Copy:", "Copied current map to clipboard.", DialogueType.CopyMap, (byte)DialogueStyle.Okay);
    }

    public static void OnPaste()
    {
        if (Data.TempTile == null)
        {
            GameLogic.Dialogue("Map Editor", "Map Paste:", "No copied map available.", DialogueType.PasteMap, (byte)DialogueStyle.Okay);
            return;
        }

        int i, x, y;
        int layerCount = Enum.GetValues(typeof(MapLayer)).Length;

        Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX = GameState.TmpMaxX;
        Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY = GameState.TmpMaxY;

        var count2 = (int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX;
        for (x = 0; x < count2; x++)
        {
            var count3 = (int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY;
            for (y = 0; y < count3; y++)
            {
                ref var instance1 = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y];
                Array.Resize(ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer, layerCount);
                Array.Resize(ref Data.Autotile![x, y].Layer, layerCount);

                instance1.Data1 = Data.TempTile![x, y].Data1;
                instance1.Data2 = Data.TempTile![x, y].Data2;
                instance1.Data3 = Data.TempTile![x, y].Data3;
                instance1.Type = Data.TempTile![x, y].Type;
                instance1.Data1_2 = Data.TempTile![x, y].Data1_2;
                instance1.Data2_2 = Data.TempTile![x, y].Data2_2;
                instance1.Data3_2 = Data.TempTile![x, y].Data3_2;
                instance1.Type2 = Data.TempTile![x, y].Type2;
                instance1.DirBlock = Data.TempTile![x, y].DirBlock;

                for (i = 0; i < layerCount; i++)
                {
                    instance1.Layer[i].X = Data.TempTile![x, y].Layer[i].X;
                    instance1.Layer[i].Y = Data.TempTile![x, y].Layer[i].Y;
                    instance1.Layer[i].Tileset = Data.TempTile![x, y].Layer[i].Tileset;
                    instance1.Layer[i].AutoTile = Data.TempTile![x, y].Layer[i].AutoTile;
                    Autotile.CacheRenderState(x, y, i);
                }
            }
        }

        GameLogic.Dialogue("Map Editor", "Map Paste:", "Map has been updated.", DialogueType.PasteMap, (byte)DialogueStyle.Okay);
        Autotile.InitAutotiles();
    }

    /// <summary>
    /// Replaces the X/Y coordinates of all tiles in the given layer with the specified values.
    /// </summary>
    /// <param name="layer">The layer to update.</param>
    /// <param name="tileX">The new X coordinate to set.</param>
    /// <param name="tileY">The new Y coordinate to set.</param>
    public static void OnReplace(MapLayer layer, int tileX, int tileY, Core.Globals.Type.Tile oldTile)
    {
        int maxX = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX;
        int maxY = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY;

        for (int x = 0; x < maxX; x++)
        {
            for (int y = 0; y < maxY; y++)
            {
                ref var tile = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y];
                if ((int)MapEditorTab.Tiles == GameState.MapEditorTab)
                {
                    if (tile.Layer[(int)layer].X == oldTile.Layer[(int)layer].X && tile.Layer[(int)layer].Y == oldTile.Layer[(int)layer].Y)
                    {
                        if (GameClient.IsMouseButtonDown(MouseButton.Left))
                        {
                            tile.Layer[(int)layer].X = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[tileX, tileY].Layer[(int)layer].X;
                            tile.Layer[(int)layer].Y = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[tileX, tileY].Layer[(int)layer].Y;
                            tile.Layer[(int)layer].Tileset = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[tileX, tileY].Layer[(int)layer].Tileset;
                        }
                        else if (GameClient.IsMouseButtonDown(MouseButton.Right))
                        {
                            tile.Layer[(int)layer].X = 0;
                            tile.Layer[(int)layer].Y = 0;
                            tile.Layer[(int)layer].Tileset = 0;
                        }
                        else
                        {
                            return; // No mouse button pressed, exit early
                        }
                    }
                }
            }
        }
    }

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
                int npcIndex = (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Npc != null && slot < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Npc.Length)
                    ? Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Npc[slot]
                    : -1;

                var npcs = Npc.Instance;
                if (npcs is not null && npcIndex >= 0 && npcIndex < Core.Globals.Variables.MaxNpcs && npcIndex < npcs.Count)
                {
                    var raw = npcs[npcIndex].Name ?? string.Empty;
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

    public static void QueueResizeFromControls()
    {
        if (!WindowManager.TryGetControl("winMapEditor", "txtMaxX", out var xCtrl) || xCtrl is not TextBox)
            return;
        if (!WindowManager.TryGetControl("winMapEditor", "txtMaxY", out var yCtrl) || yCtrl is not TextBox)
            return;

        if (!int.TryParse(xCtrl.Text?.Trim(), out var nx)) return;
        if (!int.TryParse(yCtrl.Text?.Trim(), out var ny)) return;

        nx = Math.Clamp(nx, 1, byte.MaxValue);
        ny = Math.Clamp(ny, 1, byte.MaxValue);

        GameState.MapResizePendingX = nx;
        GameState.MapResizePendingY = ny;
        GameState.MapResizeLastEditTick = Client.General.GetTickCount();
        GameState.MapResizePending = true;
    }

    public static void ResizeMap(byte newMaxX, byte newMaxY, bool updateControls = true)
    {
        var mapIndex = GetPlayerMap(GameState.MyIndex);
        if (Client.Map.Instance.Count <= mapIndex)
            return;

        var map = Client.Map.Instance[mapIndex];
        if (newMaxX < 1) newMaxX = 1;
        if (newMaxY < 1) newMaxY = 1;

        if (map.MaxX == newMaxX && map.MaxY == newMaxY)
            return;

        var oldTiles = map.Tile;
        var oldMaxX = map.MaxX;
        var oldMaxY = map.MaxY;

        map.MaxX = newMaxX;
        map.MaxY = newMaxY;

        var newTiles = new Type.Tile[map.MaxX, map.MaxY];

        // Ensure undo history arrays match new dimensions
        if (Data.TileHistory == null || Data.TileHistory.Length != GameState.MaxTileHistory)
            Data.TileHistory = new Type.TileHistory[GameState.MaxTileHistory];

        for (int i = 0; i < GameState.MaxTileHistory; i++)
        {
            Data.TileHistory[i].Tile = new Type.Tile[map.MaxX, map.MaxY];
        }

        // Recreate autotile cache for new size
        Data.Autotile = new Type.Autotile[map.MaxX, map.MaxY];

        int layerCount = Enum.GetValues(typeof(MapLayer)).Length;
        int copyMaxX = Math.Min(oldMaxX, map.MaxX);
        int copyMaxY = Math.Min(oldMaxY, map.MaxY);

        for (int x = 0; x < map.MaxX; x++)
        {
            for (int y = 0; y < map.MaxY; y++)
            {
                // Always initialize supporting caches to correct dimensions.
                Data.Autotile[x, y].Layer = new Type.QuarterTile[layerCount];
                for (int i = 0; i < GameState.MaxTileHistory; i++)
                {
                    Data.TileHistory[i].Tile[x, y].Layer = new Type.Layer[layerCount];
                }

                // Preserve old tile data when possible.
                if (oldTiles != null && x < copyMaxX && y < copyMaxY)
                {
                    var oldTile = oldTiles[x, y];
                    newTiles[x, y] = oldTile;

                    // Ensure layer array is always present and correctly sized.
                    var resizedLayers = new Type.Layer[layerCount];
                    if (oldTile.Layer != null)
                    {
                        Array.Copy(oldTile.Layer, resizedLayers, Math.Min(oldTile.Layer.Length, layerCount));
                    }
                    newTiles[x, y].Layer = resizedLayers;
                }
                else
                {
                    // New blank tile.
                    newTiles[x, y].Layer = new Type.Layer[layerCount];
                }
            }
        }

        map.Tile = newTiles;

        // Clamp boot position into bounds
        if (map.BootX >= map.MaxX) map.BootX = (byte)Math.Max(0, map.MaxX - 1);
        if (map.BootY >= map.MaxY) map.BootY = (byte)Math.Max(0, map.MaxY - 1);

        if (updateControls)
        {
            if (WindowManager.TryGetControl("winMapEditor", "txtMaxX", out var tMaxX))
                tMaxX.Text = map.MaxX.ToString();
            if (WindowManager.TryGetControl("winMapEditor", "txtMaxY", out var tMaxY))
                tMaxY.Text = map.MaxY.ToString();
        }

        try { Autotile.InitAutotiles(); } catch { }
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

        // Choosing from the tileset palette should override any captured map stamp.
        // Also disable eyedropper so the next click on the map paints as expected.
        GameState.EyeDropperSelecting = false;
        GameState.EyeDropper = false;
        GameState.EditorStampActive = false;
        GameState.EditorStampWidth = 0;
        GameState.EditorStampHeight = 0;
        GameState.EditorStampTileset = null;
        GameState.EditorStampX = null;
        GameState.EditorStampY = null;
        GameState.EditorStampAutoTile = null;

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
        int maxMaps = Core.Globals.Variables.MaxMaps - 1;
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

        // Zoom bounds (map editor only)
        if (WindowManager.TryGetControl("winMapEditor", "txtMinZoom", out var txtMinZoom))
            txtMinZoom.Text = (map.MinZoom <= 0 ? 0.5f : map.MinZoom).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        if (WindowManager.TryGetControl("winMapEditor", "txtMaxZoom", out var txtMaxZoom))
            txtMaxZoom.Text = (map.MaxZoom <= 0 ? 2.0f : map.MaxZoom).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

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

    public static void OnClose()
    {
        OnCancel();
        WindowManager.HideWindow("winMapEditor");
    }
}
