using Client.Net;
using Core;
using Core.Globals;
using Core.Net;
using Type = Core.Globals.Type;

namespace Client
{

    public class Item : IContent
    {
        public Data Data { get; set; } = Data.Item;
        
        public void OnClear(int index)
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

        public static void OnReset()
        {
            int i;

            Data.Item = new Type.Item[Variables.MaxItems];

            for (i = 0; i < Variables.MaxItems; i++)
                OnClear(i);

        }

        public void OnClearChanged()
        {
            GameState.ItemChanged = new bool[Variables.MaxItems];
        }

        public void OnStream(int index)
        {
            if (index >= 0 && string.IsNullOrEmpty(Data.Item[index].Name) && GameState.ItemLoaded[index] == 0)
            {
                GameState.ItemLoaded[index] = 1;
                Sender.SendRequestItem(index);
            }
        }
    }
}