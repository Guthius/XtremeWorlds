using Core.Globals;
using Core.Interfaces;

namespace Core.Objects;

public class SkillBase : IData
{
    public static bool[] IsChanged { get; set; } = new bool[Variables.MaxSkills];

    public SkillBase()
    {
        Name = string.Empty;
        JobReq = -1;
        Projectile = -1;
        ChainOnHitSkillId = -1;
    }

    public string Name { get; set; }
    public byte Type { get; set; }
    public int MpCost { get; set; }
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

    public static List<SkillBase> Instance { get; } = new();

    public static void EnsureSize(int size)
    {
        while (Instance.Count < size)
        {
            Instance.Add(new SkillBase());
        }
    }

    public Core.Globals.Type.Skill ToStruct()
    {
        return new Core.Globals.Type.Skill
        {
            Name = Name ?? string.Empty,
            Type = Type,
            MpCost = MpCost,
            LevelReq = LevelReq,
            AccessReq = AccessReq,
            JobReq = JobReq,
            CastTime = CastTime,
            CdTime = CdTime,
            Icon = Icon,
            Map = Map,
            X = X,
            Y = Y,
            Dir = Dir,
            Vital = Vital,
            Duration = Duration,
            Interval = Interval,
            Range = Range,
            IsAoE = IsAoE,
            AoE = AoE,
            CastAnim = CastAnim,
            SkillAnim = SkillAnim,
            StunDuration = StunDuration,
            IsProjectile = IsProjectile,
            Projectile = Projectile,
            KnockBack = KnockBack,
            KnockBackTiles = KnockBackTiles,
            MultiDirMask = MultiDirMask,
            ChainOnHitSkillId = ChainOnHitSkillId,
            CommonEventType = CommonEventType,
            CommonEventData1 = CommonEventData1,
            CommonEventData2 = CommonEventData2,
        };
    }

    public void FromStruct(Core.Globals.Type.Skill skill)
    {
        Name = skill.Name ?? string.Empty;
        Type = skill.Type;
        MpCost = skill.MpCost;
        LevelReq = skill.LevelReq;
        AccessReq = skill.AccessReq;
        JobReq = skill.JobReq;
        CastTime = skill.CastTime;
        CdTime = skill.CdTime;
        Icon = skill.Icon;
        Map = skill.Map;
        X = skill.X;
        Y = skill.Y;
        Dir = skill.Dir;
        Vital = skill.Vital;
        Duration = skill.Duration;
        Interval = skill.Interval;
        Range = skill.Range;
        IsAoE = skill.IsAoE;
        AoE = skill.AoE;
        CastAnim = skill.CastAnim;
        SkillAnim = skill.SkillAnim;
        StunDuration = skill.StunDuration;
        IsProjectile = skill.IsProjectile;
        Projectile = skill.Projectile;
        KnockBack = skill.KnockBack;
        KnockBackTiles = skill.KnockBackTiles;
        MultiDirMask = skill.MultiDirMask;
        ChainOnHitSkillId = skill.ChainOnHitSkillId;
        CommonEventType = skill.CommonEventType;
        CommonEventData1 = skill.CommonEventData1;
        CommonEventData2 = skill.CommonEventData2;
    }

    public static void SyncToData(int index)
    {
        if (index < 0 || index >= Data.Skill.Length)
        {
            return;
        }

        EnsureSize(index + 1);
        Data.Skill[index] = Instance[index].ToStruct();
    }

    public static void SyncFromData(int index)
    {
        if (index < 0 || index >= Data.Skill.Length)
        {
            return;
        }

        EnsureSize(index + 1);
        Instance[index].FromStruct(Data.Skill[index]);
    }

    public static void OnClear(int index)
    {
        if (index < 0 || index >= Variables.MaxSkills)
        {
            return;
        }

        EnsureSize(index + 1);
        Instance[index] = new SkillBase();
        SyncToData(index);
    }

    public static void OnClearChanged()
    {
        IsChanged = new bool[Variables.MaxSkills];
    }

    public static void OnReset()
    {
        for (var i = 0; i < Variables.MaxSkills; i++)
        {
            OnClear(i);
        }
    }

    public static void OnDraw(int index) => throw new NotImplementedException();
    public static void OnLoad(int index) => throw new NotImplementedException();
    public static void OnSave(int index) => throw new NotImplementedException();
    public static void OnUpdate(int index) => throw new NotImplementedException();
}
