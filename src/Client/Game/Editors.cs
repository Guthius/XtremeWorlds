using System;
using Client.Game.UI;
using Client.Game.UI.Windows;
using Client.Net;
using Core;
using Core.Globals;
using Core.Net;
using Type = Core.Globals.Type;
using static Core.Globals.Commands;

namespace Client
{

    public class Editors
    {
        private static float tilesetOffsetX = 0;
        private static float tilesetOffsetY = 0;

        public static int? PromptIndex(object? owner, string title, string prompt, int min, int max, int defaultValue)
        {
            // The client UI is event-driven and does not currently expose a synchronous modal input API.
            // Keep this as a simple, safe fallback so editor features compile and remain usable.
            if (min > max)
            {
                (min, max) = (max, min);
            }

            var value = defaultValue;
            if (value < min) value = min;
            if (value > max) value = max;
            return value;
        }

        public static void MapEditorChooseTile(float X, float Y)
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

        public static void MouseDown(int x, int y, bool movedMouse = true)
        {
            int i;
            bool isModified = false;

            // Bounds check for both CurX/CurY and x/y
            if (GameState.CurX < 0 || GameState.CurY < 0 || GameState.CurX >= Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX || GameState.CurY >= Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY)
                return;
                
            if (x < 0 || y < 0 || x >= Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX || y >= Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY)
                return;

            if (!GameLogic.IsInBounds())
                return;

            if (GameState.EyeDropper)
            {
                // Drag-select a rectangle on the map; finalize on mouse release.
                if (GameClient.IsMouseButtonDown(MouseButton.Left))
                {
                    if (!GameState.EyeDropperSelecting)
                    {
                        GameState.EyeDropperSelecting = true;
                        GameState.EyeDropperSelStart = new Microsoft.Xna.Framework.Point(x, y);
                        GameState.EyeDropperSelEnd = GameState.EyeDropperSelStart;
                    }
                    else
                    {
                        GameState.EyeDropperSelEnd = new Microsoft.Xna.Framework.Point(x, y);
                    }

                    return;
                }

                if (GameState.EyeDropperSelecting)
                {
                    GameState.EyeDropperSelecting = false;
                    MapEditorEyeDropper(GameState.EyeDropperSelStart.X, GameState.EyeDropperSelStart.Y, GameState.EyeDropperSelEnd.X, GameState.EyeDropperSelEnd.Y);
                    return;
                }
            }

            var instance = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y];

            if (GameClient.IsMouseButtonDown(MouseButton.Left))
            {
                // Only allow Set Tile on Tiles tab
                if (GameState.MapEditorTab == (int)MapEditorTab.Tiles)
                {
                    if (GameState.EditorTileWidth == 1 & GameState.EditorTileHeight == 1) // single tile
                    {
                        MapEditorSetTile(GameState.CurX, GameState.CurY, GameState.CurLayer, false, (byte)GameState.CurAutotileType);
                    }
                    else if (GameState.CurAutotileType == 0) // multi tile!
                    {
                        MapEditorSetTile(GameState.CurX, GameState.CurY, GameState.CurLayer, true);
                    }
                    else
                    {
                        MapEditorSetTile(GameState.CurX, GameState.CurY, GameState.CurLayer, true, (byte)GameState.CurAutotileType);
                    }
                }
                // Only allow attribute placement on Attributes tab
                else if (GameState.MapEditorTab == (int)MapEditorTab.Attributes)
                {
                    ref var instance1 = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[GameState.CurX, GameState.CurY];

                    if (GameState.OptInfo)
                    {
                        if (GameState.Info == false)
                        {
                            if (GameState.EditorAttribute == 1)
                            {
                                GameLogic.Dialogue("Map Editor", "Info: " + System.Enum.GetName(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[GameState.CurX, GameState.CurY].Type), " Data 1: " + Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[GameState.CurX, GameState.CurY].Data1 + " Data 2: " + Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[GameState.CurX, GameState.CurY].Data2 + " Data 3: " + Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[GameState.CurX, GameState.CurY].Data3, DialogueType.Information, (byte)DialogueStyle.Okay);
                            }
                            else
                            {
                                GameLogic.Dialogue("Map Editor", "Info: " + System.Enum.GetName(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[GameState.CurX, GameState.CurY].Type2), " Data 1: " + Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[GameState.CurX, GameState.CurY].Data1_2 + " Data 2: " + Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[GameState.CurX, GameState.CurY].Data2_2 + " Data 3: " + Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[GameState.CurX, GameState.CurY].Data3_2, DialogueType.Information, (byte)DialogueStyle.Okay);
                            }
                        }
                    }
                    
                    // blocked tile
                    if (GameState.OptBlocked)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            instance1.Type = TileType.Blocked;
                        }
                        else
                        {
                            instance1.Type2 = TileType.Blocked;
                        }
                    }

