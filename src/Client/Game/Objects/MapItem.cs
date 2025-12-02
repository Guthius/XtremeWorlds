using Core.Globals;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    public class MapItem
    {
        public static void OnClear(int index)
        {
            ref var instance = ref Data.MyMapItem[index];
            instance.Num = -1;
            instance.Value = 0;
            instance.X = 0;
            instance.Y = 0;
        }
    }
}
