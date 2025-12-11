using Core.Objects;
using static Core.Globals.Type;

namespace Core.Globals;

public static class Commands
{
    private static readonly int EquipmentCount = Enum.GetNames<Equipment>().Length;

    public static int GetPlayerExperience(int index)
    {
        return PlayerBase.Instance[index].Experience;
    }

    public static int GetPlayerRawStat(int index, Stat stat)
    {
        return PlayerBase.Instance[index].Stat[(int)stat];
    }

    public static string GetPlayerName(int index)
    {
        return PlayerBase.Instance[index].Name;
    }

    public static int GetPlayerInventoryValue(int index, int invslot)
    {
        return PlayerBase.Instance[index].Inventory[invslot].Value;
    }

    public static int GetPlayerPoints(int index)
    {
        return PlayerBase.Instance[index].Points;
    }

    public static int GetPlayerVital(int index, Vital vital)
    {
        return PlayerBase.Instance[index].Vital[(int)vital];
    }

    public static int GetPlayerSprite(int index)
    {
        return PlayerBase.Instance[index].Sprite;
    }

    public static int GetPlayerJob(int index)
    {
        return PlayerBase.Instance[index].Job;
    }

    public static int GetPlayerMap(int index)
    {
        return PlayerBase.Instance[index].Map;
    }

    public static int GetPlayerLevel(int index)
    {
        return PlayerBase.Instance[index].Level;
    }

    public static int GetPlayerPaperdoll(int index, Equipment equipmentSlot)
    {
        return PlayerBase.Instance[index].Paperdoll[(int)equipmentSlot].Num;
    }

    public static int GetPlayerSkill(int index, int skillSlot)
    {
        return PlayerBase.Instance[index].Skill[skillSlot].Num;
    }

    public static int GetPlayerSkillCd(int index, int skillSlot)
    {
        return PlayerBase.Instance[index].Skill[skillSlot].Cd;
    }

    public static int GetPlayerStat(int index, Stat stat)
    {
        int statValue = PlayerBase.Instance[index].Stat[(int)stat];

        return statValue;
    }

    public static byte GetPlayerAccess(int index)
    {
        return PlayerBase.Instance[index].Access;
    }

    public static int GetPlayerX(int index)
    {
        return (int)Math.Floor((double)PlayerBase.Instance[index].X / Constants.TileSize);
    }

    public static int GetPlayerY(int index)
    {
        return (int)Math.Floor((double)PlayerBase.Instance[index].Y / Constants.TileSize);
    }

    public static int GetPlayerRawX(int index)
    {
        return PlayerBase.Instance[index].X;
    }

    public static int GetPlayerRawY(int index)
    {
        return PlayerBase.Instance[index].Y;
    }

    public static byte GetPlayerDir(int index)
    {
        return PlayerBase.Instance[index].Dir;
    }

    public static bool GetPlayerPk(int index)
    {
        return PlayerBase.Instance[index].Pk;
    }

    public static void SetPlayerVital(int index, Vital vital, int value)
    {
        PlayerBase.Instance[index].Vital[(int)vital] = value;
    }

    public static int GetPlayerMaxVital(int index, Vital vital)
    {
        return PlayerBase.Instance[index].MaxVital[(int)vital];
    }

    public static int SetPlayerMaxVital(int index, Vital vital, int value)
    {
        PlayerBase.Instance[index].MaxVital[(int)vital] = value;
        return PlayerBase.Instance[index].MaxVital[(int)vital];
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
        PlayerBase.Instance[index].GatherSkills[skillSlot].SkillLevel = level;
    }

    public static void SetPlayerGatherSkillExperience(int index, int skillSlot, int exp)
    {
        PlayerBase.Instance[index].GatherSkills[skillSlot].SkillCurExp = exp;
    }

