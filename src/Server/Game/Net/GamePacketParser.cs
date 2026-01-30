using Core;
using Core.Common;
using Core.Globals;
using Core.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Serilog.Parsing;
using Server.Net;
using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XtremeWorlds.Server.Configuration;
using static Core.Globals.Commands;
using static Core.Globals.Type;
using static Core.Net.Packets;
using Type = Core.Globals.Type;
using static Server.Globals.Commands;
using Core.Objects;

namespace Server.Game.Net;

public sealed class GamePacketParser : PacketParser<Packets.ClientPackets, GameSession>
{
    protected override bool ValidateSession(GameSession session)
    {
        if (session is null)
            return false;

        var id = session.Id;
        if (id < 0 || id >= Core.Globals.Variables.MaxPlayers)
            return false;

        return true;
    }

    public GamePacketParser()
    {
        Bind(Packets.ClientPackets.CCheckPing, Ping);
        Bind(Packets.ClientPackets.CLogin, Login);
        Bind(Packets.ClientPackets.CRegister, Register);
        Bind(Packets.ClientPackets.CAddChar, AddChar);
        Bind(Packets.ClientPackets.CUseChar, UseChar);
        Bind(Packets.ClientPackets.CDelChar, DelChar);
        Bind(Packets.ClientPackets.CLogout, Logout);
        Bind(Packets.ClientPackets.CSayMessage, SayMessage);
        Bind(Packets.ClientPackets.CBroadcastMsg, BroadCastMsg);
        Bind(Packets.ClientPackets.CPlayerMsg, PlayerMsg);
        Bind(Packets.ClientPackets.CAdminMessage, SendAdminMessage);
        Bind(Packets.ClientPackets.CPlayerMove, PlayerMove);
        Bind(Packets.ClientPackets.CStopPlayerMove, StopPlayerMove);
        Bind(Packets.ClientPackets.CPlayerDir, PlayerDirection);
        Bind(Packets.ClientPackets.CUseItem, UseItem);
        Bind(Packets.ClientPackets.CAttack, Attack);
        Bind(Packets.ClientPackets.CMouseAttack, MouseAttack);
        Bind(Packets.ClientPackets.CWarpMeTo, WarpMeTo);
        Bind(Packets.ClientPackets.CWarpToMe, WarpToMe);
        Bind(Packets.ClientPackets.CWarpTo, WarpTo);
        Bind(Packets.ClientPackets.CSetSprite, SetSprite);
        Bind(Packets.ClientPackets.CRequestNewMap, RequestNewMap);
        Bind(Packets.ClientPackets.CSaveMap, MapData);
        Bind(Packets.ClientPackets.CNeedMap, NeedMap);
        Bind(Packets.ClientPackets.CMapGetItem, GetItem);
        Bind(Packets.ClientPackets.CMapDropItem, DropItem);
        Bind(Packets.ClientPackets.CMapRespawn, RespawnMap);
        Bind(Packets.ClientPackets.CMapReport, MapReport);
        Bind(Packets.ClientPackets.CKickPlayer, KickPlayer);
        Bind(Packets.ClientPackets.CBanList, Banlist);
        Bind(Packets.ClientPackets.CBanDestroy, DestroyBans);
        Bind(Packets.ClientPackets.CBanPlayer, BanPlayer);
        Bind(Packets.ClientPackets.CRequestEditMap, RequestEditMap);

        Bind(Packets.ClientPackets.CSetAccess, SetAccess);
        Bind(Packets.ClientPackets.CWhosOnline, WhosOnline);
        Bind(Packets.ClientPackets.CSetMotd, SetMotd);
        Bind(Packets.ClientPackets.CSearch, PlayerSearch);
        Bind(Packets.ClientPackets.CSkills, Skills);
        Bind(Packets.ClientPackets.CCast, Cast);
        Bind(Packets.ClientPackets.CSwapInvSlots, SwapInvSlots);
        Bind(Packets.ClientPackets.CSwapSkillSlots, SwapSkillSlots);

        Bind(Packets.ClientPackets.CCheckPing, CheckPing);
        Bind(Packets.ClientPackets.CUnEquip, UnEquip);
        Bind(Packets.ClientPackets.CRequestPlayerData, RequestPlayerData);
        Bind(Packets.ClientPackets.CRequestItem, RequestItem);
        Bind(Packets.ClientPackets.CRequestNpc, RequestNpc);
        Bind(Packets.ClientPackets.CRequestResource, RequestResource);
        Bind(Packets.ClientPackets.CSpawnItem, SpawnItem);
        Bind(Packets.ClientPackets.CTrainStat, TrainStat);

        Bind(Packets.ClientPackets.CRequestAnimation, RequestAnimation);
        Bind(Packets.ClientPackets.CRequestSkill, RequestSkill);
        Bind(Packets.ClientPackets.CRequestShop, RequestShop);
        Bind(Packets.ClientPackets.CRequestLevelUp, RequestLevelUp);
        Bind(Packets.ClientPackets.CForgetSkill, ForgetSkill);
        Bind(Packets.ClientPackets.CCloseShop, CloseShop);
        Bind(Packets.ClientPackets.CBuyItem, BuyItem);
        Bind(Packets.ClientPackets.CSellItem, SellItem);
        Bind(Packets.ClientPackets.CChangeBankSlots, ChangeBankSlots);
        Bind(Packets.ClientPackets.CDepositItem, DepositItem);
        Bind(Packets.ClientPackets.CWithdrawItem, WithdrawItem);
        Bind(Packets.ClientPackets.CCloseBank, CloseBank);
        Bind(Packets.ClientPackets.CAdminWarp, AdminWarp);

        Bind(Packets.ClientPackets.CTradeInvite, TradeInvite);
        Bind(Packets.ClientPackets.CHandleTradeInvite, HandleTradeInvite);
        Bind(Packets.ClientPackets.CAcceptTrade, AcceptTrade);
        Bind(Packets.ClientPackets.CDeclineTrade, DeclineTrade);
        Bind(Packets.ClientPackets.CTradeItem, TradeItem);
        Bind(Packets.ClientPackets.CUntradeItem, UntradeItem);

        Bind(Packets.ClientPackets.CAdmin, Admin);

        Bind(Packets.ClientPackets.CSetHotbarSlot, SetHotbarSlot);
        Bind(Packets.ClientPackets.CDeleteHotbarSlot, DeleteHotbarSlot);
        Bind(Packets.ClientPackets.CUseHotbarSlot, UseHotbarSlot);

        Bind(Packets.ClientPackets.CSkillLearn, SkillLearn);

        Bind(Packets.ClientPackets.CEventChatReply, EventChatReply);
        Bind(Packets.ClientPackets.CEvent, Event);
        Bind(Packets.ClientPackets.CRequestSwitchesAndVariables, RequestSwitchesAndVariables);
        Bind(Packets.ClientPackets.CSwitchesAndVariables, SwitchesAndVariables);

        Bind(Packets.ClientPackets.CRequestProjectile, RequestProjectile);
        Bind(Packets.ClientPackets.CClearProjectile, ClearProjectile);

        Bind(Packets.ClientPackets.CEmote, Emote);

        Bind(Packets.ClientPackets.CRequestParty, PartyRquest);
        Bind(Packets.ClientPackets.CAcceptParty, AcceptParty);
        Bind(Packets.ClientPackets.CDeclineParty, DeclineParty);
        Bind(Packets.ClientPackets.CLeaveParty, LeaveParty);
        Bind(Packets.ClientPackets.CPartyChatMsg, PartyChatMsg);
        Bind(Packets.ClientPackets.CRequestEditItem, RequestEditItem);
        Bind(Packets.ClientPackets.CSaveItem, SaveItem);
        Bind(Packets.ClientPackets.CRequestEditNpc, RequestEditNpc);
        Bind(Packets.ClientPackets.CSaveNpc, SaveNpc);
        Bind(Packets.ClientPackets.CRequestEditShop, RequestEditShop);
        Bind(Packets.ClientPackets.CSaveShop, SaveShop);
        Bind(Packets.ClientPackets.CRequestEditSkill, RequestEditSkill);
        Bind(Packets.ClientPackets.CSaveSkill, SaveSkill);
        Bind(Packets.ClientPackets.CRequestEditResource, RequestEditResource);
        Bind(Packets.ClientPackets.CSaveResource, SaveResource);
        Bind(Packets.ClientPackets.CRequestEditAnimation, RequestEditAnimation);
        Bind(Packets.ClientPackets.CSaveAnimation, SaveAnimation);
        Bind(Packets.ClientPackets.CRequestEditProjectile, RequestEditProjectile);
        Bind(Packets.ClientPackets.CSaveProjectile, SaveProjectile);
        Bind(Packets.ClientPackets.CRequestEditJob, RequestEditJob);
        Bind(Packets.ClientPackets.CSaveJob, SaveJob);

        Bind(Packets.ClientPackets.CRequestMoral, RequestMoral);
        Bind(Packets.ClientPackets.CRequestEditMoral, RequestEditMoral);
        Bind(Packets.ClientPackets.CSaveMoral, SaveMoral);

        Bind(Packets.ClientPackets.CRequestEditScript, RequestEditScript);
        Bind(Packets.ClientPackets.CSaveScript, SaveScript);

        Bind(Packets.ClientPackets.CCloseEditor, CloseEditor);
        Bind(Packets.ClientPackets.CCancelCast, CancelCast);
        Bind(Packets.ClientPackets.CRespawnNow, RespawnNow);
    }

    private static ValueTask Ping(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        return ValueTask.CompletedTask;
    }

    private static async ValueTask Login(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var reader = new PacketReader(bytes);

        if (NetworkConfig.IsPlaying(session.Id))
        {
            Network.AlertMessage(session, SystemMessage.Connection, Menu.Login);
            return;
        }

        if (NetworkConfig.IsLoggedIn(session.Id))
        {
            return;
        }

        if (General.GetShutDownTimer != null && General.GetShutDownTimer.IsRunning)
        {
            Network.AlertMessage(session, SystemMessage.ServerMaintenance, Menu.Login);
            return;
        }

        var login = System.Text.Encoding.UTF8.GetString(session.Decrypt(reader.ReadBytes().ToArray())).ToLower().Replace("\0", "");
        var password = System.Text.Encoding.UTF8.GetString(session.Decrypt(reader.ReadBytes().ToArray())).Replace("\0", "");

        // Read the remaining payload before any await; PacketReader cannot cross await boundaries.
        var clientVersion = System.Text.Encoding.UTF8.GetString(session.Decrypt(reader.ReadBytes().ToArray()));

        // If this account is already connected, take over the session by disconnecting the old one.
        // This prevents "stuck logged in" states from blocking re-login.
        var existingPlayerId = NetworkConfig.FindConnectedPlayerIdByLogin(login);

        if (existingPlayerId == -1)
        {
            foreach (PlayerBase player in PlayerBase.Instance)
            {
                if (player is null || player.Name is null)
                    continue;

                if (player.Name.Equals(login, StringComparison.CurrentCultureIgnoreCase))
                {
                    existingPlayerId = PlayerBase.Instance.IndexOf(player);
                    break;
                }
            }
        }

        if (existingPlayerId > 0 && existingPlayerId != session.Id)
        {
            General.Logger.LogInformation(
                "Login takeover for {AccountName}: disconnecting existing session {ExistingId} in favor of {NewId}",
                login,
                existingPlayerId,
                session.Id);

            // Best-effort notify then disconnect.
            await Server.Player.OnExit(existingPlayerId);
        }

        // Get the current executing assembly
        var assembly = Assembly.GetExecutingAssembly();

        // Retrieve the version information
        var serverVersion = assembly.GetName().Version?.ToString();

        // Check versions
        if (clientVersion != serverVersion)
        {
            Network.AlertMessage(session, SystemMessage.ClientOutdated, Menu.Login);
            return;
        }

        if (login.Length > Core.Globals.Variables.NameLength | login.Length < Core.Globals.Variables.MinimumNameLength)
        {
            Network.AlertMessage(session, SystemMessage.NameLengthInvalid);
            return;
        }

        if (NetworkConfig.IsMultiLogin(session.Id, login))
        {
            Network.AlertMessage(session, SystemMessage.MultipleAccountsNotAllowed, Menu.Login);
            return;
        }

        Account.EnsureSize(session.Id + 1);
        Account.Instance[session.Id].Login = login;
        await Account.OnLoadAsync(session.Id, new CancellationToken());

        if (Account.Instance[session.Id].Login != login)
        {
            Network.AlertMessage(session, SystemMessage.Login, Menu.Login);
            return;
        }
    
        if (GetPlayerPassword(session.Id) != password)
        {
            Network.AlertMessage(session, SystemMessage.WrongPassword, Menu.Login);
            return;
        }

        if (Database.IsBanned(session.Id, session.Channel.IpAddress))
        {
            Network.AlertMessage(session, SystemMessage.Banned, Menu.Login);
            return;
        }

        if (GetAccountLogin(session.Id) == "")
        {
            Network.AlertMessage(session, SystemMessage.DatabaseError, Menu.Login);
            return;
        }

        General.Logger.LogInformation("{AccountName} has logged in from {IpAddress}",
            GetAccountLogin(session.Id), session.Channel.IpAddress);

        PlayerService.Instance.OnAdd(session.Id, session.Channel);
        Network.Variables(session);
        Network.PlayerCharacters(session);
        Network.Jobs(session);
    }

    private static async ValueTask Register(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        if (NetworkConfig.IsPlaying(session.Id) ||
            NetworkConfig.IsLoggedIn(session.Id))
        {
            return;
        }

        Account.EnsureSize(session.Id + 1);

        // Check if its banned
        // Cut off last portion of ip
        if (Database.IsBanned(session.Id, session.Channel.IpAddress))
        {
            Network.AlertMessage(session, SystemMessage.Banned, Menu.Register);
            return;
        }

        if (General.GetShutDownTimer is { IsRunning: true })
        {
            Network.AlertMessage(session, SystemMessage.ServerMaintenance, Menu.Register);
            return;
        }

        var login = System.Text.Encoding.UTF8.GetString(session.Decrypt(buffer.ReadBytes().ToArray())).ToLower().Replace("\0", "");

        var password = System.Text.Encoding.UTF8.GetString(session.Decrypt(buffer.ReadBytes().ToArray())).Replace("\0", "");
        // Get the current executing assembly
        var assembly = Assembly.GetExecutingAssembly();

        // Retrieve the version information
        var serverVersion = assembly.GetName().Version?.ToString();
        var clientVersion = System.Text.Encoding.UTF8.GetString(session.Decrypt(buffer.ReadBytes().ToArray()));

        // Check versions
        if (clientVersion != serverVersion)
        {
            Network.AlertMessage(session, SystemMessage.ClientOutdated, Menu.Register);
            return;
        }

        var x = General.IsValidLogin(login);

        switch (x) // Check if the username is valid
        {
            case -1:
                Network.AlertMessage(session, SystemMessage.NameContainsIllegalCharacters, Menu.Register);
                return;

            case 0:
                Network.AlertMessage(session, SystemMessage.NameLengthInvalid, Menu.Register);
                return;
        }

        if (NetworkConfig.IsMultiLogin(session.Id, login))
        {
            Network.AlertMessage(session, SystemMessage.MultipleAccountsNotAllowed, Menu.Register);
            return;
        }

        var userData = Database.SelectRowByColumn("id", Database.GetStringHash(login), "account", "data");
        if (userData is not null)
        {
            Network.AlertMessage(session, SystemMessage.NameTaken, Menu.Register);
            return;
        }
    
        Account.Instance[session.Id].Login = login;
        Account.Instance[session.Id].Password = password;

        await Account.OnSave(session.Id).ConfigureAwait(false);

        // send them to the character portal
        Network.PlayerCharacters(session);
        Network.Jobs(session);
    }

