using Core.Globals;
using Core.Interfaces;

namespace Core.Objects
{
    public class MoralBase : IData
    {
        public static bool[] IsChanged = new bool[Variables.MaxMorals];

        public string Name;
        public byte Color;
        public bool CanCast;
        public bool CanPk;
        public bool CanUseItem;
        public bool DropItems;
        public bool LoseExp;
        public bool CanPickupItem;
        public bool CanDropItem;
        public bool PlayerBlock;
        public bool NpcBlock;
        public static List<MoralBase> Instance { get; private set; } = new List<MoralBase>();
        public int Index { get; set; } = -1;

        public MoralBase()
        {
            Name = "";
        }

        public static void ClearChanged()
        {
            IsChanged = new bool[Variables.MaxMorals];
        }

        public static void OnClear(int index)
        {
            if (Instance.Count > index)
                Instance[index] = new MoralBase();
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