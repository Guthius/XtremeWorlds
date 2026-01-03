using Core.Globals;
using Core.Interfaces;
using static Core.Globals.Type;

namespace Core.Objects
{
    public class PlayerBase
    {
        public string Name;
        public byte Sex;
        public byte Job;
        public int Sprite;
        public byte Level;
        public int Experience;
        public byte Access;
        public bool Pk;
        public int[] Vital;
        public int[] MaxVital;
        public int[] Stat;
        public int Points;
        public Paperdoll[] Paperdoll;
        public Item[] Inventory;
        public SkillBook[] Skill;
        public int Map;
        public int X;
        public int Y;
        public byte Dir;
        public Hotbar[] Hotbar;
        public byte[] Switches;
        public int[] Variables;
        public ResourceType[] GatherSkills;
        public byte Moving;
        public bool IsMoving;
        public byte Attacking;
        public int AttackTimer;
        public byte Steps;
        public int Emote;
        public int EmoteTimer;
        public int EventTimer;
        public int GuildId;
        public bool Dead;
        public int DeathTimer;

        public static List<PlayerBase> Instance { get; private set; } = new List<PlayerBase>();

        public static void EnsureSize(int size)
        {
            if (size <= 0)
            {
                return;
            }

            if (Instance.Count >= size)
            {
                return;
            }

            lock (Instance)
            {
                while (Instance.Count < size)
                {
                    Instance.Add(new PlayerBase());
                }
            }
        }

        public PlayerBase()
        {
            Name = "";
            Access = (byte)AccessLevel.Player;
            Vital = new int[Enum.GetNames(typeof(Vital)).Length];
            MaxVital = new int[Enum.GetNames(typeof(Vital)).Length];
            Stat = new int[Enum.GetNames(typeof(Stat)).Length];
            Paperdoll = new Paperdoll[Enum.GetNames(typeof(Equipment)).Length];
            Inventory = new Item[Core.Globals.Variables.MaxInventory];
            Skill = new SkillBook[Core.Globals.Variables.MaxSkills];
            Hotbar = new Hotbar[Core.Globals.Variables.MaxHotbar];
            Switches = new byte[Core.Globals.Variables.MaxSwitches];
            Variables = new int[Core.Globals.Variables.MaxVariables];
            GatherSkills = new ResourceType[Enum.GetNames(typeof(ResourceSkill)).Length];

            for (int i = 0; i < Paperdoll.Length; i++)
                Paperdoll[i].Num = -1;

            for (int i = 0; i < Inventory.Length; i++)
                Inventory[i].Num = -1;

            for (int i = 0; i < Skill.Length; i++)
                Skill[i].Num = -1;

            for (int i = 0; i < Hotbar.Length; i++)
                Hotbar[i].Slot = -1;
        }        
    }
}