                    // warp tile
                    if (GameState.OptWarp)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            instance1.Type = TileType.Warp;
                            instance1.Data1 = GameState.EditorWarpMap;
                            instance1.Data2 = GameState.EditorWarpX;
                            instance1.Data3 = GameState.EditorWarpY;
                        }
                        else
                        {
                            instance1.Type2 = TileType.Warp;
                            instance1.Data1_2 = GameState.EditorWarpMap;
                            instance1.Data2_2 = GameState.EditorWarpX;
                            instance1.Data3_2 = GameState.EditorWarpY;
                        }
                    }

                    // item spawn
                    if (GameState.OptItem)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            instance1.Type = TileType.Item;
                            instance1.Data1 = GameState.ItemEditor;
                            instance1.Data2 = GameState.ItemEditorValue;
                            instance1.Data3 = 0;
                        }
                        else
                        {
                            instance1.Type2 = TileType.Item;
                            instance1.Data1_2 = GameState.ItemEditor;
                            instance1.Data2_2 = GameState.ItemEditorValue;
                            instance1.Data3_2 = 0;
                        }
                    }

                    // Npc avoid
                    if (GameState.OptNpcAvoid)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            instance1.Type = TileType.NpcAvoid;
                            instance1.Data1 = 0;
                            instance1.Data2 = 0;
                            instance1.Data3 = 0;
                        }
                        else
                        {
                            instance1.Type2 = TileType.NpcAvoid;
                            instance1.Data1_2 = 0;
                            instance1.Data2_2 = 0;
                            instance1.Data3_2 = 0;
                        }
                    }

                    // resource
                    if (GameState.OptResource)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            instance1.Type = TileType.Resource;
                            instance1.Data1 = GameState.ResourceEditor;
                            instance1.Data2 = 0;
                            instance1.Data3 = 0;
                        }
                        else
                        {
                            instance1.Type2 = TileType.Resource;
                            instance1.Data1_2 = GameState.ResourceEditor;
                            instance1.Data2_2 = 0;
                            instance1.Data3_2 = 0;
                        }
                    }

                    // Npc spawn
                    if (GameState.OptNpcSpawn)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            instance1.Type = TileType.NpcSpawn;
                            instance1.Data1 = GameState.SpawnNpc;
                            instance1.Data2 = GameState.SpawnNpcDir;
                            instance1.Data3 = 0;
                        }
                        else
                        {
                            instance1.Type2 = TileType.NpcSpawn;
                            instance1.Data1_2 = GameState.SpawnNpc;
                            instance1.Data2_2 = GameState.SpawnNpcDir;
                            instance1.Data3_2 = 0;
                        }
                    }

                    // shop
                    if (GameState.OptShop)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            instance1.Type = TileType.Shop;
                            instance1.Data1 = GameState.EditorShop;
                            instance1.Data2 = 0;
                            instance1.Data3 = 0;
                        }
                        else
                        {
                            instance1.Type2 = TileType.Shop;
                            instance1.Data1_2 = GameState.EditorShop;
                            instance1.Data2_2 = 0;
                            instance1.Data3_2 = 0;
                        }
                    }

                    // bank
                    if (GameState.OptBank)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            instance1.Type = TileType.Bank;
                            instance1.Data1 = 0;
                            instance1.Data2 = 0;
                            instance1.Data3 = 0;
                        }
                        else
                        {
                            instance1.Type2 = TileType.Bank;
                            instance1.Data1_2 = 0;
                            instance1.Data2_2 = 0;
                            instance1.Data3_2 = 0;
                        }
                    }

                    // heal
                    if (GameState.OptHeal)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            instance1.Type = TileType.Heal;
                            instance1.Data1 = GameState.MapEditorHealType;
                            instance1.Data2 = GameState.MapEditorHealAmount;
                            instance1.Data3 = 0;
                        }
                        else
                        {
                            instance1.Type2 = TileType.Heal;
                            instance1.Data1_2 = GameState.MapEditorHealType;
                            instance1.Data2_2 = GameState.MapEditorHealAmount;
                            instance1.Data3_2 = 0;
                        }
                    }

                    // trap
                    if (GameState.OptTrap)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            instance1.Type = TileType.Trap;
                            instance1.Data1 = GameState.MapEditorHealAmount;
                            instance1.Data2 = GameState.MapEditorTrapVital;
                            instance1.Data3 = 0;
                        }
                        else
                        {
                            instance1.Type2 = TileType.Trap;
                            instance1.Data1_2 = GameState.MapEditorHealAmount;
                            instance1.Data2_2 = GameState.MapEditorTrapVital;
                            instance1.Data3_2 = 0;
                        }
                    }

                    // Animation
                    if (GameState.OptAnimation)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            instance1.Type = TileType.Animation;
                            instance1.Data1 = GameState.EditorAnimation;
                            instance1.Data2 = 0;
                            instance1.Data3 = 0;
                        }
                        else
                        {
                            instance1.Type2 = TileType.Animation;
                            instance1.Data1_2 = GameState.EditorAnimation;
                            instance1.Data2_2 = 0;
                            instance1.Data3_2 = 0;
                        }
                    }

                    // No Xing
                    if (GameState.OptNoCrossing)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            instance1.Type = TileType.NoCrossing;
                            instance1.Data1 = 0;
                            instance1.Data2 = 0;
                            instance1.Data3 = 0;
                        }
                        else
                        {
                            instance1.Type2 = TileType.NoCrossing;
                            instance1.Data1_2 = 0;
                            instance1.Data2_2 = 0;
                            instance1.Data3_2 = 0;
                        }
                    }
                }
               else if (GameState.MapEditorTab == (int)MapEditorTab.Directions)
                {
                    // Convert adjusted coordinates to game world coordinates
                    x = (int)Math.Round(GameState.TileView.Left + Math.Floor((GameState.CurMouseX + GameState.Camera.Left) % Constants.TileSize));
                    y = (int)Math.Round(GameState.TileView.Top + Math.Floor((GameState.CurMouseY + GameState.Camera.Top) % Constants.TileSize));

                    // see if it hits an arrow
                    for (i = 0; i < 4; i++)
                    {
                        // flip the value.
                        if (x >= GameState.DirArrowX[i] & x <= GameState.DirArrowX[i] + 16)
                        {
                            if (y >= GameState.DirArrowY[i] & y <= GameState.DirArrowY[i] + 16)
                            {
                                // flip the value.
                                bool localIsDirBlocked() { byte argdir = (byte)i; var dirBlocked = GameLogic.IsDirBlocked(ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[GameState.CurX, GameState.CurY].DirBlock, ref argdir); return dirBlocked; }

                                byte argdir = (byte)i;
                                GameLogic.SetDirBlock(ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[GameState.CurX, GameState.CurY].DirBlock, ref argdir, !localIsDirBlocked());
                                break;
                            }
                        }
                    }
                }
                else if (GameState.MapEditorTab == (int)MapEditorTab.Events)
                {
                    // Use editor tile indices (CurX/CurY) for event placement
                    if (Event.EventCopy)
                    {
                        Event.CopyEvent_Map(GameState.CurX, GameState.CurY);
                    }
                    else if (Event.EventPaste)
                    {
                        Event.PasteEvent_Map(GameState.CurX, GameState.CurY);
                    }
                    else
                    {
                        Event.AddEvent(GameState.CurX, GameState.CurY);
                    }
                }
            }

            if (GameClient.IsMouseButtonDown(MouseButton.Right))
            {
                if (GameState.MapEditorTab == (int)MapEditorTab.Tiles)
                {
                    if (GameState.EditorTileWidth == 1 & GameState.EditorTileHeight == 1) // single tile
                    {
                        MapEditorSetTile(GameState.CurX, GameState.CurY, GameState.CurLayer, false, (byte)GameState.CurAutotileType, 1);
                    }
                    else if (GameState.CurAutotileType == 0) // multi tile!
                    {
                        MapEditorSetTile(GameState.CurX, GameState.CurY, GameState.CurLayer, true, 0, 1);
                    }
                    else
                    {
                        MapEditorSetTile(GameState.CurX, GameState.CurY, GameState.CurLayer, true, (byte)GameState.CurAutotileType, 1);
                    }
                }
                else if (GameState.MapEditorTab == (int)MapEditorTab.Attributes)
                {
                    ref var instance2 = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[GameState.CurX, GameState.CurY];
                    // clear attribute
                    instance2.Type = 0;
                    instance2.Data1 = 0;
                    instance2.Data2 = 0;
                    instance2.Data3 = 0;
                    instance2.Type2 = 0;
                    instance2.Data1_2 = 0;
                    instance2.Data2_2 = 0;
                    instance2.Data3_2 = 0;
                }
                else if (GameState.MapEditorTab == (int)MapEditorTab.Events)
                    Event.DeleteEvent(GameState.CurX, GameState.CurY);
            }

            MapEditorHistory();

            x = 0;

            for (int x2 = 0, count = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX; x2 < count; x2++)
            {
                for (int y2 = 0, count2 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY; y2 < count2; y2++)
                {
                    // Use Layer.Length instead of MapLayer.Count
                    for (int i2 = 0, count3 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x2, y2].Layer != null ? Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x2, y2].Layer.Length : 0; i2 < count3; i2++)
                    {
                        ref var currentTile = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x2, y2];
                        ref var historyTile = ref Data.TileHistory![GameState.TileHistoryIndex].Tile[x2, y2];

                        // Check Layer array length for both tiles
                        if (currentTile.Layer == null || currentTile.Layer.Length <= i2 || historyTile.Layer == null || historyTile.Layer.Length <= i2)
                        {
                            continue; // Skip processing if Layer is not properly initialized
                        }

                        // Check if the tile is modified
                        isModified = currentTile.Data1 != historyTile.Data1 ||
                                            currentTile.Data2 != historyTile.Data2 ||
                                            currentTile.Data3 != historyTile.Data3 ||
                                            currentTile.Data1_2 != historyTile.Data1_2 ||
                                            currentTile.Data2_2 != historyTile.Data2_2 ||
                                            currentTile.Data3_2 != historyTile.Data3_2 ||
                                            currentTile.Type != historyTile.Type ||
                                            currentTile.Type2 != historyTile.Type2 ||
                                            currentTile.DirBlock != historyTile.DirBlock ||
                                            currentTile.Layer[i2].X != historyTile.Layer[i2].X ||
                                            currentTile.Layer[i2].Y != historyTile.Layer[i2].Y ||
                                            currentTile.Layer[i2].Tileset != historyTile.Layer[i2].Tileset ||
                                            currentTile.Layer[i2].AutoTile != historyTile.Layer[i2].AutoTile;

                        if (isModified)
                        {
                            historyTile.Data1 = currentTile.Data1;
                            historyTile.Data2 = currentTile.Data2;
                            historyTile.Data3 = currentTile.Data3;
                            historyTile.Data1_2 = currentTile.Data1_2;
                            historyTile.Data2_2 = currentTile.Data2_2;
                            historyTile.Data3_2 = currentTile.Data3_2;
                            historyTile.Type = currentTile.Type;
                            historyTile.Type2 = currentTile.Type2;
                            historyTile.DirBlock = currentTile.DirBlock;
                            historyTile.Layer[i2].X = currentTile.Layer[i2].X;
                            historyTile.Layer[i2].Y = currentTile.Layer[i2].Y;
                            historyTile.Layer[i2].Tileset = currentTile.Layer[i2].Tileset;
                            historyTile.Layer[i2].AutoTile = currentTile.Layer[i2].AutoTile;

                            if (historyTile.Layer[i2].AutoTile > 0)
                            {
                                x = 1;
                            }

                            Autotile.CacheRenderState(x2, y2, i2);
                        }
                    }
                }
            }

            if (GameClient.CurrentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) || GameClient.CurrentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl))
            {
                MapEditorReplaceTile((MapLayer)GameState.CurLayer, GameState.CurX, GameState.CurY, instance);
            }

            if (x == 1)
            {
                // do a re-init so we can see our changes
                Autotile.InitAutotiles();
            }
        }

        public static void MapEditorCancel()
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
            
            Sender.SendCloseEditor();

            // show gui
            WindowManager.ShowWindow("winHotbar", resetPosition: false);
            WindowManager.ShowWindow("winMenu", resetPosition: false);
            WindowManager.ShowWindow("winBars", resetPosition: false);
            WinChat.Hide();

            GameState.TileHistoryHighIndex = 0;
            GameState.TileHistoryIndex = 0;
        }

        public static void MapEditorSend()
        {
            // Apply queued resize (if any) only at save time.
            if (GameState.MyEditorType == EditorType.Map && GameState.MapResizePending)
            {
                var nx = (byte)Math.Clamp(GameState.MapResizePendingX, 1, byte.MaxValue);
                var ny = (byte)Math.Clamp(GameState.MapResizePendingY, 1, byte.MaxValue);
                GameState.MapResizePending = false;
                WinMapEditor.ResizeMap(nx, ny, updateControls: false);
            }

            // Send the edited map to the server
            Sender.SendMap();

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
            Sender.SendCloseEditor();

            // show gui
            WindowManager.ShowWindow("winHotbar", resetPosition: false);
            WindowManager.ShowWindow("winMenu", resetPosition: false);
            WindowManager.ShowWindow("winBars", resetPosition: false);
            WinChat.Hide();
            
            GameState.TileHistoryHighIndex = 0;
            GameState.TileHistoryIndex = 0;
        }

        public static void MapEditorSetTile(int x, int y, int CurLayer, bool multitile = false, byte theAutotile = 0, byte eraseTile = 0)
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
                        // Re-init so changes are visible.
                        Autotile.InitAutotiles();
                    }

                    return;
                }
            }

            newTileX = GameState.EditorTileX;
            newTileY = GameState.EditorTileY;

            if (Conversions.ToBoolean(eraseTile))
            {
                newTileX = 0;
                newTileY = 0;
            }

            if (theAutotile > 0)
            {
                ref var instance = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y];
                // set layer
                instance.Layer[CurLayer].X = newTileX;
                instance.Layer[CurLayer].Y = newTileY;
                if (Conversions.ToBoolean(eraseTile))
                {
                    instance.Layer[CurLayer].Tileset = 0;
                }
                else
                {
                    instance.Layer[CurLayer].Tileset = GameState.CurTileset;
                }
                instance.Layer[CurLayer].AutoTile = theAutotile;
                Autotile.CacheRenderState(x, y, CurLayer);

                // do a re-init so we can see our changes
                Autotile.InitAutotiles();
                return;
            }

            if (!multitile) // single
            {
                ref var instance1 = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y];
                // set layer
                instance1.Layer[CurLayer].X = newTileX;
                instance1.Layer[CurLayer].Y = newTileY;
                if (Conversions.ToBoolean(eraseTile))
                {
                    instance1.Layer[CurLayer].Tileset = 0;
                }
                else
                {
                    instance1.Layer[CurLayer].Tileset = GameState.CurTileset;
                }
                instance1.Layer[CurLayer].AutoTile = 0;
                Autotile.CacheRenderState(x, y, CurLayer);
            }
            else // multitile
            {
                y2 = 0; // starting tile for y axis
                var count = GameState.CurY + GameState.EditorTileHeight;
                for (y = GameState.CurY; y < count; y++)
                {
                    x2 = 0; // re-set x count every y loop
                    var count2 = GameState.CurX + GameState.EditorTileWidth;
                    for (x = GameState.CurX; x < count2; x++)
                    {
                        if (x >= 0 & x < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX)
                        {
                            if (y >= 0 & y < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY)
                            {
                                ref var instance2 = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y];
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
                                Autotile.CacheRenderState(x, y, CurLayer);
                            }
                        }
                        x2 += 1;
                    }
                    y2 += 1;
                }
            }
        }

        public static void MapEditorHistory()
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

        public static void MapEditorClearLayer(MapLayer layer)
        {
            GameLogic.Dialogue("Map Editor", "Clear Layer: " + layer.ToString(), "Are you sure you wish to clear this layer?", DialogueType.ClearLayer, DialogueStyle.YesNo, GameState.CurLayer, GameState.CurAutotileType);
        }

        public static void MapEditorFillLayer(MapLayer layer, byte theAutotile = 0, byte tileX = 0, byte tileY = 0)
        {
            GameLogic.Dialogue("Map Editor", "Fill Layer: " + layer.ToString(), "Are you sure you wish to fill this layer?", DialogueType.FillLayer, DialogueStyle.YesNo, GameState.CurLayer, GameState.CurAutotileType, tileX, tileY, GameState.CurTileset);
        }

        public static void MapEditorEyeDropper()
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

        public static void MapEditorEyeDropper(int startX, int startY, int endX, int endY)
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

            // Keep existing selection semantics so the pencil paints a rectangle.
            // CurTileset/EditorTileX/Y are still used by other UI bits; set them from top-left.
            ref var tl = ref map.Tile[minX, minY];
            GameState.CurTileset = tl.Layer[layer].Tileset;
            GameState.EditorTileX = tl.Layer[layer].X;
            GameState.EditorTileY = tl.Layer[layer].Y;
            GameState.EditorTileWidth = width;
            GameState.EditorTileHeight = height;
            GameState.EditorTileSelStart = new Microsoft.Xna.Framework.Point(GameState.EditorTileX, GameState.EditorTileY);
            GameState.EditorTileSelEnd = new Microsoft.Xna.Framework.Point(GameState.EditorTileX + width, GameState.EditorTileY + height);

            // After capture, switch back to painting.
            GameState.EyeDropperSelecting = false;
            GameState.EyeDropper = false;
        }

        public static void Undo()
        {
            bool isModified = false;

            if (GameState.TileHistoryIndex <= 0)
            {
                return;
            }

            int layerCount = Enum.GetValues(typeof(MapLayer)).Length;

            for (int x = 0, count = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX; x < count; x++)
            {
                for (int y = 0, count2 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY; y < count2; y++)
                {
                    for (int i = 0; i < layerCount; i++)
                    {
                        ref var currentTile = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y];
                        ref var historyTile = ref Data.TileHistory![GameState.TileHistoryIndex].Tile[x, y];

                        if (currentTile.Layer == null || currentTile.Layer.Length <= i || historyTile.Layer == null || historyTile.Layer.Length <= i)
                        {
                            continue; // Skip processing if Layer is not properly initialized
                        }

                        if (!isModified)
                        {
                            // Check if the tile is modified
                            isModified = currentTile.Data1 != historyTile.Data1 ||
                                                currentTile.Data2 != historyTile.Data2 ||
                                                currentTile.Data3 != historyTile.Data3 ||
                                                currentTile.Data1_2 != historyTile.Data1_2 ||
                                                currentTile.Data2_2 != historyTile.Data2_2 ||
                                                currentTile.Data3_2 != historyTile.Data3_2 ||
                                                currentTile.Type != historyTile.Type ||
                                                currentTile.Type2 != historyTile.Type2 ||
                                                currentTile.DirBlock != historyTile.DirBlock ||
                                                currentTile.Layer[i].X != historyTile.Layer[i].X ||
                                                currentTile.Layer[i].Y != historyTile.Layer[i].Y ||
                                                currentTile.Layer[i].Tileset != historyTile.Layer[i].Tileset ||
                                                currentTile.Layer[i].AutoTile != historyTile.Layer[i].AutoTile;
                        }

                        currentTile.Data1 = historyTile.Data1;
                        currentTile.Data2 = historyTile.Data2;
                        currentTile.Data3 = historyTile.Data3;
                        currentTile.Data1_2 = historyTile.Data1_2;
                        currentTile.Data2_2 = historyTile.Data2_2;
                        currentTile.Data3_2 = historyTile.Data3_2;
                        currentTile.Type = historyTile.Type;
                        currentTile.Type2 = historyTile.Type2;
                        currentTile.DirBlock = historyTile.DirBlock;
                        currentTile.Layer[i].X = historyTile.Layer[i].X;
                        currentTile.Layer[i].Y = historyTile.Layer[i].Y;
                        currentTile.Layer[i].Tileset = historyTile.Layer[i].Tileset;
                        currentTile.Layer[i].AutoTile = historyTile.Layer[i].AutoTile;
                        Autotile.CacheRenderState(x, y, i);

                        if (currentTile.Layer[i].AutoTile > 0)
                        {
                            // do a re-init so we can see our changes
                            Autotile.InitAutotiles();
                        }
                    }
                }
            }

            GameState.TileHistoryIndex -= 1;

            if (!isModified)
            {
                Undo();
            }
        }

        public static void Redo()
        {
            bool isModified = false;

            if (GameState.TileHistoryIndex > GameState.TileHistoryHighIndex)
            {
                GameState.TileHistoryIndex--;
                return;
            }

            int layerCount = Enum.GetValues(typeof(MapLayer)).Length;

            for (int x = 0, count = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX; x < count; x++)
            {
                for (int y = 0, count2 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY; y < count2; y++)
                {
                    for (int i = 0; i < layerCount; i++)
                    {
                        ref var currentTile = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y];
                        ref var historyTile = ref Data.TileHistory![GameState.TileHistoryIndex].Tile[x, y];

                        if (currentTile.Layer == null || currentTile.Layer.Length <= i || historyTile.Layer == null || historyTile.Layer.Length <= i)
                        {
                            continue; // Skip processing if Layer is not properly initialized
                        }

                        if (!isModified)
                        {
                            // Check if the tile is modified
                            isModified = currentTile.Data1 != historyTile.Data1 ||
                                                currentTile.Data2 != historyTile.Data2 ||
                                                currentTile.Data3 != historyTile.Data3 ||
                                                currentTile.Data1_2 != historyTile.Data1_2 ||
                                                currentTile.Data2_2 != historyTile.Data2_2 ||
                                                currentTile.Data3_2 != historyTile.Data3_2 ||
                                                currentTile.Type != historyTile.Type ||
                                                currentTile.Type2 != historyTile.Type2 ||
                                                currentTile.DirBlock != historyTile.DirBlock ||
                                                currentTile.Layer[i].X != historyTile.Layer[i].X ||
                                                currentTile.Layer[i].Y != historyTile.Layer[i].Y ||
                                                currentTile.Layer[i].Tileset != historyTile.Layer[i].Tileset ||
                                                currentTile.Layer[i].AutoTile != historyTile.Layer[i].AutoTile;
                        }

                        currentTile.Data1 = historyTile.Data1;
                        currentTile.Data2 = historyTile.Data2;
                        currentTile.Data3 = historyTile.Data3;
                        currentTile.Data1_2 = historyTile.Data1_2;
                        currentTile.Data2_2 = historyTile.Data2_2;
                        currentTile.Data3_2 = historyTile.Data3_2;
                        currentTile.Type = historyTile.Type;
                        currentTile.Type2 = historyTile.Type2;
                        currentTile.DirBlock = historyTile.DirBlock;
                        currentTile.Layer[i].X = historyTile.Layer[i].X;
                        currentTile.Layer[i].Y = historyTile.Layer[i].Y;
                        currentTile.Layer[i].Tileset = historyTile.Layer[i].Tileset;
                        currentTile.Layer[i].AutoTile = historyTile.Layer[i].AutoTile;
                        Autotile.CacheRenderState(x, y, i);

                        if (currentTile.Layer[i].AutoTile > 0)
                        {
                            // do a re-init so we can see our changes
                            Autotile.InitAutotiles();
                        }
                    }
                }
            }

            GameState.TileHistoryIndex++;

            if (!isModified)
            {
                Redo();
            }
        }

        public static void MapEditorCopyMap()
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

        public static void MapEditorPasteMap()
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
        public static void MapEditorReplaceTile(MapLayer layer, int tileX, int tileY, Core.Globals.Type.Tile oldTile)
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

                            tile.Layer[(int)layer].AutoTile = 0;
                            Autotile.CacheRenderState(x, y, (int)layer);
                        }
                    }
                    else if ((int)MapEditorTab.Attributes == GameState.MapEditorTab)
                    {
                        if (GameClient.IsMouseButtonDown(MouseButton.Left))
                        {
                            if (GameState.EditorAttribute == 1)
                            {
                                tile.Data1 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[tileX, tileY].Data1;
                                tile.Data2 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[tileX, tileY].Data2;
                                tile.Data3 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[tileX, tileY].Data3;
                                tile.Type = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[tileX, tileY].Type;
                            }
                            else
                            {
                                tile.Data1_2 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[tileX, tileY].Data1_2;
                                tile.Data2_2 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[tileX, tileY].Data2_2;
                                tile.Data3_2 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[tileX, tileY].Data3_2;
                                tile.Type2 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[tileX, tileY].Type2;
                            }
                        }

                        if (GameClient.IsMouseButtonDown(MouseButton.Right))
                        {
                            if (GameState.EditorAttribute == 1)
                            {
                                tile.Data1 = 0;
                                tile.Data2 = 0;
                                tile.Data3 = 0;
                                tile.Type = 0;
                            }
                            else
                            {
                                tile.Data1_2 = 0;
                                tile.Data2_2 = 0;
                                tile.Data3_2 = 0;
                                tile.Type2 = 0;
                            }
                        }
                    }
                }
            }
        }

        #region Animation Editor
        public static void AnimationEditorOK()
        {
            int i;

            for (i = 0; i < Variables.MaxAnimations; i++)
            {
                if (Animation.IsChanged[i])
                {
                    Sender.SendSaveAnimation(i);
                }
            }

            GameState.MyEditorType = EditorType.None;
            Animation.OnClearChanged();
            Sender.SendCloseEditor();
        }

        public static void AnimationEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            Animation.OnClearChanged();
            Animation.OnClear();
            Sender.SendCloseEditor();
        }

        #endregion

        #region Npc Editor

        public static void NpcEditorOK()
        {
            for (int i = 0; i < Variables.MaxNpcs; i++)
            {
                if (GameState.NpcChanged[i])
                {
                    Sender.SendSaveNpc(i);
                }
            }

            GameState.MyEditorType = EditorType.None;
            Npc.OnClearChanged();
            Sender.SendCloseEditor();
        }

        public static void NpcEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            Npc.OnClearChanged();
            Npc.OnClear();
            Sender.SendCloseEditor();
        }

        #endregion

        #region Resource Editor

        public static void ResourceEditorOK()
        {
            int i;

            for (i = 0; i < Variables.MaxResources; i++)
            {
                if (GameState.ResourceChanged[i])
                {
                    Sender.SendSaveResource(i);
                }
            }

            GameState.MyEditorType = EditorType.None;
            Resource.OnClearChanged();
            Sender.SendCloseEditor();
        }

        public static void ResourceEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            Resource.OnClearChanged();
            Resource.OnClear();
            Sender.SendCloseEditor();
        }

        #endregion

        #region Skill Editor

        public static void SkillEditorOK()
        {
            int i;

            for (i = 0; i < Variables.MaxSkills; i++)
            {
                if (GameState.SkillChanged[i])
                {
                    Sender.SendSaveSkill(i);
                }
            }

            GameState.MyEditorType = EditorType.None;
            Skill.OnClearChanged();
            Sender.SendCloseEditor();
        }

        public static void SkillEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            Skill.OnClearChanged();
            Skill.OnClear();
            Sender.SendCloseEditor();
        }

        #endregion
        public static void ShopEditorOK()
        {
            int i;

            for (i = 0; i < Variables.MaxShops; i++)
            {
                if (GameState.ShopChanged[i])
                {
                    Sender.SendSaveShop(i);
                }
            }

            GameState.MyEditorType = EditorType.None;
            Shop.OnClearChanged();
            Sender.SendCloseEditor();
        }

        public static void ShopEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            Shop.OnClearChanged();
            Shop.OnClear();
            Sender.SendCloseEditor();
        }

        #region Job Editor
        public static void JobEditorOK()
        {
            for (int i = 0; i < Variables.MaxJobs; i++)
            {
                if (Job.IsChanged[i])
                {
                    Sender.SendSaveJob(i);
                }
            }
            GameState.MyEditorType = EditorType.None;
            Sender.SendCloseEditor();
        }

        public static void JobEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            Job.OnClearChanged();
            Job.OnClear();
            Sender.SendCloseEditor();
        }

        public static void ItemEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            Item.OnClearChanged();
            Item.OnClear();
            Sender.SendCloseEditor();
        }

        public static void ItemEditorOK()
        {
            int i;

            for (i = 0; i < Core.Globals.Variables.MaxItems; i++)
            {
                if (Item.IsChanged[i])
                {
                    Sender.SendSaveItem(i);
                }
            }

            GameState.MyEditorType = EditorType.None;
            Item.OnClearChanged();
            Sender.SendCloseEditor();
        }

        #endregion

        #region Moral Editor
        public static void MoralEditorOK()
        {
            for (int i = 0; i < Variables.MaxMorals; i++)
            {
                if (Moral.IsChanged[i])
                {
                    Sender.SendSaveMoral(i);
                }
            }
            GameState.MyEditorType = EditorType.None;
            Sender.SendCloseEditor();
        }

        public static void MoralEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            Moral.OnClearChanged();
            Moral.OnClear();
            Sender.SendCloseEditor();
        }

        #endregion

        #region Projectile Editor
        public static void ProjectileEditorOK()
        {
            for (int i = 0; i < Variables.MaxProjectiles;  i++)
            {
                if (Projectile.IsChanged[i])
                {
                    Sender.SendSaveProjectile(i);
                }
            }

            GameState.MyEditorType = EditorType.None;
            Projectile.OnClearChanged();
            Sender.SendCloseEditor();
        }

        public static void ProjectileEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            Projectile.OnClearChanged();
            Projectile.OnClear();
            Sender.SendCloseEditor();
        }

        #endregion

    }
}