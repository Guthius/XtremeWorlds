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

public sealed class GamePacketParser : PacketParser<GamePacketId.FromClient, GameSession>
{
    public GamePacketParser()
    {
        Bind(GamePacketId.FromClient.CCheckPing, Packet_Ping);
        Bind(GamePacketId.FromClient.CLogin, Packet_Login);
        Bind(GamePacketId.FromClient.CRegister, Packet_Register);
        Bind(GamePacketId.FromClient.CAddChar, Packet_AddChar);
        Bind(GamePacketId.FromClient.CUseChar, Packet_UseChar);
        Bind(GamePacketId.FromClient.CDelChar, Packet_DelChar);
        Bind(GamePacketId.FromClient.CLogout, Packet_Logout);
        Bind(GamePacketId.FromClient.CSayMessage, Packet_SayMessage);
        Bind(GamePacketId.FromClient.CBroadcastMessage, Packet_BroadCastMsg);
        Bind(GamePacketId.FromClient.CPlayerMessage, Packet_PlayerMsg);
        Bind(GamePacketId.FromClient.CAdminMessage, Packet_SendAdminMessage);
        Bind(GamePacketId.FromClient.CPlayerMove, Packet_PlayerMove);
        Bind(GamePacketId.FromClient.CStopPlayerMove, Packet_StopPlayerMove);
        Bind(GamePacketId.FromClient.CPlayerDir, Packet_PlayerDirection);
        Bind(GamePacketId.FromClient.CUseItem, Packet_UseItem);
        Bind(GamePacketId.FromClient.CAttack, Packet_Attack);
        Bind(GamePacketId.FromClient.CMouseAttack, Packet_MouseAttack);
        Bind(GamePacketId.FromClient.CPlayerInfoRequest, Packet_PlayerInfo);
        Bind(GamePacketId.FromClient.CWarpMeTo, Packet_WarpMeTo);
        Bind(GamePacketId.FromClient.CWarpToMe, Packet_WarpToMe);
        Bind(GamePacketId.FromClient.CWarpTo, Packet_WarpTo);
        Bind(GamePacketId.FromClient.CSetSprite, Packet_SetSprite);
        Bind(GamePacketId.FromClient.CGetStats, Packet_GetStats);
        Bind(GamePacketId.FromClient.CRequestNewMap, Packet_RequestNewMap);
        Bind(GamePacketId.FromClient.CSaveMap, Packet_MapData);
        Bind(GamePacketId.FromClient.CNeedMap, Packet_NeedMap);
        Bind(GamePacketId.FromClient.CMapGetItem, Packet_GetItem);
        Bind(GamePacketId.FromClient.CMapDropItem, Packet_DropItem);
        Bind(GamePacketId.FromClient.CMapRespawn, Packet_RespawnMap);
        Bind(GamePacketId.FromClient.CMapReport, Packet_MapReport);
        Bind(GamePacketId.FromClient.CKickPlayer, Packet_KickPlayer);
        Bind(GamePacketId.FromClient.CBanList, Packet_Banlist);
        Bind(GamePacketId.FromClient.CBanDestroy, Packet_DestroyBans);
        Bind(GamePacketId.FromClient.CBanPlayer, Packet_BanPlayer);

        Bind(GamePacketId.FromClient.CRequestEditMap, Packet_RequestEditMap);

        Bind(GamePacketId.FromClient.CSetAccess, Packet_SetAccess);
        Bind(GamePacketId.FromClient.CWhosOnline, Packet_WhosOnline);
        Bind(GamePacketId.FromClient.CSetMotd, Packet_SetMotd);
        Bind(GamePacketId.FromClient.CSearch, Packet_PlayerSearch);
        Bind(GamePacketId.FromClient.CSkills, Packet_Skills);
        Bind(GamePacketId.FromClient.CCast, Packet_Cast);
        Bind(GamePacketId.FromClient.CSwapInvSlots, Packet_SwapInvSlots);
        Bind(GamePacketId.FromClient.CSwapSkillSlots, Packet_SwapSkillSlots);

        Bind(GamePacketId.FromClient.CCheckPing, Packet_CheckPing);
        Bind(GamePacketId.FromClient.CUnequip, Packet_UnEquip);
        Bind(GamePacketId.FromClient.CRequestPlayerData, Packet_RequestPlayerData);
        Bind(GamePacketId.FromClient.CRequestItem, Packet_RequestItem);
        Bind(GamePacketId.FromClient.CRequestNpc, Packet_RequestNpc);
        Bind(GamePacketId.FromClient.CRequestResource, Packet_RequestResource);
        Bind(GamePacketId.FromClient.CSpawnItem, Packet_SpawnItem);
        Bind(GamePacketId.FromClient.CTrainStat, Packet_TrainStat);

        Bind(GamePacketId.FromClient.CRequestAnimation, Packet_RequestAnimation);
        Bind(GamePacketId.FromClient.CRequestSkill, Packet_RequestSkill);
        Bind(GamePacketId.FromClient.CRequestShop, Packet_RequestShop);
        Bind(GamePacketId.FromClient.CRequestLevelUp, Packet_RequestLevelUp);
        Bind(GamePacketId.FromClient.CForgetSkill, Packet_ForgetSkill);
        Bind(GamePacketId.FromClient.CCloseShop, Packet_CloseShop);
        Bind(GamePacketId.FromClient.CBuyItem, Packet_BuyItem);
        Bind(GamePacketId.FromClient.CSellItem, Packet_SellItem);
        Bind(GamePacketId.FromClient.CChangeBankSlots, Packet_ChangeBankSlots);
        Bind(GamePacketId.FromClient.CDepositItem, Packet_DepositItem);
        Bind(GamePacketId.FromClient.CWithdrawItem, Packet_WithdrawItem);
        Bind(GamePacketId.FromClient.CCloseBank, Packet_CloseBank);
        Bind(GamePacketId.FromClient.CAdminWarp, Packet_AdminWarp);

        Bind(GamePacketId.FromClient.CTradeInvite, Packet_TradeInvite);
        Bind(GamePacketId.FromClient.CHandleTradeInvite, Packet_HandleTradeInvite);
        Bind(GamePacketId.FromClient.CAcceptTrade, Packet_AcceptTrade);
        Bind(GamePacketId.FromClient.CDeclineTrade, Packet_DeclineTrade);
        Bind(GamePacketId.FromClient.CTradeItem, Packet_TradeItem);
        Bind(GamePacketId.FromClient.CUntradeItem, Packet_UntradeItem);

        Bind(GamePacketId.FromClient.CAdmin, Packet_Admin);

        Bind(GamePacketId.FromClient.CSetHotbarSlot, Packet_SetHotbarSlot);
        Bind(GamePacketId.FromClient.CDeleteHotbarSlot, Packet_DeleteHotbarSlot);
        Bind(GamePacketId.FromClient.CUseHotbarSlot, Packet_UseHotbarSlot);

        Bind(GamePacketId.FromClient.CSkillLearn, Packet_SkillLearn);

        Bind(GamePacketId.FromClient.CEventChatReply, Packet_EventChatReply);
        Bind(GamePacketId.FromClient.CEvent, Packet_Event);
        Bind(GamePacketId.FromClient.CRequestSwitchesAndVariables, Packet_RequestSwitchesAndVariables);
        Bind(GamePacketId.FromClient.CSwitchesAndVariables, Packet_SwitchesAndVariables);

        Bind(GamePacketId.FromClient.CRequestProjectile, Packet_RequestProjectile);
        Bind(GamePacketId.FromClient.CClearProjectile, Packet_ClearProjectile);

        Bind(GamePacketId.FromClient.CEmote, Packet_Emote);

        Bind(GamePacketId.FromClient.CRequestParty, Packet_PartyRquest);
        Bind(GamePacketId.FromClient.CAcceptParty, Packet_AcceptParty);
        Bind(GamePacketId.FromClient.CDeclineParty, Packet_DeclineParty);
        Bind(GamePacketId.FromClient.CLeaveParty, Packet_LeaveParty);
        Bind(GamePacketId.FromClient.CPartyChatMsg, Packet_PartyChatMsg);
        Bind(GamePacketId.FromClient.CRequestEditItem, Packet_RequestEditItem);
        Bind(GamePacketId.FromClient.CSaveItem, Packet_SaveItem);
        Bind(GamePacketId.FromClient.CRequestEditNpc, Packet_RequestEditNpc);
        Bind(GamePacketId.FromClient.CSaveNpc, Packet_SaveNpc);
        Bind(GamePacketId.FromClient.CRequestEditShop, Packet_RequestEditShop);
        Bind(GamePacketId.FromClient.CSaveShop, Packet_SaveShop);
        Bind(GamePacketId.FromClient.CRequestEditSkill, Packet_RequestEditSkill);
        Bind(GamePacketId.FromClient.CSaveSkill, Packet_SaveSkill);
        Bind(GamePacketId.FromClient.CRequestEditResource, Packet_RequestEditResource);
        Bind(GamePacketId.FromClient.CSaveResource, Packet_SaveResource);
        Bind(GamePacketId.FromClient.CRequestEditAnimation, Packet_RequestEditAnimation);
        Bind(GamePacketId.FromClient.CSaveAnimation, Packet_SaveAnimation);
        Bind(GamePacketId.FromClient.CRequestEditProjectile, Packet_RequestEditProjectile);
        Bind(GamePacketId.FromClient.CSaveProjectile, Packet_SaveProjectile);
        Bind(GamePacketId.FromClient.CRequestEditJob, Packet_RequestEditJob);
        Bind(GamePacketId.FromClient.CSaveJob, Packet_SaveJob);

        Bind(GamePacketId.FromClient.CRequestMoral, Packet_RequestMoral);
        Bind(GamePacketId.FromClient.CRequestEditMoral, Packet_RequestEditMoral);
        Bind(GamePacketId.FromClient.CSaveMoral, Packet_SaveMoral);

        Bind(GamePacketId.FromClient.CRequestEditScript, Packet_RequestEditScript);
        Bind(GamePacketId.FromClient.CSaveScript, Packet_SaveScript);

        Bind(GamePacketId.FromClient.CCloseEditor, Packet_CloseEditor);
        Bind(GamePacketId.FromClient.CCancelCast, Packet_CancelCast);
    }

    private static void Packet_Ping(GameSession session, ReadOnlyMemory<byte> bytes)
    {
    }

    private static async void Packet_Login(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var reader = new PacketReader(bytes);

        if (NetworkConfig.IsPlaying(session.Id))
        {
            NetworkSend.SendAlert(session, SystemMessage.Connection, Menu.Login);
            return;
        }

        if (NetworkConfig.IsLoggedIn(session.Id))
        {
            return;
        }

        if (General.GetShutDownTimer != null && General.GetShutDownTimer.IsRunning)
        {
            NetworkSend.SendAlert(session, SystemMessage.ServerMaintenance, Menu.Login);
            return;
        }

        var usernameBytes = reader.ReadBytes().ToArray();
        var login = System.Text.Encoding.UTF8.GetString(session.Decrypt(usernameBytes)).ToLower().Replace("\0", "");

        var passwordBytes = reader.ReadBytes().ToArray();
        var password = System.Text.Encoding.UTF8.GetString(session.Decrypt(passwordBytes)).Replace("\0", "");

        // Get the current executing assembly
        var assembly = Assembly.GetExecutingAssembly();

        // Retrieve the version information
        var clientVersionBytes = reader.ReadBytes().ToArray();
        var serverVersion = assembly.GetName().Version?.ToString();
        var clientVersion = System.Text.Encoding.UTF8.GetString(session.Decrypt(clientVersionBytes));

        // Check versions
        if (clientVersion != serverVersion)
        {
            NetworkSend.SendAlert(session, SystemMessage.ClientOutdated, Menu.Login);
            return;
        }

        if (login.Length > Core.Globals.Variables.NameLength | login.Length < Core.Globals.Variables.MinimumNameLength)
        {
            NetworkSend.SendAlert(session, SystemMessage.NameLengthInvalid);
            return;
        }

        if (NetworkConfig.IsMultiLogin(session.Id, login))
        {
            NetworkSend.SendAlert(session, SystemMessage.MultipleAccountsNotAllowed, Menu.Login);
            return;
        }

        for (int i = 0; i <= session.Id; i++)
        {
            if (Account.Instance.Count <= i)
                Account.Instance.Add(new Account());
        }

        Account.Instance[session.Id].Login = login;

        await Account.OnLoadAsync(session.Id, new CancellationToken());

        if (Account.Instance[session.Id].Login != login)
        {
            NetworkSend.SendAlert(session, SystemMessage.Login, Menu.Login);
            return;
        }
        
        if (GetPlayerPassword(session.Id) != password)
        {
            NetworkSend.SendAlert(session, SystemMessage.WrongPassword, Menu.Login);
            return;
        }

        if (Database.IsBanned(session.Id, session.Channel.IpAddress))
        {
            NetworkSend.SendAlert(session, SystemMessage.Banned, Menu.Login);
            return;
        }

        if (GetAccountLogin(session.Id) == "")
        {
            NetworkSend.SendAlert(session, SystemMessage.DatabaseError, Menu.Login);
            return;
        }

        General.Logger.LogInformation("{AccountName} has logged in from {IpAddress}",
            GetAccountLogin(session.Id), session.Channel.IpAddress);

        PlayerService.Instance.AddPlayer(session.Id, session.Channel);
        NetworkSend.SendVariables(session);
        NetworkSend.SendPlayerCharacters(session);
        NetworkSend.SendJobs(session);
    }

