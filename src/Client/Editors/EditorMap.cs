using Eto.Drawing;
using System.Linq;
using System.Collections.Generic;
using Client.Game.UI;
using Client.Game.UI.Windows;
using Client.Net;
using Core;
using Core.Configurations;
using Core.Globals;
using Core.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Core.Globals.Type;
using Color = Microsoft.Xna.Framework.Color;
using Command = Eto.Forms.Command;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Type = Core.Globals.Type;

namespace Client
{

    public partial class EditorMap
    {
        private static float tilesetOffsetX = 0;
        private static float tilesetOffsetY = 0;
        public static void MapEditorChooseTile(float X, float Y)
        {
            if (GameClient.IsMouseButtonDown(MouseButton.Left)) // Primary (Left) Mouse Button
            {
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
                GameState.EditorTileX = (int)((X + tilesetOffsetX) / GameState.SizeX);
                GameState.EditorTileY = (int)((Y + tilesetOffsetY) / GameState.SizeY);

                GameState.EditorTileSelStart = new Point(GameState.EditorTileX, GameState.EditorTileY);
                GameState.EditorTileSelEnd = new Point(
                    GameState.EditorTileX + GameState.EditorTileWidth,
                    GameState.EditorTileY + GameState.EditorTileHeight
                );
            }
        }

        public new static void MouseDown(int x, int y, bool movedMouse = true)
        {
            int i;
            bool isModified = false;

            // Bounds check for both CurX/CurY and x/y
            if (GameState.CurX < 0 || GameState.CurY < 0 || GameState.CurX >= Data.MyMap.MaxX || GameState.CurY >= Data.MyMap.MaxY)
                return;
                
            if (x < 0 || y < 0 || x >= Data.MyMap.MaxX || y >= Data.MyMap.MaxY)
                return;

            if (!GameLogic.IsInBounds())
                return;

            if (GameState.EyeDropper && GameClient.IsMouseButtonDown(MouseButton.Left))
            {
                MapEditorEyeDropper();
                return;
            }

            var withBlock = Data.MyMap.Tile[x, y];

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
                    ref var withBlock1 = ref Data.MyMap.Tile[GameState.CurX, GameState.CurY];

                    if (GameState.OptInfo)
                    {
                        if (GameState.Info == false)
                        {
                            if (GameState.EditorAttribute == 1)
                            {
                                GameLogic.Dialogue("Map Editor", "Info: " + System.Enum.GetName(Data.MyMap.Tile[GameState.CurX, GameState.CurY].Type), " Data 1: " + Data.MyMap.Tile[GameState.CurX, GameState.CurY].Data1 + " Data 2: " + Data.MyMap.Tile[GameState.CurX, GameState.CurY].Data2 + " Data 3: " + Data.MyMap.Tile[GameState.CurX, GameState.CurY].Data3, DialogueType.Information, (byte)DialogueStyle.Okay);
                            }
                            else
                            {
                                GameLogic.Dialogue("Map Editor", "Info: " + System.Enum.GetName(Data.MyMap.Tile[GameState.CurX, GameState.CurY].Type2), " Data 1: " + Data.MyMap.Tile[GameState.CurX, GameState.CurY].Data1_2 + " Data 2: " + Data.MyMap.Tile[GameState.CurX, GameState.CurY].Data2_2 + " Data 3: " + Data.MyMap.Tile[GameState.CurX, GameState.CurY].Data3_2, DialogueType.Information, (byte)DialogueStyle.Okay);
                            }
                        }
                    }
                    
                    // blocked tile
                    if (GameState.OptBlocked)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            withBlock1.Type = TileType.Blocked;
                        }
                        else
                        {
                            withBlock1.Type2 = TileType.Blocked;
                        }
                    }

