using Core;
using Core.Globals;
using Core.Interfaces;
using Core.Net;
using Core.Objects;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using Server.Game;
using Server.Game.Net;
using Server.Net;
using XtremeWorlds.Server.Configuration;
using static Core.Globals.Commands;
using static Core.Net.Packets;
using static Server.Globals.Commands;

namespace Server;

public class Player : PlayerBase
{
    private static readonly int[] LastPlayerXYBroadcastMs = new int[Core.Globals.Variables.MaxPlayers];

    private static void ResetDisconnectedPlayerSlot(int playerId)
    {
        // Clear persistent player data stored in the global list.
        // Important: do not Remove() here, indices are player ids.
        if (PlayerBase.Instance.Count > playerId)
        {
            PlayerBase.Instance[playerId] = new PlayerBase();
        }

        // Reset transient per-connection/player runtime state.
        if (Data.TempPlayer is null || playerId >= Data.TempPlayer.Length)
        {
            return;
        }

        var tp = new Core.Globals.Type.TempPlayer
        {
            InGame = false,
            GettingMap = false,

            Target = -1,
            TargetType = 0,

            PartyInvite = -1,
            InParty = -1,

            SkillBuffer = -1,
            InShop = -1,
            InTrade = 0,

            Editor = EditorType.None,

            MoveSpeedMultiplier = 1.0f,
            MoveSpeedMultiplierTimer = 0,

            SkillCd = new int[Core.Globals.Variables.MaxPlayerSkills],
            TradeOffer = new Core.Globals.Type.Item[Core.Globals.Variables.MaxInventory],
            EventProcessing = new Core.Globals.Type.EventProcessing[1],
            EventMap = new Core.Globals.Type.EventMap { CurrentEvents = 0, EventPages = new Core.Globals.Type.MapEvent[1] },
        };

        Data.TempPlayer[playerId] = tp;
    }

