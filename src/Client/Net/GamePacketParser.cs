using Client.Game.UI;
using Client.Game.UI.Windows;
using Core;
using Core.Configurations;
using Core.Globals;
using Core.Net;
using System;
using static Core.Globals.Command;
using static Core.Globals.Type;

namespace Client.Net;

public sealed class GamePacketParser : PacketParser<Packets.ServerPackets>
{
    // Cache enum sizes to avoid reflection on every update
    private static readonly int EquipmentCount = Enum.GetValues<Equipment>().Length;
    private static readonly int StatCount = Enum.GetValues<Stat>().Length;
    private static readonly int VitalCount = Enum.GetValues<Vital>().Length;

    public GamePacketParser()
    {
        Bind(Packets.ServerPackets.SAes, Packet_Aes);
        Bind(Packets.ServerPackets.SAlertMsg, Packet_AlertMsg);
        Bind(Packets.ServerPackets.SVariables, Packet_Variables);
        Bind(Packets.ServerPackets.SLoginOk, Packet_LoginOk);
        Bind(Packets.ServerPackets.SPlayerChars, Packet_PlayerChars);
        Bind(Packets.ServerPackets.SUpdateJob, Packet_UpdateJob);
        Bind(Packets.ServerPackets.SJobData, Packet_JobData);
        Bind(Packets.ServerPackets.SInGame, Packet_InGame);
        Bind(Packets.ServerPackets.SPlayerInv, Packet_PlayerInv);
        Bind(Packets.ServerPackets.SPlayerInvUpdate, Packet_PlayerInvUpdate);
        Bind(Packets.ServerPackets.SPlayerWornEq, Packet_PlayerWornEquipment);
        Bind(Packets.ServerPackets.SPlayerHP, Player.Packet_PlayerHP);
        Bind(Packets.ServerPackets.SPlayerMP, Player.Packet_PlayerMP);
        Bind(Packets.ServerPackets.SPlayerSP, Player.Packet_PlayerSP);
        Bind(Packets.ServerPackets.SPlayerStats, Player.Packet_PlayerStats);
        Bind(Packets.ServerPackets.SPlayerData, Player.Packet_PlayerData);
        Bind(Packets.ServerPackets.SNpcMove, Packet_NpcMove);
        Bind(Packets.ServerPackets.SPlayerDir, Player.Packet_PlayerDir);
        Bind(Packets.ServerPackets.SNpcDir, Packet_NpcDir);
        Bind(Packets.ServerPackets.SPlayerXY, Player.Packet_PlayerXY);
        Bind(Packets.ServerPackets.SAttack, Packet_Attack);
        Bind(Packets.ServerPackets.SNpcAttack, Packet_NpcAttack);
        Bind(Packets.ServerPackets.SCheckForMap, Map.Packet_CheckMap);
        Bind(Packets.ServerPackets.SMapData, Map.MapData);
        Bind(Packets.ServerPackets.SMapItemData, Map.Packet_MapItemData);
        Bind(Packets.ServerPackets.SMapItemsData, Map.Packet_MapItemsData);
        Bind(Packets.ServerPackets.SMapNpcData, Map.Packet_MapNpcData);
        Bind(Packets.ServerPackets.SMapNpcUpdate, Map.Packet_MapNpcUpdate);
        Bind(Packets.ServerPackets.SGlobalMsg, Packet_GlobalMsg);
        Bind(Packets.ServerPackets.SAdminMsg, Packet_AdminMsg);
        Bind(Packets.ServerPackets.SPlayerMsg, Packet_PlayerMsg);
        Bind(Packets.ServerPackets.SMapMsg, Packet_MapMsg);
        Bind(Packets.ServerPackets.SSpawnItem, Packet_SpawnItem);
        Bind(Packets.ServerPackets.SUpdateItem, Packet_UpdateItem);
        Bind(Packets.ServerPackets.SSpawnNpc, Packet_SpawnNpc);
        Bind(Packets.ServerPackets.SNpcDead, Packet_NpcDead);
        Bind(Packets.ServerPackets.SPlayerDead, Packet_PlayerDead);
        Bind(Packets.ServerPackets.SUpdateNpc, Packet_UpdateNpc);
        Bind(Packets.ServerPackets.SEditMap, Map.Packet_EditMap);
        Bind(Packets.ServerPackets.SUpdateShop, Packet_UpdateShop);
        Bind(Packets.ServerPackets.SUpdateSkill, Packet_UpdateSkill);
        Bind(Packets.ServerPackets.SSkills, Packet_Skills);
        Bind(Packets.ServerPackets.SLeftMap, Packet_LeftMap);
        Bind(Packets.ServerPackets.SMapResource, Packet_MapResource);
        Bind(Packets.ServerPackets.SUpdateResource, Packet_UpdateResource);
        Bind(Packets.ServerPackets.SSendPing, Packet_Ping);
        Bind(Packets.ServerPackets.SActionMsg, Packet_ActionMessage);
        Bind(Packets.ServerPackets.SPlayerExp, Player.Packet_PlayerExp);
        Bind(Packets.ServerPackets.SBlood, Packet_Blood);
        Bind(Packets.ServerPackets.SUpdateAnimation, Packet_UpdateAnimation);
        Bind(Packets.ServerPackets.SAnimation, Packet_Animation);
        Bind(Packets.ServerPackets.SMapNpcVitals, Packet_NpcVitals);
        Bind(Packets.ServerPackets.SCooldown, Packet_Cooldown);
        Bind(Packets.ServerPackets.SClearSkillBuffer, Packet_ClearSkillBuffer);
        Bind(Packets.ServerPackets.SStartSkillBuffer, Packet_StartSkillBuffer);
        Bind(Packets.ServerPackets.SSayMsg, Packet_SayMessage);
        Bind(Packets.ServerPackets.SOpenShop, Packet_OpenShop);
        Bind(Packets.ServerPackets.SResetShopAction, Packet_ResetShopAction);
        Bind(Packets.ServerPackets.SStunned, Packet_Stunned);
        Bind(Packets.ServerPackets.SMapWornEq, Packet_MapWornEquipment);
        Bind(Packets.ServerPackets.SBank, Bank.Packet_OpenBank);
        Bind(Packets.ServerPackets.SLeftGame, Packet_LeftGame);
        Bind(Packets.ServerPackets.STradeInvite, Trade.Packet_TradeInvite);
        Bind(Packets.ServerPackets.STrade, Trade.Packet_Trade);
        Bind(Packets.ServerPackets.SCloseTrade, Trade.Packet_CloseTrade);
        Bind(Packets.ServerPackets.STradeUpdate, Trade.Packet_TradeUpdate);
        Bind(Packets.ServerPackets.STradeStatus, Trade.Packet_TradeStatus);
        Bind(Packets.ServerPackets.SMapReport, Packet_MapReport);
        Bind(Packets.ServerPackets.STarget, Packet_Target);
        Bind(Packets.ServerPackets.SAdmin, Packet_Admin);
        Bind(Packets.ServerPackets.SCritical, Packet_Critical);
        Bind(Packets.ServerPackets.SrClick, Packet_RClick);
        Bind(Packets.ServerPackets.SHotbar, Packet_Hotbar);
        Bind(Packets.ServerPackets.SSpawnEvent, Event.Packet_SpawnEvent);
        Bind(Packets.ServerPackets.SEventMove, Event.Packet_EventMove);
        Bind(Packets.ServerPackets.SEventDir, Event.Packet_EventDir);
        Bind(Packets.ServerPackets.SEventChat, Event.Packet_EventChat);
        Bind(Packets.ServerPackets.SEventStart, Event.Packet_EventStart);
        Bind(Packets.ServerPackets.SEventEnd, Event.Packet_EventEnd);
        Bind(Packets.ServerPackets.SPlayBgm, Event.Packet_PlayBGM);
        Bind(Packets.ServerPackets.SPlaySound, Event.Packet_PlaySound);
        Bind(Packets.ServerPackets.SFadeoutBgm, Event.Packet_FadeOutBGM);
        Bind(Packets.ServerPackets.SStopSound, Event.Packet_StopSound);
        Bind(Packets.ServerPackets.SSwitchesAndVariables, Event.Packet_SwitchesAndVariables);
        Bind(Packets.ServerPackets.SMapEventData, Event.Packet_MapEventData);
        Bind(Packets.ServerPackets.SChatBubble, Packet_ChatBubble);
        Bind(Packets.ServerPackets.SSpecialEffect, Event.Packet_SpecialEffect);
        Bind(Packets.ServerPackets.SPic, Event.Packet_Picture);
        Bind(Packets.ServerPackets.SHoldPlayer, Event.Packet_HoldPlayer);
        Bind(Packets.ServerPackets.SUpdateProjectile, Projectile.HandleUpdateProjectile);
        Bind(Packets.ServerPackets.SMapProjectile, Projectile.HandleMapProjectile);
        Bind(Packets.ServerPackets.SEmote, Packet_Emote);
        Bind(Packets.ServerPackets.SPartyInvite, Party.Packet_PartyInvite);
        Bind(Packets.ServerPackets.SPartyUpdate, Party.Packet_PartyUpdate);
        Bind(Packets.ServerPackets.SPartyVitals, Party.Packet_PartyVitals);
        Bind(Packets.ServerPackets.SClock, Packet_Clock);
        Bind(Packets.ServerPackets.STime, Packet_Time);
        Bind(Packets.ServerPackets.SScriptEditor, Script.Packet_EditScript);
        Bind(Packets.ServerPackets.SItemEditor, Packet_EditItem);
        Bind(Packets.ServerPackets.SNpcEditor, Packet_NpcEditor);
        Bind(Packets.ServerPackets.SShopEditor, Packet_EditShop);
        Bind(Packets.ServerPackets.SSkillEditor, Packet_EditSkill);
        Bind(Packets.ServerPackets.SResourceEditor, Packet_ResourceEditor);
        Bind(Packets.ServerPackets.SAnimationEditor, Packet_AnimationEditor);
        Bind(Packets.ServerPackets.SProjectileEditor, HandleProjectileEditor);
        Bind(Packets.ServerPackets.SJobEditor, Packet_JobEditor);
        Bind(Packets.ServerPackets.SUpdateMoral, Packet_UpdateMoral);
        Bind(Packets.ServerPackets.SMoralEditor, Packet_EditMoral);
    }

