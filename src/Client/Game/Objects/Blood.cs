using Core.Globals;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    public class Blood
    {
        public static void OnClear()
        {
            for (int i = 0; i < byte.MaxValue; i++)
                Data.Blood[i].Timer = 0;
        }

    }
}
