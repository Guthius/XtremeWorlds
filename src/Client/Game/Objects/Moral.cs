using Client.Net;
using Core;
using Core.Globals;
using Core.Interfaces;
using Core.Objects;
using Type = Core.Globals.Type;

namespace Client
{

    public class Moral : MoralBase, IStreamable
    {
        #region Database

        public static void OnStream(int index)
        {
            if (index < 0 || index >= Variables.MaxMorals) return;
            if (!IsStreaming[index])
            {
                IsStreaming[index] = true;
                Sender.SendRequestMoral(index);
            }
        }

        #endregion
    }
}