    private static void Packet_Aes(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var keyLength = packetReader.ReadByte();
        var key = packetReader.ReadBlock(keyLength).ToArray();

        var ivLength = packetReader.ReadByte();
        var iv = packetReader.ReadBlock(ivLength).ToArray();

        General.AesKey = key;
        General.AesIV = iv;
    }

    private static void Packet_Variables(ReadOnlyMemory<byte> data)
    {
        var r = new PacketReader(data);

        // Int-sized values (order must match server)
        Variables.MaxAnimations = r.ReadInt32();
        Variables.MaxItems = r.ReadInt32();
        Variables.MaxMaps = r.ReadInt32();
        Variables.MaxNpcs = r.ReadInt32();
        Variables.MaxParty = r.ReadInt32();
        Variables.MaxPartyMembers = r.ReadInt32();
        Variables.MaxPlayers = r.ReadInt32();
        Variables.MaxResources = r.ReadInt32();
        Variables.MaxShops = r.ReadInt32();
        Variables.MaxSkills = r.ReadInt32();
        Variables.MaxProjectiles = r.ReadInt32();
        Variables.MaxSwitches = r.ReadInt32();
        Variables.MaxVariables = r.ReadInt32();
        Variables.ChatLines = r.ReadInt32();
        Variables.MaxEvents = r.ReadInt32();
        Variables.TileSize = r.ReadInt32();
        Variables.MaxWeatherParticles = r.ReadInt32();

        // Byte-sized values
        Variables.MaxBank = r.ReadByte();
        Variables.MaxJobs = r.ReadByte();
        Variables.MaxMorals = r.ReadByte();
        Variables.MaxInv = r.ReadByte();
        Variables.MaxMapItems = r.ReadByte();
        Variables.MaxMapNpcs = r.ReadByte();
        Variables.MaxNpcSkills = r.ReadByte();
        Variables.MaxPlayerSkills = r.ReadByte();
        Variables.MaxTrades = r.ReadByte();
        Variables.NameLength = r.ReadByte();
        Variables.MinNameLength = r.ReadByte();
        Variables.ChatLength = r.ReadByte();
        Variables.MaxHotbar = r.ReadByte();
        Variables.MaxMapX = r.ReadByte();
        Variables.MaxMapY = r.ReadByte();
        Variables.MaxDropItems = r.ReadByte();
        Variables.MaxStartItems = r.ReadByte();
        Variables.MaxStartSkills = r.ReadByte();
        Variables.MaxPoints = r.ReadByte();
        Variables.MaxChars = r.ReadByte();
        Variables.MaxStats = r.ReadByte();
        Variables.MaxQuests = r.ReadByte();
        Variables.MaxGuilds = r.ReadByte();
        Variables.MaxEventChoices = r.ReadByte();
        
        ApplyClientSizing();
    }

