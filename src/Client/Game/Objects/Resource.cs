using System;
using System.Drawing;
using Client.Net;
using Core;
using Core.Globals;
using Core.Interfaces;
using Core.Net;
using Core.Objects;

namespace Client
{

    public class Resource : ResourceBase, IStreamable
    {
        #region Database

        public static void OnStream(int resourceNum)
        {
            if (resourceNum >= 0 && string.IsNullOrEmpty(Data.Resource[resourceNum].Name))
            {
                Sender.SendRequestResource(resourceNum);
            }
        }

        #endregion
    }
}