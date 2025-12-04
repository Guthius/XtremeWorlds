using Client.Game.UI;
using Client.Net;
using Core;
using Core.Configurations;
using Core.Globals;
using Core.Interfaces;
using Core.Net;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Drawing;
using static Core.Globals.Command;
using static Core.Globals.Type;
using Type = Core.Globals.Type;

namespace Client
{
    public class Map : IData
    {
        #region Drawing

        public static void DrawFog()
        {
            int fogNum = GameState.CurrentFog;

            if (fogNum <= 0 || fogNum > GameState.NumFogs)
                return;

            string argPath = System.IO.Path.Combine(DataPath.Fogs, fogNum.ToString());
            var gfxInfo = GameClient.GetGfxInfo(argPath);
            if (gfxInfo == null)
            {
                return;
            }
            int sW = gfxInfo.Width;
            int sH = gfxInfo.Height;

            // Calculate how many tiles are needed to cover the screen
            int screenW = GameState.ResolutionWidth;
            int screenH = GameState.ResolutionHeight;

            // Wrap fog offset so it scrolls smoothly and never leaves a gap
            int offsetX = (int)(GameState.FogOffsetX % sW);
            int offsetY = (int)(GameState.FogOffsetY % sH);
            if (offsetX > 0) offsetX -= sW;
            if (offsetY > 0) offsetY -= sH;

            // Draw the fog texture repeatedly to fill the screen
            float fogAlpha = GameState.CurrentFogOpacity / 255f;
            for (int x = offsetX; x < screenW; x += sW)
            {
                for (int y = offsetY; y < screenH; y += sH)
                {
                    GameClient.RenderTexture(ref argPath, x, y, 0, 0, sW, sH, sW, sH, fogAlpha);
                }
            }
        }

