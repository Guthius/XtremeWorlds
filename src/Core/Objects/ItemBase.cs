using Core.Globals;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Core.Objects
{
    public class ItemBase
    {
        public ItemBase()
        {
            AddStat = new byte[Enum.GetNames(typeof(Stat)).Length];
            StatReq = new byte[Enum.GetNames(typeof(Stat)).Length];
        }

        public string Name;
        public int Icon;
        public string Description;
        public byte Type;
        public byte SubType;
        public int Data1;
        public int Data2;
        public int Data3;
        public int JobReq;
        public int AccessReq;
        public int LevelReq;
        public byte Mastery;
        public int Price;
        public byte[] AddStat;
        public byte Rarity;
        public int Speed;
        public byte BindType;
        public byte[] StatReq;
        public int Animation;
        public int Paperdoll;
        public byte Stackable;
        public byte ItemLevel;
        public byte KnockBack;
        public byte KnockBackTiles;
        public int Projectile;
        public int Ammo;

        public static List<ItemBase> Instance { get; private set; } = new List<ItemBase>();
        public int Index { get; set; } = -1;

        // Ensure the Instance list is exactly 'count' long and initialize new slots
        public static void EnsureSize(int count)
        {
            if (count < 0)
            {
                count = 0;
            }

            if (Instance.Count == count)
            {
                return;
            }

            var old = Instance;
            var fresh = new ItemBase[count];

            int copy = Math.Min(old.Count, count);
            for (int i = 0; i < copy; i++)
            {
                fresh[i] = old[i];
            }

            for (int i = copy; i < count; i++)
            {
                fresh[i] = new ItemBase();
            }

            Instance = new List<ItemBase>(fresh);

            // Initialize newly added or defaulted entries
            for (int i = 0; i < Instance.Count; i++)
            {
                if (Instance[i] == null)
                {
                    Instance[i] = new ItemBase();
                }
            }
        }

        // Safe accessor that ensures the slot exists and is initialized
        public static ItemBase GetOrCreate(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            EnsureSize(Math.Max(Instance.Count, index + 1));

            return Instance[index];
        }
    }
}
