using Core.Objects;
using static Core.Globals.Type;
using Bank = Core.Objects.Bank;

namespace Core.Globals;

public static class Commands
{
    private static readonly int EquipmentCount = Enum.GetNames<Equipment>().Length;

    private static bool ValidIndex(int index)
    {
        return PlayerBase.Instance != null && index >= 0 && index < PlayerBase.Instance.Count;
    }

    public static int GetExp(int index)
    {
        if (!ValidIndex(index)) return 0;
        return PlayerBase.Instance[index].Exp;
    }

    public static int GetRawStat(int index, Stat stat)
    {
        if (!ValidIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        var si = (int)stat;
        if (p.Stat == null || si < 0 || si >= p.Stat.Length) return 0;
        return p.Stat[si];
    }

    public static string GetName(int index)
    {
        if (!ValidIndex(index)) return string.Empty;
        return PlayerBase.Instance[index].Name ?? string.Empty;
    }

    public static int GetInvValue(int index, int invslot)
    {
        if (!ValidIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        if (p.Inventory == null || invslot < 0 || invslot >= p.Inventory.Length) return 0;
        return p.Inventory[invslot].Value;
    }

    public static int GetPoints(int index)
    {
        if (!ValidIndex(index)) return 0;
        return PlayerBase.Instance[index].Points;
    }

    public static int GetVital(int index, Vital vital)
    {
        if (!ValidIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        var vi = (int)vital;
        if (p.Vital == null || vi < 0 || vi >= p.Vital.Length) return 0;
        return p.Vital[vi];
    }

    public static int GetSprite(int index)
    {
        if (!ValidIndex(index)) return 0;
        return PlayerBase.Instance[index].Sprite;
    }

    public static byte GetJob(int index)
    {
        if (!ValidIndex(index)) return 0;
        return PlayerBase.Instance[index].Job;
    }

    public static int GetMap(int index)
    {
        if (!ValidIndex(index)) return 0;
        return PlayerBase.Instance[index].Map;
    }

    public static int GetLevel(int index)
    {
        if (!ValidIndex(index)) return 0;
        return PlayerBase.Instance[index].Level;
    }

    public static int GetPaperdoll(int index, Equipment equipmentSlot)
    {
        if (!ValidIndex(index)) return -1;
        var p = PlayerBase.Instance[index];
        var es = (int)equipmentSlot;
        if (p.Paperdoll == null || es < 0 || es >= p.Paperdoll.Length) return -1;
        return p.Paperdoll[es].Num;
    }

    public static int GetSkill(int index, int skillSlot)
    {
        if (!ValidIndex(index)) return -1;
        var p = PlayerBase.Instance[index];
        if (p.Skill == null || skillSlot < 0 || skillSlot >= p.Skill.Length) return -1;
        return p.Skill[skillSlot].Num;
    }

    public static int GetSkillCd(int index, int skillSlot)
    {
        if (!ValidIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        if (p.Skill == null || skillSlot < 0 || skillSlot >= p.Skill.Length) return 0;
        return p.Skill[skillSlot].Cd;
    }

    public static int GetStat(int index, Stat stat)
    {
        if (!ValidIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        var si = (int)stat;
        if (p.Stat == null || si < 0 || si >= p.Stat.Length) return 0;
        return p.Stat[si];
    }

    public static byte GetAccess(int index)
    {
        if (!ValidIndex(index)) return 0;
        return PlayerBase.Instance[index].Access;
    }

    public static int GetX(int index)
    {
        if (!ValidIndex(index)) return 0;
        return (int)Math.Floor((double)PlayerBase.Instance[index].X / Constants.TileSize);
    }

    public static int GetY(int index)
    {
        if (!ValidIndex(index)) return 0;
        return (int)Math.Floor((double)PlayerBase.Instance[index].Y / Constants.TileSize);
    }

    public static int GetRawX(int index)
    {
        if (!ValidIndex(index)) return 0;
        return PlayerBase.Instance[index].X;
    }

    public static int GetRawY(int index)
    {
        if (!ValidIndex(index)) return 0;
        return PlayerBase.Instance[index].Y;
    }

    public static byte GetDir(int index)
    {
        if (!ValidIndex(index)) return 0;
        return PlayerBase.Instance[index].Dir;
    }

    public static bool GetPk(int index)
    {
        if (!ValidIndex(index)) return false;
        return PlayerBase.Instance[index].Pk;
    }

    public static void SetVital(int index, Vital vital, int value)
    {
        if (!ValidIndex(index)) return;
        var p = PlayerBase.Instance[index];
        var vi = (int)vital;
        if (p.Vital == null || vi < 0 || vi >= p.Vital.Length) return;
        p.Vital[vi] = value;
    }

    public static int GetMaxVital(int index, Vital vital)
    {
        if (!ValidIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        var vi = (int)vital;
        if (p.MaxVital == null || vi < 0 || vi >= p.MaxVital.Length) return 0;
        return p.MaxVital[vi];
    }

    public static int SetMaxVital(int index, Vital vital, int value)
    {
        if (!ValidIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        var vi = (int)vital;
        if (p.MaxVital == null || vi < 0 || vi >= p.MaxVital.Length) return 0;
        p.MaxVital[vi] = value;
        return p.MaxVital[vi];
    }

    public static bool IsDirBlocked(byte blockvar, Direction dir)
    {
        return dir switch
        {
            Direction.UpRight =>
                (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Up))) != 0 ||
                (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Right))) != 0,

            Direction.UpLeft =>
                (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Up))) != 0 ||
                (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Left))) != 0,

            Direction.DownRight =>
                (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Down))) != 0 ||
                (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Right))) != 0,

            Direction.DownLeft =>
                (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Down))) != 0 ||
                (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Left))) != 0,

            _ => (blockvar & (long)Math.Round(Math.Pow(2d, (byte)dir))) != 0
        };
    }

    public static void SetSkillLevel(int index, int skillSlot, int level)
    {
        if (!ValidIndex(index)) return;
        var p = PlayerBase.Instance[index];
        if (p.GatherSkills == null || skillSlot < 0 || skillSlot >= p.GatherSkills.Length) return;
        p.GatherSkills[skillSlot].Level = level;
    }

    public static void SetSkillExp(int index, int skillSlot, int exp)
    {
        if (!ValidIndex(index)) return;
        var p = PlayerBase.Instance[index];
        if (p.GatherSkills == null || skillSlot < 0 || skillSlot >= p.GatherSkills.Length) return;
        p.GatherSkills[skillSlot].Exp = exp;
    }

    public static void SetSkillMaxExp(int index, int skillSlot, int maxExp)
    {
        if (!ValidIndex(index)) return;
        var p = PlayerBase.Instance[index];
        if (p.GatherSkills == null || skillSlot < 0 || skillSlot >= p.GatherSkills.Length) return;
        p.GatherSkills[skillSlot].MaxExp = maxExp;
    }

    public static string GetResourceName(ResourceSkill skillNum)
    {
        return skillNum switch
        {
            ResourceSkill.Herbalism => "Herbalism",
            ResourceSkill.Woodcutting => "Woodcutting",
            ResourceSkill.Mining => "Mining",
            ResourceSkill.Fishing => "Fishing",
            _ => string.Empty
        };
    }

    public static long GetSkillMaxExp(int index, int skillSlot)
    {
        int level = SetGatherLevel(index, skillSlot);
        int str = GetStat(index, Stat.Strength);
        int vit = GetStat(index, Stat.Vitality);
        int intellect = GetStat(index, Stat.Intelligence);
        int luck = GetStat(index, Stat.Luck);
        int points = GetPoints(index);

        long next = (long)(level + 1) * (str + vit + intellect + luck + points) * 25L;
        return next;
    }

    public static bool IsPlaying(int index)
    {
        return GetName(index)?.Length > 0;
    }

    public static int SetGatherLevel(int index, int skillSlot)
    {
        if (!ValidIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        if (p.GatherSkills == null || skillSlot < 0 || skillSlot >= p.GatherSkills.Length) return 0;
        return p.GatherSkills[skillSlot].Level;
    }

    public static int GetGatherExp(int index, int skillSlot)
    {
        if (!ValidIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        if (p.GatherSkills == null || skillSlot < 0 || skillSlot >= p.GatherSkills.Length) return 0;
        return p.GatherSkills[skillSlot].Exp;
    }

    public static int GetGatherSkillMaxExp(int index, int skillSlot)
    {
        if (!ValidIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        if (p.GatherSkills == null || skillSlot < 0 || skillSlot >= p.GatherSkills.Length) return 0;
        return p.GatherSkills[skillSlot].MaxExp;
    }

    public static void SetMap(int index, int map)
    {
        if (!ValidIndex(index)) return;
        PlayerBase.Instance[index].Map = map;
    }

    public static int GetInv(int index, int invslot)
    {
        if (!ValidIndex(index)) return -1;
        var p = PlayerBase.Instance[index];
        if (p.Inventory == null || invslot < 0 || invslot >= p.Inventory.Length) return -1;
        return p.Inventory[invslot].Num;
    }

    public static void SetName(int index, string name)
    {
        if (!ValidIndex(index)) return;
        PlayerBase.Instance[index].Name = name;
    }

    public static void SetJob(int index, byte job)
    {
        if (!ValidIndex(index)) return;
        PlayerBase.Instance[index].Job = job;
    }

    public static void SetPoints(int index, int points)
    {
        if (!ValidIndex(index)) return;
        if (points < 0) points = 0;
        if (points > Core.Globals.Variables.MaxPoints) points = Core.Globals.Variables.MaxPoints;
        PlayerBase.Instance[index].Points = points;
    }

    public static void SetStat(int index, Stat stat, int value)
    {
        if (!ValidIndex(index)) return;
        var p = PlayerBase.Instance[index];
        var si = (int)stat;
        if (p.Stat == null || si < 0 || si >= p.Stat.Length) return;
        p.Stat[si] = (byte)value;
    }

    public static void SetInv(int index, int invSlot, int item)
    {
        if (!ValidIndex(index)) return;
        var p = PlayerBase.Instance[index];
        if (p.Inventory == null || invSlot < 0 || invSlot >= p.Inventory.Length) return;
        p.Inventory[invSlot].Num = item;
    }

    public static void SetInvValue(int index, int invSlot, int value)
    {
        if (!ValidIndex(index)) return;
        var p = PlayerBase.Instance[index];
        if (p.Inventory == null || invSlot < 0 || invSlot >= p.Inventory.Length) return;
        p.Inventory[invSlot].Value = value;
    }

    public static void SetAccess(int index, byte access)
    {
        if (!ValidIndex(index)) return;
        PlayerBase.Instance[index].Access = access;
    }

    public static void SetPk(int index, bool pk)
    {
        if (!ValidIndex(index)) return;
        PlayerBase.Instance[index].Pk = pk;
    }

    public static void SetX(int index, int x)
    {
        if (!ValidIndex(index)) return;
        PlayerBase.Instance[index].X = x;
    }

    public static void SetY(int index, int y)
    {
        if (!ValidIndex(index)) return;
        PlayerBase.Instance[index].Y = y;
    }

    public static void SetSprite(int index, int sprite)
    {
        if (!ValidIndex(index)) return;
        PlayerBase.Instance[index].Sprite = sprite;
    }

    public static void SetExp(int index, int experience)
    {
        if (!ValidIndex(index)) return;
        PlayerBase.Instance[index].Exp = experience;
    }

    public static void SetLevel(int index, int level)
    {
        if (!ValidIndex(index)) return;
        if (level < 0) level = 0;
        if (level > Core.Globals.Variables.MaxLevel) level = Core.Globals.Variables.MaxLevel;
        PlayerBase.Instance[index].Level = (byte)level;
    }

    public static void SetDir(int index, int dir)
    {
        if (!ValidIndex(index)) return;
        PlayerBase.Instance[index].Dir = (byte)dir;
    }

    public static void SetPaperdoll(int index, int item, Equipment equipmentSlot)
    {
        if (!ValidIndex(index)) return;
        var p = PlayerBase.Instance[index];
        var es = (int)equipmentSlot;
        if (p.Paperdoll == null || es < 0 || es >= p.Paperdoll.Length) return;
        p.Paperdoll[es].Num = item;
    }

    public static string IsEditorLocked(int index, EditorType id)
    {
        for (int i = 0; i < PlayerBase.Instance.Count; i++)
        {
            if (IsPlaying(i))
            {
                if (i != index)
                {
                    if (Data.TempPlayer[i].Editor == id)
                    {
                        if (GetName(i) != GetName(index))
                            return GetName(i);
                    }
                }
            }
        }

        return "";
    }

    public static int FindOpenSkill(int index)
    {
        for (var slot = 0; slot < Core.Globals.Variables.MaxPlayerSkills; slot++)
        {
            if (GetSkill(index, slot) == -1)
            {
                return slot;
            }
        }

        return -1;
    }

    public static void SetSkillCd(int index, int skillSlot, int value)
    {
        if (!ValidIndex(index)) return;
        var p = PlayerBase.Instance[index];
        if (p.Skill == null || skillSlot < 0 || skillSlot >= p.Skill.Length) return;
        p.Skill[skillSlot].Cd = value;
    }

    public static bool HasSkill(int index, double skillNum)
    {
        for (var slot = 0; slot < Core.Globals.Variables.MaxPlayerSkills; slot++)
        {
            if (GetSkill(index, slot) == skillNum)
            {
                return true;
            }
        }

        return false;
    }

    public static void SetSkill(int index, int skillSlot, int skillNum)
    {
        if (!ValidIndex(index)) return;
        var p = PlayerBase.Instance[index];
        if (p.Skill == null || skillSlot < 0 || skillSlot >= p.Skill.Length) return;
        p.Skill[skillSlot].Num = skillNum;
    }

    public static int GetBank(int index, int bankSlot)
    {
        if (Bank.Instance == null || index < 0 || index >= Bank.Instance.Count) return -1;
        var b = Bank.Instance[index];
        if (b.Item == null || bankSlot < 0 || bankSlot >= b.Item.Length) return -1;
        return b.Item[bankSlot].Num;
    }

    public static void SetBank(int index, byte bankSlot, int item)
    {
        if (Bank.Instance == null || index < 0 || index >= Bank.Instance.Count) return;
        var b = Bank.Instance[index];
        if (b.Item == null || bankSlot < 0 || bankSlot >= b.Item.Length) return;
        b.Item[bankSlot].Num = item;
    }

    public static int GetBankValue(int index, int bankSlot)
    {
        if (Bank.Instance == null || index < 0 || index >= Bank.Instance.Count) return 0;
        var b = Bank.Instance[index];
        if (b.Item == null || bankSlot < 0 || bankSlot >= b.Item.Length) return 0;
        return b.Item[bankSlot].Value;
    }

    public static void SetBankValue(int index, byte bankSlot, int itemValue)
    {
        if (Bank.Instance == null || index < 0 || index >= Bank.Instance.Count) return;
        var b = Bank.Instance[index];
        if (b.Item == null || bankSlot < 0 || bankSlot >= b.Item.Length) return;
        b.Item[bankSlot].Value = itemValue;
    }
}