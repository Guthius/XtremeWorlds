using Core.Globals;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Core.Interfaces;

namespace Client
{
    public class Blood : IData
    {
        public static void OnDraw(int index)
        {
            Rectangle srcRec;
            Rectangle destRec;
            int x;
            int y;
            
            ref var instance = ref Data.Blood[index];
            if (instance.X < GameState.TileView.Left | instance.X > GameState.TileView.Right)
                return;
                
            if (instance.Y < GameState.TileView.Top | instance.Y > GameState.TileView.Bottom)
                return;

            // check if we should be seeing it
            if (instance.Timer + 30000 < General.GetTickCount())
                return;

            x = GameLogic.ConvertMapX(Data.Blood[index].X);
            y = GameLogic.ConvertMapY(Data.Blood[index].Y);

            srcRec = new Rectangle((instance.Sprite - 1) * Constants.TileSize, 0, Constants.TileSize, Constants.TileSize);
            destRec = new Rectangle(GameLogic.ConvertMapX(instance.X),
                GameLogic.ConvertMapY(instance.Y), Constants.TileSize, Constants.TileSize);

            string argPath = System.IO.Path.Combine(Core.Globals.DataPath.Misc, "Blood");
            GameClient.RenderTexture(ref argPath, x, y, srcRec.X, srcRec.Y, srcRec.Width, srcRec.Height);
        
        }

        public static void OnClear()
        {
            for (int i = 0; i < Data.Blood.Length; i++)
                OnClear(i);
        }

        public static void OnClear(int index)
        {
            ref var instance = ref Data.Blood[index];
            instance.X = 0;
            instance.Y = 0;
            instance.Sprite = 0;
            instance.Timer = 0;
        }

        public static void OnLoad(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnStream(int index)
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
