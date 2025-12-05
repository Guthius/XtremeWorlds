using Client.Net;
using Core;
using Core.Globals;
using Core.Interfaces;
using Core.Net;
using Type = Core.Globals.Type;
using Core.Objects;

namespace Client
{
    public class Item : ItemBase, IData
    {
        #region Database
        public static void OnClear(int index)
        {
            if (index < 0 || index >= ItemBase.Instance.Count) return;

            ItemBase.Instance[index].Name = "";
            ItemBase.Instance[index].Description = "";
            ItemBase.Instance[index].Ammo = -1;
            ItemBase.Instance[index].Stackable = 1;

            GameState.ItemLoaded[index] = 0;
        }

        public static void OnReset()
        {
            // Size instance storage first, then clear all
            ItemBase.EnsureSize(Variables.MaxItems);

            for (int i = 0; i < Variables.MaxItems; i++)
                OnClear(i);
        }

        public static void OnClearChanged()
        {
            GameState.ItemChanged = new bool[Variables.MaxItems];
        }

        public static void OnDraw(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnLoad(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnStream(int itemNum)
        {
            if (itemNum < 0 || itemNum >= Variables.MaxItems) return;
            var it = ItemBase.Instance[itemNum];
            if (string.IsNullOrEmpty(it.Name) && GameState.ItemLoaded[itemNum] == 0)
            {
                GameState.ItemLoaded[itemNum] = 1;
                Sender.SendRequestItem(itemNum);
            }
        }

        public static void OnSave(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnUpdate(int index)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}