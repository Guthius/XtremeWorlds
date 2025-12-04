using Client.Net;
using Core;
using Core.Globals;
using Type = Core.Globals.Type;

namespace Client
{

    public class Moral
    {
        #region Database

        public static void OnClear(int index)
        {
            Data.Moral[index] = default;

            Data.Moral[index].Name = "";
            GameState.MoralLoaded[index] = 0;
        }

        public static void OnClearAll()
        {
            int i;

            Data.Moral = new Type.Moral[(Variables.MaxMorals)];

            for (i = 0; i < Variables.MaxMorals; i++)
                OnClear(i);
        }

        public static void OnStream(int moralNum)
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