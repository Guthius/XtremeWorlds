using Core.Globals;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Core.Configurations;
using Core.Interfaces;
using MapItemData = Core.Globals.Type.MapItem;

namespace Client
{
    public class MapItem : IData
    {
        public static MapItemData[] Instance { get; } = new MapItemData[Core.Globals.Variables.MaxMapItems];

        public static void OnDraw(int item)
        {
            Rectangle srcRec;
            Rectangle destRec;
            int icon;
            int x;
            int y;

            if (MapItem.Instance[item].Num < 0 | MapItem.Instance[item].Num > Core.Globals.Variables.MaxItems)
                return;

            Item.OnStream(MapItem.Instance[item].Num);

            // Item data may not be loaded yet; wait for streaming to populate before drawing.
            if (Item.Instance.Count <= MapItem.Instance[item].Num)
                return;

            icon = Item.Instance[MapItem.Instance[item].Num].Icon;

            if (icon < 1 | icon > GameState.NumItems)
                return;

            ref var instance = ref MapItem.Instance[item];

            if (Math.Floor((double) instance.X / Constants.TileSize) < GameState.TileView.Left | Math.Floor((double) instance.X / Constants.TileSize) > GameState.TileView.Right)
                return;

            if (Math.Floor((double) instance.Y / Constants.TileSize) < GameState.TileView.Top | Math.Floor((double) instance.Y / Constants.TileSize) > GameState.TileView.Bottom)
                return;

            srcRec = new Rectangle(0, 0, Constants.TileSize, Constants.TileSize);
            destRec = new Rectangle(GameLogic.ConvertMapX(MapItem.Instance[item].X),
                GameLogic.ConvertMapY(MapItem.Instance[item].Y), Constants.TileSize, Constants.TileSize);

            x = GameLogic.ConvertMapX(MapItem.Instance[item].X);
            y = GameLogic.ConvertMapY(MapItem.Instance[item].Y);

            string argPath = System.IO.Path.Combine(Core.Globals.DataPath.Items, icon.ToString());
            GameClient.RenderTexture(ref argPath, x, y, srcRec.X, srcRec.Y, srcRec.Width, srcRec.Height, srcRec.Width,
                srcRec.Height);
        }

        public static void OnClear(int index)
        {
            ref var instance = ref MapItem.Instance[index];
            instance.Num = -1;
            instance.Value = 0;
            instance.Durability = 0;
            instance.X = 0;
            instance.Y = 0;
        }

        public static void OnStream(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnClear()
        {
            for (int i = 0; i < MapItem.Instance.Length; i++)
                OnClear(i);
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
    }
}
