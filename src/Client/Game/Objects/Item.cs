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
            if (index < 0 || index >= Variables.MaxItems) return;
            if (Item.Instance.Count <= index)
            {
                Sender.SendRequestItem(index);
            }
        }
        #endregion
    }
}