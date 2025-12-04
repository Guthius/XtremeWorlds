using Core.Globals;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Core.Configurations;

namespace Client
{
    public class MapItem : IContent
    {
        public Data Data { get; set; } = Data.MapItem;

        public void OnDraw(int itemNum)
        {
            Rectangle srcRec;
            Rectangle destRec;
            int picNum;
            int x;
            int y;

            if (Data[itemNum].Num < 0 | Data[itemNum].Num > Variables.MaxItems)
                return;

            Item.OnStream(Data[itemNum].Num);

            picNum = Data.Item[Data[itemNum].Num].Icon;

            if (picNum < 1 | picNum > GameState.NumItems)
                return;

            ref var instance = ref Data[itemNum];

            if (Math.Floor((double) instance.X / Constants.TileSize) < GameState.TileView.Left | Math.Floor((double) instance.X / Constants.TileSize) > GameState.TileView.Right)
                return;

            if (Math.Floor((double) instance.Y / Constants.TileSize) < GameState.TileView.Top | Math.Floor((double) instance.Y / Constants.TileSize) > GameState.TileView.Bottom)
                return;

            srcRec = new Rectangle(0, 0, Constants.TileSize, Constants.TileSize);
            destRec = new Rectangle(GameLogic.ConvertMapX(Data[itemNum].X),
                GameLogic.ConvertMapY(Data[itemNum].Y), Constants.TileSize, Constants.TileSize);

            x = GameLogic.ConvertMapX(Data[itemNum].X);
            y = GameLogic.ConvertMapY(Data[itemNum].Y);

            string argPath = System.IO.Path.Combine(Core.Globals.DataPath.Items, picNum.ToString());
            GameClient.RenderTexture(ref argPath, x, y, srcRec.X, srcRec.Y, srcRec.Width, srcRec.Height, srcRec.Width,
                srcRec.Height);
        }

        public void OnClear(int index)
        {
            ref var instance = ref Data[index];
            instance.Num = -1;
            instance.Value = 0;
            instance.X = 0;
            instance.Y = 0;
        }
    }
}
