using Client.Net;
using Core;
using Core.Globals;
using Core.Interfaces;
using Core.Net;
using Core.Objects;
using static Core.Globals.Command;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Type = Core.Globals.Type;

namespace Client
{
    public class Animation : AnimationBase, IData
    {
        #region Database

        public static void OnStream(int animationNum)
        {
            if (animationNum >= 0 && string.IsNullOrEmpty(Animation.Instance[animationNum].Name) && Animation.Instance[animationNum].IsLoaded)
            {
                Sender.SendRequestAnimation(animationNum);
            }
        }
     
        #endregion

    }
}