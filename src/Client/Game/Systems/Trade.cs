using Client.Game.UI;
using Client.Net;
using Core;
using Core.Configurations;
using Core.Globals;
using Core.Net;

namespace Client
{

    public class Trade
    {
        public static void OnClose()
        {
            InTrade = 0;
            WindowManager.HideWindow(WindowManager.GetWindowIndex("winTrade"));
        }

        #region Globals & Type

        public static int InTrade;
        public static int TradeX;
        public static int TradeY;
        public static string TheirWorth = string.Empty;
        public static string YourWorth = string.Empty;

        #endregion

    }
}