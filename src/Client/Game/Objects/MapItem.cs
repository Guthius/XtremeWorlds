using Core.Globals;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Core.Configurations;

namespace Client
{
    public class MapItem
    {
        public static void OnDraw(int itemNum)
        {
            Rectangle srcRec;
            Rectangle destRec;
            int picNum;
            int x;
            int y;

            if (Data.MyMapItem[itemNum].Num < 0 | Data.MyMapItem[itemNum].Num > Variables.MaxItems)
                return;

            Item.OnStream(Data.MyMapItem[itemNum].Num);

            picNum = Data.Item[Data.MyMapItem[itemNum].Num].Icon;

            if (picNum < 1 | picNum > GameState.NumItems)
                return;

            ref var instance = ref Data.MyMapItem[itemNum];

            if (Math.Floor((double) instance.X / Constants.TileSize) < GameState.TileView.Left | Math.Floor((double) instance.X / Constants.TileSize) > GameState.TileView.Right)
                return;

            if (Math.Floor((double) instance.Y / Constants.TileSize) < GameState.TileView.Top | Math.Floor((double) instance.Y / Constants.TileSize) > GameState.TileView.Bottom)
                return;

            srcRec = new Rectangle(0, 0, Constants.TileSize, Constants.TileSize);
            destRec = new Rectangle(GameLogic.ConvertMapX(Data.MyMapItem[itemNum].X),
                GameLogic.ConvertMapY(Data.MyMapItem[itemNum].Y), Constants.TileSize, Constants.TileSize);

            x = GameLogic.ConvertMapX(Data.MyMapItem[itemNum].X);
            y = GameLogic.ConvertMapY(Data.MyMapItem[itemNum].Y);

            string argPath = Path.Combine(DataPath.Items, picNum.ToString());
            GameClient.RenderTexture(ref argPath, x, y, srcRec.X, srcRec.Y, srcRec.Width, srcRec.Height, srcRec.Width,
                srcRec.Height);
        }

        public static void OnClear(int index)
        {
            ref var instance = ref Data.MyMapItem[index];
            instance.Num = -1;
            instance.Value = 0;
            instance.X = 0;
            instance.Y = 0;
        }
    }
}
