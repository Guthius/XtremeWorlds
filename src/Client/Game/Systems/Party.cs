using Client.Game.UI;
using Client.Game.UI.Windows;
using Client.Net;
using Core;
using Core.Globals;
using Core.Net;
using Type = Core.Globals.Type;

namespace Client
{

    public class Party
    {

        #region Database

        public static void OnClear()
        {
            Data.MyParty = new Type.Party()
            {
                Leader = 0,
                MemberCount = 0
            };
            Data.MyParty.Member = new int[Core.Globals.Variables.MaxPartyMembers];
        }

        #endregion

    }
}