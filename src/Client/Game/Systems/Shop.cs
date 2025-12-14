using Client.Game.UI;
using Client.Net;
using Core;
using Core.Globals;
using Core.Interfaces;
using Core.Net;
using Core.Objects;
using Type = Core.Globals.Type;

namespace Client
{

    public class Shop : ShopBase, IStreamable
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

        public static void OnStream(int Index)
        {
            if (Index < 0 || Index >= Core.Globals.Variables.MaxShops) return;
            if (Shop.Instance.Count <= Index)
            {
                Sender.SendRequestShop(Index);
            }
            
        }

        #endregion

    }
}