                    // warp tile
                    if (GameState.OptWarp)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            withBlock1.Type = TileType.Warp;
                            withBlock1.Data1 = GameState.EditorWarpMap;
                            withBlock1.Data2 = GameState.EditorWarpX;
                            withBlock1.Data3 = GameState.EditorWarpY;
                        }
                        else
                        {
                            withBlock1.Type2 = TileType.Warp;
                            withBlock1.Data1_2 = GameState.EditorWarpMap;
                            withBlock1.Data2_2 = GameState.EditorWarpX;
                            withBlock1.Data3_2 = GameState.EditorWarpY;
                        }
                    }

                    // item spawn
                    if (GameState.OptItem)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            withBlock1.Type = TileType.Item;
                            withBlock1.Data1 = GameState.ItemEditorNum;
                            withBlock1.Data2 = GameState.ItemEditorValue;
                            withBlock1.Data3 = 0;
                        }
                        else
                        {
                            withBlock1.Type2 = TileType.Item;
                            withBlock1.Data1_2 = GameState.ItemEditorNum;
                            withBlock1.Data2_2 = GameState.ItemEditorValue;
                            withBlock1.Data3_2 = 0;
                        }
                    }

                    // Npc avoid
                    if (GameState.OptNpcAvoid)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            withBlock1.Type = TileType.NpcAvoid;
                            withBlock1.Data1 = 0;
                            withBlock1.Data2 = 0;
                            withBlock1.Data3 = 0;
                        }
                        else
                        {
                            withBlock1.Type2 = TileType.NpcAvoid;
                            withBlock1.Data1_2 = 0;
                            withBlock1.Data2_2 = 0;
                            withBlock1.Data3_2 = 0;
                        }
                    }

                    // resource
                    if (GameState.OptResource)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            withBlock1.Type = TileType.Resource;
                            withBlock1.Data1 = GameState.ResourceEditorNum;
                            withBlock1.Data2 = 0;
                            withBlock1.Data3 = 0;
                        }
                        else
                        {
                            withBlock1.Type2 = TileType.Resource;
                            withBlock1.Data1_2 = GameState.ResourceEditorNum;
                            withBlock1.Data2_2 = 0;
                            withBlock1.Data3_2 = 0;
                        }
                    }

                    // Npc spawn
                    if (GameState.OptNpcSpawn)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            withBlock1.Type = TileType.NpcSpawn;
                            withBlock1.Data1 = GameState.SpawnNpcNum;
                            withBlock1.Data2 = GameState.SpawnNpcDir;
                            withBlock1.Data3 = 0;
                        }
                        else
                        {
                            withBlock1.Type2 = TileType.NpcSpawn;
                            withBlock1.Data1_2 = GameState.SpawnNpcNum;
                            withBlock1.Data2_2 = GameState.SpawnNpcDir;
                            withBlock1.Data3_2 = 0;
                        }
                    }

                    // shop
                    if (GameState.OptShop)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            withBlock1.Type = TileType.Shop;
                            withBlock1.Data1 = GameState.EditorShop;
                            withBlock1.Data2 = 0;
                            withBlock1.Data3 = 0;
                        }
                        else
                        {
                            withBlock1.Type2 = TileType.Shop;
                            withBlock1.Data1_2 = GameState.EditorShop;
                            withBlock1.Data2_2 = 0;
                            withBlock1.Data3_2 = 0;
                        }
                    }

                    // bank
                    if (GameState.OptBank)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            withBlock1.Type = TileType.Bank;
                            withBlock1.Data1 = 0;
                            withBlock1.Data2 = 0;
                            withBlock1.Data3 = 0;
                        }
                        else
                        {
                            withBlock1.Type2 = TileType.Bank;
                            withBlock1.Data1_2 = 0;
                            withBlock1.Data2_2 = 0;
                            withBlock1.Data3_2 = 0;
                        }
                    }

                    // heal
                    if (GameState.OptHeal)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            withBlock1.Type = TileType.Heal;
                            withBlock1.Data1 = GameState.MapEditorHealType;
                            withBlock1.Data2 = GameState.MapEditorHealAmount;
                            withBlock1.Data3 = 0;
                        }
                        else
                        {
                            withBlock1.Type2 = TileType.Heal;
                            withBlock1.Data1_2 = GameState.MapEditorHealType;
                            withBlock1.Data2_2 = GameState.MapEditorHealAmount;
                            withBlock1.Data3_2 = 0;
                        }
                    }

                    // trap
                    if (GameState.OptTrap)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            withBlock1.Type = TileType.Trap;
                            withBlock1.Data1 = GameState.MapEditorHealAmount;
                            withBlock1.Data2 = GameState.MapEditorTrapVital;
                            withBlock1.Data3 = 0;
                        }
                        else
                        {
                            withBlock1.Type2 = TileType.Trap;
                            withBlock1.Data1_2 = GameState.MapEditorHealAmount;
                            withBlock1.Data2_2 = GameState.MapEditorTrapVital;
                            withBlock1.Data3_2 = 0;
                        }
                    }

                    // Animation
                    if (GameState.OptAnimation)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            withBlock1.Type = TileType.Animation;
                            withBlock1.Data1 = GameState.EditorAnimation;
                            withBlock1.Data2 = 0;
                            withBlock1.Data3 = 0;
                        }
                        else
                        {
                            withBlock1.Type2 = TileType.Animation;
                            withBlock1.Data1_2 = GameState.EditorAnimation;
                            withBlock1.Data2_2 = 0;
                            withBlock1.Data3_2 = 0;
                        }
                    }

                    // No Xing
                    if (GameState.OptNoCrossing)
                    {
                        if (GameState.EditorAttribute == 1)
                        {
                            withBlock1.Type = TileType.NoCrossing;
                            withBlock1.Data1 = 0;
                            withBlock1.Data2 = 0;
                            withBlock1.Data3 = 0;
                        }
                        else
                        {
                            withBlock1.Type2 = TileType.NoCrossing;
                            withBlock1.Data1_2 = 0;
                            withBlock1.Data2_2 = 0;
                            withBlock1.Data3_2 = 0;
                        }
                    }
                }
               else if (GameState.MapEditorTab == (int)MapEditorTab.Directions)
                {
                    // Convert adjusted coordinates to game world coordinates
                    x = (int)Math.Round(GameState.TileView.Left + Math.Floor((GameState.CurMouseX + GameState.Camera.Left) % GameState.SizeX));
                    y = (int)Math.Round(GameState.TileView.Top + Math.Floor((GameState.CurMouseY + GameState.Camera.Top) % GameState.SizeY));

                    // see if it hits an arrow
                    for (i = 0; i < 4; i++)
                    {
                        // flip the value.
                        if (x >= GameState.DirArrowX[i] & x <= GameState.DirArrowX[i] + 16)
                        {
                            if (y >= GameState.DirArrowY[i] & y <= GameState.DirArrowY[i] + 16)
                            {
                                // flip the value.
                                bool localIsDirBlocked() { byte argdir = (byte)i; var dirBlocked = GameLogic.IsDirBlocked(ref Data.MyMap.Tile[GameState.CurX, GameState.CurY].DirBlock, ref argdir); return dirBlocked; }

                                byte argdir = (byte)i;
                                GameLogic.SetDirBlock(ref Data.MyMap.Tile[GameState.CurX, GameState.CurY].DirBlock, ref argdir, !localIsDirBlocked());
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
                    ref var withBlock2 = ref Data.MyMap.Tile[GameState.CurX, GameState.CurY];
                    // clear attribute
                    withBlock2.Type = 0;
                    withBlock2.Data1 = 0;
                    withBlock2.Data2 = 0;
                    withBlock2.Data3 = 0;
                    withBlock2.Type2 = 0;
                    withBlock2.Data1_2 = 0;
                    withBlock2.Data2_2 = 0;
                    withBlock2.Data3_2 = 0;
                }
                else if (GameState.MapEditorTab == (int)MapEditorTab.Events)
                    Event.DeleteEvent(GameState.CurX, GameState.CurY);
            }

            MapEditorHistory();

            x = 0;

            for (int x2 = 0, loopTo = Data.MyMap.MaxX; x2 < loopTo; x2++)
            {
                for (int y2 = 0, loopTo1 = Data.MyMap.MaxY; y2 < loopTo1; y2++)
                {
                    // Use Layer.Length instead of MapLayer.Count
                    for (int i2 = 0, loopTo2 = Data.MyMap.Tile[x2, y2].Layer != null ? Data.MyMap.Tile[x2, y2].Layer.Length : 0; i2 < loopTo2; i2++)
                    {
                        ref var currentTile = ref Data.MyMap.Tile[x2, y2];
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
                MapEditorReplaceTile((MapLayer)GameState.CurLayer, GameState.CurX, GameState.CurY, withBlock);
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
            // Send the edited map to the server
            Map.SendMap();

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

            newTileX = GameState.EditorTileX;
            newTileY = GameState.EditorTileY;

            if (Conversions.ToBoolean(eraseTile))
            {
                newTileX = 0;
                newTileY = 0;
            }

            if (theAutotile > 0)
            {
                ref var withBlock = ref Data.MyMap.Tile[x, y];
                // set layer
                withBlock.Layer[CurLayer].X = newTileX;
                withBlock.Layer[CurLayer].Y = newTileY;
                if (Conversions.ToBoolean(eraseTile))
                {
                    withBlock.Layer[CurLayer].Tileset = 0;
                }
                else
                {
                    withBlock.Layer[CurLayer].Tileset = GameState.CurTileset;
                }
                withBlock.Layer[CurLayer].AutoTile = theAutotile;
                Autotile.CacheRenderState(x, y, CurLayer);

                // do a re-init so we can see our changes
                Autotile.InitAutotiles();
                return;
            }

            if (!multitile) // single
            {
                ref var withBlock1 = ref Data.MyMap.Tile[x, y];
                // set layer
                withBlock1.Layer[CurLayer].X = newTileX;
                withBlock1.Layer[CurLayer].Y = newTileY;
                if (Conversions.ToBoolean(eraseTile))
                {
                    withBlock1.Layer[CurLayer].Tileset = 0;
                }
                else
                {
                    withBlock1.Layer[CurLayer].Tileset = GameState.CurTileset;
                }
                withBlock1.Layer[CurLayer].AutoTile = 0;
                Autotile.CacheRenderState(x, y, CurLayer);
            }
            else // multitile
            {
                y2 = 0; // starting tile for y axis
                var loopTo = GameState.CurY + GameState.EditorTileHeight;
                for (y = GameState.CurY; y < loopTo; y++)
                {
                    x2 = 0; // re-set x count every y loop
                    var loopTo1 = GameState.CurX + GameState.EditorTileWidth;
                    for (x = GameState.CurX; x < loopTo1; x++)
                    {
                        if (x >= 0 & x < Data.MyMap.MaxX)
                        {
                            if (y >= 0 & y < Data.MyMap.MaxY)
                            {
                                ref var withBlock2 = ref Data.MyMap.Tile[x, y];
                                withBlock2.Layer[CurLayer].X = newTileX + x2;
                                withBlock2.Layer[CurLayer].Y = newTileY + y2;
                                if (Conversions.ToBoolean(eraseTile))
                                {
                                    withBlock2.Layer[CurLayer].Tileset = 0;
                                }
                                else
                                {
                                    withBlock2.Layer[CurLayer].Tileset = GameState.CurTileset;
                                }
                                withBlock2.Layer[CurLayer].AutoTile = 0;
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
                ref var withBlock = ref Data.MyMap.Tile[GameState.CurX, GameState.CurY];
                // Set tileset and directly apply the picked tile indices without invoking tileset-offset logic
                GameState.CurTileset = withBlock.Layer[CurLayer].Tileset;
                GameState.EditorTileX = withBlock.Layer[CurLayer].X;
                GameState.EditorTileY = withBlock.Layer[CurLayer].Y;
                GameState.EditorTileWidth = 1;
                GameState.EditorTileHeight = 1;
                GameState.EditorTileSelStart = new Point(GameState.EditorTileX, GameState.EditorTileY);
                GameState.EditorTileSelEnd = new Point(GameState.EditorTileX + 1, GameState.EditorTileY + 1);
                // Keep EyeDropper enabled until toggled off by user
            }
        }

        public static void Undo()
        {
            bool isModified = false;

            if (GameState.TileHistoryIndex <= 0)
            {
                return;
            }

            int layerCount = Enum.GetValues(typeof(MapLayer)).Length;

            for (int x = 0, loopTo = Data.MyMap.MaxX; x < loopTo; x++)
            {
                for (int y = 0, loopTo1 = Data.MyMap.MaxY; y < loopTo1; y++)
                {
                    for (int i = 0; i < layerCount; i++)
                    {
                        ref var currentTile = ref Data.MyMap.Tile[x, y];
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

            for (int x = 0, loopTo = Data.MyMap.MaxX; x < loopTo; x++)
            {
                for (int y = 0, loopTo1 = Data.MyMap.MaxY; y < loopTo1; y++)
                {
                    for (int i = 0; i < layerCount; i++)
                    {
                        ref var currentTile = ref Data.MyMap.Tile[x, y];
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
            Data.TempTile = new Tile[Data.MyMap.MaxX, Data.MyMap.MaxY];
            GameState.TmpMaxX = Data.MyMap.MaxX;
            GameState.TmpMaxY = Data.MyMap.MaxY;

            var loopTo = (int)Data.MyMap.MaxX;
            for (x = 0; x < loopTo; x++)
            {
                var loopTo1 = (int)Data.MyMap.MaxY;
                for (y = 0; y < loopTo1; y++)
                {
                    ref var withBlock = ref Data.MyMap.Tile[x, y];
                    Data.TempTile[x, y].Layer = new Type.Layer[layerCount];

                    Data.TempTile[x, y].Data1 = withBlock.Data1;
                    Data.TempTile[x, y].Data2 = withBlock.Data2;
                    Data.TempTile[x, y].Data3 = withBlock.Data3;
                    Data.TempTile[x, y].Type = withBlock.Type;
                    Data.TempTile[x, y].Data1_2 = withBlock.Data1_2;
                    Data.TempTile[x, y].Data2_2 = withBlock.Data2_2;
                    Data.TempTile[x, y].Data3_2 = withBlock.Data3_2;
                    Data.TempTile[x, y].Type2 = withBlock.Type2;
                    Data.TempTile[x, y].DirBlock = withBlock.DirBlock;

                    for (i = 0; i < layerCount; i++)
                    {
                        Data.TempTile[x, y].Layer[i].X = withBlock.Layer[i].X;
                        Data.TempTile[x, y].Layer[i].Y = withBlock.Layer[i].Y;
                        Data.TempTile[x, y].Layer[i].Tileset = withBlock.Layer[i].Tileset;
                        Data.TempTile[x, y].Layer[i].AutoTile = withBlock.Layer[i].AutoTile;
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

            Data.MyMap.MaxX = GameState.TmpMaxX;
            Data.MyMap.MaxY = GameState.TmpMaxY;

            var loopTo2 = (int)Data.MyMap.MaxX;
            for (x = 0; x < loopTo2; x++)
            {
                var loopTo3 = (int)Data.MyMap.MaxY;
                for (y = 0; y < loopTo3; y++)
                {
                    ref var withBlock1 = ref Data.MyMap.Tile[x, y];
                    Array.Resize(ref Data.MyMap.Tile[x, y].Layer, layerCount);
                    Array.Resize(ref Data.Autotile![x, y].Layer, layerCount);

                    withBlock1.Data1 = Data.TempTile![x, y].Data1;
                    withBlock1.Data2 = Data.TempTile![x, y].Data2;
                    withBlock1.Data3 = Data.TempTile![x, y].Data3;
                    withBlock1.Type = Data.TempTile![x, y].Type;
                    withBlock1.Data1_2 = Data.TempTile![x, y].Data1_2;
                    withBlock1.Data2_2 = Data.TempTile![x, y].Data2_2;
                    withBlock1.Data3_2 = Data.TempTile![x, y].Data3_2;
                    withBlock1.Type2 = Data.TempTile![x, y].Type2;
                    withBlock1.DirBlock = Data.TempTile![x, y].DirBlock;

                    for (i = 0; i < layerCount; i++)
                    {
                        withBlock1.Layer[i].X = Data.TempTile![x, y].Layer[i].X;
                        withBlock1.Layer[i].Y = Data.TempTile![x, y].Layer[i].Y;
                        withBlock1.Layer[i].Tileset = Data.TempTile![x, y].Layer[i].Tileset;
                        withBlock1.Layer[i].AutoTile = Data.TempTile![x, y].Layer[i].AutoTile;
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
        public static void MapEditorReplaceTile(MapLayer layer, int tileX, int tileY, Type.Tile oldTile)
        {
            int maxX = Data.MyMap.MaxX;
            int maxY = Data.MyMap.MaxY;

            for (int x = 0; x < maxX; x++)
            {
                for (int y = 0; y < maxY; y++)
                {
                    ref var tile = ref Data.MyMap.Tile[x, y];
                    if ((int)MapEditorTab.Tiles == GameState.MapEditorTab)
                    {
                        if (tile.Layer[(int)layer].X == oldTile.Layer[(int)layer].X && tile.Layer[(int)layer].Y == oldTile.Layer[(int)layer].Y)
                        {
                            if (GameClient.IsMouseButtonDown(MouseButton.Left))
                            {
                                tile.Layer[(int)layer].X = Data.MyMap.Tile[tileX, tileY].Layer[(int)layer].X;
                                tile.Layer[(int)layer].Y = Data.MyMap.Tile[tileX, tileY].Layer[(int)layer].Y;
                                tile.Layer[(int)layer].Tileset = Data.MyMap.Tile[tileX, tileY].Layer[(int)layer].Tileset;
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
                                tile.Data1 = Data.MyMap.Tile[tileX, tileY].Data1;
                                tile.Data2 = Data.MyMap.Tile[tileX, tileY].Data2;
                                tile.Data3 = Data.MyMap.Tile[tileX, tileY].Data3;
                                tile.Type = Data.MyMap.Tile[tileX, tileY].Type;
                            }
                            else
                            {
                                tile.Data1_2 = Data.MyMap.Tile[tileX, tileY].Data1_2;
                                tile.Data2_2 = Data.MyMap.Tile[tileX, tileY].Data2_2;
                                tile.Data3_2 = Data.MyMap.Tile[tileX, tileY].Data3_2;
                                tile.Type2 = Data.MyMap.Tile[tileX, tileY].Type2;
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
    }
}