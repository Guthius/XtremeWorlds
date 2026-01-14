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

        public static void OnStream(int index)
        {
            if (index < 0 || index >= Core.Globals.Variables.MaxResources) return;
            if (!IsStreaming[index])
            {
                IsStreaming[index] = true;
                Sender.RequestResource(index);
            }
        }

        #endregion
    }
}