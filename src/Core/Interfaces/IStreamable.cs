using Core.Globals;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Core.Interfaces
{
    public interface IStreamable
    {
        public static abstract void OnStream(int index);
    }
}
