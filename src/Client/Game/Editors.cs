using System;
using Client.Game.UI;
using Client.Game.UI.Windows;
using Client.Net;
using Core;
using Core.Globals;
using Core.Net;
using Eto.Forms;
using Eto.Drawing;
using Type = Core.Globals.Type;

namespace Client
{

    public static class Editors
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

                GameState.EditorTileSelStart = new Microsoft.Xna.Framework.Point(GameState.EditorTileX, GameState.EditorTileY);
                GameState.EditorTileSelEnd = new Microsoft.Xna.Framework.Point(
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
                GameState.EditorTileSelStart = new Microsoft.Xna.Framework.Point(GameState.EditorTileX, GameState.EditorTileY);
                GameState.EditorTileSelEnd = new Microsoft.Xna.Framework.Point(GameState.EditorTileX + 1, GameState.EditorTileY + 1);
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
            Data.TempTile = new Type.Tile[Data.MyMap.MaxX, Data.MyMap.MaxY];
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
        public static void MapEditorReplaceTile(MapLayer layer, int tileX, int tileY, Core.Globals.Type.Tile oldTile)
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

        // Simple modal numeric prompt to replace VB Interaction.InputBox on cross-platform
        public static int? PromptIndex(Form owner, string title, string message, int min, int max, int defaultValue)
        {
            var dlg = new Dialog { Title = title, ClientSize = new Size(360, 140), Padding = 10 };
            var num = new NumericStepper { MinValue = min, MaxValue = max, Value = defaultValue, DecimalPlaces = 0 };
            var ok = new Button { Text = "OK" };
            var cancel = new Button { Text = "Cancel" };
            int? result = null;
            ok.Click += (s, e) => { result = (int)Math.Round(num.Value); dlg.Close(); };
            cancel.Click += (s, e) => { result = null; dlg.Close(); };
            var layout = new DynamicLayout { Spacing = new Size(6, 6) };
            layout.AddRow(new Label { Text = message });
            layout.AddRow(num);
            layout.AddRow(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 6, Items = { ok, cancel } });
            dlg.Content = layout;
            dlg.ShowModal(owner);
            return result;
        }

        #region Animation Editor

        public static void AnimationEditorInit()
        {  
            ref var withBlock = ref Data.Animation[GameState.EditorIndex];
            EnsureAnimationArrays(ref withBlock);
            if (string.IsNullOrEmpty(withBlock.Sound))
            {
                EditorAnimation.Instance!.cmbSound!.SelectedIndex = 0;
            }
            else
            {
                for (int i = 0, loopTo = EditorAnimation.Instance!.cmbSound!.Items.Count; i < loopTo; i++)
                {
                    var raw = EditorAnimation.Instance!.cmbSound!.Items[i];
                    string text = raw switch { Eto.Forms.ListItem li => li.Text, _ => raw?.ToString() ?? string.Empty };
                    if (text == withBlock.Sound)
                    {
                        EditorAnimation.Instance!.cmbSound!.SelectedIndex = i;
                        break;
                    }
                }
            }
            EditorAnimation.Instance!.txtName!.Text = withBlock.Name;

            EditorAnimation.Instance!.nudSprite0!.Value = withBlock.Sprite[0];
            EditorAnimation.Instance!.nudFrameCount0!.Value = withBlock.Frames[0];
            if (Data.Animation[GameState.EditorIndex].LoopCount[0] == 0)
                Data.Animation[GameState.EditorIndex].LoopCount[0] = 1;
            EditorAnimation.Instance!.nudLoopCount0!.Value = withBlock.LoopCount[0];
            if (Data.Animation[GameState.EditorIndex].LoopTime[0] == 0)
                Data.Animation[GameState.EditorIndex].LoopTime[0] = 1;
            EditorAnimation.Instance!.nudLoopTime0!.Value = withBlock.LoopTime[0];

            EditorAnimation.Instance!.nudSprite1!.Value = withBlock.Sprite[1];
            EditorAnimation.Instance!.nudFrameCount1!.Value = withBlock.Frames[1];
            if (Data.Animation[GameState.EditorIndex].LoopCount[1] == 0)
                Data.Animation[GameState.EditorIndex].LoopCount[1] = 1;
            EditorAnimation.Instance!.nudLoopCount1!.Value = withBlock.LoopCount[1];
            if (Data.Animation[GameState.EditorIndex].LoopTime[1] == 0)
                Data.Animation[GameState.EditorIndex].LoopTime[1] = 1;
            EditorAnimation.Instance!.nudLoopTime1!.Value = withBlock.LoopTime[1];

            GameState.AnimationChanged[GameState.EditorIndex] = true;
        }

