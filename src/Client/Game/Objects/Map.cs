using Client.Game.UI;
using Client.Net;
using Core;
using Core.Configurations;
using Core.Globals;
using Core.Net;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Drawing;
using static Core.Globals.Command;
using static Core.Globals.Type;
using Type = Core.Globals.Type;

namespace Client
{
    public class Map
    {
        #region Drawing

        public static void DrawFog()
        {
            int fogNum = GameState.CurrentFog;

            if (fogNum <= 0 || fogNum > GameState.NumFogs)
                return;

            string argPath = System.IO.Path.Combine(DataPath.Fogs, fogNum.ToString());
            var gfxInfo = GameClient.GetGfxInfo(argPath);
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
                            rect.X = Data.MyMap.Tile[x, y].Layer[layerIndex].X * GameState.SizeX;
                            rect.Y = Data.MyMap.Tile[x, y].Layer[layerIndex].Y * GameState.SizeY;
                            rect.Width = GameState.SizeX;
                            rect.Height = GameState.SizeY;

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
                            GameClient.RenderTexture(ref argPath, GameLogic.ConvertMapX(x * GameState.SizeX), GameLogic.ConvertMapY(y * GameState.SizeY), rect.X, rect.Y, rect.Width, rect.Height, rect.Width, rect.Height, alpha);
                        }

                        // Autotile rendering state
                        else if (Data.Autotile[x, y].Layer[layerIndex].RenderState == GameState.RenderStateAutotile)
                        {
                            if (SettingsManager.Instance.Autotile)
                            {
                                DrawAutoTile(layerIndex, GameLogic.ConvertMapX(x * GameState.SizeX), GameLogic.ConvertMapY(y * GameState.SizeY), 1, x, y, 0, false);
                                DrawAutoTile(layerIndex, GameLogic.ConvertMapX(x * GameState.SizeX) + 16, GameLogic.ConvertMapY(y * GameState.SizeY), 2, x, y, 0, false);
                                DrawAutoTile(layerIndex, GameLogic.ConvertMapX(x * GameState.SizeX), GameLogic.ConvertMapY(y * GameState.SizeY) + 16, 3, x, y, 0, false);
                                DrawAutoTile(layerIndex, GameLogic.ConvertMapX(x * GameState.SizeX) + 16, GameLogic.ConvertMapY(y * GameState.SizeY) + 16, 4, x, y, 0, false);
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
                            rect.X = Data.MyMap.Tile[x, y].Layer[layerIndex].X * GameState.SizeX;
                            rect.Y = Data.MyMap.Tile[x, y].Layer[layerIndex].Y * GameState.SizeY;
                            rect.Width = GameState.SizeX;
                            rect.Height = GameState.SizeY;

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
                            GameClient.RenderTexture(ref argPath, GameLogic.ConvertMapX(x * GameState.SizeX), GameLogic.ConvertMapY(y * GameState.SizeY), rect.X, rect.Y, rect.Width, rect.Height, rect.Width, rect.Height, alpha);
                        }
                        // Handle autotile rendering
                        else if (Data.Autotile[x, y].Layer[layerIndex].RenderState == GameState.RenderStateAutotile)
                        {
                            if (SettingsManager.Instance.Autotile)
                            {
                                // Render autotiles
                                DrawAutoTile(layerIndex, GameLogic.ConvertMapX(x * GameState.SizeX), GameLogic.ConvertMapY(y * GameState.SizeY), 1, x, y, 0, false);
                                DrawAutoTile(layerIndex, GameLogic.ConvertMapX(x * GameState.SizeX) + 16, GameLogic.ConvertMapY(y * GameState.SizeY), 2, x, y, 0, false);
                                DrawAutoTile(layerIndex, GameLogic.ConvertMapX(x * GameState.SizeX), GameLogic.ConvertMapY(y * GameState.SizeY) + 16, 3, x, y, 0, false);
                                DrawAutoTile(layerIndex, GameLogic.ConvertMapX(x * GameState.SizeX) + 16, GameLogic.ConvertMapY(y * GameState.SizeY) + 16, 4, x, y, 0, false);
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

        public static void DrawAutoTile(int layerNum, int dX, int dY, int quarterNum, int x, int y, int forceFrame = 0, bool strict = true)
        {
            var yOffset = default(int);
            var xOffset = default(int);

            // calculate the offset
            if (forceFrame > 0)
            {
                switch (forceFrame - 1)
                {
                    case 0:
                    {
                        GameState.WaterfallFrame = 1;
                        break;
                    }
                    case 1:
                    {
                        GameState.WaterfallFrame = 2;
                        break;
                    }
                    case 2:
                    {
                        GameState.WaterfallFrame = 0;
                        break;
                    }
                }

                // animate autotiles
                switch (forceFrame - 1)
                {
                    case 0:
                    {
                        GameState.AutoTileFrame = 1;
                        break;
                    }
                    case 1:
                    {
                        GameState.AutoTileFrame = 2;
                        break;
                    }
                    case 2:
                    {
                        GameState.AutoTileFrame = 0;
                        break;
                    }
                }
            }

            switch (Data.MyMap.Tile[x, y].Layer[layerNum].AutoTile)
            {
                case GameState.AutotileWaterfall:
                {
                    yOffset = (GameState.WaterfallFrame - 1) * 32;
                    break;
                }
                case GameState.AutotileAnim:
                {
                    xOffset = GameState.AutoTileFrame * 64;
                    break;
                }
                case GameState.AutotileCliff:
                {
                    yOffset = -32;
                    break;
                }
            }

            if (Data.MyMap.Tile[x, y].Layer is null)
                return;
            string argPath = System.IO.Path.Combine(DataPath.Tilesets, Data.MyMap.Tile[x, y].Layer[layerNum].Tileset.ToString());
            if (Data.Autotile is null)
                return;
            GameClient.RenderTexture(ref argPath, dX, dY, Data.Autotile[x, y].Layer[layerNum].SrcX[quarterNum] + xOffset, Data.Autotile[x, y].Layer[layerNum].SrcY[quarterNum] + yOffset, 16, 16, 16, 16);
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
            GameClient.RenderTexture(ref argPath, 0, 0, 0, 0, GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Panoramas, index.ToString())).Width, GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Panoramas, index.ToString())).Height, GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Panoramas, index.ToString())).Width, GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Panoramas, index.ToString())).Height);
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
            GameClient.RenderTexture(ref argPath, (int) Math.Round(horz), (int) Math.Round(vert), 0, 0, GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Parallax, index.ToString())).Width, GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Parallax, index.ToString())).Height, GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Parallax, index.ToString())).Width, GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Parallax, index.ToString())).Height);
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

            // Determine position based on type
            switch ((PictureOrigin) type)
            {
                case PictureOrigin.TopLeft:
                    posX = 0 - Event.Picture.XOffset;
                    posY = 0 - Event.Picture.YOffset;
                    break;

                case PictureOrigin.CenterScreen:
                    posX = (int) Math.Round(GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Pictures, index.ToString())).Width / 2d - GameClient.GetGfxInfo(DataPath.Pictures + index).Width / 2d - Event.Picture.XOffset);
                    posY = (int) Math.Round(GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Pictures, index.ToString())).Height / 2d - GameClient.GetGfxInfo(DataPath.Pictures + index).Height / 2d - Event.Picture.YOffset);
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
            GameClient.RenderTexture(ref argPath, posX, posY, 0, 0, GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Pictures, index.ToString())).Width, GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Pictures, index.ToString())).Height, GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Pictures, index.ToString())).Width, GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Pictures, index.ToString())).Height);
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
    }

    #endregion
}