    public static void SetPlayerGatherSkillMaxExperience(int index, int skillSlot, int maxExp)
    {
        PlayerBase.Instance[index].GatherSkills[skillSlot].SkillNextLevelExp = maxExp;
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
        return PlayerBase.Instance[index].GatherSkills[skillSlot].SkillLevel;
    }

    public static int GetPlayerGatherSkillExperience(int index, int skillSlot)
    {
        return PlayerBase.Instance[index].GatherSkills[skillSlot].SkillCurExp;
    }

    public static int GetPlayerGatherSkillMaxExperience(int index, int skillSlot)
    {
        return PlayerBase.Instance[index].GatherSkills[skillSlot].SkillNextLevelExp;
    }

    public static void SetPlayerMap(int index, int mapNum)
    {
        PlayerBase.Instance[index].Map = mapNum;
    }

    public static int GetPlayerInventory(int index, int invslot)
    {
        return PlayerBase.Instance[index].Inventory[invslot].Num;
    }

    public static void SetPlayerName(int index, string name)
    {
        PlayerBase.Instance[index].Name = name;
    }

    public static void SetPlayerJob(int index, int job)
    {
        PlayerBase.Instance[index].Job = (byte)job;
    }

    public static void SetPlayerPoints(int index, int points)
    {
        PlayerBase.Instance[index].Points = (byte)points;
    }

    public static void SetPlayerStat(int index, Stat stat, int value)
    {
        PlayerBase.Instance[index].Stat[(int)stat] = (byte)value;
    }

    public static void SetInventory(int index, int invSlot, int itemNum)
    {
        PlayerBase.Instance[index].Inventory[invSlot].Num = itemNum;
    }

    public static void SetInventoryValue(int index, int invslot, int itemValue)
    {
        PlayerBase.Instance[index].Inventory[invslot].Value = itemValue;
    }

    public static void SetPlayerAccess(int index, byte access)
    {
        PlayerBase.Instance[index].Access = access;
    }

    public static void SetPlayerPk(int index, bool pk)
    {
        PlayerBase.Instance[index].Pk = pk;
    }

    public static void SetPlayerX(int index, int x)
    {
        PlayerBase.Instance[index].X = x;
    }

    public static void SetPlayerY(int index, int y)
    {
        PlayerBase.Instance[index].Y = y;
    }

    public static void SetPlayerSprite(int index, int sprite)
    {
        PlayerBase.Instance[index].Sprite = sprite;
    }

    public static void SetPlayerExperience(int index, int experience)
    {
        PlayerBase.Instance[index].Experience = experience;
    }

    public static void SetPlayerLevel(int index, int level)
    {
        PlayerBase.Instance[index].Level = (byte)level;
    }

    public static void SetPlayerDir(int index, int dir)
    {
        PlayerBase.Instance[index].Dir = (byte)dir;
    }

    public static void SetPlayerPaperdoll(int index, int itemNum, Equipment equipmentSlot)
    {
        PlayerBase.Instance[index].Paperdoll[(int)equipmentSlot].Num = itemNum;
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
        for (var slot = 0; slot < Variables.MaxPlayerSkills; slot++)
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
        PlayerBase.Instance[index].Skill[skillSlot].Cd = value;
    }

    public static bool HasSkill(int index, double skillNum)
    {
        for (var slot = 0; slot < Variables.MaxPlayerSkills; slot++)
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
        PlayerBase.Instance[index].Skill[skillslot].Num = skillNum;
    }

    public static int GetBank(int index, int bankslot)
    {
        return Objects.Bank.Instance[index].Item[bankslot].Num;
    }

    public static void SetBank(int index, byte bankSlot, int itemNum)
    {
        Objects.Bank.Instance[index].Item[bankSlot].Num = itemNum;
    }

    public static int GetBankValue(int index, int bankSlot)
    {
        return Objects.Bank.Instance[index].Item[bankSlot].Value;
    }

    public static void SetBankValue(int index, byte bankSlot, int itemValue)
    {
        Objects.Bank.Instance[index].Item[bankSlot].Value = itemValue;
    }
}