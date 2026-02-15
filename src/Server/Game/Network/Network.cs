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

public static class Network
{

    public static void AlertMessage(GameSession session, SystemMessage menuNo, Menu menuReset = 0, bool kick = true)
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

    public static void AlertMessage(int playerId, SystemMessage menuNo, Menu menuReset = 0, bool kick = true)
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

    public static void GlobalMessage(string message)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SGlobalMsg);
        packetWriter.WriteString(message);

        PlayerService.Instance.SendDataToAll(packetWriter.GetBytes());
    }

    public static void PlayerMessage(int playerId, string message, int color)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SPlayerMsg);
        packetWriter.WriteString(message);
        packetWriter.WriteInt32(color);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void StartSkillBuffer(int playerId, int skillSlot, int castTimeSeconds)
    {
        var packetWriter = new PacketWriter(20);
        packetWriter.WriteEnum(ServerPackets.SStartSkillBuffer);
        packetWriter.WriteInt32(skillSlot);
        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void ClearSkillBuffer(int playerId)
    {
        var packetWriter = new PacketWriter(4);
        packetWriter.WriteEnum(ServerPackets.SClearSkillBuffer);
        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void SkillCooldown(int playerId, int skillSlot)
    {
        var packetWriter = new PacketWriter(8);
        packetWriter.WriteEnum(ServerPackets.SCooldown);
        packetWriter.WriteInt32(skillSlot);
        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void PlayerCharacters(GameSession session)
    {
        var w = new PacketWriter();

        w.WriteEnum(ServerPackets.SPlayerCharacters);

        // Send exactly MaxCharacters entries in a predictable format
        for (int slot = 0; slot < Core.Globals.Variables.MaxCharacters; slot++)
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
            var equipmentCount = Enum.GetValues<Equipment>().Length;
            for (int eq = 0; eq < equipmentCount; eq++)
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

    public static void Variables(GameSession session)
    {
        // Send authoritative variables from script getters so client can size arrays correctly
        var w = new PacketWriter();
        w.WriteEnum(ServerPackets.SVariables);

        w.WriteInt32(Core.Globals.Variables.MaxAnimations);
        w.WriteInt32(Core.Globals.Variables.MaxItems);
        w.WriteInt32(Core.Globals.Variables.MaxMaps);
        w.WriteInt32(Core.Globals.Variables.MaxNpcs);
        w.WriteInt32(Core.Globals.Variables.MaxParty);
        w.WriteInt32(Core.Globals.Variables.MaxPartyMembers);
        w.WriteInt32(Core.Globals.Variables.MaxPlayers);
        w.WriteInt32(Core.Globals.Variables.MaxResources);
        w.WriteInt32(Core.Globals.Variables.MaxShops);
        w.WriteInt32(Core.Globals.Variables.MaxSkills);
        w.WriteInt32(Core.Globals.Variables.MaxProjectiles);
        w.WriteInt32(Core.Globals.Variables.MaxSwitches);
        w.WriteInt32(Core.Globals.Variables.MaxVariables);
        w.WriteInt32(Core.Globals.Variables.ChatLines);
        w.WriteInt32(Core.Globals.Variables.MaxEvents);
        w.WriteInt32(Core.Globals.Variables.TileSize);
        w.WriteInt32(Core.Globals.Variables.MaxWeatherParticles);

        w.WriteByte(Core.Globals.Variables.MaxBank);
        w.WriteByte(Core.Globals.Variables.MaxJobs);
        w.WriteByte(Core.Globals.Variables.MaxMorals);
        w.WriteByte(Core.Globals.Variables.MaxInventory);
        w.WriteByte(Core.Globals.Variables.MaxMapItems);

        w.WriteInt32(Core.Globals.Variables.MaxMapNpcs);
        
        w.WriteByte(Core.Globals.Variables.MaxNpcSkills);
        w.WriteByte(Core.Globals.Variables.MaxPlayerSkills);
        w.WriteByte(Core.Globals.Variables.MaxTrades);
        w.WriteByte(Core.Globals.Variables.NameLength);
        w.WriteByte(Core.Globals.Variables.MinimumNameLength);
        w.WriteByte(Core.Globals.Variables.ChatLength);
        w.WriteByte(Core.Globals.Variables.MaxHotbar);
        w.WriteByte(Core.Globals.Variables.MaxMapX);
        w.WriteByte(Core.Globals.Variables.MaxMapY);
        w.WriteByte(Core.Globals.Variables.MaxDropItems);
        w.WriteByte(Core.Globals.Variables.MaxStartItems);
        w.WriteByte(Core.Globals.Variables.MaxStartSkills);
        w.WriteByte(Core.Globals.Variables.MaxCharacters);
        w.WriteByte(Core.Globals.Variables.MaxStats);
        w.WriteByte(Core.Globals.Variables.MaxQuests);
        w.WriteByte(Core.Globals.Variables.MaxGuilds);
        w.WriteByte(Core.Globals.Variables.MaxEventChoices);
        w.WriteByte(Core.Globals.Variables.MaxLevel);
        w.WriteInt32(Core.Globals.Variables.MaxPoints);

        session.Channel.Send(w.GetBytes());
    }

    public static void CloseTrade(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SCloseTrade);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void Experience(int playerId)
    {
        var packetWriter = new PacketWriter(16);

        packetWriter.WriteEnum(ServerPackets.SPlayerExp);
        packetWriter.WriteInt32(playerId);
        packetWriter.WriteInt32(GetExp(playerId));
        packetWriter.WriteInt32(Script.Instance?.GetPlayerNextLevel(playerId));

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void LoginOk(int playerId)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(ServerPackets.SLoginOk);
        packetWriter.WriteInt32(playerId);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void InGame(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SInGame);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void PlaySound(int playerId, string fileName, int x, int y)
    {
        var packetWriter = new PacketWriter();
        packetWriter.WriteEnum(ServerPackets.SPlaySound);
        packetWriter.WriteString(fileName);
        packetWriter.WriteInt32(x);
        packetWriter.WriteInt32(y);
        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void Jobs(GameSession session)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SJobData);

        for (var i = 0; i < Core.Globals.Variables.MaxJobs; i++)
        {
            WriteJobDataToPacket(i, packetWriter);
        }

        session.Channel.Send(packetWriter.GetBytes());
    }

    public static void JobToAll(int job)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SJobData);

        for (var i = 0; i < Core.Globals.Variables.MaxJobs; i++)
        {
            WriteJobDataToPacket(i, packetWriter);
        }

        PlayerService.Instance.SendDataToAll(packetWriter.GetBytes());
    }

    public static void Inventory(int playerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SInventory);

        for (var i = 0; i < Core.Globals.Variables.MaxInventory; i++)
        {
            packetWriter.WriteInt32(GetInv(playerId, i));
            packetWriter.WriteInt32(GetInvValue(playerId, i));
            packetWriter.WriteInt32(Player.Instance[playerId].Inventory[i].Durability);
        }

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void LeftGame(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SLeftGame);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void MapEquipment(int playerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SMapWornEq);
        packetWriter.WriteInt32(playerId);

        var equipmentCount = Enum.GetValues<Equipment>().Length;
        for (var i = 0; i < equipmentCount; i++)
        {
            var itemId = GetPaperdoll(playerId, (Equipment)i);
            var durability = 0;
            if (Player.Instance[playerId].Paperdoll is not null && i >= 0 && i < Player.Instance[playerId].Paperdoll.Length)
            {
                durability = Player.Instance[playerId].Paperdoll[i].Durability;
            }

            packetWriter.WriteInt32(itemId);
            packetWriter.WriteInt32(durability);
        }

        NetworkConfig.SendDataToMap(GetMap(playerId), packetWriter.GetBytes());
    }

    public static void MapEquipmentTo(int equipmentPlayerId, int sendToPlayerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SMapWornEq);
        packetWriter.WriteInt32(equipmentPlayerId);

        var equipmentCount = Enum.GetValues<Equipment>().Length;
        for (var i = 0; i < equipmentCount; i++)
        {
            var itemId = GetPaperdoll(equipmentPlayerId, (Equipment)i);
            var durability = 0;
            if (Player.Instance[equipmentPlayerId].Paperdoll is not null && i >= 0 && i < Player.Instance[equipmentPlayerId].Paperdoll.Length)
            {
                durability = Player.Instance[equipmentPlayerId].Paperdoll[i].Durability;
            }

            packetWriter.WriteInt32(itemId);
            packetWriter.WriteInt32(durability);
        }

        PlayerService.Instance.SendDataTo(sendToPlayerId, packetWriter.GetBytes());
    }

    public static void Shops(int playerId)
    {
        for (var i = 0; i < Core.Globals.Variables.MaxShops; i++)
        {
            UpdateShopTo(playerId, i);
        }
    }

    public static void UpdateShopTo(int playerId, int index)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SUpdateShop);
        WriteShopDataToPacket(index, packetWriter);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void UpdateShopToAll(int shopNum)
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

    public static void Skills(int playerId)
    {
        for (var i = 0; i < Core.Globals.Variables.MaxSkills; i++)
        {
            UpdateSkillTo(playerId, i);
        }
    }

    public static void UpdateSkillTo(int playerId, int index)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SUpdateSkill);
        WriteSkillDataToPacket(index, packetWriter);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void UpdateSkillToAll(int skillNum)
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

        packetWriter.WriteSingle(skill.MoveSpeed);

        packetWriter.WriteBoolean(skill.MoveCast);

        packetWriter.WriteInt32(skill.SpCost);

        // Optional trailing fields (backward compatible)
        packetWriter.WriteInt32(skill.NextRank);
        packetWriter.WriteInt32(skill.NextUses);
    }

    public static void Stats(int playerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SPlayerStats);
        packetWriter.WriteInt32(playerId);

        var statCount = Enum.GetValues<Stat>().Length;
        for (var i = 0; i < statCount; i++)
        {
            packetWriter.WriteInt32(GetStat(playerId, (Stat)i));
        }

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void Vitals(int playerId)
    {
        var vitalCount = Enum.GetValues<Vital>().Length;
        for (var i = 0; i < vitalCount; i++)
        {
            Vital(playerId, (Vital)i);
        }
    }

    public static void Vital(int playerId, Vital vital)
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

        packetWriter.WriteInt32(GetVital(playerId, vital));
        packetWriter.WriteInt32(Script.Instance?.GetMaxVital(playerId, vital));

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());

        if (Data.TempPlayer[playerId].InParty >= 0)
        {
            Network.PartyVitals(Data.TempPlayer[playerId].InParty, playerId);
        }
    }

    public static void Welcome(int playerId)
    {
        if (Core.Globals.Variables.Welcome.Length > 0)
        {
            PlayerMessage(playerId, Core.Globals.Variables.Welcome, (int)ColorName.BrightCyan);
        }

        WhosOnline(playerId);
    }

    public static void WhosOnline(int playerId)
    {
        if (GetAccess(playerId) < (int)Access.Moderator)
        {
            return;
        }

        var playerNames = PlayerService.Instance.PlayerIds
            .Where(otherPlayerId => otherPlayerId != playerId)
            .Select(GetName)
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

        PlayerMessage(playerId, message, (int)ColorName.White);
    }

    public static void WornEquipment(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SPlayerWornEq);

        var equipmentCount = Enum.GetValues<Equipment>().Length;
        for (var i = 0; i < equipmentCount; i++)
        {
            var itemId = GetPaperdoll(playerId, (Equipment)i);
            var durability = 0;
            if (Player.Instance[playerId].Paperdoll is not null && i >= 0 && i < Player.Instance[playerId].Paperdoll.Length)
            {
                durability = Player.Instance[playerId].Paperdoll[i].Durability;
            }

            packetWriter.WriteInt32(itemId);
            packetWriter.WriteInt32(durability);
        }

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void MapData(int playerId, int map, bool sendMap)
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

                    var mapLayerCount = Enum.GetValues<MapLayer>().Length;
                    for (var i = 0; i < mapLayerCount; i++)
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
            packetWriter.WriteInt32(MapItem.Instance[map, i].Durability);
        }

        for (var i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
        {
            packetWriter.WriteInt32(MapNpc.Instance[map, i].Num);
            packetWriter.WriteInt32(MapNpc.Instance[map, i].X);
            packetWriter.WriteInt32(MapNpc.Instance[map, i].Y);
            packetWriter.WriteByte(MapNpc.Instance[map, i].Dir);

            var vitalCount = Enum.GetValues<Vital>().Length;
            for (var x = 0; x < vitalCount; x++)
            {
                packetWriter.WriteInt32(MapNpc.Instance[map, i].Vital[x]);
            }
        }

        if (MapResource.Instance[GetMap(playerId)].ResourceCount > 0)
        {
            packetWriter.WriteInt32(1);
            packetWriter.WriteInt32(MapResource.Instance[GetMap(playerId)].ResourceCount);

            for (var i = 0; i < MapResource.Instance[GetMap(playerId)].ResourceCount; i++)
            {
                packetWriter.WriteByte(MapResource.Instance[GetMap(playerId)].ResourceData[i].State);
                packetWriter.WriteInt32(MapResource.Instance[GetMap(playerId)].ResourceData[i].X);
                packetWriter.WriteInt32(MapResource.Instance[GetMap(playerId)].ResourceData[i].Y);
            }
        }
        else
        {
            packetWriter.WriteInt32(0);
        }

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void JoinMap(int playerId)
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
        packetWriter.WriteString(GetName(playerId));
        packetWriter.WriteByte(GetJob(playerId));
        packetWriter.WriteInt32(GetLevel(playerId));
        packetWriter.WriteInt32(GetPoints(playerId));
        packetWriter.WriteInt32(GetSprite(playerId));
        packetWriter.WriteInt32(GetMap(playerId));
        packetWriter.WriteByte(GetAccess(playerId));
        packetWriter.WriteBoolean(GetPk(playerId));

        var statCount = Enum.GetValues<Stat>().Length;
        for (var i = 0; i < statCount; i++)
        {
            packetWriter.WriteInt32(GetStat(playerId, (Stat)i));
        }

        var resourceSkillCount = Enum.GetValues<ResourceSkill>().Length;
        for (var i = 0; i < resourceSkillCount; i++)
        {
            packetWriter.WriteInt32(SetGatherLevel(playerId, i));
            packetWriter.WriteInt32(GetGatherExp(playerId, i));
            packetWriter.WriteInt32(GetGatherSkillMaxExp(playerId, i));
        }

        return packetWriter.GetBytes();
    }

    public static void PlayerXY(int playerId)
    {
        PlayerXYTo(playerId, playerId);
    }

    public static void PlayerXYTo(int sendToPlayerId, int positionPlayerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SPlayerXY);
        packetWriter.WriteInt32(positionPlayerId);
        packetWriter.WriteInt32(GetRawX(positionPlayerId));
        packetWriter.WriteInt32(GetRawY(positionPlayerId));
        packetWriter.WriteByte(GetDir(positionPlayerId));
        packetWriter.WriteByte(Player.Instance[positionPlayerId].Moving);
        packetWriter.WriteBoolean(Player.Instance[positionPlayerId].IsMoving);

        // Active movement speed multiplier (1.0f = normal).
        float mult = 1.0f;
        if (positionPlayerId >= 0 && positionPlayerId < Data.TempPlayer.Length)
        {
            mult = Data.TempPlayer[positionPlayerId].MoveSpeedMultiplier;
            if (mult <= 0) mult = 1.0f;
            var expiry = Data.TempPlayer[positionPlayerId].MoveSpeedMultiplierTimer;
            if (expiry > 0 && expiry <= General.GetTime())
            {
                Data.TempPlayer[positionPlayerId].MoveSpeedMultiplier = 1.0f;
                Data.TempPlayer[positionPlayerId].MoveSpeedMultiplierTimer = 0;
                mult = 1.0f;
            }
        }
        packetWriter.WriteSingle(mult);

        PlayerService.Instance.SendDataTo(sendToPlayerId, packetWriter.GetBytes());
    }

    public static void PlayerXYToMap(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SPlayerXY);
        packetWriter.WriteInt32(playerId);
        packetWriter.WriteInt32(GetRawX(playerId));
        packetWriter.WriteInt32(GetRawY(playerId));
        packetWriter.WriteByte(GetDir(playerId));
        packetWriter.WriteByte(Player.Instance[playerId].Moving);
        packetWriter.WriteBoolean(Player.Instance[playerId].IsMoving);

        float mult = 1.0f;
        if (playerId >= 0 && playerId < Data.TempPlayer.Length)
        {
            mult = Data.TempPlayer[playerId].MoveSpeedMultiplier;
            if (mult <= 0) mult = 1.0f;
            var expiry = Data.TempPlayer[playerId].MoveSpeedMultiplierTimer;
            if (expiry > 0 && expiry <= General.GetTime())
            {
                Data.TempPlayer[playerId].MoveSpeedMultiplier = 1.0f;
                Data.TempPlayer[playerId].MoveSpeedMultiplierTimer = 0;
                mult = 1.0f;
            }
        }
        packetWriter.WriteSingle(mult);

        NetworkConfig.SendDataToMap(GetMap(playerId), packetWriter.GetBytes());
    }

    public static void MapMessage(int map, string message)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SSendMapMessage);
        packetWriter.WriteString(message);

        NetworkConfig.SendDataToMap(map, packetWriter.GetBytes());
    }

    public static void AdminMessage(string message)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SSendAdminMessage);
        packetWriter.WriteString(message);

        foreach (var playerId in PlayerService.Instance.PlayerIds)
        {
            if (GetAccess(playerId) >= (int)Access.Moderator)
            {
                PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
            }
        }
    }

    public static void ActionMessage(int map, string message, int color, int msgType, int x, int y, int playerOnly = -1)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SActionMessage);
        packetWriter.WriteString(message);
        packetWriter.WriteInt32(color);
        packetWriter.WriteInt32(msgType);
        packetWriter.WriteInt32(x);
        packetWriter.WriteInt32(y);

        if (playerOnly >= 0)
        {
            PlayerService.Instance.SendDataTo(playerOnly, packetWriter.GetBytes());
        }
        else
        {
            NetworkConfig.SendDataToMap(map, packetWriter.GetBytes());
        }
    }

    public static void SayMessage_Map(int map, int playerId, string message, int sayColor)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SSayMessage);
        packetWriter.WriteString(GetName(playerId));
        packetWriter.WriteInt32((int)GetAccess(playerId));
        packetWriter.WriteBoolean(GetPk(playerId));
        packetWriter.WriteString(message);
        packetWriter.WriteString("[Map]:");
        packetWriter.WriteInt32(sayColor);

        NetworkConfig.SendDataToMap(map, packetWriter.GetBytes());
    }

    public static void SayMessage_Global(int playerId, string message, int sayColor)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SSayMessage);
        packetWriter.WriteString(GetName(playerId));
        packetWriter.WriteInt32((int)GetAccess(playerId));
        packetWriter.WriteBoolean(GetPk(playerId));
        packetWriter.WriteString(message);
        packetWriter.WriteString("[Global]:");
        packetWriter.WriteInt32(sayColor);

        PlayerService.Instance.SendDataToAll(packetWriter.GetBytes());
    }

    public static void PlayerData(int playerId)
    {
        NetworkConfig.SendDataToMap(GetMap(playerId), GetPlayerDataPacket(playerId));
    }

    public static void InventoryUpdate(int playerId, int invSlot)
    {
        var packetWriter = new PacketWriter(20);

        packetWriter.WriteEnum(ServerPackets.SInventoryUpdate);
        packetWriter.WriteInt32(invSlot);
        packetWriter.WriteInt32(GetInv(playerId, invSlot));
        packetWriter.WriteInt32(GetInvValue(playerId, invSlot));
        packetWriter.WriteInt32(Player.Instance[playerId].Inventory[invSlot].Durability);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void OpenShop(int playerId, int shopNum)
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

    public static void Bank(int playerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SBank);

        for (var i = 0; i < Core.Globals.Variables.MaxBank; i++)
        {
            byte slot = (byte)Data.TempPlayer[playerId].Slot;
            packetWriter.WriteInt32(Account.Instance[playerId].Bank[slot].Item[i].Num);
            packetWriter.WriteInt32(Account.Instance[playerId].Bank[slot].Item[i].Value);
            packetWriter.WriteInt32(Account.Instance[playerId].Bank[slot].Item[i].Durability);
        }

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void TradeInvite(int playerId, int tradeindex)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(ServerPackets.STradeInvite);
        packetWriter.WriteInt32(tradeindex);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void Trade(int playerId, int tradeTarget)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(ServerPackets.STrade);
        packetWriter.WriteInt32(tradeTarget);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void TradeUpdate(int playerId, byte dataType)
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
                            var invSlot = Data.TempPlayer[playerId].TradeOffer[i].Num;
                            packetWriter.WriteInt32(invSlot);
                            packetWriter.WriteInt32(Data.TempPlayer[playerId].TradeOffer[i].Value);
                            packetWriter.WriteInt32(Player.Instance[playerId].Inventory[invSlot].Durability);

                            if (Item.Instance[Data.TempPlayer[playerId].TradeOffer[i].Num].Type == (int)ItemCategory.Currency || Item.Instance[Data.TempPlayer[playerId].TradeOffer[i].Num].Stackable == 1)
                            {
                                totalWorth += Item.Instance[GetInv(playerId, Data.TempPlayer[playerId].TradeOffer[i].Num)].Price * Data.TempPlayer[playerId].TradeOffer[i].Value;
                            }
                            else
                            {
                                totalWorth += Item.Instance[GetInv(playerId, Data.TempPlayer[playerId].TradeOffer[i].Num)].Price;
                            }
                        }
                        else
                        {
                            packetWriter.WriteInt32(-1);
                            packetWriter.WriteInt32(0);
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
                            var invSlot = Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num;
                            packetWriter.WriteInt32(GetInv((int)tradeTarget, invSlot));
                            packetWriter.WriteInt32(Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Value);
                            packetWriter.WriteInt32(Player.Instance[(int)tradeTarget].Inventory[invSlot].Durability);

                            if (GetInv((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num) < 0)
                            {
                                continue;
                            }

                            if (Item.Instance[GetInv((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num)].Type == (int)ItemCategory.Currency || Item.Instance[GetInv((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num)].Stackable == 1)
                            {
                                totalWorth += Item.Instance[GetInv((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num)].Price * Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Value;
                            }
                            else
                            {
                                totalWorth += Item.Instance[GetInv((int)tradeTarget, Data.TempPlayer[(int)tradeTarget].TradeOffer[i].Num)].Price;
                            }
                        }
                        else
                        {
                            packetWriter.WriteInt32(-1);
                            packetWriter.WriteInt32(0);
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

    public static void TradeStatus(int playerId, int status)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(ServerPackets.STradeStatus);
        packetWriter.WriteInt32(status);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void PlayerSkills(int playerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SSkills);

        for (var i = 0; i < Core.Globals.Variables.MaxPlayerSkills; i++)
        {
            packetWriter.WriteInt32(GetSkill(playerId, i));
            packetWriter.WriteInt32(GetSkillUses(playerId, i));
        }

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void Target(int playerId, int target, int targetType)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(ServerPackets.STarget);
        packetWriter.WriteInt32(target);
        packetWriter.WriteInt32(targetType);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void MapReport(int playerId)
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

    public static void AdminPanel(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SAdmin);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void Hotbar(int playerId)
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

    public static void RightClick(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SrClick);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void JobEditor(int playerId)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(ServerPackets.SJobEditor);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void Emote(int playerId, int emote)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(ServerPackets.SEmote);
        packetWriter.WriteInt32(playerId);
        packetWriter.WriteInt32(emote);

        NetworkConfig.SendDataToMap(GetMap(playerId), packetWriter.GetBytes());
    }

    public static void ChatBubble(int map, int target, int targetType, string message, int color)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SChatBubble);
        packetWriter.WriteInt32(target);
        packetWriter.WriteInt32(targetType);
        packetWriter.WriteString(message);
        packetWriter.WriteInt32(color);

        NetworkConfig.SendDataToMap(map, packetWriter.GetBytes());
    }

    public static void PlayerAttack(int playerId)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(ServerPackets.SAttack);
        packetWriter.WriteInt32(playerId);

        NetworkConfig.SendDataToMapBut(playerId, GetMap(playerId), packetWriter.GetBytes());
    }
    
    public static void NpcAttack(int map, int npcIndex)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(ServerPackets.SNpcAttack);
        packetWriter.WriteInt32(npcIndex);

        NetworkConfig.SendDataToMap(map, packetWriter.GetBytes());
    }


    public static void MapItemToAll(int map, int mapSlot)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SMapItemData);
        packet.WriteByte((byte)mapSlot);
        packet.WriteInt32(MapItem.Instance[map, mapSlot].Num);
        packet.WriteInt32(MapItem.Instance[map, mapSlot].Value);
        packet.WriteInt32(MapItem.Instance[map, mapSlot].X);
        packet.WriteInt32(MapItem.Instance[map, mapSlot].Y);
        packet.WriteInt32(MapItem.Instance[map, mapSlot].Durability);

        NetworkConfig.SendDataToMap(map, packet.GetBytes());
    }

    public static void MapItemsToAll(int map)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SMapItemsData);

        for (var i = 0; i < Core.Globals.Variables.MaxMapItems; i++)
        {
            packet.WriteInt32(MapItem.Instance[map, i].Num);
            packet.WriteInt32(MapItem.Instance[map, i].Value);
            packet.WriteInt32(MapItem.Instance[map, i].X);
            packet.WriteInt32(MapItem.Instance[map, i].Y);
            packet.WriteInt32(MapItem.Instance[map, i].Durability);
        }

        NetworkConfig.SendDataToMap(map, packet.GetBytes());
    }

    public static void Morals(int playerId)
    {
        for (var i = 0; i < Core.Globals.Variables.MaxMorals; i++)
        {
            UpdateMoralTo(playerId, i);
        }
    }

    public static void UpdateMoralTo(int playerId, int moralNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateMoral);

        WriteMoralDataToPacket(moralNum, packet);

        PlayerService.Instance.SendDataTo(playerId, packet.GetBytes());
    }

    public static void UpdateMoralToAll(int moralNum)
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

    public static void ProjectileToMap(int map, int projectileNum)
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

    public static void UpdateProjectileToAll(int projectileNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateProjectile);
        WriteProjectileDataToPacket(projectileNum, packet);

        PlayerService.Instance.SendDataToAll(packet.GetBytes());
    }

    public static void UpdateProjectileTo(int playerId, int projectileNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateProjectile);
        WriteProjectileDataToPacket(projectileNum, packet);

        PlayerService.Instance.SendDataTo(playerId, packet.GetBytes());
    }

    public static void Projectiles(int playerId)
    {
        for (var projectile = 0; projectile < Core.Globals.Variables.MaxProjectiles; projectile++)
        {
            UpdateProjectileTo(playerId, projectile);
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

    public static void UpdateResourceToAll(int index)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateResource);

        WriteResourceDataToPacket(index, packet);

        PlayerService.Instance.SendDataToAll(packet.GetBytes());
    }

    public static void UpdateResourceTo(int playerId, int index)
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

    public static void MapResourceToMap(int map)
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

    public static void Resources(int playerId)
    {
        for (var i = 0; i < Core.Globals.Variables.MaxResources; i++)
        { 
            Network.UpdateResourceTo(playerId, i);
        }
    }

    public static void Items(int playerId)
    {
        for (var i = 0; i < Core.Globals.Variables.MaxItems; i++)
        {
            UpdateItemTo(playerId, i);         
        }
    }

    public static void UpdateItemTo(int playerId, int index)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateItem);

        WriteItemDataToPacket(index, packet);

        PlayerService.Instance.SendDataTo(playerId, packet.GetBytes());
    }

    public static void UpdateItemToAll(int index)
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
        packet.WriteInt32(item.AttackSpeed);
        packet.WriteSingle(item.MovementSpeed);
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

        packet.WriteInt32(item.MaxDurability);
    }

     public static void WriteJobDataToPacket(int index, PacketWriter packetWriter)
    {
        var job = index >= 0 && index < Job.Instance.Count ? Job.Instance[index] : new JobBase();

        packetWriter.WriteString(job.Name);
        packetWriter.WriteString(job.Desc);
        packetWriter.WriteInt32(job.MaleSprite);
        packetWriter.WriteInt32(job.FemaleSprite);

        var statCount = Enum.GetValues<Stat>().Length;
        for (var i = 0; i < statCount; i++)
        {
            packetWriter.WriteInt32(job.Stat[i]);
        }

        for (var q = 0; q < Core.Globals.Variables.MaxStartItems; q++)
        {
            packetWriter.WriteInt32(job.StartItem[q]);
            packetWriter.WriteInt32(job.StartValue[q]);
        }

        for (var q = 0; q < Core.Globals.Variables.MaxStartSkills; q++)
        {
            packetWriter.WriteInt32(job.StartSkill[q]);
        }

        packetWriter.WriteInt32(job.StartMap);
        packetWriter.WriteByte(job.StartX);
        packetWriter.WriteByte(job.StartY);
        packetWriter.WriteInt32(job.BaseExp);
        packetWriter.WriteSingle(job.MoveSpeed);
    }

    public static void PlayAnimation(int map, int anim, int x, int y, byte lockType = 0, int lockindex = 0)
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

    public static void PlayAnimationTo(int index, int anim, int x, int y, byte lockType = 0, int lockindex = 0)
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
    
    public static void Animations(int playerId)
    {
        for (var index = 0; index < Core.Globals.Variables.MaxAnimations; index++)
        {
            UpdateAnimationTo(playerId, index);
        }
    }

    public static void UpdateAnimationTo(int playerId, int index)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateAnimation);

        WriteAnimationDataToPacket(index, packet);

        PlayerService.Instance.SendDataTo(playerId, packet.GetBytes());
    }

    public static void UpdateAnimationToAll(int animationNum)
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


    public static void SpecialEffect(int index, int effectType, int data1 = 0, int data2 = 0, int data3 = 0, int data4 = 0)
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

    public static void SwitchesAndVariables(int index, bool everyone = false)
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

    public static void MapEventData(int index)
    {
        var buffer = new PacketWriter(4);

        var map = GetMap(index);

        buffer.WriteEnum(ServerPackets.SMapEventData);

        if (map < 0 || map >= Server.Map.Instance.Count)
        {
            General.Logger.LogWarning("SendMapEventData called with invalid map {MapId} for player {PlayerId}", map, index);
            buffer.WriteInt32(0);
            PlayerService.Instance.SendDataTo(index, buffer.GetBytes());
            SwitchesAndVariables(index);
            return;
        }

        buffer.WriteInt32(Server.Map.Instance[map].EventCount);

        if (Server.Map.Instance[map].EventCount > 0)
        {
            Event.SerializeMapEvents(buffer, map);
        }

        PlayerService.Instance.SendDataTo(index, buffer.GetBytes());

        SwitchesAndVariables(index);
    }


    public static void DataToParty(int partyNum, byte[] data)
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

    public static void PartyInvite(int playerId, int target)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SPartyInvite);
        packetWriter.WriteString(Player.Instance[target].Name);

        PlayerService.Instance.SendDataTo(playerId, packetWriter.GetBytes());
    }

    public static void PartyUpdate(int partyNum)
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

        DataToParty(partyNum, packetWriter.GetBytes());
    }

    public static void PartyUpdateTo(int index)
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
    public static void PartyVitals(int partyNum, int playerId)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SPartyVitals);
        packetWriter.WriteInt32(playerId);

        var vitalCount = Enum.GetNames<Vital>().Length;
        for (var i = 0; i < vitalCount; i++)
        {
            packetWriter.WriteInt32(Player.Instance[playerId].Vital[i]);
        }

        DataToParty(partyNum, packetWriter.GetBytes());
    }

    public static void MapNpcsToMap(int map)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SMapNpcData);

        for (var i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
        {
            packet.WriteInt32(MapNpc.Instance[map, i].Num);
            packet.WriteInt32(MapNpc.Instance[map, i].X);
            packet.WriteInt32(MapNpc.Instance[map, i].Y);
            packet.WriteByte(MapNpc.Instance[map, i].Dir);

            // Remaining ms until respawn (0 if alive)
            var remaining = 0;
            var expiry = MapNpc.Instance[map, i].DeathTimer;
            if (expiry > 0)
            {
                var now = General.GetTime();
                var ms = expiry - now;
                if (ms > 0 && ms <= int.MaxValue)
                {
                    remaining = (int)ms;
                }
            }
            packet.WriteInt32(remaining);
        }

        NetworkConfig.SendDataToMap(map, packet.GetBytes());
    }

    public static void MapNpcsToPlayer(int playerId, int map)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SMapNpcData);

        for (var i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
        {
            packet.WriteInt32(MapNpc.Instance[map, i].Num);
            packet.WriteInt32(MapNpc.Instance[map, i].X);
            packet.WriteInt32(MapNpc.Instance[map, i].Y);
            packet.WriteByte(MapNpc.Instance[map, i].Dir);

            // Remaining ms until respawn (0 if alive)
            var remaining = 0;
            var expiry = MapNpc.Instance[map, i].DeathTimer;
            if (expiry > 0)
            {
                var now = General.GetTime();
                var ms = expiry - now;
                if (ms > 0 && ms <= int.MaxValue)
                {
                    remaining = (int)ms;
                }
            }
            packet.WriteInt32(remaining);
        }

        PlayerService.Instance.SendDataTo(playerId, packet.GetBytes());
    }


    public static void Npcs(int playerId)
    {
        for (var i = 0; i < Core.Globals.Variables.MaxNpcs; i++)
        {
            UpdateNpcTo(playerId, i);
        }
    }

    public static void UpdateNpcTo(int playerId, int npcNum)
    {
        var buffer = new PacketWriter();

        buffer.WriteEnum(ServerPackets.SUpdateNpc);
        WriteNpcDataToPacket(npcNum, buffer);

        PlayerService.Instance.SendDataTo(playerId, buffer.GetBytes());
    }

    public static void UpdateNpcToAll(int npcNum)
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

        var statCount = Enum.GetValues<Stat>().Length;
        for (var i = 0; i < statCount; i++)
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

    public static void MapNpcVitals(int map, int npc)
    {
        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SMapNpcVitals);
        packet.WriteInt32(npc);

        var vitalCount = Enum.GetValues<Vital>().Length;
        for (var i = 0; i < vitalCount; i++)
        {
            packet.WriteInt32(MapNpc.Instance[map, npc].Vital[i]);
        }

        NetworkConfig.SendDataToMap(map, packet.GetBytes());
    }

    public static void LeaveMap(int playerId, int map)
    {
        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SLeftMap);
        packet.WriteInt32(playerId);

        NetworkConfig.SendDataToMapBut(playerId, map, packet.GetBytes());
    }

    public static void PlayerDeath(int playerId, int deathTimer)
    {
        var packet = new Core.Net.PacketWriter();
        packet.WriteEnum(ServerPackets.SPlayerDead);
        packet.WriteInt32(deathTimer);
        packet.WriteInt32(playerId);
        PlayerService.Instance.SendDataTo(playerId, packet.GetBytes());
    }

    public static void NpcDeath(int map, int npc, int deathTimer)
    {
        var packet = new PacketWriter(8);
        packet.WriteEnum(ServerPackets.SNpcDead);
        packet.WriteInt32(deathTimer);
        packet.WriteInt32(npc);
        NetworkConfig.SendDataToMap(map, packet.GetBytes());
    }

    public static void NpcSpawn(int map, int npc)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SSpawnNpc);
        packet.WriteInt32(npc);
        packet.WriteInt32(MapNpc.Instance[map, npc].Num);
        packet.WriteInt32(MapNpc.Instance[map, npc].X);
        packet.WriteInt32(MapNpc.Instance[map, npc].Y);
        packet.WriteByte(MapNpc.Instance[map, npc].Dir);
        packet.WriteInt32(MapNpc.Instance[map, npc].DeathTimer);

        var vitalCount = Enum.GetValues<Vital>().Length;
        for (var i = 0; i < vitalCount; i++)
        {
            packet.WriteInt32(MapNpc.Instance[map, npc].Vital[i]);
        }

        NetworkConfig.SendDataToMap(map, packet.GetBytes());
    }
}
