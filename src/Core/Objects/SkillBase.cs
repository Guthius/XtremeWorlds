using Core.Globals;

namespace Core.Objects;

public class SkillBase
{
    public static bool[] IsStreaming { get; set; } = new bool[Variables.MaxSkills];
    public static bool[] IsChanged { get; set; } = new bool[Variables.MaxSkills];

    public SkillBase()
    {
        Name = string.Empty;
        JobReq = -1;
        Projectile = -1;
        ChainOnHitSkillId = -1;

        MoveSpeedMultiplier = 1.0f;
    }

    public string Name { get; set; }
    public byte Type { get; set; }
    public int MpCost { get; set; }
    public int SpCost { get; set; }
    public int LevelReq { get; set; }
    public int AccessReq { get; set; }
    public int JobReq { get; set; }
    public int CastTime { get; set; }
    public int CdTime { get; set; }
    public int Icon { get; set; }
    public int Map { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public byte Dir { get; set; }
    public int Vital { get; set; }
    public int Duration { get; set; }
    public int Interval { get; set; }
    public int Range { get; set; }
    public bool IsAoE { get; set; }
    public int AoE { get; set; }
    public int CastAnim { get; set; }
    public int SkillAnim { get; set; }
    public int StunDuration { get; set; }
    public int IsProjectile { get; set; }
    public int Projectile { get; set; }
    public byte KnockBack { get; set; }
    public byte KnockBackTiles { get; set; }
    public int MultiDirMask { get; set; }
    public int ChainOnHitSkillId { get; set; }
    public byte CommonEventType { get; set; }
    public int CommonEventData1 { get; set; }
    public int CommonEventData2 { get; set; }

    // Multiplies movement speed while this skill effect is active (typically via Duration).
    // 1.0 = no change, <1 slows, >1 speeds up.
    public float MoveSpeedMultiplier { get; set; }

    public static List<SkillBase> Instance { get; private set; } = new List<SkillBase>();

    public static void OnClear(int index)
    {
        if (Instance.Count > index)
            Instance[index] = new SkillBase();
    }

    public static void OnClearChanged()
    {
        IsChanged = new bool[Variables.MaxSkills];
        IsStreaming = new bool[Variables.MaxSkills];
    }

    public static void OnClear()
    {
        for (var i = 0; i < Variables.MaxSkills; i++)
            OnClear(i);
    }

}
