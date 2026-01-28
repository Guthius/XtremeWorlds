using Core;
using CSScripting;
using Microsoft.CodeAnalysis.CSharp;
using Server;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Core.Globals.Commands;
using static Core.Net.Packets;
using static Core.Globals.Type;
using static Server.Animation;
using static Server.Event;
using static Server.Item;
using static Server.Moral;
using static Server.Network;
using static Server.Npc;
using static Server.Party;
using static Server.Player;
using Variables = Core.Globals.Variables;
using Microsoft.Extensions.Logging;
using XtremeWorlds.Server.Configuration;
using Item = Server.Item;
using MapItem = Server.MapItem;
using MapNpc = Server.MapNpc;
using Map = Server.Map;
using Resource = Server.Resource;
using Shop = Server.Shop;
using Projectile = Server.Projectile;
using Core.Globals;
using Server.Game;
using Core.Objects;

public class Script
{
    // Add a per-Player pickup lock
    private static bool[] _isPickingUp = new bool[Core.Globals.Variables.MaxPlayers];
    private static bool[] _isUsing = new bool[Core.Globals.Variables.MaxPlayers];
    private static bool[] _isEquipable = new bool[Core.Globals.Variables.MaxPlayers];
    private static bool[] _isUnEquipable = new bool[Core.Globals.Variables.MaxPlayers];
    private const int DoorReset = 30000; // 30 seconds
    private static readonly object _doorResetLock = new();
    private static readonly Dictionary<(int map, int x, int y), long> _doorResetExpiryByTile = new();

    // Timers for periodic regeneration
    private static long _lastNpcRegen;
    private static long _lastPlayerRegen;
    public static int NpcRegenInterval = 10000; // 10 seconds
    public static int PlayerRegenInterval = 10000; // 10 seconds
    public static int BaseAttackSpeed = 1000; // fallback when no weapon speed
    public static int DeathSpawnTime = 60000; // 1 minute
    public static long ItemSpawnTime = 30000L; // 30 seconds
    public static long ItemDespawnTime = 90000L; // 1:30 seconds
    public static byte StatPerLevel = 5;
    private readonly List<(int skillId, Entity target)> _queuedChainOnHit = new();
    private (Core.Globals.Entity.EntityType type, int id, int map)? _activeCastChainCaster;
    private int _activeCastChainSkillId = -1;
    public static byte MaxLevel = Core.Globals.Variables.MaxLevel;
    public static int MaxAnimations = Core.Globals.Variables.MaxAnimations;
    public static byte MaxBank = Core.Globals.Variables.MaxBank;
    public static byte MaxJobs = Core.Globals.Variables.MaxJobs;
    public static byte MaxMorals = Core.Globals.Variables.MaxMorals;
    public static byte MaxInv = Core.Globals.Variables.MaxInventory;
    public static int MaxItems = Core.Globals.Variables.MaxItems;
    public static int MaxMaps = Core.Globals.Variables.MaxMaps;
    public static byte MaxMapItems = Core.Globals.Variables.MaxMapItems;
    public static int MaxMapNpcs = Core.Globals.Variables.MaxMapNpcs;
    public static int MaxNpcs = Core.Globals.Variables.MaxNpcs;
    public static byte MaxNpcSkills = Core.Globals.Variables.MaxNpcSkills;
    public static int MaxParty = Core.Globals.Variables.MaxParty;
    public static int MaxPartyMembers = Core.Globals.Variables.MaxPartyMembers;
    public static int MaxPlayers = Core.Globals.Variables.MaxPlayers;
    public static byte MaxPlayerSkills = Core.Globals.Variables.MaxPlayerSkills;
    public static int MaxResources = Core.Globals.Variables.MaxResources;
    public static int MaxShops = Core.Globals.Variables.MaxShops;
    public static int MaxSkills = Core.Globals.Variables.MaxSkills;
    public static byte MaxTrades = Core.Globals.Variables.MaxTrades;
    public static byte NameLength = Variables.NameLength;
    public static byte MaxNameLength = Variables.NameLength;
    public static byte Minimum_NameLength = Variables.MinimumNameLength;
    public static byte ChatLength = Variables.ChatLength;
    public static byte MaxHotbar = Core.Globals.Variables.MaxHotbar;
    public static byte MaxMapx = Core.Globals.Variables.MaxMapX;
    public static byte MaxMapy = Core.Globals.Variables.MaxMapY;
    public static int MaxProjectiles = Core.Globals.Variables.MaxProjectiles;
    public static byte MaxDropItems = Core.Globals.Variables.MaxDropItems;
    public static byte MaxStartItems = Core.Globals.Variables.MaxStartItems;
    public static byte MaxStartSkills = Core.Globals.Variables.MaxStartSkills;
    public static int MaxSwitches = Core.Globals.Variables.MaxSwitches;
    public static int MaxVariables = Core.Globals.Variables.MaxVariables;
    public static byte MaxCharacters = Core.Globals.Variables.MaxCharacters;
    public static int ChatLines = Variables.ChatLines;
    public static byte MaxStats = Core.Globals.Variables.MaxStats;
    public static byte MaxQuests = Core.Globals.Variables.MaxQuests;
    public static int MaxEvents = Core.Globals.Variables.MaxEvents;
    public static byte MaxGuilds = Core.Globals.Variables.MaxGuilds;
    public static byte MaxEventChoices = Core.Globals.Variables.MaxEventChoices;
    public static int TileSize = Variables.TileSize;
    public static int MaxWeatherParticles = Core.Globals.Variables.MaxWeatherParticles;
    public static int MaxBackups = Core.Globals.Variables.MaxBackups;
    public static byte SaveInterval = Variables.SaveInterval;
    public static int ServerShutdown = Variables.ServerShutdown;
    public static string Welcome = Variables.Welcome;
    public static string Website = Variables.Website;

    // Apply the script-configured values back into the engine's global Variables.
    // Call this after loading the script and before initializing game content.
    public void ApplyEngineVariables()
    {
        Core.Globals.Variables.MaxLevel = MaxLevel;
        Core.Globals.Variables.MaxAnimations = MaxAnimations;
        Core.Globals.Variables.MaxBank = MaxBank;
        Core.Globals.Variables.MaxJobs = MaxJobs;
        Core.Globals.Variables.MaxMorals = MaxMorals;
        Core.Globals.Variables.MaxInventory = MaxInv;
        Core.Globals.Variables.MaxItems = MaxItems;
        Core.Globals.Variables.MaxMaps = MaxMaps;
        Core.Globals.Variables.MaxMapItems = MaxMapItems;
        Core.Globals.Variables.MaxMapNpcs = MaxMapNpcs;
        Core.Globals.Variables.MaxNpcs = MaxNpcs;
        Core.Globals.Variables.MaxNpcSkills = MaxNpcSkills;
        Core.Globals.Variables.MaxParty = MaxParty;
        Core.Globals.Variables.MaxPartyMembers = MaxPartyMembers;
        Core.Globals.Variables.MaxPlayers = MaxPlayers;
        Core.Globals.Variables.MaxPlayerSkills = MaxPlayerSkills;
        Core.Globals.Variables.MaxResources = MaxResources;
        Core.Globals.Variables.MaxShops = MaxShops;
        Core.Globals.Variables.MaxSkills = MaxSkills;
        Core.Globals.Variables.MaxTrades = MaxTrades;
        Variables.NameLength = NameLength;
        Variables.MinimumNameLength = Minimum_NameLength;
        Variables.ChatLength = ChatLength;
        Core.Globals.Variables.MaxHotbar = MaxHotbar;
        Core.Globals.Variables.MaxMapX = MaxMapx;
        Core.Globals.Variables.MaxMapY = MaxMapy;
        Core.Globals.Variables.MaxProjectiles = MaxProjectiles;
        Core.Globals.Variables.MaxDropItems = MaxDropItems;
        Core.Globals.Variables.MaxStartItems = MaxStartItems;
        Core.Globals.Variables.MaxStartSkills = MaxStartSkills;
        Core.Globals.Variables.MaxSwitches = MaxSwitches;
        Core.Globals.Variables.MaxVariables = MaxVariables;
        Core.Globals.Variables.MaxCharacters = MaxCharacters;
        Variables.ChatLines = ChatLines;
        Core.Globals.Variables.MaxStats = MaxStats;
        Core.Globals.Variables.MaxQuests = MaxQuests;
        Core.Globals.Variables.MaxEvents = MaxEvents;
        Core.Globals.Variables.MaxGuilds = MaxGuilds;
        Core.Globals.Variables.MaxEventChoices = MaxEventChoices;
        Variables.TileSize = TileSize;
        Core.Globals.Variables.MaxWeatherParticles = MaxWeatherParticles;
        Core.Globals.Variables.MaxBackups = MaxBackups;
        Variables.SaveInterval = SaveInterval;
        Variables.ServerShutdown = ServerShutdown;
        Variables.Welcome = Welcome;
        Variables.Website = Website;
    }

    public int GetPlayerMaxLevel()
    {
        return MaxLevel;
    }

    public void Loop()
    {

    }

    public void ServerSecond()
    {

    }


    public void ServerMinute()
    {

    }

    public void ServerHour()
    {

    }

    public void OnSpawnItem()
    {

    }

    public void OnSpawnNpc()
    {

    }

    public void OnJoin(int index)
    {
        // Warp the Player to his saved location
        OnWarp(index, GetMap(index), GetX(index), GetY(index), (byte)Direction.Down, true);

        // Notify everyone that a Player has joined the game.
        Network.GlobalMessage(string.Format("{0} has joined {1}!", GetName(index), SettingsManager.Instance.GameName));

        // Send all the required game data to the user.
        OnCheckEquipment(index);
        Network.PlayerData(index);
        Network.Inventory(index);
        Network.PlayerSkills(index);
        Network.WornEquipment(index);
        Network.Experience(index);
        Network.Hotbar(index);
        Network.Stats(index);

        // Send the flag so they know they can start doing stuff
        Network.InGame(index);

        // If this specific character is currently dead, send their remaining death timer.
        // This prevents the timer from leaking across character swaps and also re-applies
        // the correct timer when relogging the same dead character.
        if (Server.Player.Instance[index].Dead)
        {
            var now = General.GetTime();
            var remaining = Server.Player.Instance[index].DeathTimer - now;
            if (remaining <= 0)
            {
                // Timer already expired while offline; finish the respawn immediately.
                OnDeath(index);
            }
            else
            {
                Network.PlayerDeath(index, remaining);
            }
        }
        else
        {
            Network.PlayerDeath(index, 0);
        }

        // Send welcome messages
        Network.Welcome(index);
    
        // Use a default bell sound that ships with content; adjust if you prefer another file
        Network.PlaySound(index, "Bell.ogg", GetX(index), GetY(index));
    }

    public void OnDrop(int index, int invSlot, int amount)
    {
        var item = Item.Instance[GetInv(index, invSlot)];
        var map = GetMap(index);
        var id = GetInv(index, invSlot);

        // Determine if the item is currency or stackable
        if (item.Type == (byte)ItemCategory.Currency || item.Stackable == 1)
        {
            // Check if dropping more than the Player has, drop all if so
            var InventoryValue = GetInvValue(index, invSlot);
            if (amount >= InventoryValue)
            {
                amount = InventoryValue;
                SetInv(index, invSlot, -1);
                SetInvValue(index, invSlot, 0);
            }
            else
            {
                SetInvValue(index, invSlot, InventoryValue - amount);
            }
            Network.MapMessage(map, string.Format("{0} has dropped {1} ({2}x).", GetName(index), GameLogic.CheckGrammar(item.Name), amount));
        }
        else
        {
            // Not a currency or stackable item
            SetInv(index, invSlot, -1);
            SetInvValue(index, invSlot, 0);

            Network.MapMessage(map, string.Format("{0} has dropped {1}.", GetName(index), GameLogic.CheckGrammar(item.Name)));
        }

        // Send inventory update
        Network.InventoryUpdate(index, invSlot);

        // Spawn the item on the map
        Server.MapItem.OnSpawn(id, amount, map, GetX(index), GetY(index));
    }

    public void OnPickup(int index, int map, int mapSlot, int invSlot)
    {
        // Prevent double pickup: if already picking up, ignore
        if (_isPickingUp[index])
            return;

        _isPickingUp[index] = true;

        // Set item in Player's inventory
        var itemId = MapItem.Instance[map, mapSlot].Num;
        SetInv(index, invSlot, itemId);

        string msg;
        var item = Item.Instance[itemId];
        int mapValue = MapItem.Instance[map, mapSlot].Value;

        if (item.BindType == 1)
        {
            Server.Player.Instance[index].Inventory[invSlot].Bound = 1;
        }

        if (item.Type == (byte)ItemCategory.Currency || item.Stackable == 1)
        {
            // For stackable/currency, add the value from the map item (should be 1 for most drops)
            SetInvValue(index, invSlot, GetInvValue(index, invSlot) + mapValue);
            msg = mapValue + " " + item.Name;
        }
        else
        {
            // For non-stackable, always set to 1 regardless of map item value
            SetInvValue(index, invSlot, 1);
            msg = item.Name;
        }

        // Erase item from the map
        MapItem.Instance[map, mapSlot].Num = -1;
        MapItem.Instance[map, mapSlot].Value = 0;
        Network.MapItemToAll(map, mapSlot);
        Network.InventoryUpdate(index, invSlot);
        Network.ActionMessage(GetMap(index), msg, (int)ColorName.White, (byte)ActionMessageType.Static, GetX(index) * Variables.TileSize, GetY(index) * Variables.TileSize);

        // Unlock pickup for this Player
        _isPickingUp[index] = false;
    }

