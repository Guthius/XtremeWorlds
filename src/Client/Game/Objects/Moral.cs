using Client.Net;
using Core;
using Core.Globals;
using Type = Core.Globals.Type;

namespace Client
{

    public class Moral : IContent
    {
        public Data Data { get; set; } = Data.Moral;

        #region Database

        public void OnClear(int index)
        {
            Data.Moral[index] = default;

            Data.Moral[index].Name = "";
            GameState.MoralLoaded[index] = 0;
        }

        public void OnReset()
        {
            int i;

            Data.Moral = new Type.Moral[(Variables.MaxMorals)];

            for (i = 0; i < Variables.MaxMorals; i++)
                OnClear(i);
        }

        public void OnStream(int moralNum)
        {
            if (moralNum >= 0 & string.IsNullOrEmpty(Data.Moral[moralNum].Name) && GameState.MoralLoaded[moralNum] == 0)
            {
                GameState.MoralLoaded[moralNum] = 1;
                Sender.SendRequestMoral(moralNum);
            }
        }

        #endregion
    }
}