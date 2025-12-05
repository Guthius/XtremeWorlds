using static Core.Globals.Type;

namespace Core.Globals;

/// <summary>
/// Represents a dynamic entity that can be a Player, or Npc.
/// Allows unified access to common fields for logic processing.
/// </summary>
public class Entity
{
    public static List<Entity> Instances = [];

    public enum EntityType
    {
        Player,
        Npc,
    }

    public static int Count(Entity entity)
    {
        return Instances.FindAll(e => e.Type == entity.Type).Count;
    }

    public static int Count()
    {
        return Instances.Count;
    }

    public static int Index(Entity entity)
    {
        if (entity == null || Instances == null)
            return -1; // Handle null cases

        // Get all entities of the same type, sorted by Id
        var entities = Instances = Instances
            .OrderBy(e => e.Map)
            .ThenBy(e => e.Id)
            .ToList();

        // Find the index of the input entity in the sorted list
        return entities.FindIndex(e => e.Id == entity.Id);
    }

    public EntityType Type { get; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Pk { get; set; }
    public byte Sex { get; set; }
    public byte Job { get; set; }
    public byte Level { get; set; }
    public int[]? Vital { get; set; }
    public byte[] Stat { get; set; } = Array.Empty<byte>();
    public byte Points { get; set; }
    public PlayerEq[] Equipment { get; set; } = Array.Empty<PlayerEq>();
    public object[] Inv { get; set; } = Array.Empty<object>();
    public object[] PlayerSkill { get; set; } = Array.Empty<object>();
    public int Map { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public byte Dir { get; set; }
    public int Sprite { get; set; }
    public int Exp { get; set; }
    public byte Access { get; set; }
    public object[] Hotbar { get; set; } = Array.Empty<object>();
    public byte[] Switches { get; set; } = Array.Empty<byte>();
    public int[] Variables { get; set; } = Array.Empty<int>();
    public object? Pet { get; set; }
    public byte Moving { get; set; }
    public byte Attacking { get; set; }
    public int AttackTimer { get; set; }
    public byte Steps { get; set; }
    public int Emote { get; set; }
    public int EmoteTimer { get; set; }
    public int EventTimer { get; set; }
    public object[] Quests { get; set; } = Array.Empty<object>();
    public int GuildId { get; set; }
    public int[] DropChance { get; set; } = Array.Empty<int>();
    public int[] DropItem { get; set; } = Array.Empty<int>();
    public int[] DropItemValue { get; set; } = Array.Empty<int>();
    public string AttackSay { get; set; } = string.Empty;
    public byte SpawnTime { get; set; }
    public int SpawnSecs { get; set; }
    public byte Behavior { get; set; }
    public byte Range { get; set; }
    public int Animation { get; set; }
    public int Hp { get; set; }
    public int Damage { get; set; }
    public int[] Skill { get; set; } = Array.Empty<int>();
    public byte Faction { get; set; }
    public int Num { get; set; }
    public int SkillBuffer { get; set; }
    public int SkillBufferTimer { get; set; }
    public int SpawnWait { get; set; }
    public byte TargetType { get; set; }
    public int Target { get; set; }
    public int StunDuration { get; set; }
    public int StunTimer { get; set; }
    public byte StopRegen { get; set; }
    public ResourceType[] GatherSkills { get; set; } = Array.Empty<ResourceType>();
    public object Raw { get; }

    private Entity(EntityType type, int id, object raw)
    {
        Type = type;
        Id = id;
        Raw = raw;
    }

    public static Entity FromPlayer(int id, Player player)
    {
        return new Entity(EntityType.Player, id, player)
        {
            Name = player.Name,
            Pk = player.Pk,
            Sex = player.Sex,
            Job = player.Job,
            Sprite = player.Sprite,
            Level = player.Level,
            Exp = player.Exp,
            Access = player.Access,
            Vital = player.Vital,
            Stat = player.Stat,
            Points = player.Points,
            Equipment = player.Equipment,
            Inv = player.Inv != null ? Array.ConvertAll(player.Inv, x => (object) x) : new object[Core.Globals.Variables.MaxInv],
            PlayerSkill = player.Skill != null ? Array.ConvertAll(player.Skill, x => (object) x) : new object[Core.Globals.Variables.MaxSkills],
            Map = player.Map,
            X = player.X,
            Y = player.Y,
            Dir = player.Dir,
            Hotbar = player.Hotbar != null ? Array.ConvertAll(player.Hotbar, x => (object) x) : new object[Core.Globals.Variables.MaxHotbar],
            Switches = player.Switches,
            Variables = player.Variables,
            Moving = player.Moving,
            Attacking = player.Attacking,
            AttackTimer = player.AttackTimer,
            Steps = player.Steps,
            Emote = player.Emote,
            EmoteTimer = player.EmoteTimer,
            EventTimer = player.EventTimer,
            Quests = player.Quests != null ? Array.ConvertAll(player.Quests, x => (object) x) : new object[Core.Globals.Variables.MaxQuests],
            GuildId = player.GuildId,
            GatherSkills = player.GatherSkills
        };
    }

    public static Entity FromNpc(int id, MapNpc npc)
    {
        var entity = new Entity(EntityType.Npc, id, npc)
        {
            Num = npc.Num,
            Target = npc.Target,
            TargetType = npc.TargetType,
            Vital = npc.Vital,
            X = npc.X,
            Y = npc.Y,
            Dir = (byte)npc.Dir,
            AttackTimer = npc.AttackTimer,
            SpawnWait = npc.SpawnWait,
            StunDuration = npc.StunDuration,
            StunTimer = npc.StunTimer,
            SkillBuffer = npc.SkillBuffer,
            SkillBufferTimer = npc.SkillBufferTimer,
            Skill = npc.SkillCd != null ? (int[])npc.SkillCd.Clone() : new int[Core.Globals.Variables.MaxNpcSkills],
            Attacking = npc.Attacking,
        };

        // Pull behavior-related data from NPC template for AI logic
        if (npc.Num >= 0 && npc.Num < Data.Npc.Length)
        {
            entity.Behavior = Data.Npc[npc.Num].Behavior;
            entity.Range = Data.Npc[npc.Num].Range;
            entity.AttackSay = Data.Npc[npc.Num].AttackSay;
            entity.Name = Data.Npc[npc.Num].Name;
        }
        return entity;
    }
}