    public void OnUnEquip(int index, int item, int eqSlot, int invSlot)
    {
        // Prevent re-entrant unequip actions for this Player
        if (_isUnEquipable[index])
            return;

        _isUnEquipable[index] = true;
        try
        {
            SetInv(index, invSlot, Server.Player.Instance[index].Paperdoll[eqSlot].Num);
            Server.Player.Instance[index].Inventory[invSlot].Bound = Server.Player.Instance[index].Paperdoll[eqSlot].Bound;
            SetInvValue(index, invSlot, 1);

            Network.PlayerMessage(index, "You unequip " + GameLogic.CheckGrammar(Item.Instance[GetPaperdoll(index, (Equipment)eqSlot)].Name) + ".", (int)ColorName.Yellow);

            // remove equipment
            SetPaperdoll(index, -1, (Equipment)eqSlot);
            Network.WornEquipment(index);
            Network.MapEquipment(index);
            Network.Stats(index);
            Network.Inventory(index);

            // send vitals
            Network.Vitals(index);
        }
        finally
        {
            _isUnEquipable[index] = false;
        }
    }

    public void OnUse(int index, int item, int invSlot)
    {
        // Prevent re-entrant item usage for a single Player (e.g., rapid packet spam)
        if (_isUsing[index])
            return;

        _isUsing[index] = true;
        try
        {            
            var tempdata = new int[Enum.GetValues(typeof(Stat)).Length + 4];
            var tempstr = new string[3];

            // If the player is facing a Door/Key tile, attempt to unlock it with this item first.
            // This allows key items to be any item type (e.g., Consumable) without applying their normal effects.
            if (TryUseKeyOnFacingTile(index, item, invSlot))
            {
                return;
            }

            // Find out what kind of item it is
            switch (Item.Instance[item].Type)
            {
                case (byte)ItemCategory.Equipment:
                    {
                        OnEquip(index, item, invSlot);
                        break;
                    }

                case (byte)ItemCategory.Consumable:
                    {
                        switch (Item.Instance[item].SubType)
                        {
                            case (byte)ConsumableEffect.RestoresHealth:
                                {
                                    Network.ActionMessage(GetMap(index), "+" + Item.Instance[item].Data1, (int)ColorName.BrightGreen, (byte)ActionMessageType.Scroll, GetX(index) * Variables.TileSize, GetY(index) * Variables.TileSize);
                                    Network.PlayAnimation(GetMap(index), Item.Instance[item].Animation, 0, 0, (byte)TargetType.Player, index);
                                    SetVital(index, Core.Globals.Vital.Health, GetVital(index, Core.Globals.Vital.Health) + Item.Instance[item].Data1);
                                    TakeInv(index, item, 1);
                                    Network.Vital(index, Core.Globals.Vital.Health);
                                    break;
                                }

                            case (byte)ConsumableEffect.RestoresMana:
                                {
                                    Network.ActionMessage(GetMap(index), "+" + Item.Instance[item].Data1, (int)ColorName.BrightBlue, (byte)ActionMessageType.Scroll, GetX(index) * Variables.TileSize, GetY(index) * Variables.TileSize);
                                    Network.PlayAnimation(GetMap(index), Item.Instance[item].Animation, 0, 0, (byte)TargetType.Player, index);
                                    SetVital(index, Core.Globals.Vital.Stamina, GetVital(index, Core.Globals.Vital.Stamina) + Item.Instance[item].Data1);
                                    TakeInv(index, item, 1);
                                    Network.Vital(index, Core.Globals.Vital.Stamina);
                                    break;
                                }

                            case (byte)ConsumableEffect.RestoresStamina:
                                {
                                    Network.PlayAnimation(GetMap(index), Item.Instance[item].Animation, 0, 0, (byte)TargetType.Player, index);
                                    SetVital(index, Core.Globals.Vital.Stamina, GetVital(index, Core.Globals.Vital.Stamina) + Item.Instance[item].Data1);
                                    TakeInv(index, item, 1);
                                    Network.Vital(index, Core.Globals.Vital.Stamina);
                                    break;
                                }

                            case (byte)ConsumableEffect.GrantsExperience:
                                {
                                    Network.PlayAnimation(GetMap(index), Item.Instance[item].Animation, 0, 0, (byte)TargetType.Player, index);
                                    SetExp(index, GetExp(index) + Item.Instance[item].Data1);
                                    TakeInv(index, item, 1);
                                    Network.Experience(index);
                                    break;
                                }

                        }

                        break;
                    }

                case (byte)ItemCategory.Projectile:
                    {
                        if (Item.Instance[item].Ammo >= 0)
                        {
                            if (HasItem(index, Item.Instance[item].Ammo) > 0)
                            {
                                TakeInv(index, Item.Instance[item].Ammo, 1);
                                Server.Projectile.OnShoot(index, -1, item);
                            }
                            else
                            {
                                Network.PlayerMessage(index, "Out of " + Item.Instance[Item.Instance[GetPaperdoll(index, Equipment.Weapon)].Ammo].Name + "!", (int)ColorName.BrightRed);
                                return;
                            }
                        }
                        else
                        {
                            Server.Projectile.OnShoot(index, -1, item);
                            return;
                        }

                        break;
                    }

                case (byte)ItemCategory.Event:
                    {
                        // Trigger item-driven common event using item's SubType/Data1/Data2
                        CommonEvent(index, item, invSlot);
                        break;
                    }

                case (byte)ItemCategory.Skill:
                    {
                        OnLearn(index, item);
                        break;
                    }
            }
        }
        finally
        {
            _isUsing[index] = false;
        }
    }

    private void CommonEvent(int index, int item, int invSlot, int skill = -1)
    {
        if (skill >= 0)
        {
            TriggerCommonEvent(index,
                Skill.Instance[skill].CommonEventType,
                Skill.Instance[skill].CommonEventData1,
                Skill.Instance[skill].CommonEventData2);
        }
        else
        {
            var itemId = item;
            var itemTemplate = Item.Instance[itemId];

            // Key usage is tile-type driven (unlock/unblock the facing tile) rather than firing an event.
            // Handle both new and legacy item encodings.
            var triggerType = -1;
            if (itemTemplate.CommonEventType > 0)
            {
                triggerType = itemTemplate.CommonEventType - 1;
            }
            else
            {
                triggerType = itemTemplate.SubType;
            }

            if (triggerType == (byte)CommonEventTrigger.Key)
            {
                TryUseKeyOnFacingTile(index, itemId, invSlot);
                return;
            }

            // Items now use the same encoding as NPC/Skill/Resource editors:
            // 0 = none, 1..N = (CommonEventTrigger + 1).
            // Backward compatibility: older items stored trigger in SubType/Data1/Data2.
            if (itemTemplate.CommonEventType > 0)
            {
                TriggerCommonEvent(index, itemTemplate.CommonEventType, itemTemplate.CommonEventData1, itemTemplate.CommonEventData2);
                return;
            }

            TriggerCommonEventRaw(index,
                itemTemplate.SubType,
                itemTemplate.Data1,
                itemTemplate.Data2);
        }
    }

    // Backward-compatible overload for existing call sites (e.g. skill-driven common events).
    private void CommonEvent(int index, int item, int skill = -1)
    {
        CommonEvent(index, item, -1, skill);
    }

    private static bool TryUseKeyOnFacingTile(int playerId, int key, int invSlot)
    {
        var map = GetMap(playerId);
        if (map < 0 || map >= Server.Map.Instance.Count)
        {
            return false;
        }

        var maxX = Server.Map.Instance[map].MaxX;
        var maxY = Server.Map.Instance[map].MaxY;

        var (dx, dy) = GetDirectionDelta((Direction)GetDir(playerId));
        var x = GetX(playerId) + dx;
        var y = GetY(playerId) + dy;

        if (x < 0 || y < 0 || x >= maxX || y >= maxY)
        {
            return false;
        }

        ref var tile = ref Server.Map.Instance[map].Tile[x, y];
        var opened = false;
        var openedDoor = false;

        static bool CanUnlock(int usedKeyItemId, int requiredItemId)
        {
            // requiredItemId == 0 means "any key" (or script-triggered unlock).
            if (requiredItemId <= 0)
            {
                return true;
            }

            return usedKeyItemId == requiredItemId;
        }

        static bool ShouldConsume(int consumeFlag) => consumeFlag == 1;

        if (tile.Type == TileType.Key)
        {
            if (CanUnlock(key, tile.Data1))
            {
                tile.Type = TileType.KeyOpen;
                opened = true;
            }
        }
        else if (tile.Type == TileType.Door)
        {
            if (CanUnlock(key, tile.Data1))
            {
                tile.Type = TileType.KeyOpen;
                opened = true;
                openedDoor = true;
            }
        }

        if (tile.Type2 == TileType.Key)
        {
            if (CanUnlock(key, tile.Data1_2))
            {
                tile.Type2 = TileType.KeyOpen;
                opened = true;
            }
        }
        else if (tile.Type2 == TileType.Door)
        {
            if (CanUnlock(key, tile.Data1_2))
            {
                tile.Type2 = TileType.KeyOpen;
                opened = true;
                openedDoor = true;
            }
        }

        if (!opened)
        {
            return false;
        }

        // Unblock movement after unlocking.
        tile.DirBlock = 0;

        // Consume the key only when it successfully unlocks something and the tile says to take it.
        // Data2 == 0 => do not consume.
        var consume = false;
        if (opened)
        {
            // Prefer the layer that was just unlocked.
            if (tile.Type == TileType.KeyOpen)
            {
                consume = ShouldConsume(tile.Data2);
            }
            else if (tile.Type2 == TileType.KeyOpen)
            {
                consume = ShouldConsume(tile.Data2_2);
            }
        }

        if (consume && invSlot >= 0)
        {
            Server.Player.TakeInvSlot(playerId, invSlot, 1);
            Network.InventoryUpdate(playerId, invSlot);
        }

        // Play the key item's animation on the player (if configured).
        if (key >= 0 && key < Item.Instance.Count)
        {
            var anim = Item.Instance[key].Animation;
            if (anim > 0)
            {
                Network.PlayAnimation(map, anim, 0, 0, (byte)TargetType.Player, playerId);
            }
        }

        // Broadcast updated map so clients see the tile become unblocked/open.
        BroadcastMapToPlayersOnMap(map);

        // If we opened a Door, auto-relock it after 30 seconds.
        if (openedDoor)
        {
            ScheduleDoorReset(map, x, y);
        }

        return true;
    }

    private static void BroadcastMapToPlayersOnMap(int map)
    {
        foreach (var otherPlayerId in PlayerService.Instance.PlayerIds)
        {
            if (GetMap(otherPlayerId) != map)
            {
                continue;
            }

            Network.MapData(otherPlayerId, map, true);
        }
    }

