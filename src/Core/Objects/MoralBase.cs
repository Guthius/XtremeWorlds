using Core.Globals;

namespace Core.Objects
{
    public class MoralBase
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

        public MoralBase()
        {
            Name = "";
        }

        public static void OnClearChanged()
        {
            IsChanged = new bool[Variables.MaxMorals];
        }

        public static void OnClear(int index)
        {
            if (Instance.Count > index)
                Instance[index] = new MoralBase();
        }

        public static void OnClear()
        {
            for (int i = 0; i < Instance.Count; i++)
                OnClear(i);
        }
    }
}