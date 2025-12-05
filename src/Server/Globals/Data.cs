using XtremeWorlds.Server.Database;
using static Core.Globals.Type;

namespace Core.Globals;

public static class Data
{
    public static Job[] Job { get; } = new Job[Variables.MaxJobs];
    public static Moral[] Moral { get; } = new Moral[Variables.MaxMorals];
    public static Item[] Item { get; } = new Item[Variables.MaxItems];
    public static Npc[] Npc { get; } = new Npc[Variables.MaxNpcs];
    public static Shop[] Shop { get; } = new Shop[Variables.MaxShops];
    public static Skill[] Skill { get; } = new Skill[Variables.MaxSkills];
    public static MapResource[] MapResource { get; } = new MapResource[Variables.MaxResources];
    public static Animation[] Animation { get; } = new Animation[Variables.MaxAnimations];
    public static Map[] Map { get; } = new Map[Variables.MaxMaps];
    public static MapItem[,] MapItem { get; } = new MapItem[Variables.MaxMaps, Variables.MaxMapItems];
    public static MapData[] MapNpc { get; } = new MapData[Variables.MaxMaps];
    public static Bank[] Bank { get; } = new Bank[Variables.MaxPlayers];
    public static TempPlayer[] TempPlayer { get; } = new TempPlayer[Variables.MaxPlayers];
    public static Account[] Account { get; } = new Account[Variables.MaxPlayers];
    public static Player[] Player { get; } = new Player[Variables.MaxPlayers];
    public static Projectile[] Projectile { get; } = new Projectile[Variables.MaxProjectiles];
    public static MapProjectile[,] MapProjectile { get; } = new MapProjectile[Variables.MaxMaps, Variables.MaxProjectiles];
    public static Party[] Party { get; } = new Party[Variables.MaxParty];
    public static Resource[] Resource { get; } = new Resource[Variables.MaxResources];
    public static CharacterNameList? Char { get; set; }
    public static Type.Script Script { get; } = new();
}