using Core.Globals;
using Core.Interfaces;

namespace Core.Objects
{
    public class ResourceBase : IData
    {
        public static bool[] IsChanged = new bool[Variables.MaxResources];

        public string Name;
        public string SuccessMessage;
        public string EmptyMessage;
        public int ResourceType;
        public int ResourceImage;
        public int ExhaustedImage;
        public int ExpReward;
        public int ItemReward;
        public int LvlRequired;
        public int ToolRequired;
        public int Health;
        public int RespawnTime;
        public bool Walkthrough;
        public int Animation;

        public static List<ResourceBase> Instance { get; private set; } = new List<ResourceBase>();
        public int Index { get; set; } = -1;

        public ResourceBase()
        {
            SuccessMessage = "";
            EmptyMessage = "";
        }

        public static void ClearChanged()
        {
            IsChanged = new bool[Variables.MaxResources];
        }

        public static void OnClear(int index)
        {
            if (Instance.Count > index)
                Instance[index] = new ResourceBase();
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