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

    public static void SendAlert(GameSession session, SystemMessage menuNo, Menu menuReset = 0, bool kick = true)
    {
        var packetWriter = new PacketWriter(16);

        packetWriter.WriteEnum(ServerPackets.SAlertMsg);
        packetWriter.WriteByte((byte)menuNo);
        packetWriter.WriteInt32((byte)menuReset);
        packetWriter.WriteInt32(kick ? 1 : 0);

        session.Channel.Send(packetWriter.GetBytes());

        _ = Player.OnExit(session.Id);
    }

    public static void SendAlert(int playerId, SystemMessage menuNo, Menu menuReset = 0, bool kick = true)
    {
        var packetWriter = new PacketWriter(16);
        packetWriter.WriteEnum(ServerPackets.SAlertMsg);
        packetWriter.WriteByte((byte)menuNo);
        packetWriter.WriteInt32((byte)menuReset);
        packetWriter.WriteInt32(kick ? 1 : 0);
        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
        _ = Player.OnExit(playerId);
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
        w.WriteInt32(Variables.MaxPoints);
        w.WriteByte(Variables.MaxCharacters);
        w.WriteByte(Variables.MaxStats);
        w.WriteByte(Variables.MaxQuests);
        w.WriteByte(Variables.MaxGuilds);
        w.WriteByte(Variables.MaxEventChoices);

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
        packetWriter.WriteInt32(Script.Instance?.GetPlayerMaxLevel());
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

        WriteJobDataToPacket(job, packetWriter);

        PlayerService.Instance.SendDataToAll(packetWriter.GetBytes());
    }

    public static void SendInventory(int playerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SInventory);

        for (var i = 0; i < Core.Globals.Variables.MaxInventory; i++)
        {
            packetWriter.WriteInt32(GetPlayerInventory(playerId, i));
            packetWriter.WriteInt32(GetPlayerInventoryValue(playerId, i));
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

    public static void SendUpdateShopTo(int playerId, int shopNum)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SUpdateShop);
        packetWriter.WriteInt32(shopNum);
        packetWriter.WriteInt32(Shop.Instance[shopNum].BuyRate);
        packetWriter.WriteString(Shop.Instance[shopNum].Name);

        for (var i = 0; i < Core.Globals.Variables.MaxTrades; i++)
        {
            packetWriter.WriteInt32(Shop.Instance[shopNum].TradeItem[i].CostItem);
            packetWriter.WriteInt32(Shop.Instance[shopNum].TradeItem[i].CostValue);
            packetWriter.WriteInt32(Shop.Instance[shopNum].TradeItem[i].Item);
            packetWriter.WriteInt32(Shop.Instance[shopNum].TradeItem[i].ItemValue);
        }

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendUpdateShopToAll(int shopNum)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SUpdateShop);
        packetWriter.WriteInt32(shopNum);
        packetWriter.WriteInt32(Shop.Instance[shopNum].BuyRate);
        packetWriter.WriteString(Shop.Instance[shopNum].Name);

        for (var i = 0; i < Core.Globals.Variables.MaxTrades; i++)
        {
            packetWriter.WriteInt32(Shop.Instance[shopNum].TradeItem[i].CostItem);
            packetWriter.WriteInt32(Shop.Instance[shopNum].TradeItem[i].CostValue);
            packetWriter.WriteInt32(Shop.Instance[shopNum].TradeItem[i].Item);
            packetWriter.WriteInt32(Shop.Instance[shopNum].TradeItem[i].ItemValue);
        }

        PlayerService.Instance.SendDataToAll(packetWriter.GetBytes());
    }

    public static void SendSkills(int playerId)
    {
        for (var i = 0; i < Core.Globals.Variables.MaxSkills; i++)
        {
            if (Data.Skill[i].Name.Length > 0)
            {
                SendUpdateSkillTo(playerId, i);
            }
        }
    }

    public static void SendUpdateSkillTo(int playerId, int skillNum)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SUpdateSkill);
        packetWriter.WriteInt32(skillNum);
        packetWriter.WriteInt32(Data.Skill[skillNum].AccessReq);
        packetWriter.WriteInt32(Data.Skill[skillNum].AoE);
        packetWriter.WriteInt32(Data.Skill[skillNum].CastAnim);
        packetWriter.WriteInt32(Data.Skill[skillNum].CastTime);
        packetWriter.WriteInt32(Data.Skill[skillNum].CdTime);
        packetWriter.WriteInt32(Data.Skill[skillNum].JobReq);
        packetWriter.WriteInt32(Data.Skill[skillNum].Dir);
        packetWriter.WriteInt32(Data.Skill[skillNum].Duration);
        packetWriter.WriteInt32(Data.Skill[skillNum].Icon);
        packetWriter.WriteInt32(Data.Skill[skillNum].Interval);
        packetWriter.WriteInt32(Data.Skill[skillNum].IsAoE ? 1 : 0);
        packetWriter.WriteInt32(Data.Skill[skillNum].LevelReq);
        packetWriter.WriteInt32(Data.Skill[skillNum].Map);
        packetWriter.WriteInt32(Data.Skill[skillNum].MpCost);
        packetWriter.WriteString(Data.Skill[skillNum].Name);
        packetWriter.WriteInt32(Data.Skill[skillNum].Range);
        packetWriter.WriteInt32(Data.Skill[skillNum].SkillAnim);
        packetWriter.WriteInt32(Data.Skill[skillNum].StunDuration);
        packetWriter.WriteInt32(Data.Skill[skillNum].Type);
        packetWriter.WriteInt32(Data.Skill[skillNum].Vital);
        packetWriter.WriteInt32(Data.Skill[skillNum].X);
        packetWriter.WriteInt32(Data.Skill[skillNum].Y);
        packetWriter.WriteInt32(Data.Skill[skillNum].IsProjectile);
        packetWriter.WriteInt32(Data.Skill[skillNum].Projectile);
        packetWriter.WriteInt32(Data.Skill[skillNum].KnockBack);
        packetWriter.WriteInt32(Data.Skill[skillNum].KnockBackTiles);
        packetWriter.WriteInt32(Data.Skill[skillNum].MultiDirMask);
        packetWriter.WriteInt32(Data.Skill[skillNum].ChainOnHitSkillId);
        packetWriter.WriteInt32(Data.Skill[skillNum].CommonEventType);
        packetWriter.WriteInt32(Data.Skill[skillNum].CommonEventData1);
        packetWriter.WriteInt32(Data.Skill[skillNum].CommonEventData2);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SendUpdateSkillToAll(int skillNum)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SUpdateSkill);
        packetWriter.WriteInt32(skillNum);
        packetWriter.WriteInt32(Data.Skill[skillNum].AccessReq);
        packetWriter.WriteInt32(Data.Skill[skillNum].AoE);
        packetWriter.WriteInt32(Data.Skill[skillNum].CastAnim);
        packetWriter.WriteInt32(Data.Skill[skillNum].CastTime);
        packetWriter.WriteInt32(Data.Skill[skillNum].CdTime);
        packetWriter.WriteInt32(Data.Skill[skillNum].JobReq);
        packetWriter.WriteInt32(Data.Skill[skillNum].Dir);
        packetWriter.WriteInt32(Data.Skill[skillNum].Duration);
        packetWriter.WriteInt32(Data.Skill[skillNum].Icon);
        packetWriter.WriteInt32(Data.Skill[skillNum].IsAoE ? 1 : 0);
        packetWriter.WriteInt32(Data.Skill[skillNum].LevelReq);
        packetWriter.WriteInt32(Data.Skill[skillNum].Map);
        packetWriter.WriteInt32(Data.Skill[skillNum].MpCost);
        packetWriter.WriteString(Data.Skill[skillNum].Name);
        packetWriter.WriteInt32(Data.Skill[skillNum].Range);
        packetWriter.WriteInt32(Data.Skill[skillNum].SkillAnim);
        packetWriter.WriteInt32(Data.Skill[skillNum].StunDuration);
        packetWriter.WriteInt32(Data.Skill[skillNum].Type);
        packetWriter.WriteInt32(Data.Skill[skillNum].Vital);
        packetWriter.WriteInt32(Data.Skill[skillNum].X);
        packetWriter.WriteInt32(Data.Skill[skillNum].Y);
        packetWriter.WriteInt32(Data.Skill[skillNum].IsProjectile);
        packetWriter.WriteInt32(Data.Skill[skillNum].Projectile);
        packetWriter.WriteInt32(Data.Skill[skillNum].KnockBack);
        packetWriter.WriteInt32(Data.Skill[skillNum].KnockBackTiles);
        packetWriter.WriteInt32(Data.Skill[skillNum].MultiDirMask);
        packetWriter.WriteInt32(Data.Skill[skillNum].ChainOnHitSkillId);
        packetWriter.WriteInt32(Data.Skill[skillNum].CommonEventType);
        packetWriter.WriteInt32(Data.Skill[skillNum].CommonEventData1);
        packetWriter.WriteInt32(Data.Skill[skillNum].CommonEventData2);

        PlayerService.Instance.SendDataToAll(packetWriter.GetBytes());
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

    public static void SendMapData(int playerId, int mapNum, bool sendMap)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SMapData);

        if (sendMap)
        {
            packetWriter.WriteInt32(1);
            packetWriter.WriteInt32(mapNum);
            packetWriter.WriteString(Data.Map[mapNum].Name);
            packetWriter.WriteString(Data.Map[mapNum].Music);
            packetWriter.WriteInt32(Data.Map[mapNum].Revision);
            packetWriter.WriteByte(Data.Map[mapNum].Moral);
            packetWriter.WriteInt32(Data.Map[mapNum].Tileset);
            packetWriter.WriteInt32(Data.Map[mapNum].Up);
            packetWriter.WriteInt32(Data.Map[mapNum].Down);
            packetWriter.WriteInt32(Data.Map[mapNum].Left);
            packetWriter.WriteInt32(Data.Map[mapNum].Right);
            packetWriter.WriteInt32(Data.Map[mapNum].BootMap);
            packetWriter.WriteByte(Data.Map[mapNum].BootX);
            packetWriter.WriteByte(Data.Map[mapNum].BootY);
            packetWriter.WriteByte(Data.Map[mapNum].MaxX);
            packetWriter.WriteByte(Data.Map[mapNum].MaxY);
            packetWriter.WriteByte(Data.Map[mapNum].Weather);
            packetWriter.WriteInt32(Data.Map[mapNum].Fog);
            packetWriter.WriteInt32(Data.Map[mapNum].WeatherIntensity);
            packetWriter.WriteByte(Data.Map[mapNum].FogOpacity);
            packetWriter.WriteByte(Data.Map[mapNum].FogSpeed);
            packetWriter.WriteBoolean(Data.Map[mapNum].MapTint);
            packetWriter.WriteByte(Data.Map[mapNum].MapTintR);
            packetWriter.WriteByte(Data.Map[mapNum].MapTintG);
            packetWriter.WriteByte(Data.Map[mapNum].MapTintB);
            packetWriter.WriteByte(Data.Map[mapNum].MapTintA);
            packetWriter.WriteByte(Data.Map[mapNum].Panorama);
            packetWriter.WriteByte(Data.Map[mapNum].Parallax);
            packetWriter.WriteByte(Data.Map[mapNum].Brightness);
            packetWriter.WriteBoolean(Data.Map[mapNum].NoRespawn);
            packetWriter.WriteBoolean(Data.Map[mapNum].Indoors);
            packetWriter.WriteInt32(Data.Map[mapNum].Shop);

            for (var i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
            {
                packetWriter.WriteInt32(Data.Map[mapNum].Npc[i]);
            }

            for (var x = 0; x < Data.Map[mapNum].MaxX; x++)
            {
                for (var y = 0; y < Data.Map[mapNum].MaxY; y++)
                {
                    packetWriter.WriteInt32(Data.Map[mapNum].Tile[x, y].Data1);
                    packetWriter.WriteInt32(Data.Map[mapNum].Tile[x, y].Data2);
                    packetWriter.WriteInt32(Data.Map[mapNum].Tile[x, y].Data3);
                    packetWriter.WriteInt32(Data.Map[mapNum].Tile[x, y].Data1_2);
                    packetWriter.WriteInt32(Data.Map[mapNum].Tile[x, y].Data2_2);
                    packetWriter.WriteInt32(Data.Map[mapNum].Tile[x, y].Data3_2);
                    packetWriter.WriteByte(Data.Map[mapNum].Tile[x, y].DirBlock);

                    for (var i = 0; i < MapLayerCount; i++)
                    {
                        packetWriter.WriteInt32(Data.Map[mapNum].Tile[x, y].Layer[i].Tileset);
                        packetWriter.WriteInt32(Data.Map[mapNum].Tile[x, y].Layer[i].X);
                        packetWriter.WriteInt32(Data.Map[mapNum].Tile[x, y].Layer[i].Y);
                        packetWriter.WriteByte(Data.Map[mapNum].Tile[x, y].Layer[i].AutoTile);
                    }

                    packetWriter.WriteInt32((int)Data.Map[mapNum].Tile[x, y].Type);
                    packetWriter.WriteInt32((int)Data.Map[mapNum].Tile[x, y].Type2);
                }
            }

            packetWriter.WriteInt32(Data.Map[mapNum].EventCount);

            if (Data.Map[mapNum].EventCount > 0)
            {
                for (var i = 0; i < Data.Map[mapNum].EventCount; i++)
                {
                    ref var @event = ref Data.Map[mapNum].Event[i];

                    packetWriter.WriteString(@event.Name);
                    packetWriter.WriteByte(@event.Globals);
                    packetWriter.WriteInt32(@event.X);
                    packetWriter.WriteInt32(@event.Y);
                    packetWriter.WriteInt32(@event.PageCount);

                    if (Data.Map[mapNum].Event[i].PageCount == 0)
                    {
                        continue;
                    }

                    for (var x = 0; x < Data.Map[mapNum].Event[i].PageCount; x++)
                    {
                        ref var eventPage = ref Data.Map[mapNum].Event[i].Pages[x];

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
                            for (int y = 0, loopTo6 = eventPage.MoveRouteCount; y < loopTo6; y++)
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

                        packetWriter.WriteInt32(eventPage.IdleAnim);
                        packetWriter.WriteInt32(eventPage.DirFix);
                        packetWriter.WriteInt32(eventPage.WalkThrough);
                        packetWriter.WriteInt32(eventPage.ShowName);
                        packetWriter.WriteByte(eventPage.Trigger);
                        packetWriter.WriteInt32(eventPage.CommandListCount);
                        packetWriter.WriteByte(eventPage.Position);

                        if (Data.Map[mapNum].Event[i].Pages[x].CommandListCount == 0)
                        {
                            continue;
                        }

                        for (var y = 0; y < Data.Map[mapNum].Event[i].Pages[x].CommandListCount; y++)
                        {
                            packetWriter.WriteInt32(Data.Map[mapNum].Event[i].Pages[x].CommandList[y].CommandCount);
                            packetWriter.WriteInt32(Data.Map[mapNum].Event[i].Pages[x].CommandList[y].ParentList);

                            if (Data.Map[mapNum].Event[i].Pages[x].CommandList[y].CommandCount == 0)
                            {
                                continue;
                            }

                            for (var z = 0; z < Data.Map[mapNum].Event[i].Pages[x].CommandList[y].CommandCount; z++)
                            {
                                ref var eventCommand = ref Data.Map[mapNum].Event[i].Pages[x].CommandList[y].Commands[z];

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
            packetWriter.WriteInt32(Data.MapItem[mapNum, i].Num);
            packetWriter.WriteInt32(Data.MapItem[mapNum, i].Value);
            packetWriter.WriteInt32(Data.MapItem[mapNum, i].X);
            packetWriter.WriteInt32(Data.MapItem[mapNum, i].Y);
        }

        for (var i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
        {
            packetWriter.WriteInt32(Data.MapNpc[mapNum].Npc[i].Num);
            packetWriter.WriteInt32(Data.MapNpc[mapNum].Npc[i].X);
            packetWriter.WriteInt32(Data.MapNpc[mapNum].Npc[i].Y);
            packetWriter.WriteByte(Data.MapNpc[mapNum].Npc[i].Dir);

            for (var x = 0; x < VitalCount; x++)
            {
                packetWriter.WriteInt32(Data.MapNpc[mapNum].Npc[i].Vital[x]);
            }
        }

        if (Data.MapResource[GetPlayerMap(playerId)].ResourceCount > 0)
        {
            packetWriter.WriteInt32(1);
            packetWriter.WriteInt32(Data.MapResource[GetPlayerMap(playerId)].ResourceCount);

            for (var i = 0; i < Data.MapResource[GetPlayerMap(playerId)].ResourceCount; i++)
            {
                packetWriter.WriteByte(Data.MapResource[GetPlayerMap(playerId)].ResourceData[i].State);
                packetWriter.WriteInt32(Data.MapResource[GetPlayerMap(playerId)].ResourceData[i].X);
                packetWriter.WriteInt32(Data.MapResource[GetPlayerMap(playerId)].ResourceData[i].Y);
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
        packetWriter.WriteInt32(GetPlayerJob(playerId));
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

        NetworkConfig.SendDataToMap(GetPlayerMap(playerId), packetWriter.GetBytes());
    }

    public static void SendMapMessage(int mapNum, string message)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SSendMapMessage);
        packetWriter.WriteString(message);

        NetworkConfig.SendDataToMap(mapNum, packetWriter.GetBytes());
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

    public static void SendActionMessage(int mapNum, string message, int color, int msgType, int x, int y, int playerOnlyNum = -1)
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
            NetworkConfig.SendDataToMap(mapNum, packetWriter.GetBytes());
        }
    }

    public static void SayMsg_Map(int mapNum, int playerId, string message, int sayColor)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SSayMsg);
        packetWriter.WriteString(GetPlayerName(playerId));
        packetWriter.WriteInt32(GetPlayerAccess(playerId));
        packetWriter.WriteBoolean(GetPlayerPk(playerId));
        packetWriter.WriteString(message);
        packetWriter.WriteString("[Map]:");
        packetWriter.WriteInt32(sayColor);

        NetworkConfig.SendDataToMap(mapNum, packetWriter.GetBytes());
    }

    public static void SayMsg_Global(int playerId, string message, int sayColor)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SSayMsg);
        packetWriter.WriteString(GetPlayerName(playerId));
        packetWriter.WriteInt32(GetPlayerAccess(playerId));
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
        packetWriter.WriteInt32(GetPlayerInventory(playerId, invSlot));
        packetWriter.WriteInt32(GetPlayerInventoryValue(playerId, invSlot));

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
        packetWriter.WriteInt32(dataType);

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
                                totalWorth += Item.Instance[GetPlayerInventory(playerId, Data.TempPlayer[playerId].TradeOffer[i].Num)].Price * Data.TempPlayer[playerId].TradeOffer[i].Value;
                            }
                            else
                            {
                                totalWorth += Item.Instance[GetPlayerInventory(playerId, Data.TempPlayer[playerId].TradeOffer[i].Num)].Price;
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
                            packetWriter.WriteInt32(GetPlayerInventory((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num));
                            packetWriter.WriteInt32(Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Value);

                            if (GetPlayerInventory((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num) < 0)
                            {
                                continue;
                            }

                            if (Item.Instance[GetPlayerInventory((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num)].Type == (int)ItemCategory.Currency || Item.Instance[GetPlayerInventory((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num)].Stackable == 1)
                            {
                                totalWorth += Item.Instance[GetPlayerInventory((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num)].Price * Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Value;
                            }
                            else
                            {
                                totalWorth += Item.Instance[GetPlayerInventory((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num)].Price;
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

    public static void SendTradeStatus(int playerId, byte status)
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

        for (var i = 0; i < Core.Globals.Variables.MaxMaps; i++)
        {
            packetWriter.WriteString(Data.Map[i].Name);
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

    public static void SendChatBubble(int mapNum, int target, int targetType, string message, int color)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SChatBubble);
        packetWriter.WriteInt32(target);
        packetWriter.WriteInt32(targetType);
        packetWriter.WriteString(message);
        packetWriter.WriteInt32(color);

        NetworkConfig.SendDataToMap(mapNum, packetWriter.GetBytes());
    }

    public static void SendPlayerAttack(int playerId)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(ServerPackets.SAttack);
        packetWriter.WriteInt32(playerId);

        NetworkConfig.SendDataToMapBut(playerId, GetPlayerMap(playerId), packetWriter.GetBytes());
    }
    
    public static void SendNpcAttack(int mapNum, int npcIndex)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(ServerPackets.SNpcAttack);
        packetWriter.WriteInt32(npcIndex);

        NetworkConfig.SendDataToMap(mapNum, packetWriter.GetBytes());
    }


    public static void SendMapItemToAll(int mapNum, int mapSlot)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SMapItemData);
        packet.WriteByte((byte)mapSlot);
        packet.WriteInt32(Data.MapItem[mapNum, mapSlot].Num);
        packet.WriteInt32(Data.MapItem[mapNum, mapSlot].Value);
        packet.WriteInt32(Data.MapItem[mapNum, mapSlot].X);
        packet.WriteInt32(Data.MapItem[mapNum, mapSlot].Y);

        NetworkConfig.SendDataToMap(mapNum, packet.GetBytes());
    }

    public static void SendMapItemsToAll(int mapNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SMapItemsData);

        for (var i = 0; i < Core.Globals.Variables.MaxMapItems; i++)
        {
            packet.WriteInt32(Data.MapItem[mapNum, i].Num);
            packet.WriteInt32(Data.MapItem[mapNum, i].Value);
            packet.WriteInt32(Data.MapItem[mapNum, i].X);
            packet.WriteInt32(Data.MapItem[mapNum, i].Y);
        }

        NetworkConfig.SendDataToMap(mapNum, packet.GetBytes());
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

        var moral = new Moral();
        if (Moral.Instance.Count > index)
            moral = (Moral)Moral.Instance[index];

        packet.WriteString(moral.Name);
        packet.WriteByte(moral.Color);
        packet.WriteBoolean(moral.NpcBlock);
        packet.WriteBoolean(moral.PlayerBlock);
        packet.WriteBoolean(moral.CanCast);
        packet.WriteBoolean(moral.CanDropItem);
        packet.WriteBoolean(moral.CanPickupItem);
        packet.WriteBoolean(moral.CanPk);
        packet.WriteBoolean(moral.DropItems);
        packet.WriteBoolean(moral.LoseExp);
    }

    public static void SendProjectileToMap(int mapNum, int projectileNum)
    {
        var mapProjectile = Data.MapProjectile[mapNum, projectileNum];
        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SMapProjectile);
        packet.WriteInt32(projectileNum);
        packet.WriteInt32(mapProjectile.ProjectileNum);
        packet.WriteInt32(mapProjectile.Owner);
        packet.WriteByte(mapProjectile.OwnerType);
        packet.WriteByte(mapProjectile.Dir);
        packet.WriteInt32(mapProjectile.X);
        packet.WriteInt32(mapProjectile.Y);
        packet.WriteInt16(mapProjectile.Vx);
        packet.WriteInt16(mapProjectile.Vy);
        packet.WriteByte(mapProjectile.FreeAim);

        NetworkConfig.SendDataToMap(mapNum, packet.GetBytes());
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

        var projectile = new ProjectileBase();
        if (Projectile.Instance.Count > index)
            projectile = Projectile.Instance[index];

        packet.WriteString(projectile.Name);
        packet.WriteInt32(projectile.Sprite);
        packet.WriteInt32(projectile.Range);
        packet.WriteInt32(projectile.Speed);
        packet.WriteInt32(projectile.Damage);
        packet.WriteInt32(projectile.Animation);
    }

    public static void SendUpdateResourceToAll(int resourceNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateResource);

        WriteResourceDataToPacket(resourceNum, packet);

        PlayerService.Instance.SendDataToAll(packet.GetBytes());
    }

    public static void SendUpdateResourceTo(int playerId, int resourceNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateResource);

        WriteResourceDataToPacket(resourceNum, packet);

        PlayerService.Instance.SendDataTo(playerId, packet.GetBytes());
    }

    private static void WriteResourceDataToPacket(int index, PacketWriter packet)
    {
        packet.WriteInt32(index);

        var resource = new Resource();
        if (Resource.Instance.Count > index)
            resource = (Resource)Resource.Instance[index];

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
    }

    public static void SendMapResourceToMap(int mapNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SMapResource);
        packet.WriteInt32(Data.MapResource[mapNum].ResourceCount);

        if (Data.MapResource[mapNum].ResourceCount > 0)
        {
            for (var i = 0; i < Data.MapResource[mapNum].ResourceCount; i++)
            {
                packet.WriteByte(Data.MapResource[mapNum].ResourceData[i].State);
                packet.WriteInt32(Data.MapResource[mapNum].ResourceData[i].X);
                packet.WriteInt32(Data.MapResource[mapNum].ResourceData[i].Y);
            }
        }

        NetworkConfig.SendDataToMap(mapNum, packet.GetBytes());
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

        var item = new Item();
        if (Item.Instance.Count > index)
            item = (Item)Item.Instance[index];

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
        packet.WriteInt32(item.Mastery);
        packet.WriteString(item.Name);
        packet.WriteInt32(item.Paperdoll);
        packet.WriteInt32(item.Icon);
        packet.WriteInt32(item.Price);
        packet.WriteInt32(item.Rarity);
        packet.WriteInt32(item.Speed);
        packet.WriteInt32(item.Stackable);
        packet.WriteString(item.Description);

        for (var i = 0; i < statCount; i++)
        {
            packet.WriteInt32(item.StatReq[i]);
        }

        packet.WriteInt32(item.Type);
        packet.WriteInt32(item.SubType);
        packet.WriteInt32(item.ItemLevel);
        packet.WriteInt32(item.KnockBack);
        packet.WriteInt32(item.KnockBackTiles);
        packet.WriteInt32(item.Projectile);
        packet.WriteInt32(item.Ammo);
    }

     public static void WriteJobDataToPacket(int index, PacketWriter packetWriter)
    {
        var job = new Job();
        if (Job.Instance.Count > index)
            job = (Job)Job.Instance[index];

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
    }

    public static void SendAnimation(int mapNum, int anim, int x, int y, byte lockType = 0, int lockindex = 0)
    {
        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SAnimation);
        packet.WriteInt32(anim);
        packet.WriteInt32(x);
        packet.WriteInt32(y);
        packet.WriteInt32(lockType);
        packet.WriteInt32(lockindex);

        NetworkConfig.SendDataToMap(mapNum, packet.GetBytes());
    }

     public static void SendAnimationTo(int index, int anim, int x, int y, byte lockType = 0, int lockindex = 0)
    {
        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SAnimation);
        packet.WriteInt32(anim);
        packet.WriteInt32(x);
        packet.WriteInt32(y);
        packet.WriteInt32(lockType);
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

        var animation = new Animation();
        if (Animation.Instance.Count > index)
            animation = (Animation)Animation.Instance[index];

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

        var mapNum = GetPlayerMap(index);

        buffer.WriteEnum(ServerPackets.SMapEventData);
        buffer.WriteInt32(Data.Map[mapNum].EventCount);

        if (Data.Map[mapNum].EventCount > 0)
        {
            Event.SerializeMapEvents(buffer, mapNum);
        }

        PlayerService.Instance.SendDataTo(index, buffer.GetBytes());

        SendSwitchesAndVariables(index);
    }


    public static void SendDataToParty(int partyNum, byte[] data)
    {
        var loopTo = Data.Party[partyNum].MemberCount;
        for (var i = 0; i < loopTo; i++)
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

    public static void SendMapNpcsToMap(int mapNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SMapNpcData);

        for (var mapNpcNum = 0; mapNpcNum < Core.Globals.Variables.MaxMapNpcs; mapNpcNum++)
        {
            packet.WriteInt32(Data.MapNpc[mapNum].Npc[mapNpcNum].Num);
            packet.WriteInt32(Data.MapNpc[mapNum].Npc[mapNpcNum].X);
            packet.WriteInt32(Data.MapNpc[mapNum].Npc[mapNpcNum].Y);
            packet.WriteByte(Data.MapNpc[mapNum].Npc[mapNpcNum].Dir);
        }

        NetworkConfig.SendDataToMap(mapNum, packet.GetBytes());
    }


    public static void SendNpcs(int playerId)
    {
        for (var npcNum = 0; npcNum < Core.Globals.Variables.MaxNpcs; npcNum++)
        {
            if (Data.Npc[npcNum].Name.Length > 0)
            {
                SendUpdateNpcTo(playerId, npcNum);
            }
        }
    }

    public static void SendUpdateNpcTo(int playerId, int npcNum)
    {
        var buffer = new PacketWriter();

        buffer.WriteEnum(ServerPackets.SUpdateNpc);
        buffer.WriteInt32(npcNum);
        buffer.WriteInt32(Data.Npc[npcNum].Animation);
        buffer.WriteString(Data.Npc[npcNum].AttackSay);
        buffer.WriteByte(Data.Npc[npcNum].Behavior);

        for (var i = 0; i < Core.Globals.Variables.MaxDropItems; i++)
        {
            buffer.WriteInt32(Data.Npc[npcNum].DropChance[i]);
            buffer.WriteInt32(Data.Npc[npcNum].DropItem[i]);
            buffer.WriteInt32(Data.Npc[npcNum].DropItemValue[i]);
        }

        buffer.WriteInt32(Data.Npc[npcNum].Experience);
        buffer.WriteByte(Data.Npc[npcNum].Faction);
        buffer.WriteInt32(Data.Npc[npcNum].Hp);
        buffer.WriteString(Data.Npc[npcNum].Name);
        buffer.WriteByte(Data.Npc[npcNum].Range);
        buffer.WriteByte(Data.Npc[npcNum].SpawnTime);
        buffer.WriteInt32(Data.Npc[npcNum].SpawnSecs);
        buffer.WriteInt32(Data.Npc[npcNum].Sprite);

        var statCount = Enum.GetValues<Stat>().Length;
        for (var i = 0; i < statCount; i++)
        {
            buffer.WriteByte(Data.Npc[npcNum].Stat[i]);
        }

        for (var i = 0; i < Core.Globals.Variables.MaxNpcSkills; i++)
        {
            buffer.WriteByte(Data.Npc[npcNum].Skill[i]);
        }

        buffer.WriteInt32(Data.Npc[npcNum].Level);
        buffer.WriteInt32(Data.Npc[npcNum].Damage);

        PlayerService.Instance.SendDataTo(playerId, buffer.GetBytes());
    }

    public static void SendUpdateNpcToAll(int npcNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateNpc);
        packet.WriteInt32(npcNum);
        packet.WriteInt32(Data.Npc[npcNum].Animation);
        packet.WriteString(Data.Npc[npcNum].AttackSay);
        packet.WriteByte(Data.Npc[npcNum].Behavior);

        for (var i = 0; i < Core.Globals.Variables.MaxDropItems; i++)
        {
            packet.WriteInt32(Data.Npc[npcNum].DropChance[i]);
            packet.WriteInt32(Data.Npc[npcNum].DropItem[i]);
            packet.WriteInt32(Data.Npc[npcNum].DropItemValue[i]);
        }

        packet.WriteInt32(Data.Npc[npcNum].Experience);
        packet.WriteByte(Data.Npc[npcNum].Faction);
        packet.WriteInt32(Data.Npc[npcNum].Hp);
        packet.WriteString(Data.Npc[npcNum].Name);
        packet.WriteByte(Data.Npc[npcNum].Range);
        packet.WriteByte(Data.Npc[npcNum].SpawnTime);
        packet.WriteInt32(Data.Npc[npcNum].SpawnSecs);
        packet.WriteInt32(Data.Npc[npcNum].Sprite);

        var statCount = Enum.GetValues<Stat>().Length;
        for (var i = 0; i < statCount; i++)
        {
            packet.WriteByte(Data.Npc[npcNum].Stat[i]);
        }

        for (var i = 0; i < Core.Globals.Variables.MaxNpcSkills; i++)
        {
            packet.WriteByte(Data.Npc[npcNum].Skill[i]);
        }

        packet.WriteInt32(Data.Npc[npcNum].Level);
        packet.WriteInt32(Data.Npc[npcNum].Damage);

        PlayerService.Instance.SendDataToAll(packet.GetBytes());
    }

    public static void SendMapNpcVitals(int mapNum, byte mapNpcNum)
    {
        var packet = new PacketWriter(4);

        packet.WriteInt32((int)ServerPackets.SMapNpcVitals);
        packet.WriteInt32(mapNpcNum);

        var vitalCount = Enum.GetValues<Vital>().Length;
        for (var i = 0; i < vitalCount; i++)
        {
            packet.WriteInt32(Data.MapNpc[mapNum].Npc[mapNpcNum].Vital[i]);
        }

        NetworkConfig.SendDataToMap(mapNum, packet.GetBytes());
    }

    public static void SendLeaveMap(int playerId, int mapNum)
    {
        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SLeftMap);
        packet.WriteInt32(playerId);

        NetworkConfig.SendDataToMapBut(playerId, mapNum, packet.GetBytes());
    }
}
