using Core.Globals;
using Core.Interfaces;

namespace Core.Objects
{
    public class JobBase : IData
    { 
        public static bool[] IsChanged { get; set; } = new bool[Variables.MaxJobs];

        public JobBase()
        {
            Stat = new int[Enum.GetNames(typeof(Stat)).Length];
            StartItem = new int[Variables.MaxStartItems];
            StartValue = new int[Variables.MaxStartItems];
            StartSkill = new int[Variables.MaxStartSkills];

            Name = "";
            Desc = "";
            FemaleSprite = 0;
            MaleSprite = 0;
            StartMap = 1;
            MoveSpeed = 1.0f;

            for (int i = 0; i < Core.Globals.Variables.MaxStartItems; i++)
            {
                StartItem[i] = -1;
                StartValue[i] = 0;
            }

            for (int i = 0; i < Core.Globals.Variables.MaxStartItems; i++)
            {
                StartSkill[i] = -1;
            }
        }

        public string Name;
        public string Desc;
        public int[] Stat;
        public int MaleSprite;
        public int FemaleSprite;
        public int[] StartItem;
        public int[] StartValue;
        public int[] StartSkill;
        public int StartMap;
        public byte StartX;
        public byte StartY;
        public int BaseExp;
        public float MoveSpeed;

        public static List<JobBase> Instance { get; private set; } = new List<JobBase>();

        public static void OnClear(int index)
        {
            if (Instance.Count > index)
                Instance[index] = new JobBase();
        }

        public static void ClearChanged()
        {
            for (int i = 0; i < Variables.MaxJobs; i++)
                IsChanged[i] = false;
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