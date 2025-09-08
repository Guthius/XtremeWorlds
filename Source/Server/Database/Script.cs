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
using Constant = Core.Globals.Constant;
using EventCommand = Core.Globals.EventCommand;
using Type = Core.Globals.Type;
using Microsoft.Extensions.Logging;

public class Script
{
    // Add a per-player pickup lock
    private static bool[] _isPickingUp = new bool[Constant.MaxPlayers];

    // Timers for periodic regeneration
    private static long _lastNpcRegen;
    private static long _lastPlayerRegen;
    private const int NpcRegenIntervalMs = 10000; // 10 seconds like legacy
    private const int PlayerRegenIntervalMs = 5000; // 5 seconds (adjust as desired)

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
        PlayerWarp(index, GetPlayerMap(index), GetPlayerX(index), GetPlayerY(index), (byte)Direction.Down);

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
        Server.Item.SpawnItemSlot(mapSlot, itemNum, amount, mapNum, GetPlayerX(index), GetPlayerY(index));
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
        Server.Item.SendMapItemToAll(mapNum, mapSlot);
        NetworkSend.SendInventoryUpdate(index, invSlot);
        NetworkSend.SendActionMsg(GetPlayerMap(index), msg, (int)ColorName.White, (byte)ActionMessageType.Static, GetPlayerX(index) * 32, GetPlayerY(index) * 32);