        public static void DrawMapGroundTile(int x, int y)
        {
            int i;
            float alpha;
            var rect = new System.Drawing.Rectangle(0, 0, 0, 0);

            // Check if the map or its tile data is not ready
            if (GameState.GettingMap || !GameState.MapData)
                return;

            // Ensure x and y are within the bounds of the map
            if (x < 0 || y < 0 || x >= Data.MyMap.MaxX || y >= Data.MyMap.MaxY)
                return;

            // Check for null Layer arrays (cannot check struct for null, but can check Layer property)
            if (Data.MyMap.Tile[x, y].Layer == null)
                return;

            if (Data.Autotile?[x, y].Layer == null)
                return;

            try
            {
                for (i = (int) MapLayer.Ground; i <= (int) MapLayer.CoverAnimation; i++)
                {
                    int layerIndex = i;

                    // Handle animated layers
                    if (GameState.MapAnim)
                    {
                        switch (i)
                        {
                            case (int) MapLayer.Mask:
                                if (Data.MyMap.Tile[x, y].Layer != null &&
                                    Data.MyMap.Tile[x, y].Layer.Length > (int) MapLayer.MaskAnimation &&
                                    Data.MyMap.Tile[x, y].Layer[(int) MapLayer.MaskAnimation].Tileset > 0)
                                    layerIndex = (int) MapLayer.MaskAnimation;
                                break;
                            case (int) MapLayer.Cover:
                                if (Data.MyMap.Tile[x, y].Layer != null &&
                                    Data.MyMap.Tile[x, y].Layer.Length > (int) MapLayer.CoverAnimation &&
                                    Data.MyMap.Tile[x, y].Layer[(int) MapLayer.CoverAnimation].Tileset > 0)
                                    layerIndex = (int) MapLayer.CoverAnimation;
                                break;
                        }
                    }
                    else
                    {
                        // Skip non-animated layers
                        if (i == (int) MapLayer.MaskAnimation || i == (int) MapLayer.CoverAnimation)
                            continue;
                    }

                    // Check if this layer has a valid tileset and array is large enough
                    if (Data.MyMap.Tile[x, y].Layer != null &&
                        Data.MyMap.Tile[x, y].Layer.Length > layerIndex &&
                        Data.Autotile[x, y].Layer != null &&
                        Data.Autotile[x, y].Layer.Length > layerIndex &&
                        Data.MyMap.Tile[x, y].Layer[layerIndex].Tileset > 0 &&
                        Data.MyMap.Tile[x, y].Layer[layerIndex].Tileset <= GameState.NumTileSets)
                    {
                        // Normal rendering state
                        if (Data.Autotile[x, y].Layer[layerIndex].RenderState == GameState.RenderStateNormal)
                        {
                            rect.X = Data.MyMap.Tile[x, y].Layer[layerIndex].X * Constants.TileSize;
                            rect.Y = Data.MyMap.Tile[x, y].Layer[layerIndex].Y * Constants.TileSize;
                            rect.Width = Constants.TileSize;
                            rect.Height = Constants.TileSize;

                            alpha = 1.0f;

                            if (GameState.MyEditorType == EditorType.Map)
                            {
                                if (GameState.HideLayers)
                                {
                                    if (i != GameState.CurLayer)
                                    {
                                        alpha = 0.5f;
                                    }
                                }
                            }

                            // Render the tile
                            string argPath = System.IO.Path.Combine(DataPath.Tilesets, Data.MyMap.Tile[x, y].Layer[layerIndex].Tileset.ToString());
                            GameClient.RenderTexture(ref argPath, GameLogic.ConvertMapX(x * Constants.TileSize), GameLogic.ConvertMapY(y * Constants.TileSize), rect.X, rect.Y, rect.Width, rect.Height, rect.Width, rect.Height, alpha);
                        }

                        // Autotile rendering state
                        else if (Data.Autotile[x, y].Layer[layerIndex].RenderState == GameState.RenderStateAutotile)
                        {
                            if (SettingsManager.Instance.Autotile)
                            {
                                Autotile.OnDraw(layerIndex, GameLogic.ConvertMapX(x * Constants.TileSize), GameLogic.ConvertMapY(y * Constants.TileSize), 1, x, y, 0, false);
                                Autotile.OnDraw(layerIndex, GameLogic.ConvertMapX(x * Constants.TileSize) + 16, GameLogic.ConvertMapY(y * Constants.TileSize), 2, x, y, 0, false);
                                Autotile.OnDraw(layerIndex, GameLogic.ConvertMapX(x * Constants.TileSize), GameLogic.ConvertMapY(y * Constants.TileSize) + 16, 3, x, y, 0, false);
                                Autotile.OnDraw(layerIndex, GameLogic.ConvertMapX(x * Constants.TileSize) + 16, GameLogic.ConvertMapY(y * Constants.TileSize) + 16, 4, x, y, 0, false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void DrawMapRoofTile(int x, int y)
        {
            int i;
            float alpha;
            var rect = default(System.Drawing.Rectangle);

            // Exit early if map is still loading or tile data is not available
            if (GameState.GettingMap || !GameState.MapData)
                return;

            // Ensure x and y are within valid map bounds
            if (x < 0 || y < 0 || x >= Data.MyMap.MaxX || y >= Data.MyMap.MaxY)
                return;

            // Check for null Layer arrays (cannot check struct for null, but can check Layer property)
            if (Data.MyMap.Tile[x, y].Layer == null)
                return;

            if (Data.Autotile?[x, y].Layer == null)
                return;

            try
            {
                // Loop through the layers from Fringe to RoofAnim
                for (i = (int) MapLayer.Fringe; i <= (int) MapLayer.RoofAnimation; i++)
                {
                    int layerIndex = i;

                    // Handle animated layers
                    if (GameState.MapAnim)
                    {
                        switch (i)
                        {
                            case (int) MapLayer.Fringe:
                                if (Data.MyMap.Tile[x, y].Layer?.Length > (int) MapLayer.FringeAnimation &&
                                    Data.MyMap.Tile[x, y].Layer[(int) MapLayer.FringeAnimation].Tileset > 0)
                                    layerIndex = (int) MapLayer.FringeAnimation;
                                break;
                            case (int) MapLayer.Roof:
                                if (Data.MyMap.Tile[x, y].Layer.Length > (int) MapLayer.RoofAnimation &&
                                    Data.MyMap.Tile[x, y].Layer[(int) MapLayer.RoofAnimation].Tileset > 0)
                                    layerIndex = (int) MapLayer.RoofAnimation;
                                break;
                        }
                    }
                    else
                    {
                        // Skip non-animated layers
                        if (i == (int) MapLayer.FringeAnimation || i == (int) MapLayer.RoofAnimation)
                            continue;
                    }

                    // Check if this layer has a valid tileset and array is large enough
                    if (Data.MyMap.Tile[x, y].Layer != null &&
                        Data.MyMap.Tile[x, y].Layer.Length > layerIndex &&
                        Data.Autotile[x, y].Layer != null &&
                        Data.Autotile[x, y].Layer.Length > layerIndex &&
                        Data.MyMap.Tile[x, y].Layer[layerIndex].Tileset > 0 &&
                        Data.MyMap.Tile[x, y].Layer[layerIndex].Tileset <= GameState.NumTileSets)
                    {
                        // Check if the render state is normal and render the tile
                        if (Data.Autotile[x, y].Layer[layerIndex].RenderState == GameState.RenderStateNormal)
                        {
                            rect.X = Data.MyMap.Tile[x, y].Layer[layerIndex].X * Constants.TileSize;
                            rect.Y = Data.MyMap.Tile[x, y].Layer[layerIndex].Y * Constants.TileSize;
                            rect.Width = Constants.TileSize;
                            rect.Height = Constants.TileSize;

                            alpha = 1.0f;

                            if (GameState.MyEditorType == EditorType.Map)
                            {
                                if (GameState.HideLayers)
                                {
                                    if (i != GameState.CurLayer)
                                    {
                                        alpha = 0.5f;
                                    }
                                }
                            }

                            // Render the tile with the calculated rectangle and transparency
                            string argPath = System.IO.Path.Combine(DataPath.Tilesets, Data.MyMap.Tile[x, y].Layer[layerIndex].Tileset.ToString());
                            GameClient.RenderTexture(ref argPath, GameLogic.ConvertMapX(x * Constants.TileSize), GameLogic.ConvertMapY(y * Constants.TileSize), rect.X, rect.Y, rect.Width, rect.Height, rect.Width, rect.Height, alpha);
                        }
                        // Handle autotile rendering
                        else if (Data.Autotile[x, y].Layer[layerIndex].RenderState == GameState.RenderStateAutotile)
                        {
                            if (SettingsManager.Instance.Autotile)
                            {
                                // Render autotiles
                                Autotile.OnDraw(layerIndex, GameLogic.ConvertMapX(x * Constants.TileSize), GameLogic.ConvertMapY(y * Constants.TileSize), 1, x, y, 0, false);
                                Autotile.OnDraw(layerIndex, GameLogic.ConvertMapX(x * Constants.TileSize) + 16, GameLogic.ConvertMapY(y * Constants.TileSize), 2, x, y, 0, false);
                                Autotile.OnDraw(layerIndex, GameLogic.ConvertMapX(x * Constants.TileSize), GameLogic.ConvertMapY(y * Constants.TileSize) + 16, 3, x, y, 0, false);
                                Autotile.OnDraw(layerIndex, GameLogic.ConvertMapX(x * Constants.TileSize) + 16, GameLogic.ConvertMapY(y * Constants.TileSize) + 16, 4, x, y, 0, false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public static void DrawMapTint()
        {
            if (Conversions.ToInteger(Data.MyMap.MapTint) == 0)
                return; // Skip if no tint is applied

            var tintColor = new Microsoft.Xna.Framework.Color(GameState.CurrentTintR, GameState.CurrentTintG, GameState.CurrentTintB, GameState.CurrentTintA);
            GameClient.DrawRectangle(
                new Microsoft.Xna.Framework.Vector2(0, 0),
                new Microsoft.Xna.Framework.Vector2(GameState.ResolutionWidth, GameState.ResolutionHeight),
                tintColor,
                Microsoft.Xna.Framework.Color.Transparent,
                0f);
        }

        public static void DrawMapFade()
        {
            if (!GameState.UseFade)
                return; // Exit if fading is disabled

            var fadeColor = new Microsoft.Xna.Framework.Color(0, 0, 0, GameState.FadeAmount);
            GameClient.DrawRectangle(
                new Microsoft.Xna.Framework.Vector2(0, 0),
                new Microsoft.Xna.Framework.Vector2(GameState.ResolutionWidth, GameState.ResolutionHeight),
                fadeColor,
                Microsoft.Xna.Framework.Color.Transparent,
                0f);
        }

        public static void DrawPanorama(int index)
        {
            if (Data.MyMap.Indoors)
                return;

            if (index < 1 | index > GameState.NumPanoramas)
                return;

            string argPath = System.IO.Path.Combine(DataPath.Panoramas, index.ToString());
            var gfx = GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Panoramas, index.ToString()));
            if (gfx == null)
            {
                return;
            }
            GameClient.RenderTexture(ref argPath, 0, 0, 0, 0, gfx.Width, gfx.Height, gfx.Width, gfx.Height);
        }

        public static void DrawParallax(int index)
        {
            float horz = 0f;
            float vert = 0f;

            if (Data.MyMap.Moral == Conversions.ToShort(Data.MyMap.Indoors))
                return;

            if (index < 1 | index > GameState.NumParallax)
                return;

            // Calculate horizontal and vertical offsets based
            // yer position
            horz = GameLogic.ConvertMapX(GetPlayerX(GameState.MyIndex)) * 2.5f - 50f;
            vert = GameLogic.ConvertMapY(GetPlayerY(GameState.MyIndex)) * 2.5f - 50f;

            string argPath = System.IO.Path.Combine(DataPath.Parallax, index.ToString());
            var gfx = GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Parallax, index.ToString()));
            if (gfx == null)
            {
                return;
            }
            GameClient.RenderTexture(ref argPath, (int) Math.Round(horz), (int) Math.Round(vert), 0, 0, gfx.Width, gfx.Height, gfx.Width, gfx.Height);
        }

        public static void DrawPicture(int index = 0, int type = 0)
        {
            if (index == 0)
            {
                index = Event.Picture.Index;
            }

            if (type == 0)
            {
                type = Event.Picture.SpriteType;
            }

            // Use enum values for comparison
            if (index < 1 || index > GameState.NumPictures)
                return;

            if (type < (int) PictureOrigin.TopLeft || type > (int) PictureOrigin.CenterOnPlayer)
                return;

            int posX = 0;
            int posY = 0;
            var gfx = GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Pictures, index.ToString()));
            if (gfx == null)
            {
                return;
            }

            // Determine position based on type
            switch ((PictureOrigin) type)
            {
                case PictureOrigin.TopLeft:
                    posX = 0 - Event.Picture.XOffset;
                    posY = 0 - Event.Picture.YOffset;
                    break;

                case PictureOrigin.CenterScreen:
                    posX = (int) Math.Round(gfx.Width / 2d - gfx.Width / 2d - Event.Picture.XOffset);
                    posY = (int) Math.Round(gfx.Height / 2d - gfx.Height / 2d - Event.Picture.YOffset);
                    break;

                case PictureOrigin.CenterOnEvent:
                    if (GameState.CurrentEvents < Event.Picture.EventId)
                    {
                        // Reset picture details and exit if event is invalid
                        Event.Picture.EventId = 0;
                        Event.Picture.Index = 0;
                        Event.Picture.SpriteType = 0;
                        Event.Picture.XOffset = 0;
                        Event.Picture.YOffset = 0;
                        return;
                    }

                    if (Data.MapEvents == null)
                    {
                        return;
                    
                    }
                    posX = (int) Math.Round(GameLogic.ConvertMapX(Data.MapEvents[Event.Picture.EventId].X) / 2d - Event.Picture.XOffset);
                    posY = (int) Math.Round(GameLogic.ConvertMapY(Data.MapEvents[Event.Picture.EventId].Y) / 2d - Event.Picture.YOffset);
                    break;

                case PictureOrigin.CenterOnPlayer:
                    posX = (int) Math.Round(GameLogic.ConvertMapX(Data.Player[GameState.MyIndex].X) / 2d - Event.Picture.XOffset);
                    posY = (int) Math.Round(GameLogic.ConvertMapY(Data.Player[GameState.MyIndex].Y) / 2d - Event.Picture.YOffset);
                    break;
            }

            string argPath = System.IO.Path.Combine(DataPath.Pictures, index.ToString());
            GameClient.RenderTexture(ref argPath, posX, posY, 0, 0, gfx.Width, gfx.Height, gfx.Width, gfx.Height);
        }

        public static void OnClear()
        {
            // Reset basic map properties
            Data.MyMap.Name = string.Empty;
            Data.MyMap.Tileset = 1;
            Data.MyMap.MaxX = Variables.MaxMapX;
            Data.MyMap.MaxY = Variables.MaxMapY;
            Data.MyMap.BootMap = 0;
            Data.MyMap.BootX = 0;
            Data.MyMap.BootY = 0;
            Data.MyMap.Down = 0;
            Data.MyMap.Left = 0;
            Data.MyMap.Moral = 0;
            Data.MyMap.Music = string.Empty;
            Data.MyMap.Revision = 0;
            Data.MyMap.Right = 0;
            Data.MyMap.Up = 0;

            // Initialize Npc and Tile arrays
            Data.MyMap.Npc = new int[Variables.MaxMapNpcs];

            for (int i = 0; i < Variables.MaxMapNpcs; i++)
            {
                Data.MyMap.Npc[i] = -1;
            }

            Data.MyMap.Tile = new Type.Tile[Data.MyMap.MaxX, Data.MyMap.MaxY];
            Data.TileHistory = new Type.TileHistory[GameState.MaxTileHistory]; // Fixed type name

            // Reset tile history indices
            GameState.TileHistoryIndex = 0;

            for (int i = 0; i < GameState.MaxTileHistory; i++)
            {
                Data.TileHistory[i].Tile = new Type.Tile[Data.MyMap.MaxX, Data.MyMap.MaxY];
            }
      
            // Clear map events
            Data.MapEvents = new Type.MapEvent[Data.MyMap.EventCount];

            for (int i = 0, loopTo = Data.MyMap.EventCount; i < loopTo; i++)
            {
                Data.MapEvents = default;
            }

            GameState.CurrentEvents = 0;

            for (int i = 0; i < Variables.MaxMapNpcs; i++)
            {
                MapNpc.OnClear(i);
            }

            for (int i = 0; i < Variables.MaxMapItems; i++)
            {
                MapItem.OnClear(i);
            }
        }

        public static void OnDraw(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnClear(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnStream(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnReset()
        {
            throw new NotImplementedException();
        }

        public static void OnLoad(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnSave(int index)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}