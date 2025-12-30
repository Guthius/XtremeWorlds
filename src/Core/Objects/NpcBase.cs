using Core.Globals;
using Core.Interfaces;

namespace Core.Objects;

public class NpcBase : IData
{
    public static bool[] IsChanged { get; set; } = new bool[Variables.MaxNpcs];

    public NpcBase()
    {
        Name = string.Empty;
        AttackSay = string.Empty;

        DropChance = new int[Variables.MaxDropItems];
        DropItem = new int[Variables.MaxDropItems];
        DropItemValue = new int[Variables.MaxDropItems];

        Stat = new byte[Enum.GetNames(typeof(Stat)).Length];
        Skill = new byte[Variables.MaxNpcSkills];
    }

    public string Name { get; set; }
    public string AttackSay { get; set; }
    public int Sprite { get; set; }
    public byte SpawnTime { get; set; }
    public int SpawnSecs { get; set; }
    public byte Behavior { get; set; }
    public byte Range { get; set; }

    public int[] DropChance { get; set; }
    public int[] DropItem { get; set; }
    public int[] DropItemValue { get; set; }

    public byte[] Stat { get; set; }
    public byte Faction { get; set; }
    public int Hp { get; set; }
    public int Experience { get; set; }
    public int Animation { get; set; }
    public byte[] Skill { get; set; }
    public byte Level { get; set; }
    public int Damage { get; set; }

    // Optional death tracking (0 = none)
    public int DeathSwitch { get; set; }
    public int DeathSwitchValue { get; set; } = 1;
    public int DeathVariable { get; set; }
    public int DeathVariableValue { get; set; } = 1;

    // Optional common event trigger (0 = none; otherwise matches editor selection)
    public byte CommonEventType { get; set; }
    public int CommonEventData1 { get; set; }
    public int CommonEventData2 { get; set; }

    public static List<NpcBase> Instance { get; } = new();

    public static void OnClear(int index)
    {
        if (Instance.Count > index)
        {
            Instance[index] = new NpcBase();
        }
    }

    public static void OnClearChanged()
    {
        IsChanged = new bool[Variables.MaxNpcs];
    }

    public static void OnReset()
    {
        for (var i = 0; i < Instance.Count; i++)
        {
            OnClear(i);
        }
    }

    public static void OnDraw(int index) => throw new NotImplementedException();
    public static void OnLoad(int index) => throw new NotImplementedException();
    public static void OnSave(int index) => throw new NotImplementedException();
    public static void OnUpdate(int index) => throw new NotImplementedException();
}
