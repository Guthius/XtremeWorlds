using Client.Net;
using Core;
using Core.Globals;
using Core.Net;
using Type = Core.Globals.Type;

namespace Client
{

    public class Item
    {

        #region Database
        public static void OnClear(int index)
        {   
            Data.Item[index] = default;

            var statCount = Enum.GetNames(typeof(Stat)).Length;
            Data.Item[index].AddStat = new byte[statCount];
            Data.Item[index].StatReq = new byte[statCount];

            Data.Item[index].Name = "";
            Data.Item[index].Description = "";
            Data.Item[index].Ammo = -1;
            GameState.ItemLoaded[index] = 0;
        }

        public static void OnClearAll()
        {
            int i;

            Data.Item = new Type.Item[Variables.MaxItems];

            for (i = 0; i < Variables.MaxItems; i++)
                OnClear(i);

        }

        public static void OnClearChanged()
        {
            GameState.ItemChanged = new bool[Variables.MaxItems];
        }

        public static void OnStream(int itemNum)
        {
            if (itemNum >= 0 && string.IsNullOrEmpty(Data.Item[itemNum].Name) && GameState.ItemLoaded[itemNum] == 0)
            {
                GameState.ItemLoaded[itemNum] = 1;
                Sender.SendRequestItem(itemNum);
            }
        }

        #endregion
    }
}