    public static void OnLevel(int playerId)
    {
        try
        {
            Script.Instance?.OnLevel(playerId);
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnLevel));
        }
    }

    public static void OnAdd(GameSession session)
    {
        // Set the flag so we know the person is in the game
        Data.TempPlayer[session.Id].InGame = true;

        // Default temporary move speed modifier.
        Data.TempPlayer[session.Id].MoveSpeedMultiplier = 1.0f;
        Data.TempPlayer[session.Id].MoveSpeedMultiplierTimer = 0;

        // Send an ok to client to start receiving in game data
        NetworkSend.LoginOk(session.Id);

        OnJoin(session.Id);

        General.Logger.LogInformation("{AccountName} | {PlayerName} has began playing {GameName}",
            GetAccountLogin(session.Id), GetPlayerName(session.Id),
            SettingsManager.Instance.GameName);
    }

    public static void OnWarp(int playerId, int map, int x, int y, int dir, bool send = false)
    {
        if (!NetworkConfig.IsPlaying(playerId))
        {
            return;
        }

        if (map <= 0 || map >= Core.Globals.Variables.MaxMaps)
        {
            return;
        }

        if (Data.TempPlayer[playerId].GettingMap)
        {
            return;
        }

        // Map data is stored in a list; MaxMaps is not a guarantee that the list is populated.
        // Note: List indices are 0..Count-1, and map ids here are expected to be > 0.
        var mapCount = Server.Map.Instance.Count;
        if (map <= 0 || map >= mapCount)
        {
            General.Logger.LogWarning("OnWarp rejected: invalid map index {Map} (Map.Instance.Count={Count}) for player {PlayerId}", map, mapCount, playerId);
            return;
        }

        var mapData = Server.Map.Instance[map];
        if (mapData.Tile == null)
        {
            General.Logger.LogWarning("OnWarp rejected: map {Map} has no tile data for player {PlayerId}", map, playerId);
            return;
        }
        if (mapData.MaxX <= 0 || mapData.MaxY <= 0)
        {
            General.Logger.LogWarning("OnWarp rejected: map {Map} has invalid bounds MaxX={MaxX} MaxY={MaxY}", map, mapData.MaxX, mapData.MaxY);
            return;
        }

        x = Math.Clamp(x, 0, Math.Max(0, mapData.MaxX - 1)) * 32;
        y = Math.Clamp(y, 0, Math.Max(0, mapData.MaxY - 1)) * 32;

        // Save old map to send erase player data to
        var oldMap = GetMap(playerId);
        var changingMaps = oldMap != map;

        // Only reset event state when changing maps (or explicitly resending).
        // OnWarp is also used as a corrective "snap"; clearing events in that case causes
        // CurrentEvents to drop back to 0 after events have already been spawned.
        if (changingMaps || send)
        {
            Data.TempPlayer[playerId].EventProcessingCount = 0;
            Data.TempPlayer[playerId].EventMap.CurrentEvents = 0; // Clear events
        }

        Data.TempPlayer[playerId].Target = -1;
        Data.TempPlayer[playerId].TargetType = 0;

        NetworkSend.Target(playerId, 0, 0);
        if (changingMaps)
        {
            try
            {
                Script.Instance?.LeaveMap(playerId, oldMap);
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnWarp));
            }

            NetworkSend.LeaveMap(playerId, oldMap);   
        }

        SetPlayerMap(playerId, map);
        SetPlayerX(playerId, x);
        SetPlayerY(playerId, y);
        SetPlayerDir(playerId, dir);

        NetworkSend.PlayerXY(playerId);

        // Send equipment of all people on new map
        if (GameLogic.GetTotalMapPlayers(map) > 0)
        {
            foreach (var otherPlayerId in PlayerService.Instance.PlayerIds)
            {
                if (GetMap(otherPlayerId) == map)
                {
                    NetworkSend.MapEquipmentTo(otherPlayerId, playerId);
                }
            }
        }

        // Now we check if there were any players left on the map the player just left, and if not stop processing npcs
        if (oldMap >= 0 && oldMap < Core.Globals.Variables.MaxMaps && GameLogic.GetTotalMapPlayers(oldMap) == 0)
        {
            // Regenerate all Npcs' health
            for (var npc = 0; npc < Core.Globals.Variables.MaxMapNpcs; npc++)
            {
                var vitalCount = (int)System.Enum.GetValues(typeof(Vital)).Length;
                for (var i = 0; i < vitalCount; i++)
                {
                    if (MapNpc.Instance[oldMap, npc].Num >= 0)
                    {
                        MapNpc.Instance[oldMap, npc].Vital[i] = GameLogic.GetNpcMaxVital(MapNpc.Instance[oldMap, npc].Num, (Vital)i);
                    }
                }
            }
        }

        if (oldMap != map || send)
        {
            if (Server.Map.Instance[map].Moral < 0 || Server.Map.Instance[map].Moral >= Core.Globals.Variables.MaxMorals)
            {
                Server.Map.Instance[map].Moral = 0;
            }

            Data.TempPlayer[playerId].GettingMap = true;

            NetworkSend.UpdateMoralTo(playerId, Server.Map.Instance[map].Moral);

            var packet = new PacketWriter(12);

            packet.WriteEnum(ServerPackets.SCheckForMap);
            packet.WriteInt32(map);
            packet.WriteInt32(Server.Map.Instance[map].Revision);

            PlayerService.Instance.SendDataTo(playerId, packet.GetBytes());
        }
    }

    public static void OnMove(int playerId, int dir, int movement, bool expectingWarp)
    {
        int x;
        int y;
        var didWarp = false;

        // Heal / Trap tile effect working variables
        int healVital = -1; // -1 means no heal tile encountered; 0=HP,1=MP,2=SP
        int healAmount = 0;
        int trapVital = (int)Core.Globals.Vital.Health; // default trap vital is Health
        int trapAmount = 0;

        if (!NetworkConfig.IsPlaying(playerId))
        {
            return;
        }

        // Check for subscript out of range
        var count = System.Enum.GetValues(typeof(MovementState)).Length;
        var count2 = System.Enum.GetValues(typeof(Direction)).Length;
        if (dir < 0 || dir >= count2 || movement < 0 || movement >= count)
        {
            return;
        }

        // Prevent player from moving if they have casted a skill
        if (Data.TempPlayer[playerId].SkillBuffer >= 0)
        {
            NetworkSend.PlayerXY(playerId);
            return;
        }

        // if stunned, stop them moving
        if (Data.TempPlayer[playerId].StunDuration > 0)
        {
            NetworkSend.PlayerXY(playerId);
            return;
        }

        SetPlayerDir(playerId, dir);
        var moved = false;
        var map = GetMap(playerId);

        // Map data is stored in a list; map id is not a guarantee that the list is populated.
        var mapCount = Server.Map.Instance.Count;
        if (map < 0 || map >= mapCount)
        {
            General.Logger.LogWarning("OnMove rejected: invalid map index {Map} (Map.Instance.Count={Count}) for player {PlayerId}", map, mapCount, playerId);
            NetworkSend.PlayerXY(playerId);
            return;
        }

        var mapData = Server.Map.Instance[map];
        if (mapData.Tile == null || mapData.MaxX <= 0 || mapData.MaxY <= 0)
        {
            General.Logger.LogWarning("OnMove rejected: map {Map} has no tile data or invalid bounds (MaxX={MaxX}, MaxY={MaxY}) for player {PlayerId}", map, mapData.MaxX, mapData.MaxY, playerId);
            NetworkSend.PlayerXY(playerId);
            return;
        }

        var playerX = GetPlayerX(playerId);
        var playerY = GetPlayerY(playerId);
        if (playerX < 0 || playerY < 0 || playerX >= mapData.MaxX || playerY >= mapData.MaxY)
        {
            General.Logger.LogWarning("OnMove rejected: out-of-bounds position x={X}, y={Y} on map {Map} (MaxX={MaxX}, MaxY={MaxY}) for player {PlayerId}", playerX, playerY, map, mapData.MaxX, mapData.MaxY, playerId);
            NetworkSend.PlayerXY(playerId);
            return;
        }

        switch ((Direction) dir)
        {
            case Direction.Up:
                if (GetPlayerY(playerId) > 0)
                {
                    if (IsTileBlocked(playerId, map, GetPlayerX(playerId), GetPlayerY(playerId) - 1, Direction.Up))
                    {
                        NetworkSend.PlayerXY(playerId);
                        return;
                    }

                    SetPlayerY(playerId, GetPlayerRawY(playerId) - 1);
                    moved = true;
                }
                else if (Server.Map.Instance[map].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type != TileType.NoCrossing && Server.Map.Instance[map].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type2 != TileType.NoCrossing)
                {
                    var upMap = Server.Map.Instance[map].Up;
                    if (upMap > 0)
                    {
                        if (upMap < 0 || upMap >= Server.Map.Instance.Count)
                        {
                            General.Logger.LogWarning("OnMove warp rejected: invalid Up map index {WarpMap} from map {Map} for player {PlayerId}", upMap, map, playerId);
                            NetworkSend.PlayerXY(playerId);
                            break;
                        }

                        var newMapY = Server.Map.Instance[upMap].MaxY;
                        OnWarp(playerId, upMap, GetPlayerX(playerId), newMapY, (int)Direction.Up);
                        
                        didWarp = true;
                        moved = true;
                    }
                }

                break;

            case Direction.Down:
                if (GetPlayerY(playerId) < Server.Map.Instance[map].MaxY - 1)
                {
                    if (IsTileBlocked(playerId, map, GetPlayerX(playerId), GetPlayerY(playerId) + 1, Direction.Down))
                    {
                        NetworkSend.PlayerXY(playerId);
                        return;
                    }

                    SetPlayerY(playerId, GetPlayerRawY(playerId) + 1);
                    
                    moved = true;
                }
                else if (Server.Map.Instance[map].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type != TileType.NoCrossing && Server.Map.Instance[map].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type2 != TileType.NoCrossing)
                {
                    if (Server.Map.Instance[map].Down > 0)
                    {
                        OnWarp(playerId, Server.Map.Instance[map].Down, GetPlayerX(playerId), 0, (int)Direction.Down);
                        
                        didWarp = true;
                        moved = true;
                    }
                }

                break;

            case Direction.Left:
                if (GetPlayerX(playerId) > 0)
                {
                    if (IsTileBlocked(playerId, map, GetPlayerX(playerId) - 1, GetPlayerY(playerId), Direction.Left))
                    {
                        NetworkSend.PlayerXY(playerId);
                        return;
                    }

                    SetPlayerX(playerId, GetPlayerRawX(playerId) - 1);
                    
                    moved = true;
                }
                else if (Server.Map.Instance[map].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type != TileType.NoCrossing && Server.Map.Instance[map].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type2 != TileType.NoCrossing)
                {
                    var leftMap = Server.Map.Instance[map].Left;
                    if (leftMap > 0)
                    {
                        if (leftMap < 0 || leftMap >= Server.Map.Instance.Count)
                        {
                            General.Logger.LogWarning("OnMove warp rejected: invalid Left map index {WarpMap} from map {Map} for player {PlayerId}", leftMap, map, playerId);
                            NetworkSend.PlayerXY(playerId);
                            break;
                        }

                        var newMapX = Server.Map.Instance[leftMap].MaxX;

                        OnWarp(playerId, leftMap, newMapX, GetPlayerY(playerId), (int)Direction.Left);

                        didWarp = true;
                        moved = true;
                    }
                }

                break;

            case Direction.Right:
                if (GetPlayerX(playerId) < Server.Map.Instance[map].MaxX - 1)
                {
                    if (IsTileBlocked(playerId, map, GetPlayerX(playerId) + 1, GetPlayerY(playerId), Direction.Right))
                    {
                        NetworkSend.PlayerXY(playerId);
                        return;
                    }

                    SetPlayerX(playerId, GetPlayerRawX(playerId) + 1);
                    
                    moved = true;
                }
                else if (Server.Map.Instance[map].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type != TileType.NoCrossing && Server.Map.Instance[map].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type2 != TileType.NoCrossing)
                {
                    if (Server.Map.Instance[map].Right > 0)
                    {
                        OnWarp(playerId, Server.Map.Instance[map].Right, 0, GetPlayerY(playerId), (int)Direction.Right);
                        
                        didWarp = true;
                        moved = true;
                    }
                }

                break;

            case Direction.UpRight:
                if (GetPlayerY(playerId) > 0 && GetPlayerX(playerId) < Server.Map.Instance[map].MaxX - 1)
                {
                    if (IsTileBlocked(playerId, map, GetPlayerX(playerId) + 1, GetPlayerY(playerId) - 1, Direction.UpRight))
                    {
                        NetworkSend.PlayerXY(playerId);
                        return;
                    }

                    SetPlayerX(playerId, GetPlayerRawX(playerId) + 1);
                    SetPlayerY(playerId, GetPlayerRawY(playerId) - 1);
                    
                    moved = true;
                }

                break;

            case Direction.UpLeft:
                if (GetPlayerY(playerId) > 0 && GetPlayerX(playerId) > 0)
                {
                    if (IsTileBlocked(playerId, map, GetPlayerX(playerId) - 1, GetPlayerY(playerId) - 1, Direction.UpLeft))
                    {
                        NetworkSend.PlayerXY(playerId);
                        return;
                    }

                    SetPlayerX(playerId, GetPlayerRawX(playerId) - 1);
                    SetPlayerY(playerId, GetPlayerRawY(playerId) - 1);
                    
                    moved = true;
                }

                break;

            case Direction.DownRight:
                if (GetPlayerY(playerId) < Server.Map.Instance[map].MaxY - 1 && GetPlayerX(playerId) < Server.Map.Instance[map].MaxX - 1)
                {
                    if (IsTileBlocked(playerId, map, GetPlayerX(playerId) + 1, GetPlayerY(playerId) + 1, Direction.DownRight))
                    {
                        NetworkSend.PlayerXY(playerId);
                        return;
                    }

                    SetPlayerX(playerId, GetPlayerRawX(playerId) + 1);
                    SetPlayerY(playerId, GetPlayerRawY(playerId) + 1);
                    
                    moved = true;
                }

                break;

            case Direction.DownLeft:
                if (GetPlayerY(playerId) < Server.Map.Instance[map].MaxY - 1 && GetPlayerX(playerId) > 0)
                {
                    if (IsTileBlocked(playerId, map, GetPlayerX(playerId) - 1, GetPlayerY(playerId) + 1, Direction.DownLeft))
                    {
                        NetworkSend.PlayerXY(playerId);
                        return;
                    }

                    SetPlayerX(playerId, GetPlayerRawX(playerId) - 1);
                    SetPlayerY(playerId, GetPlayerRawY(playerId) + 1);

                    moved = true;
                }

                break;
        }

        // Re-evaluate current map after potential warp.
        var currentMap = GetMap(playerId);
        if (currentMap >= 0 && currentMap < Server.Map.Instance.Count &&
            Server.Map.Instance[currentMap].Tile != null &&
            GetPlayerX(playerId) >= 0 &&
            GetPlayerY(playerId) >= 0 &&
            GetPlayerX(playerId) < Server.Map.Instance[currentMap].MaxX &&
            GetPlayerY(playerId) < Server.Map.Instance[currentMap].MaxY)
        {
            // Player Touch events: EventPages is 1-based.
            for (var slot = 1; slot <= Data.TempPlayer[playerId].EventMap.CurrentEvents; slot++)
            {
                if (Data.TempPlayer[playerId].EventMap.EventPages == null || slot >= Data.TempPlayer[playerId].EventMap.EventPages.Length)
                    break;

                var mapEventId = Data.TempPlayer[playerId].EventMap.EventPages[slot].EventId;
                if (mapEventId < 0)
                    continue;

                EventLogic.TriggerEvent(playerId, mapEventId, 1, GetPlayerX(playerId), GetPlayerY(playerId));
            }

            ref var tile = ref Server.Map.Instance[currentMap].Tile[GetPlayerX(playerId), GetPlayerY(playerId)];

            map = -1;
            x = 0;
            y = 0;

            // Check to see if the tile is a warp tile, and if so warp them
            if (tile.Type == TileType.Warp)
            {
                map = tile.Data1;
                x = tile.Data2 * 32;
                y = tile.Data3 * 32;
            }

            if (tile.Type2 == TileType.Warp)
            {
                map = tile.Data1_2;
                x = tile.Data2_2;
                y = tile.Data3_2;
            }

            if (map >= 0 && map < Core.Globals.Variables.MaxMaps)
            {
                OnWarp(playerId, map, x, y, (int) Direction.Down);

                didWarp = true;
                moved = true;
            }

            x = -1;
            if (tile.Type == TileType.Shop)
            {
                x = tile.Data1;
            }

            if (tile.Type2 == TileType.Shop)
            {
                x = tile.Data1_2;
            }

            if (x >= 0) // shop exists?
            {
                if (x < Shop.Instance.Count && Shop.Instance[x].Name.Length > 0)
                {
                    NetworkSend.OpenShop(playerId, x);
                    
                    Data.TempPlayer[playerId].InShop = x;
                }
            }

            // Check to see if the tile is a bank, and if so send bank
            if (tile.Type == TileType.Bank || tile.Type2 == TileType.Bank)
            {
                NetworkSend.Bank(playerId);
                
                Data.TempPlayer[playerId].InBank = true;
                
                moved = true;
            }

            // Heal tile(s) processing (Data1/Data1_2 = vital index, Data2/Data2_2 = amount)
            if (tile.Type == TileType.Heal)
            {
                healVital = tile.Data1;
                healAmount = tile.Data2;
            }
            
            if (tile.Type2 == TileType.Heal)
            {
                // If a second-layer heal exists, we override vital with layer2's vital to match editor Behavior and add amounts
                if (healVital < 0) healVital = tile.Data1_2; else healVital = tile.Data1_2; // explicit override
                healAmount += tile.Data2_2;
            }

            if (healVital >= 0 && healAmount > 0 && healVital < System.Enum.GetValues(typeof(Vital)).Length)
            {
                var hv = (Vital)healVital;
                if (GetPlayerVital(playerId, hv) < GetPlayerMaxVital(playerId, hv))
                {
                    int color = hv switch
                    {
                        Core.Globals.Vital.Health => (int)ColorName.BrightGreen,
                        Core.Globals.Vital.Mana => (int)ColorName.BrightBlue,
                        _ => (int)ColorName.Yellow
                    };
                    NetworkSend.ActionMessage(GetMap(playerId), "+" + healAmount, color, (byte)ActionMessageType.Scroll, GetPlayerX(playerId) * 32, GetPlayerY(playerId) * 32, 1);
                    SetPlayerVital(playerId, hv, Math.Min(GetPlayerVital(playerId, hv) + healAmount, GetPlayerMaxVital(playerId, hv)));
                    NetworkSend.PlayerMessage(playerId, "You feel rejuvenating forces coursing through your body.", (int)ColorName.BrightGreen);
                    NetworkSend.Vital(playerId, hv);
                }
                moved = true; // stepping onto a heal tile counts as a valid move
            }

            // Trap tile(s) processing (Data1/Data1_2 = amount, Data2/Data2_2 = vital index)
            if (tile.Type == TileType.Trap)
            {
                trapAmount = tile.Data1;
                if (tile.Data2 > 0) trapVital = tile.Data2;
            }

            if (tile.Type2 == TileType.Trap)
            {
                trapAmount += tile.Data1_2;
                if (tile.Data2_2 > 0) trapVital = tile.Data2_2;
            }

            if (trapAmount > 0)
            {
                var tv = (Vital)trapVital;
                NetworkSend.ActionMessage(GetMap(playerId), "-" + trapAmount, (int)ColorName.BrightRed, (byte)ActionMessageType.Scroll, GetPlayerX(playerId) * 32, GetPlayerY(playerId) * 32, 1);
                if (tv == Core.Globals.Vital.Health && GetPlayerVital(playerId, Core.Globals.Vital.Health) - trapAmount <= 0)
                {
                    OnKill(playerId);
                    Script.Instance?.KillPlayerNoAttacker(playerId, "You've been killed by a trap.");
                }
                else
                {
                    SetPlayerVital(playerId, tv, Math.Max(0, GetPlayerVital(playerId, tv) - trapAmount));
                    NetworkSend.PlayerMessage(playerId, "You've been injured by a trap.", (int)ColorName.BrightRed);
                    NetworkSend.Vital(playerId, tv);
                }
                moved = true;
            }
        }

        // They tried to hack
        if (!moved || (expectingWarp && !didWarp))
        {
            OnWarp(playerId, GetMap(playerId), GetPlayerX(playerId), GetPlayerY(playerId), (byte) Direction.Down);
        }

        var wasMoving = Player.Instance[playerId].IsMoving;
        Player.Instance[playerId].IsMoving = true;

        // Throttle movement broadcasts: clients already have direction/move speed and can smoothly step.
        // Send immediately on movement start, on warp-triggered transitions, periodically, and on tile boundaries.
        var now = General.GetTime();
        var last = (playerId >= 0 && playerId < LastPlayerXYBroadcastMs.Length) ? LastPlayerXYBroadcastMs[playerId] : 0;
        var onTileBoundary = (GetPlayerRawX(playerId) % Constants.TileSize == 0) && (GetPlayerRawY(playerId) % Constants.TileSize == 0);
        var shouldSend = !wasMoving || expectingWarp || didWarp || onTileBoundary || now - last >= 100;
        if (shouldSend)
        {
            if (playerId >= 0 && playerId < LastPlayerXYBroadcastMs.Length)
                LastPlayerXYBroadcastMs[playerId] = now;
            NetworkSend.PlayerXYToMap(playerId);
        }

        try
        {
            Script.Instance?.OnMove(playerId);
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnMove));
        }
    }

    public static bool IsTileBlocked(int playerId, int map, int x, int y, Direction dir)
    {
        try
        {
            if (map < 0 || map >= Server.Map.Instance.Count)
                return true;

            if (x < 0 || y < 0 || x > Server.Map.Instance[map].MaxX || y > Server.Map.Instance[map].MaxY)
                return true;

            var index = Server.Map.Instance[map].Moral;
            var playerBlock = true;
            var npcBlock = true;
            if (index >= 0 && index < Moral.Instance.Count)
            {
                playerBlock = Moral.Instance[index].PlayerBlock;
                npcBlock = Moral.Instance[index].NpcBlock;
            }

            // Destination tile bounds in pixels.
            var tileLeft = x * Constants.TileSize;
            var tileTop = y * Constants.TileSize;
            var tileRight = tileLeft + Constants.TileSize;
            var tileBottom = tileTop + Constants.TileSize;

            if (playerBlock)
            {
                foreach (var otherPlayerId in PlayerService.Instance.PlayerIds)
                {
                    if (otherPlayerId == playerId)
                        continue;

                    if (GetMap(otherPlayerId) != map)
                        continue;

                    // Players are tracked in pixels and can be mid-step/off-grid.
                    // Treat them as blocking if their tile-sized bounds intersect the destination tile.
                    var otherLeft = GetPlayerRawX(otherPlayerId);
                    var otherTop = GetPlayerRawY(otherPlayerId);
                    var otherRight = otherLeft + Constants.TileSize;
                    var otherBottom = otherTop + Constants.TileSize;
                    if (otherLeft < tileRight && otherRight > tileLeft && otherTop < tileBottom && otherBottom > tileTop)
                    {
                        return true;
                    }
                }
            }

            if (npcBlock)
            {
                for (var i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
                {
                    if (MapNpc.Instance[map, i].Num < 0)
                        continue;

                    // NPCs are tracked in pixels and can be mid-step/off-grid.
                    // Treat them as blocking if their tile-sized bounds intersect the destination tile.
                    var npcLeft = MapNpc.Instance[map, i].X;
                    var npcTop = MapNpc.Instance[map, i].Y;
                    var npcRight = npcLeft + Constants.TileSize;
                    var npcBottom = npcTop + Constants.TileSize;
                    if (npcLeft < tileRight && npcRight > tileLeft && npcTop < tileBottom && npcBottom > tileTop)
                    {
                        return true;
                    }
                }
            }

            // Block by events with WalkThrough disabled.
            // Global events (authoritative server-side position)
            if (Event.TempEventMap != null && map >= 0 && map < Event.TempEventMap.Length)
            {
                var globalEvents = Event.TempEventMap[map];
                if (globalEvents.Event != null && globalEvents.EventCount > 0)
                {
                    for (var i = 1; i <= globalEvents.EventCount && i < globalEvents.Event.Length; i++)
                    {
                        var ge = globalEvents.Event[i];
                        if (ge.WalkThrough != 0)
                            continue;

                        // Global events use pixel coordinates and can be mid-step (off the tile grid).
                        // Treat them as blocking if their tile-sized bounds intersect the destination tile.
                        var evLeft = ge.X;
                        var evTop = ge.Y;
                        var evRight = evLeft + Constants.TileSize;
                        var evBottom = evTop + Constants.TileSize;
                        if (evLeft < tileRight && evRight > tileLeft && evTop < tileBottom && evBottom > tileTop)
                            return true;
                    }
                }
            }

            // Player-scoped event pages (local/non-global events, and also globals as the client sees them)
            var eventMap = Data.TempPlayer[playerId].EventMap;
            if (eventMap.CurrentEvents > 0 && eventMap.EventPages != null)
            {
                // EventPages is 1-based in this code-path.
                for (var slot = 1; slot <= eventMap.CurrentEvents && slot <= eventMap.EventPages.Length; slot++)
                {
                    var page = eventMap.EventPages[slot];
                    if (!page.Visible)
                        continue;
                        
                    if (page.WalkThrough != 0)
                        continue;

                    // MapEvent.X/Y are pixel coordinates and may be mid-step.
                    // Treat the event as blocking if its bounds intersect the destination tile.
                    var evLeft = page.X;
                    var evTop = page.Y;
                    var evRight = evLeft + Constants.TileSize;
                    var evBottom = evTop + Constants.TileSize;
                    if (evLeft < tileRight && evRight > tileLeft && evTop < tileBottom && evBottom > tileTop)
                        return true;
                }
            }

            // Check to make sure that the tile is walkable
            if (IsDirBlocked(Server.Map.Instance[map].Tile[x, y].DirBlock, dir))
            {
                return true;
            }

            return Server.Map.Instance[map].Tile[x, y].Type == TileType.Blocked ||
                Server.Map.Instance[map].Tile[x, y].Type2 == TileType.Blocked ||
                Server.Map.Instance[map].Tile[x, y].Type == TileType.Door ||
                Server.Map.Instance[map].Tile[x, y].Type2 == TileType.Door;
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "Error in IsTileBlocked for player {PlayerId} on map {Map} at x={X}, y={Y}", playerId, map, x, y);
            return false;
        }
     }

    public static int HasItem(int playerId, int item)
    {
        if (item < 0 || item > Core.Globals.Variables.MaxItems)
        {
            return 0;
        }

        var totalQuantity = 0;
        for (var i = 0; i < Core.Globals.Variables.MaxInventory; i++)
        {
            if (GetPlayerInv(playerId, i) != item)
            {
                continue;
            }

            if (Item.Instance[item].Type == (byte) ItemCategory.Currency || Item.Instance[item].Stackable == 1)
            {
                totalQuantity += GetPlayerInvValue(playerId, i);
            }
            else
            {
                totalQuantity += 1;
            }
        }

        return totalQuantity;
    }

    public static int FindItemSlot(int playerId, int item)
    {
        if (item < 0 || item >= Core.Globals.Variables.MaxItems)
        {
            return -1;
        }

        for (var i = 0; i < Core.Globals.Variables.MaxInventory; i++)
        {
            if (GetPlayerInv(playerId, i) == item)
            {
                return i;
            }
        }

        return -1;
    }

    public static bool CanPickup(int playerId, int mapItem)
    {
        var map = GetMap(playerId);

        if (!Moral.Instance[Server.Map.Instance[map].Moral].CanPickupItem)
        {
            NetworkSend.PlayerMessage(playerId, "You can't pickup items here!", (int) ColorName.BrightRed);
            return false;
        }

        if (Player.Instance[playerId].Dead)
        {
            NetworkSend.PlayerMessage(playerId, "You can't pick up items while dead.", (int) ColorName.BrightRed);
            return false;
        }

        if (string.IsNullOrEmpty(MapItem.Instance[map, mapItem].PlayerName) ||
            MapItem.Instance[map, mapItem].PlayerName == GetPlayerName(playerId))
        {
            return true;
        }

        return false;
    }

    public static void OnGetItem(int playerId)
        {
            var map = GetMap(playerId);

            for (var i = 0; i < Core.Globals.Variables.MaxMapItems; i++)
            {
                if (MapItem.Instance[map, i].Num < 0 ||
                    MapItem.Instance[map, i].Num >= Core.Globals.Variables.MaxItems)
                {
                    continue;
                }

                if (Math.Floor((double)MapItem.Instance[map, i].X / Constants.TileSize) != GetPlayerX(playerId) || Math.Floor((double)MapItem.Instance[map, i].Y / Constants.TileSize) != GetPlayerY(playerId))
                {
                    continue;
                }

                var slot = Player.FindOpenInvSlot(playerId, MapItem.Instance[map, i].Num);
                if (slot == -1)
                {
                    NetworkSend.PlayerMessage(playerId, "Your inventory is full.", (int)ColorName.BrightRed);
                    break;
                }

                if (!Player.CanPickup(playerId, i))
                {
                    break;
                }

                try
                {
                    Script.Instance?.OnPickup(playerId, map, i, slot);
                }
                catch (Exception ex)
                {
                    General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnGetItem));
                }

                break;
            }
        }


    public static int FindOpenInvSlot(int playerId, int item)
    {
        if (!NetworkConfig.IsPlaying(playerId) || item < 0 || item > Core.Globals.Variables.MaxItems)
        {
            return -1;
        }

        if (Item.Instance[item].Type == (byte) ItemCategory.Currency ||
            Item.Instance[item].Stackable == 1)
        {
            for (var invSlot = 0; invSlot < Core.Globals.Variables.MaxInventory; invSlot++)
            {
                if (GetPlayerInv(playerId, invSlot) == item)
                {
                    return invSlot;
                }
            }
        }

        for (var invSlot = 0; invSlot < Core.Globals.Variables.MaxInventory; invSlot++)
        {
            if (GetPlayerInv(playerId, invSlot) == -1)
            {
                return invSlot;
            }
        }

        return -1;
    }

    public static bool TakeInv(int playerId, int id, int val)
    {
        if (!NetworkConfig.IsPlaying(playerId) || id < 0 || id > Core.Globals.Variables.MaxItems)
        {
            return false;
        }

        var clearInvSlot = false;

        for (var invSlot = 0; invSlot < Core.Globals.Variables.MaxInventory; invSlot++)
        {
            // Check to see if the player has the item
            if (GetPlayerInv(playerId, invSlot) != id)
            {
                continue;
            }

            if (Item.Instance[id].Type == (byte) ItemCategory.Currency ||
                Item.Instance[id].Stackable == 1)
            {
                // Is what we are trying to take away more then what they have?  If so just set it to zero
                if (val >= GetPlayerInvValue(playerId, invSlot))
                {
                    clearInvSlot = true;
                }
                else
                {
                    SetInvValue(playerId, invSlot, GetPlayerInvValue(playerId, invSlot) - val);

                    NetworkSend.InventoryUpdate(playerId, invSlot);
                }
            }
            else
            {
                clearInvSlot = true;
            }

            if (!clearInvSlot)
            {
                continue;
            }

            SetInv(playerId, invSlot, -1);
            SetInvValue(playerId, invSlot, 0);

            NetworkSend.InventoryUpdate(playerId, invSlot);

            return true;
        }

        return false;
    }

    public static bool GiveInv(int playerId, int id, int val, byte bound = 0, bool sendUpdate = true)
    {
        if (!NetworkConfig.IsPlaying(playerId) || id < 0 || id > Core.Globals.Variables.MaxItems)
        {
            return false;
        }

        var slot = FindOpenInvSlot(playerId, id);
        if (slot == -1)
        {
            NetworkSend.PlayerMessage(playerId, "Your inventory is full.", (int) ColorName.BrightRed);
            return false;
        }

        val = Math.Max(val, 1);

        SetInv(playerId, slot, id);
        SetInvValue(playerId, slot, GetPlayerInvValue(playerId, slot) + val);
        Player.Instance[playerId].Inventory[slot].Bound = bound;

        if (sendUpdate)
        {
            NetworkSend.InventoryUpdate(playerId, slot);
        }

        return true;
    }

    public static void OnDrop(int playerId, int invSlot, int amount)
    {
        if (!NetworkConfig.IsPlaying(playerId) || invSlot < 0 || invSlot > Core.Globals.Variables.MaxInventory)
        {
            return;
        }

        // Check the player isn't doing something
        if (Data.TempPlayer[playerId].InBank ||
            Data.TempPlayer[playerId].InShop >= 0 ||
            Data.TempPlayer[playerId].InTrade > 0)
        {
            return;
        }

        if (!Moral.Instance[Server.Map.Instance[GetMap(playerId)].Moral].CanDropItem)
        {
            NetworkSend.PlayerMessage(playerId, "You can't drop items here!", (int) ColorName.BrightRed);
            return;
        }

        if (Player.Instance[playerId].Inventory[invSlot].Bound > 0)
        {
            NetworkSend.PlayerMessage(playerId, "You can't drop soulbound items!", (int) ColorName.BrightRed);
            return;
        }

        var itemId = GetPlayerInv(playerId, invSlot);
        if (itemId < 0 || itemId >= Core.Globals.Variables.MaxItems)
        {
            return;
        }

        var slot = MapItem.FindOpenSlot(GetMap(playerId));
        if (slot != -1)
        {
            var map = GetMap(playerId);
            var item = Item.Instance[itemId];
            ref var mapItem = ref MapItem.Instance[map, slot];

            mapItem.Num = itemId;
            mapItem.X = GetPlayerX(playerId);
            mapItem.Y = GetPlayerY(playerId);
            mapItem.PlayerName = GetPlayerName(playerId);
            mapItem.PlayerTimer = General.GetTime() + Script.Instance?.ItemSpawnTime();
            mapItem.DespawnTimer = General.GetTime() + Script.Instance?.ItemDespawnTime();
            mapItem.CanDespawn = true;

            try
            {
                Script.Instance?.OnDrop(playerId, slot, invSlot, amount, map, item, itemId);
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnDrop));
            }
        }
        else
        {
            NetworkSend.PlayerMessage(playerId, "Too many items already on the ground.", (int) ColorName.Yellow);
        }
    }

    public static bool TakeInvSlot(int playerId, int invSlot, int val)
    {
        var takeInvSlot = false;

        if (!NetworkConfig.IsPlaying(playerId) || invSlot < 0 || invSlot > Core.Globals.Variables.MaxInventory)
        {
            return false;
        }

        var item = GetPlayerInv(playerId, invSlot);

        if (Item.Instance[item].Type == (byte) ItemCategory.Currency ||
            Item.Instance[item].Stackable == 1)
        {
            // Is what we are trying to take away more then what they have?  If so just set it to zero
            if (val >= GetPlayerInvValue(playerId, invSlot))
            {
                takeInvSlot = true;
            }
            else
            {
                SetInvValue(playerId, invSlot, GetPlayerInvValue(playerId, invSlot) - val);
            }
        }
        else
        {
            takeInvSlot = true;
        }

        if (!takeInvSlot)
        {
            return false;
        }

        SetInv(playerId, invSlot, -1);
        SetInvValue(playerId, invSlot, 0);

        return true;
    }

    public static bool IsUsable(int playerId, int itemNum)
    {
        if (Server.Map.Instance[GetMap(playerId)].Moral >= 0)
        {
            if (!Moral.Instance[Server.Map.Instance[GetMap(playerId)].Moral].CanUseItem)
            {
                NetworkSend.PlayerMessage(playerId, "You can't use items with this moral!", (int) ColorName.BrightRed);
                return false;
            }
        }

        var stats = Enum.GetValues<Stat>();
        foreach (var stat in stats)
        {
            if (GetPlayerStat(playerId, stat) >= Item.Instance[itemNum].StatReq[(int) stat])
            {
                continue;
            }

            NetworkSend.PlayerMessage(playerId, "You do not meet the stat requirements to use this item.", (int) ColorName.BrightRed);
            return false;
        }

        if (Item.Instance[itemNum].LevelReq > GetPlayerLevel(playerId))
        {
            NetworkSend.PlayerMessage(playerId, "You do not meet the level requirements to use this item.", (int) ColorName.BrightRed);
            return false;
        }

        if (Item.Instance[itemNum].JobReq != -1 && Item.Instance[itemNum].JobReq != GetPlayerJob(playerId))
        {
            NetworkSend.PlayerMessage(playerId, "You do not meet the job requirements to use this item.", (int) ColorName.BrightRed);
            return false;
        }

        if (GetPlayerAccess(playerId) < Item.Instance[itemNum].AccessReq)
        {
            NetworkSend.PlayerMessage(playerId, "You do not meet the access requirement to equip this item.", (int) ColorName.BrightRed);
            return false;
        }

        if (!Data.TempPlayer[playerId].InBank && Data.TempPlayer[playerId].InShop < 0 && Data.TempPlayer[playerId].InTrade <= 0)
        {
            return true;
        }

        NetworkSend.PlayerMessage(playerId, "You can't use items while in a bank, shop, or trade!", (int) ColorName.BrightRed);
        return false;
    }

    public static void UseItem(int playerId, int invSlot)
    {
        if (invSlot < 0 || invSlot >= Core.Globals.Variables.MaxInventory)
        {
            return;
        }

        var item = GetPlayerInv(playerId, invSlot);
        if (item < 0 || item >= Core.Globals.Variables.MaxItems)
        {
            return;
        }

        if (!IsUsable(playerId, item))
        {
            return;
        }

        try
        {
            Script.Instance?.OnUse(playerId, item, invSlot);
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(UseItem));
        }
    }

    public static void PlayerSwitchInvSlots(int playerId, int oldSlot, int newSlot)
    {
        if (oldSlot == -1 || newSlot == -1)
        {
            return;
        }

        var oldInv = GetPlayerInv(playerId, oldSlot);
        var oldValue = GetPlayerInvValue(playerId, oldSlot);
        var newInv = GetPlayerInv(playerId, newSlot);
        var newValue = GetPlayerInvValue(playerId, newSlot);
        var oldBound = Player.Instance[playerId].Inventory[oldSlot].Bound;
        var newBound = Player.Instance[playerId].Inventory[newSlot].Bound;

        if (newInv >= 0)
        {
            if (oldInv == newInv & Item.Instance[newInv].Stackable == 1) // Same item, if we can stack it, lets do that :P
            {
                SetInv(playerId, newSlot, newInv);
                SetInvValue(playerId, newSlot, oldValue + newValue);
                SetInv(playerId, oldSlot, 0);
                SetInvValue(playerId, oldSlot, 0);
                Player.Instance[playerId].Inventory[oldSlot].Bound = 0;

                if (oldBound > newBound)
                {
                    Player.Instance[playerId].Inventory[newSlot].Bound = oldBound;
                }
            }
            else
            {
                SetInv(playerId, newSlot, oldInv);
                SetInvValue(playerId, newSlot, oldValue);
                SetInv(playerId, oldSlot, newInv);
                SetInvValue(playerId, oldSlot, newValue);
                Player.Instance[playerId].Inventory[oldSlot].Bound = newBound;
                Player.Instance[playerId].Inventory[newSlot].Bound = oldBound;
            }
        }
        else
        {
            SetInv(playerId, newSlot, oldInv);
            SetInvValue(playerId, newSlot, oldValue);
            SetInv(playerId, oldSlot, newInv);
            SetInvValue(playerId, oldSlot, newValue);
            Player.Instance[playerId].Inventory[oldSlot].Bound = newBound;
            Player.Instance[playerId].Inventory[newSlot].Bound = oldBound;
        }

        NetworkSend.Inventory(playerId);
    }

    public static void PlayerSwitchSkillSlots(int playerId, int oldSlot, int newSlot)
    {
        if (oldSlot == -1 || newSlot == -1)
        {
            return;
        }

        var oldSkill = GetPlayerSkill(playerId, oldSlot);
        var oldValue = GetPlayerSkillCd(playerId, oldSlot);
        var newSkill = GetPlayerSkill(playerId, newSlot);
        var newValue = GetPlayerSkillCd(playerId, newSlot);

        if (newSkill >= 0)
        {
            if (oldSkill == newSkill & Item.Instance[newSkill].Stackable == 1) // Same item, if we can stack it, lets do that :P
            {
                SetSkill(playerId, newSlot, newSkill);
                SetPlayerSkillCd(playerId, newSlot, newValue);
                SetSkill(playerId, oldSlot, 0);
                SetPlayerSkillCd(playerId, oldSlot, 0);
            }
            else
            {
                SetSkill(playerId, newSlot, oldSkill);
                SetPlayerSkillCd(playerId, newSlot, oldValue);
                SetSkill(playerId, oldSlot, newSkill);
                SetPlayerSkillCd(playerId, oldSlot, newValue);
            }
        }
        else
        {
            SetSkill(playerId, newSlot, oldSkill);
            SetPlayerSkillCd(playerId, newSlot, oldValue);
            SetSkill(playerId, oldSlot, newSkill);
            SetPlayerSkillCd(playerId, oldSlot, newValue);
        }

        NetworkSend.PlayerSkills(playerId);
    }

    public static void OnCheckEquipment(int playerId)
    {
        var equipments = Enum.GetValues<Equipment>();

        foreach (var equipment in equipments)
        {
            var item = GetPlayerPaperdoll(playerId, equipment);
            if (item < 0)
            {
                SetPlayerPaperdoll(playerId, -1, equipment);
                continue;
            }

            if (Item.Instance[item].SubType != (byte) equipment)
            {
                SetPlayerPaperdoll(playerId, -1, equipment);
            }
        }
    }

    public static void RemoveEquipment(int playerId, int eqSlot, int invSlot)
    {
        var eqCount = Enum.GetNames<Equipment>().Length;
        if (eqSlot < 0 || eqSlot > eqCount)
        {
            return;
        }

        var item = GetPlayerPaperdoll(playerId, (Equipment) eqSlot);
        if (item < 0 || item >= Core.Globals.Variables.MaxItems)
        {
            return;
        }

        if (GetPlayerPaperdoll(playerId, (Equipment)eqSlot) < 0 || GetPlayerPaperdoll(playerId, (Equipment)eqSlot) > Core.Globals.Variables.MaxItems)
            return;

        if (FindOpenInvSlot(playerId, item) >= 0)
        {
            try
            {
                Script.Instance?.OnUnEquip(playerId, item, eqSlot, invSlot);
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(RemoveEquipment));
            }
        }
        else
        {
            NetworkSend.PlayerMessage(playerId, "Your inventory is full.", (int)ColorName.BrightRed);
        }
    }

    public static void OnJoin(int playerId)
    {
        try
        {
            Script.Instance?.OnJoin(playerId);
            General.UpdateCaption();
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnJoin));
        }
    }

    public static async System.Threading.Tasks.Task OnExit(int playerId)
    {
        // Capture map before any cleanup resets player state.
        var oldMap = 0;
        try
        {
            oldMap = GetMap(playerId);
        }
        catch
        {
            oldMap = 0;
        }

        try
        {
            General.Logger.LogInformation("{AccountName} | {PlayerName} has stopped playing {GameName}",
                GetAccountLogin(playerId), GetPlayerName(playerId),
                SettingsManager.Instance.GameName);

            try
            {
                Script.Instance?.OnLeave(playerId);
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnExit));
            }

            if (Data.TempPlayer[playerId].InGame)
            {
                await Account.OnSave(playerId);
            }
        }
        catch (Exception ex)
        {
            // Save failures or unexpected errors should not prevent cleanup.
            General.Logger.LogError(ex, "Unhandled error during OnExit for playerId={PlayerId}", playerId);
        }
        finally
        {
            // If NPCs were targeting this player, clear their targets immediately so they can retarget.
            try
            {
                if (oldMap > 0 && oldMap < Core.Globals.Variables.MaxMaps)
                {
                    for (var i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
                    {
                        ref var npc = ref Server.MapNpc.Instance[oldMap, i];
                        if (npc.TargetType == (byte)TargetType.Player && npc.Target == playerId)
                        {
                            npc.TargetType = 0;
                            npc.Target = -1;
                            npc.Attacking = 0;
                            npc.AttackTimer = 0;
                            npc.SkillBuffer = -1;
                            npc.SkillBufferTimer = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "Error clearing NPC targets for exiting playerId={PlayerId}", playerId);
            }

            try
            {
                Account.OnClear(playerId);
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "Error clearing account state during OnExit for playerId={PlayerId}", playerId);
            }

            try
            {
                PlayerService.Instance.RemovePlayer(playerId);
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "Error removing player service entry during OnExit for playerId={PlayerId}", playerId);
            }

            try
            {
                Data.TempPlayer[playerId].InGame = false;
            }
            catch
            {
                // ignore
            }

            try
            {
                ResetDisconnectedPlayerSlot(playerId);
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "Error resetting disconnected player slot for playerId={PlayerId}", playerId);
            }

            General.UpdateCaption();
        }
    }

    public static int OnKill(int playerId)
    {
        try
        {
            return Script.Instance?.OnKill(playerId);
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnKill));
        }

        return 0;
    }

    public static void GiveBank(int playerId, int invSlot, int amount)
    {
        if (invSlot < 0 || invSlot >= Core.Globals.Variables.MaxInventory)
        {
            return;
        }

        amount = Math.Max(amount, 0);
        if (GetPlayerInvValue(playerId, invSlot) < amount && GetPlayerInv(playerId, invSlot) == 0)
        {
            return;
        }

        var bankSlot = FindOpenbankSlot(playerId, GetPlayerInv(playerId, invSlot));
        if (bankSlot == -1)
        {
            return;
        }

        var itemNum = GetPlayerInv(playerId, invSlot);
        var bound = Player.Instance[playerId].Inventory[invSlot].Bound;

        Bank.Instance[playerId].Item[bankSlot].Bound = bound;

        if (Item.Instance[GetPlayerInv(playerId, invSlot)].Type == (byte)ItemCategory.Currency ||
            Item.Instance[GetPlayerInv(playerId, invSlot)].Stackable == 1)
        {
            if (GetBank(playerId, bankSlot) == GetPlayerInv(playerId, invSlot))
            {
                SetBankValue(playerId, bankSlot, GetBankValue(playerId, bankSlot) + amount);

                TakeInv(playerId, GetPlayerInv(playerId, invSlot), amount);
            }
            else
            {
                SetBank(playerId, bankSlot, GetPlayerInv(playerId, invSlot));
                SetBankValue(playerId, bankSlot, amount);

                TakeInv(playerId, GetPlayerInv(playerId, invSlot), amount);
            }
        }
        else if (GetBank(playerId, bankSlot) == GetPlayerInv(playerId, invSlot))
        {
            SetBankValue(playerId, bankSlot, GetBankValue(playerId, bankSlot) + 1);

            TakeInv(playerId, GetPlayerInv(playerId, invSlot), 0);
        }
        else
        {
            SetBank(playerId, bankSlot, itemNum);
            SetBankValue(playerId, bankSlot, 1);

            TakeInv(playerId, GetPlayerInv(playerId, invSlot), 0);
        }

        NetworkSend.Bank(playerId);
    }

    public static int GetBank(int playerId, int bankSlot)
    {
        return Bank.Instance[playerId].Item[bankSlot].Num;
    }

    public static void SetBank(int playerId, int bankSlot, int itemNum)
    {
        byte slot = Data.TempPlayer[playerId].Slot;
        Account.Instance[playerId].Bank[slot].Item[bankSlot].Num = itemNum;
    }

    public static int GetBankValue(int playerId, int bankSlot)
    {
        byte slot = Data.TempPlayer[playerId].Slot;
        return Account.Instance[playerId].Bank[slot].Item[bankSlot].Value;
    }

    public static void SetBankValue(int playerId, int bankSlot, int value)
    {
        byte slot = Data.TempPlayer[playerId].Slot;
        Account.Instance[playerId].Bank[slot].Item[bankSlot].Value = value;
    }

    public static int FindOpenbankSlot(int playerId, int item)
    {
        if (!NetworkConfig.IsPlaying(playerId) || item < 0 || item >= Core.Globals.Variables.MaxItems)
        {
            return -1;
        }

        if (Item.Instance[item].Type == (byte) ItemCategory.Currency ||
            Item.Instance[item].Stackable == 1)
        {
            for (var bankSlot = 0; bankSlot < Core.Globals.Variables.MaxBank; bankSlot++)
            {
                if (GetBank(playerId, bankSlot) == item)
                {
                    return bankSlot;
                }
            }
        }

        for (var bankSlot = 0; bankSlot < Core.Globals.Variables.MaxBank; bankSlot++)
        {
            if (GetBank(playerId, bankSlot) == -1)
            {
                return bankSlot;
            }
        }

        return -1;
    }

    public static void TakeBank(int playerId, int bankSlot, int amount)
    {
        if (bankSlot < 0 || bankSlot >= Core.Globals.Variables.MaxBank)
        {
            return;
        }

        amount = Math.Max(amount, 0);
        if (GetBankValue(playerId, bankSlot) < amount)
        {
            return;
        }

        var invSlot = FindOpenInvSlot(playerId, GetBank(playerId, bankSlot));
        var bound =  Bank.Instance[playerId].Item[bankSlot].Bound;

        if (invSlot >= 0)
        {
            if (Item.Instance[GetBank(playerId, bankSlot)].Type == (byte)ItemCategory.Currency ||
                Item.Instance[GetBank(playerId, bankSlot)].Stackable == 1)
            {
                GiveInv(playerId, GetBank(playerId, bankSlot), amount, bound);
                SetBankValue(playerId, bankSlot, GetBankValue(playerId, bankSlot) - amount);

                if (GetBankValue(playerId, bankSlot) < 0)
                {
                    SetBank(playerId, bankSlot, 0);
                    SetBankValue(playerId, bankSlot, 0);
                    Bank.Instance[playerId].Item[bankSlot].Bound = 0;
                }
            }
            else if (GetBank(playerId, bankSlot) == GetPlayerInv(playerId, invSlot))
            {
                if (GetBankValue(playerId, bankSlot) > 1)
                {
                    GiveInv(playerId, GetBank(playerId, bankSlot), bound);
                    SetBankValue(playerId, bankSlot, GetBankValue(playerId, bankSlot) - 1);
                }
            }
            else
            {
                GiveInv(playerId, GetBank(playerId, bankSlot), bound);
                SetBank(playerId, bankSlot, -1);
                SetBankValue(playerId, bankSlot, 0);
            }
        }

        NetworkSend.Bank(playerId);
    }

    public static void PlayerSwitchBankSlots(int playerId, int oldSlot, int newSlot)
    {
        if (oldSlot == -1 | newSlot == -1)
        {
            return;
        }

        var oldNum = GetBank(playerId, oldSlot);
        var oldValue = GetBankValue(playerId, oldSlot);
        var newNum = GetBank(playerId, newSlot);
        var newValue = GetBankValue(playerId, newSlot);
        var oldBound = Bank.Instance[playerId].Item[oldSlot].Bound;
        var newBound = Bank.Instance[playerId].Item[newSlot].Bound;

        SetBank(playerId, newSlot, oldNum);
        SetBankValue(playerId, newSlot, oldValue);
        Bank.Instance[playerId].Item[newSlot].Bound = oldBound;

        SetBank(playerId, oldSlot, newNum);
        SetBankValue(playerId, oldSlot, newValue);
        Bank.Instance[playerId].Item[oldSlot].Bound = newBound;

        NetworkSend.Bank(playerId);
    }
}