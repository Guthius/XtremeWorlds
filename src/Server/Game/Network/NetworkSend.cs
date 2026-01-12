using Core;
using Core.Globals;
using Core.Net;
using Core.Objects;
using Microsoft.Extensions.Logging;
using Server.Game;
using Server.Game.Net;
using System.Net;
using System.Text;
using static Core.Globals.Commands;
using static Core.Net.Packets;

namespace Server;

public static class NetworkSend
{
    private static readonly int EquipmentCount = Enum.GetValues<Equipment>().Length;
    private static readonly int StatCount = Enum.GetValues<Stat>().Length;
    private static readonly int VitalCount = Enum.GetValues<Vital>().Length;
    private static readonly int MapLayerCount = Enum.GetValues<MapLayer>().Length;
    private static readonly int ResourceSkillCount = Enum.GetValues<ResourceSkill>().Length;

    public static void SendAlertMessage(GameSession session, SystemMessage menuNo, Menu menuReset = 0, bool kick = true)
    {
        var packetWriter = new PacketWriter(16);

        packetWriter.WriteEnum(ServerPackets.SAlertMsg);
        packetWriter.WriteByte((byte)menuNo);
        packetWriter.WriteByte((byte)menuReset);
        packetWriter.WriteBoolean(kick);

        session.Channel.Send(packetWriter.GetBytes());

        if (kick)
        {
            session.Channel.Close();
        }
        else
        {
            _ = Player.OnExit(session.Id);
        }
    }

    public static void SendAlertMessage(int playerId, SystemMessage menuNo, Menu menuReset = 0, bool kick = true)
    {
        var packetWriter = new PacketWriter(16);
        packetWriter.WriteEnum(ServerPackets.SAlertMsg);
        packetWriter.WriteByte((byte)menuNo);
        packetWriter.WriteByte((byte)menuReset);
        packetWriter.WriteBoolean(kick);
        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());