    private static void Packet_Register(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        if (NetworkConfig.IsPlaying(session.Id) ||
            NetworkConfig.IsLoggedIn(session.Id))
        {
            return;
        }

        // check if its banned
        // Cut off last portion of ip
        if (Database.IsBanned(session.Id, session.Channel.IpAddress))
        {
            NetworkSend.SendAlert(session, SystemMessage.Banned, Menu.Register);
            return;
        }

        if (General.GetShutDownTimer is { IsRunning: true })
        {
            NetworkSend.SendAlert(session, SystemMessage.ServerMaintenance, Menu.Register);
            return;
        }

        var usernameBytes = buffer.ReadBytes().ToArray();
        var login = System.Text.Encoding.UTF8.GetString(session.Decrypt(usernameBytes)).ToLower().Replace("\0", "");

        var passwordBytes = buffer.ReadBytes().ToArray();
        var password = System.Text.Encoding.UTF8.GetString(session.Decrypt(passwordBytes)).Replace("\0", "");

        // Get the current executing assembly
        var assembly = Assembly.GetExecutingAssembly();

        // Retrieve the version information
        var clientVersionBytes = buffer.ReadBytes().ToArray();
        var serverVersion = assembly.GetName().Version?.ToString();
        var clientVersion = System.Text.Encoding.UTF8.GetString(session.Decrypt(clientVersionBytes));

        // Check versions
        if (clientVersion != serverVersion)
        {
            NetworkSend.SendAlert(session, SystemMessage.ClientOutdated, Menu.Register);
            return;
        }

        var x = General.IsValidLogin(login);

        switch (x) // Check if the username is valid
        {
            case -1:
                NetworkSend.SendAlert(session, SystemMessage.NameContainsIllegalCharacters, Menu.Register);
                return;

            case 0:
                NetworkSend.SendAlert(session, SystemMessage.NameLengthInvalid, Menu.Register);
                return;
        }

        if (NetworkConfig.IsMultiLogin(session.Id, login))
        {
            NetworkSend.SendAlert(session, SystemMessage.MultipleAccountsNotAllowed, Menu.Register);
            return;
        }

        var userData = Database.SelectRowByColumn("id", Database.GetStringHash(login), "account", "data");
        if (userData is not null)
        {
            NetworkSend.SendAlert(session, SystemMessage.NameTaken, Menu.Register);
            return;
        }
        
        for (int i = 0; i <= session.Id; i++)
        {
            if (Account.Instance.Count <= i)
                Account.Instance.Add(new Account());
        }
        
        Account.Instance[session.Id].Login = login;
        Account.Instance[session.Id].Password = password;

        Account.OnSave(session.Id).Wait();

        // send them to the character portal
        NetworkSend.SendPlayerCharacters(session);
        NetworkSend.SendJobs(session);
    }

