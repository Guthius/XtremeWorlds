using Core;
using Core.Globals;
using CSScripting;
using Microsoft.CodeAnalysis.CSharp;
using Server;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;
using System.Reflection;
using static Core.Globals.Command;
using static Core.Net.Packets;
using static Core.Globals.Type;
using static Server.Animation;
using static Server.Event;
using static Server.Item;
using static Server.Moral;
using static Server.NetworkSend;
using static Server.Npc;
using static Server.Party;
using static Server.Player;
using static Server.Projectile;
using static Server.Resource;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Core.Configurations;
using Server.Game;
using Variables = Core.Globals.Variables;
using EventCommand = Core.Globals.EventCommand;
using Type = Core.Globals.Type;
using Microsoft.Extensions.Logging;

public class Script
{
    // Add a per-player pickup lock
    private static bool[] _isPickingUp = new bool[Variables.MaxPlayers];
    private static bool[] _isUsingItem = new bool[Variables.MaxPlayers];
    private static bool[] _isEquippingItem = new bool[Variables.MaxPlayers];
    private static bool[] _isUnequippingItem = new bool[Variables.MaxPlayers];

    // Timers for periodic regeneration
    private static long _lastNpcRegen;
    private static long _lastPlayerRegen;
    private const int NpcRegenIntervalMs = 10000; // 10 seconds like legacy
    private const int PlayerRegenIntervalMs = 10000; // 10 seconds like legacy
    private const int BaseAttackSpeedMs = 1000; // fallback when no weapon speed
    private const int DeathSpawnTimeMs = 60000; // 1 minute

    private const long ItemSpawnTime = 30000L; // 30 seconds
    private const long ItemDespawnTime = 90000L; // 1:30 seconds

    private const byte StatPerLevel = 5;
    private const byte MaxLevel = 99;

    // Mutable script-driven limits (initialized from engine defaults)
    public static int MaxAnimations = Variables.MaxAnimations;
    public static byte MaxBank = Variables.MaxBank;
    public static byte MaxJobs = Variables.MaxJobs;
    public static byte MaxMorals = Variables.MaxMorals;
    public static byte MaxInv = Variables.MaxInv;
    public static int MaxItems = Variables.MaxItems;
    public static int MaxMaps = Variables.MaxMaps;
    public static byte MaxMapItems = Variables.MaxMapItems;
    public static byte MaxMapNpcs = Variables.MaxMapNpcs;
    public static int MaxNpcs = Variables.MaxNpcs;
    public static byte MaxNpcSkills = Variables.MaxNpcSkills;
    public static int MaxParty = Variables.MaxParty;
    public static int MaxPartyMembers = Variables.MaxPartyMembers;
    public static int MaxPlayers = Variables.MaxPlayers;
    public static byte MaxPlayerSkills = Variables.MaxPlayerSkills;
    public static int MaxResources = Variables.MaxResources;
    public static int MaxShops = Variables.MaxShops;
    public static int MaxSkills = Variables.MaxSkills;
    public static byte MaxTrades = Variables.MaxTrades;
    public static byte NameLength = Variables.NameLength;
    public static byte MaxNameLength = Variables.NameLength;
    public static byte MinNameLength = Variables.MinNameLength;
    public static byte ChatLength = Variables.ChatLength;
    public static byte MaxHotbar = Variables.MaxHotbar;
    public static byte MaxMapx = Variables.MaxMapX;
    public static byte MaxMapy = Variables.MaxMapY;
    public static int MaxProjectiles = Variables.MaxProjectiles;
    public static byte MaxDropItems = Variables.MaxDropItems;
    public static byte MaxStartItems = Variables.MaxStartItems;
    public static byte MaxStartSkills = Variables.MaxStartSkills;
    public static int MaxSwitches = Variables.MaxSwitches;
    public static int MaxVariables = Variables.MaxVariables;
    public static byte MaxPoints = Variables.MaxPoints;
    public static byte MaxChars = Variables.MaxChars;
    public static int ChatLines = Variables.ChatLines;
    public static byte MaxStats = Variables.MaxStats;
    public static byte MaxQuests = Variables.MaxQuests;
    public static int MaxEvents = Variables.MaxEvents;
    public static byte MaxGuilds = Variables.MaxGuilds;
    public static byte MaxEventChoices = Variables.MaxEventChoices;
    public static int TileSize = Variables.TileSize;
    public static int MaxWeatherParticles = Variables.MaxWeatherParticles;
    public static int MaxBackups = Variables.MaxBackups;
    public static byte SaveInterval = Variables.SaveInterval;
    public static int ServerShutdown = Variables.ServerShutdown;
    public static string Welcome = Variables.Welcome;
    public static string Website = Variables.Website;
    
    // Make functions for all max values to be mutable
    public void UpdateMaxValues()
    {
        Variables.MaxAnimations = MaxAnimations;
        Variables.MaxBank = MaxBank;
        Variables.MaxJobs = MaxJobs;
        Variables.MaxMorals = MaxMorals;
        Variables.MaxInv = MaxInv;
        Variables.MaxItems = MaxItems;
        Variables.MaxMaps = MaxMaps;
        Variables.MaxMapItems = MaxMapItems;
        Variables.MaxMapNpcs = MaxMapNpcs;
        Variables.MaxNpcs = MaxNpcs;
        Variables.MaxNpcSkills = MaxNpcSkills;
        Variables.MaxParty = MaxParty;
        Variables.MaxPartyMembers = MaxPartyMembers;
        Variables.MaxPlayers = MaxPlayers;
        Variables.MaxPlayerSkills = MaxPlayerSkills;
        Variables.MaxResources = MaxResources;
        Variables.MaxShops = MaxShops;
        Variables.MaxSkills = MaxSkills;
        Variables.MaxTrades = MaxTrades;
        Variables.NameLength = NameLength;
        Variables.MinNameLength = MinNameLength;
        Variables.ChatLength = ChatLength;
        Variables.MaxHotbar = MaxHotbar;
        Variables.MaxMapX = MaxMapx;
        Variables.MaxMapY = MaxMapy;
        Variables.MaxProjectiles = MaxProjectiles;
        Variables.MaxDropItems = MaxDropItems;
        Variables.MaxStartItems = MaxStartItems;
        Variables.MaxStartSkills = MaxStartSkills;
        Variables.MaxSwitches = MaxSwitches;
        Variables.MaxVariables = MaxVariables;
        Variables.MaxPoints = MaxPoints;
        Variables.MaxChars = MaxChars;
        Variables.ChatLines = ChatLines;
        Variables.MaxStats = MaxStats;
        Variables.MaxQuests = MaxQuests;
        Variables.MaxEvents = MaxEvents;
        Variables.MaxGuilds = MaxGuilds;
        Variables.MaxEventChoices = MaxEventChoices;
        Variables.TileSize = TileSize;
        Variables.MaxWeatherParticles = MaxWeatherParticles;
        Variables.MaxBackups = MaxBackups;
        Variables.SaveInterval = SaveInterval;
        Variables.ServerShutdown = ServerShutdown;
        Variables.Welcome = Welcome;
        Variables.Website = Website;
    }

    public long ItemDespawnTimeMs()
    {
        return ItemDespawnTime;
    }