        if (kick)
        {
            PlayerService.Instance.Disconnect(playerId);
        }
        else
        {
            _ = Player.OnExit(playerId);
        }
    }

    public static void SendGlobalMessage(string message)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SGlobalMsg);
        packetWriter.WriteString(message);

        PlayerService.Instance.SendDataToAll(packetWriter.GetBytes());
    }

    public static void SendPlayerMessage(int playerId, string message, int color)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SPlayerMsg);
        packetWriter.WriteString(message);
        packetWriter.WriteInt32(color);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendStartSkillBuffer(int playerId, int skillSlot, int castTimeSeconds)
    {
        var packetWriter = new PacketWriter(20);
        packetWriter.WriteEnum(ServerPackets.SStartSkillBuffer);
        packetWriter.WriteInt32(skillSlot);
        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendClearSkillBuffer(int playerId)
    {
        var packetWriter = new PacketWriter(4);
        packetWriter.WriteEnum(ServerPackets.SClearSkillBuffer);
        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendSkillCooldown(int playerId, int skillSlot)
    {
        var packetWriter = new PacketWriter(8);
        packetWriter.WriteEnum(ServerPackets.SCooldown);
        packetWriter.WriteInt32(skillSlot);
        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendPlayerCharacters(GameSession session)
    {
        var w = new PacketWriter();

        w.WriteEnum(ServerPackets.SPlayerCharacters);

        // Send exactly MaxCharacters entries in a predictable format
        for (int slot = 0; slot < Variables.MaxCharacters; slot++)
        {
            // Defensive guards
            string name = Account.Instance[session.Id].Player[slot].Name ?? string.Empty;
            int sprite = Account.Instance[session.Id].Player[slot].Sprite;
            int access = Account.Instance[session.Id].Player[slot].Access;
            int job = Account.Instance[session.Id].Player[slot].Job;

            w.WriteString(name);
            w.WriteInt32(sprite);
            w.WriteInt32(access);
            w.WriteInt32(job);

            // Equipment list may vary; clamp to EquipmentCount and guard item reads
            for (int eq = 0; eq < EquipmentCount; eq++)
            {
                int itemId = (eq < Account.Instance[session.Id].Player[slot].Paperdoll.Length)
                    ? Account.Instance[session.Id].Player[slot].Paperdoll[eq].Num
                    : -1;

                if (itemId >= 0 && itemId < Item.Instance.Count)
                {
                    w.WriteInt32(Item.Instance[itemId].Paperdoll);
                }
                else
                {
                    w.WriteInt32(-1);
                }
            }
        }

        session.Channel.Send(w.GetBytes());
    }

    public static void SendVariables(GameSession session)
    {
        // Send authoritative variables from script getters so client can size arrays correctly
        var w = new PacketWriter();
        w.WriteEnum(ServerPackets.SVariables);

        w.WriteInt32(Variables.MaxAnimations);
        w.WriteInt32(Variables.MaxItems);
        w.WriteInt32(Variables.MaxMaps);
        w.WriteInt32(Variables.MaxNpcs);
        w.WriteInt32(Variables.MaxParty);
        w.WriteInt32(Variables.MaxPartyMembers);
        w.WriteInt32(Variables.MaxPlayers);
        w.WriteInt32(Variables.MaxResources);
        w.WriteInt32(Variables.MaxShops);
        w.WriteInt32(Variables.MaxSkills);
        w.WriteInt32(Variables.MaxProjectiles);
        w.WriteInt32(Variables.MaxSwitches);
        w.WriteInt32(Variables.MaxVariables);
        w.WriteInt32(Variables.ChatLines);
        w.WriteInt32(Variables.MaxEvents);
        w.WriteInt32(Variables.TileSize);
        w.WriteInt32(Variables.MaxWeatherParticles);

        w.WriteByte(Variables.MaxBank);
        w.WriteByte(Variables.MaxJobs);
        w.WriteByte(Variables.MaxMorals);
        w.WriteByte(Variables.MaxInventory);
        w.WriteByte(Variables.MaxMapItems);

        w.WriteInt32(Variables.MaxMapNpcs);
        
        w.WriteByte(Variables.MaxNpcSkills);
        w.WriteByte(Variables.MaxPlayerSkills);
        w.WriteByte(Variables.MaxTrades);
        w.WriteByte(Variables.NameLength);
        w.WriteByte(Variables.MinimumNameLength);
        w.WriteByte(Variables.ChatLength);
        w.WriteByte(Variables.MaxHotbar);
        w.WriteByte(Variables.MaxMapX);
        w.WriteByte(Variables.MaxMapY);
        w.WriteByte(Variables.MaxDropItems);
        w.WriteByte(Variables.MaxStartItems);
        w.WriteByte(Variables.MaxStartSkills);
        w.WriteByte(Variables.MaxCharacters);
        w.WriteByte(Variables.MaxStats);
        w.WriteByte(Variables.MaxQuests);
        w.WriteByte(Variables.MaxGuilds);
        w.WriteByte(Variables.MaxEventChoices);
        w.WriteByte(Variables.MaxLevel);
        w.WriteInt32(Variables.MaxPoints);

        session.Channel.Send(w.GetBytes());
    }

    public static void SendCloseTrade(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SCloseTrade);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendExperience(int playerId)
    {
        var packetWriter = new PacketWriter(16);

        packetWriter.WriteEnum(ServerPackets.SPlayerExp);
        packetWriter.WriteInt32(playerId);
        packetWriter.WriteInt32(GetPlayerExperience(playerId));
        packetWriter.WriteInt32(Script.Instance?.GetPlayerNextLevel(playerId));

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendLoginOk(int playerId)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(ServerPackets.SLoginOk);
        packetWriter.WriteInt32(playerId);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendInGame(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SInGame);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendPlaySound(int playerId, string fileName, int x, int y)
    {
        var packetWriter = new PacketWriter();
        packetWriter.WriteEnum(ServerPackets.SPlaySound);
        packetWriter.WriteString(fileName);
        packetWriter.WriteInt32(x);
        packetWriter.WriteInt32(y);
        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendJobs(GameSession session)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SJobData);

        for (var i = 0; i < Core.Globals.Variables.MaxJobs; i++)
        {
            WriteJobDataToPacket(i, packetWriter);
        }

        session.Channel.Send(packetWriter.GetBytes());
    }

    public static void SendJobToAll(int job)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SJobData);

        for (var i = 0; i < Core.Globals.Variables.MaxJobs; i++)
        {
            WriteJobDataToPacket(i, packetWriter);
        }

        PlayerService.Instance.SendDataToAll(packetWriter.GetBytes());
    }

    public static void SendInventory(int playerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SInventory);

        for (var i = 0; i < Core.Globals.Variables.MaxInventory; i++)
        {
            packetWriter.WriteInt32(GetPlayerInv(playerId, i));
            packetWriter.WriteInt32(GetPlayerInvValue(playerId, i));
        }

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendLeftGame(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SLeftGame);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendMapEquipment(int playerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SMapWornEq);
        packetWriter.WriteInt32(playerId);

        for (var i = 0; i < EquipmentCount; i++)
        {
            packetWriter.WriteInt32(GetPlayerPaperdoll(playerId, (Equipment)i));
        }

        NetworkConfig.SendDataToMap(GetPlayerMap(playerId), packetWriter.GetBytes());
    }

    public static void SendMapEquipmentTo(int equipmentPlayerId, int sendToPlayerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SMapWornEq);
        packetWriter.WriteInt32(equipmentPlayerId);

        for (var i = 0; i < EquipmentCount; i++)
        {
            packetWriter.WriteInt32(GetPlayerPaperdoll(equipmentPlayerId, (Equipment)i));
        }

        PlayerService.Instance.SendDataTo(sendToPlayerId, packetWriter.GetBytes());
    }

    public static void SendShops(int playerId)
    {
        for (var i = 0; i < Core.Globals.Variables.MaxShops; i++)
        {
            SendUpdateShopTo(playerId, i);
        }
    }

    public static void SendUpdateShopTo(int playerId, int index)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SUpdateShop);
        WriteShopDataToPacket(index, packetWriter);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendUpdateShopToAll(int shopNum)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SUpdateShop);
        WriteShopDataToPacket(shopNum, packetWriter);

        PlayerService.Instance.SendDataToAll(packetWriter.GetBytes());
    }

    private static void WriteShopDataToPacket(int shopNum, PacketWriter packetWriter)
    {
        packetWriter.WriteInt32(shopNum);

        var shop = shopNum >= 0 && shopNum < Shop.Instance.Count ? Shop.Instance[shopNum] : new ShopBase();

        packetWriter.WriteInt32(shop.BuyRate);
        packetWriter.WriteString(shop.Name);

        var tradeItems = shop.TradeItem;
        for (var i = 0; i < Core.Globals.Variables.MaxTrades; i++)
        {
            if (tradeItems is not null && i < tradeItems.Length)
            {
                packetWriter.WriteInt32(tradeItems[i].CostItem);
                packetWriter.WriteInt32(tradeItems[i].CostValue);
                packetWriter.WriteInt32(tradeItems[i].Item);
                packetWriter.WriteInt32(tradeItems[i].ItemValue);
            }
            else
            {
                packetWriter.WriteInt32(0);
                packetWriter.WriteInt32(0);
                packetWriter.WriteInt32(0);
                packetWriter.WriteInt32(0);
            }
        }
    }

    public static void SendSkills(int playerId)
    {
        for (var i = 0; i < Core.Globals.Variables.MaxSkills; i++)
        {
            SendUpdateSkillTo(playerId, i);
        }
    }

    public static void SendUpdateSkillTo(int playerId, int index)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SUpdateSkill);
        WriteSkillDataToPacket(index, packetWriter);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendUpdateSkillToAll(int skillNum)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SUpdateSkill);
        WriteSkillDataToPacket(skillNum, packetWriter);

        PlayerService.Instance.SendDataToAll(packetWriter.GetBytes());
    }

    private static void WriteSkillDataToPacket(int index, PacketWriter packetWriter)
    {
        packetWriter.WriteInt32(index);

        var skill = index >= 0 && index < Skill.Instance.Count ? Skill.Instance[index] : new Skill();

        packetWriter.WriteInt32(skill.AccessReq);
        packetWriter.WriteInt32(skill.AoE);
        packetWriter.WriteInt32(skill.CastAnim);
        packetWriter.WriteInt32(skill.CastTime);
        packetWriter.WriteInt32(skill.CdTime);
        packetWriter.WriteInt32(skill.JobReq);
        packetWriter.WriteByte(skill.Dir);
        packetWriter.WriteInt32(skill.Duration);
        packetWriter.WriteInt32(skill.Icon);
        packetWriter.WriteInt32(skill.Interval);
        packetWriter.WriteBoolean(skill.IsAoE);
        packetWriter.WriteInt32(skill.LevelReq);
        packetWriter.WriteInt32(skill.Map);
        packetWriter.WriteInt32(skill.MpCost);
        packetWriter.WriteString(skill.Name);
        packetWriter.WriteInt32(skill.Range);
        packetWriter.WriteInt32(skill.SkillAnim);
        packetWriter.WriteInt32(skill.StunDuration);
        packetWriter.WriteByte(skill.Type);
        packetWriter.WriteInt32(skill.Vital);
        packetWriter.WriteInt32(skill.X);
        packetWriter.WriteInt32(skill.Y);
        packetWriter.WriteInt32(skill.IsProjectile);
        packetWriter.WriteInt32(skill.Projectile);
        packetWriter.WriteByte(skill.KnockBack);
        packetWriter.WriteByte(skill.KnockBackTiles);
        packetWriter.WriteInt32(skill.MultiDirMask);
        packetWriter.WriteInt32(skill.ChainOnHitSkillId);
        packetWriter.WriteByte(skill.CommonEventType);
        packetWriter.WriteInt32(skill.CommonEventData1);
        packetWriter.WriteInt32(skill.CommonEventData2);

        packetWriter.WriteSingle(skill.MoveSpeedMultiplier);

        packetWriter.WriteInt32(skill.SpCost);
    }

    public static void SendStats(int playerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SPlayerStats);
        packetWriter.WriteInt32(playerId);

        for (var i = 0; i < StatCount; i++)
        {
            packetWriter.WriteInt32(GetPlayerStat(playerId, (Stat)i));
        }

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendVitals(int playerId)
    {
        for (var i = 0; i < VitalCount; i++)
        {
            SendVital(playerId, (Vital)i);
        }
    }

    public static void SendVital(int playerId, Vital vital)
    {
        var packetWriter = new PacketWriter(12);

        switch (vital)
        {
            case Core.Globals.Vital.Health:
                packetWriter.WriteEnum(ServerPackets.SPlayerHP);
                break;

            case Core.Globals.Vital.Mana:
                packetWriter.WriteEnum(ServerPackets.SPlayerMP);
                break;

            case Core.Globals.Vital.Stamina:
                packetWriter.WriteEnum(ServerPackets.SPlayerSP);
                break;
        }

        packetWriter.WriteInt32(GetPlayerVital(playerId, vital));
        packetWriter.WriteInt32(Script.Instance?.GetPlayerMaxVital(playerId, vital));

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());

        if (Data.TempPlayer[playerId].InParty >= 0)
        {
            NetworkSend.SendPartyVitals(Data.TempPlayer[playerId].InParty, playerId);
        }
    }

    public static void SendWelcome(int playerId)
    {
        if (Variables.Welcome.Length > 0)
        {
            SendPlayerMessage(playerId, Variables.Welcome, (int)ColorName.BrightCyan);
        }

        SendWhosOnline(playerId);
    }

    public static void SendWhosOnline(int playerId)
    {
        if (GetPlayerAccess(playerId) < (int)AccessLevel.Moderator)
        {
            return;
        }

        var playerNames = PlayerService.Instance.PlayerIds
            .Where(otherPlayerId => otherPlayerId != playerId)
            .Select(GetPlayerName)
            .ToArray();

        string message;
        if (playerNames.Length == 0)
        {
            message = "There are no other players online.";
        }
        else
        {
            message = "There are " + playerNames.Length + " other players online: " + string.Join(", ", playerNames) + ".";
        }

        SendPlayerMessage(playerId, message, (int)ColorName.White);
    }

    public static void SendWornEquipment(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SPlayerWornEq);

        for (var i = 0; i < EquipmentCount; i++)
        {
            packetWriter.WriteInt32(GetPlayerPaperdoll(playerId, (Equipment)i));
        }

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendMapData(int playerId, int map, bool sendMap)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SMapData);

        if (sendMap)
        {
            packetWriter.WriteInt32(1);
            packetWriter.WriteInt32(map);
            packetWriter.WriteString(Server.Map.Instance[map].Name);
            packetWriter.WriteString(Server.Map.Instance[map].Music);
            packetWriter.WriteInt32(Server.Map.Instance[map].Revision);
            packetWriter.WriteByte(Server.Map.Instance[map].Moral);
            packetWriter.WriteInt32(Server.Map.Instance[map].Tileset);
            packetWriter.WriteInt32(Server.Map.Instance[map].Up);
            packetWriter.WriteInt32(Server.Map.Instance[map].Down);
            packetWriter.WriteInt32(Server.Map.Instance[map].Left);
            packetWriter.WriteInt32(Server.Map.Instance[map].Right);
            packetWriter.WriteInt32(Server.Map.Instance[map].BootMap);
            packetWriter.WriteByte(Server.Map.Instance[map].BootX);
            packetWriter.WriteByte(Server.Map.Instance[map].BootY);
            packetWriter.WriteByte(Server.Map.Instance[map].MaxX);
            packetWriter.WriteByte(Server.Map.Instance[map].MaxY);
            packetWriter.WriteByte(Server.Map.Instance[map].Weather);
            packetWriter.WriteInt32(Server.Map.Instance[map].Fog);
            packetWriter.WriteInt32(Server.Map.Instance[map].WeatherIntensity);
            packetWriter.WriteByte(Server.Map.Instance[map].FogOpacity);
            packetWriter.WriteByte(Server.Map.Instance[map].FogSpeed);
            packetWriter.WriteBoolean(Server.Map.Instance[map].MapTint);
            packetWriter.WriteByte(Server.Map.Instance[map].MapTintR);
            packetWriter.WriteByte(Server.Map.Instance[map].MapTintG);
            packetWriter.WriteByte(Server.Map.Instance[map].MapTintB);
            packetWriter.WriteByte(Server.Map.Instance[map].MapTintA);
            packetWriter.WriteByte(Server.Map.Instance[map].Panorama);
            packetWriter.WriteByte(Server.Map.Instance[map].Parallax);
            packetWriter.WriteByte(Server.Map.Instance[map].Brightness);
            packetWriter.WriteBoolean(Server.Map.Instance[map].NoRespawn);
            packetWriter.WriteBoolean(Server.Map.Instance[map].Indoors);
            packetWriter.WriteInt32(Server.Map.Instance[map].Shop);

            // Per-map camera zoom bounds
            packetWriter.WriteSingle(Server.Map.Instance[map].MinZoom);
            packetWriter.WriteSingle(Server.Map.Instance[map].MaxZoom);

            for (var i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
            {
                packetWriter.WriteInt32(Server.Map.Instance[map].Npc[i]);
            }

            for (var x = 0; x < Server.Map.Instance[map].MaxX; x++)
            {
                for (var y = 0; y < Server.Map.Instance[map].MaxY; y++)
                {
                    packetWriter.WriteInt32(Server.Map.Instance[map].Tile[x, y].Data1);
                    packetWriter.WriteInt32(Server.Map.Instance[map].Tile[x, y].Data2);
                    packetWriter.WriteInt32(Server.Map.Instance[map].Tile[x, y].Data3);
                    packetWriter.WriteInt32(Server.Map.Instance[map].Tile[x, y].Data1_2);
                    packetWriter.WriteInt32(Server.Map.Instance[map].Tile[x, y].Data2_2);
                    packetWriter.WriteInt32(Server.Map.Instance[map].Tile[x, y].Data3_2);
                    packetWriter.WriteByte(Server.Map.Instance[map].Tile[x, y].DirBlock);

                    for (var i = 0; i < MapLayerCount; i++)
                    {
                        packetWriter.WriteInt32(Server.Map.Instance[map].Tile[x, y].Layer[i].Tileset);
                        packetWriter.WriteInt32(Server.Map.Instance[map].Tile[x, y].Layer[i].X);
                        packetWriter.WriteInt32(Server.Map.Instance[map].Tile[x, y].Layer[i].Y);
                        packetWriter.WriteByte(Server.Map.Instance[map].Tile[x, y].Layer[i].AutoTile);
                    }

                    packetWriter.WriteInt32((int)Server.Map.Instance[map].Tile[x, y].Type);
                    packetWriter.WriteInt32((int)Server.Map.Instance[map].Tile[x, y].Type2);
                }
            }

            packetWriter.WriteInt32(Server.Map.Instance[map].EventCount);

            if (Server.Map.Instance[map].EventCount > 0)
            {
                for (var i = 0; i < Server.Map.Instance[map].EventCount; i++)
                {
                    ref var @event = ref Server.Map.Instance[map].Event[i];

                    packetWriter.WriteString(@event.Name);
                    packetWriter.WriteByte(@event.Globals);
                    packetWriter.WriteInt32(@event.X);
                    packetWriter.WriteInt32(@event.Y);
                    packetWriter.WriteInt32(@event.PageCount);

                    if (Server.Map.Instance[map].Event[i].PageCount == 0)
                    {
                        continue;
                    }

                    for (var x = 0; x < Server.Map.Instance[map].Event[i].PageCount; x++)
                    {
                        ref var eventPage = ref Server.Map.Instance[map].Event[i].Pages[x];

                        packetWriter.WriteInt32(eventPage.ChkVariable);
                        packetWriter.WriteInt32(eventPage.VariableIndex);
                        packetWriter.WriteInt32(eventPage.VariableCondition);
                        packetWriter.WriteInt32(eventPage.VariableCompare);
                        packetWriter.WriteInt32(eventPage.ChkSwitch);
                        packetWriter.WriteInt32(eventPage.SwitchIndex);
                        packetWriter.WriteInt32(eventPage.SwitchCompare);
                        packetWriter.WriteInt32(eventPage.ChkHasItem);
                        packetWriter.WriteInt32(eventPage.HasItemIndex);
                        packetWriter.WriteInt32(eventPage.HasItemAmount);
                        packetWriter.WriteInt32(eventPage.ChkSelfSwitch);
                        packetWriter.WriteInt32(eventPage.SelfSwitchIndex);
                        packetWriter.WriteInt32(eventPage.SelfSwitchCompare);
                        packetWriter.WriteByte(eventPage.GraphicType);
                        packetWriter.WriteInt32(eventPage.Graphic);
                        packetWriter.WriteInt32(eventPage.GraphicX);
                        packetWriter.WriteInt32(eventPage.GraphicY);
                        packetWriter.WriteInt32(eventPage.GraphicX2);
                        packetWriter.WriteInt32(eventPage.GraphicY2);
                        packetWriter.WriteByte(eventPage.MoveType);
                        packetWriter.WriteByte(eventPage.MoveSpeed);
                        packetWriter.WriteByte(eventPage.MoveFreq);
                        packetWriter.WriteInt32(eventPage.MoveRouteCount);
                        packetWriter.WriteInt32(eventPage.IgnoreMoveRoute);
                        packetWriter.WriteInt32(eventPage.RepeatMoveRoute);

                        if (eventPage.MoveRouteCount > 0)
                        {
                            for (int y = 0, count6 = eventPage.MoveRouteCount; y < count6; y++)
                            {
                                packetWriter.WriteInt32(eventPage.MoveRoute[y].Index);
                                packetWriter.WriteInt32(eventPage.MoveRoute[y].Data1);
                                packetWriter.WriteInt32(eventPage.MoveRoute[y].Data2);
                                packetWriter.WriteInt32(eventPage.MoveRoute[y].Data3);
                                packetWriter.WriteInt32(eventPage.MoveRoute[y].Data4);
                                packetWriter.WriteInt32(eventPage.MoveRoute[y].Data5);
                                packetWriter.WriteInt32(eventPage.MoveRoute[y].Data6);
                            }
                        }

                        packetWriter.WriteByte(eventPage.IdleAnim);
                        packetWriter.WriteByte(eventPage.DirFix);
                        packetWriter.WriteInt32(eventPage.WalkThrough);
                        packetWriter.WriteInt32(eventPage.ShowName);
                        packetWriter.WriteByte(eventPage.Trigger);
                        packetWriter.WriteInt32(eventPage.CommandListCount);
                        packetWriter.WriteByte(eventPage.Position);

                        if (Server.Map.Instance[map].Event[i].Pages[x].CommandListCount == 0)
                        {
                            continue;
                        }

                        for (var y = 0; y < Server.Map.Instance[map].Event[i].Pages[x].CommandListCount; y++)
                        {
                            packetWriter.WriteInt32(Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].CommandCount);
                            packetWriter.WriteInt32(Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].ParentList);

                            if (Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].CommandCount == 0)
                            {
                                continue;
                            }

                            for (var z = 0; z < Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].CommandCount; z++)
                            {
                                ref var eventCommand = ref Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].Commands[z];

                                packetWriter.WriteInt32(eventCommand.Index);
                                packetWriter.WriteString(eventCommand.Text1);
                                packetWriter.WriteString(eventCommand.Text2);
                                packetWriter.WriteString(eventCommand.Text3);
                                packetWriter.WriteString(eventCommand.Text4);
                                packetWriter.WriteString(eventCommand.Text5);
                                packetWriter.WriteInt32(eventCommand.Data1);
                                packetWriter.WriteInt32(eventCommand.Data2);
                                packetWriter.WriteInt32(eventCommand.Data3);
                                packetWriter.WriteInt32(eventCommand.Data4);
                                packetWriter.WriteInt32(eventCommand.Data5);
                                packetWriter.WriteInt32(eventCommand.Data6);
                                packetWriter.WriteInt32(eventCommand.ConditionalBranch.CommandList);
                                packetWriter.WriteInt32(eventCommand.ConditionalBranch.Condition);
                                packetWriter.WriteInt32(eventCommand.ConditionalBranch.Data1);
                                packetWriter.WriteInt32(eventCommand.ConditionalBranch.Data2);
                                packetWriter.WriteInt32(eventCommand.ConditionalBranch.Data3);
                                packetWriter.WriteInt32(eventCommand.ConditionalBranch.ElseCommandList);
                                packetWriter.WriteInt32(eventCommand.MoveRouteCount);

                                if (eventCommand.MoveRouteCount == 0)
                                {
                                    continue;
                                }

                                for (var w = 0; w < eventCommand.MoveRouteCount; w++)
                                {
                                    packetWriter.WriteInt32(eventCommand.MoveRoute[w].Index);
                                    packetWriter.WriteInt32(eventCommand.MoveRoute[w].Data1);
                                    packetWriter.WriteInt32(eventCommand.MoveRoute[w].Data2);
                                    packetWriter.WriteInt32(eventCommand.MoveRoute[w].Data3);
                                    packetWriter.WriteInt32(eventCommand.MoveRoute[w].Data4);
                                    packetWriter.WriteInt32(eventCommand.MoveRoute[w].Data5);
                                    packetWriter.WriteInt32(eventCommand.MoveRoute[w].Data6);
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            packetWriter.WriteInt32(0);
        }

        for (var i = 0; i < Core.Globals.Variables.MaxMapItems; i++)
        {
            packetWriter.WriteInt32(MapItem.Instance[map, i].Num);
            packetWriter.WriteInt32(MapItem.Instance[map, i].Value);
            packetWriter.WriteInt32(MapItem.Instance[map, i].X);
            packetWriter.WriteInt32(MapItem.Instance[map, i].Y);
        }

        for (var i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
        {
            packetWriter.WriteInt32(MapNpc.Instance[map, i].Num);
            packetWriter.WriteInt32(MapNpc.Instance[map, i].X);
            packetWriter.WriteInt32(MapNpc.Instance[map, i].Y);
            packetWriter.WriteByte(MapNpc.Instance[map, i].Dir);

            for (var x = 0; x < VitalCount; x++)
            {
                packetWriter.WriteInt32(MapNpc.Instance[map, i].Vital[x]);
            }
        }

        if (MapResource.Instance[GetPlayerMap(playerId)].ResourceCount > 0)
        {
            packetWriter.WriteInt32(1);
            packetWriter.WriteInt32(MapResource.Instance[GetPlayerMap(playerId)].ResourceCount);

            for (var i = 0; i < MapResource.Instance[GetPlayerMap(playerId)].ResourceCount; i++)
            {
                packetWriter.WriteByte(MapResource.Instance[GetPlayerMap(playerId)].ResourceData[i].State);
                packetWriter.WriteInt32(MapResource.Instance[GetPlayerMap(playerId)].ResourceData[i].X);
                packetWriter.WriteInt32(MapResource.Instance[GetPlayerMap(playerId)].ResourceData[i].Y);
            }
        }
        else
        {
            packetWriter.WriteInt32(0);
        }

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendJoinMap(int playerId)
    {
        try
        {
            Script.Instance?.OnMap(playerId);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static byte[] GetPlayerDataPacket(int playerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SPlayerData);
        packetWriter.WriteInt32(playerId);
        packetWriter.WriteString(GetPlayerName(playerId));
        packetWriter.WriteByte(GetPlayerJob(playerId));
        packetWriter.WriteInt32(GetPlayerLevel(playerId));
        packetWriter.WriteInt32(GetPlayerPoints(playerId));
        packetWriter.WriteInt32(GetPlayerSprite(playerId));
        packetWriter.WriteInt32(GetPlayerMap(playerId));
        packetWriter.WriteByte(GetPlayerAccess(playerId));
        packetWriter.WriteBoolean(GetPlayerPk(playerId));

        for (var i = 0; i < StatCount; i++)
        {
            packetWriter.WriteInt32(GetPlayerStat(playerId, (Stat)i));
        }

        for (var i = 0; i < ResourceSkillCount; i++)
        {
            packetWriter.WriteInt32(GetPlayerGatherSkillLevel(playerId, i));
            packetWriter.WriteInt32(GetPlayerGatherSkillExperience(playerId, i));
            packetWriter.WriteInt32(GetPlayerGatherSkillMaxExperience(playerId, i));
        }

        return packetWriter.GetBytes();
    }

    public static void SendPlayerXY(int playerId)
    {
        SendPlayerXYTo(playerId, playerId);
    }

    public static void SendPlayerXYTo(int sendToPlayerId, int positionPlayerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SPlayerXY);
        packetWriter.WriteInt32(positionPlayerId);
        packetWriter.WriteInt32(GetPlayerRawX(positionPlayerId));
        packetWriter.WriteInt32(GetPlayerRawY(positionPlayerId));
        packetWriter.WriteByte(GetPlayerDir(positionPlayerId));
        packetWriter.WriteByte(Player.Instance[positionPlayerId].Moving);
        packetWriter.WriteBoolean(Player.Instance[positionPlayerId].IsMoving);

        // Active movement speed multiplier (1.0f = normal).
        float mult = 1.0f;
        if (positionPlayerId >= 0 && positionPlayerId < Data.TempPlayer.Length)
        {
            mult = Data.TempPlayer[positionPlayerId].MoveSpeedMultiplier;
            if (mult <= 0) mult = 1.0f;
            var expiry = Data.TempPlayer[positionPlayerId].MoveSpeedMultiplierTimer;
            if (expiry > 0 && expiry <= General.GetTimeMs())
            {
                Data.TempPlayer[positionPlayerId].MoveSpeedMultiplier = 1.0f;
                Data.TempPlayer[positionPlayerId].MoveSpeedMultiplierTimer = 0;
                mult = 1.0f;
            }
        }
        packetWriter.WriteSingle(mult);

        PlayerService.Instance.SendDataTo(sendToPlayerId, packetWriter.GetBytes());
    }

    public static void SendPlayerXYToMap(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SPlayerXY);
        packetWriter.WriteInt32(playerId);
        packetWriter.WriteInt32(GetPlayerRawX(playerId));
        packetWriter.WriteInt32(GetPlayerRawY(playerId));
        packetWriter.WriteByte(GetPlayerDir(playerId));
        packetWriter.WriteByte(Player.Instance[playerId].Moving);
        packetWriter.WriteBoolean(Player.Instance[playerId].IsMoving);

        float mult = 1.0f;
        if (playerId >= 0 && playerId < Data.TempPlayer.Length)
        {
            mult = Data.TempPlayer[playerId].MoveSpeedMultiplier;
            if (mult <= 0) mult = 1.0f;
            var expiry = Data.TempPlayer[playerId].MoveSpeedMultiplierTimer;
            if (expiry > 0 && expiry <= General.GetTimeMs())
            {
                Data.TempPlayer[playerId].MoveSpeedMultiplier = 1.0f;
                Data.TempPlayer[playerId].MoveSpeedMultiplierTimer = 0;
                mult = 1.0f;
            }
        }
        packetWriter.WriteSingle(mult);

        NetworkConfig.SendDataToMap(GetPlayerMap(playerId), packetWriter.GetBytes());
    }

    public static void SendMapMessage(int map, string message)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SSendMapMessage);
        packetWriter.WriteString(message);

        NetworkConfig.SendDataToMap(map, packetWriter.GetBytes());
    }

    public static void SendAdminMessage(string message)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SSendAdminMessage);
        packetWriter.WriteString(message);

        foreach (var playerId in PlayerService.Instance.PlayerIds)
        {
            if (GetPlayerAccess(playerId) >= (int)AccessLevel.Moderator)
            {
                PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
            }
        }
    }

    public static void SendActionMessage(int map, string message, int color, int msgType, int x, int y, int playerOnlyNum = -1)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SActionMessage);
        packetWriter.WriteString(message);
        packetWriter.WriteInt32(color);
        packetWriter.WriteInt32(msgType);
        packetWriter.WriteInt32(x);
        packetWriter.WriteInt32(y);

        if (playerOnlyNum >= 0)
        {
            PlayerService.Instance.SendDataTo(playerOnlyNum, packetWriter.GetBytes());
        }
        else
        {
            NetworkConfig.SendDataToMap(map, packetWriter.GetBytes());
        }
    }

    public static void SayMsg_Map(int map, int playerId, string message, int sayColor)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SSayMsg);
        packetWriter.WriteString(GetPlayerName(playerId));
        packetWriter.WriteInt32((int)GetPlayerAccess(playerId));
        packetWriter.WriteBoolean(GetPlayerPk(playerId));
        packetWriter.WriteString(message);
        packetWriter.WriteString("[Map]:");
        packetWriter.WriteInt32(sayColor);

        NetworkConfig.SendDataToMap(map, packetWriter.GetBytes());
    }

    public static void SayMsg_Global(int playerId, string message, int sayColor)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SSayMsg);
        packetWriter.WriteString(GetPlayerName(playerId));
        packetWriter.WriteInt32((int)GetPlayerAccess(playerId));
        packetWriter.WriteBoolean(GetPlayerPk(playerId));
        packetWriter.WriteString(message);
        packetWriter.WriteString("[Global]:");
        packetWriter.WriteInt32(sayColor);

        PlayerService.Instance.SendDataToAll(packetWriter.GetBytes());
    }

    public static void SendPlayerData(int playerId)
    {
        NetworkConfig.SendDataToMap(GetPlayerMap(playerId), GetPlayerDataPacket(playerId));
    }

    public static void SendInventoryUpdate(int playerId, int invSlot)
    {
        var packetWriter = new PacketWriter(16);

        packetWriter.WriteEnum(ServerPackets.SInventoryUpdate);
        packetWriter.WriteInt32(invSlot);
        packetWriter.WriteInt32(GetPlayerInv(playerId, invSlot));
        packetWriter.WriteInt32(GetPlayerInvValue(playerId, invSlot));

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendOpenShop(int playerId, int shopNum)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SOpenShop);
        packetWriter.WriteInt32(shopNum);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void ResetShopAction()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SResetShopAction);

        PlayerService.Instance.SendDataToAll(packetWriter.GetBytes());
    }

    public static void SendBank(int playerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SBank);

        for (var i = 0; i < Variables.MaxBank; i++)
        {
            byte slot = (byte)Data.TempPlayer[playerId].Slot;
            packetWriter.WriteInt32(Account.Instance[playerId].Bank[slot].Item[i].Num);
            packetWriter.WriteInt32(Account.Instance[playerId].Bank[slot].Item[i].Value);
        }

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendTradeInvite(int playerId, int tradeindex)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(ServerPackets.STradeInvite);
        packetWriter.WriteInt32(tradeindex);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendTrade(int playerId, int tradeTarget)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(ServerPackets.STrade);
        packetWriter.WriteInt32(tradeTarget);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendTradeUpdate(int playerId, byte dataType)
    {
        var packetWriter = new PacketWriter();

        var totalWorth = 0;

        var tradeTarget = Data.TempPlayer[playerId].InTrade;
        if (tradeTarget == -1)
        {
            return;
        }

        packetWriter.WriteEnum(ServerPackets.STradeUpdate);
        packetWriter.WriteInt32((int)dataType);

        switch (dataType)
        {
            // own inventory
            case 0:
                {
                    for (var i = 0; i < Core.Globals.Variables.MaxInventory; i++)
                    {
                        if (Data.TempPlayer[playerId].TradeOffer[i].Num >= 0)
                        {
                            packetWriter.WriteInt32(Data.TempPlayer[playerId].TradeOffer[i].Num);
                            packetWriter.WriteInt32(Data.TempPlayer[playerId].TradeOffer[i].Value);

                            if (Item.Instance[Data.TempPlayer[playerId].TradeOffer[i].Num].Type == (int)ItemCategory.Currency || Item.Instance[Data.TempPlayer[playerId].TradeOffer[i].Num].Stackable == 1)
                            {
                                totalWorth += Item.Instance[GetPlayerInv(playerId, Data.TempPlayer[playerId].TradeOffer[i].Num)].Price * Data.TempPlayer[playerId].TradeOffer[i].Value;
                            }
                            else
                            {
                                totalWorth += Item.Instance[GetPlayerInv(playerId, Data.TempPlayer[playerId].TradeOffer[i].Num)].Price;
                            }
                        }
                        else
                        {
                            packetWriter.WriteInt32(-1);
                            packetWriter.WriteInt32(0);
                        }
                    }

                    break;
                }

            // other inventory
            case 1:
                {
                    for (var i = 0; i < Core.Globals.Variables.MaxInventory; i++)
                    {
                        if (Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num >= 0)
                        {
                            packetWriter.WriteInt32(GetPlayerInv((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num));
                            packetWriter.WriteInt32(Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Value);

                            if (GetPlayerInv((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num) < 0)
                            {
                                continue;
                            }

                            if (Item.Instance[GetPlayerInv((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num)].Type == (int)ItemCategory.Currency || Item.Instance[GetPlayerInv((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num)].Stackable == 1)
                            {
                                totalWorth += Item.Instance[GetPlayerInv((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num)].Price * Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Value;
                            }
                            else
                            {
                                totalWorth += Item.Instance[GetPlayerInv((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num)].Price;
                            }
                        }
                        else
                        {
                            packetWriter.WriteInt32(-1);
                            packetWriter.WriteInt32(0);
                        }
                    }

                    break;
                }
        }

        // send total worth of trade
        packetWriter.WriteInt32(totalWorth);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendTradeStatus(int playerId, int status)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(ServerPackets.STradeStatus);
        packetWriter.WriteInt32(status);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendPlayerSkills(int playerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SSkills);

        for (var i = 0; i < Core.Globals.Variables.MaxPlayerSkills; i++)
        {
            packetWriter.WriteInt32(GetPlayerSkill(playerId, i));
        }

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendTarget(int playerId, int target, int targetType)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(ServerPackets.STarget);
        packetWriter.WriteInt32(target);
        packetWriter.WriteInt32(targetType);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendMapReport(int playerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SMapReport);

        var mapCount = Server.Map.Instance.Count;
        for (var i = 0; i < Core.Globals.Variables.MaxMaps; i++)
        {
            var name = "";
            if (i >= 0 && i < mapCount)
            {
                name = Server.Map.Instance[i]?.Name ?? "";
            }

            packetWriter.WriteString(name);
        }

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendAdminPanel(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SAdmin);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendHotbar(int playerId)
    {
        var packetWriter = new PacketWriter(4 + Core.Globals.Variables.MaxHotbar * 8);

        packetWriter.WriteEnum(ServerPackets.SHotbar);

        for (var i = 0; i < Core.Globals.Variables.MaxHotbar; i++)
        {
            packetWriter.WriteInt32(Player.Instance[playerId].Hotbar[i].Slot);
            packetWriter.WriteByte(Player.Instance[playerId].Hotbar[i].SlotType);
        }

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendRightClick(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SrClick);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendJobEditor(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SJobEditor);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendEmote(int playerId, int emote)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(ServerPackets.SEmote);
        packetWriter.WriteInt32(playerId);
        packetWriter.WriteInt32(emote);

        NetworkConfig.SendDataToMap(GetPlayerMap(playerId), packetWriter.GetBytes());
    }

    public static void SendChatBubble(int map, int target, int targetType, string message, int color)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SChatBubble);
        packetWriter.WriteInt32(target);
        packetWriter.WriteInt32(targetType);
        packetWriter.WriteString(message);
        packetWriter.WriteInt32(color);

        NetworkConfig.SendDataToMap(map, packetWriter.GetBytes());
    }

    public static void SendPlayerAttack(int playerId)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(ServerPackets.SAttack);
        packetWriter.WriteInt32(playerId);

        NetworkConfig.SendDataToMapBut(playerId, GetPlayerMap(playerId), packetWriter.GetBytes());
    }
    
    public static void SendNpcAttack(int map, int npcIndex)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(ServerPackets.SNpcAttack);
        packetWriter.WriteInt32(npcIndex);

        NetworkConfig.SendDataToMap(map, packetWriter.GetBytes());
    }


    public static void SendMapItemToAll(int map, int mapSlot)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SMapItemData);
        packet.WriteByte((byte)mapSlot);
        packet.WriteInt32(MapItem.Instance[map, mapSlot].Num);
        packet.WriteInt32(MapItem.Instance[map, mapSlot].Value);
        packet.WriteInt32(MapItem.Instance[map, mapSlot].X);
        packet.WriteInt32(MapItem.Instance[map, mapSlot].Y);

        NetworkConfig.SendDataToMap(map, packet.GetBytes());
    }

    public static void SendMapItemsToAll(int map)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SMapItemsData);

        for (var i = 0; i < Core.Globals.Variables.MaxMapItems; i++)
        {
            packet.WriteInt32(MapItem.Instance[map, i].Num);
            packet.WriteInt32(MapItem.Instance[map, i].Value);
            packet.WriteInt32(MapItem.Instance[map, i].X);
            packet.WriteInt32(MapItem.Instance[map, i].Y);
        }

        NetworkConfig.SendDataToMap(map, packet.GetBytes());
    }

    public static void SendMorals(int playerId)
    {
        for (var i = 0; i < Core.Globals.Variables.MaxMorals; i++)
        {
            SendUpdateMoralTo(playerId, i);
        }
    }

    public static void SendUpdateMoralTo(int playerId, int moralNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateMoral);

        WriteMoralDataToPacket(moralNum, packet);

        PlayerService.Instance.SendDataTo(playerId, packet.GetBytes());
    }

    public static void SendUpdateMoralToAll(int moralNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateMoral);

        WriteMoralDataToPacket(moralNum, packet);

        PlayerService.Instance.SendDataToAll(packet.GetBytes());
    }

    private static void WriteMoralDataToPacket(int index, PacketWriter packet)
    {
        packet.WriteInt32(index);

        var moral = index >= 0 && index < Moral.Instance.Count ? Moral.Instance[index] : new MoralBase();

        packet.WriteString(moral.Name);
        packet.WriteByte(moral.Color);
        packet.WriteBoolean(moral.NpcBlock);
        packet.WriteBoolean(moral.PlayerBlock);
        packet.WriteBoolean(moral.CanCast);
        packet.WriteBoolean(moral.CanDropItem);
        packet.WriteBoolean(moral.CanPickupItem);
        packet.WriteBoolean(moral.CanUseItem);
        packet.WriteBoolean(moral.CanPk);
        packet.WriteBoolean(moral.DropItems);
        packet.WriteBoolean(moral.LoseExp);
    }

    public static void SendProjectileToMap(int map, int projectileNum)
    {
        var mapProjectile = Data.MapProjectile[map, projectileNum];
        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SMapProjectile);
        packet.WriteInt32(projectileNum);
        packet.WriteInt32(mapProjectile.Index);
        packet.WriteInt32(mapProjectile.Owner);
        packet.WriteByte(mapProjectile.OwnerType);
        packet.WriteByte(mapProjectile.Dir);
        packet.WriteInt32(mapProjectile.X);
        packet.WriteInt32(mapProjectile.Y);
        packet.WriteInt16(mapProjectile.Vx);
        packet.WriteInt16(mapProjectile.Vy);
        packet.WriteByte(mapProjectile.FreeAim);

        NetworkConfig.SendDataToMap(map, packet.GetBytes());
    }

    public static void SendUpdateProjectileToAll(int projectileNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateProjectile);
        WriteProjectileDataToPacket(projectileNum, packet);

        PlayerService.Instance.SendDataToAll(packet.GetBytes());
    }

    public static void SendUpdateProjectileTo(int playerId, int projectileNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateProjectile);
        WriteProjectileDataToPacket(projectileNum, packet);

        PlayerService.Instance.SendDataTo(playerId, packet.GetBytes());
    }

    public static void SendProjectiles(int playerId)
    {
        for (var projectile = 0; projectile < Core.Globals.Variables.MaxProjectiles; projectile++)
        {
            SendUpdateProjectileTo(playerId, projectile);
        }
    }

    private static void WriteProjectileDataToPacket(int index, PacketWriter packet)
    {
        packet.WriteInt32(index);

        var projectile = index >= 0 && index < Projectile.Instance.Count ? Projectile.Instance[index] : new ProjectileBase();
            
        packet.WriteString(projectile.Name);
        packet.WriteInt32(projectile.Sprite);
        packet.WriteByte(projectile.Range);
        packet.WriteInt32(projectile.Speed);
        packet.WriteInt32(projectile.Damage);
        packet.WriteInt32(projectile.Animation);
    }

    public static void SendUpdateResourceToAll(int index)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateResource);

        WriteResourceDataToPacket(index, packet);

        PlayerService.Instance.SendDataToAll(packet.GetBytes());
    }

    public static void SendUpdateResourceTo(int playerId, int index)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateResource);

        WriteResourceDataToPacket(index, packet);

        PlayerService.Instance.SendDataTo(playerId, packet.GetBytes());
    }

    private static void WriteResourceDataToPacket(int index, PacketWriter packet)
    {
        packet.WriteInt32(index);

        var resource = index >= 0 && index < Resource.Instance.Count ? Resource.Instance[index] : new ResourceBase();

        packet.WriteInt32(resource.Animation);
        packet.WriteString(resource.EmptyMessage);
        packet.WriteInt32(resource.ExhaustedImage);
        packet.WriteInt32(resource.Health);
        packet.WriteInt32(resource.ExperienceReward);
        packet.WriteInt32(resource.ItemReward);
        packet.WriteString(resource.Name);
        packet.WriteInt32(resource.ResourceImage);
        packet.WriteInt32(resource.ResourceType);
        packet.WriteInt32(resource.RespawnTime);
        packet.WriteString(resource.SuccessMessage);
        packet.WriteInt32(resource.LvlRequired);
        packet.WriteInt32(resource.ToolRequired);
        packet.WriteBoolean(resource.Walkthrough);

        // common event fields (0 = none)
        packet.WriteByte(resource.CommonEventType);
        packet.WriteInt32(resource.CommonEventData1);
        packet.WriteInt32(resource.CommonEventData2);
    }

    public static void SendMapResourceToMap(int map)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SMapResource);
        packet.WriteInt32(MapResource.Instance[map].ResourceCount);

        if (MapResource.Instance[map].ResourceCount > 0)
        {
            for (var i = 0; i < MapResource.Instance[map].ResourceCount; i++)
            {
                packet.WriteByte(MapResource.Instance[map].ResourceData[i].State);
                packet.WriteInt32(MapResource.Instance[map].ResourceData[i].X);
                packet.WriteInt32(MapResource.Instance[map].ResourceData[i].Y);
            }
        }

        NetworkConfig.SendDataToMap(map, packet.GetBytes());
    }

    public static void SendResources(int playerId)
    {
        for (var i = 0; i < Core.Globals.Variables.MaxResources; i++)
        { 
            NetworkSend.SendUpdateResourceTo(playerId, i);
        }
    }

    public static void SendItems(int playerId)
    {
        for (var i = 0; i < Core.Globals.Variables.MaxItems; i++)
        {
            SendUpdateItemTo(playerId, i);         
        }
    }

    public static void SendUpdateItemTo(int playerId, int index)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateItem);

        WriteItemDataToPacket(index, packet);

        PlayerService.Instance.SendDataTo(playerId, packet.GetBytes());
    }

    public static void SendUpdateItemToAll(int index)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateItem);

        WriteItemDataToPacket(index, packet);

        PlayerService.Instance.SendDataToAll(packet.GetBytes());
    }

    private static void WriteItemDataToPacket(int index, PacketWriter packet)
    {
        var statCount = Enum.GetNames<Stat>().Length;

        packet.WriteInt32(index);

        var item = index >= 0 && index < Item.Instance.Count ? Item.Instance[index] : new ItemBase();

        packet.WriteInt32(item.AccessReq);

        for (var i = 0; i < statCount; i++)
        {
            packet.WriteInt32(item.AddStat[i]);
        }

        packet.WriteInt32(item.Animation);
        packet.WriteByte(item.BindType);
        packet.WriteInt32(item.JobReq);
        packet.WriteInt32(item.Data1);
        packet.WriteInt32(item.Data2);
        packet.WriteInt32(item.Data3);
        packet.WriteInt32(item.LevelReq);
        packet.WriteByte(item.Mastery);
        packet.WriteString(item.Name);
        packet.WriteInt32(item.Paperdoll);
        packet.WriteInt32(item.Icon);
        packet.WriteInt32(item.Price);
        packet.WriteByte(item.Rarity);
        packet.WriteInt32(item.Speed);
        packet.WriteByte(item.Stackable);
        packet.WriteString(item.Description);

        for (var i = 0; i < statCount; i++)
        {
            packet.WriteInt32(item.StatReq[i]);
        }

        packet.WriteByte(item.Type);
        packet.WriteByte(item.SubType);
        packet.WriteByte(item.ItemLevel);
        packet.WriteByte(item.KnockBack);
        packet.WriteByte(item.KnockBackTiles);
        packet.WriteInt32(item.Projectile);
        packet.WriteInt32(item.Ammo);

        packet.WriteByte(item.CommonEventType);
        packet.WriteInt32(item.CommonEventData1);
        packet.WriteInt32(item.CommonEventData2);
    }

     public static void WriteJobDataToPacket(int index, PacketWriter packetWriter)
    {
        var job = index >= 0 && index < Job.Instance.Count ? Job.Instance[index] : new JobBase();

        packetWriter.WriteString(job.Name);
        packetWriter.WriteString(job.Desc);
        packetWriter.WriteInt32(job.MaleSprite);
        packetWriter.WriteInt32(job.FemaleSprite);

        for (var i = 0; i < StatCount; i++)
        {
            packetWriter.WriteInt32(job.Stat[i]);
        }

        for (var q = 0; q < Variables.MaxStartItems; q++)
        {
            packetWriter.WriteInt32(job.StartItem[q]);
            packetWriter.WriteInt32(job.StartValue[q]);
        }

        for (var q = 0; q < Variables.MaxStartSkills; q++)
        {
            packetWriter.WriteInt32(job.StartSkill[q]);
        }

        packetWriter.WriteInt32(job.StartMap);
        packetWriter.WriteByte(job.StartX);
        packetWriter.WriteByte(job.StartY);
        packetWriter.WriteInt32(job.BaseExp);
        packetWriter.WriteSingle(job.MoveSpeed);
    }

    public static void SendAnimation(int map, int anim, int x, int y, byte lockType = 0, int lockindex = 0)
    {
        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SAnimation);
        packet.WriteInt32(anim);
        packet.WriteInt32(x);
        packet.WriteInt32(y);
        packet.WriteInt32((int)lockType);
        packet.WriteInt32(lockindex);

        NetworkConfig.SendDataToMap(map, packet.GetBytes());
    }

     public static void SendAnimationTo(int index, int anim, int x, int y, byte lockType = 0, int lockindex = 0)
    {
        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SAnimation);
        packet.WriteInt32(anim);
        packet.WriteInt32(x);
        packet.WriteInt32(y);
        packet.WriteInt32((int)lockType);
        packet.WriteInt32(lockindex);

        PlayerService.Instance.SendDataTo(index, packet.GetBytes());
    }
    
    public static void SendAnimations(int playerId)
    {
        for (var index = 0; index < Variables.MaxAnimations; index++)
        {
            SendUpdateAnimationTo(playerId, index);
        }
    }

    public static void SendUpdateAnimationTo(int playerId, int index)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateAnimation);

        WriteAnimationDataToPacket(index, packet);

        PlayerService.Instance.SendDataTo(playerId, packet.GetBytes());
    }

    public static void SendUpdateAnimationToAll(int animationNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateAnimation);

        WriteAnimationDataToPacket(animationNum, packet);

        PlayerService.Instance.SendDataToAll(packet.GetBytes());
    }

    private static void WriteAnimationDataToPacket(int index, PacketWriter packet)
    {
        packet.WriteInt32(index);

        var animation = index >= 0 && index < Animation.Instance.Count ? Animation.Instance[index] : new AnimationBase();

        foreach (var frame in animation.Frames)
        {
            packet.WriteInt32(frame);
        }

        foreach (var loopCount in animation.LoopCount)
        {
            packet.WriteInt32(loopCount);
        }

        foreach (var loopTime in animation.LoopTime)
        {
            packet.WriteInt32(loopTime);
        }

        packet.WriteString(animation.Name);
        packet.WriteString(animation.Sound);

        foreach (var sprite in animation.Sprite)
        {
            packet.WriteInt32(sprite);
        }
    }


    public static void SendSpecialEffect(int index, int effectType, int data1 = 0, int data2 = 0, int data3 = 0, int data4 = 0)
    {
        var buffer = new PacketWriter(24);

        buffer.WriteEnum(ServerPackets.SSpecialEffect);
        buffer.WriteInt32(effectType);

        switch (effectType)
        {
            case Event.EffectTypeFadeIn:
            case Event.EffectTypeFadeOut:
            case Event.EffectTypeFlash:
                break;
            case Event.EffectTypeFog:
                buffer.WriteInt32(data1); // Fog number
                buffer.WriteInt32(data2); // Movement speed
                buffer.WriteInt32(data3); // Opacity
                break;
            case Event.EffectTypeWeather:
                buffer.WriteInt32(data1); // Weather type
                buffer.WriteInt32(data2); // Intensity
                break;
            case Event.EffectTypeTint:
                buffer.WriteInt32(data1); // Red
                buffer.WriteInt32(data2); // Green
                buffer.WriteInt32(data3); // Blue
                buffer.WriteInt32(data4); // Alpha
                break;
            case Event.EffectTypeScreenShake:
                buffer.WriteInt32(data1); // Intensity
                buffer.WriteInt32(data2); // Duration
                break;
            default:
                General.Logger.LogWarning($"Unknown effect type {effectType} sent to player {index}");
                return;
        }

        PlayerService.Instance.SendDataTo(index, buffer.GetBytes());
    }

    public static void SendSwitchesAndVariables(int index, bool everyone = false)
    {
        var buffer = new PacketWriter(4 + (Core.Globals.Variables.MaxSwitches + Core.Globals.Variables.MaxVariables) * 256);
        buffer.WriteEnum(ServerPackets.SSwitchesAndVariables);
        for (var i = 0; i < Core.Globals.Variables.MaxSwitches; i++) buffer.WriteString(Event.Switches[i]);
        for (var i = 0; i < Core.Globals.Variables.MaxVariables; i++) buffer.WriteString(Event.Variables[i]);

        if (everyone)
        {
            PlayerService.Instance.SendDataToAll(buffer.GetBytes());
        }
        else
        {
            PlayerService.Instance.SendDataTo(index, buffer.GetBytes());
        }
    }

    public static void SendMapEventData(int index)
    {
        var buffer = new PacketWriter(4);

        var map = GetPlayerMap(index);

        buffer.WriteEnum(ServerPackets.SMapEventData);

        if (map < 0 || map >= Server.Map.Instance.Count)
        {
            General.Logger.LogWarning("SendMapEventData called with invalid map {MapId} for player {PlayerId}", map, index);
            buffer.WriteInt32(0);
            PlayerService.Instance.SendDataTo(index, buffer.GetBytes());
            SendSwitchesAndVariables(index);
            return;
        }

        buffer.WriteInt32(Server.Map.Instance[map].EventCount);

        if (Server.Map.Instance[map].EventCount > 0)
        {
            Event.SerializeMapEvents(buffer, map);
        }

        PlayerService.Instance.SendDataTo(index, buffer.GetBytes());

        SendSwitchesAndVariables(index);
    }


    public static void SendDataToParty(int partyNum, byte[] data)
    {
        var count = Data.Party[partyNum].MemberCount;
        for (var i = 0; i < count; i++)
        {
            if (Data.Party[partyNum].Member[i] > 0)
            {
                PlayerService.Instance.SendDataTo(Data.Party[partyNum].Member[i], data);
            }
        }
    }

    public static void SendPartyInvite(int playerId, int target)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SPartyInvite);
        packetWriter.WriteString(Player.Instance[target].Name);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendPartyUpdate(int partyNum)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SPartyUpdate);
        packetWriter.WriteInt32(Data.Party[partyNum].Leader == -1 ? 0 : 1);
        packetWriter.WriteInt32(Data.Party[partyNum].Leader);

        for (var i = 0; i < Core.Globals.Variables.MaxPartyMembers; i++)
        {
            packetWriter.WriteInt32(Data.Party[partyNum].Member[i]);
        }

        packetWriter.WriteInt32(Data.Party[partyNum].MemberCount);

        SendDataToParty(partyNum, packetWriter.GetBytes());
    }

    public static void SendPartyUpdateTo(int index)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SPartyUpdate);

        var partyNum = Data.TempPlayer[index].InParty;
        if (partyNum >= 0)
        {
            packetWriter.WriteInt32(1);
            packetWriter.WriteInt32(Data.Party[partyNum].Leader);

            for (var i = 0; i < Core.Globals.Variables.MaxPartyMembers; i++)
            {
                packetWriter.WriteInt32(Data.Party[partyNum].Member[i]);
            }

            packetWriter.WriteInt32(Data.Party[partyNum].MemberCount);
        }
        else
        {
            packetWriter.WriteInt32(0);
        }

        PlayerService.Instance.SendDataTo(index, packetWriter.GetBytes());
    }
    public static void SendPartyVitals(int partyNum, int playerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SPartyVitals);
        packetWriter.WriteInt32(playerId);

        for (var i = 0; i < VitalCount; i++)
        {
            packetWriter.WriteInt32(Player.Instance[playerId].Vital[i]);
        }

        SendDataToParty(partyNum, packetWriter.GetBytes());
    }

    public static void SendMapNpcsToMap(int map)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SMapNpcData);

        for (var mapNpcNum = 0; mapNpcNum < Core.Globals.Variables.MaxMapNpcs; mapNpcNum++)
        {
            packet.WriteInt32(MapNpc.Instance[map, mapNpcNum].Num);
            packet.WriteInt32(MapNpc.Instance[map, mapNpcNum].X);
            packet.WriteInt32(MapNpc.Instance[map, mapNpcNum].Y);
            packet.WriteByte(MapNpc.Instance[map, mapNpcNum].Dir);
        }

        NetworkConfig.SendDataToMap(map, packet.GetBytes());
    }


    public static void SendNpcs(int playerId)
    {
        for (var i = 0; i < Variables.MaxNpcs; i++)
        {
            SendUpdateNpcTo(playerId, i);
        }
    }

    public static void SendUpdateNpcTo(int playerId, int npcNum)
    {
        var buffer = new PacketWriter();

        buffer.WriteEnum(ServerPackets.SUpdateNpc);
        WriteNpcDataToPacket(npcNum, buffer);

        PlayerService.Instance.SendDataTo(playerId, buffer.GetBytes());
    }

    public static void SendUpdateNpcToAll(int npcNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateNpc);
        WriteNpcDataToPacket(npcNum, packet);

        PlayerService.Instance.SendDataToAll(packet.GetBytes());
    }

    private static void WriteNpcDataToPacket(int index, PacketWriter packet)
    {
        packet.WriteInt32(index);

        var npc = index >= 0 && index < Npc.Instance.Count ? Npc.Instance[index] : new NpcBase();

        packet.WriteInt32(npc.Animation);
        packet.WriteString(npc.AttackSay);
        packet.WriteByte(npc.Behavior);

        for (var i = 0; i < Core.Globals.Variables.MaxDropItems; i++)
        {
            packet.WriteInt32(npc.DropChance != null && npc.DropChance.Length > i ? npc.DropChance[i] : 0);
            packet.WriteInt32(npc.DropItem != null && npc.DropItem.Length > i ? npc.DropItem[i] : 0);
            packet.WriteInt32(npc.DropItemValue != null && npc.DropItemValue.Length > i ? npc.DropItemValue[i] : 0);
        }

        packet.WriteInt32(npc.Experience);
        packet.WriteByte(npc.Faction);
        packet.WriteInt32(npc.Hp);
        packet.WriteString(npc.Name);
        packet.WriteByte(npc.Range);
        packet.WriteByte(npc.SpawnTime);
        packet.WriteInt32(npc.SpawnSecs);
        packet.WriteInt32(npc.Sprite);

        for (var i = 0; i < StatCount; i++)
        {
            packet.WriteByte(npc.Stat != null && npc.Stat.Length > i ? npc.Stat[i] : (byte)0);
        }

        for (var i = 0; i < Core.Globals.Variables.MaxNpcSkills; i++)
        {
            packet.WriteByte(npc.Skill != null && npc.Skill.Length > i ? npc.Skill[i] : (byte)0);
        }

        packet.WriteByte(npc.Level);
        packet.WriteInt32(npc.Damage);

        packet.WriteInt32(npc.DeathSwitch);
        packet.WriteInt32(npc.DeathVariable);
        packet.WriteInt32(npc.DeathSwitchValue);
        packet.WriteInt32(npc.DeathVariableValue);

        // common event fields (0 = none)
        packet.WriteByte(npc.CommonEventType);
        packet.WriteInt32(npc.CommonEventData1);
        packet.WriteInt32(npc.CommonEventData2);
    }

    public static void SendMapNpcVitals(int map, byte mapNpcNum)
    {
        var packet = new PacketWriter(4);

        packet.WriteInt32((int)ServerPackets.SMapNpcVitals);
        packet.WriteInt32((int)mapNpcNum);

        var vitalCount = Enum.GetValues<Vital>().Length;
        for (var i = 0; i < vitalCount; i++)
        {
            packet.WriteInt32(MapNpc.Instance[map, mapNpcNum].Vital[i]);
        }

        NetworkConfig.SendDataToMap(map, packet.GetBytes());
    }

    public static void SendLeaveMap(int playerId, int map)
    {
        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SLeftMap);
        packet.WriteInt32(playerId);

        NetworkConfig.SendDataToMapBut(playerId, map, packet.GetBytes());
    }

    public static void SendPlayerDeath(int playerId, int deathTimer)
    {
        var deathPacket = new Core.Net.PacketWriter();
        deathPacket.WriteEnum(ServerPackets.SPlayerDead);
        deathPacket.WriteInt32(deathTimer);
        deathPacket.WriteInt32(playerId);
        PlayerService.Instance.SendDataTo(playerId, deathPacket.GetBytes());
}`
