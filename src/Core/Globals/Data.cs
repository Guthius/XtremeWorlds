using static Core.Globals.Type;

namespace Core.Globals;

public static class Data
{
    public static Tile[,]? TempTile;
    public static TempPlayer[] TempPlayer = new TempPlayer[Variables.MaxPlayers];
    public static MapProjectile[,] MapProjectile = new MapProjectile[Variables.MaxMaps, Variables.MaxProjectiles];
    public static Item[] TradeYourOffer = new Item[Variables.MaxInventory];
    public static Item[] TradeTheirOffer = new Item[Variables.MaxInventory];
    public static Party[] Party = new Party[Variables.MaxParty];
    public static Party MyParty;
    public static ChatBubble[] ChatBubble = new ChatBubble[byte.MaxValue];
    public static Script Script = new();

    public static Quest[] Quests = new Quest[Variables.MaxQuests];
    public static Event[] Events = new Event[Variables.MaxEvents];
    public static Guild[] Guilds = new Guild[Variables.MaxGuilds];
    public static Weather Weather = new();

    public static ActionMessage[] ActionMessage = new ActionMessage[byte.MaxValue];
    public static Blood[] Blood = new Blood[byte.MaxValue];
    public static Chat[] Chat = new Chat[Variables.ChatLines];
    public static Tile[,]? MapTile;
    public static TileHistory[]? TileHistory;
    public static Autotile[,]? Autotile;
    public static MapEvent[]? MapEvents;
}