using Client.Net;
using Core;
using Core.Globals;
using Core.Net;
using static Core.Globals.Command;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Type = Core.Globals.Type;

namespace Client
{
    public class Animation : IContent
    {

        #region Database

        public static void OnClear(int index)
        {
            Data.Animation[index] = default;
            Data.Animation[index] = new Type.Animation();

            for (int x = 0; x <= 1; x++)
                Data.Animation[index].Sprite = new int[x + 1];

            for (int x = 0; x <= 1; x++)
                Data.Animation[index].Frames = new int[x + 1];

            for (int x = 0; x <= 1; x++)
                Data.Animation[index].Frames[x] = 5;

            for (int x = 0; x <= 1; x++)
                Data.Animation[index].LoopCount = new int[x + 1];

            for (int x = 0; x <= 1; x++)
                Data.Animation[index].LoopTime = new int[x + 1];

            Data.Animation[index].Name = "";
            Data.Animation[index].LoopCount[0] = 1;
            Data.Animation[index].LoopCount[1] = 1;
            Data.Animation[index].LoopTime[0] = 1;
            Data.Animation[index].LoopTime[1] = 1;
            GameState.AnimationLoaded[index] = 0;
        }

        public static void OnReset()
        {
            int i;

            Data.Animation = new Type.Animation[Variables.MaxAnimations];

            for (i = 0; i < Variables.MaxAnimations; i++)
                OnClear(i);
        }

        public void OnStream(int animationNum)
        {
            if (animationNum >= 0 && string.IsNullOrEmpty(Data.Animation[animationNum].Name) && GameState.AnimationLoaded[animationNum] == 0)
            {
                GameState.AnimationLoaded[animationNum] = 1;
                Sender.SendRequestAnimation(animationNum);
            }
        }

        #endregion

    }
}