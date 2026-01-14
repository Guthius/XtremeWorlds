using Client.Net;
using Core;
using Core.Globals;
using Core.Interfaces;
using Core.Net;
using Type = Core.Globals.Type;
using Core.Objects;

namespace Client
{
    public class Item : ItemBase, IStreamable
    {
        #region Database
        public static void OnStream(int index)
        {
            if (index < 0 || index >= Core.Globals.Variables.MaxItems) return;
            if (!IsStreaming[index])
            {
                IsStreaming[index] = true;
                Sender.RequestItem(index);
            }
        }
        #endregion
    }
}