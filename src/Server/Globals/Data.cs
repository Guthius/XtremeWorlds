using Core.Database;
using static Core.Globals.Type;

namespace Core.Globals;

public static class Data
{
    public static Job[] Job = new Job[Variables.MaxJobs];
    public static Moral[] Moral = new Moral[Variables.MaxMorals];
    public static Item[] Item = new Item[Variables.MaxItems];
    public static Npc[] Npc = new Npc[Variables.MaxNpcs];
    public static Shop[] Shop = new Shop[Variables.MaxShops];
    public static Skill[] Skill = new Skill[Variables.MaxSkills];
    public static MapResource[] MapResource = new MapResource[Variables.MaxResources];
    public static Animation[] Animation = new Animation[Variables.MaxAnimations];
    public static Map[] Map = new Map[Variables.MaxMaps];
    public static MapItem[,] MapItem = new MapItem[Variables.MaxMaps, Variables.MaxMapItems];
    public static MapData[] MapNpc = new MapData[Variables.MaxMaps];
    public static Bank[] Bank = new Bank[Variables.MaxPlayers];
    public static TempPlayer[] TempPlayer = new TempPlayer[Variables.MaxPlayers];
    public static Account[] Account = new Account[Variables.MaxPlayers];
    public static Player[] Player = new Player[Variables.MaxPlayers];
    public static Projectile[] Projectile = new Projectile[Variables.MaxProjectiles];
    public static MapProjectile[,] MapProjectile = new MapProjectile[Variables.MaxMaps, Variables.MaxProjectiles];
    public static Party[] Party = new Party[Variables.MaxParty];
    public static Resource[] Resource = new Resource[Variables.MaxResources];
    public static CharacterNameList? Char;
    public static Type.Script Script = new();
}