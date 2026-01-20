using static Core.Globals.Type;

namespace Core.Globals;

public static class Data
{
    public static Tile[,]? TempTile;
    public static TempPlayer[] TempPlayer = new TempPlayer[Core.Globals.Variables.MaxPlayers];
    public static MapProjectile[,] MapProjectile = new MapProjectile[Core.Globals.Variables.MaxMaps, Core.Globals.Variables.MaxProjectiles];
    public static Item[] TradeYourOffer = new Item[Core.Globals.Variables.MaxInventory];
    public static Item[] TradeTheirOffer = new Item[Core.Globals.Variables.MaxInventory];
    public static Party[] Party = new Party[Core.Globals.Variables.MaxParty];
    public static Party MyParty;
    public static ChatBubble[] ChatBubble = new ChatBubble[byte.MaxValue];
    public static Script Script = new();

    public static Quest[] Quests = new Quest[Core.Globals.Variables.MaxQuests];
    public static Event[] Events = new Event[Core.Globals.Variables.MaxEvents];
    public static Guild[] Guilds = new Guild[Core.Globals.Variables.MaxGuilds];
    public static Weather Weather = new();

    public static ActionMessage[] ActionMessage = new ActionMessage[byte.MaxValue];
    public static Blood[] Blood = new Blood[byte.MaxValue];
    public static Chat[] Chat = new Chat[Variables.ChatLines];
    public static Tile[,]? MapTile;
    public static TileHistory[]? TileHistory;
    public static Autotile[,]? Autotile;
    public static MapEvent[]? MapEvents;

    static Data()
    {
        // Ensure map projectile slots start in a known "empty" state.
        // Index==0 can be a valid projectile definition, so we use -1 as the sentinel.
        for (var map = 0; map < Core.Globals.Variables.MaxMaps; map++)
        {
            for (var i = 0; i < Core.Globals.Variables.MaxProjectiles; i++)
            {
                ref var mp = ref MapProjectile[map, i];
                mp.Index = -1;
                mp.Owner = -1;
                mp.OwnerType = 0;
                mp.SkillId = -1;
                mp.Timer = 0;
                mp.TravelTime = 0;
                mp.Range = 0;
                mp.FreeAim = 0;
                mp.Vx = 0;
                mp.Vy = 0;
                mp.AccX = 0;
                mp.AccY = 0;
                mp.DestX = 0;
                mp.DestY = 0;
                mp.X = 0;
                mp.Y = 0;
                mp.Dir = 0;
            }
        }
    }
}