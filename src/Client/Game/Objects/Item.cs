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
            if (string.IsNullOrEmpty(Item.Instance[index].Name) && Item.Instance[index].IsLoaded == false)
            {
                Sender.SendRequestItem(index);
                Item.Instance[index].IsLoaded = true;
            }
        }
        #endregion
    }
}