    private static async ValueTask AddChar(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        string name;
        byte slot;
        int sex;
        int job;
        int sprite;
        var buffer = new PacketReader(bytes);

        if (!NetworkConfig.IsPlaying(session.Id))
        {
            slot = buffer.ReadByte();
            name = buffer.ReadString();
            sex = buffer.ReadInt32();
            job = buffer.ReadInt32();

            if (slot < 0 || slot >= Core.Globals.Variables.MaxCharacters)
            {
                Network.AlertMessage(session, SystemMessage.MaxCharactersReached, Menu.CharacterSelect);
                return;
            }

            Data.TempPlayer[session.Id].Slot = slot;

            var x = General.IsValidLogin(name);

            // Check if the username is valid
            if (x == -1)
            {
                Network.AlertMessage(session, SystemMessage.NameContainsIllegalCharacters, Menu.Login);
                return;
            }
            else if (x == 0)
            {
                Network.AlertMessage(session, SystemMessage.NameLengthInvalid, Menu.Login);
                return;
            }

            // Check if name is already in use
            if (Database.CharacterList?.Contains(name) == true)
            {
                Network.AlertMessage(session, SystemMessage.NameTaken, Menu.Login);
                return;
            }

            if (sex < (byte)Sex.Male | sex > (byte)Sex.Female)
                return;

            if (job < 0 | job > Core.Globals.Variables.MaxJobs)
                return;

            if (sex == (byte)Sex.Male)
            {
                sprite = Job.Instance[job].MaleSprite;
            }
            else
            {
                sprite = Job.Instance[job].FemaleSprite;
            }

            if (sprite == 0)
            {
                sprite = 1;
            }

            // Everything went ok, add the character
            Database.CharacterList?.Add(name);
            await Database.AddChar(session.Id, slot, name, (byte)sex, (byte)job, sprite).ConfigureAwait(false);

            PlayerBase.Instance[session.Id] = Account.Instance[session.Id].Player[slot];

            Log.Add("Character " + name + " added to " + GetAccountLogin(session.Id) + "'s account.", Constant.PlayerLog);
            Server.Player.OnAdd(session);
        }
    }

    private static async ValueTask UseChar(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var reader = new PacketReader(bytes);

        if (!NetworkConfig.IsPlaying(session.Id))
        {
            if (NetworkConfig.IsLoggedIn(session.Id))
            {
                var slot = reader.ReadByte();
                if (slot < 0 || slot >= Core.Globals.Variables.MaxCharacters)
                {
                    Network.AlertMessage(session, SystemMessage.MaxCharactersReached, Menu.CharacterSelect);
                    return;
                }

                PlayerBase.EnsureSize(session.Id + 1);
                PlayerBase.Instance[session.Id] = Account.Instance[session.Id].Player[slot];
                Server.Player.OnAdd(session);
            }
            else
            {
                Network.AlertMessage(session, SystemMessage.Connection, Menu.Login);
            }
        }
        else
        {
            Network.AlertMessage(session, SystemMessage.Connection, Menu.Login);
        }
    }

    private static async ValueTask DelChar(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        if (!NetworkConfig.IsPlaying(session.Id))
        {
            var slot = buffer.ReadByte();
            if (slot < 0 || slot >= Core.Globals.Variables.MaxCharacters)
            {
                Network.AlertMessage(session, SystemMessage.MaxCharactersReached, Menu.CharacterSelect);
                return;
            }

            Database.CharacterList?.Remove(Account.Instance[session.Id].Player[slot].Name);
            Account.Instance[session.Id].Player[slot] = new Server.Player();
            await Account.OnSave(session.Id);

            // send them to the character portal
            Network.PlayerCharacters(session);
        }
        else
        {
            Network.AlertMessage(session, SystemMessage.Connection, Menu.Login);
        }
    }

    private static async ValueTask Logout(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (!NetworkConfig.IsPlaying(session.Id))
        {
            return;
        }

        Network.LeftGame(session.Id);

        await Server.Player.OnExit(session.Id).ConfigureAwait(false);
    }