    private static void Packet_AddChar(GameSession session, ReadOnlyMemory<byte> bytes)
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
                NetworkSend.SendAlert(session, SystemMessage.MaxCharactersReached, Menu.CharacterSelect);
                return;
            }

            Data.TempPlayer[session.Id].Slot = slot;

            var x = General.IsValidLogin(name);

            // Check if the username is valid
            if (x == -1)
            {
                NetworkSend.SendAlert(session, SystemMessage.NameContainsIllegalCharacters, Menu.Register);
                return;
            }
            else if (x == 0)
            {
                NetworkSend.SendAlert(session, SystemMessage.NameLengthInvalid, Menu.Register);
                return;
            }

            // Check if name is already in use
            if (Database.CharacterList?.Contains(name) == true)
            {
                NetworkSend.SendAlert(session, SystemMessage.NameTaken, Menu.NewCharacter);
                return;
            }

            if (sex < (byte)Sex.Male | sex > (byte)Sex.Female)
                return;

            if (job < 0 | job > Core.Globals.Variables.MaxJobs)
                return;

            if (Job.Instance.Count <= job)
            {
                for (int i = Job.Instance.Count; i <= job; i++)
                {
                    var instance = new Job();
                    Job.Instance.Add(instance);
                }
            }

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
            Database.AddChar(session.Id, slot, name, (byte)sex, (byte)job, sprite).Wait();

            Log.Add("Character " + name + " added to " + GetAccountLogin(session.Id) + "'s account.", Constant.PlayerLog);
            Server.Player.OnAdd(session);
        }
    }

    private static void Packet_UseChar(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var reader = new PacketReader(bytes);

        if (!NetworkConfig.IsPlaying(session.Id))
        {
            if (NetworkConfig.IsLoggedIn(session.Id))
            {
                var slot = reader.ReadByte();
                if (slot < 0 || slot >= Core.Globals.Variables.MaxCharacters)
                {
                    NetworkSend.SendAlert(session, SystemMessage.MaxCharactersReached, Menu.CharacterSelect);
                    return;
                }

                for (int n = 0; n < session.Id; n++)
                {
                    if (PlayerBase.Instance?.Count <= n)
                    {
                        PlayerBase.Instance?.Add(new PlayerBase());
                    }
                }
                PlayerBase.Instance?.Add(Account.Instance[session.Id].Player[slot]);
                Server.Player.OnAdd(session);
            }
            else
            {
                NetworkSend.SendAlert(session, SystemMessage.Connection, Menu.Login);
            }
        }
        else
        {
            NetworkSend.SendAlert(session, SystemMessage.Connection, Menu.Login);
        }
    }

    private static async void Packet_DelChar(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        if (!NetworkConfig.IsPlaying(session.Id))
        {
            var slot = buffer.ReadByte();
            if (slot < 0 || slot >= Core.Globals.Variables.MaxCharacters)
            {
                NetworkSend.SendAlert(session, SystemMessage.MaxCharactersReached, Menu.CharacterSelect);
                return;
            }

            Database.CharacterList?.Remove(Account.Instance[session.Id].Player[slot].Name);
            Account.Instance[session.Id].Player[slot] = new Server.Player();
            await Account.OnSave(session.Id);

            // send them to the character portal
            NetworkSend.SendPlayerCharacters(session);
        }
        else
        {
            NetworkSend.SendAlert(session, SystemMessage.Connection, Menu.Login);
        }
    }

    private static void Packet_Logout(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (!NetworkConfig.IsPlaying(session.Id))
        {
            return;
        }

        NetworkSend.SendLeftGame(session.Id);

        var task = Server.Player.OnExit(session.Id);

        task.Wait();
    }

    private static void Packet_SayMessage(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var msg = buffer.ReadString();

        Log.Add("Map #" + GetPlayerMap(session.Id) + ": " + GetPlayerName(session.Id) + " says, '" + msg + "'", Constant.PlayerLog);

        NetworkSend.SayMsg_Map(GetPlayerMap(session.Id), session.Id, msg, (int)ColorName.White);
        NetworkSend.SendChatBubble(GetPlayerMap(session.Id), session.Id, (int)TargetType.Player, msg, (int)ColorName.White);
    }

    private static void Packet_BroadCastMsg(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var msg = buffer.ReadString();

        var s = "[Global] " + GetPlayerName(session.Id) + ": " + msg;
        NetworkSend.SayMsg_Global(session.Id, msg, (int)ColorName.White);
        Log.Add(s, Constant.PlayerLog);
        Console.WriteLine(s);
    }

    public static void Packet_PlayerMsg(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var otherPlayer = buffer.ReadString();
        var msg = buffer.ReadString();

        var otherPlayerIndex = GameLogic.FindPlayer(otherPlayer);
        if (otherPlayerIndex != session.Id)
        {
            if (otherPlayerIndex >= 0)
            {
                Log.Add(GetPlayerName(session.Id) + " tells " + GetPlayerName(session.Id) + ", '" + msg + "'", Constant.PlayerLog);
                NetworkSend.SendPlayerMessage(otherPlayerIndex, GetPlayerName(session.Id) + " tells you, '" + msg + "'", (int)ColorName.Pink);
                NetworkSend.SendPlayerMessage(session.Id, "You tell " + GetPlayerName(otherPlayerIndex) + ", '" + msg + "'", (int)ColorName.Pink);
            }
            else
            {
                NetworkSend.SendPlayerMessage(session.Id, "Player is not online.", (int)ColorName.BrightRed);
            }
        }
        else
        {
            NetworkSend.SendPlayerMessage(session.Id, "Cannot message your self!", (int)ColorName.BrightRed);
        }
    }

    private static void Packet_SendAdminMessage(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var s = default(string);
        var buffer = new PacketReader(bytes);

        var msg = buffer.ReadString();

        NetworkSend.SendAdminMessage(msg);
        Log.Add(s ?? string.Empty, Constant.PlayerLog);
        Console.WriteLine(s);
    }

    private static void Packet_PlayerMove(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        if (Data.TempPlayer[session.Id].GettingMap)
            return;

        var dir = buffer.ReadByte();
        var movement = buffer.ReadByte();
        var tmpX = buffer.ReadInt32();
        var tmpY = buffer.ReadInt32();

        SetPlayerDir(session.Id, dir);

        if (tmpX != GetPlayerRawX(session.Id) || tmpY != GetPlayerRawY(session.Id))
        {
            // Desync detected, correct client
            NetworkSend.SendPlayerXYToMap(session.Id);
            return;
        }

        PlayerBase.Instance[session.Id].Moving = movement;

        // Requirement: moving cancels any buffered cast
        if (Data.TempPlayer[session.Id].SkillBuffer >= 0)
        {
            Data.TempPlayer[session.Id].SkillBuffer = -1;
            Data.TempPlayer[session.Id].SkillBufferTimer = 0;
            NetworkSend.SendClearSkillBuffer(session.Id);
        }
    }

    private static void Packet_CancelCast(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Client intends to cancel current cast/buffer (e.g., Escape). Server is authoritative.
        if (Data.TempPlayer[session.Id].SkillBuffer >= 0)
        {
            Data.TempPlayer[session.Id].SkillBuffer = -1;
            Data.TempPlayer[session.Id].SkillBufferTimer = 0;
            NetworkSend.SendClearSkillBuffer(session.Id);
        }
    }

    public static void Packet_StopPlayerMove(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        if (Data.TempPlayer[session.Id].GettingMap)
            return;

        PlayerBase.Instance[session.Id].IsMoving = false;
        PlayerBase.Instance[session.Id].Moving = 0;

        // Broadcast final resting position & flags immediately
        NetworkSend.SendPlayerXYToMap(session.Id);
    }

    public static void Packet_PlayerDirection(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        if (Data.TempPlayer[session.Id].GettingMap == true)
            return;

        var dir = buffer.ReadInt32();

        // Prevent hacking: accept full 8-direction enum range
        int dirCount = Enum.GetNames(typeof(Direction)).Length;
        if (dir < 0 | dir > dirCount)
            return;

        SetPlayerDir(session.Id, dir);

        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ServerPackets.SPlayerDir);
        packetWriter.WriteInt32(session.Id);
        packetWriter.WriteByte(GetPlayerDir(session.Id));

        NetworkConfig.SendDataToMapBut(session.Id, GetPlayerMap(session.Id), packetWriter.GetBytes());
    }

    public static void Packet_UseItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var invNum = buffer.ReadInt32();

        Server.Player.UseItem(session.Id, invNum);
    }

    public static void Packet_Attack(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var x = 0;
        var y = 0;

        // can't attack whilst casting
        if (Data.TempPlayer[session.Id].SkillBuffer >= 0)
            return;

        // can't attack whilst stunned
        if (Data.TempPlayer[session.Id].StunDuration > 0)
            return;

        NetworkSend.SendPlayerAttack(session.Id);

        // Projectile check
        if (GetPlayerPaperdoll(session.Id, Equipment.Weapon) >= 0)
        {
            if (Item.Instance[GetPlayerPaperdoll(session.Id, Equipment.Weapon)].Projectile >= 0) // Item has a projectile
            {
                if (Item.Instance[GetPlayerPaperdoll(session.Id, Equipment.Weapon)].Ammo >= 0)
                {
                    if (Server.Player.HasItem(session.Id, Item.Instance[GetPlayerPaperdoll(session.Id, Equipment.Weapon)].Ammo) > 0)
                    {
                        Server.Player.TakeInv(session.Id, Item.Instance[GetPlayerPaperdoll(session.Id, Equipment.Weapon)].Ammo, 1);
                        Projectile.OnShoot(session.Id, -1, GetPlayerPaperdoll(session.Id, Equipment.Weapon));
                        return;
                    }
                    else
                    {
                        NetworkSend.SendPlayerMessage(session.Id, "Out of " + Item.Instance[Item.Instance[GetPlayerPaperdoll(session.Id, Equipment.Weapon)].Ammo].Name + "!", (int)ColorName.BrightRed);
                        return;
                    }
                }
                else
                {
                    Projectile.OnShoot(session.Id, -1, GetPlayerPaperdoll(session.Id, Equipment.Weapon));
                    return;
                }
            }
        }

        // Check tradeskills
        switch (GetPlayerDir(session.Id))
        {
            case (byte)Direction.Up:
                {
                    if (GetPlayerY(session.Id) == 0)
                        return;
                    x = GetPlayerX(session.Id);
                    y = GetPlayerY(session.Id) - 1;
                    break;
                }
            case (byte)Direction.Down:
                {
                    if (GetPlayerY(session.Id) == Server.Map.Instance[GetPlayerMap(session.Id)].MaxY)
                        return;
                    x = GetPlayerX(session.Id);
                    y = GetPlayerY(session.Id) + 1;
                    break;
                }
            case (byte)Direction.Left:
                {
                    if (GetPlayerX(session.Id) == 0)
                        return;
                    x = GetPlayerX(session.Id) - 1;
                    y = GetPlayerY(session.Id);
                    break;
                }
            case (byte)Direction.Right:
                {
                    if (GetPlayerX(session.Id) == Server.Map.Instance[GetPlayerMap(session.Id)].MaxX)
                        return;
                    x = GetPlayerX(session.Id) + 1;
                    y = GetPlayerY(session.Id);
                    break;
                }

            case (byte)Direction.UpRight:
                {
                    if (GetPlayerX(session.Id) == Server.Map.Instance[GetPlayerMap(session.Id)].MaxX)
                        return;
                    if (GetPlayerY(session.Id) == 0)
                        return;
                    x = GetPlayerX(session.Id) + 1;
                    y = GetPlayerY(session.Id) - 1;
                    break;
                }

            case (byte)Direction.UpLeft:
                {
                    if (GetPlayerX(session.Id) == 0)
                        return;
                    if (GetPlayerY(session.Id) == 0)
                        return;
                    x = GetPlayerX(session.Id) - 1;
                    y = GetPlayerY(session.Id) - 1;
                    break;
                }

            case (byte)Direction.DownRight:
                {
                    if (GetPlayerX(session.Id) == Server.Map.Instance[GetPlayerMap(session.Id)].MaxX)
                        return;
                    if (GetPlayerY(session.Id) == Server.Map.Instance[GetPlayerMap(session.Id)].MaxY)
                        return;
                    x = GetPlayerX(session.Id) + 1;
                    y = GetPlayerY(session.Id) + 1;
                    break;
                }

            case (byte)Direction.DownLeft:
                {
                    if (GetPlayerX(session.Id) == 0)
                        return;
                    if (GetPlayerY(session.Id) == Server.Map.Instance[GetPlayerMap(session.Id)].MaxY)
                        return;
                    x = GetPlayerX(session.Id) - 1;
                    y = GetPlayerY(session.Id) + 1;
                    break;
                }
        }

        MapResource.OnUpdate(session.Id, x, y);

        // New combat system integration: attempt a melee attack on the entity (player or npc)
        // occupying the targeted tile (x,y). Legacy code only triggered animation + resource checks.                                  
        var map = GetPlayerMap(session.Id);

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
                if (GetPlayerMap(p.Id) != map) continue;
                if (GetPlayerX(p.Id) == x && GetPlayerY(p.Id) == y)
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

    public static void Packet_MouseAttack(GameSession session, ReadOnlyMemory<byte> bytes)
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
        int itemNum = GetPlayerPaperdoll(session.Id, Equipment.Weapon);
        if (itemNum < 0 || Item.Instance[itemNum].Projectile < 0)
        {
            // fallback: trigger normal attack if no projectile
            Packet_Attack(session, ReadOnlyMemory<byte>.Empty);
            return;
        }

        // Check ammo availability first (do not deduct yet)
        int ammoId = Item.Instance[itemNum].Ammo;
        if (ammoId >= 0 && Server.Player.HasItem(session.Id, ammoId) <= 0)
        {
            NetworkSend.SendPlayerMessage(session.Id, "Out of " + Item.Instance[ammoId].Name + "!", (int)ColorName.BrightRed);
            return;
        }

        // Cooldown gate using weapon attack speed to prevent spamming
        var attackerEntity = Core.Globals.Entity.FromPlayer(session.Id, PlayerBase.Instance[session.Id]);
        attackerEntity.Map = GetPlayerMap(session.Id);
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
        int startX = GetPlayerRawX(session.Id);
        int startY = GetPlayerRawY(session.Id);
        int dx = targetX - startX;
        int dy = targetY - startY;
        if (dx == 0 && dy == 0)
        {
            // if zero vector, default to current facing
            Projectile.OnShoot(session.Id, -1, itemNum);
            return;
        }

        // Normalize to fixed-point 1000 scale
        double length = Math.Sqrt((double)dx * dx + (double)dy * dy);
        short vx = (short)Math.Clamp((int)Math.Round(dx / length * 1000.0), short.MinValue, short.MaxValue);
        short vy = (short)Math.Clamp((int)Math.Round(dy / length * 1000.0), short.MinValue, short.MaxValue);

        // Fire with free-aim using helper and stop at target
        Server.Projectile.OnFreeAim(session.Id, vx, vy, itemNum, targetX, targetY);
        NetworkSend.SendPlayerAttack(session.Id);
    }

    public static void Packet_PlayerInfo(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        int n;
        var buffer = new PacketReader(bytes);

        var name = buffer.ReadString();
        var i = GameLogic.FindPlayer(name);

        if (i >= 0)
        {
            NetworkSend.SendPlayerMessage(session.Id, "Account:  " + GetAccountLogin(i) + ", Name: " + GetPlayerName(i), (int)ColorName.Yellow);

            if (GetPlayerAccess(session.Id) > (byte)AccessLevel.Moderator)
            {
                NetworkSend.SendPlayerMessage(session.Id, " Stats for " + GetPlayerName(i) + " ", (int)ColorName.Yellow);
                NetworkSend.SendPlayerMessage(session.Id, "Level: " + GetPlayerLevel(i) + "  Exp: " + GetPlayerExperience(i) + "/" + Script.Instance?.GetPlayerNextLevel(i), (int)ColorName.Yellow);
                NetworkSend.SendPlayerMessage(session.Id, "HP: " + GetPlayerVital(i, Core.Globals.Vital.Health) + "/" + Script.Instance?.GetPlayerMaxVital(i, Core.Globals.Vital.Health) + "  MP: " + GetPlayerVital(i, Core.Globals.Vital.Stamina) + "/" + Script.Instance?.GetPlayerMaxVital(i, Core.Globals.Vital.Stamina) + "  SP: " + GetPlayerVital(i, Core.Globals.Vital.Stamina) + "/" + Script.Instance?.GetPlayerMaxVital(i, Core.Globals.Vital.Stamina), (int)ColorName.Yellow);
                NetworkSend.SendPlayerMessage(session.Id, "Strength: " + GetPlayerStat(i, Stat.Strength) + "  Defense: " + GetPlayerStat(i, Stat.Luck) + "  Magic: " + GetPlayerStat(i, Stat.Intelligence) + "  Speed: " + GetPlayerStat(i, Stat.Spirit), (int)ColorName.Yellow);
                n = GetPlayerStat(i, Stat.Strength) / 2 + GetPlayerLevel(i) / 2;
                i = GetPlayerStat(i, Stat.Luck) / 2 + GetPlayerLevel(i) / 2;

                if (n > 100)
                    n = 100;

                if (i > 100)
                    i = 100;
                NetworkSend.SendPlayerMessage(session.Id, "Critical Hit Chance: " + n + "%, Block Chance: " + i + "%", (int)ColorName.Yellow);
            }
        }
        else
        {
            NetworkSend.SendPlayerMessage(session.Id, "Player is not online.", (int)ColorName.BrightRed);
        }
    }

    public static void Packet_WarpMeTo(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Mapper)
            return;

        // The player
        var n = GameLogic.FindPlayer(buffer.ReadString());

        if (n != session.Id)
        {
            if (n >= 0)
            {
                Server.Player.OnWarp(session.Id, GetPlayerMap(n), GetPlayerX(n), GetPlayerY(n), (byte)Direction.Down);
                NetworkSend.SendPlayerMessage(n, GetPlayerName(session.Id) + " has warped to you.", (int)ColorName.Yellow);
                NetworkSend.SendPlayerMessage(session.Id, "You have been warped to " + GetPlayerName(n) + ".", (int)ColorName.Yellow);
                Log.Add(GetPlayerName(session.Id) + " has warped to " + GetPlayerName(n) + ", map #" + GetPlayerMap(n) + ".", Constant.AdminLog);
            }
            else
            {
                NetworkSend.SendPlayerMessage(session.Id, "Player is not online.", (int)ColorName.BrightRed);
            }
        }
        else
        {
            NetworkSend.SendPlayerMessage(session.Id, "You cannot warp to yourself, dumbass!", (int)ColorName.BrightRed);
        }
    }

    public static void Packet_WarpToMe(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Mapper)
            return;

        // The player
        var n = GameLogic.FindPlayer(buffer.ReadString());


        if (n != session.Id)
        {
            if (n >= 0)
            {
                Server.Player.OnWarp(n, GetPlayerMap(session.Id), GetPlayerX(session.Id), GetPlayerY(session.Id), (byte)Direction.Down);
                NetworkSend.SendPlayerMessage(n, "You have been summoned by " + GetPlayerName(session.Id) + ".", (int)ColorName.Yellow);
                NetworkSend.SendPlayerMessage(session.Id, GetPlayerName(n) + " has been summoned.", (int)ColorName.Yellow);
                Log.Add(GetPlayerName(session.Id) + " has warped " + GetPlayerName(n) + " to self, map #" + GetPlayerMap(session.Id) + ".", Constant.AdminLog);
            }
            else
            {
                NetworkSend.SendPlayerMessage(session.Id, "Player is not online.", (int)ColorName.BrightRed);
            }
        }
        else
        {
            NetworkSend.SendPlayerMessage(session.Id, "You cannot warp yourself to yourself, dumbass!", (int)ColorName.BrightRed);
        }
    }

    public static void Packet_WarpTo(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Mapper)
            return;

        // The map
        var n = buffer.ReadInt32();

        // Prevent hacking
        if (n < 0 | n > Core.Globals.Variables.MaxMaps)
            return;

        Server.Player.OnWarp(session.Id, n, GetPlayerX(session.Id), GetPlayerY(session.Id), (byte)Direction.Down);
        NetworkSend.SendPlayerMessage(session.Id, "You have been warped to map #" + n, (int)ColorName.Yellow);
        Log.Add(GetPlayerName(session.Id) + " warped to map #" + n + ".", Constant.AdminLog);
    }

    public static void Packet_SetSprite(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Mapper)
            return;

        // The sprite
        var n = buffer.ReadInt32();


        SetPlayerSprite(session.Id, n);
        NetworkSend.SendPlayerData(session.Id);
    }

    public static void Packet_GetStats(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        NetworkSend.SendPlayerMessage(session.Id, "Stats: " + GetPlayerName(session.Id), (int)ColorName.Yellow);
        NetworkSend.SendPlayerMessage(session.Id, "Level: " + GetPlayerLevel(session.Id) + "  Exp: " + GetPlayerExperience(session.Id) + "/" + Script.Instance?.GetPlayerNextLevel(session.Id), (int)ColorName.Yellow);
        NetworkSend.SendPlayerMessage(session.Id, "HP: " + GetPlayerVital(session.Id, Core.Globals.Vital.Health) + "/" + Script.Instance?.GetPlayerMaxVital(session.Id, Core.Globals.Vital.Health) + "  MP: " + GetPlayerVital(session.Id, Core.Globals.Vital.Stamina) + "/" + Script.Instance?.GetPlayerMaxVital(session.Id, Core.Globals.Vital.Stamina) + "  SP: " + GetPlayerVital(session.Id, Core.Globals.Vital.Stamina) + "/" + Script.Instance?.GetPlayerMaxVital(session.Id, Core.Globals.Vital.Stamina), (int)ColorName.Yellow);
        NetworkSend.SendPlayerMessage(session.Id, "STR: " + GetPlayerStat(session.Id, Stat.Strength) + "  DEF: " + GetPlayerStat(session.Id, Stat.Luck) + "  MAGI: " + GetPlayerStat(session.Id, Stat.Intelligence) + "  Speed: " + GetPlayerStat(session.Id, Stat.Spirit), (int)ColorName.Yellow);
        var n = GetPlayerStat(session.Id, Stat.Strength) / 2;
        var n2 = GetPlayerStat(session.Id, Stat.Intelligence) / 2;
        var i = GetPlayerStat(session.Id, Stat.Vitality) / 5;

        if (n > 100)
            n = 100;

        if (n2 > 100)
            n2 = 100;

        if (i > 100)
            i = 100;
        NetworkSend.SendPlayerMessage(session.Id, "Critical Hit Chance: " + n + "% Critical Cast Chance: " + n2 + "%, Block Chance: " + i + "%", (int)ColorName.Yellow);
    }

    public static void Packet_RequestNewMap(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);
        var dir = buffer.ReadInt32();

        Server.Player.OnMove(session.Id, dir, 1, true);
    }

    public static void Packet_MapData(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        int x;
        int y;

        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Mapper)
        {
            return;
        }

        var map = GetPlayerMap(session.Id);

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

        Server.Map.Instance[map].Tile = new Type.Tile[Server.Map.Instance[map].MaxX, Server.Map.Instance[map].MaxY];

        for (x = 0; x < Core.Globals.Variables.MaxMapNpcs; x++)
        {
            MapNpc.Clear(x, map);
            Server.Map.Instance[map].Npc[x] = packetReader.ReadInt32();
        }

        var instance = Server.Map.Instance[map];
        var loopTo1 = (int)instance.MaxX;
        for (x = 0; x < loopTo1; x++)
        {
            var loopTo2 = (int)instance.MaxY;
            for (y = 0; y < loopTo2; y++)
            {
                instance.Tile[x, y].Data1 = packetReader.ReadInt32();
                instance.Tile[x, y].Data2 = packetReader.ReadInt32();
                instance.Tile[x, y].Data3 = packetReader.ReadInt32();
                instance.Tile[x, y].Data1_2 = packetReader.ReadInt32();
                instance.Tile[x, y].Data2_2 = packetReader.ReadInt32();
                instance.Tile[x, y].Data3_2 = packetReader.ReadInt32();
                instance.Tile[x, y].DirBlock = (byte)packetReader.ReadInt32();
                var loopTo3 = Enum.GetValues(typeof(MapLayer)).Length;
                instance.Tile[x, y].Layer = new Type.Layer[loopTo3];
                for (var i = 0; i < (int)loopTo3; i++)
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
            var loopTo4 = Server.Map.Instance[map].EventCount;
            for (var i = 0; i < loopTo4; i++)
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

                    var loopTo5 = Server.Map.Instance[map].Event[i].PageCount;
                    for (x = 0; x < (int)loopTo5; x++)
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
                                var loopTo6 = instance2.MoveRouteCount;
                                for (y = 0; y < (int)loopTo6; y++)
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

                            instance2.IdleAnim = packetReader.ReadInt32();
                            instance2.DirFix = packetReader.ReadInt32();
                            instance2.WalkThrough = packetReader.ReadInt32();
                            instance2.ShowName = packetReader.ReadInt32();
                            instance2.Trigger = packetReader.ReadByte();
                            instance2.CommandListCount = packetReader.ReadInt32();
                            instance2.Position = packetReader.ReadByte();
                        }

                        if (Server.Map.Instance[map].Event[i].Pages[x].CommandListCount > 0)
                        {
                            Server.Map.Instance[map].Event[i].Pages[x].CommandList = new Type.CommandList[Server.Map.Instance[map].Event[i].Pages[x].CommandListCount];
                            var loopTo7 = Server.Map.Instance[map].Event[i].Pages[x].CommandListCount;
                            for (y = 0; y < (int)loopTo7; y++)
                            {
                                Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].CommandCount = packetReader.ReadInt32();
                                Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].ParentList = packetReader.ReadInt32();
                                if (Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].CommandCount > 0)
                                {
                                    Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].Commands = new Type.EventCommand[Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].CommandCount];
                                    for (int z = 0, loopTo8 = Server.Map.Instance[map].Event[i].Pages[x].CommandList[y].CommandCount; z < (int)loopTo8; z++)
                                    {
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
                                                for (int w = 0, loopTo9 = tmpCount; w < (int)loopTo9; w++)
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
        }

        var loopTo13 = Server.Map.Instance[map].EventCount;
        for (var i = 0; i < loopTo13; i++)
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
        MapNpc.OnSpawn(map).GetAwaiter().GetResult();
        EventLogic.SpawnGlobalEvents(map).GetAwaiter().GetResult();

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
        var loopTo11 = Core.Globals.Variables.MaxMapItems;
        for (var i = 0; i < loopTo11; i++)
        {
            MapItem.OnClear(i, GetPlayerMap(session.Id));
        }

        // Respawn
        MapItem.Spawn(GetPlayerMap(session.Id));
        MapResource.OnUpdate(map);

        // Refresh map for everyone online
        foreach (var i in PlayerService.Instance.PlayerIds)
        {
            if (NetworkConfig.IsPlaying(i) & GetPlayerMap(i) == map)
            {
                Server.Player.OnWarp(i, map, GetPlayerX(i), GetPlayerY(i), (byte)Direction.Down);
                NetworkSend.SendMapData(i, map, true);
            }
        }
    }

    private static void Packet_NeedMap(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Get yes/no value
        var s = buffer.ReadInt32();

        // Check if data is needed to be sent
        if (s == 1)
        {
            NetworkSend.SendMapData(session.Id, GetPlayerMap(session.Id), true);
        }
        else
        {
            NetworkSend.SendMapData(session.Id, GetPlayerMap(session.Id), false);
        }

        if (Server.Map.Instance[GetPlayerMap(session.Id)].Shop >= 0 && Server.Map.Instance[GetPlayerMap(session.Id)].Shop < Core.Globals.Variables.MaxShops)
        {
            var shop = Server.Map.Instance[GetPlayerMap(session.Id)].Shop;
            if (shop >= 0 && shop < Shop.Instance.Count && !string.IsNullOrEmpty(Shop.Instance[shop].Name))
            {
                Data.TempPlayer[session.Id].InShop = shop;
                NetworkSend.SendOpenShop(session.Id, (int)Data.TempPlayer[session.Id].InShop);
            }
        }

        NetworkSend.SendJoinMap(session.Id);

        Data.TempPlayer[session.Id].GettingMap = false;
    }

    public static void Packet_RespawnMap(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        int i;
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Mapper)
            return;

        // Clear out it all
        var loopTo = Core.Globals.Variables.MaxMapItems;
        for (i = 0; i < loopTo; i++)
        {
            MapItem.OnClear(i, GetPlayerMap(session.Id));
        }

        // Respawn
        MapItem.Spawn(GetPlayerMap(session.Id));

        // Respawn NpcS
        var loopTo1 = Core.Globals.Variables.MaxMapNpcs;
        for (i = 0; i < loopTo1; i++)
            MapNpc.OnSpawn(i, GetPlayerMap(session.Id));

        EventLogic.SpawnMapEventsFor(session.Id, GetPlayerMap(session.Id));

        MapResource.OnUpdate(GetPlayerMap(session.Id));
        NetworkSend.SendPlayerMessage(session.Id, "Map respawned.", (int)ColorName.BrightGreen);
        Log.Add(GetPlayerName(session.Id) + " has respawned map #" + GetPlayerMap(session.Id), Constant.AdminLog);
    }

    public static void Packet_MapReport(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Mapper)
            return;

        NetworkSend.SendMapReport(session.Id);
    }

    public static void Packet_KickPlayer(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Moderator)
        {
            return;
        }

        // The player session.Id
        var n = GameLogic.FindPlayer(buffer.ReadString());

        if (n != session.Id)
        {
            if (n >= 0)
            {
                if (GetPlayerAccess(n) < GetPlayerAccess(session.Id))
                {
                    NetworkSend.SendGlobalMessage(GetPlayerName(n) + " has been kicked from " + SettingsManager.Instance.GameName + " by " + GetPlayerName(session.Id) + "!");
                    Log.Add(GetPlayerName(session.Id) + " has kicked " + GetPlayerName(n) + ".", Constant.AdminLog);
                    NetworkSend.SendAlert(session, SystemMessage.Kicked, Menu.Login);
                }
                else
                {
                    NetworkSend.SendPlayerMessage(session.Id, "That is a higher or same access admin then you!", (int)ColorName.BrightRed);
                }
            }
            else
            {
                NetworkSend.SendPlayerMessage(session.Id, "Player is not online.", (int)ColorName.BrightRed);
            }
        }
        else
        {
            NetworkSend.SendPlayerMessage(session.Id, "You cannot kick yourself!", (int)ColorName.BrightRed);
        }
    }

    public static void Packet_Banlist(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Moderator)
        {
            return;
        }

        NetworkSend.SendPlayerMessage(session.Id, "Command /banlist is not available.", (int)ColorName.Yellow);
    }

    public static void Packet_DestroyBans(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Owner)
            return;

        var filename = System.IO.Path.Combine(DataPath.Database, "banlist.txt");

        if (File.Exists(filename))
            File.Delete(filename);

        NetworkSend.SendPlayerMessage(session.Id, "Ban list destroyed.", (int)ColorName.BrightGreen);
    }

    public static void Packet_BanPlayer(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Moderator)
            return;

        // The player session.Id
        var n = GameLogic.FindPlayer(buffer.ReadString());

        if (n != session.Id)
        {
            if (n >= 0)
            {
                if (GetPlayerAccess(n) < GetPlayerAccess(session.Id))
                {
                    Database.BanPlayer(n, session.Id);
                }
                else
                {
                    NetworkSend.SendPlayerMessage(session.Id, "That is a higher or same access admin then you!", (int)ColorName.BrightRed);
                }
            }
            else
            {
                NetworkSend.SendPlayerMessage(session.Id, "Player is not online.", (int)ColorName.BrightRed);
            }
        }
        else
        {
            NetworkSend.SendPlayerMessage(session.Id, "You cannot ban yourself!", (int)ColorName.BrightRed);
        }
    }

    private static void Packet_RequestEditMap(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Mapper)
            return;

        var user = IsEditorLocked(session.Id, EditorType.Map);

        if (!string.IsNullOrEmpty(user))
        {
            NetworkSend.SendPlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        NetworkSend.SendNpcs(session.Id);
        NetworkSend.SendItems(session.Id);
        NetworkSend.SendAnimations(session.Id);
        NetworkSend.SendShops(session.Id);
        NetworkSend.SendResources(session.Id);
        NetworkSend.SendMapEventData(session.Id);
        NetworkSend.SendMorals(session.Id);

        Data.TempPlayer[session.Id].Editor = EditorType.Map;

        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ServerPackets.SEditMap);

        PlayerService.Instance.SendDataTo(session.Id, packetWriter.GetBytes());
    }

    public static void Packet_RequestEditShop(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
            return;

        var user = IsEditorLocked(session.Id, EditorType.Shop);

        if (!string.IsNullOrEmpty(user))
        {
            NetworkSend.SendPlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Shop;

        NetworkSend.SendItems(session.Id);
        NetworkSend.SendShops(session.Id);

        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ServerPackets.SShopEditor);

        PlayerService.Instance.SendDataTo(session.Id, packetWriter.GetBytes());
    }

    public static void Packet_SaveShop(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
            return;

        var shopNum = buffer.ReadInt32();

        // Prevent hacking
        if (shopNum < 0 | shopNum > Core.Globals.Variables.MaxShops)
            return;

        for (var i = 0; i <= shopNum; i++)
        {
            if (Shop.Instance.Count <= i)
            {
                Shop.Instance.Add(new Shop());
            }
        }

        Shop.Instance[shopNum].BuyRate = buffer.ReadInt32();
        Shop.Instance[shopNum].Name = buffer.ReadString();

        for (int i = 0, loopTo = Core.Globals.Variables.MaxTrades; i < loopTo; i++)
        {
            Shop.Instance[shopNum].TradeItem[i].CostItem = buffer.ReadInt32();
            Shop.Instance[shopNum].TradeItem[i].CostValue = buffer.ReadInt32();
            Shop.Instance[shopNum].TradeItem[i].Item = buffer.ReadInt32();
            Shop.Instance[shopNum].TradeItem[i].ItemValue = buffer.ReadInt32();
        }


        // Save it
        NetworkSend.SendUpdateShopToAll(shopNum);
        Shop.OnSave(shopNum);
        Log.Add(GetAccountLogin(session.Id) + " saving shop #" + shopNum + ".", Constant.AdminLog);
    }

    public static void Packet_RequestEditSkill(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
            return;

        var user = IsEditorLocked(session.Id, EditorType.Skill);

        if (!string.IsNullOrEmpty(user))
        {
            NetworkSend.SendPlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Skill;

        NetworkSend.SendJobs(session);
        NetworkSend.SendProjectiles(session.Id);
        NetworkSend.SendAnimations(session.Id);
        NetworkSend.SendSkills(session.Id);

        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ServerPackets.SSkillEditor);

        PlayerService.Instance.SendDataTo(session.Id, packetWriter.GetBytes());
    }

    public static void Packet_SaveSkill(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var skillNum = buffer.ReadInt32();

        // Prevent hacking
        if (skillNum < 0 | skillNum > Core.Globals.Variables.MaxSkills)
            return;

        for (int i = 0; i <= skillNum; i++)
        {
            if (Skill.Instance.Count <= i)
            {
                Skill.Instance.Add(new Skill());
            }
        }

        Skill.Instance[skillNum].AccessReq = buffer.ReadInt32();
        Skill.Instance[skillNum].AoE = buffer.ReadInt32();
        Skill.Instance[skillNum].CastAnim = buffer.ReadInt32();
        Skill.Instance[skillNum].CastTime = buffer.ReadInt32();
        Skill.Instance[skillNum].CdTime = buffer.ReadInt32();
        Skill.Instance[skillNum].JobReq = buffer.ReadInt32();
        Skill.Instance[skillNum].Dir = buffer.ReadByte();
        Skill.Instance[skillNum].Duration = buffer.ReadInt32();
        Skill.Instance[skillNum].Icon = buffer.ReadInt32();
        Skill.Instance[skillNum].Interval = buffer.ReadInt32();
        Skill.Instance[skillNum].IsAoE = buffer.ReadBoolean();
        Skill.Instance[skillNum].LevelReq = buffer.ReadInt32();
        Skill.Instance[skillNum].Map = buffer.ReadInt32();
        Skill.Instance[skillNum].MpCost = buffer.ReadInt32();
        Skill.Instance[skillNum].Name = buffer.ReadString();
        Skill.Instance[skillNum].Range = buffer.ReadInt32();
        Skill.Instance[skillNum].SkillAnim = buffer.ReadInt32();
        Skill.Instance[skillNum].StunDuration = buffer.ReadInt32();
        Skill.Instance[skillNum].Type = buffer.ReadByte();
        Skill.Instance[skillNum].Vital = buffer.ReadInt32();
        Skill.Instance[skillNum].X = buffer.ReadInt32();
        Skill.Instance[skillNum].Y = buffer.ReadInt32();

        // projectiles
        Skill.Instance[skillNum].IsProjectile = buffer.ReadInt32();
        Skill.Instance[skillNum].Projectile = buffer.ReadInt32();

        Skill.Instance[skillNum].KnockBack = buffer.ReadByte();
        Skill.Instance[skillNum].KnockBackTiles = buffer.ReadByte();
        Skill.Instance[skillNum].MultiDirMask = buffer.ReadInt32();
        
        // chain skills
        Skill.Instance[skillNum].ChainOnHitSkillId = buffer.ReadInt32();

        // common event fields
        Skill.Instance[skillNum].CommonEventType = buffer.ReadByte();
        Skill.Instance[skillNum].CommonEventData1 = buffer.ReadInt32();
        Skill.Instance[skillNum].CommonEventData2 = buffer.ReadInt32();

        // Save it
        NetworkSend.SendUpdateSkillToAll(skillNum);
        Skill.OnSave(skillNum);
        Log.Add(GetAccountLogin(session.Id) + " saved Skill #" + skillNum + ".", Constant.AdminLog);
    }

    public static void Packet_SetAccess(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Owner)
            return;

        // The session.Id
        var n = GameLogic.FindPlayer(buffer.ReadString());

        // The access
        var i = buffer.ReadInt32();

        // Check for invalid access level
        if (i >= (int)AccessLevel.Player && i <= (int)AccessLevel.Owner)
        {
            // Check if player is on
            if (n >= 0)
            {
                if (n != session.Id)
                {
                    // check to see if same level access is trying to change another access of the very same level and boot them if they are.
                    if (GetPlayerAccess(n) == GetPlayerAccess(session.Id))
                    {
                        NetworkSend.SendPlayerMessage(session.Id, "Invalid access level.", (int)ColorName.BrightRed);
                        return;
                    }
                }

                if (GetPlayerAccess(n) == (int)AccessLevel.Player && i > (int)AccessLevel.Player)
                {
                    NetworkSend.SendGlobalMessage(GetPlayerName(n) + " has been blessed with administrative access.");
                }

                SetPlayerAccess(n, (byte)i);
                NetworkSend.SendPlayerData(n);
                Log.Add(GetPlayerName(session.Id) + " has modified " + GetPlayerName(n) + "'s access.", Constant.AdminLog);
            }
            else
            {
                NetworkSend.SendPlayerMessage(session.Id, "Player is not online.", (int)ColorName.BrightRed);
            }
        }
        else
        {
            NetworkSend.SendPlayerMessage(session.Id, "Invalid access level.", (int)ColorName.BrightRed);
        }
    }

    public static void Packet_WhosOnline(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        NetworkSend.SendWhosOnline(session.Id);
    }

    public static void Packet_SetMotd(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Mapper)
            return;

        Variables.Welcome = buffer.ReadString();
        SettingsManager.Save();

        NetworkSend.SendGlobalMessage("Welcome changed to: " + Variables.Welcome);
        Log.Add(GetPlayerName(session.Id) + " changed welcome to: " + Variables.Welcome, Constant.AdminLog);
    }

    public static void Packet_PlayerSearch(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var x = buffer.ReadInt32();
        var y = buffer.ReadInt32();
        var rclick = (byte)buffer.ReadInt32();

        // Prevent subscript out of range
        if (x < 0 | x > (int)Server.Map.Instance[GetPlayerMap(session.Id)].MaxX | y < 0 | y > (int)Server.Map.Instance[GetPlayerMap(session.Id)].MaxY)
            return;

        // Check for a player   
        foreach (var i in PlayerService.Instance.PlayerIds)
        {
            if (GetPlayerMap(session.Id) == GetPlayerMap(i))
            {
                if (GetPlayerX(i) == x)
                {
                    if (GetPlayerY(i) == y)
                    {
                        // Consider the player
                        if (i != session.Id)
                        {
                            if (GetPlayerLevel(i) >= GetPlayerLevel(session.Id) + 5)
                            {
                                NetworkSend.SendPlayerMessage(session.Id, "You wouldn't stand a chance.", (int)ColorName.BrightRed);
                            }

                            else if (GetPlayerLevel(i) > GetPlayerLevel(session.Id))
                            {
                                NetworkSend.SendPlayerMessage(session.Id, "This one seems to have an advantage over you.", (int)ColorName.Yellow);
                            }

                            else if (GetPlayerLevel(i) == GetPlayerLevel(session.Id))
                            {
                                NetworkSend.SendPlayerMessage(session.Id, "This would be an even fight.", (int)ColorName.White);
                            }

                            else if (GetPlayerLevel(session.Id) >= GetPlayerLevel(i) + 5)
                            {
                                NetworkSend.SendPlayerMessage(session.Id, "You could slaughter that player.", (int)ColorName.BrightBlue);
                            }

                            else if (GetPlayerLevel(session.Id) > GetPlayerLevel(i))
                            {
                                NetworkSend.SendPlayerMessage(session.Id, "You would have an advantage over that player.", (int)ColorName.BrightCyan);
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
                            NetworkSend.SendPlayerMessage(session.Id, "Your target is now " + GetPlayerName(i) + ".", (int)ColorName.Yellow);
                        }

                        NetworkSend.SendTarget(session.Id, Data.TempPlayer[session.Id].Target, Data.TempPlayer[session.Id].TargetType);
                        if (rclick == 1)
                            NetworkSend.SendRightClick(session.Id);
                        return;
                    }
                }
            }
        }

        // Check for an item
        var loopTo1 = Core.Globals.Variables.MaxMapItems;
        for (var i = 0; i < loopTo1; i++)
        {
            if (MapItem.Instance[GetPlayerMap(session.Id), i].Num >= 0)
            {
                if (!string.IsNullOrEmpty(Item.Instance[(int)MapItem.Instance[GetPlayerMap(session.Id), i].Num].Name))
                {
                    if (Math.Floor((double)MapItem.Instance[GetPlayerMap(session.Id), i].X / Constants.TileSize) == x)
                    {
                        if (Math.Floor((double)MapItem.Instance[GetPlayerMap(session.Id), i].Y / Constants.TileSize) == y)
                        {
                            NetworkSend.SendPlayerMessage(session.Id, "You see " + MapItem.Instance[GetPlayerMap(session.Id), i].Value + " " + Item.Instance[(int)MapItem.Instance[GetPlayerMap(session.Id), i].Num].Name + ".", (int)ColorName.BrightGreen);
                            return;
                        }
                    }
                }
            }
        }

        // Check for an npc
        var loopTo2 = Core.Globals.Variables.MaxMapNpcs;
        for (var i = 0; i < loopTo2; i++)
        {
            if (MapNpc.Instance[GetPlayerMap(session.Id), i].Num >= 0)
            {
                if (Math.Floor((double)MapNpc.Instance[GetPlayerMap(session.Id), i].X / Constants.TileSize) == x)
                {
                    if (Math.Floor((double)MapNpc.Instance[GetPlayerMap(session.Id), i].Y / Constants.TileSize) == y)
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
                            NetworkSend.SendPlayerMessage(session.Id, "Your target is now " + GameLogic.CheckGrammar(Npc.Instance[(int)MapNpc.Instance[GetPlayerMap(session.Id), i].Num].Name) + ".", (int)ColorName.Yellow);
                        }

                        NetworkSend.SendTarget(session.Id, Data.TempPlayer[session.Id].Target, Data.TempPlayer[session.Id].TargetType);
                        return;
                    }
                }
            }
        }
    }

    public static void Packet_Skills(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        NetworkSend.SendPlayerSkills(session.Id);
    }

    public static void Packet_Cast(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Skill slot
        var n = buffer.ReadInt32();

        try
        {
            Script.Instance?.BufferSkill(session.Id, n);
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(Packet_Cast));
        }
    }

    public static void Packet_SwapInvSlots(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        if (Data.TempPlayer[session.Id].InTrade > 0 | Data.TempPlayer[session.Id].InBank | Data.TempPlayer[session.Id].InShop >= 0)
            return;

        // Old Slot
        double oldSlot = buffer.ReadInt32();
        double newSlot = buffer.ReadInt32();


        Server.Player.PlayerSwitchInvSlots(session.Id, (int)oldSlot, (int)newSlot);
    }

    public static void Packet_SwapSkillSlots(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        if (Data.TempPlayer[session.Id].InTrade > 0 | Data.TempPlayer[session.Id].InBank | Data.TempPlayer[session.Id].InShop >= 0)
            return;

        // Old Slot
        double oldSlot = buffer.ReadInt32();
        double newSlot = buffer.ReadInt32();


        Server.Player.PlayerSwitchSkillSlots(session.Id, (int)oldSlot, (int)newSlot);
    }

    public static void Packet_CheckPing(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ServerPackets.SSendPing);

        PlayerService.Instance.SendDataTo(session.Id, packetWriter.GetBytes());
    }

    public static void Packet_UnEquip(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);
        int eqSlot = buffer.ReadInt32();
        int m = Server.Player.FindOpenInvSlot(session.Id, (int)PlayerBase.Instance[session.Id].Paperdoll[eqSlot].Num);
        Server.Player.RemoveEquipment(session.Id, eqSlot, m);
    }

    public static void Packet_RequestPlayerData(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        NetworkSend.SendPlayerData(session.Id);
    }

    public static void Packet_RequestNpc(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var n = buffer.ReadInt32();

        if (n < 0 | n > Core.Globals.Variables.MaxNpcs)
            return;

        NetworkSend.SendUpdateNpcTo(session.Id, n);
    }

    public static void Packet_SpawnItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // item
        var tmpItem = buffer.ReadInt32();
        var tmpAmount = buffer.ReadInt32();

        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
            return;

        MapItem.OnSpawn(tmpItem, tmpAmount, GetPlayerMap(session.Id), GetPlayerX(session.Id), GetPlayerY(session.Id));
    }

    public static void Packet_TrainStat(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // check points
        if (GetPlayerPoints(session.Id) == 0)
            return;

        // stat
        var tmpStat = buffer.ReadInt32();

        try
        {
            Script.Instance?.OnTrain(session.Id, tmpStat);
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(Packet_TrainStat));
        }
    }

    public static void Packet_RequestSkill(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var n = buffer.ReadInt32();

        if (n < 0 | n > Core.Globals.Variables.MaxSkills)
            return;

        NetworkSend.SendUpdateSkillTo(session.Id, n);
    }

    public static void Packet_RequestShop(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var n = buffer.ReadInt32();

        if (n < 0 | n > Core.Globals.Variables.MaxShops)
            return;

        NetworkSend.SendUpdateShopTo(session.Id, n);
    }

    public static void Packet_RequestLevelUp(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
            return;

        SetPlayerExperience(session.Id, Script.Instance?.GetPlayerNextLevel(session.Id));
        Server.Player.OnLevel(session.Id);
    }

    public static void Packet_ForgetSkill(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var skillSlot = buffer.ReadInt32();

        // Check for subscript out of range
        if (skillSlot < 0 | skillSlot > Core.Globals.Variables.MaxPlayerSkills)
            return;

        // dont let them forget a skill which is in CD
        if (Data.TempPlayer[session.Id].SkillCd[skillSlot] > 0)
        {
            NetworkSend.SendPlayerMessage(session.Id, "Cannot forget a skill which is cooling down!", (int)ColorName.BrightRed);
            return;
        }

        // dont let them forget a skill which is buffered
        if (Data.TempPlayer[session.Id].SkillBuffer == skillSlot)
        {
            NetworkSend.SendPlayerMessage(session.Id, "Cannot forget a skill which you are casting!", (int)ColorName.BrightRed);
            return;
        }

        PlayerBase.Instance[session.Id].Skill[skillSlot].Num = -1;
        NetworkSend.SendPlayerSkills(session.Id);
    }

    public static void Packet_CloseShop(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Data.TempPlayer[session.Id].InShop = -1;
    }

    public static void Packet_BuyItem(GameSession session, ReadOnlyMemory<byte> bytes)
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
            NetworkSend.SendPlayerMessage(session.Id, "You do not have enough to buy this item.", (int)ColorName.BrightRed);
            NetworkSend.ResetShopAction();
            return;
        }

        // it's fine, let's go ahead
        for (int i = 0, loopTo = instance.CostValue; i < loopTo; i++)
            Server.Player.TakeInv(session.Id, instance.CostItem, instance.CostValue);
        Server.Player.GiveInv(session.Id, instance.Item, instance.ItemValue);

        // send confirmation message & reset their shop action
        NetworkSend.SendPlayerMessage(session.Id, "Trade successful.", (int)ColorName.BrightGreen);
        NetworkSend.ResetShopAction();
    }

    public static void Packet_SellItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var invSlot = buffer.ReadInt32();

        // if invalid, exit out
        if (invSlot < 0 || invSlot > Core.Globals.Variables.MaxInventory)
            return;

        // has item?
        if (GetPlayerInventory(session.Id, invSlot) < 0 || GetPlayerInventory(session.Id, invSlot) > Core.Globals.Variables.MaxItems)
            return;

        // seems to be valid
        double itemNum = GetPlayerInventory(session.Id, invSlot);
        var shopNum = Data.TempPlayer[session.Id].InShop;

        if (shopNum < 0 || shopNum > Core.Globals.Variables.MaxShops)
        {
            return;
        }

        // work out price
        var multiplier = Shop.Instance[(int)shopNum].BuyRate / 100d;
        var price = (int)Math.Round(Item.Instance[(int)itemNum].Price * multiplier);

        // item has cost?
        if (price < 0)
        {
            NetworkSend.SendPlayerMessage(session.Id, "The shop doesn't want that item.", (int)ColorName.Yellow);
            NetworkSend.ResetShopAction();
            return;
        }

        // take item and give gold
        Server.Player.TakeInv(session.Id, (int)itemNum, 1);
        Server.Player.GiveInv(session.Id, 0, price);

        // send confirmation message & reset their shop action
        NetworkSend.SendPlayerMessage(session.Id, "Sold the " + Item.Instance[(int)itemNum].Name + " for " + price + " " + Item.Instance[(int)itemNum].Name + "!", (int)ColorName.BrightGreen);
        NetworkSend.ResetShopAction();
    }

    public static void Packet_ChangeBankSlots(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var oldslot = buffer.ReadInt32();
        var newslot = buffer.ReadInt32();

        Server.Player.PlayerSwitchBankSlots(session.Id, oldslot, newslot);
    }

    public static void Packet_DepositItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var invslot = buffer.ReadInt32();
        var amount = buffer.ReadInt32();

        Server.Player.GiveBank(session.Id, invslot, amount);
    }

    public static void Packet_WithdrawItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var bankSlot = buffer.ReadByte();
        var amount = buffer.ReadInt32();

        Server.Player.TakeBank(session.Id, bankSlot, amount);
    }

    public static void Packet_CloseBank(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Data.TempPlayer[session.Id].InBank = false;
    }

    public static void Packet_AdminWarp(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var x = buffer.ReadInt32();
        var y = buffer.ReadInt32();

        if (x < 0 || x >= Server.Map.Instance[GetPlayerMap(session.Id)].MaxX || y < 0 || y >= Server.Map.Instance[GetPlayerMap(session.Id)].MaxY)
            return;

        x *= 32;
        y *= 32;

        if (GetPlayerAccess(session.Id) >= (byte)AccessLevel.Mapper)
        {
            PlayerBase.Instance[session.Id].IsMoving = false;

            // Set the information
            SetPlayerX(session.Id, x);
            SetPlayerY(session.Id, y);
            SetPlayerDir(session.Id, (byte)Direction.Down);
            NetworkSend.SendPlayerXYToMap(session.Id);
        }
    }

    public static void Packet_TradeInvite(GameSession session, ReadOnlyMemory<byte> bytes)
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
            NetworkSend.SendPlayerMessage(session.Id, "You can't trade with yourself!", (int)ColorName.BrightRed);
            return;
        }

        // send the trade request
        Data.TempPlayer[session.Id].TradeRequest = tradeTarget;
        Data.TempPlayer[tradeTarget].TradeRequest = session.Id;

        NetworkSend.SendPlayerMessage(tradeTarget, GetPlayerName(session.Id) + " has invited you to trade.", (int)ColorName.Yellow);
        NetworkSend.SendPlayerMessage(session.Id, "You have invited " + GetPlayerName(tradeTarget) + " to trade.", (int)ColorName.BrightGreen);

        NetworkSend.SendTradeInvite(tradeTarget, session.Id);
    }

    public static void Packet_HandleTradeInvite(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var status = (byte)buffer.ReadInt32();

        var tradeTarget = Data.TempPlayer[session.Id].TradeRequest;

        if (tradeTarget < 0 | tradeTarget >= Core.Globals.Variables.MaxPlayers)
            return;

        if (status == 0)
        {
            NetworkSend.SendPlayerMessage(tradeTarget, GetPlayerName(session.Id) + " has declined your trade request.", (int)ColorName.BrightRed);
            NetworkSend.SendPlayerMessage(session.Id, "You have declined the trade with " + GetPlayerName(tradeTarget) + ".", (int)ColorName.BrightRed);
            Data.TempPlayer[session.Id].TradeRequest = -1;
            return;
        }

        // Let them tradetradeTarget
        if (Data.TempPlayer[tradeTarget].TradeRequest == session.Id)
        {
            // let them know they're trading
            NetworkSend.SendPlayerMessage(session.Id, "You have accepted " + GetPlayerName(tradeTarget) + "'s trade request.", (int)ColorName.Yellow);
            NetworkSend.SendPlayerMessage(tradeTarget, GetPlayerName(session.Id) + " has accepted your trade request.", (int)ColorName.BrightGreen);

            // clear the tradeRequest server-side
            Data.TempPlayer[session.Id].TradeRequest = -1;
            Data.TempPlayer[tradeTarget].TradeRequest = -1;

            // set that they're trading with each other
            Data.TempPlayer[session.Id].InTrade = tradeTarget;

            // clear out their trade offers
            Data.TempPlayer[tradeTarget].InTrade = session.Id;

            Array.Resize(ref Data.TempPlayer[session.Id].TradeOffer, Core.Globals.Variables.MaxInventory);
            Array.Resize(ref Data.TempPlayer[tradeTarget].TradeOffer, Core.Globals.Variables.MaxInventory);

            for (int i = 0, loopTo = Core.Globals.Variables.MaxInventory; i < loopTo; i++)
            {
                Data.TempPlayer[session.Id].TradeOffer[i].Num = -1;
                Data.TempPlayer[session.Id].TradeOffer[i].Value = 0;
                Data.TempPlayer[tradeTarget].TradeOffer[i].Num = -1;
                Data.TempPlayer[tradeTarget].TradeOffer[i].Value = 0;
            }

            // Used to init the trade window clientside
            NetworkSend.SendTrade(session.Id, tradeTarget);
            NetworkSend.SendTrade(tradeTarget, session.Id);

            // Send the offer data - Used to clear their client
            NetworkSend.SendTradeUpdate(session.Id, 0);
            NetworkSend.SendTradeUpdate(session.Id, 1);
            NetworkSend.SendTradeUpdate(tradeTarget, 0);
            NetworkSend.SendTradeUpdate(tradeTarget, 1);
        }
    }

    public static void Packet_TradeInviteDecline(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Data.TempPlayer[session.Id].TradeRequest = -1;
    }

    public static void Packet_AcceptTrade(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        int itemNum;
        int i;
        var tmpTradeItem = new Type.Item[Core.Globals.Variables.MaxInventory];
        var tmpTradeItem2 = new Type.Item[Core.Globals.Variables.MaxInventory];

        Data.TempPlayer[session.Id].AcceptTrade = true;

        var tradeTarget = (int)Data.TempPlayer[session.Id].InTrade;

        // if not both of them accept, then exit
        if (!Data.TempPlayer[tradeTarget].AcceptTrade)
        {
            NetworkSend.SendTradeStatus(session.Id, 2);
            NetworkSend.SendTradeStatus(tradeTarget, 1);
            return;
        }

        // take their items
        var loopTo = Core.Globals.Variables.MaxInventory;
        for (i = 0; i < loopTo; i++)
        {
            tmpTradeItem[i].Num = -1;
            tmpTradeItem2[i].Num = -1;

            // player
            if (Data.TempPlayer[session.Id].TradeOffer[i].Num >= 0)
            {
                itemNum = (int)PlayerBase.Instance[session.Id].Inventory[(int)Data.TempPlayer[session.Id].TradeOffer[i].Num].Num;
                if (itemNum >= 0)
                {
                    // store temp
                    tmpTradeItem[i].Num = itemNum;
                    tmpTradeItem[i].Value = Data.TempPlayer[session.Id].TradeOffer[i].Value;
                    // take item
                    Server.Player.TakeInvSlot(session.Id, (int)Data.TempPlayer[session.Id].TradeOffer[i].Num, tmpTradeItem[i].Value);
                }
            }

            // target
            if (Data.TempPlayer[tradeTarget].TradeOffer[i].Num >= 0)
            {
                itemNum = GetPlayerInventory(tradeTarget, (int)Data.TempPlayer[tradeTarget].TradeOffer[i].Num);
                if (itemNum >= 0)
                {
                    // store temp
                    tmpTradeItem2[i].Num = itemNum;
                    tmpTradeItem2[i].Value = Data.TempPlayer[tradeTarget].TradeOffer[i].Value;
                    // take item
                    Server.Player.TakeInvSlot(tradeTarget, (int)Data.TempPlayer[tradeTarget].TradeOffer[i].Num, tmpTradeItem2[i].Value);
                }
            }
        }

        // taken all items. now they can't not get items because of no inventory space.
        var loopTo1 = Core.Globals.Variables.MaxInventory;
        for (i = 0; i < loopTo1; i++)
        {
            // player
            if (tmpTradeItem2[i].Num >= 0)
            {
                // give away!
                Server.Player.GiveInv(session.Id, (int)tmpTradeItem2[i].Num, tmpTradeItem2[i].Value, 0, false);
            }

            // target
            if (tmpTradeItem[i].Num >= 0)
            {
                // give away!
                Server.Player.GiveInv(tradeTarget, (int)tmpTradeItem[i].Num, tmpTradeItem[i].Value, 0, false);
            }
        }

        NetworkSend.SendInventory(session.Id);
        NetworkSend.SendInventory(tradeTarget);

        // they now have all the items. Clear out values + let them out of the trade.
        var loopTo2 = Core.Globals.Variables.MaxInventory;
        for (i = 0; i < loopTo2; i++)
        {
            Data.TempPlayer[session.Id].TradeOffer[i].Num = -1;
            Data.TempPlayer[session.Id].TradeOffer[i].Value = 0;
            Data.TempPlayer[tradeTarget].TradeOffer[i].Num = -1;
            Data.TempPlayer[tradeTarget].TradeOffer[i].Value = 0;
        }

        Data.TempPlayer[session.Id].InTrade = 0;
        Data.TempPlayer[tradeTarget].InTrade = 0;

        NetworkSend.SendPlayerMessage(session.Id, "Trade completed.", (int)ColorName.BrightGreen);
        NetworkSend.SendPlayerMessage(tradeTarget, "Trade completed.", (int)ColorName.BrightGreen);

        NetworkSend.SendCloseTrade(session.Id);
        NetworkSend.SendCloseTrade(tradeTarget);
    }

    public static void Packet_DeclineTrade(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var tradeTarget = (int)Data.TempPlayer[session.Id].InTrade;
        var hasValidTarget = tradeTarget >= 0 && tradeTarget < Core.Globals.Variables.MaxPlayers;

        for (int i = 0, loopTo = Core.Globals.Variables.MaxInventory; i < loopTo; i++)
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
        NetworkSend.SendPlayerMessage(session.Id, "You declined the trade.", (int)ColorName.BrightRed);
        NetworkSend.SendCloseTrade(session.Id);

        if (hasValidTarget)
        {
            Data.TempPlayer[tradeTarget].InTrade = 0;
            NetworkSend.SendPlayerMessage(tradeTarget, GetPlayerName(session.Id) + " has declined the trade.", (int)ColorName.BrightRed);
            NetworkSend.SendCloseTrade(tradeTarget);
        }
    }

    public static void Packet_TradeItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var emptyslot = default(int);
        int i;
        var buffer = new PacketReader(bytes);

        var invSlot = buffer.ReadInt32();
        var amount = buffer.ReadInt32();

        if (invSlot < 0 | invSlot > Core.Globals.Variables.MaxInventory)
            return;

        var itemNum = GetPlayerInventory(session.Id, invSlot);

        if (itemNum < 0 || itemNum > Core.Globals.Variables.MaxItems)
            return;

        // make sure they have the amount they offer
        if (amount < 0 || amount > GetPlayerInventoryValue(session.Id, invSlot))
            return;

        if (PlayerBase.Instance[session.Id].Inventory[invSlot].Bound > 0)
        {
            NetworkSend.SendPlayerMessage(session.Id, "You can't trade soulbound items.", (int)ColorName.BrightRed);
            return;
        }

        if (Item.Instance[itemNum].Type == (byte)ItemCategory.Currency | Item.Instance[itemNum].Stackable == 1)
        {
            // check if already offering same currency item
            var loopTo = Core.Globals.Variables.MaxInventory;
            for (i = 0; i < loopTo; i++)
            {
                if (Data.TempPlayer[session.Id].TradeOffer[i].Num == invSlot)
                {
                    // add amount
                    Data.TempPlayer[session.Id].TradeOffer[i].Value = Data.TempPlayer[session.Id].TradeOffer[i].Value + amount;

                    // clamp to limits
                    if (Data.TempPlayer[session.Id].TradeOffer[i].Value > GetPlayerInventoryValue(session.Id, invSlot))
                    {
                        Data.TempPlayer[session.Id].TradeOffer[i].Value = GetPlayerInventoryValue(session.Id, invSlot);
                    }

                    // cancel any trade agreement
                    Data.TempPlayer[session.Id].AcceptTrade = false;
                    Data.TempPlayer[(int)Data.TempPlayer[session.Id].InTrade].AcceptTrade = false;

                    NetworkSend.SendTradeStatus(session.Id, 0);
                    NetworkSend.SendTradeStatus((int)Data.TempPlayer[session.Id].InTrade, 1);

                    NetworkSend.SendTradeUpdate(session.Id, 0);
                    NetworkSend.SendTradeUpdate(session.Id, 1);
                    NetworkSend.SendTradeUpdate((int)Data.TempPlayer[session.Id].InTrade, 0);
                    NetworkSend.SendTradeUpdate((int)Data.TempPlayer[session.Id].InTrade, 1);
                    return;
                }
            }
        }
        else
        {
            // make sure they're not already offering it
            var loopTo1 = Core.Globals.Variables.MaxInventory;
            for (i = 0; i < loopTo1; i++)
            {
                if (Data.TempPlayer[session.Id].TradeOffer[i].Num == invSlot)
                {
                    NetworkSend.SendPlayerMessage(session.Id, "You've already offered this item.", (int)ColorName.BrightRed);
                    return;
                }
            }
        }

        // not already offering - find earliest empty slot
        var loopTo2 = Core.Globals.Variables.MaxInventory;
        for (i = 0; i < loopTo2; i++)
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

        NetworkSend.SendTradeStatus(session.Id, 0);
        NetworkSend.SendTradeStatus((int)Data.TempPlayer[session.Id].InTrade, 0);

        NetworkSend.SendTradeUpdate(session.Id, 0);
        NetworkSend.SendTradeUpdate(session.Id, 1);
        NetworkSend.SendTradeUpdate((int)Data.TempPlayer[session.Id].InTrade, 0);
        NetworkSend.SendTradeUpdate((int)Data.TempPlayer[session.Id].InTrade, 1);
    }

    public static void Packet_UntradeItem(GameSession session, ReadOnlyMemory<byte> bytes)
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

        NetworkSend.SendTradeStatus(session.Id, 0);
        NetworkSend.SendTradeStatus((int)Data.TempPlayer[session.Id].InTrade, 0);

        NetworkSend.SendTradeUpdate(session.Id, 0);
        NetworkSend.SendTradeUpdate((int)Data.TempPlayer[session.Id].InTrade, 1);
    }

    public static void HackingAttempt(int index, string reason)
    {
        if (index > 0 & NetworkConfig.IsPlaying(index))
        {
            NetworkSend.SendGlobalMessage(GetAccountLogin(index) + "/" + GetPlayerName(index) + " has been booted for (" + reason + ")");
            var task = Server.Player.OnExit(index);
            task.Wait();
        }
    }

    public static void Packet_Admin(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Moderator)
            return;

        NetworkSend.SendAdminPanel(session.Id);
    }

    public static void Packet_SetHotbarSlot(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var @type = (byte)buffer.ReadInt32();
        var newSlot = buffer.ReadInt32();
        var oldSlot = buffer.ReadInt32();
        var skill = buffer.ReadInt32();

        if (newSlot < 0 | newSlot > Core.Globals.Variables.MaxHotbar)
            return;

        if (type == (byte)PartOrigin.Hotbar)
        {
            if (oldSlot < 0 | oldSlot > Core.Globals.Variables.MaxHotbar)
                return;

            var oldItem = PlayerBase.Instance[session.Id].Hotbar[oldSlot].Slot;
            var oldType = PlayerBase.Instance[session.Id].Hotbar[oldSlot].SlotType;
            var newItem = PlayerBase.Instance[session.Id].Hotbar[newSlot].Slot;
            var newType = PlayerBase.Instance[session.Id].Hotbar[newSlot].SlotType;

            PlayerBase.Instance[session.Id].Hotbar[newSlot].Slot = oldItem;
            PlayerBase.Instance[session.Id].Hotbar[newSlot].SlotType = oldType;
            PlayerBase.Instance[session.Id].Hotbar[oldSlot].Slot = newItem;
            PlayerBase.Instance[session.Id].Hotbar[oldSlot].SlotType = newType;
        }
        else
        {
            PlayerBase.Instance[session.Id].Hotbar[newSlot].Slot = skill;
            PlayerBase.Instance[session.Id].Hotbar[newSlot].SlotType = type;
        }

        NetworkSend.SendHotbar(session.Id);
    }

    public static void Packet_DeleteHotbarSlot(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var slot = buffer.ReadInt32();

        if (slot < 0 | slot > Core.Globals.Variables.MaxHotbar)
            return;

        PlayerBase.Instance[session.Id].Hotbar[slot].Slot = -1;
        PlayerBase.Instance[session.Id].Hotbar[slot].SlotType = 0;

        NetworkSend.SendHotbar(session.Id);
    }

    public static void Packet_UseHotbarSlot(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var slot = buffer.ReadInt32();

        if (slot < 0 | slot > Core.Globals.Variables.MaxHotbar)
            return;

        if (PlayerBase.Instance[session.Id].Hotbar[slot].Slot >= 0)
        {
            if (PlayerBase.Instance[session.Id].Hotbar[slot].SlotType == (byte)DraggablePartType.Item)
            {
                int eqSlot = -1;
                for (int i = 0; i < 4; i++)
                {
                    if (PlayerBase.Instance[session.Id].Paperdoll[i].Num == PlayerBase.Instance[session.Id].Hotbar[slot].Slot)
                    {
                        eqSlot = i;
                        break;
                    }
                }

                int m = Server.Player.FindOpenInvSlot(session.Id, (int)PlayerBase.Instance[session.Id].Paperdoll[eqSlot].Num);

                if (eqSlot >= 0 && m >= 0)
                {
                    Server.Player.RemoveEquipment(session.Id, eqSlot, m);
                }
                else
                {
                    Server.Player.UseItem(session.Id, Server.Player.FindItemSlot(session.Id, (int)PlayerBase.Instance[session.Id].Hotbar[slot].Slot));
                }
            }
            else if (PlayerBase.Instance[session.Id].Hotbar[slot].SlotType == (byte)DraggablePartType.Skill)
            {
                try
                {
                    Script.Instance?.BufferSkill(session.Id, PlayerBase.Instance[session.Id].Hotbar[slot].Slot);
                }
                catch (Exception ex)
                {
                    General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(Packet_UseHotbarSlot));
                }
            }
        }

        NetworkSend.SendHotbar(session.Id);
    }

    public static void Packet_SkillLearn(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
            return;

        var skillNum = buffer.ReadInt32();

        try
        {
            Script.Instance?.LearnSkill(session.Id, -1, skillNum);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static void Packet_RequestEditJob(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
            return;

        var user = IsEditorLocked(session.Id, EditorType.Job);

        if (!string.IsNullOrEmpty(user))
        {
            NetworkSend.SendPlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        NetworkSend.SendJobEditor(session.Id);

        NetworkSend.SendItems(session.Id);
        NetworkSend.SendJobs(session);

        Data.TempPlayer[session.Id].Editor = EditorType.Job;        
    }

    public static void Packet_SaveJob(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        int x;
        var buffer = new PacketReader(bytes);

        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
            return;

        var index = buffer.ReadInt32();
    
        var instance = Job.Instance[index];
        instance.Name = buffer.ReadString();
        instance.Desc = buffer.ReadString();

        instance.MaleSprite = buffer.ReadInt32();
        instance.FemaleSprite = buffer.ReadInt32();

        var loopTo = Enum.GetNames(typeof(Stat)).Length;
        for (x = 0; x < loopTo; x++)
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
    
        Job.OnSave(index);
        NetworkSend.SendJobToAll(session.Id);
    }

    private static void Packet_Emote(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var emote = buffer.ReadInt32();

        NetworkSend.SendEmote(session.Id, emote);
    }

    private static void Packet_CloseEditor(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent hacking
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Mapper)
            return;

        if (Data.TempPlayer[session.Id].Editor == EditorType.None)
            return;

        Data.TempPlayer[session.Id].Editor = EditorType.None;
    }


    public static void Packet_RequestEditMoral(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
        {
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Moral);
        if (!string.IsNullOrEmpty(user))
        {
            NetworkSend.SendPlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        NetworkSend.SendMorals(session.Id);

        Data.TempPlayer[session.Id].Editor = EditorType.Moral;

        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SMoralEditor);

        PlayerService.Instance.SendDataTo(session.Id, packet.GetBytes());
    }

    public static void Packet_SaveMoral(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
        {
            return;
        }

        var index = packetReader.ReadInt32();
        if (index < 0 || index >= Core.Globals.Variables.MaxMorals)
        {
            return;
        }

        for (var i = 0; i <= index; i++)
        {
            if (Moral.Instance.Count <= i)
            {
                Moral.Instance.Add(new Moral());
            }
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

        General.Logger.LogInformation("{AccountName} saved moral #{MoralNum}",
            GetAccountLogin(session.Id), index);

        NetworkSend.SendUpdateMoralToAll(index);
        NetworkSend.SendMorals(session.Id);
    }

    public static void Packet_RequestMoral(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        NetworkSend.SendMorals(session.Id);
    }

    public static void Packet_RequestEditScript(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Owner)
        {
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Script);
        if (!string.IsNullOrEmpty(user))
        {
            NetworkSend.SendPlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
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

    public static void Packet_SaveScript(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Owner)
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

        _ = Script.OnLoadAsync(session.Id);
    }

    public static void Packet_RequestProjectile(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        var projectile = packetReader.ReadInt32();

        NetworkSend.SendUpdateProjectileTo(session.Id, projectile);
    }

    public static void Packet_ClearProjectile(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        var projectile = packetReader.ReadInt32();
        _ = packetReader.ReadInt32(); // Target Index
        _ = (TargetType)packetReader.ReadInt32(); // Target TYpe
        _ = packetReader.ReadInt32(); // Target Zone

        var map = GetPlayerMap(session.Id);

        MapProjectile.OnClear(map, projectile);
    }

    public static void Packet_RequestEditProjectile(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
        {
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Projectile);
        if (!string.IsNullOrEmpty(user))
        {
            NetworkSend.SendPlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Projectile;

        var buffer = new PacketWriter(4);

        buffer.WriteEnum(ServerPackets.SProjectileEditor);

        PlayerService.Instance.SendDataTo(session.Id, buffer.GetBytes());

        NetworkSend.SendProjectiles(session.Id);
        NetworkSend.SendAnimations(session.Id);
    }

    public static void Packet_SaveProjectile(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
        {
            return;
        }

        var index = packetReader.ReadInt32();
        if (index < 0 || index > Core.Globals.Variables.MaxProjectiles)
        {
            return;
        }

        for (var i = 0; i <= index; i++)
        {
            if (Projectile.Instance.Count <= i)
            {
                Projectile.Instance.Add(new Projectile());
            }
        }

        Projectile.Instance[index].Name = packetReader.ReadString();
        Projectile.Instance[index].Sprite = packetReader.ReadInt32();
        Projectile.Instance[index].Range = (byte)packetReader.ReadInt32();
        Projectile.Instance[index].Speed = packetReader.ReadInt32();
        Projectile.Instance[index].Damage = packetReader.ReadInt32();
        Projectile.Instance[index].Animation = packetReader.ReadInt32();

        Projectile.OnSave(index);

        General.Logger.LogInformation("{AccountName} saved projectile #{ProjectileNum}",
            GetAccountLogin(session.Id), index);

        NetworkSend.SendUpdateProjectileToAll(index);
    }

    public static void Packet_RequestEditResource(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
        {
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Resource);
        if (!string.IsNullOrEmpty(user))
        {
            NetworkSend.SendPlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Resource;

        NetworkSend.SendItems(session.Id);
        NetworkSend.SendAnimations(session.Id);

        NetworkSend.SendResources(session.Id);

        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SResourceEditor);

        PlayerService.Instance.SendDataTo(session.Id, packet.GetBytes());
    }

    public static void Packet_SaveResource(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
        {
            return;
        }

        var index = packetReader.ReadInt32();
        if (index < 0 || index >= Core.Globals.Variables.MaxResources)
        {
            return;
        }

        for (var i = 0; i <= index; i++)
        {
            if (Resource.Instance.Count <= i)
            {
                Resource.Instance.Add(new Resource());
            }
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

        Resource.OnSave(index);

        General.Logger.LogInformation("{AccountName} saved Resource #{ResourceNum}",
            GetAccountLogin(session.Id), index);

        NetworkSend.SendUpdateResourceToAll(index);
    }

    public static void Packet_RequestResource(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        var index = packetReader.ReadInt32();
        if (index < 0 | index > Core.Globals.Variables.MaxResources)
        {
            return;
        }

        NetworkSend.SendUpdateResourceTo(session.Id, index);
    }

    public static void Packet_RequestItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        var index = packetReader.ReadInt32();
        if (index < 0 || index > Core.Globals.Variables.MaxItems)
        {
            return;
        }

        NetworkSend.SendUpdateItemTo(session.Id, index);
    }

    public static void Packet_RequestEditItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Mapper)
        {
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Item);
        if (!string.IsNullOrEmpty(user))
        {
            NetworkSend.SendPlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Item;

        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SItemEditor);

        PlayerService.Instance.SendDataTo(session.Id, packet.GetBytes());

        NetworkSend.SendAnimations(session.Id);
        NetworkSend.SendProjectiles(session.Id);
        NetworkSend.SendJobs(session);
        NetworkSend.SendItems(session.Id);
    }

    public static void Packet_SaveItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
        {
            return;
        }

        var index = packetReader.ReadInt32();
        if (index < 0 || index > Core.Globals.Variables.MaxItems)
        {
            return;
        }

        for (var i = 0; i <= index; i++)
        {
            if (Item.Instance.Count <= i)
            {
                Item.Instance.Add(new Item());
            }
        }

        Item.Instance[index].AccessReq = packetReader.ReadInt32();

        var statCount = Enum.GetNames<Stat>().Length;
        for (var i = 0; i < statCount; i++)
        {
            Item.Instance[index].AddStat[i] = (byte)packetReader.ReadInt32();
        }

        Item.Instance[index].Animation = packetReader.ReadInt32();
        Item.Instance[index].BindType = packetReader.ReadByte();
        Item.Instance[index].JobReq = packetReader.ReadInt32();
        Item.Instance[index].Data1 = packetReader.ReadInt32();
        Item.Instance[index].Data2 = packetReader.ReadInt32();
        Item.Instance[index].Data3 = packetReader.ReadInt32();
        Item.Instance[index].LevelReq = packetReader.ReadInt32();
        Item.Instance[index].Mastery = (byte)packetReader.ReadInt32();
        Item.Instance[index].Name = packetReader.ReadString();
        Item.Instance[index].Paperdoll = packetReader.ReadInt32();
        Item.Instance[index].Icon = packetReader.ReadInt32();
        Item.Instance[index].Price = packetReader.ReadInt32();
        Item.Instance[index].Rarity = (byte)packetReader.ReadInt32();
        Item.Instance[index].Speed = packetReader.ReadInt32();
        Item.Instance[index].Stackable = (byte)packetReader.ReadInt32();
        Item.Instance[index].Description = packetReader.ReadString();

        for (var i = 0; i < statCount; i++)
        {
            Item.Instance[index].StatReq[i] = (byte)packetReader.ReadInt32();
        }

        Item.Instance[index].Type = (byte)packetReader.ReadInt32();
        Item.Instance[index].SubType = (byte)packetReader.ReadInt32();
        Item.Instance[index].ItemLevel = (byte)packetReader.ReadInt32();
        Item.Instance[index].KnockBack = (byte)packetReader.ReadInt32();
        Item.Instance[index].KnockBackTiles = (byte)packetReader.ReadInt32();
        Item.Instance[index].Projectile = packetReader.ReadInt32();
        Item.Instance[index].Ammo = packetReader.ReadInt32();
        Item.OnSave(index);

        General.Logger.LogInformation("{AccountName} saved item #{ItemNum}",
            GetAccountLogin(session.Id), index);
        NetworkSend.SendUpdateItemToAll(index);
    }

    public static void Packet_GetItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Server.Player.OnGetItem(session.Id);
    }

    public static void Packet_DropItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var invNum = buffer.ReadInt32();
        var amount = buffer.ReadInt32();

        if (Data.TempPlayer[session.Id].InBank || Data.TempPlayer[session.Id].InShop >= 0)
        {
            return;
        }

        if (invNum < 0 || invNum > Core.Globals.Variables.MaxInventory)
        {
            return;
        }

        if (GetPlayerInventory(session.Id, invNum) < 0 || GetPlayerInventory(session.Id, invNum) > Core.Globals.Variables.MaxItems)
        {
            return;
        }

        if (Item.Instance[GetPlayerInventory(session.Id, invNum)].Type == (byte)ItemCategory.Currency ||
            Item.Instance[GetPlayerInventory(session.Id, invNum)].Stackable == 1)
        {
            if (amount < 0 | amount > GetPlayerInventoryValue(session.Id, invNum))
            {
                return;
            }
        }

        try
        {
            Script.Instance?.MapDropItem(session.Id, invNum, amount);
        }
        catch (Exception ex)
        {
            General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(Packet_DropItem));
        }
    }

    public static void Packet_RequestEditAnimation(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
        {
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Animation);
        if (!string.IsNullOrEmpty(user))
        {
            NetworkSend.SendPlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Animation;

        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SAnimationEditor);

        PlayerService.Instance.SendDataTo(session.Id, packet.GetBytes());

        NetworkSend.SendAnimations(session.Id);
    }

    public static void Packet_SaveAnimation(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
        {
            return;
        }

        var index = packetReader.ReadInt32();
        if (index < 0 || index > Variables.MaxAnimations)
        {
            return;
        }

        for (var i = 0; i <= index; i++)
        {
            if (Animation.Instance.Count <= i)
            {
                Animation.Instance.Add(new Animation());
            }
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

        General.Logger.LogInformation("{AccountName} saved animation #{AnimationNum}",
            GetAccountLogin(session.Id), index);

        NetworkSend.SendUpdateAnimationToAll(index);
    }

    public static void Packet_RequestAnimation(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        var animationNum = packetReader.ReadInt32();
        if (animationNum < 0 || animationNum >= Variables.MaxAnimations)
        {
            return;
        }

        NetworkSend.SendUpdateAnimationTo(session.Id, animationNum);
    }

    public static void Packet_Event(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);
        var eventId = buffer.ReadInt32();
        EventLogic.TriggerEvent(session.Id, eventId, 0, GetPlayerX(session.Id), GetPlayerY(session.Id));
    }

    public static void Packet_RequestSwitchesAndVariables(GameSession session, ReadOnlyMemory<byte> bytes) => NetworkSend.SendSwitchesAndVariables(session.Id);

    public static void Packet_SwitchesAndVariables(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);
        for (var i = 0; i < Core.Globals.Variables.MaxSwitches; i++) Event.Switches[i] = buffer.ReadString();
        for (var i = 0; i < Core.Globals.Variables.MaxVariables; i++) Event.Variables[i] = buffer.ReadString();

        Event.SaveSwitches();
        Event.SaveVariables();
        NetworkSend.SendSwitchesAndVariables(0, true);
    }

    public static void Packet_EventChatReply(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);
        int eventId = buffer.ReadInt32(), pageId = buffer.ReadInt32(), reply = buffer.ReadInt32();

        General.Logger.LogInformation($"Player {session.Id} responded to event {eventId} with reply {reply}");
        Event.ProcessEventReply(session.Id, eventId, pageId, reply);
    }


    public static void Packet_PartyRquest(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        // Prevent partying with self
        if (Data.TempPlayer[session.Id].Target == session.Id)
            return;

        // make sure it's a valid target
        if (Data.TempPlayer[session.Id].TargetType != (byte)TargetType.Player)
            return;

        // make sure they're connected and on the same map
        if (GetPlayerMap(Data.TempPlayer[session.Id].Target) != GetPlayerMap(session.Id))
            return;

        // init the request
        Party.OnInvite(session.Id, Data.TempPlayer[session.Id].Target);
    }

    public static void Packet_AcceptParty(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Party.OnAccept(Data.TempPlayer[session.Id].PartyInvite, session.Id);
    }

    public static void Packet_DeclineParty(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Party.OnDecline(Data.TempPlayer[session.Id].PartyInvite, session.Id);
    }

    public static void Packet_LeaveParty(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Party.OnLeave(session.Id);
    }

    public static void Packet_PartyChatMsg(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        Party.OnMessage(session.Id, buffer.ReadString());
    }

    public static void Packet_RequestEditNpc(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
        {
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Npc);
        if (!string.IsNullOrEmpty(user))
        {
            NetworkSend.SendPlayerMessage(session.Id, "The game editor is locked and being used by " + user + ".", (int)ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Npc;

        NetworkSend.SendItems(session.Id);
        NetworkSend.SendAnimations(session.Id);
        NetworkSend.SendSkills(session.Id);

        NetworkSend.SendNpcs(session.Id);

        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SNpcEditor);

        PlayerService.Instance.SendDataTo(session.Id, packet.GetBytes());
    }

    public static void Packet_SaveNpc(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetPlayerAccess(session.Id) < (byte)AccessLevel.Developer)
        {
            return;
        }

        var npcNum = packetReader.ReadInt32();
        if (npcNum < 0 | npcNum > Core.Globals.Variables.MaxNpcs)
        {
            return;
        }

        Npc.Instance[npcNum].Animation = packetReader.ReadInt32();
        Npc.Instance[npcNum].AttackSay = packetReader.ReadString();
        Npc.Instance[npcNum].Behavior = packetReader.ReadByte();

        for (var i = 0; i < Core.Globals.Variables.MaxDropItems; i++)
        {
            Npc.Instance[npcNum].DropChance[i] = packetReader.ReadInt32();
            Npc.Instance[npcNum].DropItem[i] = packetReader.ReadInt32();
            Npc.Instance[npcNum].DropItemValue[i] = packetReader.ReadInt32();
        }

        Npc.Instance[npcNum].Experience = packetReader.ReadInt32();
        Npc.Instance[npcNum].Faction = packetReader.ReadByte();
        Npc.Instance[npcNum].Hp = packetReader.ReadInt32();
        Npc.Instance[npcNum].Name = packetReader.ReadString();
        Npc.Instance[npcNum].Range = packetReader.ReadByte();
        Npc.Instance[npcNum].SpawnTime = packetReader.ReadByte();
        Npc.Instance[npcNum].SpawnSecs = packetReader.ReadInt32();
        Npc.Instance[npcNum].Sprite = packetReader.ReadInt32();

        var statCount = Enum.GetValues<Stat>().Length;
        for (var i = 0; i < statCount; i++)
        {
            Npc.Instance[npcNum].Stat[i] = packetReader.ReadByte();
        }

        for (var i = 0; i < Core.Globals.Variables.MaxNpcSkills; i++)
        {
            Npc.Instance[npcNum].Skill[i] = packetReader.ReadByte();
        }

        Npc.Instance[npcNum].Level = packetReader.ReadByte();
        Npc.Instance[npcNum].Damage = packetReader.ReadInt32();

        Npc.OnSave(npcNum);

        General.Logger.LogInformation("{AccountName} saved NPC #{NpcNum}",
            GetAccountLogin(session.Id), npcNum);

        NetworkSend.SendUpdateNpcToAll(npcNum);
    }
}
