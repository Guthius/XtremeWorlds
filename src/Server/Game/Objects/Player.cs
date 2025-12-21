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

        // Send an ok to client to start receiving in game data
        NetworkSend.SendLoginOk(session.Id);

        OnJoin(session.Id);

        General.Logger.LogInformation("{AccountName} | {PlayerName} has began playing {GameName}",
            GetAccountLogin(session.Id), GetPlayerName(session.Id),
            SettingsManager.Instance.GameName);
    }

    public static void OnWarp(int playerId, int map, int x, int y, int dir, bool send = false)
    {
        if (!NetworkConfig.IsPlaying(playerId) || map <= 0 || map >= Core.Globals.Variables.MaxMaps || Data.TempPlayer[playerId].GettingMap == true || map < 0 || map >= Core.Globals.Variables.MaxMaps)
        {
            return;
        }

        x = Math.Clamp(x, 0, Server.Map.Instance[map].MaxX) * 32;
        y = Math.Clamp(y, 0, Server.Map.Instance[map].MaxY) * 32;

        Data.TempPlayer[playerId].EventProcessingCount = 0;
        Data.TempPlayer[playerId].EventMap.CurrentEvents = 0; // Clear events
        Data.TempPlayer[playerId].Target = -1;
        Data.TempPlayer[playerId].TargetType = 0;

        NetworkSend.SendTarget(playerId, 0, 0);

        // Save old map to send erase player data to
        var oldMapNum = GetPlayerMap(playerId);
        if (oldMapNum != map)
        {
            try
            {
                Script.Instance?.LeaveMap(playerId, oldMapNum);
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnWarp));
            }

            NetworkSend.SendLeaveMap(playerId, oldMapNum);   
        }

        SetPlayerMap(playerId, map);
        SetPlayerX(playerId, x);
        SetPlayerY(playerId, y);
        SetPlayerDir(playerId, dir);

        NetworkSend.SendPlayerXY(playerId);

        // Send equipment of all people on new map
        if (GameLogic.GetTotalMapPlayers(map) > 0)
        {
            foreach (var otherPlayerId in PlayerService.Instance.PlayerIds)
            {
                if (GetPlayerMap(otherPlayerId) == map)
                {
                    NetworkSend.SendMapEquipmentTo(otherPlayerId, playerId);
                }
            }
        }

        // Now we check if there were any players left on the map the player just left, and if not stop processing npcs
        if (GameLogic.GetTotalMapPlayers(oldMapNum) == 0)
        {
            // Regenerate all Npcs' health
            for (var mapNpcNum = 0; mapNpcNum < Core.Globals.Variables.MaxMapNpcs; mapNpcNum++)
            {
                var vitalCount = (int)System.Enum.GetValues(typeof(Vital)).Length;
                for (var i = 0; i < vitalCount; i++)
                {
                    if (MapNpc.Instance[oldMapNum, mapNpcNum].Num >= 0)
                    {
                        MapNpc.Instance[oldMapNum, mapNpcNum].Vital[i] = GameLogic.GetNpcMaxVital(MapNpc.Instance[oldMapNum, mapNpcNum].Num, (Vital)i);
                    }
                }
            }
        }

        if (oldMapNum != map || send)
        {
            if (Server.Map.Instance[map].Moral < 0 || Server.Map.Instance[map].Moral >= Core.Globals.Variables.MaxMorals)
            {
                Server.Map.Instance[map].Moral = 0;
            }

            Data.TempPlayer[playerId].GettingMap = true;

            NetworkSend.SendUpdateMoralTo(playerId, Server.Map.Instance[map].Moral);

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

        // Check for subscript out of range
        var count = System.Enum.GetValues(typeof(MovementState)).Length;
        var count2 = System.Enum.GetValues(typeof(Direction)).Length;
        if (dir < 0 || dir > count2 || movement < 0 || movement > count)
        {
            return;
        }

        // Prevent player from moving if they have casted a skill
        if (Data.TempPlayer[playerId].SkillBuffer >= 0)
        {
            NetworkSend.SendPlayerXY(playerId);
            return;
        }

        // if stunned, stop them moving
        if (Data.TempPlayer[playerId].StunDuration > 0)
        {
            NetworkSend.SendPlayerXY(playerId);
            return;
        }

        if (Data.TempPlayer[playerId].InShop >= 0 || Data.TempPlayer[playerId].InBank)
        {
            NetworkSend.SendPlayerXY(playerId);
            return;
        }

        SetPlayerDir(playerId, dir);
        var moved = false;
        var map = GetPlayerMap(playerId);

        switch ((Direction) dir)
        {
            case Direction.Up:
                if (GetPlayerY(playerId) > 0)
                {
                    if (IsTileBlocked(map, GetPlayerX(playerId), GetPlayerY(playerId) - 1, Direction.Up))
                    {
                        NetworkSend.SendPlayerXY(playerId);
                        return;
                    }

                    SetPlayerY(playerId, GetPlayerRawY(playerId) - 1);
                    moved = true;
                }
                else if (Server.Map.Instance[map].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type != TileType.NoCrossing && Server.Map.Instance[map].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type2 != TileType.NoCrossing)
                {
                    if (Server.Map.Instance[GetPlayerMap(playerId)].Up > 0)
                    {
                        var newMapY = Server.Map.Instance[Server.Map.Instance[GetPlayerMap(playerId)].Up].MaxY;
                        
                        OnWarp(playerId, Server.Map.Instance[GetPlayerMap(playerId)].Up, GetPlayerX(playerId), newMapY, (int) Direction.Up);
                        
                        didWarp = true;
                        moved = true;
                    }
                }

                break;

            case Direction.Down:
                if (GetPlayerY(playerId) < Server.Map.Instance[map].MaxY - 1)
                {
                    if (IsTileBlocked(map, GetPlayerX(playerId), GetPlayerY(playerId) + 1, Direction.Down))
                    {
                        NetworkSend.SendPlayerXY(playerId);
                        return;
                    }

                    SetPlayerY(playerId, GetPlayerRawY(playerId) + 1);
                    
                    moved = true;
                }
                else if (Server.Map.Instance[GetPlayerMap(playerId)].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type != TileType.NoCrossing && Server.Map.Instance[GetPlayerMap(playerId)].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type2 != TileType.NoCrossing)
                {
                    if (Server.Map.Instance[GetPlayerMap(playerId)].Down > 0)
                    {
                        OnWarp(playerId, Server.Map.Instance[GetPlayerMap(playerId)].Down, GetPlayerX(playerId), 0, (int) Direction.Down);
                        
                        didWarp = true;
                        moved = true;
                    }
                }

                break;

            case Direction.Left:
                if (GetPlayerX(playerId) > 0)
                {
                    if (IsTileBlocked(map, GetPlayerX(playerId) - 1, GetPlayerY(playerId), Direction.Left))
                    {
                        NetworkSend.SendPlayerXY(playerId);
                        return;
                    }

                    SetPlayerX(playerId, GetPlayerRawX(playerId) - 1);
                    
                    moved = true;
                }
                else if (Server.Map.Instance[GetPlayerMap(playerId)].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type != TileType.NoCrossing && Server.Map.Instance[GetPlayerMap(playerId)].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type2 != TileType.NoCrossing)
                {
                    if (Server.Map.Instance[GetPlayerMap(playerId)].Left > 0)
                    {
                        var newMapX = Server.Map.Instance[Server.Map.Instance[GetPlayerMap(playerId)].Left].MaxX;

                        OnWarp(playerId, Server.Map.Instance[GetPlayerMap(playerId)].Left, newMapX, GetPlayerY(playerId), (int) Direction.Left);

                        didWarp = true;
                        moved = true;
                    }
                }

                break;

            case Direction.Right:
                if (GetPlayerX(playerId) < Server.Map.Instance[map].MaxX - 1)
                {
                    if (IsTileBlocked(map, GetPlayerX(playerId) + 1, GetPlayerY(playerId), Direction.Right))
                    {
                        NetworkSend.SendPlayerXY(playerId);
                        return;
                    }

                    SetPlayerX(playerId, GetPlayerRawX(playerId) + 1);
                    
                    moved = true;
                }
                else if (Server.Map.Instance[GetPlayerMap(playerId)].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type != TileType.NoCrossing && Server.Map.Instance[GetPlayerMap(playerId)].Tile[GetPlayerX(playerId), GetPlayerY(playerId)].Type2 != TileType.NoCrossing)
                {
                    if (Server.Map.Instance[GetPlayerMap(playerId)].Right > 0)
                    {
                        OnWarp(playerId, Server.Map.Instance[GetPlayerMap(playerId)].Right, 0, GetPlayerY(playerId), (int) Direction.Right);
                        
                        didWarp = true;
                        moved = true;
                    }
                }

                break;

            case Direction.UpRight:
                if (GetPlayerY(playerId) > 0 && GetPlayerX(playerId) < Server.Map.Instance[map].MaxX - 1)
                {
                    if (IsTileBlocked(map, GetPlayerX(playerId) + 1, GetPlayerY(playerId) - 1, Direction.UpRight))
                    {
                        NetworkSend.SendPlayerXY(playerId);
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
                    if (IsTileBlocked(map, GetPlayerX(playerId) - 1, GetPlayerY(playerId) - 1, Direction.UpLeft))
                    {
                        NetworkSend.SendPlayerXY(playerId);
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
                    if (IsTileBlocked(map, GetPlayerX(playerId) + 1, GetPlayerY(playerId) + 1, Direction.DownRight))
                    {
                        NetworkSend.SendPlayerXY(playerId);
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
                    if (IsTileBlocked(map, GetPlayerX(playerId) - 1, GetPlayerY(playerId) + 1, Direction.DownLeft))
                    {
                        NetworkSend.SendPlayerXY(playerId);
                        return;
                    }

                    SetPlayerX(playerId, GetPlayerRawX(playerId) - 1);
                    SetPlayerY(playerId, GetPlayerRawY(playerId) + 1);

                    moved = true;
                }

                break;
        }

        if (GetPlayerX(playerId) >= 0 &&
            GetPlayerY(playerId) >= 0 &&
            GetPlayerX(playerId) < Server.Map.Instance[GetPlayerMap(playerId)].MaxX &&
            GetPlayerY(playerId) < Server.Map.Instance[GetPlayerMap(playerId)].MaxY)
        {
            for (var i = 0; i < Data.TempPlayer[playerId].EventMap.CurrentEvents; i++)
            {
                EventLogic.TriggerEvent(playerId, i, 1, GetPlayerX(playerId), GetPlayerY(playerId));
            }

            ref var tile = ref Server.Map.Instance[GetPlayerMap(playerId)].Tile[GetPlayerX(playerId), GetPlayerY(playerId)];

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
                    NetworkSend.SendOpenShop(playerId, x);
                    
                    Data.TempPlayer[playerId].InShop = x;
                }
            }

            // Check to see if the tile is a bank, and if so send bank
            if (tile.Type == TileType.Bank || tile.Type2 == TileType.Bank)
            {
                NetworkSend.SendBank(playerId);
                
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
                    NetworkSend.SendActionMessage(GetPlayerMap(playerId), "+" + healAmount, color, (byte)ActionMessageType.Scroll, GetPlayerX(playerId) * 32, GetPlayerY(playerId) * 32, 1);
                    SetPlayerVital(playerId, hv, Math.Min(GetPlayerVital(playerId, hv) + healAmount, GetPlayerMaxVital(playerId, hv)));
                    NetworkSend.SendPlayerMessage(playerId, "You feel rejuvenating forces coursing through your body.", (int)ColorName.BrightGreen);
                    NetworkSend.SendVital(playerId, hv);
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
                NetworkSend.SendActionMessage(GetPlayerMap(playerId), "-" + trapAmount, (int)ColorName.BrightRed, (byte)ActionMessageType.Scroll, GetPlayerX(playerId) * 32, GetPlayerY(playerId) * 32, 1);
                if (tv == Core.Globals.Vital.Health && GetPlayerVital(playerId, Core.Globals.Vital.Health) - trapAmount <= 0)
                {
                    OnKill(playerId);
                    NetworkSend.SendPlayerMessage(playerId, "You've been killed by a trap.", (int)ColorName.BrightRed);
                }
                else
                {
                    SetPlayerVital(playerId, tv, Math.Max(0, GetPlayerVital(playerId, tv) - trapAmount));
                    NetworkSend.SendPlayerMessage(playerId, "You've been injured by a trap.", (int)ColorName.BrightRed);
                    NetworkSend.SendVital(playerId, tv);
                }
                moved = true;
            }
        }

        // They tried to hack
        if (!moved || (expectingWarp && !didWarp))
        {
            OnWarp(playerId, GetPlayerMap(playerId), GetPlayerX(playerId), GetPlayerY(playerId), (byte) Direction.Down);
        }
        
        Player.Instance[playerId].IsMoving = true;
        NetworkSend.SendPlayerXYToMap(playerId);

        try
        {
            Script.Instance?.OnMove(playerId);
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnMove));
        }

        x = GetPlayerX(playerId);
        y = GetPlayerY(playerId);

        if (Data.TempPlayer[playerId].EventMap.CurrentEvents <= 0)
        {
            return;
        }
            
        for (var i = 0; i < Data.TempPlayer[playerId].EventMap.CurrentEvents; i++)
        {
            var beginEvent = false;

            if (Data.TempPlayer[playerId].EventMap.EventPages[i].EventId < 0)
            {
                continue;
            }
                
            if (Server.Map.Instance[GetPlayerMap(playerId)].Event[Data.TempPlayer[playerId].EventMap.EventPages[i].EventId].Globals == 1)
            {
                if (Server.Map.Instance[GetPlayerMap(playerId)].Event[Data.TempPlayer[playerId].EventMap.EventPages[i].EventId].X == x & Server.Map.Instance[GetPlayerMap(playerId)].Event[Data.TempPlayer[playerId].EventMap.EventPages[i].EventId].Y == y & Server.Map.Instance[GetPlayerMap(playerId)].Event[Data.TempPlayer[playerId].EventMap.EventPages[i].EventId].Pages[Data.TempPlayer[playerId].EventMap.EventPages[i].PageId].Trigger == 1 & Data.TempPlayer[playerId].EventMap.EventPages[i].Visible)
                {
                    beginEvent = true;
                }
            }
            else if (Data.TempPlayer[playerId].EventMap.EventPages[i].X == x & Data.TempPlayer[playerId].EventMap.EventPages[i].Y == y & Server.Map.Instance[GetPlayerMap(playerId)].Event[Data.TempPlayer[playerId].EventMap.EventPages[i].EventId].Pages[Data.TempPlayer[playerId].EventMap.EventPages[i].PageId].Trigger == 1 & Data.TempPlayer[playerId].EventMap.EventPages[i].Visible)
            {
                beginEvent = true;
            }

            if (!beginEvent)
            {
                continue;
            }
            
            // Process this event, it is on-touch and everything checks out.
            if (Server.Map.Instance[GetPlayerMap(playerId)].Event[Data.TempPlayer[playerId].EventMap.EventPages[i].EventId].Pages[Data.TempPlayer[playerId].EventMap.EventPages[i].PageId].CommandListCount > 0)
            {
                Data.TempPlayer[playerId].EventProcessing[Data.TempPlayer[playerId].EventMap.EventPages[i].EventId].Active = 0;
                Data.TempPlayer[playerId].EventProcessing[Data.TempPlayer[playerId].EventMap.EventPages[i].EventId].ActionTimer = General.GetTimeMs();
                Data.TempPlayer[playerId].EventProcessing[Data.TempPlayer[playerId].EventMap.EventPages[i].EventId].CurList = 0;
                Data.TempPlayer[playerId].EventProcessing[Data.TempPlayer[playerId].EventMap.EventPages[i].EventId].CurSlot = 0;
                Data.TempPlayer[playerId].EventProcessing[Data.TempPlayer[playerId].EventMap.EventPages[i].EventId].EventId = Data.TempPlayer[playerId].EventMap.EventPages[i].EventId;
                Data.TempPlayer[playerId].EventProcessing[Data.TempPlayer[playerId].EventMap.EventPages[i].EventId].PageId = Data.TempPlayer[playerId].EventMap.EventPages[i].PageId;
                Data.TempPlayer[playerId].EventProcessing[Data.TempPlayer[playerId].EventMap.EventPages[i].EventId].WaitingForResponse = 0;

                var eventId = Data.TempPlayer[playerId].EventMap.EventPages[i].EventId;
                var pageId = Data.TempPlayer[playerId].EventMap.EventPages[i].PageId;
                var commandListCount = Server.Map.Instance[GetPlayerMap(playerId)].Event[eventId].Pages[pageId].CommandListCount;

                Array.Resize(ref Data.TempPlayer[playerId].EventProcessing[eventId].ListLeftOff, commandListCount);
            }

            beginEvent = false;
        }
    }

    public static bool IsTileBlocked(int map, int x, int y, Direction dir)
    {
        try
        {
            if (Moral.Instance[Server.Map.Instance[map].Moral].PlayerBlock)
            {
                foreach (var playerId in PlayerService.Instance.PlayerIds)
                {
                    if (GetPlayerMap(playerId) == map &&
                        GetPlayerX(playerId) == x &&
                        GetPlayerY(playerId) == y)
                    {
                        return true;
                    }
                }
            }

            if (Moral.Instance[Server.Map.Instance[map].Moral].NpcBlock)
            {
                for (var mapNpcNum = 0; mapNpcNum < Core.Globals.Variables.MaxMapNpcs; mapNpcNum++)
                {
                    if (MapNpc.Instance[map, mapNpcNum].Num >= 0 &&
                        MapNpc.Instance[map, mapNpcNum].X == x &&
                        MapNpc.Instance[map, mapNpcNum].Y == y)
                    {
                        return true;
                    }
                }
            }

            // Check to make sure that the tile is walkable
            if (IsDirBlocked(Server.Map.Instance[map].Tile[x, y].DirBlock, dir))
            {
                return true;
            }

            return Server.Map.Instance[map].Tile[x, y].Type == TileType.Blocked ||
                   Server.Map.Instance[map].Tile[x, y].Type2 == TileType.Blocked;
        }
        catch (Exception)
        {
            return false;
        }
     }

    public static int HasItem(int playerId, int itemNum)
    {
        if (itemNum < 0 || itemNum > Core.Globals.Variables.MaxItems)
        {
            return 0;
        }

        var totalQuantity = 0;
        for (var invSlot = 0; invSlot < Core.Globals.Variables.MaxInventory; invSlot++)
        {
            if (GetPlayerInventory(playerId, invSlot) != itemNum)
            {
                continue;
            }

            if (Item.Instance[itemNum].Type == (byte) ItemCategory.Currency || Item.Instance[itemNum].Stackable == 1)
            {
                totalQuantity += GetPlayerInventoryValue(playerId, invSlot);
            }
            else
            {
                totalQuantity += 1;
            }
        }

        return totalQuantity;
    }

    public static int FindItemSlot(int playerId, int itemNum)
    {
        if (itemNum < 0 || itemNum >= Core.Globals.Variables.MaxItems)
        {
            return -1;
        }

        for (var invSlot = 0; invSlot < Core.Globals.Variables.MaxInventory; invSlot++)
        {
            if (GetPlayerInventory(playerId, invSlot) == itemNum)
            {
                return invSlot;
            }
        }

        return -1;
    }

    public static bool CanPickup(int playerId, int mapitemNum)
    {
        var map = GetPlayerMap(playerId);

        if (Server.Map.Instance[map].Moral < 0)
        {
            return false;
        }

        if (!Moral.Instance[Server.Map.Instance[map].Moral].CanPickupItem)
        {
            NetworkSend.SendPlayerMessage(playerId, "You can't pickup items here!", (int) ColorName.BrightRed);
            return false;
        }

        if (string.IsNullOrEmpty(MapItem.Instance[map, mapitemNum].PlayerName) ||
            MapItem.Instance[map, mapitemNum].PlayerName == GetPlayerName(playerId))
        {
            return true;
        }

        return false;
    }

    public static void OnGetItem(int playerId)
        {
            var map = GetPlayerMap(playerId);

            for (var mapItemNum = 0; mapItemNum < Core.Globals.Variables.MaxMapItems; mapItemNum++)
            {
                if (MapItem.Instance[map, mapItemNum].Num < 0 ||
                    MapItem.Instance[map, mapItemNum].Num >= Core.Globals.Variables.MaxItems)
                {
                    continue;
                }

                if (Math.Floor((double)MapItem.Instance[map, mapItemNum].X / Constants.TileSize) != GetPlayerX(playerId) || Math.Floor((double)MapItem.Instance[map, mapItemNum].Y / Constants.TileSize) != GetPlayerY(playerId))
                {
                    continue;
                }

                var slot = Player.FindOpenInvSlot(playerId, MapItem.Instance[map, mapItemNum].Num);
                if (slot == -1)
                {
                    NetworkSend.SendPlayerMessage(playerId, "Your inventory is full.", (int)ColorName.BrightRed);
                    break;
                }

                if (!Player.CanPickup(playerId, mapItemNum))
                {
                    break;
                }

                try
                {
                    Script.Instance?.MapGetItem(playerId, map, mapItemNum, slot);
                }
                catch (Exception ex)
                {
                    General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnGetItem));
                }

                break;
            }
        }


    public static int FindOpenInvSlot(int playerId, int itemNum)
    {
        if (!NetworkConfig.IsPlaying(playerId) || itemNum < 0 || itemNum > Core.Globals.Variables.MaxItems)
        {
            return -1;
        }

        if (Item.Instance[itemNum].Type == (byte) ItemCategory.Currency ||
            Item.Instance[itemNum].Stackable == 1)
        {
            for (var invSlot = 0; invSlot < Core.Globals.Variables.MaxInventory; invSlot++)
            {
                if (GetPlayerInventory(playerId, invSlot) == itemNum)
                {
                    return invSlot;
                }
            }
        }

        for (var invSlot = 0; invSlot < Core.Globals.Variables.MaxInventory; invSlot++)
        {
            if (GetPlayerInventory(playerId, invSlot) == -1)
            {
                return invSlot;
            }
        }

        return -1;
    }

    public static bool TakeInv(int playerId, int itemNum, int itemVal)
    {
        if (!NetworkConfig.IsPlaying(playerId) || itemNum < 0 || itemNum > Core.Globals.Variables.MaxItems)
        {
            return false;
        }

        var clearInvSlot = false;

        for (var invSlot = 0; invSlot < Core.Globals.Variables.MaxInventory; invSlot++)
        {
            // Check to see if the player has the item
            if (GetPlayerInventory(playerId, invSlot) != itemNum)
            {
                continue;
            }

            if (Item.Instance[itemNum].Type == (byte) ItemCategory.Currency ||
                Item.Instance[itemNum].Stackable == 1)
            {
                // Is what we are trying to take away more then what they have?  If so just set it to zero
                if (itemVal >= GetPlayerInventoryValue(playerId, invSlot))
                {
                    clearInvSlot = true;
                }
                else
                {
                    SetInventoryValue(playerId, invSlot, GetPlayerInventoryValue(playerId, invSlot) - itemVal);

                    NetworkSend.SendInventoryUpdate(playerId, invSlot);
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

            SetInventory(playerId, invSlot, -1);
            SetInventoryValue(playerId, invSlot, 0);

            NetworkSend.SendInventoryUpdate(playerId, invSlot);

            return true;
        }

        return false;
    }

    public static bool GiveInv(int playerId, int itemNum, int itemVal, byte bound = 0, bool sendUpdate = true)
    {
        if (!NetworkConfig.IsPlaying(playerId) || itemNum < 0 || itemNum > Core.Globals.Variables.MaxItems)
        {
            return false;
        }

        var slot = FindOpenInvSlot(playerId, itemNum);
        if (slot == -1)
        {
            NetworkSend.SendPlayerMessage(playerId, "Your inventory is full.", (int) ColorName.BrightRed);
            return false;
        }

        itemVal = Math.Max(itemVal, 1);

        SetInventory(playerId, slot, itemNum);
        SetInventoryValue(playerId, slot, GetPlayerInventoryValue(playerId, slot) + itemVal);
        Player.Instance[playerId].Inventory[slot].Bound = bound;

        if (sendUpdate)
        {
            NetworkSend.SendInventoryUpdate(playerId, slot);
        }

        return true;
    }

    public static void OnDrop(int playerId, int invNum, int amount)
    {
        if (!NetworkConfig.IsPlaying(playerId) || invNum < 0 || invNum > Core.Globals.Variables.MaxInventory)
        {
            return;
        }

        // Check the player isn't doing something
        if (Data.TempPlayer[playerId].InBank ||
            Data.TempPlayer[playerId].InShop >= 0 ||
            Data.TempPlayer[playerId].InTrade >= 0)
        {
            return;
        }

        if (!Moral.Instance[Server.Map.Instance[GetPlayerMap(playerId)].Moral].CanDropItem)
        {
            NetworkSend.SendPlayerMessage(playerId, "You can't drop items here!", (int) ColorName.BrightRed);
            return;
        }

        if (Player.Instance[playerId].Inventory[invNum].Bound > 0)
        {
            NetworkSend.SendPlayerMessage(playerId, "You can't drop soulbound items!", (int) ColorName.BrightRed);
            return;
        }

        var itemNum = GetPlayerInventory(playerId, invNum);
        if (itemNum < 0 || itemNum >= Core.Globals.Variables.MaxItems)
        {
            return;
        }

        var slot = MapItem.FindOpenSlot(GetPlayerMap(playerId));
        if (slot != -1)
        {
            var map = GetPlayerMap(playerId);

            var item = Item.Instance[itemNum];
            ref var mapItem = ref MapItem.Instance[map, slot];

            mapItem.Num = itemNum;
            mapItem.X = GetPlayerX(playerId);
            mapItem.Y = GetPlayerY(playerId);
            mapItem.PlayerName = GetPlayerName(playerId);
            mapItem.PlayerTimer = General.GetTimeMs() + Script.Instance?.ItemSpawnTimeMs();
            mapItem.DespawnTimer = General.GetTimeMs() + Script.Instance?.ItemDespawnTimeMs();
            mapItem.CanDespawn = true;

            try
            {
                Script.Instance?.OnDrop(playerId, slot, invNum, amount, map, item, itemNum);
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnDrop));
            }
        }
        else
        {
            NetworkSend.SendPlayerMessage(playerId, "Too many items already on the ground.", (int) ColorName.Yellow);
        }
    }

    public static bool TakeInvSlot(int playerId, int invSlot, int itemVal)
    {
        var takeInvSlot = false;

        if (!NetworkConfig.IsPlaying(playerId) || invSlot < 0 || invSlot > Core.Globals.Variables.MaxInventory)
        {
            return false;
        }

        var itemNum = GetPlayerInventory(playerId, invSlot);

        if (Item.Instance[itemNum].Type == (byte) ItemCategory.Currency ||
            Item.Instance[itemNum].Stackable == 1)
        {
            // Is what we are trying to take away more then what they have?  If so just set it to zero
            if (itemVal >= GetPlayerInventoryValue(playerId, invSlot))
            {
                takeInvSlot = true;
            }
            else
            {
                SetInventoryValue(playerId, invSlot, GetPlayerInventoryValue(playerId, invSlot) - itemVal);
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

        SetInventory(playerId, invSlot, -1);
        SetInventoryValue(playerId, invSlot, 0);

        return true;
    }

    public static bool CanUseItem(int playerId, int itemNum)
    {
        if (Server.Map.Instance[GetPlayerMap(playerId)].Moral >= 0)
        {
            if (!Moral.Instance[Server.Map.Instance[GetPlayerMap(playerId)].Moral].CanUseItem)
            {
                NetworkSend.SendPlayerMessage(playerId, "You can't use items here!", (int) ColorName.BrightRed);
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

            NetworkSend.SendPlayerMessage(playerId, "You do not meet the stat requirements to use this item.", (int) ColorName.BrightRed);
            return false;
        }

        if (Item.Instance[itemNum].LevelReq > GetPlayerLevel(playerId))
        {
            NetworkSend.SendPlayerMessage(playerId, "You do not meet the level requirements to use this item.", (int) ColorName.BrightRed);
            return false;
        }

        if (Item.Instance[itemNum].JobReq != -1 && Item.Instance[itemNum].JobReq != GetPlayerJob(playerId))
        {
            NetworkSend.SendPlayerMessage(playerId, "You do not meet the job requirements to use this item.", (int) ColorName.BrightRed);
            return false;
        }

        if (GetPlayerAccess(playerId) < Item.Instance[itemNum].AccessReq)
        {
            NetworkSend.SendPlayerMessage(playerId, "You do not meet the access requirement to equip this item.", (int) ColorName.BrightRed);
            return false;
        }

        if (!Data.TempPlayer[playerId].InBank && Data.TempPlayer[playerId].InShop < 0 && Data.TempPlayer[playerId].InTrade < 0)
        {
            return true;
        }

        NetworkSend.SendPlayerMessage(playerId, "You can't use items while in a bank, shop, or trade!", (int) ColorName.BrightRed);
        return false;
    }

    public static void UseItem(int playerId, int invNum)
    {
        if (invNum < 0 || invNum > Core.Globals.Variables.MaxInventory)
        {
            return;
        }

        var itemNum = GetPlayerInventory(playerId, invNum);
        if (itemNum < 0 || itemNum > Core.Globals.Variables.MaxItems)
        {
            return;
        }

        if (!CanUseItem(playerId, itemNum))
        {
            return;
        }

        try
        {
            Script.Instance?.UseItem(playerId, itemNum, invNum);
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

        var oldNum = GetPlayerInventory(playerId, oldSlot);
        var oldValue = GetPlayerInventoryValue(playerId, oldSlot);
        var newNum = GetPlayerInventory(playerId, newSlot);
        var newValue = GetPlayerInventoryValue(playerId, newSlot);
        var oldBound = Player.Instance[playerId].Inventory[oldSlot].Bound;
        var newBound = Player.Instance[playerId].Inventory[newSlot].Bound;

        if (newNum >= 0)
        {
            if (oldNum == newNum & Item.Instance[newNum].Stackable == 1) // Same item, if we can stack it, lets do that :P
            {
                SetInventory(playerId, newSlot, newNum);
                SetInventoryValue(playerId, newSlot, oldValue + newValue);
                SetInventory(playerId, oldSlot, 0);
                SetInventoryValue(playerId, oldSlot, 0);
                Player.Instance[playerId].Inventory[oldSlot].Bound = 0;

                if (oldBound > newBound)
                {
                    Player.Instance[playerId].Inventory[newSlot].Bound = oldBound;
                }
            }
            else
            {
                SetInventory(playerId, newSlot, oldNum);
                SetInventoryValue(playerId, newSlot, oldValue);
                SetInventory(playerId, oldSlot, newNum);
                SetInventoryValue(playerId, oldSlot, newValue);
                Player.Instance[playerId].Inventory[oldSlot].Bound = newBound;
                Player.Instance[playerId].Inventory[newSlot].Bound = oldBound;
            }
        }
        else
        {
            SetInventory(playerId, newSlot, oldNum);
            SetInventoryValue(playerId, newSlot, oldValue);
            SetInventory(playerId, oldSlot, newNum);
            SetInventoryValue(playerId, oldSlot, newValue);
            Player.Instance[playerId].Inventory[oldSlot].Bound = newBound;
            Player.Instance[playerId].Inventory[newSlot].Bound = oldBound;
        }

        NetworkSend.SendInventory(playerId);
    }

    public static void PlayerSwitchSkillSlots(int playerId, int oldSlot, int newSlot)
    {
        if (oldSlot == -1 || newSlot == -1)
        {
            return;
        }

        var oldNum = GetPlayerSkill(playerId, oldSlot);
        var oldValue = GetPlayerSkillCd(playerId, oldSlot);
        var newNum = GetPlayerSkill(playerId, newSlot);
        var newValue = GetPlayerSkillCd(playerId, newSlot);

        if (newNum >= 0)
        {
            if (oldNum == newNum & Item.Instance[newNum].Stackable == 1) // Same item, if we can stack it, lets do that :P
            {
                SetPlayerSkill(playerId, newSlot, newNum);
                SetPlayerSkillCd(playerId, newSlot, newValue);
                SetPlayerSkill(playerId, oldSlot, 0);
                SetPlayerSkillCd(playerId, oldSlot, 0);
            }
            else
            {
                SetPlayerSkill(playerId, newSlot, oldNum);
                SetPlayerSkillCd(playerId, newSlot, oldValue);
                SetPlayerSkill(playerId, oldSlot, newNum);
                SetPlayerSkillCd(playerId, oldSlot, newValue);
            }
        }
        else
        {
            SetPlayerSkill(playerId, newSlot, oldNum);
            SetPlayerSkillCd(playerId, newSlot, oldValue);
            SetPlayerSkill(playerId, oldSlot, newNum);
            SetPlayerSkillCd(playerId, oldSlot, newValue);
        }

        NetworkSend.SendPlayerSkills(playerId);
    }

    public static void OnCheckEquipment(int playerId)
    {
        var equipments = Enum.GetValues<Equipment>();

        foreach (var equipment in equipments)
        {
            var itemNum = GetPlayerPaperdoll(playerId, equipment);
            if (itemNum < 0)
            {
                SetPlayerPaperdoll(playerId, -1, equipment);
                continue;
            }

            if (Item.Instance[itemNum].SubType != (byte) equipment)
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

        var itemNum = GetPlayerPaperdoll(playerId, (Equipment) eqSlot);
        if (itemNum < 0 || itemNum >= Core.Globals.Variables.MaxItems)
        {
            return;
        }

        if (GetPlayerPaperdoll(playerId, (Equipment)eqSlot) < 0 || GetPlayerPaperdoll(playerId, (Equipment)eqSlot) > Core.Globals.Variables.MaxItems)
            return;

        if (FindOpenInvSlot(playerId, itemNum) >= 0)
        {
            try
            {
                Script.Instance?.UnEquipItem(playerId, itemNum, eqSlot, invSlot);
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(RemoveEquipment));
            }
        }
        else
        {
            NetworkSend.SendPlayerMessage(playerId, "Your inventory is full.", (int)ColorName.BrightRed);
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
        
        Account.OnClear(playerId);

        PlayerService.Instance.RemovePlayer(playerId);
        
        Data.TempPlayer[playerId].InGame = false;
        
        General.UpdateCaption();
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
        if (GetPlayerInventoryValue(playerId, invSlot) < amount && GetPlayerInventory(playerId, invSlot) == 0)
        {
            return;
        }

        var bankSlot = FindOpenbankSlot(playerId, GetPlayerInventory(playerId, invSlot));
        if (bankSlot == -1)
        {
            return;
        }

        var itemNum = GetPlayerInventory(playerId, invSlot);
        var bound = Player.Instance[playerId].Inventory[invSlot].Bound;

        Bank.Instance[playerId].Item[bankSlot].Bound = bound;

        if (Item.Instance[GetPlayerInventory(playerId, invSlot)].Type == (byte)ItemCategory.Currency ||
            Item.Instance[GetPlayerInventory(playerId, invSlot)].Stackable == 1)
        {
            if (GetBank(playerId, bankSlot) == GetPlayerInventory(playerId, invSlot))
            {
                SetBankValue(playerId, bankSlot, GetBankValue(playerId, bankSlot) + amount);

                TakeInv(playerId, GetPlayerInventory(playerId, invSlot), amount);
            }
            else
            {
                SetBank(playerId, bankSlot, GetPlayerInventory(playerId, invSlot));
                SetBankValue(playerId, bankSlot, amount);

                TakeInv(playerId, GetPlayerInventory(playerId, invSlot), amount);
            }
        }
        else if (GetBank(playerId, bankSlot) == GetPlayerInventory(playerId, invSlot))
        {
            SetBankValue(playerId, bankSlot, GetBankValue(playerId, bankSlot) + 1);

            TakeInv(playerId, GetPlayerInventory(playerId, invSlot), 0);
        }
        else
        {
            SetBank(playerId, bankSlot, itemNum);
            SetBankValue(playerId, bankSlot, 1);

            TakeInv(playerId, GetPlayerInventory(playerId, invSlot), 0);
        }

        NetworkSend.SendBank(playerId);
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

    public static int FindOpenbankSlot(int playerId, int itemNum)
    {
        if (!NetworkConfig.IsPlaying(playerId) || itemNum < 0 || itemNum >= Core.Globals.Variables.MaxItems)
        {
            return -1;
        }

        if (Item.Instance[itemNum].Type == (byte) ItemCategory.Currency ||
            Item.Instance[itemNum].Stackable == 1)
        {
            for (var bankSlot = 0; bankSlot < Core.Globals.Variables.MaxBank; bankSlot++)
            {
                if (GetBank(playerId, bankSlot) == itemNum)
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
            else if (GetBank(playerId, bankSlot) == GetPlayerInventory(playerId, invSlot))
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

        NetworkSend.SendBank(playerId);
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

        NetworkSend.SendBank(playerId);
    }
}