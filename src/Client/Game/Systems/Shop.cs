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
            Sender.CloseShop();
            WindowManager.HideWindow(WindowManager.GetWindow("winShop"));
            WindowManager.HideWindow(WindowManager.GetWindow("winDescription"));
            GameState.ShopSelectedSlot = 0;
            GameState.ShopSelectedItem = 0;
            GameState.ShopIsSelling = false;
            GameState.InShop = -1;
        }

        #region Database

        public static void OnStream(int index)
        {
            if (index < 0 || index >= Core.Globals.Variables.MaxShops) return;
            if (!IsStreaming[index])
            {
                IsStreaming[index] = true;
                Sender.RequestShop(index);
            }
            
        }

        #endregion

    }
}