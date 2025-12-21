using Client;
using Core.Globals;
using Core.Interfaces;
using static Core.Globals.Commands;
using MapResourceCacheData = Core.Globals.Type.MapResourceCache;

namespace Core.Objects
{
    public class MapResource : IData
    {
        public static MapResourceCacheData[] Instance { get; private set; } = new MapResourceCacheData[Variables.MaxResources];

        public static void OnClear(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnLoad(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnReset()
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

        public static void OnDraw(int index)
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

            if (MapResource.Instance[index].X > Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX | MapResource.Instance[index].Y > Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY)
                return;

            mapResourceNum = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[MapResource.Instance[index].X, MapResource.Instance[index].Y].Data1;

            if (mapResourceNum == 0)
                mapResourceNum = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[MapResource.Instance[index].X, MapResource.Instance[index].Y].Data1_2;

            Resource.OnStream(mapResourceNum);

            if (Resource.Instance[mapResourceNum].ResourceImage == 0)
                return;

            // Get the Resource state
            resourceState = MapResource.Instance[index].State;

            if (resourceState == 0) // normal
            {
                resourceSprite = Resource.Instance[mapResourceNum].ResourceImage;
            }
            else if (resourceState == 1) // used
            {
                resourceSprite = Resource.Instance[mapResourceNum].ExhaustedImage;
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
            x = (int)Math.Round(MapResource.Instance[index].X * Constants.TileSize - gfxInfo.Width / 2d + 16d);
            y = MapResource.Instance[index].Y * Constants.TileSize - gfxInfo.Height + 32;

            if (resourceSprite < 1 | resourceSprite > GameState.NumResources)
                return;
        
            x = GameLogic.ConvertMapX(x);
            y = GameLogic.ConvertMapY(y);
            int width = rec.Right - rec.Left;
            int height = rec.Bottom - rec.Top;

            if (rec.Width < 0 | rec.Height < 0)
                return;

            string argPath = System.IO.Path.Combine(DataPath.Resources, resourceSprite.ToString());
            GameClient.RenderTexture(ref argPath, x, y, rec.X, rec.Y, rec.Width, rec.Height, rec.Width, rec.Height);
        }
    }
}