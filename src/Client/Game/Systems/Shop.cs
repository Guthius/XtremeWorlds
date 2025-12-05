using Client.Game.UI;
using Client.Net;
using Core;
using Core.Globals;
using Core.Interfaces;
using Core.Net;
using Type = Core.Globals.Type;

namespace Client
{

    public class Shop : IData
    {
        public static void OnClose()
        {
            Sender.SendCloseShop();
            WindowManager.HideWindow(WindowManager.GetWindowIndex("winShop"));
            WindowManager.HideWindow(WindowManager.GetWindowIndex("winDescription"));
            GameState.ShopSelectedSlot = 0;
            GameState.ShopSelectedItem = 0;
            GameState.ShopIsSelling = false;
            GameState.InShop = -1;
        }

        #region Database

        public static void OnClear(int index)
        {
            Data.Shop[index] = default;
            Data.Shop[index].Name = "";
            Data.Shop[index].TradeItem = new Type.TradeItem[Variables.MaxTrades];
            for (int x = 0; x < Variables.MaxTrades; x++)
            {            
                Data.Shop[index].TradeItem[x].Item = -1;
                Data.Shop[index].TradeItem[x].CostItem = - 1;
            }
            GameState.ShopLoaded[index] = 0;
        }

        public static void OnReset()
        {
            int i;

            Data.Shop = new Type.Shop[Variables.MaxShops];

            for (i = 0; i < Variables.MaxShops; i++)
                OnClear(i);

        }

        public static void OnStream(int shopNum)
        {
            if (shopNum >= 0 && string.IsNullOrEmpty(Data.Shop[shopNum].Name) && GameState.ShopLoaded[shopNum] == 0)
            {
                GameState.ShopLoaded[shopNum] = 1;
                Sender.SendRequestShop(shopNum);
            }
        }

        public static void OnDraw(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnLoad(int index)
        {
            throw new NotImplementedException();
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