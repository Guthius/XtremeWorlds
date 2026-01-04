using Core.Globals;

namespace Core.Objects
{
    public class ProjectileBase
    {
        public static bool[] IsStreaming = new bool[Variables.MaxProjectiles];
        public static bool[] IsChanged = new bool[Variables.MaxProjectiles];

        public string Name;
        public int Sprite;
        public byte Range;
        public int Speed;
        public int Damage;
        public int Animation;

        public static List<ProjectileBase> Instance { get; private set; } = new List<ProjectileBase>();

        public ProjectileBase()
        {
            Name = "";
        }

        public static void OnClearChanged()
        {
            IsChanged = new bool[Variables.MaxProjectiles];
            IsStreaming = new bool[Variables.MaxProjectiles];
        }

        public static void OnClear(int index)
        {
            if (Instance.Count > index)
                Instance[index] = new ProjectileBase();
            IsChanged[index] = false;
            IsStreaming[index] = false;
        }

        public static void OnClear()
        {
            for (int i = 0; i < Instance.Count; i++)
                OnClear(i);
        }

    }
}