using Client.Game.UI;
using Client.Net;
using Core;
using Core.Globals;
using Core.Interfaces;
using Core.Net;
using static Core.Globals.Command;
using Type = Core.Globals.Type;

namespace Client
{

    public class Bank : IData
    {
        #region Database

        public static void OnReset()
        {
            int i;
            int x;

            for (x = 0; x < Variables.MaxPlayers; x++)
            {
               OnClear(x);
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

        public static void OnClear(int index)
        {
            int i;
            
            Data.Bank[index].Item = new Type.PlayerInv[(Variables.MaxBank)];

            for (i = 0; i < Variables.MaxBank; i++)
            {
                Data.Bank[index].Item[i].Num = -1;
                Data.Bank[index].Item[i].Value = 0;
            }
        }

        public static void OnStream(int index)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}