using Core.Objects;
using static Core.Globals.Type;
using Bank = Core.Objects.Bank;

namespace Core.Globals;

public static class Commands
{
    private static readonly int EquipmentCount = Enum.GetNames<Equipment>().Length;

    private static bool ValidPlayerIndex(int index)
    {
        return PlayerBase.Instance != null && index >= 0 && index < PlayerBase.Instance.Count;
    }

    public static int GetPlayerExperience(int index)
    {
        if (!ValidPlayerIndex(index)) return 0;
        return PlayerBase.Instance[index].Experience;
    }

    public static int GetPlayerRawStat(int index, Stat stat)
    {
        if (!ValidPlayerIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        var si = (int)stat;
        if (p.Stat == null || si < 0 || si >= p.Stat.Length) return 0;
        return p.Stat[si];
    }

    public static string GetPlayerName(int index)
    {
        if (!ValidPlayerIndex(index)) return string.Empty;
        return PlayerBase.Instance[index].Name ?? string.Empty;
    }

    public static int GetPlayerInvValue(int index, int invslot)
    {
        if (!ValidPlayerIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        if (p.Inventory == null || invslot < 0 || invslot >= p.Inventory.Length) return 0;
        return p.Inventory[invslot].Value;
    }

    public static int GetPlayerPoints(int index)
    {
        if (!ValidPlayerIndex(index)) return 0;
        return PlayerBase.Instance[index].Points;
    }

    public static int GetPlayerVital(int index, Vital vital)
    {
        if (!ValidPlayerIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        var vi = (int)vital;
        if (p.Vital == null || vi < 0 || vi >= p.Vital.Length) return 0;
        return p.Vital[vi];
    }

    public static int GetPlayerSprite(int index)
    {
        if (!ValidPlayerIndex(index)) return 0;
        return PlayerBase.Instance[index].Sprite;
    }

    public static byte GetPlayerJob(int index)
    {
        if (!ValidPlayerIndex(index)) return 0;
        return PlayerBase.Instance[index].Job;
    }

    public static int GetPlayerMap(int index)
    {
        if (!ValidPlayerIndex(index)) return 0;
        return PlayerBase.Instance[index].Map;
    }

    public static int GetPlayerLevel(int index)
    {
        if (!ValidPlayerIndex(index)) return 0;
        return PlayerBase.Instance[index].Level;
    }

    public static int GetPlayerPaperdoll(int index, Equipment equipmentSlot)
    {
        if (!ValidPlayerIndex(index)) return -1;
        var p = PlayerBase.Instance[index];
        var es = (int)equipmentSlot;
        if (p.Paperdoll == null || es < 0 || es >= p.Paperdoll.Length) return -1;
        return p.Paperdoll[es].Num;
    }

    public static int GetPlayerSkill(int index, int skillSlot)
    {
        if (!ValidPlayerIndex(index)) return -1;
        var p = PlayerBase.Instance[index];
        if (p.Skill == null || skillSlot < 0 || skillSlot >= p.Skill.Length) return -1;
        return p.Skill[skillSlot].Num;
    }

    public static int GetPlayerSkillCd(int index, int skillSlot)
    {
        if (!ValidPlayerIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        if (p.Skill == null || skillSlot < 0 || skillSlot >= p.Skill.Length) return 0;
        return p.Skill[skillSlot].Cd;
    }

    public static int GetPlayerStat(int index, Stat stat)
    {
        if (!ValidPlayerIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        var si = (int)stat;
        if (p.Stat == null || si < 0 || si >= p.Stat.Length) return 0;
        return p.Stat[si];
    }

    public static byte GetPlayerAccess(int index)
    {
        if (!ValidPlayerIndex(index)) return 0;
        return PlayerBase.Instance[index].Access;
    }

    public static int GetPlayerX(int index)
    {
        if (!ValidPlayerIndex(index)) return 0;
        return (int)Math.Floor((double)PlayerBase.Instance[index].X / Constants.TileSize);
    }

    public static int GetPlayerY(int index)
    {
        if (!ValidPlayerIndex(index)) return 0;
        return (int)Math.Floor((double)PlayerBase.Instance[index].Y / Constants.TileSize);
    }

    public static int GetPlayerRawX(int index)
    {
        if (!ValidPlayerIndex(index)) return 0;
        return PlayerBase.Instance[index].X;
    }

    public static int GetPlayerRawY(int index)
    {
        if (!ValidPlayerIndex(index)) return 0;
        return PlayerBase.Instance[index].Y;
    }

    public static byte GetPlayerDir(int index)
    {
        if (!ValidPlayerIndex(index)) return 0;
        return PlayerBase.Instance[index].Dir;
    }

    public static bool GetPlayerPk(int index)
    {
        if (!ValidPlayerIndex(index)) return false;
        return PlayerBase.Instance[index].Pk;
    }

    public static void SetPlayerVital(int index, Vital vital, int value)
    {
        if (!ValidPlayerIndex(index)) return;
        var p = PlayerBase.Instance[index];
        var vi = (int)vital;
        if (p.Vital == null || vi < 0 || vi >= p.Vital.Length) return;
        p.Vital[vi] = value;
    }

    public static int GetPlayerMaxVital(int index, Vital vital)
    {
        if (!ValidPlayerIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        var vi = (int)vital;
        if (p.MaxVital == null || vi < 0 || vi >= p.MaxVital.Length) return 0;
        return p.MaxVital[vi];
    }

    public static int SetPlayerMaxVital(int index, Vital vital, int value)
    {
        if (!ValidPlayerIndex(index)) return 0;
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

    public static void SetPlayerGatherSkillLevel(int index, int skillSlot, int level)
    {
        if (!ValidPlayerIndex(index)) return;
        var p = PlayerBase.Instance[index];
        if (p.GatherSkills == null || skillSlot < 0 || skillSlot >= p.GatherSkills.Length) return;
        p.GatherSkills[skillSlot].SkillLevel = level;
    }

    public static void SetPlayerGatherSkillExperience(int index, int skillSlot, int exp)
    {
        if (!ValidPlayerIndex(index)) return;
        var p = PlayerBase.Instance[index];
        if (p.GatherSkills == null || skillSlot < 0 || skillSlot >= p.GatherSkills.Length) return;
        p.GatherSkills[skillSlot].SkillCurExperience = exp;
    }

    public static void SetPlayerGatherSkillMaxExperience(int index, int skillSlot, int maxExp)
    {
        if (!ValidPlayerIndex(index)) return;
        var p = PlayerBase.Instance[index];
        if (p.GatherSkills == null || skillSlot < 0 || skillSlot >= p.GatherSkills.Length) return;
        p.GatherSkills[skillSlot].SkillNextLevelExperience = maxExp;
    }

    public static string GetResourceSkillName(ResourceSkill skillNum)
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

    public static long GetSkillNextLevel(int index, int skillSlot)
    {
        int level = GetPlayerGatherSkillLevel(index, skillSlot);
        int str = GetPlayerStat(index, Stat.Strength);
        int vit = GetPlayerStat(index, Stat.Vitality);
        int intellect = GetPlayerStat(index, Stat.Intelligence);
        int luck = GetPlayerStat(index, Stat.Luck);
        int points = GetPlayerPoints(index);

        long next = (long)(level + 1) * (str + vit + intellect + luck + points) * 25L;
        return next;
    }

    public static bool IsPlaying(int index)
    {
        return GetPlayerName(index)?.Length > 0;
    }

    public static int GetPlayerGatherSkillLevel(int index, int skillSlot)
    {
        if (!ValidPlayerIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        if (p.GatherSkills == null || skillSlot < 0 || skillSlot >= p.GatherSkills.Length) return 0;
        return p.GatherSkills[skillSlot].SkillLevel;
    }

    public static int GetPlayerGatherSkillExperience(int index, int skillSlot)
    {
        if (!ValidPlayerIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        if (p.GatherSkills == null || skillSlot < 0 || skillSlot >= p.GatherSkills.Length) return 0;
        return p.GatherSkills[skillSlot].SkillCurExperience;
    }

    public static int GetPlayerGatherSkillMaxExperience(int index, int skillSlot)
    {
        if (!ValidPlayerIndex(index)) return 0;
        var p = PlayerBase.Instance[index];
        if (p.GatherSkills == null || skillSlot < 0 || skillSlot >= p.GatherSkills.Length) return 0;
        return p.GatherSkills[skillSlot].SkillNextLevelExperience;
    }

    public static void SetPlayerMap(int index, int map)
    {
        if (!ValidPlayerIndex(index)) return;
        PlayerBase.Instance[index].Map = map;
    }

    public static int GetPlayerInv(int index, int invslot)
    {
        if (!ValidPlayerIndex(index)) return -1;
        var p = PlayerBase.Instance[index];
        if (p.Inventory == null || invslot < 0 || invslot >= p.Inventory.Length) return -1;
        return p.Inventory[invslot].Num;
    }

    public static void SetPlayerName(int index, string name)
    {
        if (!ValidPlayerIndex(index)) return;
        PlayerBase.Instance[index].Name = name;
    }

    public static void SetPlayerJob(int index, byte job)
    {
        if (!ValidPlayerIndex(index)) return;
        PlayerBase.Instance[index].Job = job;
    }

    public static void SetPlayerPoints(int index, int points)
    {
        if (!ValidPlayerIndex(index)) return;
        if (points < 0) points = 0;
        if (points > Core.Globals.Variables.MaxPoints) points = Core.Globals.Variables.MaxPoints;
        PlayerBase.Instance[index].Points = points;
    }

    public static void SetPlayerStat(int index, Stat stat, int value)
    {
        if (!ValidPlayerIndex(index)) return;
        var p = PlayerBase.Instance[index];
        var si = (int)stat;
        if (p.Stat == null || si < 0 || si >= p.Stat.Length) return;
        p.Stat[si] = (byte)value;
    }

    public static void SetInv(int index, int invSlot, int item)
    {
        if (!ValidPlayerIndex(index)) return;
        var p = PlayerBase.Instance[index];
        if (p.Inventory == null || invSlot < 0 || invSlot >= p.Inventory.Length) return;
        p.Inventory[invSlot].Num = item;
    }

    public static void SetInvValue(int index, int invSlot, int value)
    {
        if (!ValidPlayerIndex(index)) return;
        var p = PlayerBase.Instance[index];
        if (p.Inventory == null || invSlot < 0 || invSlot >= p.Inventory.Length) return;
        p.Inventory[invSlot].Value = value;
    }

    public static void SetPlayerAccess(int index, byte access)
    {
        if (!ValidPlayerIndex(index)) return;
        PlayerBase.Instance[index].Access = access;
    }

    public static void SetPlayerPk(int index, bool pk)
    {
        if (!ValidPlayerIndex(index)) return;
        PlayerBase.Instance[index].Pk = pk;
    }

    public static void SetPlayerX(int index, int x)
    {
        if (!ValidPlayerIndex(index)) return;
        PlayerBase.Instance[index].X = x;
    }

    public static void SetPlayerY(int index, int y)
    {
        if (!ValidPlayerIndex(index)) return;
        PlayerBase.Instance[index].Y = y;
    }

    public static void SetPlayerSprite(int index, int sprite)
    {
        if (!ValidPlayerIndex(index)) return;
        PlayerBase.Instance[index].Sprite = sprite;
    }

    public static void SetPlayerExperience(int index, int experience)
    {
        if (!ValidPlayerIndex(index)) return;
        PlayerBase.Instance[index].Experience = experience;
    }

    public static void SetPlayerLevel(int index, int level)
    {
        if (!ValidPlayerIndex(index)) return;
        if (level < 0) level = 0;
        if (level > Core.Globals.Variables.MaxLevel) level = Core.Globals.Variables.MaxLevel;
        PlayerBase.Instance[index].Level = (byte)level;
    }

    public static void SetPlayerDir(int index, int dir)
    {
        if (!ValidPlayerIndex(index)) return;
        PlayerBase.Instance[index].Dir = (byte)dir;
    }

    public static void SetPlayerPaperdoll(int index, int item, Equipment equipmentSlot)
    {
        if (!ValidPlayerIndex(index)) return;
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
                        if (GetPlayerName(i) != GetPlayerName(index))
                            return GetPlayerName(i);
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
            if (GetPlayerSkill(index, slot) == -1)
            {
                return slot;
            }
        }

        return -1;
    }

    public static void SetPlayerSkillCd(int index, int skillSlot, int value)
    {
        if (!ValidPlayerIndex(index)) return;
        var p = PlayerBase.Instance[index];
        if (p.Skill == null || skillSlot < 0 || skillSlot >= p.Skill.Length) return;
        p.Skill[skillSlot].Cd = value;
    }

    public static bool HasSkill(int index, double skillNum)
    {
        for (var slot = 0; slot < Core.Globals.Variables.MaxPlayerSkills; slot++)
        {
            if (GetPlayerSkill(index, slot) == skillNum)
            {
                return true;
            }
        }

        return false;
    }

    public static void SetPlayerSkill(int index, int skillslot, int skillNum)
    {
        if (!ValidPlayerIndex(index)) return;
        var p = PlayerBase.Instance[index];
        if (p.Skill == null || skillslot < 0 || skillslot >= p.Skill.Length) return;
        p.Skill[skillslot].Num = skillNum;
    }

    public static int GetBank(int index, int bankslot)
    {
        if (Bank.Instance == null || index < 0 || index >= Bank.Instance.Count) return -1;
        var b = Bank.Instance[index];
        if (b.Item == null || bankslot < 0 || bankslot >= b.Item.Length) return -1;
        return b.Item[bankslot].Num;
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