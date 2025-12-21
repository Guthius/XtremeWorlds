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
        public static MapItemData[] Instance { get; } = new MapItemData[Variables.MaxMapItems];
        
        public static void OnDraw(int itemNum)
        {
            Rectangle srcRec;
            Rectangle destRec;
            int picNum;
            int x;
            int y;

            if (MapItem.Instance[itemNum].Num < 0 | MapItem.Instance[itemNum].Num > Core.Globals.Variables.MaxItems)
                return;

            Item.OnStream(MapItem.Instance[itemNum].Num);

            picNum = Item.Instance[MapItem.Instance[itemNum].Num].Icon;

            if (picNum < 1 | picNum > GameState.NumItems)
                return;

            ref var instance = ref MapItem.Instance[itemNum];

            if (Math.Floor((double) instance.X / Constants.TileSize) < GameState.TileView.Left | Math.Floor((double) instance.X / Constants.TileSize) > GameState.TileView.Right)
                return;

            if (Math.Floor((double) instance.Y / Constants.TileSize) < GameState.TileView.Top | Math.Floor((double) instance.Y / Constants.TileSize) > GameState.TileView.Bottom)
                return;

            srcRec = new Rectangle(0, 0, Constants.TileSize, Constants.TileSize);
            destRec = new Rectangle(GameLogic.ConvertMapX(MapItem.Instance[itemNum].X),
                GameLogic.ConvertMapY(MapItem.Instance[itemNum].Y), Constants.TileSize, Constants.TileSize);

            x = GameLogic.ConvertMapX(MapItem.Instance[itemNum].X);
            y = GameLogic.ConvertMapY(MapItem.Instance[itemNum].Y);

            string argPath = System.IO.Path.Combine(Core.Globals.DataPath.Items, picNum.ToString());
            GameClient.RenderTexture(ref argPath, x, y, srcRec.X, srcRec.Y, srcRec.Width, srcRec.Height, srcRec.Width,
                srcRec.Height);
        }

        public static void OnClear(int index)
        {
            ref var instance = ref MapItem.Instance[index];
            instance.Num = -1;
            instance.Value = 0;
            instance.X = 0;
            instance.Y = 0;
        }

        public static void OnStream(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnReset()
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
