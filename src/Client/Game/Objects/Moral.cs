using Client.Net;
using Core;
using Core.Globals;
using Core.Interfaces;
using Type = Core.Globals.Type;

namespace Client
{

    public class Moral : IData
    {
        #region Database

        public static void OnClear(int index)
        {
            Data.Moral[index] = default;

            Data.Moral[index].Name = "";
            GameState.MoralLoaded[index] = 0;
        }

        public static void OnReset()
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