    private static void ApplyClientSizing()
    {
        // Character select arrays
        if (GameState.CharName.Length != Variables.MaxChars)
        {
            Array.Resize(ref GameState.CharName, Variables.MaxChars);
            Array.Resize(ref GameState.CharSprite, Variables.MaxChars);
            Array.Resize(ref GameState.CharAccess, Variables.MaxChars);
            Array.Resize(ref GameState.CharJob, Variables.MaxChars);
            GameState.CharEq = new long[Variables.MaxChars, EquipmentCount];
        }

        // Bars and map names
        if (GameState.BarWidthNpcHP.Length != Variables.MaxMapNpcs)
        {
            GameState.BarWidthNpcHP = new int[Variables.MaxMapNpcs];
            GameState.BarWidthNpcHPMax = new int[Variables.MaxMapNpcs];
        }
        
        if (GameState.BarWidthPlayerHP.Length != Variables.MaxPlayers)
        {
            GameState.BarWidthPlayerHP = new int[Variables.MaxPlayers];
            GameState.BarWidthPlayerHPMax = new int[Variables.MaxPlayers];
            GameState.BarWidthPlayerMP = new int[Variables.MaxPlayers];
            GameState.BarWidthPlayerMPMax = new int[Variables.MaxPlayers];
        }

        if (GameState.MapNames.Length != Variables.MaxMaps)
        {
            Array.Resize(ref GameState.MapNames, Variables.MaxMaps);
        }
    }

    private static void Packet_AlertMsg(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        var dialogueIndex = buffer.ReadByte();
        var menuReset = buffer.ReadInt32();
        var kick = buffer.ReadInt32();

        if (menuReset > 0)
        {
            // We're going back to a menu screen; ensure flags are consistent
            GameState.InGame = false;
            GameState.InMenu = true;
            WindowManager.HideWindows();

            switch ((Menu) menuReset)
            {
                case Menu.Login:
                    WindowManager.ShowWindow("winLogin");
                    break;

                case Menu.CharacterSelect:
                    WindowManager.ShowWindow("winChars");
                    break;

                case Menu.JobSelection:
                    WindowManager.ShowWindow("winJobs");
                    break;

                case Menu.NewCharacter:
                    WindowManager.ShowWindow("winNewChar");
                    break;

                case Menu.MainMenu:
                    WindowManager.ShowWindow("winLogin");
                    break;

                case Menu.Register:
                    WindowManager.ShowWindow("winRegister");
                    break;
            }
        }
        else if (kick > 0 || GameState.InGame)
        {
            GameLogic.LogoutGame();
        }
        GameLogic.DialogueAlert(dialogueIndex);
    }

    private static void Packet_LoginOk(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        GameState.MyIndex = packetReader.ReadInt32();
    }

