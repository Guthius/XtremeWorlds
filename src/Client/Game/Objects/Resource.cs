using System;
using System.Drawing;
using Client.Net;
using Core;
using Core.Globals;
using Core.Net;

namespace Client
{

    public class MapResource : IContent
    {

        #region Database

        public void OnClear(int index)
        {
            Data.Resource[index] = default;
            Data.Resource[index].Name = "";
            GameState.ResourceLoaded[index] = 0;
        }

        public void OnReset()
        {
            Array.Resize(ref Data.Resource, Variables.MaxResources);

            for (int i = 0; i < Variables.MaxResources; i++)
                OnClear(i);

        }

        public void OnStream(int index)
        {
            if (index >= 0 && string.IsNullOrEmpty(Data.Resource[index].Name) && GameState.ResourceLoaded[index] == 0)
            {
                GameState.ResourceLoaded[index] = 1;
                Sender.SendRequestResource(index);
            }
        }

        #endregion

        #region Drawing

        public void OnDraw(int resource, int dx, int dy, System.Drawing.Rectangle rec)
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

            // src rect
            rec.Y = 0;
            rec.Height = GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Resources, resourceSprite.ToString())).Height;
            rec.X = 0;
            rec.Width = GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Resources, resourceSprite.ToString())).Width;

            // Set base x + y, then the offset due to size
            x = (int)Math.Round(Data.MyMapResource[resourceNum].X * Constants.TileSize - GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Resources, resourceSprite.ToString())).Width / 2d + 16d);
            y = Data.MyMapResource[resourceNum].Y * Constants.TileSize - GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Resources, resourceSprite.ToString())).Height + 32;

            OnDraw(resourceSprite, x, y, rec);
        }

        #endregion

    }
}