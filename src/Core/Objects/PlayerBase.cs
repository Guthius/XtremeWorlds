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
        public int Exp;
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
                if (Data.TempPlayer is null)
                {
                    Data.TempPlayer = new TempPlayer[Math.Max(size, Core.Globals.Variables.MaxPlayers)];
                }

                if (Data.TempPlayer.Length < size)
                {
                    Array.Resize(ref Data.TempPlayer, size);
                }

                while (Instance.Count < size)
                {
                    var index = Instance.Count;
                    Instance.Add(new PlayerBase());

                    // Clear and re-init transient per-player runtime state for the new slot.
                    Data.TempPlayer[index] = new TempPlayer
                    {
                        InGame = false,
                        GettingMap = false,

                        Target = -1,
                        TargetType = 0,

                        PartyInvite = -1,
                        InParty = -1,

                        SkillBuffer = -1,
                        InShop = -1,
                        InTrade = 0,

                        Editor = EditorType.None,

                        MoveSpeedMultiplier = 1.0f,
                        MoveSpeedMultiplierTimer = 0,

                        SkillCd = new int[Core.Globals.Variables.MaxPlayerSkills],
                        TradeOffer = new Item[Core.Globals.Variables.MaxInventory],
                        EventProcessing = new EventProcessing[1],
                        EventMap = new EventMap { CurrentEvents = 0, EventPages = new MapEvent[1] },
                    };
                }
            }
        }

        public PlayerBase()
        {
            Name = "";
            Access = (byte)Globals.Access.Player;
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