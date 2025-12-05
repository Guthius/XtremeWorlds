using System;
using System.Drawing;
using Client.Net;
using Core;
using Core.Globals;
using Core.Interfaces;
using Core.Net;

namespace Client
{

    public class MapResource : IData
    {

        #region Database

        public static void OnClear(int index)
        {
            Data.Resource[index] = default;
            Data.Resource[index].Name = "";
            GameState.ResourceLoaded[index] = 0;
        }

        public static void OnReset()
        {
            Data.Resource = new Core.Globals.Type.Resource[Variables.MaxResources];

            for (int i = 0; i < Variables.MaxResources; i++)
                OnClear(i);

        }

        public static void OnStream(int resourceNum)
        {
            if (resourceNum >= 0 && string.IsNullOrEmpty(Data.Resource[resourceNum].Name) && GameState.ResourceLoaded[resourceNum] == 0)
            {
                GameState.ResourceLoaded[resourceNum] = 1;
                Sender.SendRequestResource(resourceNum);
            }
        }

        #endregion

        #region Drawing

        public static void OnDraw(int resource, int dx, int dy, System.Drawing.Rectangle rec)
        {
            int x;
            int y;
            int width;
            int height;

            if (resource < 1 | resource > GameState.NumResources)
                return;

            x = GameLogic.ConvertMapX(dx);
            y = GameLogic.ConvertMapY(dy);
            width = rec.Right - rec.Left;
            height = rec.Bottom - rec.Top;

            if (rec.Width < 0 | rec.Height < 0)
                return;

            string argPath = System.IO.Path.Combine(DataPath.Resources, resource.ToString());
            GameClient.RenderTexture(ref argPath, x, y, rec.X, rec.Y, rec.Width, rec.Height, rec.Width, rec.Height);
        }

        public static void OnDraw(int resourceNum)
        {
            int mapResourceNum;
            int resourceState;
            var resourceSprite = default(int);
            var rec = default(System.Drawing.Rectangle);
            int x;
            int y;

            if (GameState.GettingMap)
                return;

            if (GameState.MapEditorTab != (byte)MapEditorTab.Tiles && GameState.MyEditorType == EditorType.Map)
                return;

            if (!GameState.MapData)
                return;

            if (Data.MyMapResource[resourceNum].X > Data.MyMap.MaxX | Data.MyMapResource[resourceNum].Y > Data.MyMap.MaxY)
                return;

            mapResourceNum = Data.MyMap.Tile[Data.MyMapResource[resourceNum].X, Data.MyMapResource[resourceNum].Y].Data1;

            if (mapResourceNum == 0)
                mapResourceNum = Data.MyMap.Tile[Data.MyMapResource[resourceNum].X, Data.MyMapResource[resourceNum].Y].Data1_2;

            OnStream(mapResourceNum);

            if (Data.Resource[mapResourceNum].ResourceImage == 0)
                return;

            // Get the Resource state
            resourceState = Data.MyMapResource[resourceNum].State;

            if (resourceState == 0) // normal
            {
                resourceSprite = Data.Resource[mapResourceNum].ResourceImage;
            }
            else if (resourceState == 1) // used
            {
                resourceSprite = Data.Resource[mapResourceNum].ExhaustedImage;
            }

            var gfxInfo = GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Resources, resourceSprite.ToString()));
            if (gfxInfo == null)
                return;

            // src rect
            rec.Y = 0;
            rec.Height = gfxInfo.Height;
            rec.X = 0;
            rec.Width = gfxInfo.Width;

            // Set base x + y, then the offset due to size
            x = (int)Math.Round(Data.MyMapResource[resourceNum].X * Constants.TileSize - gfxInfo.Width / 2d + 16d);
            y = Data.MyMapResource[resourceNum].Y * Constants.TileSize - gfxInfo.Height + 32;

            OnDraw(resourceSprite, x, y, rec);
        }

        public static void OnLoad(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnSave(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnUpdate(int index)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}