    public long ItemSpawnTimeMs()
    {
        return ItemSpawnTime;
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

    public void JoinGame(int index)
    {
        // Warp the player to his saved location
        Warp(index, GetPlayerMap(index), GetPlayerX(index), GetPlayerY(index), (byte)Direction.Down, true);

        // Notify everyone that a player has joined the game.
        NetworkSend.GlobalMsg(string.Format("{0} has joined {1}!", GetPlayerName(index), SettingsManager.Instance.GameName));

        // Send all the required game data to the user.
        CheckEquippedItems(index);
        NetworkSend.SendInventory(index);
        NetworkSend.SendWornEquipment(index);
        NetworkSend.SendExp(index);
        NetworkSend.SendHotbar(index);
        NetworkSend.SendPlayerSkills(index);
        NetworkSend.SendStats(index);

        // Send the flag so they know they can start doing stuff
        NetworkSend.SendInGame(index);

        // Send welcome messages
        NetworkSend.SendWelcome(index);
    
        // Use a default bell sound that ships with content; adjust if you prefer another file
        NetworkSend.SendPlaySound(index, "Bell.ogg", GetPlayerX(index), GetPlayerY(index));
    }

    public void MapDropItem(int index, int mapSlot, int invSlot, int amount, int mapNum, Type.Item item, int itemNum)
    {
        // Determine if the item is currency or stackable
        if (item.Type == (byte)ItemCategory.Currency || item.Stackable == 1)
        {
            // Check if dropping more than the player has, drop all if so
            var playerInvValue = GetPlayerInvValue(index, invSlot);
            if (amount >= playerInvValue)
            {
                amount = playerInvValue;
                SetPlayerInv(index, invSlot, -1);
                SetPlayerInvValue(index, invSlot, 0);
            }
            else
            {
                SetPlayerInvValue(index, invSlot, playerInvValue - amount);
            }
            NetworkSend.MapMsg(mapNum, string.Format("{0} has dropped {1} ({2}x).", GetPlayerName(index), GameLogic.CheckGrammar(item.Name), amount));
        }
        else
        {
            // Not a currency or stackable item
            SetPlayerInv(index, invSlot, -1);
            SetPlayerInvValue(index, invSlot, 0);

            NetworkSend.MapMsg(mapNum, string.Format("{0} has dropped {1}.", GetPlayerName(index), GameLogic.CheckGrammar(item.Name)));
        }

        // Send inventory update
        NetworkSend.SendInventoryUpdate(index, invSlot);

        // Spawn the item on the map
        Server.MapItem.Spawn(itemNum, amount, mapNum, GetPlayerX(index), GetPlayerY(index));
    }

    public void MapGetItem(int index, int mapNum, int mapSlot, int invSlot)
    {
        // Prevent double pickup: if already picking up, ignore
        if (_isPickingUp[index])
            return;

        _isPickingUp[index] = true;

        // Set item in player's inventory
        int itemNum = Data.MapItem[mapNum, mapSlot].Num;
        SetPlayerInv(index, invSlot, itemNum);

        string msg;
        var item = Data.Item[itemNum];
        int mapValue = Data.MapItem[mapNum, mapSlot].Value;

        if (item.BindType == 1)
        {
            Data.Player[index].Inv[invSlot].Bound = 1;
        }

        if (item.Type == (byte)ItemCategory.Currency || item.Stackable == 1)
        {
            // For stackable/currency, add the value from the map item (should be 1 for most drops)
            SetPlayerInvValue(index, invSlot, GetPlayerInvValue(index, invSlot) + mapValue);
            msg = mapValue + " " + item.Name;
        }
        else
        {
            // For non-stackable, always set to 1 regardless of map item value
            SetPlayerInvValue(index, invSlot, 1);
            msg = item.Name;
        }

        // Erase item from the map
        Data.MapItem[mapNum, mapSlot].Num = -1;
        Data.MapItem[mapNum, mapSlot].Value = 0;
        NetworkSend.SendMapItemToAll(mapNum, mapSlot);
        NetworkSend.SendInventoryUpdate(index, invSlot);
        NetworkSend.SendActionMsg(GetPlayerMap(index), msg, (int)ColorName.White, (byte)ActionMessageType.Static, GetPlayerX(index) * 32, GetPlayerY(index) * 32);

        // Unlock pickup for this player
        _isPickingUp[index] = false;
    }

    public void UnEquipItem(int index, int itemNum, int eqSlot, int invSlot)
    {
        // Prevent re-entrant unequip actions for this player
        if (_isUnequippingItem[index])
            return;

        _isUnequippingItem[index] = true;
        try
        {
            SetPlayerInv(index, invSlot, Data.Player[index].Equipment[eqSlot].Num);
            Data.Player[index].Inv[invSlot].Bound = Data.Player[index].Equipment[eqSlot].Bound;
            SetPlayerInvValue(index, invSlot, 1);

            NetworkSend.PlayerMsg(index, "You unequip " + GameLogic.CheckGrammar(Data.Item[GetPlayerEquipment(index, (Equipment)eqSlot)].Name), (int)ColorName.Yellow);

            // remove equipment
            SetPlayerEquipment(index, -1, (Equipment)eqSlot);
            NetworkSend.SendWornEquipment(index);
            NetworkSend.SendMapEquipment(index);
            NetworkSend.SendStats(index);
            NetworkSend.SendInventory(index);

            // send vitals
            NetworkSend.SendVitals(index);
        }
        finally
        {
            _isUnequippingItem[index] = false;
        }
    }

    public void UseItem(int index, int itemNum, int invNum)
    {
        // Prevent re-entrant item usage for a single player (e.g., rapid packet spam)
        if (_isUsingItem[index])
            return;

        _isUsingItem[index] = true;
        try
        {            

            var tempdata = new int[Enum.GetValues(typeof(Stat)).Length + 4];
            var tempstr = new string[3];

            // Find out what kind of item it is
            switch (Data.Item[itemNum].Type)
            {
                case (byte)ItemCategory.Equipment:
                    {
                        EquipItem(index, itemNum, invNum);
                        break;
                    }

                case (byte)ItemCategory.Consumable:
                    {
                        switch (Data.Item[itemNum].SubType)
                        {
                            case (byte)ConsumableEffect.RestoresHealth:
                                {
                                    NetworkSend.SendActionMsg(GetPlayerMap(index), "+" + Data.Item[itemNum].Data1, (int)ColorName.BrightGreen, (byte)ActionMessageType.Scroll, GetPlayerX(index) * 32, GetPlayerY(index) * 32);
                                    NetworkSend.SendAnimation(GetPlayerMap(index), Data.Item[itemNum].Animation, 0, 0, (byte)TargetType.Player, index);
                                    SetPlayerVital(index, Vital.Health, GetPlayerVital(index, Vital.Health) + Data.Item[itemNum].Data1);
                                    TakeInv(index, itemNum, 1);
                                    NetworkSend.SendVital(index, Vital.Health);
                                    break;
                                }

                            case (byte)ConsumableEffect.RestoresMana:
                                {
                                    NetworkSend.SendActionMsg(GetPlayerMap(index), "+" + Data.Item[itemNum].Data1, (int)ColorName.BrightBlue, (byte)ActionMessageType.Scroll, GetPlayerX(index) * 32, GetPlayerY(index) * 32);
                                    NetworkSend.SendAnimation(GetPlayerMap(index), Data.Item[itemNum].Animation, 0, 0, (byte)TargetType.Player, index);
                                    SetPlayerVital(index, Vital.Stamina, GetPlayerVital(index, Vital.Stamina) + Data.Item[itemNum].Data1);
                                    TakeInv(index, itemNum, 1);
                                    NetworkSend.SendVital(index, Vital.Stamina);
                                    break;
                                }

                            case (byte)ConsumableEffect.RestoresStamina:
                                {
                                    NetworkSend.SendAnimation(GetPlayerMap(index), Data.Item[itemNum].Animation, 0, 0, (byte)TargetType.Player, index);
                                    SetPlayerVital(index, Vital.Stamina, GetPlayerVital(index, Vital.Stamina) + Data.Item[itemNum].Data1);
                                    TakeInv(index, itemNum, 1);
                                    NetworkSend.SendVital(index, Vital.Stamina);
                                    break;
                                }

                            case (byte)ConsumableEffect.GrantsExperience:
                                {
                                    NetworkSend.SendAnimation(GetPlayerMap(index), Data.Item[itemNum].Animation, 0, 0, (byte)TargetType.Player, index);
                                    SetPlayerExp(index, GetPlayerExp(index) + Data.Item[itemNum].Data1);
                                    TakeInv(index, itemNum, 1);
                                    NetworkSend.SendExp(index);
                                    break;
                                }

                        }

                        break;
                    }

                case (byte)ItemCategory.Projectile:
                    {
                        if (Data.Item[itemNum].Ammo >= 0)
                        {
                            if (HasItem(index, Data.Item[itemNum].Ammo) > 0)
                            {
                                TakeInv(index, Data.Item[itemNum].Ammo, 1);
                                Server.Projectile.PlayerFireProjectile(index, -1, itemNum);
                            }
                            else
                            {
                                NetworkSend.PlayerMsg(index, "Out of " + Data.Item[Data.Item[GetPlayerEquipment(index, Equipment.Weapon)].Ammo].Name + "!", (int)ColorName.BrightRed);
                                return;
                            }
                        }
                        else
                        {
                            Server.Projectile.PlayerFireProjectile(index, -1, itemNum);
                            return;
                        }

                        break;
                    }

                case (byte)ItemCategory.Event:
                    {
                        // Trigger item-driven common event using item's SubType/Data1/Data2
                        CommonEvent(index, itemNum);
                        break;
                    }

                case (byte)ItemCategory.Skill:
                    {
                        LearnSkill(index, itemNum);
                        break;
                    }
            }
        }
        finally
        {
            _isUsingItem[index] = false;
        }
    }

    private void CommonEvent(int index, int itemNum, int skillNum = -1)
    {
        int idxType;
        int n, n2;

        if (skillNum >= 0)
        {
            idxType = Data.Skill[skillNum].CommonEventType - 1;
            n = Data.Skill[skillNum].CommonEventData1;
            n2 = Data.Skill[skillNum].CommonEventData2;
        }
        else
        {
            // Item-driven common events directly use the enum as SubType
            idxType = Data.Item[itemNum].SubType;
            n = Data.Item[itemNum].Data1;
            n2 = Data.Item[itemNum].Data2;
        }

        switch (idxType)
        {
            case (byte)CommonEventTrigger.Switch:
                Data.Player[index].Switches[Math.Max(0, n)] = (byte)Math.Max(0, n2); break;
                
            case (byte)CommonEventTrigger.Variable:
                Data.Player[index].Variables[Math.Max(0, n)] = n2; break;

            case (byte)CommonEventTrigger.Key:
                EventLogic.TriggerEvent(index, 1, 0, GetPlayerX(index), GetPlayerY(index)); break;

            case (byte)CommonEventTrigger.Script:
                // Minimal sample custom scripts
                if (n == 0)
                    NetworkSend.PlayerMsg(index, "You feel a strange sensation...", (int)ColorName.BrightCyan);
                else
                    NetworkSend.PlayerMsg(index, "Nothing happens.", (int)ColorName.Yellow);
                break;
            default:
                break;
        }
    }
        
    private void EquipItem(int index, int itemNum, int invNum)
    {
        if (_isEquippingItem[index])
            return;

        _isEquippingItem[index] = true;
        try
        {
            int tempItem = -1;
            int m;
            Equipment eqType = (Equipment)Data.Item[itemNum].SubType;
            if (Data.Item[itemNum].BindType == 2)
            {
                Data.Player[index].Inv[invNum].Bound = 2;
            }

            if (GetPlayerEquipment(index, eqType) >= 0)
            {
                tempItem = GetPlayerEquipment(index, eqType);
            }
            SetPlayerEquipment(index, itemNum, eqType);
            Data.Player[index].Equipment[(byte)eqType].Bound = Data.Player[index].Inv[invNum].Bound;
            NetworkSend.PlayerMsg(index, "You equip " + GameLogic.CheckGrammar(Data.Item[itemNum].Name), (int)ColorName.BrightGreen);
            TakeInv(index, itemNum, 1);
            if (tempItem >= 0)
            {
                m = FindOpenInvSlot(index, tempItem);
                SetPlayerInv(index, m, tempItem);
                SetPlayerInvValue(index, m, 1);
            }
            NetworkSend.SendWornEquipment(index);
            NetworkSend.SendMapEquipment(index);
            NetworkSend.SendStats(index);
            NetworkSend.SendVitals(index);
        }
        finally
        {
            _isEquippingItem[index] = false;
        }
    }

    public void LearnSkill(int index, int itemNum, int skillNum = -1)
    {
        int n;
        int i;

        // Get the skill num
        if (skillNum >= 0)
        {
            n = skillNum;
        }
        else
        {
            n = Data.Item[itemNum].Data1;
        }

        if (n < 0 | n >= Variables.MaxSkills)
            return;

        // Make sure they are the right class
        if (Data.Skill[n].JobReq == GetPlayerJob(index) | Data.Skill[n].JobReq == -1)
        {
            // Make sure they are the right level
            i = Data.Skill[n].LevelReq;

            if (i <= GetPlayerLevel(index))
            {
                i = FindOpenSkill(index);

                // Make sure they have an open skill slot
                if (i >= 0)
                {
                    // Make sure they dont already have the skill
                    if (!HasSkill(index, n))
                    {
                        SetPlayerSkill(index, i, n);
                        if (itemNum >= 0)
                        {
                            NetworkSend.SendAnimation(GetPlayerMap(index), Data.Item[itemNum].Animation, 0, 0, (byte)TargetType.Player, index);
                            TakeInv(index, itemNum, 1);
                        }
                        NetworkSend.PlayerMsg(index, "You study the skill carefully.", (int)ColorName.Yellow);
                        NetworkSend.PlayerMsg(index, "You have learned a new skill!", (int)ColorName.BrightGreen);
                        NetworkSend.SendPlayerSkills(index);
                    }
                    else
                    {
                        NetworkSend.PlayerMsg(index, "You have already learned this skill!", (int)ColorName.BrightRed);
                    }
                }
                else
                {
                    NetworkSend.PlayerMsg(index, "You have learned all that you can learn!", (int)ColorName.BrightRed);
                }
            }
            else
            {
                NetworkSend.PlayerMsg(index, "You must be level " + i + " to learn this skill.", (int)ColorName.Yellow);
            }
        }
        else
        {
            NetworkSend.PlayerMsg(index, string.Format("Only {0} can use this skill.", GameLogic.CheckGrammar(Data.Job[Data.Skill[n].JobReq].Name, 1)), (int)ColorName.BrightRed);
        }
    }

    public void JoinMap(int index)
    {
        byte[] data;
        int mapNum = GetPlayerMap(index);

        // Send all players on current map to index
        foreach (var player in PlayerService.Instance.Players)
        {
            if (IsPlaying(player.Id))
            {
                if (player.Id != index)
                {
                    if (GetPlayerMap(player.Id) == mapNum)
                    {
                        data = GetPlayerDataPacket(player.Id);
                        PlayerService.Instance.SendDataTo(index, data);
                        SendPlayerXYTo(index, player.Id);
                        NetworkSend.SendMapEquipmentTo(index, player.Id);
                    }
                }
            }
        }

        EventLogic.SpawnMapEventsFor(index, GetPlayerMap(index));

        // Send index's player data to everyone on the map including himself
        data = GetPlayerDataPacket(index);
        NetworkConfig.SendDataToMap(mapNum, data);
        SendPlayerXYToMap(index);
        NetworkSend.SendMapEquipment(index);
        NetworkSend.SendVitals(index);
    }

    public void LeaveMap(int index, int mapNum)
    {

    }

    public void LeftGame(int index)
    {

    }

    public void OnPlayerDeath(int index)
    {
        // Set HP to nothing
        SetPlayerVital(index, Vital.Health, 0);

        // Restore vitals
        var count = System.Enum.GetValues(typeof(Vital)).Length;
        for (int i = 0, loopTo = count; i < loopTo; i++)
            SetPlayerVital(index, (Vital)i, GetPlayerMaxVital(index, (Vital)i));

        // If the player the attacker killed was a pk then take it away
        if (GetPlayerPk(index))
        {
            SetPlayerPk(index, false);
        }

        ref var withBlock = ref Data.Map[GetPlayerMap(index)];

        // Warp player away
        SetPlayerDir(index, (byte)Direction.Down);
        Data.Player[index].Dead = false;

        // clear targets
        Data.TempPlayer[index].Target = -1;
        Data.TempPlayer[index].TargetType = 0;

        foreach (var player in PlayerService.Instance.Players)
        {
            if (IsPlaying(player.Id))
            {
                if (GetPlayerMap(player.Id) == GetPlayerMap(index))
                {
                    if (Data.TempPlayer[player.Id].TargetType == (byte)TargetType.Player & Data.TempPlayer[player.Id].Target == index)
                    {
                        Data.TempPlayer[player.Id].TargetType = 0;
                        Data.TempPlayer[player.Id].Target = -1;
                    }
                }
            }
        }

        for (int i = 0; i < Script.MaxMapNpcs; i++)
        {
            if (Data.MapNpc[GetPlayerMap(index)].Npc[i].TargetType == (byte)TargetType.Player & Data.MapNpc[GetPlayerMap(index)].Npc[i].Target == index)
            {
                Data.MapNpc[GetPlayerMap(index)].Npc[i].TargetType = 0;
                Data.MapNpc[GetPlayerMap(index)].Npc[i].Target = -1;
            }
        }

        // to the bootmap if it is set
        if (withBlock.BootMap > 0)
        {
            Warp(index, withBlock.BootMap, withBlock.BootX, withBlock.BootY, (int)Direction.Down);
        }
        else
        {
            Warp(index, Data.Job[GetPlayerJob(index)].StartMap, Data.Job[GetPlayerJob(index)].StartX, Data.Job[GetPlayerJob(index)].StartY, (int)Direction.Down);
        }
    }

    // Initiate a player skill cast (buffer or instant). Called from Packet_Cast with (playerIndex, skillSlot)
    public void BufferSkill(int playerIndex, int skillSlot)
    {
        // Basic validations
        if (playerIndex < 0 || playerIndex >= Data.Player.Length) return;
        if (!IsPlaying(playerIndex)) return;
        if (skillSlot < 0 || skillSlot >= Data.Player[playerIndex].Skill.Length) return;

        // Already casting something
        if (Data.TempPlayer[playerIndex].SkillBuffer >= 0) return;

        // Stunned
        if (Data.TempPlayer[playerIndex].StunDuration > 0) return;

        int skillId = Data.Player[playerIndex].Skill[skillSlot].Num;
        if (skillId < 0 || skillId >= Data.Skill.Length) return;

        ref var skill = ref Data.Skill[skillId];

        // Cooldown check
        long now = General.GetTimeMs();
        if (Data.TempPlayer[playerIndex].SkillCd != null && skillSlot < Data.TempPlayer[playerIndex].SkillCd.Length)
        {
            var cdExpiry = Data.TempPlayer[playerIndex].SkillCd[skillSlot];
            if (cdExpiry > now)
            {
                NetworkSend.PlayerMsg(playerIndex, "That skill is still cooling down.", (int)ColorName.BrightRed);
                return;
            }
        }

        // Mana check (only deduct on finalize) - ensure sufficient now
        if (GetPlayerVital(playerIndex, Vital.Mana) < skill.MpCost)
        {
            NetworkSend.PlayerMsg(playerIndex, "Not enough mana.", (int)ColorName.BrightRed);
            return;
        }

        // Moral / map rule check
        var mapNum = GetPlayerMap(playerIndex);
        if (mapNum < 0 || mapNum >= Data.Map.Length) return;

        var moralId = Data.Map[mapNum].Moral;
        if (moralId >= 0 && !Data.Moral[moralId].CanCast)
        {
            NetworkSend.PlayerMsg(playerIndex, "You cannot cast here.", (int)ColorName.BrightRed);
            return;
        }

        // Always buffer, even for instant-cast skills. If castTime == 0 we treat it as 1 tick latency (next 25ms cycle) for consistency.
        int effectiveCastTime = skill.CastTime;
        if (effectiveCastTime < 0) effectiveCastTime = 0;

        // Buffer the skill for later completion by server loop
        Data.TempPlayer[playerIndex].SkillBuffer = skillSlot;
        Data.TempPlayer[playerIndex].SkillBufferTimer = (int)now;
        NetworkSend.SendStartSkillBuffer(playerIndex, skillSlot, effectiveCastTime);
    }

    public int KillPlayer(int index)
    {
        if (!Data.Moral[Data.Map[GetPlayerMap(index)].Moral].LoseExp)
            return 0;

        int exp = GetPlayerExp(index) / 3;

        if (exp == 0)
        {
            NetworkSend.PlayerMsg(index, "You've lost no experience.", (int)ColorName.BrightGreen);
        }
        else
        {
            NetworkSend.SendExp(index);
            NetworkSend.PlayerMsg(index, string.Format("You've lost {0} experience.", exp), (int)ColorName.BrightRed);
        }

        return exp;
    }

    public void TrainStat(int index, int tmpStat)
    {
        // make sure their stats are not maxed
        if (GetPlayerRawStat(index, (Stat)tmpStat) >= Variables.MaxStats)
        {
            NetworkSend.PlayerMsg(index, "You cannot spend any more points on that stat.", (int)ColorName.BrightRed);
            return;
        }

        // increment stat
        SetPlayerStat(index, (Stat)tmpStat, GetPlayerRawStat(index, (Stat)tmpStat) + 1);

        // decrement points
        SetPlayerPoints(index, GetPlayerPoints(index) - 1);

        // send player new data
        NetworkSend.SendPlayerData(index);
    }

    public void PlayerMove(int index)
    {

    }

    public void UpdateMapAi()
    {
        long tickCount = General.GetTimeMs();
        var entities = Core.Globals.Entity.Instances;

        for (int x = 0; x < entities.Count; x++)
        {
            var entity = entities[x];
            if (entity == null) continue;
            var vitals = entity.Vital; // capture early
            var mapNum = entity.Map;

            // Only process entities that are Npcs
            if (entity.Num < 0) continue;

            // Resolve completed skill buffers for both players and NPCs
            long nowMsBuff = General.GetTimeMs();
            if (entity.Type == Core.Globals.Entity.EntityType.Player)
            {
                int slot = (int)Data.TempPlayer[entity.Id].SkillBuffer;
                if (slot >= 0)
                {
                    int skillId = -1;
                    if (Data.Player[entity.Id].Skill != null && slot < Data.Player[entity.Id].Skill.Length)
                        skillId = Data.Player[entity.Id].Skill[slot].Num;
                    int castMs = (skillId >= 0 && skillId < Data.Skill.Length) ? Data.Skill[skillId].CastTime * 1000 : 0;
                    if (nowMsBuff > Data.TempPlayer[entity.Id].SkillBufferTimer + castMs)
                    {
                        CastSkill(mapNum, entity, slot); // bufferedValue is slot for players
                        // clear buffer
                        Data.TempPlayer[entity.Id].SkillBuffer = -1;
                        Data.TempPlayer[entity.Id].SkillBufferTimer = 0;
                        SendClearSkillBuffer(entity.Id);
                    }
                }
            }
            else if (entity.Type == Core.Globals.Entity.EntityType.Npc)
            {
                int skillId = entity.SkillBuffer; // NPC stores skillId directly
                if (skillId >= 0)
                {
                    int castMs = (skillId < Data.Skill.Length) ? Data.Skill[skillId].CastTime * 1000 : 0;
                    if (nowMsBuff > entity.SkillBufferTimer + castMs)
                    {
                        CastSkill(mapNum, entity, skillId); // bufferedValue is skillId for NPCs
                        // clear snapshot & underlying map npc buffer
                        entity.SkillBuffer = -1;
                        entity.SkillBufferTimer = 0;
                        if (entity.Id >= 0 && entity.Id < Script.MaxMapNpcs && mapNum >= 0 && mapNum < Data.MapNpc.Length)
                        {
                            ref var baseNpc = ref Data.MapNpc[mapNum].Npc[entity.Id];
                            baseNpc.SkillBuffer = -1;
                            baseNpc.SkillBufferTimer = 0;
                        }
                    }
                }
            }
            else
            {
                // ATTACKING ON SIGHT (use tile-based distance; ensure property name consistency)
                if (entity.Behavior == (byte)NpcBehavior.AttackOnSight || entity.Behavior == (byte)NpcBehavior.Guard)
                {
                    // make sure it's not stunned
                    if (!(entity.StunDuration > 0))
                    {
                        foreach (var player in PlayerService.Instance.Players)
                        {
                            if (NetworkConfig.IsPlaying(player.Id))
                            {
                                if (GetPlayerMap(player.Id) == mapNum && entity.TargetType == 0 && GetPlayerAccess(player.Id) <= (byte)AccessLevel.Moderator)
                                {
                                    // Detection range: if NPC template has Range=0, use a sensible default (8 tiles)
                                    int n = entity.Range;
                                    int ex = entity.X / 32;
                                    int ey = entity.Y / 32;
                                    int px = GetPlayerX(player.Id);
                                    int py = GetPlayerY(player.Id);
                                    int distanceX = Math.Abs(ex - px);
                                    int distanceY = Math.Abs(ey - py);

                                    if (distanceX <= n && distanceY <= n)
                                    {
                                        if (entity.Behavior == (byte)NpcBehavior.AttackOnSight || GetPlayerPk(player.Id))
                                        {
                                            if (!string.IsNullOrEmpty(entity.AttackSay))
                                            {
                                                NetworkSend.PlayerMsg(player.Id, GameLogic.CheckGrammar(entity.Name, 1) + " says, '" + entity.AttackSay + "' to you.", (int)ColorName.Yellow);
                                            }
                                            entity.TargetType = (byte)TargetType.Player;
                                            entity.Target = player.Id;
                                            // Persist target into base map data for movement logic
                                            if (entity.Id >= 0 && entity.Id < Script.MaxMapNpcs && mapNum >= 0 && mapNum < Data.MapNpc.Length)
                                            {
                                                ref var mapNpc = ref Data.MapNpc[mapNum].Npc[entity.Id];
                                                mapNpc.TargetType = entity.TargetType;
                                                mapNpc.Target = entity.Target;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Check if target was found for Npc targeting
                        if (entity.TargetType == 0 && entity.Faction > 0)
                        {
                            for (int i = 0; i < entities.Count; i++)
                            {
                                var otherEntity = entities[i];
                                if (otherEntity != null && otherEntity.Num >= 0)
                                {
                                    if (otherEntity.Map != mapNum) continue;
                                    if (ReferenceEquals(otherEntity, entity)) continue;
                                    if ((int)otherEntity.Faction > 0 && otherEntity.Faction != entity.Faction)
                                    {
                                        // Detection range between NPCs (same default behavior)
                                        int n = otherEntity.Range;
                                        int ex = entity.X / 32;
                                        int ey = entity.Y / 32;
                                        int ox = otherEntity.X / 32;
                                        int oy = otherEntity.Y / 32;
                                        int distanceX = Math.Abs(ex - ox);
                                        int distanceY = Math.Abs(ey - oy);

                                        if (distanceX <= n && distanceY <= n && entity.Behavior == (byte)NpcBehavior.AttackOnSight)
                                        {
                                            entity.TargetType = (byte)TargetType.Npc;
                                            entity.Target = i;
                                            if (entity.Id >= 0 && entity.Id < Script.MaxMapNpcs && mapNum >= 0 && mapNum < Data.MapNpc.Length)
                                            {
                                                ref var mapNpc = ref Data.MapNpc[mapNum].Npc[entity.Id];
                                                mapNpc.TargetType = entity.TargetType;
                                                mapNpc.Target = entity.Target;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Attempt attack using new combat system when target acquired
                if (entity != null && entity.Target >= 0)
                {
                    if (entity.TargetType == (byte)TargetType.Player)
                    {
                        var pid = entity.Target;
                        if (NetworkConfig.IsPlaying(pid) && GetPlayerMap(pid) == mapNum)
                        {
                            // Clear target if out of chase range
                            int ex = entity.X / 32;
                            int ey = entity.Y / 32;
                            int px = GetPlayerX(pid);
                            int py = GetPlayerY(pid);
                            // Clear target only if well beyond detection range (leash) to avoid flicker
                            int r = entity.Range;
                            if (Math.Abs(ex - px) > r || Math.Abs(ey - py) > r)
                            {
                                entity.Target = -1;
                                entity.TargetType = 0;
                                // reflect to base map npc if this is an NPC snapshot
                                if (entity.Type == Core.Globals.Entity.EntityType.Npc && entity.Id >= 0 && entity.Id < Script.MaxMapNpcs)
                                {
                                    ref var baseNpcClr = ref Data.MapNpc[mapNum].Npc[entity.Id];
                                    baseNpcClr.TargetType = 0;
                                    baseNpcClr.Target = -1;
                                }
                            }
                            else
                            {
                                var targetEntity = Core.Globals.Entity.FromPlayer(pid, Data.Player[pid]);
                                targetEntity.Map = mapNum;
                                // NPC skills: select a valid skill and cast it directly; otherwise do a basic attack
                                bool didCast = false;
                                if (entity.Type == Core.Globals.Entity.EntityType.Npc && entity.Num >= 0 && entity.Num < Data.Npc.Length)
                                {
                                    var skills = Data.Npc[entity.Num].Skill;
                                    if (skills != null)
                                    {
                                        long nowMs = General.GetTimeMs();
                                        // ex, ey, px, py already calculated above in this scope
                                        int dist = Math.Max(Math.Abs(ex - px), Math.Abs(ey - py));
                                        for (int slot = 0; slot < Script.MaxNpcSkills && slot < skills.Length; slot++)
                                        {
                                            int sid = skills[slot];
                                            if (sid <= 0 || sid >= Data.Skill.Length) continue;
                                            ref var sk = ref Data.Skill[sid];
                                            // Range check (0 range means self or adjacent unless AoE)
                                            bool inRange = sk.Range == 0 ? (sk.IsAoE || dist <= 1) : dist <= sk.Range;
                                            if (!inRange) continue;
                                            // Cooldown check
                                            if (mapNum < 0 || mapNum >= Data.MapNpc.Length || entity.Id < 0 || entity.Id >= Script.MaxMapNpcs) break;
                                            ref var baseNpc = ref Data.MapNpc[mapNum].Npc[entity.Id];
                                            bool cdReady = baseNpc.SkillCd == null || slot >= baseNpc.SkillCd.Length || baseNpc.SkillCd[slot] <= nowMs;
                                            if (!cdReady) continue;
                                            // Mana check
                                            if (entity.Vital == null || entity.Vital.Length <= (int)Vital.Mana || entity.Vital[(int)Vital.Mana] < sk.MpCost) continue;
                                            // Cast immediately using entity-centric casting
                                            CastSkill(mapNum, entity, sid);
                                            didCast = true;
                                            break;
                                        }
                                    }
                                }
                                
                                if (!didCast)
                                {
                                    AttemptAttack(entity, targetEntity);
                                }
                            }
                        }
                        else
                        {
                            entity.Target = -1;
                            entity.TargetType = 0;
                        }
                    }
                    else if (entity.TargetType == (byte)TargetType.Npc)
                    {
                        var idx = entity.Target;
                        if (idx >= 0 && idx < entities.Count)
                        {
                            var targetEntity = entities[idx];
                            if (targetEntity != null && targetEntity.Type == Core.Globals.Entity.EntityType.Npc && targetEntity.Map == mapNum && targetEntity.Num >= 0)
                            {
                                // Clear target if out of chase range
                                int ex = entity.X / 32;
                                int ey = entity.Y / 32;
                                int tx = targetEntity.X / 32;
                                int ty = targetEntity.Y / 32;
                                int r = entity.Range;
                                if (Math.Abs(ex - tx) > r || Math.Abs(ey - ty) > r)
                                {
                                    entity.Target = -1;
                                    entity.TargetType = 0;
                                    if (entity.Type == Core.Globals.Entity.EntityType.Npc && entity.Id >= 0 && entity.Id < Script.MaxMapNpcs)
                                    {
                                        ref var baseNpc = ref Data.MapNpc[mapNum].Npc[entity.Id];
                                        baseNpc.TargetType = 0;
                                        baseNpc.Target = -1;
                                    }
                                }
                                else
                                {
                                    // NPC skills: select a valid skill and cast it directly; otherwise do a basic attack
                                    bool didCast2 = false;
                                    if (entity.Type == Core.Globals.Entity.EntityType.Npc && entity.Num >= 0 && entity.Num < Data.Npc.Length)
                                    {
                                        var skills2 = Data.Npc[entity.Num].Skill;
                                        if (skills2 != null)
                                        {
                                            long nowMs2 = General.GetTimeMs();
                                            // ex, ey, tx, ty already calculated above in this scope
                                            int dist2 = Math.Max(Math.Abs(ex - tx), Math.Abs(ey - ty));
                                            for (int slot2 = 0; slot2 < Script.MaxNpcSkills && slot2 < skills2.Length; slot2++)
                                            {
                                                int sid2 = skills2[slot2];
                                                if (sid2 <= 0 || sid2 >= Data.Skill.Length) continue;
                                                ref var sk2 = ref Data.Skill[sid2];
                                                bool inRange2 = sk2.Range == 0 ? (sk2.IsAoE || dist2 <= 1) : dist2 <= sk2.Range;
                                                if (!inRange2) continue;
                                                if (mapNum < 0 || mapNum >= Data.MapNpc.Length || entity.Id < 0 || entity.Id >= Script.MaxMapNpcs) break;
                                                ref var baseNpc2 = ref Data.MapNpc[mapNum].Npc[entity.Id];
                                                bool cdReady2 = baseNpc2.SkillCd == null || slot2 >= baseNpc2.SkillCd.Length || baseNpc2.SkillCd[slot2] <= nowMs2;
                                                if (!cdReady2) continue;
                                                if (entity.Vital == null || entity.Vital.Length <= (int)Vital.Mana || entity.Vital[(int)Vital.Mana] < sk2.MpCost) continue;
                                                CastSkill(mapNum, entity, sid2);
                                                didCast2 = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (!didCast2)
                                    {
                                        AttemptAttack(entity, targetEntity);
                                    }
                                }
                            }
                            else
                            {
                                entity.Target = -1;
                                entity.TargetType = 0;
                            }
                        }
                        else
                        {
                            entity.Target = -1;
                            entity.TargetType = 0;
                        }
                    }
                }

                // Simplified death/spawn handling (entity is non-null here)
                #pragma warning disable CS8602
                if (vitals != null && vitals[(byte)Vital.Health] < 0 && entity.SpawnWait > 0)
                {
                    entity.Num = 0;
                    entity.SpawnWait = General.GetTimeMs();
                    vitals[(byte)Vital.Health] = 0;
                }
#pragma warning restore CS8602

#pragma warning disable CS8602
                // Handle npc respawn logic (no special death window state)
                if (entity.Type == Core.Globals.Entity.EntityType.Npc)
                {
                    if (entity.Num == -1 && entity.SpawnSecs > 0)
                    {
                        // Regular respawn logic
                        if (tickCount > entity.SpawnWait)
                        {
                            Server.Npc.SpawnNpc(x, mapNum);
                        }
                    }
                }
#pragma warning restore CS8602
            }
        }

        // ----- NPC Movement (Chase + Wander) -----
        // Basic tick-based movement: if an NPC has a target and is not adjacent, step toward the target tile.
        // Otherwise perform occasional wandering (random step) if Behavior allows (AttackOnSight / Guard idle roam kept minimal).
        try
        {
            var nowMove = General.GetTimeMs();
            foreach (var e in entities)
            {
                if (e == null) continue;
                if (e.Type != Core.Globals.Entity.EntityType.Npc) continue;
                if (e.Num < 0) continue;
                var npcIndex = e.Id; // Index into Data.MapNpc[map].Npc
                var map = e.Map;
                if (map < 0 || map >= Variables.MaxMaps) continue;
                if (npcIndex < 0 || npcIndex >= Script.MaxMapNpcs) continue;

                ref var baseNpc = ref Data.MapNpc[map].Npc[npcIndex];

                // Skip if stunned
                if (baseNpc.StunDuration > 0) continue;

                // Sync any target assigned on snapshot back to base data if base has none.
                if (baseNpc.TargetType == 0 && e.TargetType != 0)
                {
                    baseNpc.TargetType = e.TargetType;
                    baseNpc.Target = e.Target;
                }

                bool moved = false;

                // If target exists but is out of range, clear it before deciding movement
                if (baseNpc.TargetType == (byte)TargetType.Player && baseNpc.Target >= 0 && NetworkConfig.IsPlaying(baseNpc.Target) && GetPlayerMap(baseNpc.Target) == map)
                {
                    int sxR = baseNpc.X / 32;
                    int syR = baseNpc.Y / 32;
                    int txR = GetPlayerX(baseNpc.Target);
                    int tyR = GetPlayerY(baseNpc.Target);
                    int rR = Math.Max(0, (int)Data.Npc[baseNpc.Num].Range);
                    if (Math.Abs(sxR - txR) > rR || Math.Abs(syR - tyR) > rR)
                    {
                        baseNpc.TargetType = 0;
                        baseNpc.Target = -1;
                    }
                }
                else if (baseNpc.TargetType == (byte)TargetType.Npc && baseNpc.Target >= 0 && baseNpc.Target < Script.MaxMapNpcs)
                {
                    if (Data.MapNpc[map].Npc[baseNpc.Target].Num >= 0)
                    {
                        int sxR = baseNpc.X / 32;
                        int syR = baseNpc.Y / 32;
                        int txR = Data.MapNpc[map].Npc[baseNpc.Target].X / 32;
                        int tyR = Data.MapNpc[map].Npc[baseNpc.Target].Y / 32;
                        int rR = Math.Max(0, (int)Data.Npc[baseNpc.Num].Range);
                        if (Math.Abs(sxR - txR) > rR || Math.Abs(syR - tyR) > rR)
                        {
                            baseNpc.TargetType = 0;
                            baseNpc.Target = -1;
                        }
                    }
                    else
                    {
                        baseNpc.TargetType = 0;
                        baseNpc.Target = -1;
                    }
                }

                // Read target info from persistent npc record
                // Allow player index 0 as a valid target (some arrays are 1-based but be permissive)
                if (baseNpc.TargetType == (byte)TargetType.Player && baseNpc.Target >= 0 && NetworkConfig.IsPlaying(baseNpc.Target) && GetPlayerMap(baseNpc.Target) == map)
                {
                    int sx = baseNpc.X / 32;
                    int sy = baseNpc.Y / 32;
                    int tx = GetPlayerX(baseNpc.Target);
                    int ty = GetPlayerY(baseNpc.Target);
                    moved = TryChase(map, npcIndex, sx, sy, tx, ty);
                }
                else if (baseNpc.TargetType == (byte)TargetType.Npc && baseNpc.Target >= 0 && baseNpc.Target < Script.MaxMapNpcs)
                {
                    // We only have snapshot entities list with indexes unrelated to mapNpc slot ordering for other NPCs; perform tile search.
                    int targetSlot = baseNpc.Target;
                    // Validate the target exists on map
                    if (Data.MapNpc[map].Npc[targetSlot].Num >= 0)
                    {
                        int sx = baseNpc.X / 32;
                        int sy = baseNpc.Y / 32;
                        int tx = Data.MapNpc[map].Npc[targetSlot].X / 32;
                        int ty = Data.MapNpc[map].Npc[targetSlot].Y / 32;
                        moved = TryChase(map, npcIndex, sx, sy, tx, ty);
                    }
                    else
                    {
                        baseNpc.TargetType = 0;
                        baseNpc.Target = -1;
                    }
                }

                // Wander if not moved and no target. AttackOnSight/Guard now also wander albeit less frequently.
                if (!moved && baseNpc.TargetType == 0)
                {
                    // MapNpc struct does not store Behavior; use snapshot entity's Behavior field.
                    bool aggressive = e.Behavior == (byte)NpcBehavior.AttackOnSight || e.Behavior == (byte)NpcBehavior.Guard;
                    double chance = aggressive ? 0.02 : 0.05; // aggressive wander less
                    if (Random.Shared.NextDouble() < chance)
                    {
                        byte dir = (byte)(Random.Shared.Next(0, 4));
                        if (Server.Npc.CanNpcMove(map, npcIndex, dir))
                        {
                            Server.Npc.NpcMove(map, npcIndex, dir, (int)MovementState.Walking);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AI Movement] Error: {ex.Message}");
        }

        var now = General.GetTimeMs();
        var itemCount = Variables.MaxMapItems;
        var mapCount = Variables.MaxMaps;

        for (int mapNum = 0; mapNum < mapCount; mapNum++)
        {
            // Handle map items (public/despawn)
            for (int i = 0; i < itemCount; i++)
            {
                var item = Data.MapItem[mapNum, i];
                if (item.Num >= 0 && !string.IsNullOrEmpty(item.PlayerName))
                {
                    if (item.PlayerTimer < now)
                    {
                        item.PlayerName = "";
                        item.PlayerTimer = 0;
                        NetworkSend.SendMapItemToAll(mapNum, i);
                    }

                    if (item.CanDespawn && item.DespawnTimer < now)
                    {
                        Server.MapItem.Clear(i, mapNum);
                        NetworkSend.SendMapItemToAll(mapNum, i);
                    }
                }
            }

            // Respawn resources
            var mapResource = Data.MapResource[mapNum];
            if (mapResource.ResourceCount > 0)
            {
                for (int i = 0; i < mapResource.ResourceCount; i++)
                {
                    var resData = mapResource.ResourceData[i];
                    int resourceindex = Data.Map[mapNum].Tile[resData.X, resData.Y].Data1;
                    if (resourceindex > 0)
                    {
                        if (resData.State == 1 || resData.Health < 1)
                        {
                            if (resData.Timer + Data.Resource[resourceindex].RespawnTime * 1000 < now)
                            {
                                resData.Timer = now;
                                resData.State = 0;
                                resData.Health = (byte)Data.Resource[resourceindex].Health;
                                NetworkSend.SendMapResourceToMap(mapNum);
                            }
                        }
                    }
                }
            }
        }

        // Group vital regeneration executed after NPC AI loop (wrapped for script safety)
        RegenVitals();
    }

    public void CheckLevelUp(int index)
    {
        int level_count;

        level_count = 0;

        while (GetPlayerExp(index) >= GetPlayerNextLevel(index))
        {
            var expRollover = GetPlayerExp(index) - GetPlayerNextLevel(index);
            SetPlayerLevel(index, GetPlayerLevel(index) + 1);
            int points = StatPerLevel;
            points += ((int)Math.Floor((decimal)GetPlayerStat(index, Stat.Luck) / 10));
            SetPlayerPoints(index, expRollover);
            SetPlayerExp(index, expRollover);
            level_count += 1;
        }

        if (level_count > 0)
        {
            if (level_count == 1)
            {
                // singular
                NetworkSend.GlobalMsg(GetPlayerName(index) + " has gained " + level_count + " level!");
            }
            else
            {
                // plural
                NetworkSend.GlobalMsg(GetPlayerName(index) + " has gained " + level_count + " levels!");
            }
            NetworkSend.SendActionMsg(GetPlayerMap(index), "Level Up", (int)ColorName.Yellow, 1, GetPlayerX(index) * 32, GetPlayerY(index) * 32);
            NetworkSend.SendExp(index);
            NetworkSend.SendPlayerData(index);
        }
    }

    public void RegenVitals()
    {
        long now = General.GetTimeMs();
        bool doNpc = now - _lastNpcRegen >= NpcRegenIntervalMs;
        bool doPlayer = now - _lastPlayerRegen >= PlayerRegenIntervalMs;
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
                int maxHp = GameLogic.GetNpcMaxVital(e.Num, Vital.Health);
                int curHp = e.Vital[(byte)Vital.Health];
                if (curHp > 0 && curHp < maxHp)
                {
                    int amount = Math.Max(1, Data.Npc[e.Num].Stat[(byte)Stat.Vitality] / 2);
                    e.Vital[(byte)Vital.Health] = Math.Min(maxHp, curHp + amount);
                    Server.Npc.SendMapNpcVitals(e.Map, (byte)Core.Globals.Entity.Index(e));
                }
                int maxMana = GameLogic.GetNpcMaxVital(e.Num, Vital.Mana);
                if (maxMana > 0)
                {
                    int curMana = e.Vital[(byte)Vital.Mana];
                    if (curMana < maxMana)
                    {
                        int amount = Math.Max(1, Data.Npc[e.Num].Stat[(byte)Stat.Intelligence] / 2);
                        e.Vital[(byte)Vital.Mana] = Math.Min(maxMana, curMana + amount);
                        Server.Npc.SendMapNpcVitals(e.Map, (byte)Core.Globals.Entity.Index(e));
                    }
                }

                int maxStam = GameLogic.GetNpcMaxVital(e.Num, Vital.Stamina);
                if (maxStam > 0)
                {
                    int curStam = e.Vital[(byte)Vital.Stamina];
                    if (curStam < maxStam)
                    {
                        int amount = Math.Max(1, Data.Npc[e.Num].Stat[(byte)Stat.Spirit] / 2);
                        e.Vital[(byte)Vital.Stamina] = (int)Math.Min(maxStam, curStam + amount);
                        Server.Npc.SendMapNpcVitals(e.Map, (byte)Core.Globals.Entity.Index(e));
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
                int hpMax = GetPlayerMaxVital(id, Vital.Health);
                int hpCur = GetPlayerVital(id, Vital.Health);
                if (hpCur > 0 && hpCur < hpMax)
                {
                    int amount = Math.Max(1, GetPlayerStat(id, Stat.Vitality) / 2);
                    SetPlayerVital(id, Vital.Health, Math.Min(hpMax, hpCur + amount));
                    NetworkSend.SendVital(id, Vital.Health);
                }
                int manaMax = GetPlayerMaxVital(id, Vital.Mana);
                int manaCur = GetPlayerVital(id, Vital.Mana);
                if (manaCur < manaMax)
                {
                    int amount = Math.Max(1, GetPlayerStat(id, Stat.Intelligence) / 2);
                    SetPlayerVital(id, Vital.Mana, Math.Min(manaMax, manaCur + amount));
                    NetworkSend.SendVital(id, Vital.Mana);
                }
                int stamMax = GetPlayerMaxVital(id, Vital.Stamina);
                int stamCur = GetPlayerVital(id, Vital.Stamina);
                if (stamCur < stamMax)
                {
                    int amount = Math.Max(1, GetPlayerStat(id, Stat.Spirit) / 2);
                    SetPlayerVital(id, Vital.Stamina, Math.Min(stamMax, stamCur + amount));
                    NetworkSend.SendVital(id, Vital.Stamina);
                }
            }
        }
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

    private void HandleDeath(Entity attacker, Entity target)
    {
        if (target.Type == Entity.EntityType.Player)
        {
            if (Data.Moral[Data.Map[GetPlayerMap(target.Id)].Moral].DropItems)
            {
                var equipCount = Enum.GetValues(typeof(Equipment)).Length;
                
                // Drop equipment
                for (int i = 0; i < equipCount; i++)
                {
                    if (GetPlayerEquipment(target.Id, (Equipment)i) >= 0)
                    {
                        Server.MapItem.Spawn(GetPlayerEquipment(target.Id, (Equipment)i), 1, GetPlayerMap(target.Id), GetPlayerX(target.Id), GetPlayerY(target.Id));
                        NetworkSend.PlayerMsg(target.Id, "You have dropped your " + Data.Item[GetPlayerEquipment(target.Id, (Equipment)i)].Name + " upon death.", (int)ColorName.BrightRed);
                        SetPlayerEquipment(target.Id, -1, (Equipment)i);
                    }
                }
            }

            // Apply death penalty & get exp lost
            int lost = Server.Player.KillPlayer(target.Id);

            // Basic attacker reward (if attacker is player) with party sharing
            if (attacker.Type == Entity.EntityType.Player && attacker.Id != target.Id)
            {
                if (lost > 0)
                {
                    int gain = Math.Max(1, lost);
                    int partyId = Data.TempPlayer[attacker.Id].InParty;
                    int mapNum = GetPlayerMap(attacker.Id);
                    if (partyId >= 0)
                    {
                        // Share EXP among party members on the same map
                        ShareExp(partyId, gain, attacker.Id, mapNum);
                    }
                    else
                    {
                        // Solo award
                        SetPlayerExp(attacker.Id, GetPlayerExp(attacker.Id) + gain);
                        NetworkSend.SendExp(attacker.Id);
                    }
                    NetworkSend.PlayerMsg(attacker.Id, $"You gained {gain} experience for defeating {GetPlayerName(target.Id)}.", (int)ColorName.BrightGreen);
                }
            }

            NetworkSend.GlobalMsg(GetPlayerName(target.Id) + " was slain by " + GetEntityName(attacker) + ".");

            // Hide the player on their client immediately (do not broadcast to map)
            var deathPacket = new Core.Net.PacketWriter();
            var deathTimerMs = DeathSpawnTimeMs;
            deathPacket.WriteInt32((int)ServerPackets.SPlayerDead);
            deathPacket.WriteInt32(deathTimerMs);
            deathPacket.WriteInt32(target.Id);
            PlayerService.Instance.SendDataTo(target.Id, deathPacket.GetBytes());

            // After timer expires: perform the actual death warp and then release hold
            System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(DeathSpawnTimeMs);
                if (IsPlaying(target.Id) && Data.Player[target.Id].Dead)
                {
                    OnPlayerDeath(target.Id);
                }
            });

        }
        else if (target.Type == Entity.EntityType.Npc)
        {
            var map = target.Map;
            var mapNpcNum = target.Id;
            if (map >= 0 && map < Data.MapNpc.Length && mapNpcNum >= 0 && mapNpcNum < Script.MaxMapNpcs)
            {
                // Loot
                DropNpcLoot(map, mapNpcNum);

                // Mark dead & schedule respawn with a 60-second countdown via action messages
                ref var mapNpc = ref Data.MapNpc[map].Npc[mapNpcNum];
                var deathTimerMs = DeathSpawnTimeMs;
                var currentTime = (int)General.GetTimeMs();
                
                // Store original NPC number for respawn and set to dead state
                int originalNpcNum = mapNpc.Num;
                mapNpc.Num = -1; // regular dead
                mapNpc.SpawnWait = currentTime + deathTimerMs; // respawn time
                mapNpc.Vital[(int)Vital.Health] = 0;

                // Hide this single NPC on clients (send slot index as payload)
                var deathPacket = new Core.Net.PacketWriter();
                deathPacket.WriteInt32((int)ServerPackets.SNpcDead);
                deathPacket.WriteInt32(deathTimerMs);
                deathPacket.WriteInt32(mapNpcNum);
                NetworkConfig.SendDataToMap(map, deathPacket.GetBytes());

                // clear this npc's own target
                mapNpc.Target = -1;
                mapNpc.TargetType = 0;

                for (int i = 0; i < Script.MaxMapNpcs; i++)
                {
                    if (Data.MapNpc[map].Npc[i].TargetType == (byte)TargetType.Npc && Data.MapNpc[map].Npc[i].Target == mapNpcNum)
                    {
                        Data.MapNpc[map].Npc[i].TargetType = 0;
                        Data.MapNpc[map].Npc[i].Target = -1;
                    }
                }

                foreach (var player in PlayerService.Instance.Players)
                {
                    if (IsPlaying(player.Id))
                    {
                        if (GetPlayerMap(player.Id) == map)
                        {
                            if (Data.TempPlayer[player.Id].TargetType == (byte)TargetType.Npc && Data.TempPlayer[player.Id].Target == mapNpcNum)
                            {
                                Data.TempPlayer[player.Id].TargetType = 0;
                                Data.TempPlayer[player.Id].Target = -1;
                            }
                        }
                    }
                }

                // Grant exp to attacker if player (share with party if applicable)
                if (attacker.Type == Entity.EntityType.Player && mapNpc.Num == -1)
                {
                    int baseExp = 0;
                    if (originalNpcNum >= 0 && originalNpcNum < Data.Npc.Length)
                    {
                        baseExp = Data.Npc[originalNpcNum].Exp; // NPC base EXP
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
                            SetPlayerExp(attacker.Id, GetPlayerExp(attacker.Id) + baseExp);
                            NetworkSend.SendExp(attacker.Id);
                        }
                        NetworkSend.PlayerMsg(attacker.Id, $"You gained {baseExp} experience.", (int)ColorName.BrightGreen);
                    }
                }
            }
        }
    }

    private string GetEntityName(Entity e)
    {
        if (e.Type == Entity.EntityType.Player)
        {
            return GetPlayerName(e.Id);
        }
        if (e.Type == Entity.EntityType.Npc)
        {
            return (e.Num >= 0 && e.Num < Data.Npc.Length) ? Data.Npc[e.Num].Name : "NPC";
        }
        return "Entity";
    }

    private bool IsSkillRanged(int? skillId)
    {
        if (!skillId.HasValue) return false;
        var id = skillId.Value;
        if (id < 0 || id >= Data.Skill.Length) return false;
        return Data.Skill[id].Range > 1; // simple heuristic
    }

    public bool AttemptAttack(Entity attacker, Entity target, int? skillId = null, int? damage = null, bool? allowOutOfRange = false)
    {
        if (attacker == null || target == null) return false;
        if (attacker.Map != target.Map) return false;
        if (!IsAlive(attacker) || !IsAlive(target)) return false;
        if (!IsSkillRanged(skillId) && !IsInMeleeRange(attacker, target) && allowOutOfRange == false) return false;

        if (!Data.Moral[Data.Map[GetPlayerMap(attacker.Id)].Moral].CanPk && attacker.Type == Entity.EntityType.Player && target.Type == Entity.EntityType.Player)
        {
            return false; // PvP not allowed on this map
        }
        var now = General.GetTimeMs();
        var cd = GetAttackSpeed(attacker, skillId);
        if (attacker.AttackTimer + cd > now) return false;

        var dmg = CalculateDamage(attacker, target, skillId, damage);
        var killed = ApplyDamageExtended(attacker, target, dmg, skillId);

        // set cooldown
        attacker.AttackTimer = (int)now; // attacker is a snapshot; we must also update underlying store
        UpdateUnderlyingAttackTimer(attacker, (int)now);
        BroadcastAttack(attacker);

        if (target.Type == Entity.EntityType.Player)
        {
            if (attacker.Type == Entity.EntityType.Player)
            {
                NetworkSend.PlayerMsg(target.Id, $"You were hit by {GetPlayerName(attacker.Id)} for {dmg.Final} damage.", (int)ColorName.BrightRed);
            }
            else if (attacker.Type == Entity.EntityType.Npc)
            {
                NetworkSend.PlayerMsg(target.Id, $"You were hit by {GetEntityName(attacker)} for {dmg.Final} damage.", (int)ColorName.BrightRed);
            }
        }
        else if (target.Type == Entity.EntityType.Npc)
        {
            if (attacker.Type == Entity.EntityType.Player)
            {
                NetworkSend.PlayerMsg(attacker.Id, $"You hit {GetEntityName(target)} for {dmg.Final} damage.", (int)ColorName.BrightGreen);
            }
        }

        // If target is an npc and was attacked by player/npc, make it retaliate (set chase target)
        if (target.Type == Entity.EntityType.Npc && target.Num >= 0)
        {
            // Acquire underlying map npc to set target persistent
            var map = target.Map;
            var mapNpcIndex = target.Id;
            if (map >= 0 && map < Data.MapNpc.Length && mapNpcIndex >= 0 && mapNpcIndex < Script.MaxMapNpcs)
            {
                ref var baseNpc = ref Data.MapNpc[map].Npc[mapNpcIndex];
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
            var mapNpcIndex = attacker.Id;
            if (map >= 0 && map < Data.MapNpc.Length && mapNpcIndex >= 0 && mapNpcIndex < Script.MaxMapNpcs)
            {
                ref var baseNpc = ref Data.MapNpc[map].Npc[mapNpcIndex];
                if (baseNpc.TargetType == 0)
                {
                    if (target.Type == Entity.EntityType.Player)
                    {
                        baseNpc.TargetType = (byte)TargetType.Player;
                        baseNpc.Target = target.Id;
                    }
                    else if (target.Type == Entity.EntityType.Npc)
                    {
                        baseNpc.TargetType = (byte)TargetType.Npc;
                        baseNpc.Target = target.Id;
                    }
                }
            }
        }

        if (killed)
        {
            HandleDeath(attacker, target);
        }

        // Chain casting on hit if this came from a skill and damage actually landed
        if (skillId.HasValue && skillId.Value >= 0 && dmg.Final > 0)
        {
            TryChainOnHit(attacker.Map, attacker, skillId.Value, target);
            ApplyKnockbackIfAny(attacker, target, skillId);
        }

        // Death is handled inside ApplyDamage now; killed flag returned for external hooks.
        return true;
    }

    private bool IsAlive(Entity e)
    {
        if (e.Vital == null) return false;
        return e.Vital[(int)Vital.Health] > 0;
    }

    private bool IsInMeleeRange(Entity a, Entity b)
    {
        // Tile-based adjacency including diagonals (8-direction) so diagonal melee hits connect.
        var ax = a.X / 32; var ay = a.Y / 32;
        var bx = b.X / 32; var by = b.Y / 32;
        var dx = Math.Abs(ax - bx);
        var dy = Math.Abs(ay - by);
        if (dx == 0 && dy == 0) return false; // same tile not considered melee
        return dx <= 1 && dy <= 1; // any adjacent (including diagonals)
    }

    private bool TryChase(int mapNum, int npcIndex, int sx, int sy, int tx, int ty)
    {
        int dx = tx - sx;
        int dy = ty - sy;
        if (dx == 0 && dy == 0) return false; // already on target tile

        // Try to compute a short path and enqueue it so animation continues between tiles.
        var route = ComputePathAStar(mapNum, sx, sy, tx, ty, maxSteps: 12);
        if (route != null && route.Count > 0)
        {
            Server.Npc.SetRoute(mapNum, npcIndex, route);
            Server.Npc.TryStartNextStepNow(mapNum, npcIndex);
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
            if (Server.Npc.CanNpcMove(mapNum, npcIndex, d))
            {
                Server.Npc.NpcMove(mapNum, npcIndex, d, (int)MovementState.Walking);
                return true;
            }
        }
        return false;
    }

    private static System.Collections.Generic.List<byte>? ComputePathAStar(int mapNum, int sx, int sy, int tx, int ty, int maxSteps)
    {
        int maxX = Data.Map[mapNum].MaxX;
        int maxY = Data.Map[mapNum].MaxY;
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

            foreach (var (nx, ny, dir) in Neighbors(mapNum, cx, cy, maxX, maxY))
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

    private static System.Collections.Generic.IEnumerable<(int x,int y,byte dir)> Neighbors(int mapNum, int x, int y, int maxX, int maxY)
    {
        // Cardinal moves
        if (x+1 < maxX && IsTileWalkable(mapNum, x+1, y)) yield return (x+1, y, (byte)Direction.Right);
        if (x-1 >= 0  && IsTileWalkable(mapNum, x-1, y)) yield return (x-1, y, (byte)Direction.Left);
        if (y+1 < maxY && IsTileWalkable(mapNum, x, y+1)) yield return (x, y+1, (byte)Direction.Down);
        if (y-1 >= 0  && IsTileWalkable(mapNum, x, y-1)) yield return (x, y-1, (byte)Direction.Up);
    }

    private static bool IsTileWalkable(int mapNum, int x, int y)
    {
        // Mirror CanNpcMove constraints loosely using tile types only; dynamic collisions are validated at step time.
        var t = Data.Map[mapNum].Tile[x, y];
        int n = (int)t.Type; int n2 = (int)t.Type2;
        bool ok = (n == (byte)TileType.None || n == (byte)TileType.Item || n == (byte)TileType.NpcSpawn) ||
                  (n2 == (byte)TileType.None || n2 == (byte)TileType.Item || n2 == (byte)TileType.NpcSpawn);
        return ok;
    }

    private int GetAttackSpeed(Entity attacker, int? skillId)
    {
        // Skill cast time gating handled elsewhere; for now consider weapon speed for players
        if (attacker.Type == Entity.EntityType.Player)
        {
            var weaponId = GetEquippedItemId(attacker, Equipment.Weapon);
            if (weaponId >= 0)
            {
                return Data.Item[weaponId].Speed > 0 ? Data.Item[weaponId].Speed : BaseAttackSpeedMs;
            }
        }
        return BaseAttackSpeedMs;
    }

    // Public helper to enforce per-attacker cooldown based on attack speed.
    // Returns true if cooldown was available and is now consumed; false if still on cooldown.
    public bool TryConsumeAttackCooldown(Entity attacker, int? skillId = null)
    {
        if (attacker == null) return false;
        var now = General.GetTimeMs();
        var cd = GetAttackSpeed(attacker, skillId);
        if (attacker.AttackTimer + cd > now)
        {
            return false;
        }
        attacker.AttackTimer = (int)now;
        UpdateUnderlyingAttackTimer(attacker, (int)now);
        return true;
    }

    private int GetEquippedItemId(Entity player, Equipment eq)
    {
        if (player.Equipment == null) return -1;
        var slot = (int)eq;
        if (slot < 0 || slot >= player.Equipment.Length) return -1;
        return player.Equipment[slot].Num;
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
            if (attacker.Num >= 0 && attacker.Num < Data.Npc.Length)
            {
                raw = Math.Max(1, Data.Npc[attacker.Num].Damage);
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
            if (target.Num >= 0 && target.Num < Data.Npc.Length)
            {
                mitigation += (int)Data.Npc[target.Num].Stat[(int)Stat.Vitality / 5];
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

    private int GetPlayerDamage(int playerId, int? skillId)
    {
        if (playerId < 0 || playerId >= Data.Player.Length) return 0;
        int power = 0;
        if (skillId >= 0)
            power = GetPlayerStat(playerId, Stat.Intelligence) / 2;
        else
            power = GetPlayerStat(playerId, Stat.Strength) / 2;

        int weaponId = GetPlayerEquipment(playerId, Equipment.Weapon);
        int weaponPower = (weaponId >= 0 && weaponId < Data.Item.Length) ? Data.Item[weaponId].Data2 : 0;
        // Keep formula aligned with prior CalculateDamage logic (without RNG)
        int baseDamage = power + weaponPower;
        return Math.Max(0, baseDamage);
    }

    private int GetPlayerDefense(int playerId)
    {
        int def = GetPlayerStat(playerId, Stat.Vitality) / 5;
        return def;
    }

    public int GetPlayerNextLevel(int index)
    {
        int level = GetPlayerLevel(index);
        int str = GetPlayerStat(index, Stat.Strength);
        int vit = GetPlayerStat(index, Stat.Vitality);
        int intellect = GetPlayerStat(index, Stat.Intelligence);
        int luck = GetPlayerStat(index, Stat.Luck);
        int points = GetPlayerPoints(index);

        long next = (long)(level + 1) * (str + vit + intellect + luck + points) * 25L;
        return next > int.MaxValue ? int.MaxValue : (int)Math.Max(0, next);
    }

    private int SafeStat(Entity e, Stat stat)
    {
        if (e.Stat == null) return 0;
        var idx = (int)stat;
        if (idx < 0 || idx >= e.Stat.Length) return 0;
        return e.Stat[idx];
    }

    // Adjust vital on an entity (player or npc). If isHeal=false we subtract (damage). If true we add (heal).
    // amountParam is base amount from skill; for now no scaling besides simple clamp.
    // caster may be used later for threat/aggro or scaling.
    private void AdjustVital(Entity target, Vital vital, int amountParam, bool isHeal, int skillId, int mapNum, Entity caster)
    {
        if (target == null) return;
        if (vital != Vital.Health && vital != Vital.Mana && vital != Vital.Stamina) return; // only support these

        int amount = Math.Max(0, amountParam);
        if (amount == 0) return;

        if (target.Type == Core.Globals.Entity.EntityType.Player)
        {
            int pid = target.Id;
            if (!NetworkConfig.IsPlaying(pid)) return;
            int cur = GetPlayerVital(pid, vital);
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
            SetPlayerVital(pid, vital, newVal);
            NetworkSend.SendVital(pid, vital);
            if (!isHeal && newVal <= 0 && vital == Vital.Health)
            {
                // Player death routine (reuse existing logic if available)
                if (caster != null && caster.Type == Core.Globals.Entity.EntityType.Player)
                {
                    HandleDeath(caster, target);
                }
            }
        }
        else if (target.Type == Core.Globals.Entity.EntityType.Npc)
        {
            if (target.Map < 0 || target.Map >= Data.MapNpc.Length) return;
            if (target.Id < 0 || target.Id >= Script.MaxMapNpcs) return;
            ref var mapNpc = ref Data.MapNpc[target.Map].Npc[target.Id];
            if (mapNpc.Num < 0) return;
            int idx = (int)vital;
            if (mapNpc.Vital == null || idx < 0 || idx >= mapNpc.Vital.Length) return;
            int cur = mapNpc.Vital[idx];
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
            mapNpc.Vital[idx] = newVal;
            if (vital == Vital.Health && !isHeal)
            {
                // show damage amount like existing ApplyDamage does (keep consistent color if possible)
                NetworkSend.SendActionMsg(target.Map, (isHeal ? "+" : "-") + amount, (int)(isHeal ? ColorName.BrightGreen : ColorName.BrightRed), 1, target.X, target.Y);
            }
            Server.Npc.SendMapNpcVitals(target.Map, (byte)target.Id);
            if (!isHeal && vital == Vital.Health && newVal <= 0)
            {
                HandleDeath(caster, target);
            }
        }
    }

    private void CastSkill(int mapNum, Entity entity, int bufferedValue)
    {
        if (entity == null) return;
        if (entity.Map != mapNum) return;

        int skillId = -1;
        int playerSkillSlot = -1;
        if (entity.Type == Core.Globals.Entity.EntityType.Player)
        {
            playerSkillSlot = bufferedValue;
            if (playerSkillSlot < 0 || playerSkillSlot >= Data.Player[entity.Id].Skill.Length) return;
            skillId = Data.Player[entity.Id].Skill[playerSkillSlot].Num;
        }
        else if (entity.Type == Core.Globals.Entity.EntityType.Npc)
        {
            for (int i = 0; i < Script.MaxNpcSkills; i++)
            {
                if (i == bufferedValue)
                {
                    skillId = Data.Npc[entity.Num].Skill[i];
                    break;
                }
            }
        }
        
        if (skillId < 0 || skillId >= Data.Skill.Length) return;
        ref var skill = ref Data.Skill[skillId];

        // Re-check mana just before execution (player or npc could have spent mana meanwhile)
        if (skill.MpCost > 0)
        {
            if (entity.Type == Core.Globals.Entity.EntityType.Player)
            {
                if (GetPlayerVital(entity.Id, Vital.Mana) < skill.MpCost) return;
            }
            else if (entity.Type == Core.Globals.Entity.EntityType.Npc)
            {
                if (entity.Vital == null || entity.Vital.Length <= (int)Vital.Mana || entity.Vital[(int)Vital.Mana] < skill.MpCost) return;
            }
        }

        Entity resolvedTarget = null;
        if (skill.Range > 0)
        {
            resolvedTarget = ResolveTargetEntity(mapNum, entity);
        }

        // Optional cast (wind-up) animation already played when buffering; only play execution anim here.
        bool isProjectile = skill.IsProjectile == 1;
        bool isAoE = skill.IsAoE;
        int range = skill.Range;

        if (isProjectile)
        {
            HandleProjectileSkill(mapNum, entity, skillId, resolvedTarget);
        }
        else if (range == 0 && !isAoE)
        {
            HandleSelfCastSkill(mapNum, entity, skillId);
        }
        else if (range == 0 && isAoE)
        {
            HandleSelfCastAoESkill(mapNum, entity, skillId);
        }
        else if (range > 0 && isAoE)
        {
            HandleTargetedAoESkill(mapNum, entity, skillId, resolvedTarget);
        }
        else if (range > 0)
        {
            HandleTargetedSkill(mapNum, entity, skillId, resolvedTarget);
        }

        FinalizeCast(mapNum, entity, skillId, playerSkillSlot);
    }

    private Entity? ResolveTargetEntity(int mapNum, Entity entity)
    {
        if (entity.TargetType == (byte)TargetType.Player)
        {
            var pid = entity.Target;
            if (NetworkConfig.IsPlaying(pid) && GetPlayerMap(pid) == mapNum)
            {
                var e = Core.Globals.Entity.FromPlayer(pid, Data.Player[pid]);
                e.Map = mapNum;
                return e;
            }
        }
        else if (entity.TargetType == (byte)TargetType.Npc)
        {
            var tid = entity.Target;
            if (tid >= 0 && tid < Core.Globals.Entity.Instances.Count)
            {
                var tEnt = Core.Globals.Entity.Instances[tid];
                if (tEnt != null && tEnt.Type == Core.Globals.Entity.EntityType.Npc && tEnt.Map == mapNum && tEnt.Num >= 0)
                    return tEnt;
            }
        }
        return null;
    }

    private void HandleProjectileSkill(int mapNum, Entity caster, int skillId, Entity? target)
    {
        // Spawn one or more projectiles depending on MultiDirMask. If mask==0, fire in caster's facing or skill.Dir
        ref var skill = ref Data.Skill[skillId];
        int mask = skill.MultiDirMask;
        if (mask == 0)
        {
            if (caster.Type == Core.Globals.Entity.EntityType.Player)
                Server.Projectile.PlayerFireProjectile(caster.Id, -1, skillId);
            else if (caster.Type == Core.Globals.Entity.EntityType.Npc)
                Server.Projectile.NpcFireProjectile(mapNum, caster.Id, skillId);
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
                Server.Projectile.PlayerFireProjectile(caster.Id, -1, skillId, caster.Dir, suppressCooldown: true);
            else if (caster.Type == Core.Globals.Entity.EntityType.Npc)
                Server.Projectile.NpcFireProjectile(mapNum, caster.Id, skillId, caster.Dir);
        }
        caster.Dir = originalDir;

        // Apply a single cooldown based on this skill's projectile speed
        if (caster.Type == Core.Globals.Entity.EntityType.Player)
        {
            var projNum = Data.Skill[skillId].Projectile;
            if (projNum >= 0)
            {
                Data.TempPlayer[caster.Id].ProjectileTimer = General.GetTimeMs() + Math.Max(0, Data.Projectile[projNum].Speed);
            }
        }
    }

    private void HandleSelfCastSkill(int mapNum, Entity caster, int skillId)
    {
        ref var skill = ref Data.Skill[skillId];
        switch (skill.Type)
        {
            case 0: // Damage HP self
                AdjustVital(caster, Vital.Health, skill.Vital, false, skillId, mapNum, caster);
                break;
            case 1: // Damage MP self
                AdjustVital(caster, Vital.Mana, skill.Vital, false, skillId, mapNum, caster);
                break;
            case 2: // Heal HP self
                AdjustVital(caster, Vital.Health, skill.Vital, true, skillId, mapNum, caster);
                break;
            case 3: // Heal MP self
                AdjustVital(caster, Vital.Mana, skill.Vital, true, skillId, mapNum, caster);
                break;
            case 4: // Warp
                if (skill.Map >= 0 && skill.Map < Data.Map.Length)
                {
                    int destMap = skill.Map;
                    int destX = skill.X;
                    int destY = skill.Y;
                    if (destMap >= 0 && destMap < Data.Map.Length && destX >= 0 && destX < Core.Globals.Variables.MaxMapX && destY >= 0 && destY < Core.Globals.Variables.MaxMapY)
                    {
                        if (caster.Type == Core.Globals.Entity.EntityType.Player)
                        {
                            // Accept any of the 8 directions from the editor; default to Down if out of range
                            byte dir = (skill.Dir <= (byte)Direction.UpLeft) ? skill.Dir : (byte)Direction.Down;
                            Warp(caster.Id, destMap, destX, destY, dir);
                            NetworkSend.PlayerMsg(caster.Id, "You feel space bend around you...", (int)ColorName.Cyan);
                        }
                    }
                }
                break;
        }
        PlaySkillAnimation(mapNum, caster, skillId, caster);
    }

    private void HandleSelfCastAoESkill(int mapNum, Entity caster, int skillId)
    {
        ApplyAoE(mapNum, caster, skillId, caster.X / 32, caster.Y / 32);
    }

    private void HandleTargetedSkill(int mapNum, Entity caster, int skillId, Entity? target)
    {
        if (target == null) return;
        ref var s = ref Data.Skill[skillId];
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
                int tx = caster.X/32 + deltas[i].dx;
                int ty = caster.Y/32 + deltas[i].dy;
                // Try find another entity on this tile and attack
                var extraTarget = FindEntityAt(mapNum, tx, ty, preferOpponentsOf: caster);
                if (extraTarget != null && (extraTarget.Id != target.Id || extraTarget.Type != target.Type))
                {
                    AttemptAttack(caster, extraTarget, skillId);
                }
            }
        }
        ref var skill = ref Data.Skill[skillId];
        if (skill.Type == 2 || skill.Type == 3)
        {
            var vital = skill.Type == 2 ? Vital.Health : Vital.Mana;
            AdjustVital(target, vital, skill.Vital, true, skillId, mapNum, caster);
        }
        PlaySkillAnimation(mapNum, caster, skillId, target);
    }

    private void HandleTargetedAoESkill(int mapNum, Entity caster, int skillId, Entity? target)
    {
        int baseX = (target != null ? target.X : caster.X) / 32;
        int baseY = (target != null ? target.Y : caster.Y) / 32;
        ref var s = ref Data.Skill[skillId];
        if (s.MultiDirMask == 0)
        {
            ApplyAoE(mapNum, caster, skillId, baseX, baseY);
        }
        else
        {
            Span<(int dx,int dy)> deltas = stackalloc (int,int)[] { (0,1),(1,0),(-1,0),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1) };
            var dirCount = System.Enum.GetValues<Direction>().Length;
            for (int i = 0; i < dirCount; i++)
            {
                if ((s.MultiDirMask & (1 << i)) == 0) continue;
                ApplyAoE(mapNum, caster, skillId, baseX + deltas[i].dx, baseY + deltas[i].dy);
            }
        }
    }

    public Entity? FindEntityAt(int mapNum, int tx, int ty, Entity preferOpponentsOf)
    {
        // Players first
        foreach (var p in PlayerService.Instance.Players)
        {
            if (!NetworkConfig.IsPlaying(p.Id)) continue;
            if (GetPlayerMap(p.Id) != mapNum) continue;
            if (GetPlayerX(p.Id) == tx && GetPlayerY(p.Id) == ty)
            {
                var e = Core.Globals.Entity.FromPlayer(p.Id, Data.Player[p.Id]); e.Map = mapNum; return e;
            }
        }
        // NPCs
        if (mapNum >= 0 && mapNum < Data.MapNpc.Length)
        {
            for (int i = 0; i < Script.MaxMapNpcs; i++)
            {
                ref var mn = ref Data.MapNpc[mapNum].Npc[i];
                if (mn.Num < 0) continue;
                if (mn.X == tx && mn.Y == ty)
                {
                    var e = Core.Globals.Entity.Instances[i];
                    if (e != null && e.Type == Core.Globals.Entity.EntityType.Npc) return e;
                }
            }
        }
        return null;
    }

    public void ApplyAoE(int mapNum, Entity caster, int skillId, int centerX, int centerY)
    {
        ref var skill = ref Data.Skill[skillId];
        int radius = skill.AoE;
        bool isDamage = skill.Type == 0 || skill.Type == 1;
        bool isHeal = skill.Type == 2 || skill.Type == 3;
        var vital = (skill.Type == 1 || skill.Type == 3) ? Vital.Mana : Vital.Health;

        // Players
        foreach (var player in PlayerService.Instance.Players)
        {
            if (!NetworkConfig.IsPlaying(player.Id)) continue;
            if (GetPlayerMap(player.Id) != mapNum) continue;
            int px = GetPlayerX(player.Id);
            int py = GetPlayerY(player.Id);
            if (Math.Abs(px - centerX) <= radius && Math.Abs(py - centerY) <= radius)
            {
                var targetEntity = Core.Globals.Entity.FromPlayer(player.Id, Data.Player[player.Id]);
                targetEntity.Map = mapNum;
                if (isDamage) AttemptAttack(caster, targetEntity, skillId);
                if (isHeal) AdjustVital(targetEntity, vital, skill.Vital, true, skillId, mapNum, caster);
                PlaySkillAnimation(mapNum, caster, skillId, targetEntity);
            }
        }

        // NPCs via map data (avoid LINQ)
        if (mapNum >= 0 && mapNum < Data.MapNpc.Length)
        {
            for (int i = 0; i < Script.MaxMapNpcs; i++)
            {
                if (Data.MapNpc[mapNum].Npc[i].Num < 0) continue;
                int nx = Data.MapNpc[mapNum].Npc[i].X / 32;
                int ny = Data.MapNpc[mapNum].Npc[i].Y / 32;
                if (Math.Abs(nx - centerX) <= radius && Math.Abs(ny - centerY) <= radius)
                {
                    var npcEntity = Core.Globals.Entity.FromNpc(i, Data.MapNpc[mapNum].Npc[i]);
                    npcEntity.Map = mapNum;
                    if (isDamage) AttemptAttack(caster, npcEntity, skillId);
                    if (isHeal) AdjustVital(npcEntity, vital, skill.Vital, true, skillId, mapNum, caster);
                    PlaySkillAnimation(mapNum, caster, skillId, npcEntity);
                }
            }
        }
    }

    private void PlaySkillAnimation(int mapNum, Entity caster, int skillId, Entity target)
    {
        int anim = Data.Skill[skillId].SkillAnim;
        if (anim < 0) return;
        byte tType = (byte)(target.Type == Core.Globals.Entity.EntityType.Player ? TargetType.Player : TargetType.Npc);
        NetworkSend.SendAnimation(mapNum, anim, 0, 0, tType, target.Id);
    }

    private void TryChainOnHit(int mapNum, Entity caster, int baseSkillId, Entity target)
    {
        if (baseSkillId < 0 || baseSkillId >= Data.Skill.Length) return;
        int chainId = Data.Skill[baseSkillId].ChainOnHitSkillId;
        if (chainId < 0 || chainId >= Data.Skill.Length) return;
        // For targeted chaining, we can re-use targeted attack semantics by routing HandleTargetedSkill
        // but respect the chain skill's own type definitions.
        ref var chain = ref Data.Skill[chainId];
        if (chain.IsProjectile == 1)
        {
            // Fire projectile(s) from caster using chain skill
            HandleProjectileSkill(mapNum, caster, chainId, target);
        }
        else if (chain.Range == 0 && !chain.IsAoE)
        {
            HandleSelfCastSkill(mapNum, caster, chainId);
        }
        else if (chain.Range == 0 && chain.IsAoE)
        {
            HandleSelfCastAoESkill(mapNum, caster, chainId);
        }
        else if (chain.Range > 0 && chain.IsAoE)
        {
            HandleTargetedAoESkill(mapNum, caster, chainId, target);
        }
        else if (chain.Range > 0)
        {
            HandleTargetedSkill(mapNum, caster, chainId, target);
        }
        // No cooldown or mana additional cost for automated chain to keep it responsive; adjust if needed.
    }

    private void FinalizeCast(int mapNum, Entity caster, int skillId, int playerSkillSlot)
    {
        ref var skill = ref Data.Skill[skillId];
        if (skill.MpCost > 0)
        {
            if (caster.Type == Core.Globals.Entity.EntityType.Player)
            {
                int pid = caster.Id;
                int cur = GetPlayerVital(pid, Vital.Mana);
                SetPlayerVital(pid, Vital.Mana, Math.Max(0, cur - skill.MpCost));
                NetworkSend.SendVital(pid, Vital.Mana);
            }
            else if (caster.Type == Core.Globals.Entity.EntityType.Npc && caster.Vital != null && caster.Vital.Length > (int)Vital.Mana)
            {
                caster.Vital[(int)Vital.Mana] = Math.Max(0, caster.Vital[(int)Vital.Mana] - skill.MpCost);
            }
        }
        if (caster.Type == Core.Globals.Entity.EntityType.Player && playerSkillSlot >= 0)
        {
            int pid = caster.Id;
            if (Data.TempPlayer[pid].SkillCd != null && playerSkillSlot < Data.TempPlayer[pid].SkillCd.Length)
            {
                Data.TempPlayer[pid].SkillCd[playerSkillSlot] = General.GetTimeMs() + skill.CdTime * 1000;
                NetworkSend.SendSkillCooldown(pid, playerSkillSlot);
            }
        }
        else if (caster.Type == Core.Globals.Entity.EntityType.Npc)
        {
            // Set NPC cooldown on the slot that matches this skillId
            if (caster.Map >= 0 && caster.Map < Data.MapNpc.Length && caster.Id >= 0 && caster.Id < Script.MaxMapNpcs)
            {
                ref var baseNpc = ref Data.MapNpc[caster.Map].Npc[caster.Id];
                var npcTemplate = caster.Num >= 0 && caster.Num < Data.Npc.Length ? Data.Npc[caster.Num] : default;
                if (npcTemplate.Skill != null && baseNpc.SkillCd != null)
                {
                    for (int slot = 0; slot < Script.MaxNpcSkills && slot < npcTemplate.Skill.Length && slot < baseNpc.SkillCd.Length; slot++)
                    {
                        if (npcTemplate.Skill[slot] == skillId)
                        {
                            baseNpc.SkillCd[slot] = General.GetTimeMs() + skill.CdTime * 1000;
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
        if (entity.Type != Entity.EntityType.Player || entity.Equipment == null) return 0;

        int total = 0;

        for (int i = 0; i < entity.Equipment.Length; i++)
        {
            var itemNum = entity.Equipment[i].Num;
            if (itemNum >= 0 && itemNum < Data.Item.Length)
            {
                total += Data.Item[itemNum].Data2;
            }
        }
        return total + GetPlayerDefense(entity.Id);
    }

    private bool HasShield(Entity e)
    {
        if (e.Type != Entity.EntityType.Player || e.Equipment == null) return false;
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
            NetworkSend.SendActionMsg(map, "Dodge!", (int)ColorName.Pink, 1, tx, ty);
            return;
        }
        if (dmg.Parry)
        {
            NetworkSend.SendActionMsg(map, "Parry!", (int)ColorName.Pink, 1, tx, ty);
            return;
        }
        if (dmg.Block)
        {
            NetworkSend.SendActionMsg(map, "Block!", (int)ColorName.BrightCyan, 1, tx, ty);
            return;
        }

        if (dmg.Crit)
        {
            NetworkSend.SendActionMsg(map, "Critical!", (int)ColorName.BrightCyan, 1, attacker.X, attacker.Y);
        }

        var final = dmg.Final;
        if (final <= 0)
        {
            NetworkSend.PlayerMsg(attacker.Id, "Your attack does nothing.", (int)ColorName.BrightRed);
            return;
        }

        // Apply
        if (target.Type == Entity.EntityType.Player)
        {
            var current = GetPlayerVital(target.Id, Vital.Health);
            var newHp = Math.Max(0, current - final);
            SetPlayerVital(target.Id, Vital.Health, newHp);
            NetworkSend.SendVital(target.Id, Vital.Health);
            NetworkSend.SendActionMsg(map, "-" + final, (int)ColorName.BrightRed, 1, tx, ty);
        }
        else if (target.Type == Entity.EntityType.Npc)
        {
            if (target.Num >= 0 && target.Num < Data.Npc.Length)
            {
                var mapNpcNum = target.Id; // id is map npc index
                var hpIndex = (int)Vital.Health;
                var current = Data.MapNpc[map].Npc[mapNpcNum].Vital[hpIndex];
                var newHp = Math.Max(0, current - final);
                Data.MapNpc[map].Npc[mapNpcNum].Vital[hpIndex] = newHp;
                NetworkSend.SendActionMsg(map, "-" + final, (int)ColorName.BrightRed, 1, tx, ty);
                if (newHp > 0)
                {
                    // still alive
                    Server.Npc.SendMapNpcVitals(map, (byte)mapNpcNum);
                }
            }
        }
    }

    private void UpdateUnderlyingAttackTimer(Entity entity, int newTime)
    {
        if (entity.Type == Entity.EntityType.Player)
        {
            Data.TempPlayer[entity.Id].AttackTimer = newTime;
        }
        else if (entity.Type == Entity.EntityType.Npc)
        {
            if (entity.Map >= 0 && entity.Map < Data.MapNpc.Length && entity.Id >= 0 && entity.Id < Script.MaxMapNpcs)
            {
                Data.MapNpc[entity.Map].Npc[entity.Id].AttackTimer = newTime;
            }
        }
    }

    private void BroadcastAttack(Entity attacker)
    {
        if (attacker.Type == Entity.EntityType.Player)
        {
            NetworkSend.SendPlayerAttack(attacker.Id);
        }
        else if (attacker.Type == Entity.EntityType.Npc)
        {
            NetworkSend.SendNpcAttack(attacker.Map, (byte)attacker.Id);
        }
    }

    private bool ApplyDamageExtended(Entity attacker, Entity target, DamageResult dmg, int? skillId)
    {
        // Reuse existing ApplyDamage but capture death result
        var before = target.Vital != null ? target.Vital[(int)Vital.Health] : 0;
        ApplyDamage(attacker, target, dmg, skillId);
        var after = target.Type == Entity.EntityType.Player ? GetPlayerVital(target.Id, Vital.Health) : (target.Vital != null ? target.Vital[(int)Vital.Health] : 0);
        if (target.Type == Entity.EntityType.Npc && target.Map >= 0 && target.Map < Data.MapNpc.Length && target.Id >= 0 && target.Id < Script.MaxMapNpcs)
        {
            after = Data.MapNpc[target.Map].Npc[target.Id].Vital[(int)Vital.Health];
        }
        return before > 0 && after <= 0; // HandleDeath already executed if true
    }

    private void ApplyKnockbackIfAny(Entity attacker, Entity target, int? skillId)
    {
        if (!skillId.HasValue || skillId.Value < 0 || skillId.Value >= Data.Skill.Length) return;
        ref var s = ref Data.Skill[skillId.Value];
        if (s.KnockBack != 1 || s.KnockBackTiles <= 0) return;
        int steps = Math.Min(5, Math.Max(1, (int)s.KnockBackTiles));
        int map = attacker.Map;
        int ax = attacker.X / 32, ay = attacker.Y / 32;
        int tx = target.X / 32, ty = target.Y / 32;
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
            int nx = (target.X / 32) + dx;
            int ny = (target.Y / 32) + dy;
            // Bounds
            if (nx < 0 || ny < 0 || nx >= Data.Map[map].MaxX || ny >= Data.Map[map].MaxY) break;
            // Blocked tiles
            bool blocked = Data.Map[map].Tile[nx, ny].Type == TileType.Blocked || Data.Map[map].Tile[nx, ny].Type2 == TileType.Blocked;
            if (blocked) break;
            // Prevent collisions with other entities
            bool occ = false;
            if (target.Type == Entity.EntityType.Player)
            {
                foreach (var pid in PlayerService.Instance.PlayerIds)
                {
                    if (pid != target.Id && GetPlayerMap(pid) == map && GetPlayerX(pid) == nx && GetPlayerY(pid) == ny) { occ = true; break; }
                }
                if (!occ)
                {
                    // Move player by setting raw coordinates
                    SetPlayerX(target.Id, nx * 32);
                    SetPlayerY(target.Id, ny * 32);
                    NetworkSend.SendPlayerXYToMap(target.Id);
                }
                else break;
            }
            else if (target.Type == Entity.EntityType.Npc)
            {
                // Ensure no other NPC occupying
                for (int mi = 0; mi < Script.MaxMapNpcs; mi++)
                {
                    if (mi == target.Id) continue;
                    if (Data.MapNpc[map].Npc[mi].Num >= 0 && Data.MapNpc[map].Npc[mi].X/32 == nx && Data.MapNpc[map].Npc[mi].Y/32 == ny) { occ = true; break; }
                }
                if (!occ)
                {
                    Data.MapNpc[map].Npc[target.Id].X = nx * 32;
                    Data.MapNpc[map].Npc[target.Id].Y = ny * 32;
                    // Notify clients by sending SNpcDir (keeps anim simple) and vitals/position via SMapNpcData on next sync
                    var stopPacket = new Core.Net.PacketWriter(9);
                    stopPacket.WriteEnum(ServerPackets.SNpcDir);
                    stopPacket.WriteInt32(target.Id);
                    stopPacket.WriteByte(Data.MapNpc[map].Npc[target.Id].Dir);
                    NetworkConfig.SendDataToMap(map, stopPacket.GetBytes());
                }
                else break;
            }
        }
    }

    private void DropNpcLoot(int mapNum, int mapNpcNum)
    {
        if (mapNum < 0 || mapNum >= Data.MapNpc.Length) return;
        ref var mapNpc = ref Data.MapNpc[mapNum].Npc[mapNpcNum];
        var npcNum = mapNpc.Num;
        if (npcNum < 0 || npcNum >= Data.Npc.Length) return;
        // Simple single-roll logic similar to legacy: choose one drop slot 0-4
        var slot = General.GetRandom.NextInt(0, Math.Min(5, Data.Npc[npcNum].DropChance.Length));
        if (slot < 0) return;
        var chance = Data.Npc[npcNum].DropChance[slot];
        if (chance <= 0) return;
        var roll = General.GetRandom.NextInt(1, chance + 1);
        if (roll == 1)
        {
            var itemId = Data.Npc[npcNum].DropItem[slot];
            var itemVal = Data.Npc[npcNum].DropItemValue[slot];
            if (itemId >= 0 && itemId < Data.Item.Length)
            {
                Server.MapItem.Spawn(itemId, itemVal, mapNum, mapNpc.X / 32, mapNpc.Y / 32);
            }
        }
    }

    public int GetPlayerMaxHP(int index)
    {
        if (index < 0 || index >= Data.Player.Length) return 1;

        int str = GetPlayerStat(index, Stat.Strength);
        int job = GetPlayerJob(index);

        int baseJobStr = 0;
        if (job >= 0 && job < Data.Job.Length)
            baseJobStr = Data.Job[job].Stat[(int)Stat.Strength];

        long val = (long)(1 + (str / 2) + baseJobStr) * 2L;
        return (int)Math.Max(1, Math.Min(int.MaxValue, val));
    }

    public int GetPlayerMaxMP(int index)
    {
        if (index < 0 || index >= Data.Player.Length) return 1;

        int magi = GetPlayerStat(index, Stat.Intelligence);
        int job = GetPlayerJob(index);

        int basejobInt = 0;
        if (job >= 0 && job < Data.Job.Length)
            basejobInt = Data.Job[job].Stat[(int)Stat.Intelligence];

        long val = (long)(1 + (magi / 2) + basejobInt) * 2L;
        return (int)Math.Max(1, Math.Min(int.MaxValue, val));
    }

    public int GetPlayerMaxSP(int index)
    {
        if (index < 0 || index >= Data.Player.Length) return 1;

        int speed = GetPlayerStat(index, Stat.Spirit); // current codebase maps “Speed” to Stat.Spirit
        int job = GetPlayerJob(index);

        int baseJobSpirit = 0;
        if (job >= 0 && job < Data.Job.Length)
            baseJobSpirit = Data.Job[job].Stat[(int)Stat.Spirit]; // base “Speed” on Stat.Spirit

        long val = (long)(1 + (speed / 2) + baseJobSpirit) * 2L;
        return (int)Math.Max(1, Math.Min(int.MaxValue, val));
    }

    public int GetPlayerMaxVital(int index, Vital vital)
    {
        switch (vital)
        {
            case Vital.Health:
                return GetPlayerMaxHP(index);

            case Vital.Mana:
                return GetPlayerMaxMP(index);

            case Vital.Stamina:
                return GetPlayerMaxSP(index);
                
            default:
                return 1;
        }
    }
}