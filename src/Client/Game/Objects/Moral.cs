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
            if (index >= 0 & string.IsNullOrEmpty(Moral.Instance[index].Name))
            {
                Sender.SendRequestMoral(index);
            }
        }

        #endregion
    }
}