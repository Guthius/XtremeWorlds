using static Core.Globals.Type;

namespace Core.Globals;

public static class Data
{
    public static Moral[] Moral = new Moral[Variables.MaxMorals];
    public static Npc[] Npc = new Npc[Variables.MaxNpcs];
    public static Shop[] Shop = new Shop[Variables.MaxShops];
    public static Skill[] Skill = new Skill[Variables.MaxSkills];
    public static MapResource[] MapResource = new MapResource[Variables.MaxResources];
    public static MapResourceCache[] MyMapResource = new MapResourceCache[Variables.MaxResources];
    public static Map[] Map = new Map[Variables.MaxMaps];
    public static Map MyMap;
    public static Tile[,]? TempTile;
    public static MapItem[,] MapItem = new MapItem[Variables.MaxMaps, Variables.MaxMapItems];
    public static MapItem[] MyMapItem = new MapItem[Variables.MaxMapItems];
    public static MapData[] MapNpc = new MapData[Variables.MaxMaps];
    public static MapNpc[] MyMapNpc = new MapNpc[Variables.MaxMapNpcs];
    public static Bank[] Bank = new Bank[Variables.MaxPlayers];
    public static TempPlayer[] TempPlayer = new TempPlayer[Variables.MaxPlayers];
    public static Account[] Account = new Account[Variables.MaxPlayers];
    public static Player[] Player = new Player[Variables.MaxPlayers];
    public static Projectile[] Projectile = new Projectile[Variables.MaxProjectiles];
    public static MapProjectile[,] MapProjectile = new MapProjectile[Variables.MaxMaps, Variables.MaxProjectiles];
    public static PlayerInv[] TradeYourOffer = new PlayerInv[Variables.MaxInv];
    public static PlayerInv[] TradeTheirOffer = new PlayerInv[Variables.MaxInv];
    public static Party[] Party = new Party[Variables.MaxParty];
    public static Party MyParty;
    public static ChatBubble[] ChatBubble = new ChatBubble[byte.MaxValue];
    public static Script Script = new();

    public static Quest[] Quests = new Quest[Variables.MaxQuests];
    public static Event[] Events = new Event[Variables.MaxEvents];
    public static Guild[] Guilds = new Guild[Variables.MaxGuilds];
    public static Weather Weather = new();

    public static ActionMsg[] ActionMsg = new ActionMsg[byte.MaxValue];
    public static Blood[] Blood = new Blood[byte.MaxValue];
    public static Chat[] Chat = new Chat[Variables.ChatLines];
    public static Tile[,]? MapTile;
    public static TileHistory[]? TileHistory;
    public static Autotile[,]? Autotile;
    public static MapEvent[]? MapEvents;
}