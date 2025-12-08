using Client.Net;
using Core.Globals;
using Core.Interfaces;
using Core.Objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    public class Job : JobBase, IData, IStreamable
    {
        #region Database
        public static void OnStream(int index)
        {
            if (index < 0 || index >= Variables.MaxJobs) return;
            if (JobBase.Instance.Count <= index)
            {
                //Sender.SendRequestJob(index);
            }
        }
        #endregion
    }
}
