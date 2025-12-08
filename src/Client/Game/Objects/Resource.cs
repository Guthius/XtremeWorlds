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
            if (index < 0 || index >= Variables.MaxResources) return;
            if (Resource.Instance.Count <= index)
            {
                Sender.SendRequestResource(index);
            }
        }

        #endregion
    }
}