        // Unlock pickup for this player
        _isPickingUp[index] = false;
    }

    public void UnEquipItem(int index, int itemNum, int eqSlot)
    {
        int m;

        itemNum = GetPlayerEquipment(index, (Equipment)eqSlot);

        m = FindOpenInvSlot(index, (int)Data.Player[index].Equipment[eqSlot].Num);
        SetPlayerInv(index, m, Data.Player[index].Equipment[eqSlot].Num);
        Data.Player[index].Inv[m].Bound = Data.Player[index].Equipment[eqSlot].Bound;
        SetPlayerInvValue(index, m, 0);

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

    public void UseItem(int index, int itemNum, int invNum)
    {
        // removed unused variable 'i'
        int n;
        var tempItem = default(int);
        int m;
        var tempdata = new int[Enum.GetValues(typeof(Stat)).Length + 4];
        var tempstr = new string[3];

        // Find out what kind of item it is
        switch (Data.Item[itemNum].Type)
        {
            case (byte)ItemCategory.Equipment:
                {
                    // All equipment types use the same logic, just with different slots
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
                        SetPlayerInvValue(index, m, 0);
                    }
                    NetworkSend.SendWornEquipment(index);
                    NetworkSend.SendMapEquipment(index);
                    NetworkSend.SendInventory(index);
                    NetworkSend.SendInventoryUpdate(index, invNum);
                    NetworkSend.SendStats(index);
                    // send vitals
                    NetworkSend.SendVitals(index);
                    break;
                }

            case (byte)ItemCategory.Consumable:
                {
                    switch (Data.Item[itemNum].SubType)
                    {
                        case (byte)ConsumableEffect.RestoresHealth:
                            {
                                NetworkSend.SendActionMsg(GetPlayerMap(index), "+" + Data.Item[itemNum].Data1, (int)ColorName.BrightGreen, (byte)ActionMessageType.Scroll, GetPlayerX(index) * 32, GetPlayerY(index) * 32);
                                Server.Animation.SendAnimation(GetPlayerMap(index), Data.Item[itemNum].Animation, 0, 0, (byte)TargetType.Player, index);
                                SetPlayerVital(index, Vital.Health, GetPlayerVital(index, Vital.Health) + Data.Item[itemNum].Data1);
                                if (Data.Item[itemNum].Stackable == 1)
                                {
                                    TakeInv(index, itemNum, 1);
                                }
                                else
                                {
                                    TakeInv(index, itemNum, 0);
                                }
                                NetworkSend.SendVital(index, Vital.Health);
                                break;
                            }

                        case (byte)ConsumableEffect.RestoresMana:
                            {
                                NetworkSend.SendActionMsg(GetPlayerMap(index), "+" + Data.Item[itemNum].Data1, (int)ColorName.BrightBlue, (byte)ActionMessageType.Scroll, GetPlayerX(index) * 32, GetPlayerY(index) * 32);
                                Server.Animation.SendAnimation(GetPlayerMap(index), Data.Item[itemNum].Animation, 0, 0, (byte)TargetType.Player, index);
                                SetPlayerVital(index, Vital.Stamina, GetPlayerVital(index, Vital.Stamina) + Data.Item[itemNum].Data1);
                                if (Data.Item[itemNum].Stackable == 1)
                                {
                                    TakeInv(index, itemNum, 1);
                                }
                                else
                                {
                                    TakeInv(index, itemNum, 0);
                                }
                                NetworkSend.SendVital(index, Vital.Stamina);
                                break;
                            }

                        case (byte)ConsumableEffect.RestoresStamina:
                            {
                                Server.Animation.SendAnimation(GetPlayerMap(index), Data.Item[itemNum].Animation, 0, 0, (byte)TargetType.Player, index);
                                SetPlayerVital(index, Vital.Stamina, GetPlayerVital(index, Vital.Stamina) + Data.Item[itemNum].Data1);
                                if (Data.Item[itemNum].Stackable == 1)
                                {
                                    TakeInv(index, itemNum, 1);
                                }
                                else
                                {
                                    TakeInv(index, itemNum, 0);
                                }
                                NetworkSend.SendVital(index, Vital.Stamina);
                                break;
                            }

                        case (byte)ConsumableEffect.GrantsExperience:
                            {
                                Server.Animation.SendAnimation(GetPlayerMap(index), Data.Item[itemNum].Animation, 0, 0, (byte)TargetType.Player, index);
                                SetPlayerExp(index, GetPlayerExp(index) + Data.Item[itemNum].Data1);
                                if (Data.Item[itemNum].Stackable == 1)
                                {
                                    TakeInv(index, itemNum, 1);
                                }
                                else
                                {
                                    TakeInv(index, itemNum, 0);
                                }
                                NetworkSend.SendExp(index);
                                break;
                            }

                    }

                    break;
                }

            case (byte)ItemCategory.Projectile:
                {
                    if (Data.Item[itemNum].Ammo > 0)
                    {
                        if (HasItem(index, Data.Item[itemNum].Ammo) > 0)
                        {
                            TakeInv(index, Data.Item[itemNum].Ammo, 1);
                            Server.Projectile.PlayerFireProjectile(index);
                        }
                        else
                        {
                            NetworkSend.PlayerMsg(index, "No More " + Data.Item[Data.Item[GetPlayerEquipment(index, Equipment.Weapon)].Ammo].Name + " !", (int)ColorName.BrightRed);
                            return;
                        }
                    }
                    else
                    {
                        Server.Projectile.PlayerFireProjectile(index);
                        return;
                    }

                    break;
                }

            case (byte)ItemCategory.Event:
                {
                    n = Data.Item[itemNum].Data1;

                    switch (Data.Item[itemNum].SubType)
                    {
                        case (byte)EventCommand.ModifyVariable:
                            {
                                Data.Player[index].Variables[n] = Data.Item[itemNum].Data2;
                                break;
                            }
                        case (byte)EventCommand.ModifySwitch:
                            {
                                Data.Player[index].Switches[n] = (byte)Data.Item[itemNum].Data2;
                                break;
                            }
                        case (byte)EventCommand.Key:
                            {
                                EventLogic.TriggerEvent(index, 1, 0, GetPlayerX(index), GetPlayerY(index));
                                break;
                            }
                    }

                    break;
                }

            case (byte)ItemCategory.Skill:
                {
                    PlayerLearnSkill(index, itemNum);
                    break;
                }
        }
    }

    public static void PlayerLearnSkill(int index, int itemNum, int skillNum = -1)
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

        if (n < 0 | n > Constant.MaxSkills)
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
                            Server.Animation.SendAnimation(GetPlayerMap(index), Data.Item[itemNum].Animation, 0, 0, (byte)TargetType.Player, index);
                            TakeInv(index, itemNum, 0);
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
                        SendPlayerXyTo(index, player.Id);
                        NetworkSend.SendMapEquipmentTo(index, player.Id);
                    }
                }
            }
        }

        EventLogic.SpawnMapEventsFor(index, GetPlayerMap(index));

        // Send index's player data to everyone on the map including himself
        data = GetPlayerDataPacket(index);
        NetworkConfig.SendDataToMap(mapNum, data);
        SendPlayerXyToMap(index);
        NetworkSend.SendMapEquipment(index);
        NetworkSend.SendVitals(index);
    }

    public void LeaveMap(int index, int mapNum)
    {

    }

    public void LeftGame(int index)
    {

    }

    public void OnDeath(int index)
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

        // to the bootmap if it is set
        if (withBlock.BootMap > 0)
        {
            PlayerWarp(index, withBlock.BootMap, withBlock.BootX, withBlock.BootY, (int)Direction.Down);
        }
        else
        {
            PlayerWarp(index, Data.Job[GetPlayerJob(index)].StartMap, Data.Job[GetPlayerJob(index)].StartX, Data.Job[GetPlayerJob(index)].StartY, (int)Direction.Down);
        }
    }

    public void BufferSkill(int mapNum, int index, int skillNum)
    {

    }

    public int KillPlayer(int index)
    {
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
        if (GetPlayerRawStat(index, (Stat)tmpStat) >= Constant.MaxStats)
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
            var mapNum = entity.Map;

            // Only process entities that are Npcs
            if (entity.Num < 0) continue;

            // check if they've completed casting, and if so set the actual skill going
            if (entity.SkillBuffer >= 0)
            {
                if (General.GetTimeMs() > entity.SkillBufferTimer + Data.Skill[entity.SkillBuffer].CastTime * 1000)
                {
                    if (Data.Moral[Data.Map[mapNum].Moral].CanCast)
                    {
                        //BufferSkill(mapNum, [Core.Globals.Entity.Index(entity), entity.SkillBuffer);
                        entity.SkillBuffer = -1;
                        entity.SkillBufferTimer = 0;
                    }
                }
            }
            else
            {
                // ATTACKING ON SIGHT
                if (entity.Behaviour == (byte)NpcBehavior.AttackOnSight || entity.Behaviour == (byte)NpcBehavior.Guard)
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
                                    int n = entity.Range;
                                    int distanceX = entity.X - GetPlayerX(player.Id);
                                    int distanceY = entity.Y - GetPlayerY(player.Id);

                                    if (distanceX < 0) distanceX *= -1;
                                    if (distanceY < 0) distanceY *= -1;

                                    if (distanceX <= n && distanceY <= n)
                                    {
                                        if (entity.Behaviour == (byte)NpcBehavior.AttackOnSight || GetPlayerPk(player.Id))
                                        {
                                            if (!string.IsNullOrEmpty(entity.AttackSay))
                                            {
                                                NetworkSend.PlayerMsg(player.Id, GameLogic.CheckGrammar(entity.Name, 1) + " says, '" + entity.AttackSay + "' to you.", (int)ColorName.Yellow);
                                            }
                                            entity.TargetType = (byte)TargetType.Player;
                                            entity.Target = player.Id;
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
                                        int n = entity.Range;
                                        int distanceX = entity.X - otherEntity.X;
                                        int distanceY = entity.Y - otherEntity.Y;

                                        if (distanceX < 0) distanceX *= -1;
                                        if (distanceY < 0) distanceY *= -1;

                                        if (distanceX <= n && distanceY <= n && entity.Behaviour == (byte)NpcBehavior.AttackOnSight)
                                        {
                                            entity.TargetType = (byte)TargetType.Npc;
                                            entity.Target = i;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Attempt attack using new combat system when target acquired
                if (entity != null && entity.Target > 0)
                {
                    if (entity.TargetType == (byte)TargetType.Player)
                    {
                        var pid = entity.Target;
                        if (NetworkConfig.IsPlaying(pid) && GetPlayerMap(pid) == mapNum)
                        {
                            var targetEntity = Core.Globals.Entity.FromPlayer(pid, Data.Player[pid]);
                            targetEntity.Map = mapNum;
                            AttemptAttack(entity, targetEntity);
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
                                AttemptAttack(entity, targetEntity);
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
                if (entity.Vital[(byte)Vital.Health] < 0 && entity.SpawnWait > 0)
                {
                    entity.Num = 0;
                    entity.SpawnWait = General.GetTimeMs();
                    entity.Vital[(byte)Vital.Health] = 0;
                }
#pragma warning restore CS8602

                if (entity.Type == Core.Globals.Entity.EntityType.Npc && entity.Num == -1 && entity.SpawnSecs > 0)
                {
                    if (tickCount > entity.SpawnWait + entity.SpawnSecs * 1000)
                    {
                        Server.Npc.SpawnNpc(x, mapNum);
                    }
                }
            }
        }

        var now = General.GetTimeMs();
        var itemCount = Constant.MaxMapItems;
        var mapCount = Constant.MaxMaps;

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
                        Server.Item.SendMapItemToAll(mapNum, i);
                    }

                    if (item.CanDespawn && item.DespawnTimer < now)
                    {
                        Database.ClearMapItem(i, mapNum);
                        Server.Item.SendMapItemToAll(mapNum, i);
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
                                Server.Resource.SendMapResourceToMap(mapNum);
                            }
                        }
                    }
                }
            }
        }

        // Group vital regeneration executed after NPC AI loop (wrapped for script safety)
        RunRegen();
    }

    public void CheckPlayerLevelUp(int index)
    {
        int level_count;

        level_count = 0;

        while (GetPlayerExp(index) >= GetPlayerNextLevel(index))
        {
            var expRollover = GetPlayerExp(index) - GetPlayerNextLevel(index);
            SetPlayerLevel(index, GetPlayerLevel(index) + 1);
            SetPlayerPoints(index, GetPlayerPoints(index) + Server.Constant.StatPerLevel);
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

    public void RunRegen()
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
                    int amount = Math.Max(1, Data.Npc[e.Num].Stat[(byte)Stat.Vitality] / 3);
                    e.Vital[(byte)Vital.Health] = Math.Min(maxHp, curHp + amount);
                    Server.Npc.SendMapNpcVitals(e.Map, (byte)Core.Globals.Entity.Index(e));
                }
                int maxMana = GameLogic.GetNpcMaxVital(e.Num, Vital.Mana);
                if (maxMana > 0)
                {
                    int curMana = e.Vital[(byte)Vital.Mana];
                    if (curMana < maxMana)
                    {
                        int amount = Math.Max(1, Data.Npc[e.Num].Stat[(byte)Stat.Intelligence] / 3);
                        e.Vital[(byte)Vital.Mana] = Math.Min(maxMana, curMana + amount);
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
                    int amount = Math.Max(1, GetPlayerStat(id, Stat.Vitality) / 3);
                    SetPlayerVital(id, Vital.Health, Math.Min(hpMax, hpCur + amount));
                    NetworkSend.SendVital(id, Vital.Health);
                }
                int manaMax = GetPlayerMaxVital(id, Vital.Mana);
                int manaCur = GetPlayerVital(id, Vital.Mana);
                if (manaCur < manaMax)
                {
                    int amount = Math.Max(1, GetPlayerStat(id, Stat.Spirit) / 4);
                    SetPlayerVital(id, Vital.Mana, Math.Min(manaMax, manaCur + amount));
                    NetworkSend.SendVital(id, Vital.Mana);
                }
                int stamMax = GetPlayerMaxVital(id, Vital.Stamina);
                int stamCur = GetPlayerVital(id, Vital.Stamina);
                if (stamCur < stamMax)
                {
                    int amount = Math.Max(1, GetPlayerStat(id, Stat.Intelligence) / 5);
                    SetPlayerVital(id, Vital.Stamina, Math.Min(stamMax, stamCur + amount));
                    NetworkSend.SendVital(id, Vital.Stamina);
                }
            }
        }
    }
    
      private const int BaseAttackSpeedMs = 1000; // fallback when no weapon speed

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

    private static void HandleDeath(Entity attacker, Entity target)
    {
        if (target.Type == Entity.EntityType.Player)
        {
            // Use Script pipeline for experience/penalties
            Server.Script.Instance?.KillPlayer(target.Id);
            NetworkSend.GlobalMsg(GetPlayerName(target.Id) + " was slain by " + GetEntityDisplayName(attacker) + ".");
        }
        else if (target.Type == Entity.EntityType.Npc)
        {
            var map = target.Map;
            var mapNpcNum = target.Id;
            if (map >= 0 && map < Data.MapNpc.Length && mapNpcNum >= 0 && mapNpcNum < Core.Globals.Constant.MaxMapNpcs)
            {
                DropNpcLoot(map, mapNpcNum);
            }
        }
    }

    private static string GetEntityDisplayName(Entity e)
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

    private static bool IsSkillRanged(int? skillId)
    {
        if (!skillId.HasValue) return false;
        var id = skillId.Value;
        if (id < 0 || id >= Data.Skill.Length) return false;
        return Data.Skill[id].Range > 1; // simple heuristic
    }

    public static bool AttemptAttack(Entity attacker, Entity target, int? skillId = null)
    {
        if (attacker == null || target == null) return false;
        if (attacker.Map != target.Map) return false;
        if (!IsAlive(attacker) || !IsAlive(target)) return false;
        if (!IsSkillRanged(skillId) && !IsInMeleeRange(attacker, target)) return false;

        var now = General.GetTimeMs();
        var cd = GetAttackSpeed(attacker, skillId);
        if (attacker.AttackTimer + cd > now) return false;

        var dmg = CalculateDamage(attacker, target, skillId);
        var killed = ApplyDamageExtended(attacker, target, dmg, skillId);

        // set cooldown
        attacker.AttackTimer = (int) now; // attacker is a snapshot; we must also update underlying store
        UpdateUnderlyingAttackTimer(attacker, (int) now);
        BroadcastAttack(attacker);

        if (killed)
        {
            HandleDeath(attacker, target);
        }
        return true;
    }

    private static bool IsAlive(Entity e)
    {
        if (e.Vital == null) return false;
        return e.Vital[(int)Vital.Health] > 0;
    }

    private static bool IsInMeleeRange(Entity a, Entity b)
    {
        // Tile-based adjacency (4-direction)
        var ax = a.X / 32; var ay = a.Y / 32;
        var bx = b.X / 32; var by = b.Y / 32;
        var dx = Math.Abs(ax - bx);
        var dy = Math.Abs(ay - by);
        return (dx + dy) == 1; // orthogonal neighbor
    }

    private static int GetAttackSpeed(Entity attacker, int? skillId)
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

    private static int GetEquippedItemId(Entity player, Equipment eq)
    {
        if (player.Equipment == null) return -1;
        var slot = (int)eq;
        if (slot < 0 || slot >= player.Equipment.Length) return -1;
        return player.Equipment[slot].Num;
    }

    private static DamageResult CalculateDamage(Entity attacker, Entity target, int? skillId)
    {
        var result = new DamageResult();

        // Base raw damage
        int raw = 0;
        if (attacker.Type == Entity.EntityType.Player)
        {
            var str = SafeStat(attacker, Stat.Strength);
            var lvl = attacker.Level;
            var weaponId = GetEquippedItemId(attacker, Equipment.Weapon);
            if (weaponId >= 0)
            {
                raw = str * 2 + Data.Item[weaponId].Data2 * 2 + lvl * 3 + (int)General.GetRandom.NextDouble(0d, 20d);
            }
            else
            {
                raw = str * 2 + lvl * 3 + (int)General.GetRandom.NextDouble(0d, 20d);
            }
        }
        else // NPC
        {
            if (attacker.Num >= 0 && attacker.Num < Data.Npc.Length)
            {
                raw = Math.Max(1, Data.Npc[attacker.Num].Damage);
            }
        }

        // Skill modifier (very naive: + Skill.Level * 2 etc.)
        if (skillId.HasValue && skillId.Value >= 0 && skillId.Value < Data.Skill.Length)
        {
            raw = (int)(raw * 1.1); // placeholder scaling
        }

        result.Raw = raw;

        // Defense / mitigation
        int mitigation = 0;
        if (target.Type == Entity.EntityType.Player)
        {
            mitigation += SafeStat(target, Stat.Spirit) * 2 + target.Level * 3;
            mitigation += SumArmor(target);
        }
        else if (target.Type == Entity.EntityType.Npc)
        {
            // NPC spirit & level stored in Data.Npc template
            if (target.Num >= 0 && target.Num < Data.Npc.Length)
            {
                mitigation += (int)Data.Npc[target.Num].Stat[(int)Stat.Spirit] * 2 + Data.Npc[target.Num].Level * 3;
            }
        }

        var mitigated = Math.Max(0, raw - mitigation);

        // Defensive rolls only if mitigated > 0
        if (mitigated > 0)
        {
            result.Dodge = Roll(SafeStat(target, Stat.Luck) / 4);
            if (!result.Dodge) result.Parry = Roll(SafeStat(target, Stat.Luck) / 6);
            if (!result.Dodge && !result.Parry)
            {
                result.Block = HasShield(target) && Roll((SafeStat(target, Stat.Luck) / 2 + target.Level / 2));
            }
        }

        // Critical only if attack not fully avoided
        if (!result.Dodge && !result.Parry && !result.Block)
        {
            result.Crit = Roll(SafeStat(attacker, Stat.Strength) / 2 + attacker.Level / 2);
            if (result.Crit)
            {
                mitigated = (int)Math.Round(mitigated * 1.5);
            }
        }

        if (result.Block)
        {
            mitigated = 0; // simple full block
        }

        result.Mitigated = mitigated;
        return result;
    }

    private static int SafeStat(Entity e, Stat stat)
    {
        if (e.Stat == null) return 0;
        var idx = (int)stat;
        if (idx < 0 || idx >= e.Stat.Length) return 0;
        return e.Stat[idx];
    }

    private static int SumArmor(Entity player)
    {
        if (player.Type != Entity.EntityType.Player || player.Equipment == null) return 0;
        int total = 0;
        for (int i = 0; i < player.Equipment.Length; i++)
        {
            var itemNum = player.Equipment[i].Num;
            if (itemNum >= 0 && itemNum < Data.Item.Length)
            {
                total += Data.Item[itemNum].Data2;
            }
        }
        return total / 6; // legacy divide
    }

    private static bool HasShield(Entity e)
    {
        if (e.Type != Entity.EntityType.Player || e.Equipment == null) return false;
        var shieldId = GetEquippedItemId(e, Equipment.Shield);
        return shieldId >= 0;
    }

    private static bool Roll(int threshold)
    {
        if (threshold <= 0) return false;
        var roll = (int)Math.Round(General.GetRandom.NextDouble(1d, 100d));
        return roll <= threshold;
    }

    private static void ApplyDamage(Entity attacker, Entity target, DamageResult dmg, int? skillId)
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
            // Death check
            if (newHp <= 0)
            {
                // TODO: integrate OnDeath pipeline via existing Script / Player methods
            }
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
                // Death
                if (newHp <= 0)
                {
                    Data.MapNpc[map].Npc[mapNpcNum].Num = -1;
                    Data.MapNpc[map].Npc[mapNpcNum].SpawnWait = (int)General.GetTimeMs();
                    Data.MapNpc[map].Npc[mapNpcNum].Vital[hpIndex] = 0;
                    Server.Npc.SendMapNpcVitals(map, (byte)mapNpcNum); // may need access adjustment
                }
                else
                {
                    Server.Npc.SendMapNpcVitals(map, (byte)mapNpcNum);
                }
            }
        }
    }

    private static void UpdateUnderlyingAttackTimer(Entity entity, int newTime)
    {
        if (entity.Type == Entity.EntityType.Player)
        {
            Data.TempPlayer[entity.Id].AttackTimer = newTime;
        }
        else if (entity.Type == Entity.EntityType.Npc)
        {
            if (entity.Map >= 0 && entity.Map < Data.MapNpc.Length && entity.Id >= 0 && entity.Id < Core.Globals.Constant.MaxMapNpcs)
            {
                Data.MapNpc[entity.Map].Npc[entity.Id].AttackTimer = newTime;
            }
        }
    }

    private static void BroadcastAttack(Entity attacker)
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

    private static bool ApplyDamageExtended(Entity attacker, Entity target, DamageResult dmg, int? skillId)
    {
        // reuse existing ApplyDamage but capture death result
        var before = target.Vital != null ? target.Vital[(int)Vital.Health] : 0;
        ApplyDamage(attacker, target, dmg, skillId);
        var after = target.Type == Entity.EntityType.Player ? GetPlayerVital(target.Id, Vital.Health) : (target.Vital != null ? target.Vital[(int)Vital.Health] : 0);
        // For npc we will read from Data.MapNpc after ApplyDamage mutated underlying
        if (target.Type == Entity.EntityType.Npc && target.Map >= 0 && target.Map < Data.MapNpc.Length && target.Id >= 0 && target.Id < Core.Globals.Constant.MaxMapNpcs)
        {
            after = Data.MapNpc[target.Map].Npc[target.Id].Vital[(int)Vital.Health];
            if (after <= 0)
            {
                // ensure Num marked dead
                Data.MapNpc[target.Map].Npc[target.Id].Num = -1;
            }
        }
        return before > 0 && after <= 0;
    }

    private static void DropNpcLoot(int mapNum, int mapNpcNum)
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
                Server.Item.SpawnItem(itemId, itemVal, mapNum, mapNpc.X / 32, mapNpc.Y / 32);
            }
        }
    }
}