        private static void EnsureAnimationArrays(ref Core.Globals.Type.Animation a)
        {
            // Ensure arrays exist and have at least length 2
            if (a.Sprite == null) a.Sprite = new int[2];
            else if (a.Sprite.Length < 2) Array.Resize(ref a.Sprite, 2);

            if (a.Frames == null) a.Frames = new int[2];
            else if (a.Frames.Length < 2) Array.Resize(ref a.Frames, 2);

            if (a.LoopCount == null) a.LoopCount = new int[2];
            else if (a.LoopCount.Length < 2) Array.Resize(ref a.LoopCount, 2);

            if (a.LoopTime == null) a.LoopTime = new int[2];
            else if (a.LoopTime.Length < 2) Array.Resize(ref a.LoopTime, 2);

            // Sensible minimums to prevent zero/invalid state
            if (a.LoopCount[0] == 0) a.LoopCount[0] = 1;
            if (a.LoopCount[1] == 0) a.LoopCount[1] = 1;
            if (a.LoopTime[0] == 0) a.LoopTime[0] = 1;
            if (a.LoopTime[1] == 0) a.LoopTime[1] = 1;
        }

        public static void AnimationEditorOK()
        {
            int i;

            for (i = 0; i < Variables.MaxAnimations; i++)
            {
                if (GameState.AnimationChanged[i])
                {
                    Sender.SendSaveAnimation(i);
                }
            }

            GameState.MyEditorType = EditorType.None;
            ClearChanged_Animation();
            Sender.SendCloseEditor();
        }

