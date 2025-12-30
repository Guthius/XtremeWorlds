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
        public int ExperienceReward;
        public int ItemReward;
        public int LvlRequired;
        public int ToolRequired;
        public int Health;
        public int RespawnTime;
        public bool Walkthrough;
        public int Animation;

        // Optional common event trigger (0 = none; otherwise matches editor selection)
        public byte CommonEventType;
        public int CommonEventData1;
        public int CommonEventData2;

        public static List<ResourceBase> Instance { get; private set; } = new List<ResourceBase>();

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
            for (int i = 0; i < Instance.Count; i++)
                OnClear(i);
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