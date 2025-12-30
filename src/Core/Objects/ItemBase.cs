using Core.Globals;
using Core.Interfaces;
using System;
using System.Collections.Generic;

namespace Core.Objects
{
    public class ItemBase : IData
    {
        public static bool[] IsChanged { get; set; } = new bool[Variables.MaxItems];

        public ItemBase()
        {
            AddStat = new int[Enum.GetNames(typeof(Stat)).Length];
            StatReq = new int[Enum.GetNames(typeof(Stat)).Length];

            Name = "";
            Description = "";
            Ammo = -1;
            Stackable = 1;
        }

        public string Name;
        public int Icon;
        public string Description;
        public byte Type;
        public byte SubType;
        public int Data1;
        public int Data2;
        public int Data3;

        // Common event trigger (match NPC/Skill/Resource editors)
        // 0 = None, 1..N = (CommonEventTrigger + 1)
        public byte CommonEventType;
        public int CommonEventData1;
        public int CommonEventData2;
        public int JobReq;
        public int AccessReq;
        public int LevelReq;
        public byte Mastery;
        public int Price;
        public int[] AddStat;
        public byte Rarity;
        public int Speed;
        public byte BindType;
        public int[] StatReq;
        public int Animation;
        public int Paperdoll;
        public byte Stackable;
        public byte ItemLevel;
        public byte KnockBack;
        public byte KnockBackTiles;
        public int Projectile;
        public int Ammo;

        public static List<ItemBase> Instance { get; private set; } = new List<ItemBase>();

        public static void OnClear(int index)
        {
            if (Instance.Count > index)
                Instance[index] = new ItemBase();
        }

        public static void OnClearChanged()
        {
            IsChanged = new bool[Variables.MaxItems];
        }

        public static void OnReset()
        {
            for (int i = 0; i < Instance.Count; i++)
                OnClear(i);
        }

        public static void OnDraw(int index) => throw new NotImplementedException();
        public static void OnLoad(int index) => throw new NotImplementedException();
        public static void OnSave(int index) => throw new NotImplementedException();
        public static void OnUpdate(int index) => throw new NotImplementedException();
    }
}