    public static void Packet_PlayerChars(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var isSlotEmpty = new bool[Variables.MaxChars];

        if (WindowManager.TryGetControl("winLogin", "txtUsername", out var usernameCtrl))
        {
            SettingsManager.Instance.Username = usernameCtrl!.Text;
        }
        SettingsManager.Save();

        for (var i = 0; i < Variables.MaxChars; i++)
        {
            GameState.CharName[i] = packetReader.ReadString();
            GameState.CharSprite[i] = packetReader.ReadInt32();
            GameState.CharAccess[i] = packetReader.ReadInt32();
            GameState.CharJob[i] = packetReader.ReadInt32();
            for (var j = 0; j < EquipmentCount; j++)
            {
                GameState.CharEq[i, j] = packetReader.ReadInt32();
            }
            if (string.IsNullOrEmpty(GameState.CharName[i]))
            {
                isSlotEmpty[i] = true;
            }
        }


        WindowManager.HideWindows();
        WindowManager.ShowWindow("winChars");

        long winNum = WindowManager.GetWindowIndex("winChars");
        for (var i = 0L; i < Variables.MaxChars; i++)
        {
            long conNum = WindowManager.GetControlIndex("winChars", "lblCharName_" + (i + 1));
            {
                var control = WindowManager.Windows[winNum].Controls[(int) conNum];

                control.Text = !isSlotEmpty[(int) i] ? (GameState.CharName[(int) i] ?? string.Empty) : "Blank Slot";
            }

            if (isSlotEmpty[(int) i])
            {
                // create button
                conNum = WindowManager.GetControlIndex("winChars", "btnCreateChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = true;

                // select button
                conNum = WindowManager.GetControlIndex("winChars", "btnSelectChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = false;

                // delete button
                conNum = WindowManager.GetControlIndex("winChars", "btnDelChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = false;
            }
            else
            {
                // create button
                conNum = WindowManager.GetControlIndex("winChars", "btnCreateChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = false;

                // select button
                conNum = WindowManager.GetControlIndex("winChars", "btnSelectChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = true;

                // delete button
                conNum = WindowManager.GetControlIndex("winChars", "btnDelChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = true;
            }
        }
    }

    public static void Packet_UpdateJob(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var jobNum = packetReader.ReadInt32();

        ref var job = ref Data.Job[jobNum];

        job.Name = packetReader.ReadString();
        job.Desc = packetReader.ReadString();
        job.MaleSprite = packetReader.ReadInt32();
        job.FemaleSprite = packetReader.ReadInt32();

        for (var i = 0; i < StatCount; i++)
        {
            job.Stat[i] = packetReader.ReadInt32();
        }

        for (var i = 0; i < Variables.MaxStartItems; i++)
        {
            job.StartItem[i] = packetReader.ReadInt32();
            job.StartValue[i] = packetReader.ReadInt32();
        }

        for (var i = 0; i < Variables.MaxStartSkills; i++)
        {
            job.StartSkill[i] = packetReader.ReadInt32();
        }

        job.StartMap = packetReader.ReadInt32();
        job.StartX = packetReader.ReadByte();
        job.StartY = packetReader.ReadByte();
        job.BaseExp = packetReader.ReadInt32();
    }

    public static void Packet_JobData(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        for (var jobNum = 0; jobNum < Variables.MaxJobs; jobNum++)
        {
            ref var job = ref Data.Job[jobNum];

            job.Name = packetReader.ReadString();
            job.Desc = packetReader.ReadString();
            job.MaleSprite = packetReader.ReadInt32();
            job.FemaleSprite = packetReader.ReadInt32();

            for (var i = 0; i < StatCount; i++)
            {
                job.Stat[i] = packetReader.ReadInt32();
            }

            for (var i = 0; i < Variables.MaxStartItems; i++)
            {
                job.StartItem[i] = packetReader.ReadInt32();
                job.StartValue[i] = packetReader.ReadInt32();
            }

            for (var i = 0; i < Variables.MaxStartSkills; i++)
            {
                job.StartSkill[i] = packetReader.ReadInt32();
            }

            job.StartMap = packetReader.ReadInt32();
            job.StartX = packetReader.ReadByte();
            job.StartY = packetReader.ReadByte();
            job.BaseExp = packetReader.ReadInt32();
        }
    }

    private static void Packet_InGame(ReadOnlyMemory<byte> data)
    {
        GameState.InMenu = false;
        GameState.InGame = true;

        WindowManager.HideWindows();

        GameState.CanMoveNow = true;
        GameState.MyEditorType = EditorType.None;
        GameState.SkillBuffer = -1;
        GameState.InShop = -1;

        WindowManager.ShowWindow("winHotbar", resetPosition: false);
        WindowManager.ShowWindow("winMenu", resetPosition: false);
        WindowManager.ShowWindow("winBars", resetPosition: false);

        try { WinChat.Hide(); } catch (Exception ex) { Console.WriteLine($"WinChat.Hide error: {ex.Message}"); }

        General.GameInit();
    }

    private static void Packet_PlayerInv(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        for (var i = 0; i < Variables.MaxInv; i++)
        {
            var itemNum = packetReader.ReadInt32();
            var amount = packetReader.ReadInt32();

            SetPlayerInv(GameState.MyIndex, i, itemNum);
            SetPlayerInvValue(GameState.MyIndex, i, amount);
        }

        GameLogic.SetGoldLabel();
    }

    private static void Packet_PlayerInvUpdate(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var invSlot = packetReader.ReadInt32();

        SetPlayerInv(GameState.MyIndex, invSlot, packetReader.ReadInt32());
        SetPlayerInvValue(GameState.MyIndex, invSlot, packetReader.ReadInt32());

        GameLogic.SetGoldLabel();
    }

    private static void Packet_PlayerWornEquipment(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        for (var i = 0; i < EquipmentCount; i++)
        {
            var itemNum = packetReader.ReadInt32();

            SetPlayerEquipment(GameState.MyIndex, itemNum, (Equipment)i);
            Item.OnStream(itemNum);
        }
    }

    private static void Packet_NpcMove(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var mapNpcNum = packetReader.ReadInt32();
        var x = packetReader.ReadInt32();
        var y = packetReader.ReadInt32();
        var dir = packetReader.ReadByte();
        var movement = packetReader.ReadInt32();

        ref var mapNpc = ref Data.MyMapNpc[mapNpcNum];

        // Server signals start of a 1-tile move. Keep the authoritative starting position,
        // initialize client-side step bookkeeping, and set moving state/dir.
        mapNpc.X = x;
        mapNpc.Y = y;
        mapNpc.Dir = dir;
        mapNpc.Moving = (byte)movement;
        Client.Npc.StartStep(mapNpcNum, x, y, dir);
    }

    private static void Packet_NpcDir(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var mapNpcNum = packetReader.ReadInt32();
        var dir = packetReader.ReadByte();

        ref var mapNpc = ref Data.MyMapNpc[mapNpcNum];

        mapNpc.Dir = dir;
        // Ensure we finish at the exact destination for the last step
        Client.Npc.SnapToDest(mapNpcNum);
        mapNpc.Moving = 0;
        // Mark movement stop so renderer may finish the run cycle visually
        Client.Npc.MarkMoveStop(mapNpcNum);
    }

    private static void Packet_Attack(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var playerIndex = packetReader.ReadInt32();

        Data.Player[playerIndex].Attacking = 1;
        Data.Player[playerIndex].AttackTimer = General.GetTickCount();
    }

    private static void Packet_NpcAttack(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var mapNpcNum = packetReader.ReadInt32();

        Data.MyMapNpc[mapNpcNum].Attacking = 1;
        Data.MyMapNpc[mapNpcNum].AttackTimer = General.GetTickCount();
    }

    private static void Packet_GlobalMsg(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var message = packetReader.ReadString();

        TextRenderer.AddText(message, (int) ColorName.Yellow, channel: (byte) ChatChannel.Broadcast);
    }

    private static void Packet_MapMsg(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var message = packetReader.ReadString();

        TextRenderer.AddText(message, (int) ColorName.White, channel: (byte) ChatChannel.Map);
    }

    private static void Packet_AdminMsg(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var message = packetReader.ReadString();

        TextRenderer.AddText(message, (int) ColorName.BrightCyan, channel: (byte) ChatChannel.Broadcast);
    }

    private static void Packet_PlayerMsg(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var message = packetReader.ReadString();
        var color = packetReader.ReadInt32();

        TextRenderer.AddText(message, color, channel: (byte) ChatChannel.Private);
    }

    private static void Packet_SpawnItem(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var mapItemNum = packetReader.ReadInt32();

        ref var mapItem = ref Data.MyMapItem[mapItemNum];

        mapItem.Num = packetReader.ReadInt32();
        mapItem.Value = packetReader.ReadInt32();
        mapItem.X = packetReader.ReadInt32();
        mapItem.Y = packetReader.ReadInt32();
    }

    private static void Packet_SpawnNpc(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var mapNpcNum = packetReader.ReadInt32();

        ref var mapNpc = ref Data.MyMapNpc[mapNpcNum];

        mapNpc.Num = packetReader.ReadInt32();
        mapNpc.X = packetReader.ReadInt32();
        mapNpc.Y = packetReader.ReadInt32();
        mapNpc.Dir = packetReader.ReadByte();

        for (mapNpcNum = 0; mapNpcNum < VitalCount; mapNpcNum++)
        {
            mapNpc.Vital[mapNpcNum] = packetReader.ReadInt32();
        }

        mapNpc.Moving = 0;
    }

    private static void Packet_NpcDead(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var timer = packetReader.ReadInt32(); // milliseconds until respawn
        var mapNpcNum = packetReader.ReadInt32();

        Data.MyMapNpc[mapNpcNum].DeathTimer = Client.General.GetTickCount() + timer;
        Map.ClearMapNpc(mapNpcNum);
    }

    private static void Packet_PlayerDead(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);
        var timer = packetReader.ReadInt32(); // milliseconds until respawn

        Data.Player[packetReader.ReadInt32()].DeathTimer = Client.General.GetTickCount() + timer;
    }

    private static void Packet_UpdateNpc(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var npcNum = packetReader.ReadInt32();

        Data.Npc[npcNum].Animation = packetReader.ReadInt32();
        Data.Npc[npcNum].AttackSay = packetReader.ReadString();
        Data.Npc[npcNum].Behavior = packetReader.ReadByte();

        for (var i = 0; i < Variables.MaxDropItems; i++)
        {
            Data.Npc[npcNum].DropChance[i] = packetReader.ReadInt32();
            Data.Npc[npcNum].DropItem[i] = packetReader.ReadInt32();
            Data.Npc[npcNum].DropItemValue[i] = packetReader.ReadInt32();
        }

        Data.Npc[npcNum].Exp = packetReader.ReadInt32();
        Data.Npc[npcNum].Faction = packetReader.ReadByte();
        Data.Npc[npcNum].Hp = packetReader.ReadInt32();
        Data.Npc[npcNum].Name = packetReader.ReadString();
        Data.Npc[npcNum].Range = packetReader.ReadByte();
        Data.Npc[npcNum].SpawnTime = packetReader.ReadByte();
        Data.Npc[npcNum].SpawnSecs = packetReader.ReadInt32();
        Data.Npc[npcNum].Sprite = packetReader.ReadInt32();

        for (var i = 0; i < StatCount; i++)
        {
            Data.Npc[npcNum].Stat[i] = packetReader.ReadByte();
        }

        for (var i = 0; i < Variables.MaxNpcSkills; i++)
        {
            Data.Npc[npcNum].Skill[i] = packetReader.ReadByte();
        }

        Data.Npc[npcNum].Level = packetReader.ReadByte();
        Data.Npc[npcNum].Damage = packetReader.ReadInt32();
    }

    private static void Packet_UpdateSkill(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var skillNum = packetReader.ReadInt32();

        Data.Skill[skillNum].AccessReq = packetReader.ReadInt32();
        Data.Skill[skillNum].AoE = packetReader.ReadInt32();
        Data.Skill[skillNum].CastAnim = packetReader.ReadInt32();
        Data.Skill[skillNum].CastTime = packetReader.ReadInt32();
        Data.Skill[skillNum].CdTime = packetReader.ReadInt32();
        Data.Skill[skillNum].JobReq = packetReader.ReadInt32();
        Data.Skill[skillNum].Dir = (byte)packetReader.ReadInt32();
        Data.Skill[skillNum].Duration = packetReader.ReadInt32();
        Data.Skill[skillNum].Icon = packetReader.ReadInt32();
        Data.Skill[skillNum].Interval = packetReader.ReadInt32();
        Data.Skill[skillNum].IsAoE = packetReader.ReadInt32() != 0;
        Data.Skill[skillNum].LevelReq = packetReader.ReadInt32();
        Data.Skill[skillNum].Map = packetReader.ReadInt32();
        Data.Skill[skillNum].MpCost = packetReader.ReadInt32();
        Data.Skill[skillNum].Name = packetReader.ReadString();
        Data.Skill[skillNum].Range = packetReader.ReadInt32();
        Data.Skill[skillNum].SkillAnim = packetReader.ReadInt32();
        Data.Skill[skillNum].StunDuration = packetReader.ReadInt32();
        Data.Skill[skillNum].Type = (byte)packetReader.ReadInt32();
        Data.Skill[skillNum].Vital = packetReader.ReadInt32();
        Data.Skill[skillNum].X = packetReader.ReadInt32();
        Data.Skill[skillNum].Y = packetReader.ReadInt32();
        Data.Skill[skillNum].IsProjectile = packetReader.ReadInt32();
        Data.Skill[skillNum].Projectile = packetReader.ReadInt32();
        Data.Skill[skillNum].KnockBack = (byte)packetReader.ReadInt32();
        Data.Skill[skillNum].KnockBackTiles = (byte)packetReader.ReadInt32();
        Data.Skill[skillNum].MultiDirMask = packetReader.ReadInt32();
        Data.Skill[skillNum].ChainOnHitSkillId = packetReader.ReadInt32();
        Data.Skill[skillNum].CommonEventType = (byte)packetReader.ReadInt32();
        Data.Skill[skillNum].CommonEventData1 = packetReader.ReadInt32();
        Data.Skill[skillNum].CommonEventData2 = packetReader.ReadInt32();
    }

    private static void Packet_Skills(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        for (var i = 0; i < Variables.MaxPlayerSkills; i++)
        {
            Data.Player[GameState.MyIndex].Skill[i].Num = packetReader.ReadInt32();
        }
    }

    private static void Packet_LeftMap(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        Player.ClearPlayer(packetReader.ReadInt32());
    }

    private static void Packet_Ping(ReadOnlyMemory<byte> data)
    {
        GameState.PingEnd = General.GetTickCount();
        GameState.Ping = GameState.PingEnd - GameState.PingStart;
    }

    private static void Packet_ActionMessage(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var message = packetReader.ReadString();
        var color = packetReader.ReadInt32();
        var tmpType = packetReader.ReadInt32();
        var x = packetReader.ReadInt32();
        var y = packetReader.ReadInt32();


        GameLogic.CreateActionMsg(message, color, (byte) tmpType, x, y);
    }

    private static void Packet_Blood(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var x = packetReader.ReadInt32();
        var y = packetReader.ReadInt32();

        var sprite = GameLogic.Rand(1, 3);

        GameState.BloodIndex = (byte) (GameState.BloodIndex + 1);
        if (GameState.BloodIndex >= byte.MaxValue)
        {
            GameState.BloodIndex = 1;
        }

        ref var blood = ref Data.Blood[GameState.BloodIndex];

        blood.X = x;
        blood.Y = y;
        blood.Sprite = sprite;
        blood.Timer = General.GetTickCount();
    }

    private static void Packet_NpcVitals(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var mapNpcNum = packetReader.ReadInt32();
        for (var i = 0; i < VitalCount; i++)
        {
            Data.MyMapNpc[mapNpcNum].Vital[i] = packetReader.ReadInt32();
        }
    }

    private static void Packet_Cooldown(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var slot = packetReader.ReadInt32();

        Data.Player[GameState.MyIndex].Skill[slot].Cd = General.GetTickCount();
    }

    private static void Packet_ClearSkillBuffer(ReadOnlyMemory<byte> data)
    {
        GameState.SkillBuffer = -1;
        GameState.SkillBufferTimer = 0;
    }

    private static void Packet_StartSkillBuffer(ReadOnlyMemory<byte> data)
    {
        var reader = new PacketReader(data);
        // Packet id already consumed by dispatcher
        int slot = reader.ReadInt32();
        GameState.SkillBuffer = slot;
        GameState.SkillBufferTimer = General.GetTickCount(); // could offset with serverStart if clock sync later
    }

    private static void Packet_SayMessage(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var name = packetReader.ReadString();
        var access = (AccessLevel) packetReader.ReadInt32();
        var pk = packetReader.ReadBoolean();
        var message = packetReader.ReadString();
        var header = packetReader.ReadString();

        // Check access level
        var color = access switch
        {
            AccessLevel.Player => (byte) ColorName.White,
            AccessLevel.Moderator => (byte) ColorName.Cyan,
            AccessLevel.Mapper => (byte) ColorName.Green,
            AccessLevel.Developer => (byte) ColorName.BrightBlue,
            AccessLevel.Owner => (byte) ColorName.Yellow,
            _ => (byte) ColorName.White
        };

        if (pk)
        {
            color = (byte) ColorName.BrightRed;
        }

        var channelType = header switch
        {
            "[Map]:" => (byte) ChatChannel.Map,
            "[Global]:" => (byte) ChatChannel.Broadcast,
            _ => (byte) 0
        };

        // add to the chat box
        TextRenderer.AddText(header + " " + name + ": " + message, color, channel: channelType);
    }

    private static void Packet_Stunned(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        GameState.StunDuration = packetReader.ReadInt32();
    }

    private static void Packet_MapWornEquipment(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var playerNum = packetReader.ReadInt32();

        for (var i = 0; i < EquipmentCount; i++)
        {
            var itemNum = packetReader.ReadInt32();

            SetPlayerEquipment(playerNum, itemNum, (Equipment) i);
        }
    }

    private static void Packet_Target(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        GameState.MyTarget = packetReader.ReadInt32();
        GameState.MyTargetType = packetReader.ReadInt32();
    }

    private static void Packet_MapReport(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        for (var i = 0; i < Variables.MaxMaps; i++)
        {
            GameState.MapNames[i] = packetReader.ReadString();
        }

        GameState.InitMapReport = true;
    }

    private static void Packet_Admin(ReadOnlyMemory<byte> data)
    {
        GameState.InitAdminForm = true;
    }

    private static void Packet_Critical(ReadOnlyMemory<byte> data)
    {
        GameState.ShakeTimerEnabled = true;
        GameState.ShakeTimer = General.GetTickCount();
    }

    private static void Packet_RClick(ReadOnlyMemory<byte> data)
    {

    }

    private static void Packet_Emote(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var playerIndex = packetReader.ReadInt32();

        ref var player = ref Data.Player[playerIndex];

        player.Emote = packetReader.ReadInt32();
        player.EmoteTimer = General.GetTickCount() + 5000;
    }

    private static void Packet_ChatBubble(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        GameLogic.AddChatBubble(
            target: packetReader.ReadInt32(),
            targetType: (byte) packetReader.ReadInt32(),
            msg: packetReader.ReadString(),
            color: packetReader.ReadInt32());
    }

    private static void Packet_LeftGame(ReadOnlyMemory<byte> data)
    {
        GameLogic.LogoutGame();
    }

    private static void Packet_AnimationEditor(ReadOnlyMemory<byte> data)
    {
        GameState.InitAnimationEditor = true;
    }

    private static void Packet_JobEditor(ReadOnlyMemory<byte> data)
    {
        GameState.InitJobEditor = true;
    }

    public static void Packet_EditItem(ReadOnlyMemory<byte> data)
    {
        GameState.InitItemEditor = true;
    }

    private static void Packet_NpcEditor(ReadOnlyMemory<byte> data)
    {
        GameState.InitNpcEditor = true;
    }

    private static void Packet_ResourceEditor(ReadOnlyMemory<byte> data)
    {
        GameState.InitResourceEditor = true;
    }

    public static void HandleProjectileEditor(ReadOnlyMemory<byte> data)
    {
        GameState.InitProjectileEditor = true;
    }

    private static void Packet_EditShop(ReadOnlyMemory<byte> data)
    {
        GameState.InitShopEditor = true;
    }

    private static void Packet_EditSkill(ReadOnlyMemory<byte> data)
    {
        GameState.InitSkillEditor = true;
    }

    private static void Packet_Clock(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        Clock.Instance.GameSpeed = packetReader.ReadInt32();
        Clock.Instance.Time = new DateTime(BitConverter.ToInt64(packetReader.ReadBytes().ToArray(), 0));
    }

    private static void Packet_Time(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        Clock.Instance.TimeOfDay = (TimeOfDay) packetReader.ReadByte();

        switch (Clock.Instance.TimeOfDay)
        {
            case TimeOfDay.Dawn:
                TextRenderer.AddText("A chilling, refreshing, breeze has come with the morning.", (int) ColorName.DarkGray);
                break;

            case TimeOfDay.Day:
                TextRenderer.AddText("Day has dawned in this region.", (int) ColorName.DarkGray);
                break;

            case TimeOfDay.Dusk:
                TextRenderer.AddText("Dusk has begun darkening the skies...", (int) ColorName.DarkGray);
                break;

            default:
                TextRenderer.AddText("Night has fallen upon the weary travelers.", (int) ColorName.DarkGray);
                break;
        }
    }

    public static void Packet_Hotbar(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        for (var i = 0; i < Variables.MaxHotbar; i++)
        {
            Data.Player[GameState.MyIndex].Hotbar[i].Slot = buffer.ReadInt32();
            Data.Player[GameState.MyIndex].Hotbar[i].SlotType = buffer.ReadByte();
        }
    }

    public static void Packet_EditMoral(ReadOnlyMemory<byte> data)
    {
        GameState.InitMoralEditor = true;
    }

    public static void Packet_UpdateMoral(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var moralNum = packetReader.ReadInt32();

        ref var moral = ref Data.Moral[moralNum];

        moral.Name = packetReader.ReadString();
        moral.Color = packetReader.ReadByte();
        moral.NpcBlock = packetReader.ReadBoolean();
        moral.PlayerBlock = packetReader.ReadBoolean();
        moral.CanCast = packetReader.ReadBoolean();
        moral.CanDropItem = packetReader.ReadBoolean();
        moral.CanPickupItem = packetReader.ReadBoolean();
        moral.CanPk = packetReader.ReadBoolean();
        moral.DropItems = packetReader.ReadBoolean();
        moral.LoseExp = packetReader.ReadBoolean();
    }

    public static void Packet_UpdateItem(ReadOnlyMemory<byte> data)
    {
        int n;
        int i;
        var buffer = new PacketReader(data);

        n = buffer.ReadInt32();

        // Update the item
        Data.Item[n].AccessReq = buffer.ReadInt32();

        int statCount = System.Enum.GetValues(typeof(Stat)).Length;
        for (i = 0; i < statCount; i++)
            Data.Item[n].AddStat[i] = (byte)buffer.ReadInt32();

        Data.Item[n].Animation = buffer.ReadInt32();
        Data.Item[n].BindType = buffer.ReadByte();
        Data.Item[n].JobReq = buffer.ReadInt32();
        Data.Item[n].Data1 = buffer.ReadInt32();
        Data.Item[n].Data2 = buffer.ReadInt32();
        Data.Item[n].Data3 = buffer.ReadInt32();
        Data.Item[n].LevelReq = buffer.ReadInt32();
        Data.Item[n].Mastery = (byte)buffer.ReadInt32();
        Data.Item[n].Name = buffer.ReadString();
        Data.Item[n].Paperdoll = buffer.ReadInt32();
        Data.Item[n].Icon = buffer.ReadInt32();
        Data.Item[n].Price = buffer.ReadInt32();
        Data.Item[n].Rarity = (byte)buffer.ReadInt32();
        Data.Item[n].Speed = buffer.ReadInt32();

        Data.Item[n].Stackable = (byte)buffer.ReadInt32();
        Data.Item[n].Description = buffer.ReadString();

        for (i = 0; i < statCount; i++)
            Data.Item[n].StatReq[i] = (byte)buffer.ReadInt32();

        Data.Item[n].Type = (byte)buffer.ReadInt32();
        Data.Item[n].SubType = (byte)buffer.ReadInt32();
        Data.Item[n].ItemLevel = (byte)buffer.ReadInt32();

        Data.Item[n].KnockBack = (byte)buffer.ReadInt32();
        Data.Item[n].KnockBackTiles = (byte)buffer.ReadInt32();

        Data.Item[n].Projectile = buffer.ReadInt32();
        Data.Item[n].Ammo = buffer.ReadInt32();

        if (n == GameState.DescLastItem)
        {
            GameState.DescLastType = 0;
            GameState.DescLastItem = 0L;
        }
    }

    public static void Packet_UpdateAnimation(ReadOnlyMemory<byte> data)
    {
        int n;
        int i;
        var buffer = new PacketReader(data);

        n = buffer.ReadInt32();

        for (i = 0; i < Data.Animation[n].Frames.Length; i++)
            Data.Animation[n].Frames[i] = buffer.ReadInt32();

        for (i = 0; i < Data.Animation[n].LoopCount.Length; i++)
            Data.Animation[n].LoopCount[i] = buffer.ReadInt32();

        for (i = 0; i < Data.Animation[n].LoopTime.Length; i++)
            Data.Animation[n].LoopTime[i] = buffer.ReadInt32();

        Data.Animation[n].Name = buffer.ReadString();
        Data.Animation[n].Sound = buffer.ReadString();

        for (i = 0; i < Data.Animation[n].Sprite.Length; i++)
            Data.Animation[n].Sprite[i] = buffer.ReadInt32();
    }

    public static void Packet_Animation(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        Animation.Index = (byte)(Animation.Index + 1);
        if (Animation.Index >= byte.MaxValue)
            Animation.Index = 1;
        {
            if (Animation.Instance == null)
                Animation.OnClear();

            if (Animation.Instance == null)
                return;

            ref var instance = ref Animation.Instance[Animation.Index];
            instance.Timer ??= new int[2];
            instance.Used ??= new bool[2];
            instance.LoopIndex ??= new int[2];
            instance.FrameIndex ??= new int[2];
            instance.Animation = buffer.ReadInt32();
            instance.X = buffer.ReadInt32();
            instance.Y = buffer.ReadInt32();
            instance.LockType = (byte)buffer.ReadInt32();
            instance.LockIndex = buffer.ReadInt32();
            instance.Used[0] = true;
            instance.Used[1] = true;
        }
    }


    public static void Packet_MapResource(ReadOnlyMemory<byte> data)
    {
        int i;
        var buffer = new PacketReader(data);
        GameState.ResourceIndex = buffer.ReadInt32();
        GameState.ResourcesInit = false;

        if (GameState.ResourceIndex > 0)
        {
            Array.Resize(ref Data.MapResource, GameState.ResourceIndex);
            Array.Resize(ref Data.MyMapResource, GameState.ResourceIndex);

            var loopTo = GameState.ResourceIndex;
            for (i = 0; i < loopTo; i++)
            {
                Data.MyMapResource[i].State = buffer.ReadByte();
                Data.MyMapResource[i].X = buffer.ReadInt32();
                Data.MyMapResource[i].Y = buffer.ReadInt32();
            }

            GameState.ResourcesInit = true;
        }
    }

    public static void Packet_UpdateResource(ReadOnlyMemory<byte> data)
    {
        int resourceNum;
        var buffer = new PacketReader(data);
        resourceNum = buffer.ReadInt32();

        Data.Resource[resourceNum].Animation = buffer.ReadInt32();
        Data.Resource[resourceNum].EmptyMessage = buffer.ReadString();
        Data.Resource[resourceNum].ExhaustedImage = buffer.ReadInt32();
        Data.Resource[resourceNum].Health = buffer.ReadInt32();
        Data.Resource[resourceNum].ExpReward = buffer.ReadInt32();
        Data.Resource[resourceNum].ItemReward = buffer.ReadInt32();
        Data.Resource[resourceNum].Name = buffer.ReadString();
        Data.Resource[resourceNum].ResourceImage = buffer.ReadInt32();
        Data.Resource[resourceNum].ResourceType = buffer.ReadInt32();
        Data.Resource[resourceNum].RespawnTime = buffer.ReadInt32();
        Data.Resource[resourceNum].SuccessMessage = buffer.ReadString();
        Data.Resource[resourceNum].LvlRequired = buffer.ReadInt32();
        Data.Resource[resourceNum].ToolRequired = buffer.ReadInt32();
        Data.Resource[resourceNum].Walkthrough = buffer.ReadBoolean();
    }

    public static void Packet_OpenShop(ReadOnlyMemory<byte> data)
    {
        int shopnum;
        var buffer = new PacketReader(data);

        shopnum = buffer.ReadInt32();

        GameLogic.OpenShop(shopnum);
    }

    public static void Packet_ResetShopAction(ReadOnlyMemory<byte> data)
    {
        GameState.ShopAction = 0;
    }

    public static void Packet_UpdateShop(ReadOnlyMemory<byte> data)
    {
        int shopnum;
        var buffer = new PacketReader(data);
        shopnum = buffer.ReadInt32();

        Data.Shop[shopnum].BuyRate = buffer.ReadInt32();
        Data.Shop[shopnum].Name = buffer.ReadString();

        for (int i = 0; i < Variables.MaxTrades; i++)
        {
            Data.Shop[shopnum].TradeItem[i].CostItem = buffer.ReadInt32();
            Data.Shop[shopnum].TradeItem[i].CostValue = buffer.ReadInt32();
            Data.Shop[shopnum].TradeItem[i].Item = buffer.ReadInt32();
            Data.Shop[shopnum].TradeItem[i].ItemValue = buffer.ReadInt32();
        }

        if (Data.Shop[shopnum].Name is null)
            Data.Shop[shopnum].Name = "";
    }

}