using Core.Globals;
using Core.Interfaces;

namespace Core.Objects
{
    public class ResourceBase : IData
    {
        public static bool[] IsChanged = new bool[Variables.MaxResources];

        public ResourceBase()
        {
        }

        public static void OnClear(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnDraw(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnLoad(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnReset()
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
    }
}