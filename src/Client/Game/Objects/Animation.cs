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
    public class Animation : AnimationBase, IStreamable
    {
        #region Database

        public static void OnStream(int index)
        {
            if (index < 0 || index >= Variables.MaxAnimations) return;
            if (string.IsNullOrEmpty(Animation.Instance[index].Name) && Animation.Instance[index].IsLoaded)
            {
                Sender.SendRequestAnimation(index);
            }
        }
     
        #endregion

    }
}