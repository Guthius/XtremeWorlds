using Client.Game.UI;
using Client.Net;
using Core;
using Core.Globals;
using Core.Net;
using static Core.Globals.Command;
using Type = Core.Globals.Type;

namespace Client
{

    public class Bank : IContent
    {
        public Data Data { get; set; } = Data.Bank;
        
        #region Database

        public void OnReset()
        {
            int i;
            int x;

            for (x = 0; x < Variables.MaxPlayers; x++)
            {
                Data.Bank[x].Item = new Type.PlayerInv[(Variables.MaxBank)];

                for (i = 0; i < Variables.MaxBank; i++)
                {
                    Data.Bank[x].Item[i].Num = -1;
                    Data.Bank[x].Item[i].Value = 0;
                }
            }
        }

        #endregion

    }
}