        public static void AnimationEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            ClearChanged_Animation();
            Animation.ClearAnimations();
            Sender.SendCloseEditor();
        }

        public static void ClearChanged_Animation()
        {
            for (int i = 0; i < Variables.MaxAnimations; i++)
                GameState.AnimationChanged[i] = false;
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
            ClearChanged_Npc();
            Sender.SendCloseEditor();
        }

        public static void NpcEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            ClearChanged_Npc();
            Database.ClearNpcs();
            Sender.SendCloseEditor();
        }

        public static void ClearChanged_Npc()
        {
            for (int i = 0; i < Variables.MaxNpcs; i++)
                GameState.NpcChanged[i] = false;
        }

        #endregion

        #region Resource Editor
        public static void ClearChanged_Resource()
        {
            GameState.ResourceChanged = new bool[Variables.MaxResources];
        }

        public static void ResourceEditorInit()
        {
            var withBlock = EditorResource.Instance;
            withBlock.txtName.Text = Data.Resource[GameState.EditorIndex].Name;
            withBlock.txtMessage.Text = Data.Resource[GameState.EditorIndex].SuccessMessage;
            withBlock.txtMessage2.Text = Data.Resource[GameState.EditorIndex].EmptyMessage;
            withBlock.cmbType.SelectedIndex = Data.Resource[GameState.EditorIndex].ResourceType;
            withBlock.nudNormalPic.Value = Data.Resource[GameState.EditorIndex].ResourceImage;
            withBlock.nudExhaustedPic.Value = Data.Resource[GameState.EditorIndex].ExhaustedImage;
            withBlock.cmbRewardItem.SelectedIndex = Data.Resource[GameState.EditorIndex].ItemReward;
            withBlock.nudRewardExp.Value = Data.Resource[GameState.EditorIndex].ExpReward;
            withBlock.cmbTool.SelectedIndex = Data.Resource[GameState.EditorIndex].ToolRequired;
            withBlock.nudHealth.Value = Data.Resource[GameState.EditorIndex].Health;
            withBlock.nudRespawn.Value = Data.Resource[GameState.EditorIndex].RespawnTime;
            withBlock.cmbAnimation.SelectedIndex = Data.Resource[GameState.EditorIndex].Animation;
            withBlock.nudLvlReq.Value = Data.Resource[GameState.EditorIndex].LvlRequired;
 
            GameState.ResourceChanged[GameState.EditorIndex] = true;
        }

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
            ClearChanged_Resource();
            Sender.SendCloseEditor();
        }

        public static void ResourceEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            ClearChanged_Resource();
            MapResource.ClearResources();
            Sender.SendCloseEditor();
        }

        #endregion

        #region Skill Editor

        public static void SkillEditorInit()
        {
            var withBlock = EditorSkill.Instance;

            withBlock.cmbAnimCast.SelectedIndex = 0;
            withBlock.cmbAnim.SelectedIndex = 0;

            // set values
            withBlock.txtName.Text = Strings.Trim(Data.Skill[GameState.EditorIndex].Name);
            withBlock.cmbType.SelectedIndex = Data.Skill[GameState.EditorIndex].Type;
            withBlock.nudMp.Value = Data.Skill[GameState.EditorIndex].MpCost;
            withBlock.nudLevel.Value = Data.Skill[GameState.EditorIndex].LevelReq;
            withBlock.cmbAccessReq.SelectedIndex = Data.Skill[GameState.EditorIndex].AccessReq;
            withBlock.cmbJob.SelectedIndex = Data.Skill[GameState.EditorIndex].JobReq;
            withBlock.nudCast.Value = Data.Skill[GameState.EditorIndex].CastTime;
            withBlock.nudCool.Value = Data.Skill[GameState.EditorIndex].CdTime;
            withBlock.nudIcon.Value = Data.Skill[GameState.EditorIndex].Icon;
            withBlock.nudMap.Value = Data.Skill[GameState.EditorIndex].Map;
            withBlock.nudX.Value = Data.Skill[GameState.EditorIndex].X;
            withBlock.nudY.Value = Data.Skill[GameState.EditorIndex].Y;
            withBlock.cmbDir.SelectedIndex = Data.Skill[GameState.EditorIndex].Dir;
            withBlock.nudVital.Value = Data.Skill[GameState.EditorIndex].Vital;
            withBlock.nudDuration.Value = Data.Skill[GameState.EditorIndex].Duration;
            withBlock.nudInterval.Value = Data.Skill[GameState.EditorIndex].Interval;
            withBlock.nudRange.Value = Data.Skill[GameState.EditorIndex].Range;

            withBlock.chkAoE.Checked = Data.Skill[GameState.EditorIndex].IsAoE;

            withBlock.nudAoE.Value = Data.Skill[GameState.EditorIndex].AoE;
            withBlock.cmbAnimCast.SelectedIndex = Data.Skill[GameState.EditorIndex].CastAnim;
            withBlock.cmbAnim.SelectedIndex = Data.Skill[GameState.EditorIndex].SkillAnim;
            withBlock.nudStun.Value = Data.Skill[GameState.EditorIndex].StunDuration;
            withBlock.SyncMultiDirMask();

            if (Data.Skill[GameState.EditorIndex].IsProjectile == 1)
            {
                withBlock.chkProjectile.Checked = true;
            }
            else
            {
                withBlock.chkProjectile.Checked = false;
            }
            withBlock.cmbProjectile.SelectedIndex = Data.Skill[GameState.EditorIndex].Projectile;

            if (Data.Skill[GameState.EditorIndex].KnockBack == 1)
            {
                withBlock.chkKnockBack.Checked = true;
            }
            else
            {
                withBlock.chkKnockBack.Checked = false;
            }
            withBlock.cmbKnockBackTiles.SelectedIndex = Data.Skill[GameState.EditorIndex].KnockBackTiles;
            withBlock.SyncMultiDirMask();

            // Chain skills: map -1 to None (0), otherwise +1 index
            int onHit = Data.Skill[GameState.EditorIndex].ChainOnHitSkillId;
            withBlock.cmbChainOnHit.SelectedIndex = onHit >= 0 && onHit < Variables.MaxSkills ? onHit + 1 : 0;

            // Common event init
            withBlock.cmbCommonEventType.SelectedIndex = Data.Skill[GameState.EditorIndex].CommonEventType;
            withBlock.nudCommonEventData1.Value = Data.Skill[GameState.EditorIndex].CommonEventData1;
            withBlock.nudCommonEventData2.Value = Data.Skill[GameState.EditorIndex].CommonEventData2;

            EditorSkill.Instance.DrawIcon();
          
            GameState.SkillChanged[GameState.EditorIndex] = true;
        }

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
            ClearChanged_Skill();
            Sender.SendCloseEditor();
        }

        public static void SkillEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            ClearChanged_Skill();
            Database.ClearSkills();
            Sender.SendCloseEditor();
        }

        public static void ClearChanged_Skill()
        {
            for (int i = 0; i < Variables.MaxSkills; i++)
                GameState.SkillChanged[i] = false;
        }

        #endregion

        #region Shop editor
        public static void ShopEditorInit()
        {            
            var withBlock = EditorShop.Instance;
            withBlock.txtName.Text = Data.Shop[GameState.EditorIndex].Name;

            if (Data.Shop[GameState.EditorIndex].BuyRate > 0)
            {
                withBlock.nudBuy.Value = Data.Shop[GameState.EditorIndex].BuyRate;
            }
            else
            {
                withBlock.nudBuy.Value = 100d;
            }

            withBlock.cmbItem.SelectedIndex = 0;
            withBlock.cmbCostItem.SelectedIndex = 0;
            
            UpdateShopTrade();
            GameState.ShopChanged[GameState.EditorIndex] = true;
        }

        public static void UpdateShopTrade()
        {
            int i;

            EditorShop.Instance.lstTradeItem.Items.Clear();

            for (i = 0; i < Variables.MaxTrades; i++)
            {
                {
                    ref var withBlock = ref Data.Shop[GameState.EditorIndex].TradeItem[i];
                    // if none, show as none
                    if (withBlock.Item == -1 & withBlock.CostItem == -1)
                    {
                        EditorShop.Instance.lstTradeItem.Items.Add("Empty Trade Slot");
                    }
                    else
                    {
                        EditorShop.Instance.lstTradeItem.Items.Add(i + 1 + ": " + withBlock.ItemValue + "x " + Data.Item[withBlock.Item].Name + " for " + withBlock.CostValue + "x " + Data.Item[withBlock.CostItem].Name);
                    }
                }
            }

            EditorShop.Instance.lstTradeItem.SelectedIndex = 0;
        }

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
            ClearChanged_Shop();
            Sender.SendCloseEditor();
        }

        public static void ShopEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            ClearChanged_Shop();
            Shop.ClearShops();
            Sender.SendCloseEditor();
        }

        public static void ClearChanged_Shop()
        {
            for (int i = 0; i < Variables.MaxShops; i++)
                GameState.ShopChanged[i] = false;
        }

        #endregion

        #region Job Editor
        public static void JobEditorOK()
        {
            for (int i = 0; i < Variables.MaxJobs; i++)
            {
                if (GameState.JobChanged[i])
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
            ClearChanged_Job();
            Database.ClearJobs();
            Sender.SendCloseEditor();
        }

        public static void JobEditorInit()
        {
            var withBlock = EditorJob.Instance;
            withBlock.txtName!.Text = Data.Job[GameState.EditorIndex].Name;
            withBlock.txtDescription!.Text = Data.Job[GameState.EditorIndex].Desc;
            if (Data.Job[GameState.EditorIndex].MaleSprite == 0)
                Data.Job[GameState.EditorIndex].MaleSprite = 1;
            withBlock.nudMaleSprite!.Value = Data.Job[GameState.EditorIndex].MaleSprite;
            if (Data.Job[GameState.EditorIndex].FemaleSprite == 0)
                Data.Job[GameState.EditorIndex].FemaleSprite = 1;
            withBlock.nudFemaleSprite!.Value = Data.Job[GameState.EditorIndex].FemaleSprite;

            withBlock.cmbItems!.SelectedIndex = 0;

            int statCount = Enum.GetValues(typeof(Stat)).Length;
            for (int i = 0; i < statCount; i++)
            {
                if (Data.Job[GameState.EditorIndex].Stat[i] == 0)
                    Data.Job[GameState.EditorIndex].Stat[i] = 1;
            }

            withBlock.nudStrength!.Value = Data.Job[GameState.EditorIndex].Stat[(int)Stat.Strength];
            withBlock.nudLuck!.Value = Data.Job[GameState.EditorIndex].Stat[(int)Stat.Luck];
            withBlock.nudIntelligence!.Value = Data.Job[GameState.EditorIndex].Stat[(int)Stat.Intelligence];
            withBlock.nudVitality!.Value = Data.Job[GameState.EditorIndex].Stat[(int)Stat.Vitality];
            withBlock.nudSpirit!.Value = Data.Job[GameState.EditorIndex].Stat[(int)Stat.Spirit];
            withBlock.nudBaseExp!.Value = Data.Job[GameState.EditorIndex].BaseExp;

            if (Data.Job[GameState.EditorIndex].StartMap == 0)
                Data.Job[GameState.EditorIndex].StartMap = 1;
            withBlock.nudStartMap!.Value = Data.Job[GameState.EditorIndex].StartMap;
            withBlock.nudStartX!.Value = Data.Job[GameState.EditorIndex].StartX;
            withBlock.nudStartY!.Value = Data.Job[GameState.EditorIndex].StartY;

            GameState.JobChanged[GameState.EditorIndex] = true;
            withBlock.DrawPreview();
        }

        public static void ClearChanged_Job()
        {
            for (int i = 0; i < Variables.MaxJobs; i++)
                GameState.JobChanged[i] = false;
        }


        public static void ItemEditorInit()
        {
            ref var withBlock = ref Data.Item[GameState.EditorIndex];
            EditorItem.Instance!.txtName!.Text = withBlock.Name;
            EditorItem.Instance!.txtDescription!.Text = withBlock.Description;

            if (withBlock.Icon > EditorItem.Instance!.nudIcon!.MaxValue)
                withBlock.Icon = 0;
            EditorItem.Instance!.nudIcon!.Value = withBlock.Icon;
            int itemCategoryCount = Enum.GetValues(typeof(ItemCategory)).Length;
            if (withBlock.Type < 0 || withBlock.Type >= itemCategoryCount)
                withBlock.Type = 0;
            EditorItem.Instance!.cmbType!.SelectedIndex = withBlock.Type;
            EditorItem.Instance!.cmbAnimation!.SelectedIndex = withBlock.Animation;

            if (withBlock.ItemLevel == 0)
                withBlock.ItemLevel = 1;
            EditorItem.Instance.nudItemLvl.Value = withBlock.ItemLevel;

            // Type specific settings
            if (EditorItem.Instance.cmbType.SelectedIndex == (int)ItemCategory.Equipment)
            {
                EditorItem.Instance!.fraEquipment!.Visible = true;
                EditorItem.Instance!.nudDamage!.Value = withBlock.Data2;
                EditorItem.Instance!.cmbTool!.SelectedIndex = withBlock.Data3;

                EditorItem.Instance!.cmbSubType!.SelectedIndex = withBlock.SubType;

                if (withBlock.Speed < 1000)
                    withBlock.Speed = 100;
                if (withBlock.Speed > EditorItem.Instance!.nudSpeed!.MaxValue)
                    withBlock.Speed = (int)Math.Round(EditorItem.Instance!.nudSpeed!.MaxValue);
                EditorItem.Instance!.nudSpeed!.Value = withBlock.Speed;

                EditorItem.Instance!.nudStrength!.Value = withBlock.AddStat[(int)Stat.Strength];
                EditorItem.Instance!.nudIntelligence!.Value = withBlock.AddStat[(int)Stat.Intelligence];
                EditorItem.Instance!.nudVitality!.Value = withBlock.AddStat[(int)Stat.Vitality];
                EditorItem.Instance!.nudLuck!.Value = withBlock.AddStat[(int)Stat.Luck];
                EditorItem.Instance!.nudSpirit!.Value = withBlock.AddStat[(int)Stat.Spirit];

                if (withBlock.KnockBack == 1)
                {
                    EditorItem.Instance!.chkKnockBack!.Checked = true;
                }
                else
                {
                    EditorItem.Instance!.chkKnockBack!.Checked = false;
                }
                EditorItem.Instance!.cmbKnockBackTiles!.SelectedIndex = withBlock.KnockBackTiles;
                EditorItem.Instance.nudPaperdoll.Value = withBlock.Paperdoll;

                if (withBlock.SubType == (byte)Equipment.Weapon)
                {
                    EditorItem.Instance!.fraProjectile!.Visible = true;
                }
                else
                {
                    EditorItem.Instance!.fraProjectile!.Visible = false;
                }
            }
            else
            {
                EditorItem.Instance!.fraEquipment!.Visible = false;
            }

            if (EditorItem.Instance.cmbType.SelectedIndex == (int)ItemCategory.Consumable)
            {
                EditorItem.Instance!.fraVitals!.Visible = true;
                EditorItem.Instance!.nudVitalMod!.Value = withBlock.Data1;
            }
            else
            {
                EditorItem.Instance!.fraVitals!.Visible = false;
            }

            if (EditorItem.Instance.cmbType.SelectedIndex == (int)ItemCategory.Skill)
            {
                EditorItem.Instance!.fraSkill!.Visible = true;
                EditorItem.Instance!.cmbSkills!.SelectedIndex = withBlock.Data1;
            }
            else
            {
                EditorItem.Instance!.fraSkill!.Visible = false;
            }

            if (EditorItem.Instance.cmbType.SelectedIndex == (int)ItemCategory.Projectile)
            {
                EditorItem.Instance!.fraProjectile!.Visible = true;
                EditorItem.Instance!.fraEquipment!.Visible = true;
            }
            else if (withBlock.Type != (byte)ItemCategory.Equipment)
            {
                EditorItem.Instance!.fraProjectile!.Visible = false;
            }

            if (EditorItem.Instance.cmbType.SelectedIndex == (int)ItemCategory.Event)
            {
                EditorItem.Instance!.fraEvents!.Visible = true;
                EditorItem.Instance!.nudEvent!.Value = withBlock.Data1;
                EditorItem.Instance!.nudEventValue!.Value = withBlock.Data2;
            }
            else
            {
                EditorItem.Instance!.fraEvents!.Visible = false;
            }

            // Projectile
            EditorItem.Instance!.cmbProjectile!.SelectedIndex = withBlock.Projectile;
            EditorItem.Instance!.cmbAmmo!.SelectedIndex = withBlock.Ammo + 1;

            // Basic requirements
            EditorItem.Instance!.cmbAccessReq!.SelectedIndex = withBlock.AccessReq;
            EditorItem.Instance!.nudLevelReq!.Value = withBlock.LevelReq;

            EditorItem.Instance!.nudStrReq!.Value = withBlock.StatReq[(int)Stat.Strength];
            EditorItem.Instance!.nudVitReq!.Value = withBlock.StatReq[(int)Stat.Vitality];
            EditorItem.Instance!.nudLuckReq!.Value = withBlock.StatReq[(int)Stat.Luck];
            EditorItem.Instance!.nudIntReq!.Value = withBlock.StatReq[(int)Stat.Intelligence];
            EditorItem.Instance!.nudSprReq!.Value = withBlock.StatReq[(int)Stat.Spirit];

            // Build cmbJobReq
            EditorItem.Instance!.cmbJobReq!.Items.Clear();
            for (int j = 0; j < Variables.MaxJobs; j++)
                EditorItem.Instance!.cmbJobReq!.Items.Add(Data.Job[j].Name);

            EditorItem.Instance!.cmbJobReq!.SelectedIndex = withBlock.JobReq;
            // Info
            EditorItem.Instance!.nudPrice!.Value = withBlock.Price;
            EditorItem.Instance!.cmbBind!.SelectedIndex = withBlock.BindType;
            EditorItem.Instance!.nudRarity!.Value = withBlock.Rarity;

            if (withBlock.Stackable == 1)
            {
                EditorItem.Instance!.chkStackable!.Checked = true;
            }
            else
            {
                EditorItem.Instance!.chkStackable!.Checked = false;
            }

            EditorItem.Instance!.DrawIcon();

            GameState.ItemChanged[GameState.EditorIndex] = true;
        }

        public static void ItemEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            Item.ClearChangedItem();
            Item.ClearItems();
            Sender.SendCloseEditor();
        }

        public static void ItemEditorOK()
        {
            int i;

            for (i = 0; i < Variables.MaxItems; i++)
            {
                if (GameState.ItemChanged[i])
                {
                    Sender.SendSaveItem(i);
                }
            }

            GameState.MyEditorType = EditorType.None;
            Item.ClearChangedItem();
            Sender.SendCloseEditor();
        }

        #endregion

        #region Moral Editor
        public static void MoralEditorOK()
        {
            for (int i = 0; i < Variables.MaxMorals; i++)
            {
                if (GameState.MoralChanged[i])
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
            ClearChanged_Moral();
            Moral.ClearMorals();
            Sender.SendCloseEditor();
        }

        public static void MoralEditorInit()
        {
            var moralBlock = EditorMoral.Instance;
            moralBlock.txtName!.Text = Data.Moral[GameState.EditorIndex].Name;
            moralBlock.cmbColor!.SelectedIndex = Data.Moral[GameState.EditorIndex].Color;
            moralBlock.chkCanCast!.Checked = Data.Moral[GameState.EditorIndex].CanCast;
            moralBlock.chkCanPK!.Checked = Data.Moral[GameState.EditorIndex].CanPk;
            moralBlock.chkCanPickupItem!.Checked = Data.Moral[GameState.EditorIndex].CanPickupItem;
            moralBlock.chkCanDropItem!.Checked = Data.Moral[GameState.EditorIndex].CanDropItem;
            moralBlock.chkCanUseItem!.Checked = Data.Moral[GameState.EditorIndex].CanUseItem;
            moralBlock.chkDropItems!.Checked = Data.Moral[GameState.EditorIndex].DropItems;
            moralBlock.chkLoseExp!.Checked = Data.Moral[GameState.EditorIndex].LoseExp;
            moralBlock.chkPlayerBlock!.Checked = Data.Moral[GameState.EditorIndex].PlayerBlock;
            moralBlock.chkNpcBlock!.Checked = Data.Moral[GameState.EditorIndex].NpcBlock;
            GameState.MoralChanged[GameState.EditorIndex] = true;
        }

        public static void ClearChanged_Moral()
        {
            for (int i = 0; i < Variables.MaxMorals; i++)
                GameState.MoralChanged[i] = false;
        }
        #endregion

        #region Projectile Editor
        public static void ProjectileEditorInit()
        {            
            ref var withBlock = ref Data.Projectile[GameState.EditorIndex];
            EditorProjectile.Instance.txtName.Text = withBlock.Name;
            EditorProjectile.Instance.nudPic.Value = withBlock.Sprite;
            EditorProjectile.Instance.nudRange.Value = withBlock.Range;
            EditorProjectile.Instance.nudSpeed.Value = withBlock.Speed;
            EditorProjectile.Instance.nudDamage.Value = withBlock.Damage;
            EditorProjectile.Instance.cmbPlayAnimHit.SelectedIndex = Math.Clamp(withBlock.Animation, 0, Variables.MaxAnimations);
            EditorProjectile.Instance.Drawicon();
            GameState.ProjectileChanged[GameState.EditorIndex] = true;
        }

        public static void ProjectileEditorOK()
        {
            for (int i = 0; i < Variables.MaxProjectiles;  i++)
            {
                if (GameState.ProjectileChanged[i])
                {
                    Projectile.SendSaveProjectile(i);
                }
            }

            GameState.MyEditorType = EditorType.None;
            ClearChanged_Projectile();
            Sender.SendCloseEditor();
        }

        public static void ProjectileEditorCancel()
        {
            GameState.MyEditorType = EditorType.None;
            ClearChanged_Projectile();
            Projectile.ClearProjectile();
            Sender.SendCloseEditor();
        }

        public static void ClearChanged_Projectile()
        {
            for (int i = 0; i < Variables.MaxProjectiles;  i++)
                GameState.ProjectileChanged[i] = false;

        }

        #endregion

    }
}