    private static void ScheduleDoorReset(int map, int x, int y)
    {
        var expiry = General.GetTime() + DoorReset;
        lock (_doorResetLock)
        {
            _doorResetExpiryByTile[(map, x, y)] = expiry;
        }

        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            await System.Threading.Tasks.Task.Delay(DoorReset).ConfigureAwait(false);

            long expectedExpiry;
            lock (_doorResetLock)
            {
                if (!_doorResetExpiryByTile.TryGetValue((map, x, y), out expectedExpiry))
                {
                    return;
                }

                if (expectedExpiry != expiry)
                {
                    // Door was re-opened; a newer timer will handle it.
                    return;
                }

                _doorResetExpiryByTile.Remove((map, x, y));
            }

            if (map < 0 || map >= Server.Map.Instance.Count)
            {
                return;
            }

            if (x < 0 || y < 0 || x >= Server.Map.Instance[map].MaxX || y >= Server.Map.Instance[map].MaxY)
            {
                return;
            }

            ref var tile = ref Server.Map.Instance[map].Tile[x, y];

            // Only reset tiles that are currently open back into Door.
            if (tile.Type == TileType.KeyOpen)
            {
                tile.Type = TileType.Door;
            }

            if (tile.Type2 == TileType.KeyOpen)
            {
                tile.Type2 = TileType.Door;
            }

            BroadcastMapToPlayersOnMap(map);
        });
    }

    private static (int dx, int dy) GetDirectionDelta(Direction direction)
    {
        return direction switch
        {
            Direction.Up => (0, -1),
            Direction.Down => (0, 1),
            Direction.Left => (-1, 0),
            Direction.Right => (1, 0),
            Direction.UpLeft => (-1, -1),
            Direction.UpRight => (1, -1),
            Direction.DownLeft => (-1, 1),
            Direction.DownRight => (1, 1),
            _ => (0, 0)
        };
    }

    /// <summary>
    /// Triggers a common event using the skill-style encoding:
    /// 0 = none, 1..N = (CommonEventTrigger + 1).
    /// </summary>
    public void TriggerCommonEvent(int playerId, byte commonEventType, int data1, int data2)
    {
        if (commonEventType <= 0)
        {
            return;
        }

        TriggerCommonEventRaw(playerId, commonEventType - 1, data1, data2);
    }

    /// <summary>
    /// Triggers a common event where the trigger type is already 0-based (CommonEventTrigger).
    /// </summary>
    private void TriggerCommonEventRaw(int playerId, int triggerType, int data1, int data2)
    {
        switch (triggerType)
        {
            case (byte)CommonEventTrigger.Switch:
                Server.Player.Instance[playerId].Switches[Math.Max(0, data1)] = (byte)Math.Max(0, data2);
                break;

            case (byte)CommonEventTrigger.Variable:
                Server.Player.Instance[playerId].Variables[Math.Max(0, data1)] = data2;
                break;

            case (byte)CommonEventTrigger.Key:
                TryUseKeyOnFacingTile(playerId, -1, -1);
                break;

            case (byte)CommonEventTrigger.Script:
                if (data1 == 0)
                    Network.PlayerMessage(playerId, "You feel a strange sensation...", (int)ColorName.BrightCyan);
                else
                    Network.PlayerMessage(playerId, "Nothing happens.", (int)ColorName.Yellow);
                break;
        }
    }
        
    private void OnEquip(int index, int item, int invSlot)
    {
        if (_isEquipable[index])
            return;

        _isEquipable[index] = true;
        try
        {
            int tempItem = -1;
            int m;
            Equipment eqType = (Equipment)Item.Instance[item].SubType;
            if (Item.Instance[item].BindType == 2)
            {
                Server.Player.Instance[index].Inventory[invSlot].Bound = 2;
            }

            if (GetPaperdoll(index, eqType) >= 0)
            {
                tempItem = GetPaperdoll(index, eqType);
            }
            SetPaperdoll(index, item, eqType);
            Server.Player.Instance[index].Paperdoll[(byte)eqType].Bound = Server.Player.Instance[index].Inventory[invSlot].Bound;
            Network.PlayerMessage(index, "You equip " + GameLogic.CheckGrammar(Item.Instance[item].Name) + ".", (int)ColorName.BrightGreen);

            // Play equip animation (if configured) on the player.
            var equipAnim = Item.Instance[item].Animation;
            if (equipAnim >= 0)
            {
                Network.PlayAnimation(GetMap(index), equipAnim, 0, 0, (byte)TargetType.Player, index);
            }

            TakeInv(index, item, 1);
            if (tempItem >= 0)
            {
                m = FindOpenInvSlot(index, tempItem);
                SetInv(index, m, tempItem);
                SetInvValue(index, m, 1);
            }
            Network.WornEquipment(index);
            Network.MapEquipment(index);
            Network.Stats(index);
            Network.Vitals(index);
        }
        finally
        {
            _isEquipable[index] = false;
        }
    }

    public void OnLearn(int index, int item, int skill = -1)
    {
        int n;
        int i;

        // Get the skill num
        if (skill >= 0)
        {
            n = skill;
        }
        else
        {
            n = Item.Instance[item].Data1;
        }

        if (n < 0 | n >= Core.Globals.Variables.MaxSkills)
            return;

        // Make sure they are the right class
        if (Skill.Instance[n].JobReq == GetJob(index) | Skill.Instance[n].JobReq == -1)
        {
            // Make sure they are the right level
            i = Skill.Instance[n].LevelReq;

            if (i <= GetLevel(index))
            {
                i = FindOpenSkill(index);

                // Make sure they have an open skill slot
                if (i >= 0)
                {
                    // Make sure they dont already have the skill
                    if (!HasSkill(index, n))
                    {
                        SetSkill(index, i, n);
                        if (item >= 0)
                        {
                            Network.PlayAnimation(GetMap(index), Item.Instance[item].Animation, 0, 0, (byte)TargetType.Player, index);
                            TakeInv(index, item, 1);
                        }
                        Network.PlayerMessage(index, "You study the skill carefully.", (int)ColorName.Yellow);
                        Network.PlayerMessage(index, "You have learned a new skill!", (int)ColorName.BrightGreen);
                        Network.PlayerSkills(index);
                    }
                    else
                    {
                        Network.PlayerMessage(index, "You have already learned this skill!", (int)ColorName.BrightRed);
                    }
                }
                else
                {
                    Network.PlayerMessage(index, "You have learned all that you can learn!", (int)ColorName.BrightRed);
                }
            }
            else
            {
                Network.PlayerMessage(index, "You must be level " + i + " to learn this skill.", (int)ColorName.Yellow);
            }
        }
        else
        {
            Network.PlayerMessage(index, string.Format("Only {0} can use this skill.", GameLogic.CheckGrammar(Job.Instance[Skill.Instance[n].JobReq].Name, 1)), (int)ColorName.BrightRed);
        }
    }

    public void OnMap(int index)
    {
        byte[] data;
        int map = GetMap(index);

        // Send all Players on current map to index
        foreach (var Player in PlayerService.Instance.Players)
        {
            if (IsPlaying(Player.Id))
            {
                if (Player.Id != index)
                {
                    if (GetMap(Player.Id) == map)
                    {
                        data = GetPlayerDataPacket(Player.Id);
                        PlayerService.Instance.SendDataTo(index, data);
                        PlayerXYTo(index, Player.Id);
                        Network.MapEquipmentTo(index, Player.Id);
                    }
                }
            }
        }

        EventLogic.SpawnMapEventsFor(index, GetMap(index));

        // Send index's Player data to everyone on the map including himself
        data = GetPlayerDataPacket(index);
        NetworkConfig.SendDataToMap(map, data);
        PlayerXYToMap(index);
        Network.MapEquipment(index);
        Network.Vitals(index);
        
        // Send map animations
        for (int x = 0; x < Server.Map.Instance[map].MaxX; x++)
        {
            for (int y = 0; y < Server.Map.Instance[map].MaxY; y++)
            {
                if (Server.Map.Instance[map].Tile[x, y].Type == TileType.Animation)
                {
                    Network.UpdateAnimationTo(index, Server.Map.Instance[map].Tile[x, y].Data1);
                    Network.PlayAnimationTo(index, Server.Map.Instance[map].Tile[x, y].Data1, x, y, 0, -1);
                }
                else if (Server.Map.Instance[map].Tile[x, y].Type2 == TileType.Animation)
                {
                    Network.UpdateAnimationTo(index, Server.Map.Instance[map].Tile[x, y].Data1_2);
                    Network.PlayAnimationTo(index, Server.Map.Instance[map].Tile[x, y].Data1_2, x, y, 0, -1);
                }
            }
        }
    }

    public void LeaveMap(int index, int map)
    {

    }

    public void OnLeave(int index)
    {

    }

    public void OnDeath(int index)
    {
        // Set HP to nothing
        SetVital(index, Core.Globals.Vital.Health, 0);

        // Restore vitals
        var vitalCount = System.Enum.GetValues(typeof(Vital)).Length;
        for (int i = 0, count = vitalCount; i < count; i++)
            SetVital(index, (Vital)i, GetPlayerMaxVital(index, (Vital)i));

        Vitals(index);

        // If the Player the attacker killed was a pk then take it away
        if (GetPk(index))
        {
            SetPk(index, false);
        }

        var instance = Server.Map.Instance[GetMap(index)];

        // Warp Player away
        SetDir(index, (byte)Direction.Down);
        Server.Player.Instance[index].Dead = false;
        Server.Player.Instance[index].DeathTimer = 0;
        PlayerDeath(index, Server.Player.Instance[index].DeathTimer);

        // clear targets
        Data.TempPlayer[index].Target = -1;
        Data.TempPlayer[index].TargetType = 0;

        foreach (var Player in PlayerService.Instance.Players)
        {
            if (IsPlaying(Player.Id))
            {
                if (GetMap(Player.Id) == GetMap(index))
                {
                    if (Data.TempPlayer[Player.Id].TargetType == (byte)TargetType.Player & Data.TempPlayer[Player.Id].Target == index)
                    {
                        Data.TempPlayer[Player.Id].TargetType = 0;
                        Data.TempPlayer[Player.Id].Target = -1;
                    }
                }
            }
        }

        for (int i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
        {
            if (MapNpc.Instance[GetMap(index), i].TargetType == (byte)TargetType.Player & MapNpc.Instance[GetMap(index), i].Target == index)
            {
                MapNpc.Instance[GetMap(index), i].TargetType = 0;
                MapNpc.Instance[GetMap(index), i].Target = -1;
            }
        }

        // to the bootmap if it is set
        if (instance.BootMap > 0)
        {
            OnWarp(index, instance.BootMap, instance.BootX, instance.BootY, (int)Direction.Down);
        }
        else
        {
            OnWarp(index, Job.Instance[GetJob(index)].StartMap, Job.Instance[GetJob(index)].StartX, Job.Instance[GetJob(index)].StartY, (int)Direction.Down);
        }
    }

    // Initiate a Player skill cast (buffer or instant). Called from Cast with (PlayerIndex, skillSlot)
    public void OnCast(int player, int skillSlot)
    {
        // Basic validations
        if (player < 0 || player >= Server.Player.Instance.Count) return;
        if (!IsPlaying(player)) return;
        if (skillSlot < 0 || skillSlot >= Server.Player.Instance[player].Skill.Length) return;

        // Already casting something
        if (Data.TempPlayer[player].SkillBuffer >= 0) return;

        // Stunned
        if (Data.TempPlayer[player].StunDuration > 0) return;

        int skill = Server.Player.Instance[player].Skill[skillSlot].Num;
        if (skill < 0 || skill >= Skill.Instance.Count) return;

        // Cooldown check
        long now = General.GetTime();
        if (Data.TempPlayer[player].SkillCd != null && skillSlot < Data.TempPlayer[player].SkillCd.Length)
        {
            var cdExpiry = Data.TempPlayer[player].SkillCd[skillSlot];
            if (cdExpiry > now)
            {
                Network.PlayerMessage(player, "That skill is still cooling down.", (int)ColorName.BrightRed);
                return;
            }
        }

        // Mana check (only deduct on finalize) - ensure sufficient now
        if (GetVital(player, Core.Globals.Vital.Mana) < Skill.Instance[skill].MpCost)
        {
            Network.PlayerMessage(player, "Not enough mana.", (int)ColorName.BrightRed);
            return;
        }

        if (GetVital(player, Core.Globals.Vital.Stamina) < Skill.Instance[skill].SpCost)
        {
            Network.PlayerMessage(player, "Not enough stamina.", (int)ColorName.BrightRed);
            return;
        }

        // Moral / map rule check
        var map = GetMap(player);
        if (map < 0 || map >= Server.Map.Instance.Count) return;

        var moral = Server.Map.Instance[map].Moral;
        if (moral >= 0 && !Moral.Instance[moral].CanCast)
        {
            Network.PlayerMessage(player, "You cannot cast here.", (int)ColorName.BrightRed);
            return;
        }

        // Always buffer, even for instant-cast skills. If castTime == 0 we treat it as 1 tick latency (next 25ms cycle) for consistency.
        int effectiveCastTime = Skill.Instance[skill].CastTime;
        if (effectiveCastTime < 0) effectiveCastTime = 0;

        // Buffer the skill for later completion by server loop
        Data.TempPlayer[player].SkillBuffer = skillSlot;
        Data.TempPlayer[player].SkillBufferTimer = (int)now;
        Network.StartSkillBuffer(player, skillSlot, effectiveCastTime);
    }

    public int OnKill(int index)
    {
        if (!Moral.Instance[Server.Map.Instance[GetMap(index)].Moral].LoseExp)
            return 0;

        int exp = GetExp(index) / 3;

        if (exp == 0)
        {
            Network.PlayerMessage(index, "You've lost no experience.", (int)ColorName.BrightGreen);
        }
        else
        {
            Network.Experience(index);
            Network.PlayerMessage(index, string.Format("You've lost {0} experience.", exp), (int)ColorName.BrightRed);
        }

        return exp;
    }

    public void OnTrain(int index, int tmpStat)
    {
        // make sure their stats are not maxed
        if (GetRawStat(index, (Stat)tmpStat) >= Core.Globals.Variables.MaxStats)
        {
            Network.PlayerMessage(index, "You cannot spend any more points on that stat.", (int)ColorName.BrightRed);
            return;
        }

        // increment stat
        SetStat(index, (Stat)tmpStat, GetRawStat(index, (Stat)tmpStat) + 1);

        // decrement points
        SetPoints(index, GetPoints(index) - 1);

        // send Player new data
        Network.PlayerData(index);
    }

    public void OnMove(int index)
    {

    }

    public void OnLevel(int index)
    {
        int count = 0;
        while (GetExp(index) >= GetPlayerNextLevel(index))
        {
            var expRollover = GetExp(index) - GetPlayerNextLevel(index);
            SetLevel(index, GetLevel(index) + 1);
            int points = GetPlayerPointsPerLevel(index);
            points += ((int)Math.Floor((decimal)GetStat(index, Stat.Luck) / 10));
            SetPoints(index, GetPoints(index) + points);
            SetExp(index, expRollover);
            count += 1;
        }

        if (count > 0)
        {
            if (count == 1)
            {
                // singular
                Network.GlobalMessage(GetName(index) + " has gained " + count + " level!");
            }
            else
            {
                // plural
                Network.GlobalMessage(GetName(index) + " has gained " + count + " levels!");
            }
            Network.ActionMessage(GetMap(index), "Level Up", (int)ColorName.Yellow, 1, GetX(index) * Variables.TileSize, GetY(index) * Variables.TileSize);
            Network.Experience(index);
            Network.PlayerData(index);
        }
    }

    public void RegenVitals()
    {
        long now = General.GetTime();
        bool doNpc = now - _lastNpcRegen >= NpcRegenInterval;
        bool doPlayer = now - _lastPlayerRegen >= PlayerRegenInterval;
        if (!doNpc && !doPlayer) return;

        if (doNpc) _lastNpcRegen = now;
        if (doPlayer) _lastPlayerRegen = now;

        if (doNpc)
        {
            foreach (var e in Core.Globals.Entity.Instances)
            {
                if (e == null) continue;
                if (e.Type != Core.Globals.Entity.EntityType.Npc) continue;
                if (e.Num < 0) continue;
                if (e.Vital == null) continue;
                int maxHp = GameLogic.GetNpcMaxVital(e.Num, Core.Globals.Vital.Health);
                int curHp = e.Vital[(byte)Core.Globals.Vital.Health];
                if (curHp > 0 && curHp < maxHp)
                {
                    int amount = Math.Max(1, Npc.Instance[e.Num].Stat[(byte)Stat.Vitality] / 2);
                    e.Vital[(byte)Core.Globals.Vital.Health] = Math.Min(maxHp, curHp + amount);
                    Network.MapNpcVitals(e.Map, (byte)Core.Globals.Entity.Index(e));
                }
                int maxMana = GameLogic.GetNpcMaxVital(e.Num, Core.Globals.Vital.Mana);
                if (maxMana > 0)
                {
                    int curMana = e.Vital[(byte)Core.Globals.Vital.Mana];
                    if (curMana < maxMana)
                    {
                        int amount = Math.Max(1, Npc.Instance[e.Num].Stat[(byte)Stat.Intelligence] / 2);
                        e.Vital[(byte)Core.Globals.Vital.Mana] = Math.Min(maxMana, curMana + amount);
                        Network.MapNpcVitals(e.Map, (byte)Core.Globals.Entity.Index(e));
                    }
                }

                int maxStam = GameLogic.GetNpcMaxVital(e.Num, Core.Globals.Vital.Stamina);
                if (maxStam > 0)
                {
                    int curStam = e.Vital[(byte)Core.Globals.Vital.Stamina];
                    if (curStam < maxStam)
                    {
                        int amount = Math.Max(1, Npc.Instance[e.Num].Stat[(byte)Stat.Spirit] / 2);
                        e.Vital[(byte)Core.Globals.Vital.Stamina] = (int)Math.Min(maxStam, curStam + amount);
                        Network.MapNpcVitals(e.Map, (byte)Core.Globals.Entity.Index(e));
                    }
                }
            }
        }

        if (doPlayer)
        {
            foreach (var p in PlayerService.Instance.Players)
            {
                int id = p.Id;
                if (!NetworkConfig.IsPlaying(id)) continue;
                int hpMax = GetPlayerMaxVital(id, Core.Globals.Vital.Health);
                int hpCur = GetVital(id, Core.Globals.Vital.Health);
                if (hpMax > 0 && hpCur <= 0 && !Server.Player.Instance[id].Dead)
                {
                    KillPlayerNoAttacker(id);
                }
                else if (hpCur > 0 && hpCur < hpMax && !Server.Player.Instance[id].Dead)
                {
                    int amount = Math.Max(1, GetStat(id, Stat.Vitality) / 2);
                    SetVital(id, Core.Globals.Vital.Health, Math.Min(hpMax, hpCur + amount));
                    Network.Vital(id, Core.Globals.Vital.Health);
                }
                int manaMax = GetPlayerMaxVital(id, Core.Globals.Vital.Mana);
                int manaCur = GetVital(id, Core.Globals.Vital.Mana);
                if (manaCur < manaMax)
                {
                    int amount = Math.Max(1, GetStat(id, Stat.Intelligence) / 2);
                    SetVital(id, Core.Globals.Vital.Mana, Math.Min(manaMax, manaCur + amount));
                    Network.Vital(id, Core.Globals.Vital.Mana);
                }
                int stamMax = GetPlayerMaxVital(id, Core.Globals.Vital.Stamina);
                int stamCur = GetVital(id, Core.Globals.Vital.Stamina);
                if (stamCur < stamMax)
                {
                    int amount = Math.Max(1, GetStat(id, Stat.Spirit) / 2);
                    SetVital(id, Core.Globals.Vital.Stamina, Math.Min(stamMax, stamCur + amount));
                    Network.Vital(id, Core.Globals.Vital.Stamina);
                }
            }
        }
    }

    public void KillPlayerNoAttacker(int playerId, string? deathMessage = null)
    {
        if (!NetworkConfig.IsPlaying(playerId)) return;
        if (Server.Player.Instance[playerId].Dead) return;

        Server.Player.Instance[playerId].Dead = true;
        SetVital(playerId, Core.Globals.Vital.Health, 0);
        Network.Vital(playerId, Core.Globals.Vital.Health);

        ClearTargetsToDeadPlayer(playerId);

        if (!string.IsNullOrWhiteSpace(deathMessage))
        {
            Network.PlayerMessage(playerId, deathMessage, (int)ColorName.BrightRed);
        }

        // Record a per-character respawn deadline.
        var now = General.GetTime();
        Server.Player.Instance[playerId].DeathTimer = DeathSpawnTime;
        Network.PlayerDeath(playerId, DeathSpawnTime);

        System.Threading.Tasks.Task.Run(async () =>
        {
            await System.Threading.Tasks.Task.Delay(DeathSpawnTime);
            if (IsPlaying(playerId) && Server.Player.Instance[playerId].Dead)
            {
                OnDeath(playerId);
            }
        });
    }

    public struct DamageResult
    {
        public int Raw;
        public int Mitigated;
        public bool Block;
        public bool Dodge;
        public bool Parry;
        public bool Crit;
        public int Final => (Dodge || Parry) ? 0 : Mitigated;
    }

    private static void ClearTargetsToDeadPlayer(int deadPlayerId)
    {
        if (deadPlayerId < 0 || deadPlayerId >= Core.Globals.Variables.MaxPlayers)
        {
            return;
        }

        // Clear the dead player's own target.
        if (NetworkConfig.IsPlaying(deadPlayerId))
        {
            Data.TempPlayer[deadPlayerId].TargetType = 0;
            Data.TempPlayer[deadPlayerId].Target = -1;
            Network.Target(deadPlayerId, 0, 0);
        }

        var map = GetMap(deadPlayerId);

        // Clear other players targeting this player.
        foreach (var otherPlayer in PlayerService.Instance.Players)
        {
            var otherId = otherPlayer.Id;
            if (!NetworkConfig.IsPlaying(otherId)) continue;
            if (GetMap(otherId) != map) continue;

            if (Data.TempPlayer[otherId].TargetType == (byte)TargetType.Player && Data.TempPlayer[otherId].Target == deadPlayerId)
            {
                Data.TempPlayer[otherId].TargetType = 0;
                Data.TempPlayer[otherId].Target = -1;
                Network.Target(otherId, 0, 0);
            }
        }

        // Clear NPCs targeting this player.
        if (map >= 0 && map < Core.Globals.Variables.MaxMaps)
        {
            for (var npc = 0; npc < Core.Globals.Variables.MaxMapNpcs; npc++)
            {
                if (MapNpc.Instance[map, npc].TargetType == (byte)TargetType.Player && MapNpc.Instance[map, npc].Target == deadPlayerId)
                {
                    MapNpc.Instance[map, npc].TargetType = 0;
                    MapNpc.Instance[map, npc].Target = -1;

                    MapNpc.Instance[map, npc].Attacking = 0;
                    MapNpc.Instance[map, npc].AttackTimer = 0;
                    MapNpc.Instance[map, npc].SkillBuffer = -1;
                    MapNpc.Instance[map, npc].SkillBufferTimer = 0;
                }
            }
        }
    }

    private void HandleDeath(Entity attacker, Entity target)
    {
        if (target.Type == Entity.EntityType.Player)
        {
            if (target.Id < 0 || target.Id >= Server.Player.Instance.Count) return;
            if (Server.Player.Instance[target.Id].Dead) return;

            Server.Player.Instance[target.Id].Dead = true;
            SetVital(target.Id, Core.Globals.Vital.Health, 0);
            Network.Vital(target.Id, Core.Globals.Vital.Health);

            ClearTargetsToDeadPlayer(target.Id);

            if (Moral.Instance[Server.Map.Instance[GetMap(target.Id)].Moral].DropItems)
            {
                var equipCount = Enum.GetValues(typeof(Equipment)).Length;
                
                // Drop equipment
                for (int i = 0; i < equipCount; i++)
                {
                    if (GetPaperdoll(target.Id, (Equipment)i) >= 0)
                    {
                        Server.MapItem.OnSpawn(GetPaperdoll(target.Id, (Equipment)i), 1, GetMap(target.Id), GetX(target.Id), GetY(target.Id));
                        Network.PlayerMessage(target.Id, "You have dropped your " + Item.Instance[GetPaperdoll(target.Id, (Equipment)i)].Name + " upon death.", (int)ColorName.BrightRed);
                        SetPaperdoll(target.Id, -1, (Equipment)i);
                    }
                }
            }

            // Apply death penalty & get exp lost
            int lost = Server.Player.OnKill(target.Id);

            // Basic attacker reward (if attacker is Player) with party sharing
            if (attacker.Type == Entity.EntityType.Player && attacker.Id != target.Id)
            {
                if (lost > 0)
                {
                    int gain = Math.Max(1, lost);
                    int partyId = Data.TempPlayer[attacker.Id].InParty;
                    int map = GetMap(attacker.Id);
                    if (partyId >= 0)
                    {
                        // Share EXP among party members on the same map
                        ShareExp(partyId, gain, attacker.Id, map);
                    }
                    else
                    {
                        // Solo award
                        SetExp(attacker.Id, GetExp(attacker.Id) + gain);
                        Network.Experience(attacker.Id);
                    }
                    Network.PlayerMessage(attacker.Id, $"You gained {gain} experience for defeating {GetName(target.Id)}.", (int)ColorName.BrightGreen);
                }
            }

            Network.GlobalMessage(GetName(target.Id) + " was slain by " + GetEntityName(attacker) + ".");

            // Record a per-character respawn deadline.
            var now = (int)General.GetTime();
            Server.Player.Instance[target.Id].DeathTimer = DeathSpawnTime;

            // Hide the Player on their client immediately (do not broadcast to map)
            Network.PlayerDeath(target.Id, DeathSpawnTime);

            // After timer expires: perform the actual death warp and then release hold
            System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(DeathSpawnTime);
                if (IsPlaying(target.Id) && Server.Player.Instance[target.Id].Dead)
                {
                    OnDeath(target.Id);
                }
            });

        }
        else if (target.Type == Entity.EntityType.Npc)
        {
            var map = target.Map;
            var id = target.Id;
            if (map >= 0 && map < Core.Globals.Variables.MaxMaps && id >= 0 && id < Core.Globals.Variables.MaxMapNpcs)
            {
                // Loot
            DropNpcLoot(map, id);

                // Mark dead & schedule respawn with a 60-second countdown via action messages
                ref var mapNpc = ref MapNpc.Instance[map, id];
                var deathTimer = DeathSpawnTime;
                var currentTime = General.GetTime();
                
                // Store original NPC number for respawn and set to dead state
                int npc = mapNpc.Num;

                // Death switch/variable (applied to killer if Player)
                if (attacker.Type == Entity.EntityType.Player && npc >= 0 && npc < Npc.Instance.Count)
                {
                    var npcTemplate = Npc.Instance[npc];

                    if (npcTemplate.DeathSwitch > 0 && npcTemplate.DeathSwitch < Core.Globals.Variables.MaxSwitches)
                    {
                        var val = npcTemplate.DeathSwitchValue;
                        if (val < 0) val = 0;
                        Server.Player.Instance[attacker.Id].Switches[npcTemplate.DeathSwitch] = (byte)Math.Clamp(val, 0, byte.MaxValue);
                    }

                    if (npcTemplate.DeathVariable > 0 && npcTemplate.DeathVariable < Core.Globals.Variables.MaxVariables)
                    {
                        var add = npcTemplate.DeathVariableValue;
                        Server.Player.Instance[attacker.Id].Variables[npcTemplate.DeathVariable] += add;
                    }
                }

                // Keep the NPC number so the corpse stays visible client-side.
                // Use DeathTimer as an absolute expiry timestamp (ms) on the server.
                mapNpc.Vital[(int)Core.Globals.Vital.Health] = 0;
                mapNpc.Attacking = 0;
                mapNpc.AttackTimer = 0;
                mapNpc.Moving = 0;
                mapNpc.Steps = 0;
                mapNpc.DeathTimer = currentTime + deathTimer;
                mapNpc.SpawnWait = mapNpc.DeathTimer; // respawn time

                Network.NpcDeath(map, npc, deathTimer);

                // clear this npc's own target
                mapNpc.Target = -1;
                mapNpc.TargetType = 0;

                for (int i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
                {
                    if (MapNpc.Instance[map, i].TargetType == (byte)TargetType.Npc && MapNpc.Instance[map, i].Target == npc)
                    {
                        MapNpc.Instance[map, i].TargetType = 0;
                        MapNpc.Instance[map, i].Target = -1;
                    }
                }

                foreach (var Player in PlayerService.Instance.Players)
                {
                    if (IsPlaying(Player.Id))
                    {
                        if (GetMap(Player.Id) == map)
                        {
                            if (Data.TempPlayer[Player.Id].TargetType == (byte)TargetType.Npc && Data.TempPlayer[Player.Id].Target == npc)
                            {
                                Data.TempPlayer[Player.Id].TargetType = 0;
                                Data.TempPlayer[Player.Id].Target = -1;
                                Network.Target(Player.Id, 0, 0);
                            }
                        }
                    }
                }

                // Grant exp to attacker if Player (share with party if applicable)
                if (attacker.Type == Entity.EntityType.Player && mapNpc.Num == -1)
                {
                    int baseExp = 0;
                    if (npc >= 0 && npc < Npc.Instance.Count)
                    {
                        baseExp = Npc.Instance[npc].Experience; // NPC base EXP
                    }
                    if (baseExp > 0)
                    {
                        int partyId = Data.TempPlayer[attacker.Id].InParty;
                        if (partyId >= 0)
                        {
                            // Share EXP among eligible party members on the same map
                            ShareExp(partyId, baseExp, attacker.Id, map);
                        }
                        else
                        {
                            // Solo: award directly
                            SetExp(attacker.Id, GetExp(attacker.Id) + baseExp);
                            Network.Experience(attacker.Id);
                        }
                        Network.PlayerMessage(attacker.Id, $"You gained {baseExp} experience.", (int)ColorName.BrightGreen);
                    }
                }
            }
        }
    }

    private string GetEntityName(Entity e)
    {
        if (e.Type == Entity.EntityType.Player)
        {
            return GetName(e.Id);
        }
        if (e.Type == Entity.EntityType.Npc)
        {
            return (e.Num >= 0 && e.Num < Npc.Instance.Count) ? Npc.Instance[e.Num].Name : "NPC";
        }
        return "Entity";
    }

    private bool IsSkillRanged(int? skillId)
    {
        if (!skillId.HasValue) return false;
        var id = skillId.Value;
        if (id < 0 || id >= Skill.Instance.Count) return false;
        return Skill.Instance[id].Range > 1; // simple heuristic
    }

    public bool AttemptAttack(Entity attacker, Entity target, int? skillId = null, int? damage = null, bool? allowOutOfRange = false)
    {
        if (attacker == null || target == null) return false;
        if (attacker.Map != target.Map) return false;
        if (!IsAlive(attacker) || !IsAlive(target)) return false;
        if (!IsSkillRanged(skillId) && !IsInMeleeRange(attacker, target) && allowOutOfRange == false) return false;

        if (attacker.Type == Entity.EntityType.Player && target.Type == Entity.EntityType.Player && !Moral.Instance[Server.Map.Instance[attacker.Map].Moral].CanPk)
        {
            return false; // PvP not allowed on this map
        }
        var now = General.GetTime();
        var cd = GetAttackSpeed(attacker, skillId);
        if (attacker.AttackTimer + cd > now) return false;

        var dmg = CalculateDamage(attacker, target, skillId, damage);
        var killed = ApplyDamageExtended(attacker, target, dmg, skillId);

        // Trigger skill common event on successful damage skills when the hit actually lands.
        // This also covers projectile skills since projectile impact uses AttemptAttack(..., skillId).
        if (attacker.Type == Entity.EntityType.Player && skillId.HasValue)
        {
            int sid = skillId.Value;
            if (sid >= 0 && sid < Skill.Instance.Count)
            {
                CommonEvent(attacker.Id -1, sid);
            }
        }

        // set cooldown
        attacker.AttackTimer = (int)now; // attacker is a snapshot; we must also update underlying store
        UpdateUnderlyingAttackTimer(attacker, (int)now);
        BroadcastAttack(attacker);

        if (target.Type == Entity.EntityType.Player)
        {
            if (attacker.Type == Entity.EntityType.Player)
            {
                Network.PlayerMessage(target.Id, $"You were hit by {GetName(attacker.Id)} for {dmg.Final} damage.", (int)ColorName.BrightRed);
            }
            else if (attacker.Type == Entity.EntityType.Npc)
            {
                Network.PlayerMessage(target.Id, $"You were hit by {GetEntityName(attacker)} for {dmg.Final} damage.", (int)ColorName.BrightRed);
            }
        }
        else if (target.Type == Entity.EntityType.Npc)
        {
            if (attacker.Type == Entity.EntityType.Player)
            {
                Network.PlayerMessage(attacker.Id, $"You hit {GetEntityName(target)} for {dmg.Final} damage.", (int)ColorName.BrightGreen);
            }
        }

        // If target is an npc and was attacked by Player/npc, make it retaliate (set chase target)
        if (target.Type == Entity.EntityType.Npc && target.Num >= 0)
        {
            // Acquire underlying map npc to set target persistent
            var map = target.Map;
            var npc = target.Id;
            if (map >= 0 && map < Core.Globals.Variables.MaxMaps && npc >= 0 && npc < Core.Globals.Variables.MaxMapNpcs)
            {
                ref var baseNpc = ref MapNpc.Instance[map, npc];
                // Always switch target to the attacker on hit for snappy aggro behavior
                if (attacker.Type == Entity.EntityType.Player)
                {
                    baseNpc.TargetType = (byte)TargetType.Player;
                    baseNpc.Target = attacker.Id;
                    // Also reflect on snapshot target entity so current tick logic can act without waiting for rebuild.
                    target.TargetType = (byte)TargetType.Player; // retaliation engages immediately
                    target.Target = attacker.Id;
                }
                else if (attacker.Type == Entity.EntityType.Npc)
                {
                    baseNpc.TargetType = (byte)TargetType.Npc;
                    baseNpc.Target = attacker.Id; // attacker.Id is map npc slot
                    target.TargetType = (byte)TargetType.Npc;
                    target.Target = attacker.Id;
                }
            }
        }

        // If attacker is an npc with no target set (e.g., guard retaliating) ensure its target is the victim
        if (attacker.Type == Entity.EntityType.Npc && attacker.Num >= 0)
        {
            var map = attacker.Map;
            var index = attacker.Id;
            if (map >= 0 && map < Core.Globals.Variables.MaxMaps && index >= 0 && index < Core.Globals.Variables.MaxMapNpcs)
            {
                ref var npc = ref MapNpc.Instance[map, index];
                if (npc.TargetType == 0)
                {
                    if (target.Type == Entity.EntityType.Player)
                    {
                        npc.TargetType = (byte)TargetType.Player;
                        npc.Target = target.Id;
                    }
                    else if (target.Type == Entity.EntityType.Npc)
                    {
                        npc.TargetType = (byte)TargetType.Npc;
                        npc.Target = target.Id;
                    }
                }
            }
        }

        if (killed)
        {
            HandleDeath(attacker, target);
        }

        // Chain casting on hit if this came from a skill and damage actually landed.
        // If this hit is part of a buffered cast execution, defer chaining until after FinalizeCast().
        if (skillId.HasValue && skillId.Value >= 0 && dmg.Final > 0)
        {
            if (ShouldDeferChainOnHit(attacker, skillId.Value))
            {
                _queuedChainOnHit.Add((skillId.Value, target));
            }
            else
            {
                TryChainOnHit(attacker.Map, attacker, skillId.Value, target);
            }

            ApplyKnockbackIfAny(attacker, target, skillId);
        }

        // Death is handled inside ApplyDamage now; killed flag returned for external hooks.
        return true;
    }

    private bool IsAlive(Entity e)
    {
        if (e == null) return false;
        if (e.Type == Entity.EntityType.Player)
        {
            if (e.Id < 0 || e.Id >= Server.Player.Instance.Count) return false;
            return !Server.Player.Instance[e.Id].Dead && GetVital(e.Id, Core.Globals.Vital.Health) > 0;
        }
        if (e.Vital == null) return false;
        return e.Vital[(int)Core.Globals.Vital.Health] > 0;
    }

    private bool IsInMeleeRange(Entity a, Entity b)
    {
        // Tile-based adjacency including diagonals (8-direction) so diagonal melee hits connect.
        var ax = a.X / Constants.TileSize; var ay = a.Y / Constants.TileSize;
        var bx = b.X / Constants.TileSize; var by = b.Y / Constants.TileSize;
        var dx = Math.Abs(ax - bx);
        var dy = Math.Abs(ay - by);
        if (dx == 0 && dy == 0) return false; // same tile not considered melee
        return dx <= 1 && dy <= 1; // any adjacent (including diagonals)
    }

    public bool TryChase(int map, int npcIndex, int sx, int sy, int tx, int ty)
    {
        int dx = tx - sx;
        int dy = ty - sy;
        if (dx == 0 && dy == 0) return false; // already on target tile

        // Try to compute a short path and enqueue it so animation continues between tiles.
        var route = ComputePathAStar(map, sx, sy, tx, ty, maxSteps: 12);
        if (route != null && route.Count > 0)
        {
            Server.MapNpc.SetRoute(map, npcIndex, route);
            Server.MapNpc.TryStartNextStepNow(map, npcIndex);
            return true;
        }

        // Fallback: attempt a single greedy step like before
        Span<byte> dirs = stackalloc byte[4];
        int count = 0;
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            dirs[count++] = (byte)(dx > 0 ? Direction.Right : Direction.Left);
            if (dy != 0) dirs[count++] = (byte)(dy > 0 ? Direction.Down : Direction.Up);
        }
        else
        {
            dirs[count++] = (byte)(dy > 0 ? Direction.Down : Direction.Up);
            if (dx != 0) dirs[count++] = (byte)(dx > 0 ? Direction.Right : Direction.Left);
        }
        for (int i = 0; i < count; i++)
        {
            var d = dirs[i];
            if (Server.MapNpc.CanMove(map, npcIndex, d))
            {
                Server.MapNpc.OnMove(map, npcIndex, d, (int)MovementState.Walking);
                return true;
            }
        }
        return false;
    }

    private static System.Collections.Generic.List<byte>? ComputePathAStar(int map, int sx, int sy, int tx, int ty, int maxSteps)
    {
        int maxX = Server.Map.Instance[map].MaxX;
        int maxY = Server.Map.Instance[map].MaxY;
        if (sx < 0 || sy < 0 || tx < 0 || ty < 0 || sx >= maxX || sy >= maxY || tx >= maxX || ty >= maxY) return null;

        var open = new System.Collections.Generic.SortedSet<(int f,int g,int x,int y)>(System.Collections.Generic.Comparer<(int,int,int,int)>.Create((a,b)=> a.Item1!=b.Item1? a.Item1-b.Item1 : (a.Item2!=b.Item2? a.Item2-b.Item2 : (a.Item3!=b.Item3? a.Item3-b.Item3 : a.Item4-b.Item4))));
            var cameFrom = new System.Collections.Generic.Dictionary<(int,int),(int,int)>();
            var gScore = new System.Collections.Generic.Dictionary<(int,int),int>();
            var start = (sx, sy);
            var goal = (tx, ty);
            gScore[start] = 0;
            open.Add((Heuristic(sx, sy, tx, ty), 0, sx, sy));

        int stepsExplored = 0;
        while (open.Count > 0 && stepsExplored < 2000)
        {
            stepsExplored++;
            var current = open.Min; open.Remove(current);
            int cx = current.Item3, cy = current.Item4, cg = current.Item2;
            if (cx == tx && cy == ty) break;

            foreach (var (nx, ny, dir) in Neighbors(map, cx, cy, maxX, maxY))
            {
                int tentative = cg + 1;
                var key = (nx, ny);
                if (!gScore.TryGetValue(key, out int old) || tentative < old)
                {
                    gScore[key] = tentative;
                    cameFrom[key] = (cx, cy);
                    int f = tentative + Heuristic(nx, ny, tx, ty);
                    open.Add((f, tentative, nx, ny));
                }
            }
        }

        // Reconstruct
        if (!cameFrom.ContainsKey(goal) && !(sx == tx && sy == ty))
            return null;

        var pathTiles = new System.Collections.Generic.List<(int x,int y)>();
        var cur = goal;
        pathTiles.Add(cur);
        while (!cur.Equals(start) && cameFrom.TryGetValue(cur, out var prev))
        {
            cur = prev;
            pathTiles.Add(cur);
            if (pathTiles.Count > 1000) break; // safety
        }
        pathTiles.Reverse();

        // Convert consecutive tiles into directions, cap length
        var dirsOut = new System.Collections.Generic.List<byte>(pathTiles.Count);
        int limit = System.Math.Min(maxSteps, pathTiles.Count - 1);
        for (int i = 0; i < limit; i++)
        {
            var a = i==0 ? (sx, sy) : pathTiles[i];
            var b = pathTiles[i+1];
            int dx2 = b.x - a.Item1;
            int dy2 = b.y - a.Item2;
            byte d;
            if (dx2 == 1) d = (byte)Direction.Right;
            else if (dx2 == -1) d = (byte)Direction.Left;
            else if (dy2 == 1) d = (byte)Direction.Down;
            else d = (byte)Direction.Up;
            dirsOut.Add(d);
        }
        return dirsOut;
    }

    private static int Heuristic(int x1, int y1, int x2, int y2) => System.Math.Abs(x1 - x2) + System.Math.Abs(y1 - y2);

    private static System.Collections.Generic.IEnumerable<(int x,int y,byte dir)> Neighbors(int map, int x, int y, int maxX, int maxY)
    {
        // Cardinal moves
        if (x+1 < maxX && IsTileWalkable(map, x+1, y)) yield return (x+1, y, (byte)Direction.Right);
        if (x-1 >= 0  && IsTileWalkable(map, x-1, y)) yield return (x-1, y, (byte)Direction.Left);
        if (y+1 < maxY && IsTileWalkable(map, x, y+1)) yield return (x, y+1, (byte)Direction.Down);
        if (y-1 >= 0  && IsTileWalkable(map, x, y-1)) yield return (x, y-1, (byte)Direction.Up);
    }

    private static bool IsTileWalkable(int map, int x, int y)
    {
        // Mirror CanNpcMove constraints loosely using tile types only; dynamic collisions are validated at step time.
        var t = Server.Map.Instance[map].Tile[x, y];
        int n = (int)t.Type; int n2 = (int)t.Type2;
        bool ok = (n == (byte)TileType.None || n == (byte)TileType.Item || n == (byte)TileType.NpcSpawn) ||
                  (n2 == (byte)TileType.None || n2 == (byte)TileType.Item || n2 == (byte)TileType.NpcSpawn);
        return ok;
    }

    private int GetAttackSpeed(Entity attacker, int? skillId)
    {
        // Skill cast time gating handled elsewhere; for now consider weapon speed for Players
        if (attacker.Type == Entity.EntityType.Player)
        {
            var weaponId = GetEquippedItemId(attacker, Equipment.Weapon);
            if (weaponId >= 0)
            {
                return Item.Instance[weaponId].AttackSpeed > 0 ? Item.Instance[weaponId].AttackSpeed : BaseAttackSpeed;
            }
        }
        return BaseAttackSpeed;
    }

    // Public helper to enforce per-attacker cooldown based on attack speed.
    // Returns true if cooldown was available and is now consumed; false if still on cooldown.
    public bool TryConsumeAttackCooldown(Entity attacker, int? skillId = null)
    {
        if (attacker == null) return false;
        var now = General.GetTime();
        var cd = GetAttackSpeed(attacker, skillId);
        if (attacker.AttackTimer + cd > now)
        {
            return false;
        }
        attacker.AttackTimer = (int)now;
        UpdateUnderlyingAttackTimer(attacker, (int)now);
        return true;
    }

    private int GetEquippedItemId(Entity Player, Equipment eq)
    {
        if (Player.Paperdoll == null) return -1;
        var slot = (int)eq;
        if (slot < 0 || slot >= Player.Paperdoll.Length) return -1;
        return Player.Paperdoll[slot].Num;
    }

    private DamageResult CalculateDamage(Entity attacker, Entity target, int? skillId = -1, int? damage = null)
    {
        var result = new DamageResult();

        // Base raw damage
        int raw = 0;
        if (attacker.Type == Entity.EntityType.Player)
        {
            raw = GetPlayerDamage(attacker.Id, skillId);
        }
        else // npc
        {
            if (attacker.Num >= 0 && attacker.Num < Npc.Instance.Count)
            {
                raw = Math.Max(1, Npc.Instance[attacker.Num].Damage);
            }
        }

        result.Raw = raw + (damage ?? 0); 

        // Defense / mitigation
        int mitigation = 0;
        if (target.Type == Entity.EntityType.Player)
        {
            mitigation += GetPlayerProtection(target);
        }
        else if (target.Type == Entity.EntityType.Npc)
        {
            if (target.Num >= 0 && target.Num < Npc.Instance.Count)
            {
                mitigation += (int)Npc.Instance[target.Num].Stat[(int)Stat.Vitality / 5];
            }
        }

        var mitigated = Math.Max(0, raw - mitigation);

        if (mitigated > 0)
        {
            result.Dodge = Roll(SafeStat(target, Stat.Luck) / 5);
            if (!result.Dodge) result.Parry = Roll(SafeStat(target, Stat.Strength) / 5);
            if (!result.Dodge && !result.Parry)
            {
                result.Block = HasShield(target) && Roll((SafeStat(target, Stat.Vitality) / 5));
            }
        }

        // Critical only if attack not fully avoided
        if (!result.Dodge && !result.Parry && !result.Block)
        {
            if (skillId >= 0)
            {
                result.Crit = Roll(SafeStat(attacker, Stat.Intelligence) / 2);
                if (result.Crit)
                {
                    mitigated = (int)Math.Round(mitigated * 1.5);
                }
            }
            else
            {
                result.Crit = Roll(SafeStat(attacker, Stat.Strength) / 2);
                if (result.Crit)
                {
                    mitigated = (int)Math.Round(mitigated * 1.5);
                }
            }
        }

        if (result.Block)
        {
            mitigated = 0; // simple full block
        }

        result.Mitigated = mitigated;
        return result;
    }

    private int GetPlayerDamage(int PlayerId, int? skillId)
    {
        int power = 0;
        if (skillId >= 0)
            power = GetStat(PlayerId, Stat.Intelligence) / 2;
        else
            power = GetStat(PlayerId, Stat.Strength) / 2;

        int weaponId = GetPaperdoll(PlayerId, Equipment.Weapon);
        int weaponPower = (weaponId >= 0 && weaponId < Item.Instance.Count) ? Item.Instance[weaponId].Data2 : 0;
        // Keep formula aligned with prior CalculateDamage logic (without RNG)
        int baseDamage = power + weaponPower;
        return Math.Max(0, baseDamage);
    }

    private int GetPlayerDefense(int PlayerId)
    {
        int def = GetStat(PlayerId, Stat.Vitality) / 5;
        return def;
    }

    public int GetPlayerNextLevel(int index)
    {
        int level = GetLevel(index);
        int str = GetStat(index, Stat.Strength);
        int vit = GetStat(index, Stat.Vitality);
        int intellect = GetStat(index, Stat.Intelligence);
        int luck = GetStat(index, Stat.Luck);
        int points = GetPoints(index);

        long next = (long)(level + 1) * (str + vit + intellect + luck + points) * 25L;
        return next > int.MaxValue ? int.MaxValue : (int)Math.Max(0, next);
    }

    private int SafeStat(Entity e, Stat stat)
    {
        if (e.Stat == null) return 0;
        var id = (int)stat;
        if (id < 0 || id >= e.Stat.Length) return 0;
        return e.Stat[id];
    }

    // Adjust vital on an entity (Player or npc). If isHeal=false we subtract (damage). If true we add (heal).
    // amountParam is base amount from skill; for now no scaling besides simple clamp.
    // caster may be used later for threat/aggro or scaling.
    private void AdjustVital(Entity target, Vital vital, int amountParam, bool isHeal, int skillId, int map, Entity caster)
    {
        if (target == null) return;
        if (vital != Core.Globals.Vital.Health && vital != Core.Globals.Vital.Mana && vital != Core.Globals.Vital.Stamina) return; // only support these

        int amount = Math.Max(0, amountParam);
        if (amount == 0) return;

        if (target.Type == Core.Globals.Entity.EntityType.Player)
        {
            int pid = target.Id;
            if (!NetworkConfig.IsPlaying(pid)) return;
            int cur = GetVital(pid, vital);
            int max = GetPlayerMaxVital(pid, vital);
            int newVal;
            if (isHeal)
            {
                if (cur >= max) return; // nothing to do
                newVal = Math.Min(max, cur + amount);
            }
            else
            {
                if (cur <= 0) return; // already dead/empty
                newVal = Math.Max(0, cur - amount);
            }
            SetVital(pid, vital, newVal);
            Network.Vital(pid, vital);
            if (!isHeal && newVal <= 0 && vital == Core.Globals.Vital.Health)
            {
                HandleDeath(caster, target);
            }
        }
        else if (target.Type == Core.Globals.Entity.EntityType.Npc)
        {
            if (target.Map < 0 || target.Map >= Core.Globals.Variables.MaxMaps) return;
            if (target.Id < 0 || target.Id >= Core.Globals.Variables.MaxMapNpcs) return;
            var mapNpc = MapNpc.Instance[target.Map, target.Id];
            if (mapNpc.Num < 0) return;
            int id = (int)vital;
            if (mapNpc.Vital == null || id < 0 || id >= mapNpc.Vital.Length) return;
            int cur = mapNpc.Vital[id];
            int max = GameLogic.GetNpcMaxVital(mapNpc.Num, vital);
            int newVal;
            if (isHeal)
            {
                if (cur >= max) return;
                newVal = Math.Min(max, cur + amount);
            }
            else
            {
                if (cur <= 0) return;
                newVal = Math.Max(0, cur - amount);
            }
            mapNpc.Vital[id] = newVal;
            if (vital == Core.Globals.Vital.Health && !isHeal)
            {
                // show damage amount like existing ApplyDamage does (keep consistent color if possible)
                Network.ActionMessage(target.Map, (isHeal ? "+" : "-") + amount, (int)(isHeal ? ColorName.BrightGreen : ColorName.BrightRed), 1, target.X, target.Y);
            }
            Network.MapNpcVitals(target.Map, (byte)target.Id);
            if (!isHeal && vital == Core.Globals.Vital.Health && newVal <= 0)
            {
                HandleDeath(caster, target);
            }
        }
    }

    public void CastSkill(int map, Entity entity, int skill, int slot)
    {
        if (entity == null) return;
        if (entity.Map != map) return;
        if (skill < 0 || skill >= Skill.Instance.Count) return;

        // Re-check resource costs just before execution (Player or npc could have spent resources meanwhile)
        if (Skill.Instance[skill].MpCost > 0)
        {
            if (entity.Type == Core.Globals.Entity.EntityType.Player)
            {
                if (GetVital(entity.Id, Core.Globals.Vital.Mana) < Skill.Instance[skill].MpCost) return;
            }
            else if (entity.Type == Core.Globals.Entity.EntityType.Npc)
            {
                if (entity.Vital == null || entity.Vital.Length <= (int)Core.Globals.Vital.Mana || entity.Vital[(int)Core.Globals.Vital.Mana] < Skill.Instance[skill].MpCost) return;
            }
        }

        if (Skill.Instance[skill].SpCost > 0)
        {
            if (entity.Type == Core.Globals.Entity.EntityType.Player)
            {
                if (GetVital(entity.Id, Core.Globals.Vital.Stamina) < Skill.Instance[skill].SpCost) return;
            }
            else if (entity.Type == Core.Globals.Entity.EntityType.Npc)
            {
                if (entity.Vital == null || entity.Vital.Length <= (int)Core.Globals.Vital.Stamina || entity.Vital[(int)Core.Globals.Vital.Stamina] < Skill.Instance[skill].SpCost) return;
            }
        }

        Entity resolvedTarget = null;
        if (Skill.Instance[skill].Range > 0)
        {
            resolvedTarget = ResolveTargetEntity(map, entity);
        }

        // Optional cast (wind-up) animation already played when buffering; only play execution anim here.
        bool isProjectile = Skill.Instance[skill].IsProjectile == 1;
        bool isAoE = Skill.Instance[skill].IsAoE;
        int range = Skill.Instance[skill].Range;

        _activeCastChainCaster = (entity.Type, entity.Id, map);
        _activeCastChainSkillId = skill;
        _queuedChainOnHit.Clear();

        try
        {
            if (isProjectile)
            {
                HandleProjectileSkill(map, entity, skill, resolvedTarget);
            }
            else if (range == 0 && !isAoE)
            {
                HandleSelfCastSkill(map, entity, skill);
            }
            else if (range == 0 && isAoE)
            {
                HandleSelfCastAoESkill(map, entity, skill);
            }
            else if (range > 0 && isAoE)
            {
                HandleTargetedAoESkill(map, entity, skill, resolvedTarget);
            }
            else if (range > 0)
            {
                HandleTargetedSkill(map, entity, skill, resolvedTarget);
            }

            // Apply temporary movement speed modifier to the caster (player only).
            // Uses Skill.Duration (seconds) for effect lifetime.
            if (entity.Type == Core.Globals.Entity.EntityType.Player)
            {
                var mult = Skill.Instance[skill].MoveSpeed;
                if (mult <= 0) mult = 1.0f;

                // Only treat this as a timed effect if Duration is positive.
                // (Duration == 0 => no persistent speed effect)
                if (Math.Abs(mult - 1.0f) > 0.0001f && Skill.Instance[skill].Duration > 0)
                {
                    var now = General.GetTime();
                    Data.TempPlayer[entity.Id].MoveSpeedMultiplier = mult;
                    Data.TempPlayer[entity.Id].MoveSpeedMultiplierTimer = now + Skill.Instance[skill].Duration * 1000;
                }
            }

            FinalizeCast(map, entity, skill, slot);

            if (_queuedChainOnHit.Count > 0)
            {
                foreach (var (skillId, target) in _queuedChainOnHit)
                {
                    TryChainOnHit(map, entity, skillId, target);
                }
                _queuedChainOnHit.Clear();
            }
        }
        finally
        {
            _activeCastChainCaster = null;
            _activeCastChainSkillId = -1;
            _queuedChainOnHit.Clear();
        }
    }

    private bool ShouldDeferChainOnHit(Entity attacker, int skillId)
    {
        if (_activeCastChainCaster == null) return false;
        if (_activeCastChainSkillId != skillId) return false;
        var (type, id, map) = _activeCastChainCaster.Value;
        return attacker.Type == type && attacker.Id == id && attacker.Map == map;
    }

    private Entity? ResolveTargetEntity(int map, Entity entity)
    {
        if (entity.TargetType == (byte)TargetType.Player)
        {
            var pid = entity.Target;
            if (NetworkConfig.IsPlaying(pid) && GetMap(pid) == map)
            {
                var e = Core.Globals.Entity.FromPlayer(pid, Server.Player.Instance[pid]);
                e.Map = map;
                return e;
            }
        }
        else if (entity.TargetType == (byte)TargetType.Npc)
        {
            var tid = entity.Target;
            if (tid >= 0 && tid < Core.Globals.Entity.Instances.Count)
            {
                var tEnt = Core.Globals.Entity.Instances[tid];
                if (tEnt != null && tEnt.Type == Core.Globals.Entity.EntityType.Npc && tEnt.Map == map && tEnt.Num >= 0)
                    return tEnt;
            }
        }
        return null;
    }

    private void HandleProjectileSkill(int map, Entity caster, int skillId, Entity? target)
    {
        // Spawn one or more projectiles depending on MultiDirMask. If mask==0, fire in caster's facing or skill.Dir
        var skill = Skill.Instance[skillId];
        int mask = skill.MultiDirMask;
        if (mask == 0)
        {
            if (caster.Type == Core.Globals.Entity.EntityType.Player)
                Server.Projectile.OnShoot(caster.Id, -1, skillId);
            else if (caster.Type == Core.Globals.Entity.EntityType.Npc)
                Server.Projectile.OnNpcProjectile(map, caster.Id, skillId);
            return;
        }

        // For multi-direction: temporarily adjust caster dir per bit and fire once per enabled direction
        Span<byte> dirs = stackalloc byte[] { (byte)Direction.Down, (byte)Direction.Right, (byte)Direction.Left, (byte)Direction.Up, (byte)Direction.DownRight, (byte)Direction.DownLeft, (byte)Direction.UpRight, (byte)Direction.UpLeft };
        byte originalDir = caster.Dir;
        var dirCount = System.Enum.GetValues<Direction>().Length;
        // Multi-direction batch: do not reset cooldown per shot; set once at the end
        for (int i = 0; i < dirCount; i++)
        {
            if ((mask & (1 << i)) == 0) continue;
            caster.Dir = dirs[i];
            if (caster.Type == Core.Globals.Entity.EntityType.Player)
                Server.Projectile.OnShoot(caster.Id, -1, skillId, caster.Dir, suppressCooldown: true);
            else if (caster.Type == Core.Globals.Entity.EntityType.Npc)
                Server.Projectile.OnNpcProjectile(map, caster.Id, skillId, caster.Dir);
        }
        caster.Dir = originalDir;

        // Apply a single cooldown based on this skill's projectile speed
        if (caster.Type == Core.Globals.Entity.EntityType.Player)
        {
            var projNum = Skill.Instance[skillId].Projectile;
            if (projNum >= 0)
            {
                Data.TempPlayer[caster.Id].ProjectileTimer = General.GetTime() + Math.Max(0, Projectile.Instance[projNum].Speed);
            }
        }
    }

    private void HandleSelfCastSkill(int map, Entity caster, int skillId)
    {
        var skill = Skill.Instance[skillId];
        switch (skill.Type)
        {
            case 0: // Damage HP self
                AdjustVital(caster, Core.Globals.Vital.Health, skill.Vital, false, skillId, map, caster);
                break;
            case 1: // Damage MP self
                AdjustVital(caster, Core.Globals.Vital.Mana, skill.Vital, false, skillId, map, caster);
                break;
            case 2: // Heal HP self
                AdjustVital(caster, Core.Globals.Vital.Health, skill.Vital, true, skillId, map, caster);
                break;
            case 3: // Heal MP self
                AdjustVital(caster, Core.Globals.Vital.Mana, skill.Vital, true, skillId, map, caster);
                break;
            case 4: // Warp
                if (skill.Map >= 0 && skill.Map < Server.Map.Instance.Count)
                {
                    int destMap = skill.Map;
                    int destX = skill.X;
                    int destY = skill.Y;
                    if (destMap >= 0 && destMap < Server.Map.Instance.Count &&
                        destX >= 0 && destY >= 0 &&
                        destX < Server.Map.Instance[destMap].MaxX && destY < Server.Map.Instance[destMap].MaxY)
                    {
                        if (caster.Type == Core.Globals.Entity.EntityType.Player)
                        {
                            // Accept any of the 8 directions from the editor; default to Down if out of range
                            byte dir = (skill.Dir <= (byte)Direction.UpLeft) ? skill.Dir : (byte)Direction.Down;
                            OnWarp(caster.Id, destMap, destX, destY, dir);
                            Network.PlayerMessage(caster.Id, "You feel space bend around you...", (int)ColorName.Cyan);
                        }
                    }
                }
                break;
        }
        PlaySkillAnimation(map, caster, skillId, caster);
    }

    private void HandleSelfCastAoESkill(int map, Entity caster, int skillId)
    {
        ApplyAoE(map, caster, skillId, caster.X / Constants.TileSize, caster.Y / Constants.TileSize);
    }

    private void HandleTargetedSkill(int map, Entity caster, int skillId, Entity? target)
    {
        if (target == null) return;
        var s = Skill.Instance[skillId];
        if (s.MultiDirMask == 0)
        {
            AttemptAttack(caster, target, skillId);
        }
        else
        {
            // For multi-direction targeted skills, we resolve separate attacks along up to 8 adjacent directions from caster.
            // Primary target still gets hit once; additionally, attempt in other adjacent directions.
            AttemptAttack(caster, target, skillId);
            Span<(int dx, int dy)> deltas = stackalloc (int, int)[] { (0, 1), (1, 0), (-1, 0), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) };
            var dirCount = System.Enum.GetValues<Direction>().Length;
            for (int i = 0; i < dirCount; i++)
            {
                if ((s.MultiDirMask & (1 << i)) == 0) continue;
                int tx = caster.X / 32 + deltas[i].dx;
                int ty = caster.Y / 32 + deltas[i].dy;
                // Try find another entity on this tile and attack
                var extraTarget = FindEntityAt(map, tx, ty, preferOpponentsOf: caster);
                if (extraTarget != null && (extraTarget.Id != target.Id || extraTarget.Type != target.Type))
                {
                    AttemptAttack(caster, extraTarget, skillId);
                }
            }
        }
        var skill = Skill.Instance[skillId];
        if (skill.Type == 2 || skill.Type == 3)
        {
            var vital = skill.Type == 2 ? Core.Globals.Vital.Health : Core.Globals.Vital.Mana;
            AdjustVital(target, vital, skill.Vital, true, skillId, map, caster);
        }
        PlaySkillAnimation(map, caster, skillId, target);
    }

    private void HandleTargetedAoESkill(int map, Entity caster, int skillId, Entity? target)
    {
        int baseX = (target != null ? target.X : caster.X) / Constants.TileSize;
        int baseY = (target != null ? target.Y : caster.Y) / Constants.TileSize;
        var s = Skill.Instance[skillId];
        if (s.MultiDirMask == 0)
        {
            ApplyAoE(map, caster, skillId, baseX, baseY);
        }
        else
        {
            Span<(int dx,int dy)> deltas = stackalloc (int,int)[] { (0,1),(1,0),(-1,0),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1) };
            var dirCount = System.Enum.GetValues<Direction>().Length;
            for (int i = 0; i < dirCount; i++)
            {
                if ((s.MultiDirMask & (1 << i)) == 0) continue;
                ApplyAoE(map, caster, skillId, baseX + deltas[i].dx, baseY + deltas[i].dy);
            }
        }
    }

    public Entity? FindEntityAt(int map, int tx, int ty, Entity preferOpponentsOf)
    {
        // Players first
        foreach (var p in PlayerService.Instance.Players)
        {
            if (!NetworkConfig.IsPlaying(p.Id)) continue;
            if (GetMap(p.Id) != map) continue;
            if (GetX(p.Id) == tx && GetY(p.Id) == ty)
            {
                var e = Core.Globals.Entity.FromPlayer(p.Id, Server.Player.Instance[p.Id]); e.Map = map; return e;
            }
        }
        // NPCs
        if (map >= 0 && map < Core.Globals.Variables.MaxMaps)
        {
            for (int i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
            {
                var mn = MapNpc.Instance[map, i];
                if (mn.Num < 0) continue;
                if (mn.X == tx && mn.Y == ty)
                {
                    var e = Core.Globals.Entity.FromNpc(i, MapNpc.Instance[map, i]);
                    e.Map = map;
                    return e;
                }
            }
        }
        return null;
    }

    public void ApplyAoE(int map, Entity caster, int skillId, int centerX, int centerY)
    {
        var skill = Skill.Instance[skillId];
        int radius = skill.AoE;
        bool isDamage = skill.Type == 0 || skill.Type == 1;
        bool isHeal = skill.Type == 2 || skill.Type == 3;
        var vital = (skill.Type == 1 || skill.Type == 3) ? Core.Globals.Vital.Mana : Core.Globals.Vital.Health;

        // Players
        foreach (var Player in PlayerService.Instance.Players)
        {
            if (!NetworkConfig.IsPlaying(Player.Id)) continue;
            if (GetMap(Player.Id) != map) continue;
            int px = GetX(Player.Id);
            int py = GetY(Player.Id);
            if (Math.Abs(px - centerX) <= radius && Math.Abs(py - centerY) <= radius)
            {
                var targetEntity = Core.Globals.Entity.FromPlayer(Player.Id, Server.Player.Instance[Player.Id]);
                targetEntity.Map = map;
                if (isDamage) AttemptAttack(caster, targetEntity, skillId);
                if (isHeal) AdjustVital(targetEntity, vital, skill.Vital, true, skillId, map, caster);
                PlaySkillAnimation(map, caster, skillId, targetEntity);
            }
        }

        // NPCs via map data (avoid LINQ)
        if (map >= 0 && map < Core.Globals.Variables.MaxMaps)
        {
            for (int i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
            {
                if (MapNpc.Instance[map, i].Num < 0) continue;
                int nx = MapNpc.Instance[map, i].X / Constants.TileSize;
                int ny = MapNpc.Instance[map, i].Y / Constants.TileSize;
                if (Math.Abs(nx - centerX) <= radius && Math.Abs(ny - centerY) <= radius)
                {
                    var npcEntity = Core.Globals.Entity.FromNpc(i, MapNpc.Instance[map, i]);
                    npcEntity.Map = map;
                    if (isDamage) AttemptAttack(caster, npcEntity, skillId);
                    if (isHeal) AdjustVital(npcEntity, vital, skill.Vital, true, skillId, map, caster);
                    PlaySkillAnimation(map, caster, skillId, npcEntity);
                }
            }
        }
    }

    private void PlaySkillAnimation(int map, Entity caster, int skillId, Entity target)
    {
        int anim = Skill.Instance[skillId].SkillAnim;
        if (anim < 0) return;
        byte tType = (byte)(target.Type == Core.Globals.Entity.EntityType.Player ? TargetType.Player : TargetType.Npc);
        Network.PlayAnimation(map, anim, 0, 0, tType, target.Id);
    }

    private void TryChainOnHit(int map, Entity caster, int skill, Entity target)
    {
        if (skill < 0 || skill >= Skill.Instance.Count) return;
        int chainId = Skill.Instance[skill].ChainOnHitSkillId;
        if (chainId < 0 || chainId >= Skill.Instance.Count) return;
        // For targeted chaining, we can re-use targeted attack semantics by routing HandleTargetedSkill
        // but respect the chain skill's own type definitions.
        var chain = Skill.Instance[chainId];
        if (chain.IsProjectile == 1)
        {
            // Fire projectile(s) from caster using chain skill
            HandleProjectileSkill(map, caster, chainId, target);
        }
        else if (chain.Range == 0 && !chain.IsAoE)
        {
            HandleSelfCastSkill(map, caster, chainId);
        }
        else if (chain.Range == 0 && chain.IsAoE)
        {
            HandleSelfCastAoESkill(map, caster, chainId);
        }
        else if (chain.Range > 0 && chain.IsAoE)
        {
            HandleTargetedAoESkill(map, caster, chainId, target);
        }
        else if (chain.Range > 0)
        {
            HandleTargetedSkill(map, caster, chainId, target);
        }
        // No cooldown or mana additional cost for automated chain to keep it responsive; adjust if needed.
    }

    private void FinalizeCast(int map, Entity caster, int skillId, int PlayerSkillSlot)
    {
        var skill = Skill.Instance[skillId];
        if (skill.MpCost > 0)
        {
            if (caster.Type == Core.Globals.Entity.EntityType.Player)
            {
                int pid = caster.Id;
                int cur = GetVital(pid, Core.Globals.Vital.Mana);
                SetVital(pid, Core.Globals.Vital.Mana, Math.Max(0, cur - skill.MpCost));
                Network.Vital(pid, Core.Globals.Vital.Mana);
            }
            else if (caster.Type == Core.Globals.Entity.EntityType.Npc && caster.Vital != null && caster.Vital.Length > (int)Core.Globals.Vital.Mana)
            {
                caster.Vital[(int)Core.Globals.Vital.Mana] = Math.Max(0, caster.Vital[(int)Core.Globals.Vital.Mana] - skill.MpCost);
            }
        }

        if (skill.SpCost > 0)
        {
            if (caster.Type == Core.Globals.Entity.EntityType.Player)
            {
                int pid = caster.Id;
                int cur = GetVital(pid, Core.Globals.Vital.Stamina);
                SetVital(pid, Core.Globals.Vital.Stamina, Math.Max(0, cur - skill.SpCost));
                Network.Vital(pid, Core.Globals.Vital.Stamina);
            }
            else if (caster.Type == Core.Globals.Entity.EntityType.Npc && caster.Vital != null && caster.Vital.Length > (int)Core.Globals.Vital.Stamina)
            {
                caster.Vital[(int)Core.Globals.Vital.Stamina] = Math.Max(0, caster.Vital[(int)Core.Globals.Vital.Stamina] - skill.SpCost);
            }
        }
        
        if (caster.Type == Core.Globals.Entity.EntityType.Player && PlayerSkillSlot >= 0)
        {
            int pid = caster.Id;
            if (Data.TempPlayer[pid].SkillCd != null && PlayerSkillSlot < Data.TempPlayer[pid].SkillCd.Length)
            {
                Data.TempPlayer[pid].SkillCd[PlayerSkillSlot] = General.GetTime() + skill.CdTime * 1000;
                Network.SkillCooldown(pid, PlayerSkillSlot);
            }
        }
        else if (caster.Type == Core.Globals.Entity.EntityType.Npc)
        {
            // Set NPC cooldown on the slot that matches this skillId
            if (caster.Map >= 0 && caster.Map < Core.Globals.Variables.MaxMaps && caster.Id >= 0 && caster.Id < Core.Globals.Variables.MaxMapNpcs)
            {
                ref var npc = ref MapNpc.Instance[caster.Map, caster.Id];
                var npcTemplate = caster.Num >= 0 && caster.Num < Npc.Instance.Count ? Npc.Instance[caster.Num] : default;
                if (npcTemplate?.Skill != null && npc.SkillCd != null)
                {
                    for (int slot = 0; slot < Script.MaxNpcSkills && slot < npcTemplate.Skill.Length && slot < npc.SkillCd.Length; slot++)
                    {
                        if (npcTemplate.Skill[slot] == skillId)
                        {
                            npc.SkillCd[slot] = General.GetTime() + skill.CdTime * 1000;
                            break;
                        }
                    }
                }
            }
        }

        // Trigger skill common event (like items) after cast resolves on the caster's map
        if (caster.Type == Core.Globals.Entity.EntityType.Player && skill.CommonEventType > 0)
        {
            int pid = caster.Id;
            CommonEvent(pid, -1, skillId);
        }
    }

    private int GetPlayerProtection(Entity entity)
    {
        if (entity.Type != Entity.EntityType.Player || entity.Paperdoll == null) return 0;

        int total = 0;

        for (int i = 0; i < entity.Paperdoll.Length; i++)
        {
            var item = entity.Paperdoll[i].Num;
            if (item >= 0 && item < Item.Instance.Count)
            {
                total += Item.Instance[item].Data2;
            }
        }
        return total + GetPlayerDefense(entity.Id);
    }

    private bool HasShield(Entity e)
    {
        if (e.Type != Entity.EntityType.Player || e.Paperdoll == null) return false;
        var shieldId = GetEquippedItemId(e, Equipment.Shield);
        return shieldId >= 0;
    }

    private bool Roll(int threshold)
    {
        if (threshold <= 0) return false;
        var roll = (int)Math.Round(General.GetRandom.NextDouble(1d, 100d));
        return roll <= threshold;
    }

    private void ApplyDamage(Entity attacker, Entity target, DamageResult dmg, int? skillId)
    {
        // Feedback messages
        var map = attacker.Map;
        var tx = target.X; // tile origin in pixels
        var ty = target.Y;

        if (dmg.Dodge)
        {
            Network.ActionMessage(map, "Dodge!", (int)ColorName.Pink, 1, tx, ty);
            return;
        }
        if (dmg.Parry)
        {
            Network.ActionMessage(map, "Parry!", (int)ColorName.Pink, 1, tx, ty);
            return;
        }
        if (dmg.Block)
        {
            Network.ActionMessage(map, "Block!", (int)ColorName.BrightCyan, 1, tx, ty);
            return;
        }

        if (dmg.Crit)
        {
            Network.ActionMessage(map, "Critical!", (int)ColorName.BrightCyan, 1, attacker.X, attacker.Y);
        }

        var final = dmg.Final;
        if (final <= 0)
        {
            Network.PlayerMessage(attacker.Id, "Your attack does nothing.", (int)ColorName.BrightRed);
            return;
        }

        // Apply
        if (target.Type == Entity.EntityType.Player)
        {
            var current = GetVital(target.Id, Core.Globals.Vital.Health);
            var newHp = Math.Max(0, current - final);
            SetVital(target.Id, Core.Globals.Vital.Health, newHp);
            Network.Vital(target.Id, Core.Globals.Vital.Health);
            Network.ActionMessage(map, "-" + final, (int)ColorName.BrightRed, 1, tx, ty);
        }
        else if (target.Type == Entity.EntityType.Npc)
        {
            if (target.Num >= 0 && target.Num < Npc.Instance.Count)
            {
                var npc = target.Id; // id is map npc index
                var hp = (int)Core.Globals.Vital.Health;
                var current = MapNpc.Instance[map, npc].Vital[hp];
                var newHp = Math.Max(0, current - final);
                MapNpc.Instance[map, npc].Vital[hp] = newHp;
                Network.ActionMessage(map, "-" + final, (int)ColorName.BrightRed, 1, tx, ty);
                if (newHp > 0)
                {
                    // still alive
                    Network.MapNpcVitals(map, (byte)Core.Globals.Entity.Index(target));
                }
            }
        }
    }

    private void UpdateUnderlyingAttackTimer(Entity entity, int newTime)
    {
        if (entity.Type == Entity.EntityType.Player)
        {
        }
        else if (entity.Type == Entity.EntityType.Npc)
        {
            if (entity.Map >= 0 && entity.Map < Core.Globals.Variables.MaxMaps && entity.Id >= 0 && entity.Id < Core.Globals.Variables.MaxMapNpcs)
            {
                MapNpc.Instance[entity.Map, entity.Id].AttackTimer = newTime;
            }
        }
    }

    private void BroadcastAttack(Entity attacker)
    {
        if (attacker.Type == Entity.EntityType.Player)
        {
            Network.PlayerAttack(attacker.Id);
        }
        else if (attacker.Type == Entity.EntityType.Npc)
        {
            Network.NpcAttack(attacker.Map, attacker.Id);
        }
    }

    private bool ApplyDamageExtended(Entity attacker, Entity target, DamageResult dmg, int? skillId)
    {
        // Reuse existing ApplyDamage but capture death result
        int before;
        if (target.Type == Entity.EntityType.Player)
        {
            before = GetVital(target.Id, Core.Globals.Vital.Health);
        }
        else if (target.Type == Entity.EntityType.Npc && target.Map >= 0 && target.Map < Core.Globals.Variables.MaxMaps && target.Id >= 0 && target.Id < Core.Globals.Variables.MaxMapNpcs)
        {
            before = MapNpc.Instance[target.Map, target.Id].Vital[(int)Core.Globals.Vital.Health];
        }
        else
        {
            before = target.Vital != null ? target.Vital[(int)Core.Globals.Vital.Health] : 0;
        }
        ApplyDamage(attacker, target, dmg, skillId);
        int after = target.Type == Entity.EntityType.Player
            ? GetVital(target.Id, Core.Globals.Vital.Health)
            : (target.Vital != null ? target.Vital[(int)Core.Globals.Vital.Health] : 0);
        if (target.Type == Entity.EntityType.Npc && target.Map >= 0 && target.Map < Core.Globals.Variables.MaxMaps && target.Id >= 0 && target.Id < Core.Globals.Variables.MaxMapNpcs)
        {
            after = MapNpc.Instance[target.Map, target.Id].Vital[(int)Core.Globals.Vital.Health];
        }
        return before > 0 && after <= 0;
    }

    private void ApplyKnockbackIfAny(Entity attacker, Entity target, int? skillId)
    {
        if (!skillId.HasValue || skillId.Value < 0 || skillId.Value >= Skill.Instance.Count) return;
        var s = Skill.Instance[skillId.Value];
        if (s.KnockBack != 1 || s.KnockBackTiles <= 0) return;
        int steps = Math.Min(5, Math.Max(1, (int)s.KnockBackTiles));
        int map = attacker.Map;
        int ax = attacker.X / Constants.TileSize, ay = attacker.Y / Constants.TileSize;
        int tx = target.X / Constants.TileSize, ty = target.Y / Constants.TileSize;
        int dx = Math.Sign(tx - ax);
        int dy = Math.Sign(ty - ay);

        // push away from attacker; if same tile or dx/dy zero, derive from attacker facing
        if (dx == 0 && dy == 0)
        {
            switch ((Direction)attacker.Dir)
            {
                case Direction.Up: dy = -1; break;
                case Direction.Down: dy = 1; break;
                case Direction.Left: dx = -1; break;
                case Direction.Right: dx = 1; break;
                case Direction.UpRight: dx = 1; dy = -1; break;
                case Direction.UpLeft: dx = -1; dy = -1; break;
                case Direction.DownRight: dx = 1; dy = 1; break;
                case Direction.DownLeft: dx = -1; dy = 1; break;
            }
        }

        for (int i = 0; i < steps; i++)
        {
            int nx = (target.X / Constants.TileSize) + dx;
            int ny = (target.Y / Constants.TileSize) + dy;
            // Bounds
            if (nx < 0 || ny < 0 || nx >= Server.Map.Instance[map].MaxX || ny >= Server.Map.Instance[map].MaxY) break;
            // Blocked tiles
            bool blocked = Server.Map.Instance[map].Tile[nx, ny].Type == TileType.Blocked || Server.Map.Instance[map].Tile[nx, ny].Type2 == TileType.Blocked;
            if (blocked) break;
            // Prevent collisions with other entities
            bool occ = false;
            if (target.Type == Entity.EntityType.Player)
            {
                foreach (var pid in PlayerService.Instance.PlayerIds)
                {
                    if (pid != target.Id && GetMap(pid) == map && GetX(pid) == nx && GetY(pid) == ny) { occ = true; break; }
                }
                if (!occ)
                {
                    // Move Player by setting raw coordinates
                    SetX(target.Id, nx * Variables.TileSize);
                    SetY(target.Id, ny * Variables.TileSize);
                    Network.PlayerXYToMap(target.Id);
                }
                else break;
            }
            else if (target.Type == Entity.EntityType.Npc)
            {
                // Ensure no other NPC occupying
                for (int mi = 0; mi < Core.Globals.Variables.MaxMapNpcs; mi++)
                {
                    if (mi == target.Id) continue;
                    if (MapNpc.Instance[map, mi].Num >= 0 && MapNpc.Instance[map, mi].X / Variables.TileSize == nx && MapNpc.Instance[map, mi].Y / Variables.TileSize == ny) { occ = true; break; }
                }
                if (!occ)
                {
                    MapNpc.Instance[map, target.Id].X = nx * Variables.TileSize;
                    MapNpc.Instance[map, target.Id].Y = ny * Variables.TileSize;
                    // Notify clients by sending SNpcDir (keeps anim simple) and vitals/position via SMapNpcData on next sync
                    var stopPacket = new Core.Net.PacketWriter(9);
                    stopPacket.WriteEnum(ServerPackets.SNpcDir);
                    stopPacket.WriteInt32(target.Id);
                    stopPacket.WriteByte(MapNpc.Instance[map, target.Id].Dir);
                    NetworkConfig.SendDataToMap(map, stopPacket.GetBytes());
                }
                else break;
            }
        }
    }

    private void DropNpcLoot(int map, int npc)
    {
        if (map < 0 || map >= Core.Globals.Variables.MaxMaps) return;
        ref var mapNpc = ref MapNpc.Instance[map, npc];
        var npcNum = mapNpc.Num;
        if (npcNum < 0 || npcNum >= Npc.Instance.Count) return;
        // Simple single-roll logic similar to legacy: choose one drop slot 0-4
        var slot = General.GetRandom.NextInt(0, Math.Min(5, Npc.Instance[npcNum].DropChance.Length));
        if (slot < 0) return;
        var chance = Npc.Instance[npcNum].DropChance[slot];
        if (chance <= 0) return;
        var roll = General.GetRandom.NextInt(1, chance + 1);
        if (roll == 1)
        {
            var itemId = Npc.Instance[npcNum].DropItem[slot];
            var itemVal = Npc.Instance[npcNum].DropItemValue[slot];
            if (itemId >= 0 && itemId < Item.Instance.Count)
            {
                Server.MapItem.OnSpawn(itemId, itemVal, map, mapNpc.X / Constants.TileSize, mapNpc.Y / Constants.TileSize);
            }
        }
    }

    public int GetPlayerMaxHP(int index)
    {
        int vit = GetStat(index, Stat.Vitality);
        int job = GetJob(index);
        int stat = Job.Instance[job].Stat[(int)Stat.Vitality];
        long val = (long)(1 + (vit / 2) + stat) * 2L;

        return (int)Math.Max(1, Math.Min(int.MaxValue, val));
    }

    public int GetPlayerMaxMP(int index)
    {
        int @int = GetStat(index, Stat.Intelligence);
        int job = GetJob(index);
        int stat = Job.Instance[job].Stat[(int)Stat.Intelligence];
        long val = (1 + (@int / 2) + stat) * 2L;

        return (int)Math.Max(1, Math.Min(int.MaxValue, val));
    }

    public int GetPlayerMaxSP(int index)
    {
        int spirit = GetStat(index, Stat.Spirit);
        int job = GetJob(index);
        int stat = Job.Instance[job].Stat[(int)Stat.Spirit];
        long val = (1 + (spirit / 2) + stat) * 2L;

        return (int)Math.Max(1, Math.Min(int.MaxValue, val));
    }

    public int GetPlayerPointsPerLevel(int index)
    {
        return StatPerLevel;
    }

    public int GetPlayerMaxVital(int index, Vital vital)
    {
        switch (vital)
        {
            case Core.Globals.Vital.Health:
                return GetPlayerMaxHP(index);

            case Core.Globals.Vital.Mana:
                return GetPlayerMaxMP(index);

            case Core.Globals.Vital.Stamina:
                return GetPlayerMaxSP(index);
                
            default:
                return 0;
        }
    }
}