    private static async ValueTask RespawnNow(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var playerId = session.Id;
        
        // No payload.
        if (!NetworkConfig.IsPlaying(session.Id))
        {
            return;
        }

        // Only allow during the respawn window.
        if (!Server.Player.Instance[playerId].Dead)
        {
            return;
        }

        var now = (int)General.GetTime();
        var expiry = Server.Player.Instance[playerId].DeathTimer;
        if (expiry <= 0 || expiry <= now)
        {
            // Timer already expired; normal loop/queued task will handle it.
            return;
        }

        // Respawn immediately.
        try
        {
            Script.Instance?.OnDeath(playerId);
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "Error handling early respawn for playerId={PlayerId}", playerId);
        }
    }

    private static async ValueTask SayMessage(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);
        var msg = buffer.ReadString();

        if (PlayerBase.Instance[session.Id].Dead)
        {
            Network.PlayerMessage(session.Id, "You are dead and cannot chat.", (int)ColorName.BrightRed);
            return;
        }

        Log.Add("Map #" + GetMap(session.Id) + ": " + GetName(session.Id) + " says, '" + msg + "'", Constant.PlayerLog);

        Network.SayMessage_Map(GetMap(session.Id), session.Id, msg, (int)ColorName.White);
        Network.ChatBubble(GetMap(session.Id), session.Id, (int)TargetType.Player, msg, (int)ColorName.White);
    }

    private static async ValueTask BroadCastMsg(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);
        var msg = buffer.ReadString();

        if (PlayerBase.Instance[session.Id].Dead)
        {
            Network.PlayerMessage(session.Id, "You are dead and cannot chat.", (int)ColorName.BrightRed);
            return;
        }

        var s = "[Global] " + GetName(session.Id) + ": " + msg;
        Network.SayMessage_Global(session.Id, msg, (int)ColorName.White);
        Log.Add(s, Constant.PlayerLog);
        Console.WriteLine(s);
    }

    public static async ValueTask PlayerMsg(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);
        var name = buffer.ReadString();
        var msg = buffer.ReadString();

        if (PlayerBase.Instance[session.Id].Dead)
        {
            Network.PlayerMessage(session.Id, "You are dead and cannot chat.", (int)ColorName.BrightRed);
            return;
        }

        var otherPlayerId = GameLogic.FindPlayer(name);
        if (otherPlayerId != session.Id)
        {
            if (otherPlayerId >= 0)
            {
                Log.Add(GetName(session.Id) + " tells " + GetName(otherPlayerId) + ", '" + msg + "'", Constant.PlayerLog);
                Network.PlayerMessage(otherPlayerId, GetName(session.Id) + " tells you, '" + msg + "'", (int)ColorName.Pink);
                Network.PlayerMessage(session.Id, "You tell " + GetName(otherPlayerId) + ", '" + msg + "'", (int)ColorName.Pink);
            }
            else
            {
                Network.PlayerMessage(session.Id, "Player is not online.", (int)ColorName.BrightRed);
            }
        }
        else
        {
            Network.PlayerMessage(session.Id, "Cannot message your self!", (int)ColorName.BrightRed);
        }
    }

    private static async ValueTask SendAdminMessage(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var s = default(string);
        var buffer = new PacketReader(bytes);
        var msg = buffer.ReadString();

        if (PlayerBase.Instance[session.Id].Dead)
        {
            Network.PlayerMessage(session.Id, "You are dead and cannot chat.", (int)ColorName.BrightRed);
            return;
        }

        Network.AdminMessage(msg);
        Log.Add(s ?? string.Empty, Constant.PlayerLog);
        Console.WriteLine(s);
    }

    private static async ValueTask PlayerMove(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        if (Data.TempPlayer[session.Id].GettingMap)
            return;

        var dir = buffer.ReadByte();
        var movement = buffer.ReadByte();
        var tmpX = buffer.ReadInt32();
        var tmpY = buffer.ReadInt32();

        // Prevent invalid movement states (client can send arbitrary bytes)
        var movementCount = Enum.GetValues(typeof(MovementState)).Length;
        if (movement >= movementCount)
        {
            return;
        }

        SetDir(session.Id, dir);

        // Always apply requested movement so the server loop can start stepping.
        // If the client is slightly out of sync, we correct them without broadcasting to the whole map.
        PlayerBase.Instance[session.Id].Moving = movement;

        if (tmpX != GetRawX(session.Id) || tmpY != GetRawY(session.Id))
        {
            // Desync detected, correct client
            Network.PlayerXY(session.Id);
        }

        // Requirement: moving cancels the buffered cast only if the skill disallows moving while casting.
        if (Data.TempPlayer[session.Id].SkillBuffer >= 0)
        {
            var skillSlot = (int)Data.TempPlayer[session.Id].SkillBuffer;
            var shouldCancel = false;

            if (skillSlot >= 0 && PlayerBase.Instance[session.Id].Skill != null && skillSlot < PlayerBase.Instance[session.Id].Skill.Length)
            {
                var skillId = PlayerBase.Instance[session.Id].Skill[skillSlot].Num;
                if (skillId >= 0 && skillId < Skill.Instance.Count)
                {
                    shouldCancel = !Skill.Instance[skillId].MoveCast;
                }
            }

            if (shouldCancel)
            {
                Data.TempPlayer[session.Id].SkillBuffer = -1;
                Data.TempPlayer[session.Id].SkillBufferTimer = 0;
                Network.ClearSkillBuffer(session.Id);
            }
        }
    }

    private static async ValueTask CancelCast(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Client intends to cancel current cast/buffer (e.g., Escape). Server is authoritative.
        if (Data.TempPlayer[session.Id].SkillBuffer >= 0)
        {
            Data.TempPlayer[session.Id].SkillBuffer = -1;
            Data.TempPlayer[session.Id].SkillBufferTimer = 0;
            Network.ClearSkillBuffer(session.Id);
        }
    }

    public static async ValueTask StopPlayerMove(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        if (Data.TempPlayer[session.Id].GettingMap)
            return;

        PlayerBase.Instance[session.Id].IsMoving = false;
        PlayerBase.Instance[session.Id].Moving = 0;

        // Broadcast final resting position & flags immediately
        Network.PlayerXYToMap(session.Id);
    }

    public static async ValueTask PlayerDirection(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        if (Data.TempPlayer[session.Id].GettingMap == true)
            return;

        var dir = buffer.ReadByte();

        // Prevent hacking: accept full 8-direction enum range
        int dirCount = Enum.GetNames(typeof(Direction)).Length;
        if (dir < 0 | dir > dirCount)
            return;

        SetDir(session.Id, dir);

        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ServerPackets.SPlayerDir);
        packetWriter.WriteInt32(session.Id);
        packetWriter.WriteByte(GetDir(session.Id));

        NetworkConfig.SendDataToMapBut(session.Id, GetMap(session.Id), packetWriter.GetBytes());
    }

    public static async ValueTask UseItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var inv = buffer.ReadInt32();

        Server.Player.UseItem(session.Id, inv);
    }

    public static async ValueTask Attack(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var x = 0;
        var y = 0;

        // can't attack whilst casting
        if (Data.TempPlayer[session.Id].SkillBuffer >= 0)
            return;

        // can't attack whilst stunned
        if (Data.TempPlayer[session.Id].StunDuration > 0)
            return;

        Network.PlayerAttack(session.Id);

        // Durability gate: don't allow attacking with a broken weapon.
        var weapon = GetPaperdoll(session.Id, Equipment.Weapon);
        if (weapon >= 0
            && weapon < Item.Instance.Count
            && Item.Instance[weapon].MaxDurability > 0)
        {
            var paperdoll = PlayerBase.Instance[session.Id].Paperdoll;
            var weaponSlot = (int)Equipment.Weapon;
            if (paperdoll is not null
                && weaponSlot >= 0
                && weaponSlot < paperdoll.Length
                && paperdoll[weaponSlot].Durability <= 0)
            {
                Network.PlayerMessage(session.Id, "This item is broken! Please fix it at a broker!", (int)ColorName.BrightRed);
                return;
            }
        }

        // Projectile check
        if (GetPaperdoll(session.Id, Equipment.Weapon) >= 0)
        {
            if (Item.Instance[GetPaperdoll(session.Id, Equipment.Weapon)].Projectile >= 0) // Item has a projectile
            {
                if (Item.Instance[GetPaperdoll(session.Id, Equipment.Weapon)].Ammo >= 0)
                {
                    if (Server.Player.HasItem(session.Id, Item.Instance[GetPaperdoll(session.Id, Equipment.Weapon)].Ammo) > 0)
                    {
                        Server.Player.TakeInv(session.Id, Item.Instance[GetPaperdoll(session.Id, Equipment.Weapon)].Ammo, 1);
                        Projectile.OnShoot(session.Id, -1, GetPaperdoll(session.Id, Equipment.Weapon));
                        return;
                    }
                    else
                    {
                        Network.PlayerMessage(session.Id, "Out of " + Item.Instance[Item.Instance[GetPaperdoll(session.Id, Equipment.Weapon)].Ammo].Name + "!", (int)ColorName.BrightRed);
                        return;
                    }
                }
                else
                {
                    Projectile.OnShoot(session.Id, -1, GetPaperdoll(session.Id, Equipment.Weapon));
                    return;
                }
            }
        }

        // Check tradeskills
        switch (GetDir(session.Id))
        {
            case (byte)Direction.Up:
                {
                    if (GetY(session.Id) == 0)
                        return;
                    x = GetX(session.Id);
                    y = GetY(session.Id) - 1;
                    break;
                }
            case (byte)Direction.Down:
                {
                    if (GetY(session.Id) == Server.Map.Instance[GetMap(session.Id)].MaxY)
                        return;
                    x = GetX(session.Id);
                    y = GetY(session.Id) + 1;
                    break;
                }
            case (byte)Direction.Left:
                {
                    if (GetX(session.Id) == 0)
                        return;
                    x = GetX(session.Id) - 1;
                    y = GetY(session.Id);
                    break;
                }
            case (byte)Direction.Right:
                {
                    if (GetX(session.Id) == Server.Map.Instance[GetMap(session.Id)].MaxX)
                        return;
                    x = GetX(session.Id) + 1;
                    y = GetY(session.Id);
                    break;
                }

            case (byte)Direction.UpRight:
                {
                    if (GetX(session.Id) == Server.Map.Instance[GetMap(session.Id)].MaxX)
                        return;
                    if (GetY(session.Id) == 0)
                        return;
                    x = GetX(session.Id) + 1;
                    y = GetY(session.Id) - 1;
                    break;
                }

            case (byte)Direction.UpLeft:
                {
                    if (GetX(session.Id) == 0)
                        return;
                    if (GetY(session.Id) == 0)
                        return;
                    x = GetX(session.Id) - 1;
                    y = GetY(session.Id) - 1;
                    break;
                }

            case (byte)Direction.DownRight:
                {
                    if (GetX(session.Id) == Server.Map.Instance[GetMap(session.Id)].MaxX)
                        return;
                    if (GetY(session.Id) == Server.Map.Instance[GetMap(session.Id)].MaxY)
                        return;
                    x = GetX(session.Id) + 1;
                    y = GetY(session.Id) + 1;
                    break;
                }

            case (byte)Direction.DownLeft:
                {
                    if (GetX(session.Id) == 0)
                        return;
                    if (GetY(session.Id) == Server.Map.Instance[GetMap(session.Id)].MaxY)
                        return;
                    x = GetX(session.Id) - 1;
                    y = GetY(session.Id) + 1;
                    break;
                }
        }

        MapResource.OnUpdate(session.Id, x, y);

        // New combat system integration: attempt a melee attack on the entity (player or npc)
        // occupying the targeted tile (x,y). Legacy code only triggered animation + resource checks.                                  
        var map = GetMap(session.Id);

        // Build attacker entity snapshot
        var attackerEntity = Core.Globals.Entity.FromPlayer(session.Id, PlayerBase.Instance[session.Id]);
        attackerEntity.Map = map;

        Core.Globals.Entity? targetEntity = null;

        // 1. Prefer npc target at tile (x,y)
        for (int i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
        {
            var npc = MapNpc.Instance[map, i];
            if (npc.Num < 0) continue;
            int npcTileX = npc.X / Constants.TileSize;
            int npcTileY = npc.Y / Constants.TileSize;
            if (npcTileX == x && npcTileY == y)
            {
                targetEntity = Core.Globals.Entity.FromNpc(i, npc);
                targetEntity.Map = map;
                break;
            }
        }

        // 2. If no NPC, look for player (PvP) on that tile (excluding self)
        if (targetEntity == null)
        {
            foreach (var p in PlayerService.Instance.Players)
            {
                if (p.Id == session.Id) continue;
                if (!NetworkConfig.IsPlaying(p.Id)) continue;
                if (GetMap(p.Id) != map) continue;
                if (GetX(p.Id) == x && GetY(p.Id) == y)
                {
                    targetEntity = Core.Globals.Entity.FromPlayer(p.Id, PlayerBase.Instance[p.Id]);
                    targetEntity.Map = map;
                    break;
                }
            }
        }

        // 3. Execute combat attempt if a valid target was found.
        try
        {
            Script.Instance?.AttemptAttack(attackerEntity, targetEntity);
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "[Script] Error in {MethodName}", "AttemptAttack");
        }
    }

    public static async ValueTask MouseAttack(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // read target world pixel coordinates relative to map origin
        int targetX = buffer.ReadInt32();
        int targetY = buffer.ReadInt32();

        // basic validation
        if (!NetworkConfig.IsPlaying(session.Id)) return;
        if (Data.TempPlayer[session.Id].SkillBuffer >= 0) return;
        if (Data.TempPlayer[session.Id].StunDuration > 0) return;

        // Ensure player holds a weapon with projectile or skill casting projectile
        int item = GetPaperdoll(session.Id, Equipment.Weapon);
        if (item < 0 || Item.Instance[item].Projectile < 0)
        {
            // fallback: trigger normal attack if no projectile
            await Attack(session, ReadOnlyMemory<byte>.Empty).ConfigureAwait(false);
            return;
        }

        // Check ammo availability first (do not deduct yet)
        int ammoId = Item.Instance[item].Ammo;
        if (ammoId >= 0 && Server.Player.HasItem(session.Id, ammoId) <= 0)
        {
            Network.PlayerMessage(session.Id, "Out of " + Item.Instance[ammoId].Name + "!", (int)ColorName.BrightRed);
            return;
        }

        // Cooldown gate using weapon attack speed to prevent spamming
        var attackerEntity = Core.Globals.Entity.FromPlayer(session.Id, PlayerBase.Instance[session.Id]);
        attackerEntity.Map = GetMap(session.Id);
        try
        {
            if (Script.Instance?.TryConsumeAttackCooldown(attackerEntity) != true)
            {
                return; // still on cooldown; ignore
            }
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "[Script] Error in {MethodName}", "TryConsumeAttackCooldown");
        }

        // Deduct ammo now that the shot is confirmed
        if (ammoId >= 0)
        {
            Server.Player.TakeInv(session.Id, ammoId, 1);
        }

        // Compute vector from player center to target in pixels
        int startX = GetRawX(session.Id);
        int startY = GetRawY(session.Id);
        int dx = targetX - startX;
        int dy = targetY - startY;
        if (dx == 0 && dy == 0)
        {
            // if zero vector, default to current facing
            Projectile.OnShoot(session.Id, -1, item);
            return;
        }

        // Normalize to fixed-point 1000 scale
        double length = Math.Sqrt((double)dx * dx + (double)dy * dy);
        short vx = (short)Math.Clamp((int)Math.Round(dx / length * 1000.0), short.MinValue, short.MaxValue);
        short vy = (short)Math.Clamp((int)Math.Round(dy / length * 1000.0), short.MinValue, short.MaxValue);

        // Fire with free-aim using helper and stop at target
        Server.Projectile.OnFreeAim(session.Id, vx, vy, item, targetX, targetY);
        Network.PlayerAttack(session.Id);
    }

    public static async ValueTask WarpMeTo(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Mapper)
            return;

        // The player
        var n = GameLogic.FindPlayer(buffer.ReadString());

        if (n != session.Id)
        {
            if (n >= 0)
            {
                Server.Player.OnWarp(session.Id, GetMap(n), GetX(n), GetY(n), (byte)Direction.Down);
                Network.PlayerMessage(n, GetName(session.Id) + " has warped to you.", (int)ColorName.Yellow);
                Network.PlayerMessage(session.Id, "You have been warped to " + GetName(n) + ".", (int)ColorName.Yellow);
                Log.Add(GetName(session.Id) + " has warped to " + GetName(n) + ", map #" + GetMap(n) + ".", Constant.AdminLog);
            }
            else
            {
                Network.PlayerMessage(session.Id, "Player is not online.", (int)ColorName.BrightRed);
            }
        }
        else
        {
            Network.PlayerMessage(session.Id, "You cannot warp to yourself, dumbass!", (int)ColorName.BrightRed);
        }
    }

    public static async ValueTask WarpToMe(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Mapper)
            return;

        // The player
        var n = GameLogic.FindPlayer(buffer.ReadString());


        if (n != session.Id)
        {
            if (n >= 0)
            {
                Server.Player.OnWarp(n, GetMap(session.Id), GetX(session.Id), GetY(session.Id), (byte)Direction.Down);
                Network.PlayerMessage(n, "You have been summoned by " + GetName(session.Id) + ".", (int)ColorName.Yellow);
                Network.PlayerMessage(session.Id, GetName(n) + " has been summoned.", (int)ColorName.Yellow);
                Log.Add(GetName(session.Id) + " has warped " + GetName(n) + " to self, map #" + GetMap(session.Id) + ".", Constant.AdminLog);
            }
            else
            {
                Network.PlayerMessage(session.Id, "Player is not online.", (int)ColorName.BrightRed);
            }
        }
        else
        {
            Network.PlayerMessage(session.Id, "You cannot warp yourself to yourself, dumbass!", (int)ColorName.BrightRed);
        }
    }

    public static async ValueTask WarpTo(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Mapper)
            return;

        // The map
        var n = buffer.ReadInt32();

        // Prevent hacking
        if (n < 0 | n > Core.Globals.Variables.MaxMaps)
            return;

        Server.Player.OnWarp(session.Id, n, GetX(session.Id), GetY(session.Id), (byte)Direction.Down);
        Network.PlayerMessage(session.Id, "You have been warped to map #" + n, (int)ColorName.Yellow);
        Log.Add(GetName(session.Id) + " warped to map #" + n + ".", Constant.AdminLog);
    }

    public static async ValueTask SetSprite(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Mapper)
            return;

        // The sprite
        var n = buffer.ReadInt32();

        Commands.SetSprite(session.Id, n);
        Network.PlayerData(session.Id);
    }

    public static async ValueTask RequestNewMap(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);
        var dir = buffer.ReadInt32();

        Server.Player.OnMove(session.Id, dir, 1, true);
    }

    public static async ValueTask MapData(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        int x;
        int y;

        if (GetAccess(session.Id) < (byte)Access.Mapper)
        {
            return;
        }

        var map = GetMap(session.Id);

        var ii = Server.Map.Instance[map].Revision + 1;
        Map.OnClear(map);

        var packetReader = new PacketReader(bytes);

        Server.Map.Instance[map].Name = packetReader.ReadString();
        Server.Map.Instance[map].Music = packetReader.ReadString();
        Server.Map.Instance[map].Revision = ii;
        Server.Map.Instance[map].Moral = (byte)packetReader.ReadInt32();
        Server.Map.Instance[map].Tileset = packetReader.ReadInt32();
        Server.Map.Instance[map].Up = packetReader.ReadInt32();
        Server.Map.Instance[map].Down = packetReader.ReadInt32();
        Server.Map.Instance[map].Left = packetReader.ReadInt32();
        Server.Map.Instance[map].Right = packetReader.ReadInt32();
        Server.Map.Instance[map].BootMap = packetReader.ReadInt32();
        Server.Map.Instance[map].BootX = (byte)packetReader.ReadInt32();
        Server.Map.Instance[map].BootY = (byte)packetReader.ReadInt32();
        Server.Map.Instance[map].MaxX = (byte)packetReader.ReadInt32();
        Server.Map.Instance[map].MaxY = (byte)packetReader.ReadInt32();
        Server.Map.Instance[map].Weather = (byte)packetReader.ReadInt32();
        Server.Map.Instance[map].Fog = packetReader.ReadInt32();
        Server.Map.Instance[map].WeatherIntensity = packetReader.ReadInt32();
        Server.Map.Instance[map].FogOpacity = (byte)packetReader.ReadInt32();
        Server.Map.Instance[map].FogSpeed = (byte)packetReader.ReadInt32();
        Server.Map.Instance[map].MapTint = packetReader.ReadBoolean();
        Server.Map.Instance[map].MapTintR = (byte)packetReader.ReadInt32();
        Server.Map.Instance[map].MapTintG = (byte)packetReader.ReadInt32();
        Server.Map.Instance[map].MapTintB = (byte)packetReader.ReadInt32();
        Server.Map.Instance[map].MapTintA = (byte)packetReader.ReadInt32();
        Server.Map.Instance[map].Panorama = packetReader.ReadByte();
        Server.Map.Instance[map].Parallax = packetReader.ReadByte();
        Server.Map.Instance[map].Brightness = packetReader.ReadByte();
        Server.Map.Instance[map].NoRespawn = packetReader.ReadBoolean();
        Server.Map.Instance[map].Indoors = packetReader.ReadBoolean();
        Server.Map.Instance[map].Shop = packetReader.ReadInt32();

        // Per-map camera zoom bounds
        Server.Map.Instance[map].MinZoom = packetReader.ReadSingle();
        Server.Map.Instance[map].MaxZoom = packetReader.ReadSingle();

        Server.Map.Instance[map].Tile = new Type.Tile[Server.Map.Instance[map].MaxX, Server.Map.Instance[map].MaxY];

        for (x = 0; x < Core.Globals.Variables.MaxMapNpcs; x++)
        {
            MapNpc.OnClear(x, map);
            Server.Map.Instance[map].Npc[x] = packetReader.ReadInt32();
        }

        var instance = Server.Map.Instance[map];
        var count = (int)instance.MaxX;
        for (x = 0; x < count; x++)
        {
            var count2 = (int)instance.MaxY;
            for (y = 0; y < count2; y++)
            {
                instance.Tile[x, y].Data1 = packetReader.ReadInt32();
                instance.Tile[x, y].Data2 = packetReader.ReadInt32();
                instance.Tile[x, y].Data3 = packetReader.ReadInt32();
                instance.Tile[x, y].Data1_2 = packetReader.ReadInt32();
                instance.Tile[x, y].Data2_2 = packetReader.ReadInt32();
                instance.Tile[x, y].Data3_2 = packetReader.ReadInt32();
                instance.Tile[x, y].DirBlock = (byte)packetReader.ReadInt32();
                var count3 = Enum.GetValues(typeof(MapLayer)).Length;
                instance.Tile[x, y].Layer = new Type.Layer[count3];
                for (var i = 0; i < (int)count3; i++)
                {
                    instance.Tile[x, y].Layer[i].Tileset = packetReader.ReadInt32();
                    instance.Tile[x, y].Layer[i].X = packetReader.ReadInt32();
                    instance.Tile[x, y].Layer[i].Y = packetReader.ReadInt32();
                    instance.Tile[x, y].Layer[i].AutoTile = (byte)packetReader.ReadInt32();
                }

                instance.Tile[x, y].Type = (TileType)packetReader.ReadInt32();
                instance.Tile[x, y].Type2 = (TileType)packetReader.ReadInt32();
            }
        }

        Server.Map.Instance[map].EventCount = packetReader.ReadInt32();

        if (Server.Map.Instance[map].EventCount > 0)
        {
            Server.Map.Instance[map].Event = new Type.Event[Server.Map.Instance[map].EventCount];
            var count4 = Server.Map.Instance[map].EventCount;
            for (var i = 0; i < count4; i++)
            {
                {
                    ref var instance1 = ref Server.Map.Instance[map].Event[i];
                    instance1.Name = packetReader.ReadString();
                    instance1.Globals = packetReader.ReadByte();
                    instance1.X = packetReader.ReadInt32();
                    instance1.Y = packetReader.ReadInt32();
                    instance1.PageCount = packetReader.ReadInt32();
                }

                if (Server.Map.Instance[map].Event[i].PageCount > 0)
                {
                    Server.Map.Instance[map].Event[i].Pages = new Type.EventPage[Server.Map.Instance[map].Event[i].PageCount];
                    Array.Resize(ref Data.TempPlayer[i].EventMap.EventPages, Server.Map.Instance[map].Event[i].PageCount);

                    var count5 = Server.Map.Instance[map].Event[i].PageCount;
                    for (x = 0; x < (int)count5; x++)
                    {
                        {
                            ref var instance2 = ref Server.Map.Instance[map].Event[i].Pages[x];
                            instance2.ChkVariable = packetReader.ReadInt32();
                            instance2.VariableIndex = packetReader.ReadInt32();
                            instance2.VariableCondition = packetReader.ReadInt32();
                            instance2.VariableCompare = packetReader.ReadInt32();

                            instance2.ChkSwitch = packetReader.ReadInt32();
                            instance2.SwitchIndex = packetReader.ReadInt32();
                            instance2.SwitchCompare = packetReader.ReadInt32();

                            instance2.ChkHasItem = packetReader.ReadInt32();
                            instance2.HasItemIndex = packetReader.ReadInt32();
                            instance2.HasItemAmount = packetReader.ReadInt32();

                            instance2.ChkSelfSwitch = packetReader.ReadInt32();
                            instance2.SelfSwitchIndex = packetReader.ReadInt32();
                            instance2.SelfSwitchCompare = packetReader.ReadInt32();

                            instance2.GraphicType = packetReader.ReadByte();
                            instance2.Graphic = packetReader.ReadInt32();
                            instance2.GraphicX = packetReader.ReadInt32();
                            instance2.GraphicY = packetReader.ReadInt32();
                            instance2.GraphicX2 = packetReader.ReadInt32();
                            instance2.GraphicY2 = packetReader.ReadInt32();

                            instance2.MoveType = packetReader.ReadByte();
                            instance2.MoveSpeed = packetReader.ReadByte();
                            instance2.MoveFreq = packetReader.ReadByte();
                            instance2.MoveRouteCount = packetReader.ReadInt32();
                            instance2.IgnoreMoveRoute = packetReader.ReadInt32();
                            instance2.RepeatMoveRoute = packetReader.ReadInt32();

                            if (instance2.MoveRouteCount > 0)
                            {
                                Server.Map.Instance[map].Event[i].Pages[x].MoveRoute = new Type.MoveRoute[instance2.MoveRouteCount];
                                var count6 = instance2.MoveRouteCount;
                                for (y = 0; y < (int)count6; y++)
                                {
                                    instance2.MoveRoute[y].Index = packetReader.ReadInt32();
                                    instance2.MoveRoute[y].Data1 = packetReader.ReadInt32();
                                    instance2.MoveRoute[y].Data2 = packetReader.ReadInt32();
                                    instance2.MoveRoute[y].Data3 = packetReader.ReadInt32();
                                    instance2.MoveRoute[y].Data4 = packetReader.ReadInt32();
                                    instance2.MoveRoute[y].Data5 = packetReader.ReadInt32();
                                    instance2.MoveRoute[y].Data6 = packetReader.ReadInt32();
                                }
                            }

                            instance2.IdleAnim = packetReader.ReadByte();
                            instance2.DirFix = packetReader.ReadByte();
                            instance2.WalkThrough = packetReader.ReadInt32();
                            instance2.ShowName = packetReader.ReadInt32();
                            instance2.Trigger = packetReader.ReadByte();
                            instance2.CommandListCount = packetReader.ReadInt32();
                            instance2.Position = packetReader.ReadByte();
                        }

                        if (Server.Map.Instance[map].Event[i].Pages[x].CommandListCount > 0)
                        {
                            Server.Map.Instance[map].Event[i].Pages[x].CommandList = new Type.CommandList[Server.Map.Instance[map].Event[i].Pages[x].CommandListCount];
                            var count7 = Server.Map.Instance[map].Event[i].Pages[x].CommandListCount;
                            for (y = 0; y < (int)count7; y++)
                            {
                                Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].CommandCount = packetReader.ReadInt32();
                                Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].ParentList = packetReader.ReadInt32();
                                if (Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].CommandCount > 0)
                                {
                                    Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].Commands = new Type.EventCommand[Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].CommandCount];
                                    for (int z = 0, count8 = Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].CommandCount; z < (int)count8; z++)
                                    {             
                                        ref var instance3 = ref Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].Commands[z];
                                        instance3.Index = packetReader.ReadInt32();
                                        instance3.Text1 = packetReader.ReadString();
                                        instance3.Text2 = packetReader.ReadString();
                                        instance3.Text3 = packetReader.ReadString();
                                        instance3.Text4 = packetReader.ReadString();
                                        instance3.Text5 = packetReader.ReadString();
                                        instance3.Data1 = packetReader.ReadInt32();
                                        instance3.Data2 = packetReader.ReadInt32();
                                        instance3.Data3 = packetReader.ReadInt32();
                                        instance3.Data4 = packetReader.ReadInt32();
                                        instance3.Data5 = packetReader.ReadInt32();
                                        instance3.Data6 = packetReader.ReadInt32();
                                        instance3.ConditionalBranch.CommandList = packetReader.ReadInt32();
                                        instance3.ConditionalBranch.Condition = packetReader.ReadInt32();
                                        instance3.ConditionalBranch.Data1 = packetReader.ReadInt32();
                                        instance3.ConditionalBranch.Data2 = packetReader.ReadInt32();
                                        instance3.ConditionalBranch.Data3 = packetReader.ReadInt32();
                                        instance3.ConditionalBranch.ElseCommandList = packetReader.ReadInt32();
                                        instance3.MoveRouteCount = packetReader.ReadInt32();
                                        var tmpCount = instance3.MoveRouteCount;
                                        if (tmpCount > 0)
                                        {
                                            Array.Resize(ref instance3.MoveRoute, tmpCount);
                                            for (int w = 0, count9 = tmpCount; w < (int)count9; w++)
                                            {
                                                instance3.MoveRoute[w].Index = packetReader.ReadInt32();
                                                instance3.MoveRoute[w].Data1 = packetReader.ReadInt32();
                                                instance3.MoveRoute[w].Data2 = packetReader.ReadInt32();
                                                instance3.MoveRoute[w].Data3 = packetReader.ReadInt32();
                                                instance3.MoveRoute[w].Data4 = packetReader.ReadInt32();
                                                instance3.MoveRoute[w].Data5 = packetReader.ReadInt32();
                                                instance3.MoveRoute[w].Data6 = packetReader.ReadInt32();
                                            }
                                        }
                                    }
                                
                                }
                            }
                        }
                    }
                }
            }
        }

        var count13 = Server.Map.Instance[map].EventCount;
        for (var i = 0; i < count13; i++)
        {
            if (Server.Map.Instance[map].Event[i].PageCount == 0)
            {
                Server.Map.Instance[map].Event[i] = Server.Map.Instance[map].Event[i + 1];
                Server.Map.Instance[map].Event[i + 1] = default;
                Server.Map.Instance[map].EventCount = Server.Map.Instance[map].EventCount - 1;
            }
        }

        // Save the map
        Map.OnSave(map);
        await MapNpc.OnSpawn(map).ConfigureAwait(false);
        await EventLogic.SpawnGlobalEvents(map).ConfigureAwait(false);

        foreach (var i in PlayerService.Instance.PlayerIds)
        {
            if (NetworkConfig.IsPlaying(i))
            {
                if (PlayerBase.Instance[i].Map == map)
                {
                    EventLogic.SpawnMapEventsFor(i, map);
                }
            }
        }

        // Clear it all out
        var count11 = Core.Globals.Variables.MaxMapItems;
        for (var i = 0; i < count11; i++)
        {
            MapItem.OnClear(i, GetMap(session.Id));
        }

        // Respawn
        MapItem.Spawn(GetMap(session.Id));
        MapResource.OnUpdate(map);

        // Refresh map for everyone online
        foreach (var i in PlayerService.Instance.PlayerIds)
        {
            if (NetworkConfig.IsPlaying(i) & GetMap(i) == map)
            {
                Server.Player.OnWarp(i, map, GetX(i), GetY(i), (byte)Direction.Down);
                Network.MapData(i, map, true);
            }
        }
    }

    private static async ValueTask NeedMap(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Get yes/no value
        var s = buffer.ReadInt32();

        // Check if data is needed to be sent
        if (s == 1)
        {
            Network.MapData(session.Id, GetMap(session.Id), true);
        }
        else
        {
            Network.MapData(session.Id, GetMap(session.Id), false);
        }

        if (Server.Map.Instance[GetMap(session.Id)].Shop >= 0 && Server.Map.Instance[GetMap(session.Id)].Shop < Core.Globals.Variables.MaxShops)
        {
            var shop = Server.Map.Instance[GetMap(session.Id)].Shop;
            if (shop >= 0 && shop < Shop.Instance.Count && !string.IsNullOrEmpty(Shop.Instance[shop].Name))
            {
                Data.TempPlayer[session.Id].InShop = shop;
                Network.OpenShop(session.Id, (int)Data.TempPlayer[session.Id].InShop);
            }
        }

        Network.JoinMap(session.Id);

        // Ensure the joining client receives current NPC death timers (corpse countdowns).
        Network.MapNpcsToPlayer(session.Id, GetMap(session.Id));

        Data.TempPlayer[session.Id].GettingMap = false;
    }

    public static async ValueTask RespawnMap(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        int i;
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Mapper)
            return;

        // Clear out it all
        var count = Core.Globals.Variables.MaxMapItems;
        for (i = 0; i < count; i++)
        {
            MapItem.OnClear(i, GetMap(session.Id));
        }

        // Respawn
        MapItem.Spawn(GetMap(session.Id));

        // Respawn NpcS
        var count2 = Core.Globals.Variables.MaxMapNpcs;
        for (i = 0; i < count2; i++)
            MapNpc.OnSpawn(i, GetMap(session.Id));

        EventLogic.SpawnMapEventsFor(session.Id, GetMap(session.Id));

        MapResource.OnUpdate(GetMap(session.Id));
        Network.PlayerMessage(session.Id, "Map respawned.", (int)ColorName.BrightGreen);
        Log.Add(GetName(session.Id) + " has respawned map #" + GetMap(session.Id), Constant.AdminLog);
    }

    public static async ValueTask MapReport(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Mapper)
            return;

        Network.MapReport(session.Id);
    }

    public static async ValueTask KickPlayer(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Moderator)
        {
            return;
        }

        // The player session.Id
        var n = GameLogic.FindPlayer(buffer.ReadString());

        if (n != session.Id)
        {
            if (n >= 0)
            {
                if (GetAccess(n) < GetAccess(session.Id))
                {
                    Network.GlobalMessage(GetName(n) + " has been kicked from " + SettingsManager.Instance.GameName + " by " + GetName(session.Id) + "!");
                    Log.Add(GetName(session.Id) + " has kicked " + GetName(n) + ".", Constant.AdminLog);
                    Network.AlertMessage(session, SystemMessage.Kicked, Menu.Login);
                }
                else
                {
                    Network.PlayerMessage(session.Id, "That is a higher or same access admin then you!", (int)ColorName.BrightRed);
                }
            }
            else
            {
                Network.PlayerMessage(session.Id, "Player is not online.", (int)ColorName.BrightRed);
            }
        }
        else
        {
            Network.PlayerMessage(session.Id, "You cannot kick yourself!", (int)ColorName.BrightRed);
        }
    }

    public static async ValueTask Banlist(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Moderator)
        {
            return;
        }

        Network.PlayerMessage(session.Id, "Command /banlist is not available.", (int)ColorName.Yellow);
    }

    public static async ValueTask DestroyBans(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Owner)
            return;

        var filename = System.IO.Path.Combine(DataPath.Database, "banlist.txt");

        if (File.Exists(filename))
            File.Delete(filename);

        Network.PlayerMessage(session.Id, "Ban list destroyed.", (int)ColorName.BrightGreen);
    }

    public static async ValueTask BanPlayer(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Moderator)
            return;

        // The player session.Id
        var n = GameLogic.FindPlayer(buffer.ReadString());

        if (n != session.Id)
        {
            if (n >= 0)
            {
                if (GetAccess(n) < GetAccess(session.Id))
                {
                    Database.BanPlayer(n, session.Id);
                }
                else
                {
                    Network.PlayerMessage(session.Id, "That is a higher or same access admin then you!", (int)ColorName.BrightRed);
                }
            }
            else
            {
                Network.PlayerMessage(session.Id, "Player is not online.", (int)ColorName.BrightRed);
            }
        }
        else
        {
            Network.PlayerMessage(session.Id, "You cannot ban yourself!", (int)ColorName.BrightRed);
        }
    }

    private static async ValueTask RequestEditMap(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Mapper)
        {
            Network.PlayerMessage(session.Id, "Invalid access level.", (int)ColorName.BrightRed);
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Map);

        if (!string.IsNullOrEmpty(user))
        {
            Network.PlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Network.Npcs(session.Id);
        Network.Items(session.Id);
        Network.Animations(session.Id);
        Network.Shops(session.Id);
        Network.Resources(session.Id);
        Network.MapEventData(session.Id);
        Network.Morals(session.Id);

        Data.TempPlayer[session.Id].Editor = EditorType.Map;

        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ServerPackets.SEditMap);

        PlayerService.Instance.SendDataTo(session.Id, packetWriter.GetBytes());
    }

    public static async ValueTask RequestEditShop(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Developer)
        {
            Network.PlayerMessage(session.Id, "Invalid access level.", (int)ColorName.BrightRed);
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Shop);

        if (!string.IsNullOrEmpty(user))
        {
            Network.PlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Shop;

        Network.Items(session.Id);
        Network.Shops(session.Id);

        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ServerPackets.SShopEditor);

        PlayerService.Instance.SendDataTo(session.Id, packetWriter.GetBytes());
    }

    public static async ValueTask SaveShop(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Developer)
            return;

        var shop = buffer.ReadInt32();

        // Prevent hacking
        if (shop < 0 | shop > Core.Globals.Variables.MaxShops)
            return;

        Shop.Instance[shop].BuyRate = buffer.ReadInt32();
        Shop.Instance[shop].Name = buffer.ReadString();

        for (int i = 0, count = Core.Globals.Variables.MaxTrades; i < count; i++)
        {
            Shop.Instance[shop].TradeItem[i].CostItem = buffer.ReadInt32();
            Shop.Instance[shop].TradeItem[i].CostValue = buffer.ReadInt32();
            Shop.Instance[shop].TradeItem[i].Item = buffer.ReadInt32();
            Shop.Instance[shop].TradeItem[i].ItemValue = buffer.ReadInt32();
        }


        // Save it
        Network.UpdateShopToAll(shop);
        Shop.OnSave(shop);
        Log.Add(GetAccountLogin(session.Id) + " saving shop #" + shop + ".", Constant.AdminLog);
    }

    public static async ValueTask RequestEditSkill(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Developer)
        {
            Network.PlayerMessage(session.Id, "Invalid access level.", (int)ColorName.BrightRed);
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Skill);

        if (!string.IsNullOrEmpty(user))
        {
            Network.PlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Skill;

        Network.Jobs(session);
        Network.Projectiles(session.Id);
        Network.Animations(session.Id);
        Network.Skills(session.Id);

        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ServerPackets.SSkillEditor);

        PlayerService.Instance.SendDataTo(session.Id, packetWriter.GetBytes());
    }

    public static async ValueTask SaveSkill(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var skill = buffer.ReadInt32();

        // Prevent hacking
        if (skill < 0 | skill > Core.Globals.Variables.MaxSkills)
            return;

        Skill.Instance[skill].AccessReq = buffer.ReadInt32();
        Skill.Instance[skill].AoE = buffer.ReadInt32();
        Skill.Instance[skill].CastAnim = buffer.ReadInt32();
        Skill.Instance[skill].CastTime = buffer.ReadInt32();
        Skill.Instance[skill].CdTime = buffer.ReadInt32();
        Skill.Instance[skill].JobReq = buffer.ReadInt32();
        Skill.Instance[skill].Dir = buffer.ReadByte();
        Skill.Instance[skill].Duration = buffer.ReadInt32();
        Skill.Instance[skill].Icon = buffer.ReadInt32();
        Skill.Instance[skill].Interval = buffer.ReadInt32();
        Skill.Instance[skill].IsAoE = buffer.ReadBoolean();
        Skill.Instance[skill].LevelReq = buffer.ReadInt32();
        Skill.Instance[skill].Map = buffer.ReadInt32();
        Skill.Instance[skill].MpCost = buffer.ReadInt32();
        Skill.Instance[skill].Name = buffer.ReadString();
        Skill.Instance[skill].Range = buffer.ReadInt32();
        Skill.Instance[skill].SkillAnim = buffer.ReadInt32();
        Skill.Instance[skill].StunDuration = buffer.ReadInt32();
        Skill.Instance[skill].Type = buffer.ReadByte();
        Skill.Instance[skill].Vital = buffer.ReadInt32();
        Skill.Instance[skill].X = buffer.ReadInt32();
        Skill.Instance[skill].Y = buffer.ReadInt32();

        // projectiles
        Skill.Instance[skill].IsProjectile = buffer.ReadInt32();
        Skill.Instance[skill].Projectile = buffer.ReadInt32();

        Skill.Instance[skill].KnockBack = buffer.ReadByte();
        Skill.Instance[skill].KnockBackTiles = buffer.ReadByte();
        Skill.Instance[skill].MultiDirMask = buffer.ReadInt32();
        
        // chain skills
        Skill.Instance[skill].ChainOnHitSkillId = buffer.ReadInt32();

        // common event fields
        Skill.Instance[skill].CommonEventType = buffer.ReadByte();
        Skill.Instance[skill].CommonEventData1 = buffer.ReadInt32();
        Skill.Instance[skill].CommonEventData2 = buffer.ReadInt32();

        Skill.Instance[skill].MoveSpeed = buffer.ReadSingle();

        // Optional trailing fields (backward compatible)
        Skill.Instance[skill].MoveCast = buffer.ReadBoolean();

        Skill.Instance[skill].SpCost = buffer.ReadInt32();

        Skill.Instance[skill].NextRank = buffer.ReadInt32();
        Skill.Instance[skill].NextUses = buffer.ReadInt32();
    

        // Save it
        Network.UpdateSkillToAll(skill);
        Skill.OnSave(skill);
        Log.Add(GetAccountLogin(session.Id) + " saved Skill #" + skill + ".", Constant.AdminLog);
    }

    public static async ValueTask SetAccess(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Owner)
            return;

        // The session.Id
        var n = GameLogic.FindPlayer(buffer.ReadString());

        // The access
        var i = buffer.ReadByte();

        // Check for invalid access level
        if (i >= (int)Access.Player && i <= (int)Access.Owner)
        {
            // Check if player is on
            if (n >= 0)
            {
                if (n != session.Id)
                {
                    // check to see if same level access is trying to change another access of the very same level and boot them if they are.
                    if (GetAccess(n) == GetAccess(session.Id))
                    {
                        Network.PlayerMessage(session.Id, "Invalid access level.", (int)ColorName.BrightRed);
                        return;
                    }
                }

                if (GetAccess(n) == (int)Access.Player && i > (int)Access.Player)
                {
                    Network.GlobalMessage(GetName(n) + " has been blessed with administrative access.");
                }

                Commands.SetAccess(n, (byte)i);
                Network.PlayerData(n);
                Log.Add(GetName(session.Id) + " has modified " + GetName(n) + "'s access.", Constant.AdminLog);
            }
            else
            {
                Network.PlayerMessage(session.Id, "Player is not online.", (int)ColorName.BrightRed);
            }
        }
        else
        {
            Network.PlayerMessage(session.Id, "Invalid access level.", (int)ColorName.BrightRed);
        }
    }

    public static async ValueTask WhosOnline(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Network.WhosOnline(session.Id);
    }

    public static async ValueTask SetMotd(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Mapper)
            return;

        Variables.Welcome = buffer.ReadString();
        SettingsManager.Save();

        Network.GlobalMessage("Welcome changed to: " + Variables.Welcome);
        Log.Add(GetName(session.Id) + " changed welcome to: " + Variables.Welcome, Constant.AdminLog);
    }

    public static async ValueTask PlayerSearch(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var x = buffer.ReadInt32();
        var y = buffer.ReadInt32();
        var rclick = (byte)buffer.ReadInt32();

        var mapId = GetMap(session.Id);
        if (mapId < 0 || mapId >= Server.Map.Instance.Count)
            return;

        var map = Server.Map.Instance[mapId];

        // Prevent subscript out of range
        if (x < 0 | x > (int)map.MaxX | y < 0 | y > (int)map.MaxY)
            return;

        // Check for a player   
        foreach (var i in PlayerService.Instance.PlayerIds)
        {
            if (GetMap(session.Id) == GetMap(i))
            {
                if (GetX(i) == x)
                {
                    if (GetY(i) == y)
                    {
                        // Consider the player
                        if (i != session.Id)
                        {
                            if (GetLevel(i) >= GetLevel(session.Id) + 5)
                            {
                                Network.PlayerMessage(session.Id, "You wouldn't stand a chance.", (int)ColorName.BrightRed);
                            }

                            else if (GetLevel(i) > GetLevel(session.Id))
                            {
                                Network.PlayerMessage(session.Id, "This one seems to have an advantage over you.", (int)ColorName.Yellow);
                            }

                            else if (GetLevel(i) == GetLevel(session.Id))
                            {
                                Network.PlayerMessage(session.Id, "This would be an even fight.", (int)ColorName.White);
                            }

                            else if (GetLevel(session.Id) >= GetLevel(i) + 5)
                            {
                                Network.PlayerMessage(session.Id, "You could slaughter that player.", (int)ColorName.BrightBlue);
                            }

                            else if (GetLevel(session.Id) > GetLevel(i))
                            {
                                Network.PlayerMessage(session.Id, "You would have an advantage over that player.", (int)ColorName.BrightCyan);
                            }
                        }

                        // Change target
                        if (Data.TempPlayer[session.Id].TargetType == 0 | i != Data.TempPlayer[session.Id].Target)
                        {
                            Data.TempPlayer[session.Id].Target = i;
                            Data.TempPlayer[session.Id].TargetType = (byte)TargetType.Player;
                        }
                        else
                        {
                            Data.TempPlayer[session.Id].Target = -1;
                            Data.TempPlayer[session.Id].TargetType = 0;
                        }

                        if (Data.TempPlayer[session.Id].Target >= 0)
                        {
                            Network.PlayerMessage(session.Id, "Your target is now " + GetName(i) + ".", (int)ColorName.Yellow);
                        }

                        Network.Target(session.Id, Data.TempPlayer[session.Id].Target, Data.TempPlayer[session.Id].TargetType);
                        if (rclick == 1)
                            Network.RightClick(session.Id);
                        return;
                    }
                }
            }
        }

        // Check for an item
        var count = Core.Globals.Variables.MaxMapItems;
        for (var i = 0; i < count; i++)
        {
            if (MapItem.Instance[mapId, i].Num >= 0)
            {
                if (!string.IsNullOrEmpty(Item.Instance[(int)MapItem.Instance[mapId, i].Num].Name))
                {
                    if (Math.Floor((double)MapItem.Instance[mapId, i].X / Constants.TileSize) == x)
                    {
                        if (Math.Floor((double)MapItem.Instance[mapId, i].Y / Constants.TileSize) == y)
                        {
                            Network.PlayerMessage(session.Id, "You see " + MapItem.Instance[mapId, i].Value + " " + Item.Instance[(int)MapItem.Instance[mapId, i].Num].Name + ".", (int)ColorName.BrightGreen);
                            return;
                        }
                    }
                }
            }
        }

        // Check for an npc
        var count2 = Core.Globals.Variables.MaxMapNpcs;
        for (var i = 0; i < count2; i++)
        {
            if (MapNpc.Instance[mapId, i].Num >= 0)
            {
                if (Math.Floor((double)MapNpc.Instance[mapId, i].X / Constants.TileSize) == x)
                {
                    if (Math.Floor((double)MapNpc.Instance[mapId, i].Y / Constants.TileSize) == y)
                    {
                        // Change target
                        if (Data.TempPlayer[session.Id].TargetType == 0)
                        {
                            Data.TempPlayer[session.Id].Target = i;
                            Data.TempPlayer[session.Id].TargetType = (byte)TargetType.Npc;
                        }
                        else
                        {
                            Data.TempPlayer[session.Id].Target = -1;
                            Data.TempPlayer[session.Id].TargetType = 0;
                        }

                        if (Data.TempPlayer[session.Id].Target >= 0)
                        {
                            Network.PlayerMessage(session.Id, "Your target is now " + GameLogic.CheckGrammar(Npc.Instance[(int)MapNpc.Instance[GetMap(session.Id), i].Num].Name) + ".", (int)ColorName.Yellow);
                        }

                        Network.Target(session.Id, Data.TempPlayer[session.Id].Target, Data.TempPlayer[session.Id].TargetType);
                        return;
                    }
                }
            }
        }
    }

    public static async ValueTask Skills(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Network.PlayerSkills(session.Id);
    }

    public static async ValueTask Cast(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Skill slot
        var n = buffer.ReadInt32();

        try
        {
            Script.Instance?.OnCast(session.Id, n);
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(Cast));
        }
    }

    public static async ValueTask SwapInvSlots(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        if (Data.TempPlayer[session.Id].InTrade > 0 | Data.TempPlayer[session.Id].InBank | Data.TempPlayer[session.Id].InShop >= 0)
            return;

        // Old Slot
        double oldSlot = buffer.ReadInt32();
        double newSlot = buffer.ReadInt32();

        Server.Player.PlayerSwitchInvSlots(session.Id, (int)oldSlot, (int)newSlot);
    }

    public static async ValueTask SwapSkillSlots(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        if (Data.TempPlayer[session.Id].InTrade > 0 | Data.TempPlayer[session.Id].InBank | Data.TempPlayer[session.Id].InShop >= 0)
            return;

        // Old Slot
        double oldSlot = buffer.ReadInt32();
        double newSlot = buffer.ReadInt32();


        Server.Player.PlayerSwitchSkillSlots(session.Id, (int)oldSlot, (int)newSlot);
    }

    public static async ValueTask CheckPing(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ServerPackets.SSendPing);

        PlayerService.Instance.SendDataTo(session.Id, packetWriter.GetBytes());
    }

    public static async ValueTask UnEquip(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);
        int eqSlot = buffer.ReadInt32();
        int m = Server.Player.FindOpenInvSlot(session.Id, (int)PlayerBase.Instance[session.Id].Paperdoll[eqSlot].Num);
        Server.Player.RemoveEquipment(session.Id, eqSlot, m);
    }

    public static async ValueTask RequestPlayerData(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Network.PlayerData(session.Id);
    }

    public static async ValueTask RequestNpc(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var n = buffer.ReadInt32();

        if (n < 0 | n > Core.Globals.Variables.MaxNpcs)
            return;

        Network.UpdateNpcTo(session.Id, n);
    }

    public static async ValueTask SpawnItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // item
        var tmpItem = buffer.ReadInt32();
        var tmpAmount = buffer.ReadInt32();

        if (GetAccess(session.Id) < (byte)Access.Developer)
            return;

        MapItem.OnSpawn(tmpItem, tmpAmount, GetMap(session.Id), GetX(session.Id), GetY(session.Id), Item.Instance[tmpItem].MaxDurability);
    }

    public static async ValueTask TrainStat(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // check points
        if (GetPoints(session.Id) == 0)
            return;

        // stat
        var tmpStat = buffer.ReadInt32();

        try
        {
            Script.Instance?.OnTrain(session.Id, tmpStat);
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(TrainStat));
        }
    }

    public static async ValueTask RequestSkill(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var n = buffer.ReadInt32();

        if (n < 0 | n > Core.Globals.Variables.MaxSkills)
            return;

        Network.UpdateSkillTo(session.Id, n);
    }

    public static async ValueTask RequestShop(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var n = buffer.ReadInt32();

        if (n < 0 | n > Core.Globals.Variables.MaxShops)
            return;

        Network.UpdateShopTo(session.Id, n);
    }

    public static async ValueTask RequestLevelUp(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Developer)
            return;

        SetExp(session.Id, Script.Instance?.GetPlayerNextLevel(session.Id));
        Server.Player.OnLevel(session.Id);
    }

    public static async ValueTask ForgetSkill(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var skillSlot = buffer.ReadInt32();

        // Check for subscript out of range
        if (skillSlot < 0 | skillSlot > Core.Globals.Variables.MaxPlayerSkills)
            return;

        // dont let them forget a skill which is in CD
        if (Data.TempPlayer[session.Id].SkillCd[skillSlot] > 0)
        {
            Network.PlayerMessage(session.Id, "Cannot forget a skill which is cooling down!", (int)ColorName.BrightRed);
            return;
        }

        // dont let them forget a skill which is buffered
        if (Data.TempPlayer[session.Id].SkillBuffer == skillSlot)
        {
            Network.PlayerMessage(session.Id, "Cannot forget a skill which you are casting!", (int)ColorName.BrightRed);
            return;
        }

        PlayerBase.Instance[session.Id].Skill[skillSlot].Num = -1;
        PlayerBase.Instance[session.Id].Skill[skillSlot].Uses = 0;
        Network.PlayerSkills(session.Id);
    }

    public static async ValueTask CloseShop(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Data.TempPlayer[session.Id].InShop = -1;
    }

    public static async ValueTask BuyItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var shopSlot = buffer.ReadInt32();

        // not in shop, exit out
        var shopMum = Data.TempPlayer[session.Id].InShop;

        if (shopMum < 0 | shopMum > Core.Globals.Variables.MaxShops)
            return;

        ref var instance = ref Shop.Instance[(int)shopMum].TradeItem[shopSlot];

        // check trade exists
        if (instance.Item < 0)
            return;

        // check has the cost item
        var itemAmount = Server.Player.HasItem(session.Id, instance.CostItem);
        if (itemAmount == 0 | itemAmount < instance.CostValue)
        {
            Network.PlayerMessage(session.Id, "You do not have enough to buy this item.", (int)ColorName.BrightRed);
            Network.ResetShopAction();
            return;
        }

        // it's fine, let's go ahead
        for (int i = 0, count = instance.CostValue; i < count; i++)
            Server.Player.TakeInv(session.Id, instance.CostItem, instance.CostValue);
        Server.Player.GiveInv(session.Id, instance.Item, instance.ItemValue);

        // send confirmation message & reset their shop action
        Network.PlayerMessage(session.Id, "Trade successful.", (int)ColorName.BrightGreen);
        Network.ResetShopAction();
    }

    public static async ValueTask SellItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var invSlot = buffer.ReadInt32();

        // if invalid, exit out
        if (invSlot < 0 || invSlot > Core.Globals.Variables.MaxInventory)
            return;

        // has item?
        if (GetInv(session.Id, invSlot) < 0 || GetInv(session.Id, invSlot) > Core.Globals.Variables.MaxItems)
            return;

        // seems to be valid
        double item = GetInv(session.Id, invSlot);
        var shop = Data.TempPlayer[session.Id].InShop;

        if (shop < 0 || shop > Core.Globals.Variables.MaxShops)
        {
            return;
        }

        // work out price
        var multiplier = Shop.Instance[(int)shop].BuyRate / 100d;
        var price = (int)Math.Round(Item.Instance[(int)item].Price * multiplier);

        // item has cost?
        if (price < 0)
        {
            Network.PlayerMessage(session.Id, "The shop doesn't want that item.", (int)ColorName.Yellow);
            Network.ResetShopAction();
            return;
        }

        // take item and give gold
        Server.Player.TakeInv(session.Id, (int)item, 1);
        Server.Player.GiveInv(session.Id, 0, price);

        // send confirmation message & reset their shop action
        Network.PlayerMessage(session.Id, "Sold the " + Item.Instance[(int)item].Name + " for " + price + " " + Item.Instance[(int)item].Name + "!", (int)ColorName.BrightGreen);
        Network.ResetShopAction();
    }

    public static async ValueTask ChangeBankSlots(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var oldslot = buffer.ReadInt32();
        var newslot = buffer.ReadInt32();

        Server.Player.PlayerSwitchBankSlots(session.Id, oldslot, newslot);
    }

    public static async ValueTask DepositItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var invslot = buffer.ReadInt32();
        var amount = buffer.ReadInt32();

        Server.Player.GiveBank(session.Id, invslot, amount);
    }

    public static async ValueTask WithdrawItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var bankSlot = buffer.ReadByte();
        var amount = buffer.ReadInt32();

        Server.Player.TakeBank(session.Id, bankSlot, amount);
    }

    public static async ValueTask CloseBank(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Data.TempPlayer[session.Id].InBank = false;
    }

    public static async ValueTask AdminWarp(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var x = buffer.ReadInt32();
        var y = buffer.ReadInt32();

        var map = GetMap(session.Id);
        if (map < 0 || map >= Server.Map.Instance.Count)
            return;

        if (x < 0 || x >= Server.Map.Instance[map].MaxX || y < 0 || y >= Server.Map.Instance[map].MaxY)
            return;

        x *= 32;
        y *= 32;

        if (GetAccess(session.Id) >= (byte)Access.Mapper)
        {
            PlayerBase.Instance[session.Id].IsMoving = false;

            // Set the information
            SetX(session.Id, x);
            SetY(session.Id, y);
            SetDir(session.Id, (byte)Direction.Down);
            Network.PlayerXYToMap(session.Id);
        }
    }

    public static async ValueTask TradeInvite(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var name = buffer.ReadString();

        // Check for a player
        var tradeTarget = GameLogic.FindPlayer(name);

        if (tradeTarget < 0 | tradeTarget >= Core.Globals.Variables.MaxPlayers)
            return;

        // can't trade with yourself..
        if (tradeTarget == session.Id)
        {
            Network.PlayerMessage(session.Id, "You can't trade with yourself!", (int)ColorName.BrightRed);
            return;
        }

        // send the trade request
        Data.TempPlayer[session.Id].TradeRequest = tradeTarget;
        Data.TempPlayer[tradeTarget].TradeRequest = session.Id;

        Network.PlayerMessage(tradeTarget, GetName(session.Id) + " has invited you to trade.", (int)ColorName.Yellow);
        Network.PlayerMessage(session.Id, "You have invited " + GetName(tradeTarget) + " to trade.", (int)ColorName.BrightGreen);

        Network.TradeInvite(tradeTarget, session.Id);
    }

    public static async ValueTask HandleTradeInvite(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var status = (byte)buffer.ReadInt32();

        var tradeTarget = Data.TempPlayer[session.Id].TradeRequest;

        if (tradeTarget < 0 | tradeTarget >= Core.Globals.Variables.MaxPlayers)
            return;

        if (status == 0)
        {
            Network.PlayerMessage(tradeTarget, GetName(session.Id) + " has declined your trade request.", (int)ColorName.BrightRed);
            Network.PlayerMessage(session.Id, "You have declined the trade with " + GetName(tradeTarget) + ".", (int)ColorName.BrightRed);
            Data.TempPlayer[session.Id].TradeRequest = -1;
            return;
        }

        // Let them tradetradeTarget
        if (Data.TempPlayer[tradeTarget].TradeRequest == session.Id)
        {
            // let them know they're trading
            Network.PlayerMessage(session.Id, "You have accepted " + GetName(tradeTarget) + "'s trade request.", (int)ColorName.Yellow);
            Network.PlayerMessage(tradeTarget, GetName(session.Id) + " has accepted your trade request.", (int)ColorName.BrightGreen);

            // clear the tradeRequest server-side
            Data.TempPlayer[session.Id].TradeRequest = -1;
            Data.TempPlayer[tradeTarget].TradeRequest = -1;

            // set that they're trading with each other
            Data.TempPlayer[session.Id].InTrade = tradeTarget;

            // clear out their trade offers
            Data.TempPlayer[tradeTarget].InTrade = session.Id;

            Array.Resize(ref Data.TempPlayer[session.Id].TradeOffer, Core.Globals.Variables.MaxInventory);
            Array.Resize(ref Data.TempPlayer[tradeTarget].TradeOffer, Core.Globals.Variables.MaxInventory);

            for (int i = 0, count = Core.Globals.Variables.MaxInventory; i < count; i++)
            {
                Data.TempPlayer[session.Id].TradeOffer[i].Num = -1;
                Data.TempPlayer[session.Id].TradeOffer[i].Value = 0;
                Data.TempPlayer[tradeTarget].TradeOffer[i].Num = -1;
                Data.TempPlayer[tradeTarget].TradeOffer[i].Value = 0;
            }

            // Used to init the trade window clientside
            Network.Trade(session.Id, tradeTarget);
            Network.Trade(tradeTarget, session.Id);

            // Send the offer data - Used to clear their client
            Network.TradeUpdate(session.Id, 0);
            Network.TradeUpdate(session.Id, 1);
            Network.TradeUpdate(tradeTarget, 0);
            Network.TradeUpdate(tradeTarget, 1);
        }
    }

    public static async ValueTask TradeInviteDecline(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Data.TempPlayer[session.Id].TradeRequest = -1;
    }

    public static async ValueTask AcceptTrade(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        int item;
        int i;
        var tmpTradeItem = new Type.Item[Core.Globals.Variables.MaxInventory];
        var tmpTradeItem2 = new Type.Item[Core.Globals.Variables.MaxInventory];

        Data.TempPlayer[session.Id].AcceptTrade = true;

        var tradeTarget = (int)Data.TempPlayer[session.Id].InTrade;

        // if not both of them accept, then exit
        if (!Data.TempPlayer[tradeTarget].AcceptTrade)
        {
            Network.TradeStatus(session.Id, 2);
            Network.TradeStatus(tradeTarget, 1);
            return;
        }

        // take their items
        var count = Core.Globals.Variables.MaxInventory;
        for (i = 0; i < count; i++)
        {
            tmpTradeItem[i].Num = -1;
            tmpTradeItem2[i].Num = -1;

            // player
            if (Data.TempPlayer[session.Id].TradeOffer[i].Num >= 0)
            {
                var offeredSlot = (int)Data.TempPlayer[session.Id].TradeOffer[i].Num;
                item = (int)PlayerBase.Instance[session.Id].Inventory[offeredSlot].Num;
                if (item >= 0)
                {
                    // store temp
                    tmpTradeItem[i].Num = item;
                    tmpTradeItem[i].Value = Data.TempPlayer[session.Id].TradeOffer[i].Value;
                    tmpTradeItem[i].Durability = PlayerBase.Instance[session.Id].Inventory[offeredSlot].Durability;
                    // take item
                    Server.Player.TakeInvSlot(session.Id, offeredSlot, tmpTradeItem[i].Value);
                }
            }

            // target
            if (Data.TempPlayer[tradeTarget].TradeOffer[i].Num >= 0)
            {
                var offeredSlot = (int)Data.TempPlayer[tradeTarget].TradeOffer[i].Num;
                item = GetInv(tradeTarget, offeredSlot);
                if (item >= 0)
                {
                    // store temp
                    tmpTradeItem2[i].Num = item;
                    tmpTradeItem2[i].Value = Data.TempPlayer[tradeTarget].TradeOffer[i].Value;
                    tmpTradeItem2[i].Durability = PlayerBase.Instance[tradeTarget].Inventory[offeredSlot].Durability;
                    // take item
                    Server.Player.TakeInvSlot(tradeTarget, offeredSlot, tmpTradeItem2[i].Value);
                }
            }
        }

        // taken all items. now they can't not get items because of no inventory space.
        var count2 = Core.Globals.Variables.MaxInventory;
        for (i = 0; i < count2; i++)
        {
            // player
            if (tmpTradeItem2[i].Num >= 0)
            {
                // give away!
                Server.Player.GiveInv(session.Id, (int)tmpTradeItem2[i].Num, tmpTradeItem2[i].Value, 0, false, tmpTradeItem2[i].Durability);
            }

            // target
            if (tmpTradeItem[i].Num >= 0)
            {
                // give away!
                Server.Player.GiveInv(tradeTarget, (int)tmpTradeItem[i].Num, tmpTradeItem[i].Value, 0, false, tmpTradeItem[i].Durability);
            }
        }

        Network.Inventory(session.Id);
        Network.Inventory(tradeTarget);

        // they now have all the items. Clear out values + let them out of the trade.
        var count3 = Core.Globals.Variables.MaxInventory;
        for (i = 0; i < count3; i++)
        {
            Data.TempPlayer[session.Id].TradeOffer[i].Num = -1;
            Data.TempPlayer[session.Id].TradeOffer[i].Value = 0;
            Data.TempPlayer[tradeTarget].TradeOffer[i].Num = -1;
            Data.TempPlayer[tradeTarget].TradeOffer[i].Value = 0;
        }

        Data.TempPlayer[session.Id].InTrade = 0;
        Data.TempPlayer[tradeTarget].InTrade = 0;

        Network.PlayerMessage(session.Id, "Trade completed.", (int)ColorName.BrightGreen);
        Network.PlayerMessage(tradeTarget, "Trade completed.", (int)ColorName.BrightGreen);

        Network.CloseTrade(session.Id);
        Network.CloseTrade(tradeTarget);
    }

    public static async ValueTask DeclineTrade(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var tradeTarget = (int)Data.TempPlayer[session.Id].InTrade;
        var hasValidTarget = tradeTarget >= 0 && tradeTarget < Core.Globals.Variables.MaxPlayers;

        for (int i = 0, count = Core.Globals.Variables.MaxInventory; i < count; i++)
        {
            Data.TempPlayer[session.Id].TradeOffer[i].Num = -1;
            Data.TempPlayer[session.Id].TradeOffer[i].Value = 0;

            if (hasValidTarget)
            {
                Data.TempPlayer[tradeTarget].TradeOffer[i].Num = -1;
                Data.TempPlayer[tradeTarget].TradeOffer[i].Value = 0;
            }
        }

        Data.TempPlayer[session.Id].InTrade = 0;
        Network.PlayerMessage(session.Id, "You declined the trade.", (int)ColorName.BrightRed);
        Network.CloseTrade(session.Id);

        if (hasValidTarget)
        {
            Data.TempPlayer[tradeTarget].InTrade = 0;
            Network.PlayerMessage(tradeTarget, GetName(session.Id) + " has declined the trade.", (int)ColorName.BrightRed);
            Network.CloseTrade(tradeTarget);
        }
    }

    public static async ValueTask TradeItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var emptyslot = default(int);
        int i;
        var buffer = new PacketReader(bytes);

        var invSlot = buffer.ReadInt32();
        var amount = buffer.ReadInt32();

        if (invSlot < 0 | invSlot > Core.Globals.Variables.MaxInventory)
            return;

        var item = GetInv(session.Id, invSlot);

        if (item < 0 || item > Core.Globals.Variables.MaxItems)
            return;

        // make sure they have the amount they offer
        if (amount < 0 || amount > GetInvValue(session.Id, invSlot))
            return;

        if (PlayerBase.Instance[session.Id].Inventory[invSlot].Bound > 0)
        {
            Network.PlayerMessage(session.Id, "You can't trade soulbound items.", (int)ColorName.BrightRed);
            return;
        }

        if (Item.Instance[item].Type == (byte)ItemCategory.Currency | Item.Instance[item].Stackable == 1)
        {
            // check if already offering same currency item
            var count = Core.Globals.Variables.MaxInventory;
            for (i = 0; i < count; i++)
            {
                if (Data.TempPlayer[session.Id].TradeOffer[i].Num == invSlot)
                {
                    // add amount
                    Data.TempPlayer[session.Id].TradeOffer[i].Value = Data.TempPlayer[session.Id].TradeOffer[i].Value + amount;

                    // clamp to limits
                    if (Data.TempPlayer[session.Id].TradeOffer[i].Value > GetInvValue(session.Id, invSlot))
                    {
                        Data.TempPlayer[session.Id].TradeOffer[i].Value = GetInvValue(session.Id, invSlot);
                    }

                    // cancel any trade agreement
                    Data.TempPlayer[session.Id].AcceptTrade = false;
                    Data.TempPlayer[(int)Data.TempPlayer[session.Id].InTrade].AcceptTrade = false;

                    Network.TradeStatus(session.Id, 0);
                    Network.TradeStatus((int)Data.TempPlayer[session.Id].InTrade, 1);

                    Network.TradeUpdate(session.Id, 0);
                    Network.TradeUpdate(session.Id, 1);
                    Network.TradeUpdate((int)Data.TempPlayer[session.Id].InTrade, 0);
                    Network.TradeUpdate((int)Data.TempPlayer[session.Id].InTrade, 1);
                    return;
                }
            }
        }
        else
        {
            // make sure they're not already offering it
            var count3 = Core.Globals.Variables.MaxInventory;
            for (i = 0; i < count3; i++)
            {
                if (Data.TempPlayer[session.Id].TradeOffer[i].Num == invSlot)
                {
                    Network.PlayerMessage(session.Id, "You've already offered this item.", (int)ColorName.BrightRed);
                    return;
                }
            }
        }

        // not already offering - find earliest empty slot
        var count2 = Core.Globals.Variables.MaxInventory;
        for (i = 0; i < count2; i++)
        {
            if (Data.TempPlayer[session.Id].TradeOffer[i].Num == -1)
            {
                emptyslot = i;
                break;
            }
        }

        Data.TempPlayer[session.Id].TradeOffer[emptyslot].Num = invSlot;
        Data.TempPlayer[session.Id].TradeOffer[emptyslot].Value = amount;

        // cancel any trade agreement and send new data
        Data.TempPlayer[session.Id].AcceptTrade = false;
        Data.TempPlayer[(int)Data.TempPlayer[session.Id].InTrade].AcceptTrade = false;

        Network.TradeStatus(session.Id, 0);
        Network.TradeStatus((int)Data.TempPlayer[session.Id].InTrade, 0);

        Network.TradeUpdate(session.Id, 0);
        Network.TradeUpdate(session.Id, 1);
        Network.TradeUpdate((int)Data.TempPlayer[session.Id].InTrade, 0);
        Network.TradeUpdate((int)Data.TempPlayer[session.Id].InTrade, 1);
    }

    public static async ValueTask UntradeItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var tradeslot = buffer.ReadInt32();


        if (tradeslot < 0 | tradeslot > Core.Globals.Variables.MaxInventory)
            return;

        if (Data.TempPlayer[session.Id].TradeOffer[tradeslot].Num < 0)
            return;

        Data.TempPlayer[session.Id].TradeOffer[tradeslot].Num = -1;
        Data.TempPlayer[session.Id].TradeOffer[tradeslot].Value = 0;

        if (Data.TempPlayer[session.Id].AcceptTrade)
            Data.TempPlayer[session.Id].AcceptTrade = false;
        if (Data.TempPlayer[(int)Data.TempPlayer[session.Id].InTrade].AcceptTrade)
            Data.TempPlayer[(int)Data.TempPlayer[session.Id].InTrade].AcceptTrade = false;

        Network.TradeStatus(session.Id, 0);
        Network.TradeStatus((int)Data.TempPlayer[session.Id].InTrade, 0);

        Network.TradeUpdate(session.Id, 0);
        Network.TradeUpdate((int)Data.TempPlayer[session.Id].InTrade, 1);
    }

    public static void HackingAttempt(int index, string reason)
    {
        if (index > 0 & NetworkConfig.IsPlaying(index))
        {
            Network.GlobalMessage(GetAccountLogin(index) + "/" + GetName(index) + " has been booted for (" + reason + ")");
            _ = Server.Player.OnExit(index).ContinueWith(
                t => General.Logger.LogError(t.Exception, "Unhandled error during forced logout"),
                TaskContinuationOptions.OnlyOnFaulted
            );
        }
    }

    public static async ValueTask Admin(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Moderator)
            return;

        Network.AdminPanel(session.Id);
    }

    public static async ValueTask SetHotbarSlot(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var @type = (byte)buffer.ReadInt32();
        var newSlot = buffer.ReadInt32();
        var oldSlot = buffer.ReadInt32();
        var skill = buffer.ReadInt32();

        var hotbar = PlayerBase.Instance[session.Id].Hotbar;
        if (newSlot < 0 || newSlot >= hotbar.Length || newSlot >= Core.Globals.Variables.MaxHotbar)
            return;

        if (type == (byte)PartOrigin.Hotbar)
        {
            if (oldSlot < 0 || oldSlot >= hotbar.Length || oldSlot >= Core.Globals.Variables.MaxHotbar)
                return;

            var oldItem = hotbar[oldSlot].Slot;
            var oldType = hotbar[oldSlot].SlotType;
            var newItem = hotbar[newSlot].Slot;
            var newType = hotbar[newSlot].SlotType;

            hotbar[newSlot].Slot = oldItem;
            hotbar[newSlot].SlotType = oldType;
            hotbar[oldSlot].Slot = newItem;
            hotbar[oldSlot].SlotType = newType;
        }
        else
        {
            hotbar[newSlot].Slot = skill;
            hotbar[newSlot].SlotType = type;
        }

        Network.Hotbar(session.Id);
    }

    public static async ValueTask DeleteHotbarSlot(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var slot = buffer.ReadInt32();

        var hotbar = PlayerBase.Instance[session.Id].Hotbar;
        if (slot < 0 || slot >= hotbar.Length || slot >= Core.Globals.Variables.MaxHotbar)
            return;

        hotbar[slot].Slot = -1;
        hotbar[slot].SlotType = 0;

        Network.Hotbar(session.Id);
    }

    public static async ValueTask UseHotbarSlot(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var slot = buffer.ReadInt32();

        var hotbar = PlayerBase.Instance[session.Id].Hotbar;
        if (slot < 0 || slot >= hotbar.Length || slot >= Core.Globals.Variables.MaxHotbar)
            return;

        if (hotbar[slot].Slot >= 0)
        {
            if (hotbar[slot].SlotType == (byte)DraggablePartType.Item)
            {
                int eqSlot = -1;
                var paperdoll = PlayerBase.Instance[session.Id].Paperdoll;
                for (int i = 0; i < paperdoll.Length; i++)
                {
                    if (paperdoll[i].Num == hotbar[slot].Slot)
                    {
                        eqSlot = i;
                        break;
                    }
                }

                if (eqSlot >= 0)
                {
                    int m = Server.Player.FindOpenInvSlot(session.Id, (int)paperdoll[eqSlot].Num);
                    if (m >= 0)
                    {
                        Server.Player.RemoveEquipment(session.Id, eqSlot, m);
                    }
                    else
                    {
                        var invSlot = Server.Player.FindItemSlot(session.Id, (int)hotbar[slot].Slot);
                        if (invSlot >= 0)
                        {
                            Server.Player.UseItem(session.Id, invSlot);
                        }
                    }
                }
                else
                {
                    var invSlot = Server.Player.FindItemSlot(session.Id, (int)hotbar[slot].Slot);
                    if (invSlot >= 0)
                    {
                        Server.Player.UseItem(session.Id, invSlot);
                    }
                }
            }
            else if (hotbar[slot].SlotType == (byte)DraggablePartType.Skill)
            {
                try
                {
                    Script.Instance?.OnCast(session.Id, hotbar[slot].Slot);
                }
                catch (Exception ex)
                {
                    General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(UseHotbarSlot));
                }
            }
        }

        Network.Hotbar(session.Id);
    }

    public static async ValueTask SkillLearn(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Developer)
            return;

        var skill = buffer.ReadInt32();

        try
        {
            Script.Instance?.OnLearn(session.Id, -1, skill);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static async ValueTask RequestEditJob(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Developer)
        {
            Network.PlayerMessage(session.Id, "Invalid access level.", (int)ColorName.BrightRed);
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Job);

        if (!string.IsNullOrEmpty(user))
        {
            Network.PlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Network.JobEditor(session.Id);

        Network.Items(session.Id);
        Network.Jobs(session);

        Data.TempPlayer[session.Id].Editor = EditorType.Job;        
    }

    public static async ValueTask SaveJob(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        int x;
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Developer)
            return;

        var index = buffer.ReadInt32();
    
        var instance = Job.Instance[index];
        instance.Name = buffer.ReadString();
        instance.Desc = buffer.ReadString();

        instance.MaleSprite = buffer.ReadInt32();
        instance.FemaleSprite = buffer.ReadInt32();

        var count = Enum.GetNames(typeof(Stat)).Length;
        for (x = 0; x < count; x++)
            instance.Stat[x] = buffer.ReadInt32();

        for (var q = 0; q < Core.Globals.Variables.MaxStartItems; q++)
        {
            instance.StartItem[q] = buffer.ReadInt32();
            instance.StartValue[q] = buffer.ReadInt32();
        }

        for (var q = 0; q < Core.Globals.Variables.MaxStartSkills; q++)
        {
            instance.StartSkill[q] = buffer.ReadInt32();
        }

        instance.StartMap = buffer.ReadInt32();
        instance.StartX = buffer.ReadByte();
        instance.StartY = buffer.ReadByte();
        instance.BaseExp = buffer.ReadInt32();
        instance.MoveSpeed = buffer.ReadSingle();
    
        Job.OnSave(index);
        Network.JobToAll(session.Id);
    }

    private static async ValueTask Emote(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var emote = buffer.ReadInt32();

        Network.Emote(session.Id, emote);
    }

    private static async ValueTask CloseEditor(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetAccess(session.Id) < (byte)Access.Mapper)
            return;

        if (Data.TempPlayer[session.Id].Editor == EditorType.None)
            return;

        Data.TempPlayer[session.Id].Editor = EditorType.None;
    }


    public static async ValueTask RequestEditMoral(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetAccess(session.Id) < (byte)Access.Developer)
        {
            Network.PlayerMessage(session.Id, "Invalid access level.", (int)ColorName.BrightRed);
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Moral);
        if (!string.IsNullOrEmpty(user))
        {
            Network.PlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Network.Morals(session.Id);

        Data.TempPlayer[session.Id].Editor = EditorType.Moral;

        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SMoralEditor);

        PlayerService.Instance.SendDataTo(session.Id, packet.GetBytes());
    }

    public static async ValueTask SaveMoral(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetAccess(session.Id) < (byte)Access.Developer)
        {
            return;
        }

        var index = packetReader.ReadInt32();
        if (index < 0 || index >= Core.Globals.Variables.MaxMorals)
        {
            return;
        }

        var moral = Moral.Instance[index];

        moral.Name = packetReader.ReadString();
        moral.Color = packetReader.ReadByte();
        moral.CanCast = packetReader.ReadBoolean();
        moral.CanPk = packetReader.ReadBoolean();
        moral.CanDropItem = packetReader.ReadBoolean();
        moral.CanPickupItem = packetReader.ReadBoolean();
        moral.CanUseItem = packetReader.ReadBoolean();
        moral.DropItems = packetReader.ReadBoolean();
        moral.LoseExp = packetReader.ReadBoolean();
        moral.PlayerBlock = packetReader.ReadBoolean();
        moral.NpcBlock = packetReader.ReadBoolean();

        Moral.OnSave(index);

        General.Logger.LogInformation("{AccountName} saved moral #{Moral}",
            GetAccountLogin(session.Id), index);

        Network.UpdateMoralToAll(index);
        Network.Morals(session.Id);
    }

    public static async ValueTask RequestMoral(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Network.Morals(session.Id);
    }

    public static async ValueTask RequestEditScript(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetAccess(session.Id) < (byte)Access.Owner)
        {
            Network.PlayerMessage(session.Id, "Invalid access level.", (int)ColorName.BrightRed);
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Script);
        if (!string.IsNullOrEmpty(user))
        {
            Network.PlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Script;

        var lines = Data.Script.Code ?? [];

        var packetReader = new PacketReader(bytes);

        var requestedChunk = packetReader.ReadInt32();
        var numberOfChunks = (int)Math.Ceiling((double)lines.Length / Script.MaxScriptLinesPerChunk);
        var offset = requestedChunk * Script.MaxScriptLinesPerChunk;
        var chunkLines = lines.Skip(offset).Take(Script.MaxScriptLinesPerChunk).ToArray();
        if (chunkLines.Length == 0)
        {
            return;
        }

        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(ServerPackets.SScriptEditor);
        packetWriter.WriteInt32(requestedChunk < numberOfChunks - 1 ? requestedChunk + 1 : -1);
        packetWriter.WriteInt32(offset);
        packetWriter.WriteInt32(lines.Length);
        packetWriter.WriteInt32(chunkLines.Length);

        foreach (var line in chunkLines)
        {
            packetWriter.WriteString(line);
        }

        session.Channel.Send(packetWriter.GetBytes());
    }

    public static async ValueTask SaveScript(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetAccess(session.Id) < (byte)Access.Owner)
        {
            return;
        }

        var path = DataPath.Database;
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        path = System.IO.Path.Combine(path, "Script.cs");

        var script = packetReader.ReadString();

        File.WriteAllText(path, script, Encoding.UTF8);

        _ = Script.OnLoad(session.Id);
    }

    public static async ValueTask RequestProjectile(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        var projectile = packetReader.ReadInt32();

        Network.UpdateProjectileTo(session.Id, projectile);
    }

    public static async ValueTask ClearProjectile(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        var projectile = packetReader.ReadInt32();
        _ = packetReader.ReadInt32(); // Target Index
        _ = (TargetType)packetReader.ReadInt32(); // Target TYpe
        _ = packetReader.ReadInt32(); // Target Zone

        var map = GetMap(session.Id);

        MapProjectile.OnClear(map, projectile);
    }

    public static async ValueTask RequestEditProjectile(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetAccess(session.Id) < (byte)Access.Developer)
        {
            Network.PlayerMessage(session.Id, "Invalid access level.", (int)ColorName.BrightRed);
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Projectile);
        if (!string.IsNullOrEmpty(user))
        {
            Network.PlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Projectile;

        var buffer = new PacketWriter(4);

        buffer.WriteEnum(ServerPackets.SProjectileEditor);

        PlayerService.Instance.SendDataTo(session.Id, buffer.GetBytes());

        Network.Projectiles(session.Id);
        Network.Animations(session.Id);
    }

    public static async ValueTask SaveProjectile(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetAccess(session.Id) < (byte)Access.Developer)
        {
            return;
        }

        var index = packetReader.ReadInt32();
        if (index < 0 || index > Core.Globals.Variables.MaxProjectiles)
        {
            return;
        }

        Projectile.Instance[index].Name = packetReader.ReadString();
        Projectile.Instance[index].Sprite = packetReader.ReadInt32();
        Projectile.Instance[index].Range = packetReader.ReadByte();
        Projectile.Instance[index].Speed = packetReader.ReadInt32();
        Projectile.Instance[index].Damage = packetReader.ReadInt32();
        Projectile.Instance[index].Animation = packetReader.ReadInt32();

        Projectile.OnSave(index);

        General.Logger.LogInformation("{AccountName} saved projectile #{Projectile}",
            GetAccountLogin(session.Id), index);

        Network.UpdateProjectileToAll(index);
    }

    public static async ValueTask RequestEditResource(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetAccess(session.Id) < (byte)Access.Developer)
        {
            Network.PlayerMessage(session.Id, "Invalid access level.", (int)ColorName.BrightRed);
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Resource);
        if (!string.IsNullOrEmpty(user))
        {
            Network.PlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Resource;

        Network.Items(session.Id);
        Network.Animations(session.Id);

        Network.Resources(session.Id);

        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SResourceEditor);

        PlayerService.Instance.SendDataTo(session.Id, packet.GetBytes());
    }

    public static async ValueTask SaveResource(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetAccess(session.Id) < (byte)Access.Developer)
        {
            return;
        }

        var index = packetReader.ReadInt32();
        if (index < 0 || index >= Core.Globals.Variables.MaxResources)
        {
            return;
        }

        Resource.Instance[index].Animation = packetReader.ReadInt32();
        Resource.Instance[index].EmptyMessage = packetReader.ReadString();
        Resource.Instance[index].ExhaustedImage = packetReader.ReadInt32();
        Resource.Instance[index].Health = packetReader.ReadInt32();
        Resource.Instance[index].ExperienceReward = packetReader.ReadInt32();
        Resource.Instance[index].ItemReward = packetReader.ReadInt32();
        Resource.Instance[index].Name = packetReader.ReadString();
        Resource.Instance[index].ResourceImage = packetReader.ReadInt32();
        Resource.Instance[index].ResourceType = packetReader.ReadInt32();
        Resource.Instance[index].RespawnTime = packetReader.ReadInt32();
        Resource.Instance[index].SuccessMessage = packetReader.ReadString();
        Resource.Instance[index].LvlRequired = packetReader.ReadInt32();
        Resource.Instance[index].ToolRequired = packetReader.ReadInt32();
        Resource.Instance[index].Walkthrough = packetReader.ReadBoolean();

        // common event fields (0 = none)
        Resource.Instance[index].CommonEventType = packetReader.ReadByte();
        Resource.Instance[index].CommonEventData1 = packetReader.ReadInt32();
        Resource.Instance[index].CommonEventData2 = packetReader.ReadInt32();

        Resource.OnSave(index);

        General.Logger.LogInformation("{AccountName} saved Resource #{Resource}",
            GetAccountLogin(session.Id), index);

        Network.UpdateResourceToAll(index);
    }

    public static async ValueTask RequestResource(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        var index = packetReader.ReadInt32();
        if (index < 0 | index > Core.Globals.Variables.MaxResources)
        {
            return;
        }

        Network.UpdateResourceTo(session.Id, index);
    }

    public static async ValueTask RequestItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        var index = packetReader.ReadInt32();
        if (index < 0 || index > Core.Globals.Variables.MaxItems)
        {
            return;
        }

        Network.UpdateItemTo(session.Id, index);
    }

    public static async ValueTask RequestEditItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetAccess(session.Id) < (byte)Access.Mapper)
        {
            Network.PlayerMessage(session.Id, "Invalid access level.", (int)ColorName.BrightRed);
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Item);
        if (!string.IsNullOrEmpty(user))
        {
            Network.PlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Item;

        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SItemEditor);

        PlayerService.Instance.SendDataTo(session.Id, packet.GetBytes());

        Network.Animations(session.Id);
        Network.Projectiles(session.Id);
        Network.Jobs(session);
        Network.Items(session.Id);
    }

    public static async ValueTask SaveItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetAccess(session.Id) < (byte)Access.Developer)
        {
            return;
        }

        var index = packetReader.ReadInt32();
        if (index < 0 || index > Core.Globals.Variables.MaxItems)
        {
            return;
        }

        Item.Instance[index].AccessReq = packetReader.ReadInt32();

        var statCount = Enum.GetNames<Stat>().Length;
        for (var i = 0; i < statCount; i++)
        {
            Item.Instance[index].AddStat[i] = packetReader.ReadInt32();
        }

        Item.Instance[index].Animation = packetReader.ReadInt32();
        Item.Instance[index].BindType = packetReader.ReadByte();
        Item.Instance[index].JobReq = packetReader.ReadInt32();
        Item.Instance[index].Data1 = packetReader.ReadInt32();
        Item.Instance[index].Data2 = packetReader.ReadInt32();
        Item.Instance[index].Data3 = packetReader.ReadInt32();
        Item.Instance[index].LevelReq = packetReader.ReadInt32();
        Item.Instance[index].Mastery = packetReader.ReadByte();
        Item.Instance[index].Name = packetReader.ReadString();
        Item.Instance[index].Paperdoll = packetReader.ReadInt32();
        Item.Instance[index].Icon = packetReader.ReadInt32();
        Item.Instance[index].Price = packetReader.ReadInt32();
        Item.Instance[index].Rarity = packetReader.ReadByte();
        Item.Instance[index].AttackSpeed = packetReader.ReadInt32();
        Item.Instance[index].MovementSpeed = packetReader.ReadSingle();
        
        Item.Instance[index].Stackable = packetReader.ReadByte();
        Item.Instance[index].Description = packetReader.ReadString();

        for (var i = 0; i < statCount; i++)
        {
            Item.Instance[index].StatReq[i] = packetReader.ReadInt32();
        }

        Item.Instance[index].Type = packetReader.ReadByte();
        Item.Instance[index].SubType = packetReader.ReadByte();
        Item.Instance[index].ItemLevel = packetReader.ReadByte();
        Item.Instance[index].KnockBack = packetReader.ReadByte();
        Item.Instance[index].KnockBackTiles = packetReader.ReadByte();
        Item.Instance[index].Projectile = packetReader.ReadInt32();
        Item.Instance[index].Ammo = packetReader.ReadInt32();

        Item.Instance[index].CommonEventType = packetReader.ReadByte();
        Item.Instance[index].CommonEventData1 = packetReader.ReadInt32();
        Item.Instance[index].CommonEventData2 = packetReader.ReadInt32();

        Item.Instance[index].MaxDurability = packetReader.ReadInt32();
    
        Item.OnSave(index);

        General.Logger.LogInformation("{AccountName} saved item #{Item}",
            GetAccountLogin(session.Id), index);
        Network.UpdateItemToAll(index);
    }

    public static async ValueTask GetItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Server.Player.OnGetItem(session.Id);
    }

    public static async ValueTask DropItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var inv = buffer.ReadInt32();
        var amount = buffer.ReadInt32();

        if (Data.TempPlayer[session.Id].InBank || Data.TempPlayer[session.Id].InShop >= 0)
        {
            return;
        }

        if (inv < 0 || inv > Core.Globals.Variables.MaxInventory)
        {
            return;
        }

        if (GetInv(session.Id, inv) < 0 || GetInv(session.Id, inv) > Core.Globals.Variables.MaxItems)
        {
            return;
        }

        // Never allow dropping zero/negative amounts.
        if (amount < 1)
        {
            Network.PlayerMessage(session.Id, "You must drop at least 1.", (int)ColorName.BrightRed);
            return;
        }

        var itemId = GetInv(session.Id, inv);
        var itemTemplate = Item.Instance[itemId];

        if (itemTemplate.Type == (byte)ItemCategory.Currency ||
            itemTemplate.Stackable == 1)
        {
            if (amount > GetInvValue(session.Id, inv))
            {
                return;
            }
        }
        else
        {
            // Non-stackable items are dropped as a single instance regardless of client input.
            amount = 1;
        }

        try
        {
            Script.Instance?.OnDrop(session.Id, inv, amount);
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(DropItem));
        }
    }

    public static async ValueTask RequestEditAnimation(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetAccess(session.Id) < (byte)Access.Developer)
        {
            Network.PlayerMessage(session.Id, "Invalid access level.", (int)ColorName.BrightRed);
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Animation);
        if (!string.IsNullOrEmpty(user))
        {
            Network.PlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Animation;

        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SAnimationEditor);

        PlayerService.Instance.SendDataTo(session.Id, packet.GetBytes());

        Network.Animations(session.Id);
    }

    public static async ValueTask SaveAnimation(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetAccess(session.Id) < (byte)Access.Developer)
        {
            return;
        }

        var index = packetReader.ReadInt32();
        if (index < 0 || index > Core.Globals.Variables.MaxAnimations)
        {
            return;
        }

        for (var i = 0; i < Animation.Instance[index].Frames.Length; i++)
        {
            Animation.Instance[index].Frames[i] = packetReader.ReadInt32();
        }

        for (var i = 0; i < Animation.Instance[index].LoopCount.Length; i++)
        {
            Animation.Instance[index].LoopCount[i] = packetReader.ReadInt32();
        }

        for (var i = 0; i < Animation.Instance[index].LoopTime.Length; i++)
        {
            Animation.Instance[index].LoopTime[i] = packetReader.ReadInt32();
        }

        Animation.Instance[index].Name = packetReader.ReadString();
        Animation.Instance[index].Sound = packetReader.ReadString();
        for (var i = 0; i < Animation.Instance[index].Sprite.Length; i++)
        {
            Animation.Instance[index].Sprite[i] = packetReader.ReadInt32();
        }

        Animation.OnSave(index);

        General.Logger.LogInformation("{AccountName} saved animation #{Animation}",
            GetAccountLogin(session.Id), index);

        Network.UpdateAnimationToAll(index);
    }

    public static async ValueTask RequestAnimation(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        var animation = packetReader.ReadInt32();
        if (animation < 0 || animation >= Core.Globals.Variables.MaxAnimations)
        {
            return;
        }

        Network.UpdateAnimationTo(session.Id, animation);
    }

    public static async ValueTask Event(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);
        var localEventIndex = buffer.ReadInt32();

        int slot = localEventIndex + 1;
        if (slot <= 0 || slot > Data.TempPlayer[session.Id].EventMap.CurrentEvents)
            return;
            
        if (Data.TempPlayer[session.Id].EventMap.EventPages == null || slot >= Data.TempPlayer[session.Id].EventMap.EventPages.Length)
            return;

        int mapEventId = Data.TempPlayer[session.Id].EventMap.EventPages[slot].EventId;
        if (mapEventId < 0)
            return;

        EventLogic.TriggerEvent(session.Id, mapEventId, 0, GetX(session.Id), GetY(session.Id));
    }

    public static async ValueTask RequestSwitchesAndVariables(GameSession session, ReadOnlyMemory<byte> bytes) => Network.SwitchesAndVariables(session.Id);

    public static async ValueTask SwitchesAndVariables(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);
        for (var i = 0; i < Core.Globals.Variables.MaxSwitches; i++) Server.Event.Switches[i] = buffer.ReadString();
        for (var i = 0; i < Core.Globals.Variables.MaxVariables; i++) Server.Event.Variables[i] = buffer.ReadString();

        Server.Event.SaveSwitches();
        Server.Event.SaveVariables();
        Network.SwitchesAndVariables(0, true);
    }

    public static async ValueTask EventChatReply(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);
        int eventId = buffer.ReadInt32(), pageId = buffer.ReadInt32(), reply = buffer.ReadInt32();

        General.Logger.LogInformation(
            "Player {PlayerId} responded to event {EventId}/{PageId} with reply {Reply}",
            session.Id,
            eventId,
            pageId,
            reply
        );
        Server.Event.ProcessEventReply(session.Id, eventId, pageId, reply);
    }


    public static async ValueTask PartyRquest(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent partying with self
        if (Data.TempPlayer[session.Id].Target == session.Id)
            return;

        // make sure it's a valid target
        if (Data.TempPlayer[session.Id].TargetType != (byte)TargetType.Player)
            return;

        // make sure they're connected and on the same map
        if (GetMap(Data.TempPlayer[session.Id].Target) != GetMap(session.Id))
            return;

        // init the request
        Party.OnInvite(session.Id, Data.TempPlayer[session.Id].Target);
    }

    public static async ValueTask AcceptParty(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Party.OnAccept(Data.TempPlayer[session.Id].PartyInvite, session.Id);
    }

    public static async ValueTask DeclineParty(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Party.OnDecline(Data.TempPlayer[session.Id].PartyInvite, session.Id);
    }

    public static async ValueTask LeaveParty(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Party.OnLeave(session.Id);
    }

    public static async ValueTask PartyChatMsg(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        Party.OnMessage(session.Id, buffer.ReadString());
    }

    public static async ValueTask RequestEditNpc(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetAccess(session.Id) < (byte)Access.Developer)
        {
            Network.PlayerMessage(session.Id, "Invalid access level.", (int)ColorName.BrightRed);
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Npc);
        if (!string.IsNullOrEmpty(user))
        {
            Network.PlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Npc;

        Network.Items(session.Id);
        Network.Animations(session.Id);
        Network.Skills(session.Id);

        Network.Npcs(session.Id);

        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SNpcEditor);

        PlayerService.Instance.SendDataTo(session.Id, packet.GetBytes());
    }

    public static async ValueTask SaveNpc(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetAccess(session.Id) < (byte)Access.Developer)
        {
            return;
        }

        var npc = packetReader.ReadInt32();
        if (npc < 0 | npc > Core.Globals.Variables.MaxNpcs)
        {
            return;
        }

        Npc.Instance[npc].Animation = packetReader.ReadInt32();
        Npc.Instance[npc].AttackSay = packetReader.ReadString();
        Npc.Instance[npc].Behavior = packetReader.ReadByte();

        for (var i = 0; i < Core.Globals.Variables.MaxDropItems; i++)
        {
            Npc.Instance[npc].DropChance[i] = packetReader.ReadInt32();
            Npc.Instance[npc].DropItem[i] = packetReader.ReadInt32();
            Npc.Instance[npc].DropItemValue[i] = packetReader.ReadInt32();
        }

        Npc.Instance[npc].Experience = packetReader.ReadInt32();
        Npc.Instance[npc].Faction = packetReader.ReadByte();
        Npc.Instance[npc].Hp = packetReader.ReadInt32();
        Npc.Instance[npc].Name = packetReader.ReadString();
        Npc.Instance[npc].Range = packetReader.ReadByte();
        Npc.Instance[npc].SpawnTime = packetReader.ReadByte();
        Npc.Instance[npc].SpawnSecs = packetReader.ReadInt32();
        Npc.Instance[npc].Sprite = packetReader.ReadInt32();

        var statCount = Enum.GetValues<Stat>().Length;
        for (var i = 0; i < statCount; i++)
        {
            Npc.Instance[npc].Stat[i] = packetReader.ReadByte();
        }

        for (var i = 0; i < Core.Globals.Variables.MaxNpcSkills; i++)
        {
            Npc.Instance[npc].Skill[i] = packetReader.ReadByte();
        }

        Npc.Instance[npc].Level = packetReader.ReadByte();
        Npc.Instance[npc].Damage = packetReader.ReadInt32();

        Npc.Instance[npc].DeathSwitch = packetReader.ReadInt32();
        Npc.Instance[npc].DeathVariable = packetReader.ReadInt32();

        Npc.Instance[npc].DeathSwitchValue = packetReader.ReadInt32();
        Npc.Instance[npc].DeathVariableValue = packetReader.ReadInt32();

        // common event fields (0 = none)
        Npc.Instance[npc].CommonEventType = packetReader.ReadByte();
        Npc.Instance[npc].CommonEventData1 = packetReader.ReadInt32();
        Npc.Instance[npc].CommonEventData2 = packetReader.ReadInt32();

        Npc.OnSave(npc);

        General.Logger.LogInformation("{AccountName} saved NPC #{Npc}",
            GetAccountLogin(session.Id), npc);

        Network.UpdateNpcToAll(npc);
    }
}
