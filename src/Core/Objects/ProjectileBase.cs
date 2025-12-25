using Core.Globals;
using Core.Interfaces;

namespace Core.Objects
{
    public class ProjectileBase : IData
    {
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
        }

        public static void OnDraw(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnClear(int index)
        {
            if (Instance.Count > index)
                Instance[index] = new ProjectileBase();
        }

        public static void OnReset()
        {
            for (int i = 0; i < Instance.Count; i++)
                OnClear(i);
        }

        public static void OnLoad(int index)
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

        public static implicit operator ProjectileBase(int v)
        {
            throw new NotImplementedException();
        }
    }
}