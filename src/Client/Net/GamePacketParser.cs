using Client.Game.UI;
using Client.Game.UI.Windows;
using Core;
using Core.Configurations;
using Core.Globals;
using Core.Net;
using System;
using static Core.Globals.Commands;
using static Core.Globals.Type;
using Core.Objects;

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
        Bind(Packets.ServerPackets.SPlayerCharacters, Packet_PlayerCharacters);
        Bind(Packets.ServerPackets.SUpdateJob, Packet_UpdateJob);
        Bind(Packets.ServerPackets.SJobData, Packet_JobData);
        Bind(Packets.ServerPackets.SInGame, Packet_InGame);
        Bind(Packets.ServerPackets.SInventory, Packet_Inventory);
        Bind(Packets.ServerPackets.SInventoryUpdate, Packet_InventoryUpdate);
        Bind(Packets.ServerPackets.SPlayerWornEq, Packet_PlayerWornEquipment);
        Bind(Packets.ServerPackets.SPlayerHP, Packet_PlayerHP);
        Bind(Packets.ServerPackets.SPlayerMP, Packet_PlayerMP);
        Bind(Packets.ServerPackets.SPlayerSP, Packet_PlayerSP);
        Bind(Packets.ServerPackets.SPlayerStats, Packet_PlayerStats);
        Bind(Packets.ServerPackets.SPlayerData, Packet_PlayerData);
        Bind(Packets.ServerPackets.SNpcMove, Packet_NpcMove);
        Bind(Packets.ServerPackets.SPlayerDir, Packet_PlayerDir);
        Bind(Packets.ServerPackets.SNpcDir, Packet_NpcDir);
        Bind(Packets.ServerPackets.SPlayerXY, Packet_PlayerXY);
        Bind(Packets.ServerPackets.SAttack, Packet_Attack);
        Bind(Packets.ServerPackets.SNpcAttack, Packet_NpcAttack);
        Bind(Packets.ServerPackets.SCheckForMap, Packet_CheckMap);
        Bind(Packets.ServerPackets.SMapData, Packet_MapData);
        Bind(Packets.ServerPackets.SMapItemData, Packet_MapItemData);
        Bind(Packets.ServerPackets.SMapItemsData, Packet_MapItemsData);
        Bind(Packets.ServerPackets.SMapNpcData, Packet_MapNpcData);
        Bind(Packets.ServerPackets.SMapNpcUpdate, Packet_MapNpcUpdate);
        Bind(Packets.ServerPackets.SGlobalMsg, Packet_GlobalMsg);
        Bind(Packets.ServerPackets.SSendAdminMessage, Packet_SendAdminMessage);
        Bind(Packets.ServerPackets.SPlayerMsg, Packet_PlayerMsg);
        Bind(Packets.ServerPackets.SSendMapMessage, Packet_SendMapMessage);
        Bind(Packets.ServerPackets.SSpawnItem, Packet_SpawnItem);
        Bind(Packets.ServerPackets.SUpdateItem, Packet_UpdateItem);
        Bind(Packets.ServerPackets.SSpawnNpc, Packet_SpawnNpc);
        Bind(Packets.ServerPackets.SNpcDead, Packet_NpcDead);
        Bind(Packets.ServerPackets.SPlayerDead, Packet_PlayerDead);
        Bind(Packets.ServerPackets.SUpdateNpc, Packet_UpdateNpc);
        Bind(Packets.ServerPackets.SEditMap, Packet_EditMap);
        Bind(Packets.ServerPackets.SUpdateShop, Packet_UpdateShop);
        Bind(Packets.ServerPackets.SUpdateSkill, Packet_UpdateSkill);
        Bind(Packets.ServerPackets.SSkills, Packet_Skills);
        Bind(Packets.ServerPackets.SLeftMap, Packet_LeftMap);
        Bind(Packets.ServerPackets.SMapResource, Packet_MapResource);
        Bind(Packets.ServerPackets.SUpdateResource, Packet_UpdateResource);
        Bind(Packets.ServerPackets.SSendPing, Packet_Ping);
        Bind(Packets.ServerPackets.SActionMessage, Packet_ActionMessage);
        Bind(Packets.ServerPackets.SPlayerExp, Packet_PlayerExp);
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
        Bind(Packets.ServerPackets.SBank, Packet_OpenBank);
        Bind(Packets.ServerPackets.SLeftGame, Packet_LeftGame);
        Bind(Packets.ServerPackets.STradeInvite, Packet_TradeInvite);
        Bind(Packets.ServerPackets.STrade, Packet_Trade);
        Bind(Packets.ServerPackets.SCloseTrade, Packet_CloseTrade);
        Bind(Packets.ServerPackets.STradeUpdate, Packet_TradeUpdate);
        Bind(Packets.ServerPackets.STradeStatus, Packet_TradeStatus);
        Bind(Packets.ServerPackets.SMapReport, Packet_MapReport);
        Bind(Packets.ServerPackets.STarget, Packet_Target);
        Bind(Packets.ServerPackets.SAdmin, Packet_Admin);
        Bind(Packets.ServerPackets.SCritical, Packet_Critical);
        Bind(Packets.ServerPackets.SrClick, Packet_RClick);
        Bind(Packets.ServerPackets.SHotbar, Packet_Hotbar);
        Bind(Packets.ServerPackets.SSpawnEvent, Packet_SpawnEvent);
        Bind(Packets.ServerPackets.SEventMove, Packet_EventMove);
        Bind(Packets.ServerPackets.SEventDir, Packet_EventDir);
        Bind(Packets.ServerPackets.SEventChat, Packet_EventChat);
        Bind(Packets.ServerPackets.SEventStart, Packet_EventStart);
        Bind(Packets.ServerPackets.SEventEnd, Packet_EventEnd);
        Bind(Packets.ServerPackets.SPlayBgm, Packet_PlayBGM);
        Bind(Packets.ServerPackets.SPlaySound, Packet_PlaySound);
        Bind(Packets.ServerPackets.SFadeoutBgm, Packet_FadeOutBGM);
        Bind(Packets.ServerPackets.SStopSound, Packet_StopSound);
        Bind(Packets.ServerPackets.SSwitchesAndVariables, Packet_SwitchesAndVariables);
        Bind(Packets.ServerPackets.SMapEventData, Packet_MapEventData);
        Bind(Packets.ServerPackets.SChatBubble, Packet_ChatBubble);
        Bind(Packets.ServerPackets.SSpecialEffect, Packet_SpecialEffect);
        Bind(Packets.ServerPackets.SPic, Packet_Picture);
        Bind(Packets.ServerPackets.SHoldPlayer, Packet_HoldPlayer);
        Bind(Packets.ServerPackets.SUpdateProjectile, Packet_UpdateProjectile);
        Bind(Packets.ServerPackets.SMapProjectile, Packet_MapProjectile);
        Bind(Packets.ServerPackets.SEmote, Packet_Emote);
        Bind(Packets.ServerPackets.SPartyInvite, Packet_PartyInvite);
        Bind(Packets.ServerPackets.SPartyUpdate, Packet_PartyUpdate);
        Bind(Packets.ServerPackets.SPartyVitals, Packet_PartyVitals);
        Bind(Packets.ServerPackets.SClock, Packet_Clock);
        Bind(Packets.ServerPackets.STime, Packet_Time);
        Bind(Packets.ServerPackets.SScriptEditor, Packet_EditScript);
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

        Variables.MaxAnimations = r.ReadInt32();
        Core.Globals.Variables.MaxItems = r.ReadInt32();
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

        Variables.MaxBank = r.ReadByte();
        Variables.MaxJobs = r.ReadByte();
        Variables.MaxMorals = r.ReadByte();
        Variables.MaxInventory = r.ReadByte();
        Variables.MaxMapItems = r.ReadByte();
        
        Variables.MaxMapNpcs = r.ReadInt32();

        Variables.MaxNpcSkills = r.ReadByte();
        Variables.MaxPlayerSkills = r.ReadByte();
        Variables.MaxTrades = r.ReadByte();
        Variables.NameLength = r.ReadByte();
        Variables.MinimumNameLength = r.ReadByte();
        Variables.ChatLength = r.ReadByte();
        Variables.MaxHotbar = r.ReadByte();
        Variables.MaxMapX = r.ReadByte();
        Variables.MaxMapY = r.ReadByte();
        Variables.MaxDropItems = r.ReadByte();
        Variables.MaxStartItems = r.ReadByte();
        Variables.MaxStartSkills = r.ReadByte();
        Variables.MaxPoints = r.ReadInt32();
        Variables.MaxCharacters = r.ReadByte();
        Variables.MaxStats = r.ReadByte();
        Variables.MaxQuests = r.ReadByte();
        Variables.MaxGuilds = r.ReadByte();
        Variables.MaxEventChoices = r.ReadByte();

        General.ClearGameData();  
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
                    WindowManager.ShowWindow("WinCharacters");
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

        ResetClientStateForNewLogin();

        GameState.MyIndex = packetReader.ReadInt32();
    }

    private static void ResetClientStateForNewLogin()
    {
        // Wipe any leftover player instances from a previous session.
        Player.Instance.Clear();
        for (var i = 0; i < Variables.MaxPlayers; i++)
        {
            Player.Instance.Add(new Player());
        }

        // Reset per-player transient state used by movement smoothing and misc gameplay flags.
        Data.TempPlayer = new TempPlayer[Variables.MaxPlayers];

        // Clear local input/mode flags that can keep the client simulating movement.
        GameState.DirUp = false;
        GameState.DirDown = false;
        GameState.DirLeft = false;
        GameState.DirRight = false;

        GameState.InGame = false;
        GameState.GettingMap = false;
        GameState.PlayerData = false;
        GameState.MapData = false;
    }

    public static void Packet_PlayerCharacters(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var isSlotEmpty = new bool[Variables.MaxCharacters];

        if (WindowManager.TryGetControl("winLogin", "txtUsername", out var usernameCtrl))
        {
            SettingsManager.Instance.Username = usernameCtrl!.Text;
        }
        SettingsManager.Save();

        for (var i = 0; i < Variables.MaxCharacters; i++)
        {
            GameState.CharName[i] = packetReader.ReadString();
            GameState.Charactersprite[i] = packetReader.ReadInt32();
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
        WindowManager.ShowWindow("WinCharacters");

        long winNum = WindowManager.GetWindowIndex("WinCharacters");
        for (var i = 0L; i < Variables.MaxCharacters; i++)
        {
            long conNum = WindowManager.GetControlIndex("WinCharacters", "lblCharName_" + (i + 1));
            {
                var control = WindowManager.Windows[winNum].Controls[(int) conNum];

                control.Text = !isSlotEmpty[(int) i] ? (GameState.CharName[(int) i] ?? string.Empty) : "Blank Slot";
            }

            if (isSlotEmpty[(int) i])
            {
                // create button
                conNum = WindowManager.GetControlIndex("WinCharacters", "btnCreateChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = true;

                // select button
                conNum = WindowManager.GetControlIndex("WinCharacters", "btnSelectChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = false;

                // delete button
                conNum = WindowManager.GetControlIndex("WinCharacters", "btnDelChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = false;
            }
            else
            {
                // create button
                conNum = WindowManager.GetControlIndex("WinCharacters", "btnCreateChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = false;

                // select button
                conNum = WindowManager.GetControlIndex("WinCharacters", "btnSelectChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = true;

                // delete button
                conNum = WindowManager.GetControlIndex("WinCharacters", "btnDelChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = true;
            }
        }
    }

    public static void Packet_JobData(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);
        Job.Instance.Clear();

        for (var n = 0; n < Variables.MaxJobs; n++)
        {
            var job = new Job();

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
            job.MoveSpeed = packetReader.ReadSingle();

            Job.Instance.Add(job);

            if ((n + 1) == Variables.MaxJobs)
            {
                if (GameState.InitJobEditor)
                {
                    GameState.MyEditorType = EditorType.Job;
                    GameState.EditorIndex = 0;
                    WindowManager.ShowWindow("winJobEditor");
                    GameState.InitJobEditor = false;
                    Client.Game.UI.Windows.WinJobEditor.Init();
                }
            }
        }
    }

    public static void Packet_UpdateJob(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var n = packetReader.ReadInt32();
        
        if (n == 0)
            Job.Instance.Clear();

        var job = new Job();

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
        job.MoveSpeed = packetReader.ReadSingle();

        // Update the job
        Job.Instance.Add(job);

        if ((n + 1) == Variables.MaxJobs)
        {
            if (GameState.InitJobEditor)
            {
                GameState.MyEditorType = EditorType.Job;
                GameState.EditorIndex = 0;
                WindowManager.ShowWindow("winJobEditor");
                GameState.InitJobEditor = false;
                Client.Game.UI.Windows.WinJobEditor.Init();
            }
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

    private static void Packet_Inventory(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        for (var i = 0; i < Variables.MaxInventory; i++)
        {
            var itemNum = packetReader.ReadInt32();
            var amount = packetReader.ReadInt32();
            // Guard against invalid indices
            if (i >= 0 && i < Variables.MaxInventory && GameState.MyIndex >= 0)
            {
                SetInventory(GameState.MyIndex, i, itemNum);
                SetInventoryValue(GameState.MyIndex, i, amount);
            }
        }

        GameLogic.SetGoldLabel();
    }

    private static void Packet_InventoryUpdate(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var invSlot = packetReader.ReadInt32();
        var itemNum = packetReader.ReadInt32();
        var amount = packetReader.ReadInt32();

        if (invSlot >= 0 && invSlot < Variables.MaxInventory && GameState.MyIndex >= 0)
        {
            SetInventory(GameState.MyIndex, invSlot, itemNum);
            SetInventoryValue(GameState.MyIndex, invSlot, amount);
        }

        GameLogic.SetGoldLabel();
    }

    private static void Packet_PlayerWornEquipment(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        for (var i = 0; i < EquipmentCount; i++)
        {
            var itemNum = packetReader.ReadInt32();

            SetPlayerPaperdoll(GameState.MyIndex, itemNum, (Equipment)i);
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

        ref var mapNpc = ref MapNpc.Instance[mapNpcNum];

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

        ref var mapNpc = ref MapNpc.Instance[mapNpcNum];

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

        Player.Instance[playerIndex].Attacking = 1;
        Player.Instance[playerIndex].AttackTimer = General.GetTickCount();
    }

    private static void Packet_NpcAttack(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var mapNpcNum = packetReader.ReadInt32();

        MapNpc.Instance[mapNpcNum].Attacking = 1;
        MapNpc.Instance[mapNpcNum].AttackTimer = General.GetTickCount();
    }

    private static void Packet_GlobalMsg(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var message = packetReader.ReadString();

        TextRenderer.AddText(message, (int) ColorName.Yellow, channel: (byte) ChatChannel.Broadcast);
    }

    private static void Packet_SendMapMessage(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var message = packetReader.ReadString();

        TextRenderer.AddText(message, (int) ColorName.White, channel: (byte) ChatChannel.Map);
    }

    private static void Packet_SendAdminMessage(ReadOnlyMemory<byte> data)
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

        ref var mapItem = ref MapItem.Instance[mapItemNum];

        mapItem.Num = packetReader.ReadInt32();
        mapItem.Value = packetReader.ReadInt32();
        mapItem.X = packetReader.ReadInt32();
        mapItem.Y = packetReader.ReadInt32();
    }

    private static void Packet_SpawnNpc(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var mapNpcNum = packetReader.ReadInt32();

        ref var mapNpc = ref MapNpc.Instance[mapNpcNum];

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

        MapNpc.Instance[mapNpcNum].DeathTimer = Client.General.GetTickCount() + timer;
        MapNpc.OnClear(mapNpcNum);
    }

    private static void Packet_PlayerDead(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);
        var timer = packetReader.ReadInt32(); // milliseconds until respawn

        Player.Instance[packetReader.ReadInt32()].DeathTimer = Client.General.GetTickCount() + timer;
    }

    private static void Packet_UpdateNpc(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var n = packetReader.ReadInt32();

        if (n == 0)
        {
            Npc.Instance.Clear();
        }

        var npc = new Core.Objects.NpcBase
        {
            Animation = packetReader.ReadInt32(),
            AttackSay = packetReader.ReadString() ?? string.Empty,
            Behavior = packetReader.ReadByte(),
        };

        for (var i = 0; i < Variables.MaxDropItems; i++)
        {
            npc.DropChance[i] = packetReader.ReadInt32();
            npc.DropItem[i] = packetReader.ReadInt32();
            npc.DropItemValue[i] = packetReader.ReadInt32();
        }

        npc.Experience = packetReader.ReadInt32();
        npc.Faction = packetReader.ReadByte();
        npc.Hp = packetReader.ReadInt32();
        npc.Name = packetReader.ReadString() ?? string.Empty;
        npc.Range = packetReader.ReadByte();
        npc.SpawnTime = packetReader.ReadByte();
        npc.SpawnSecs = packetReader.ReadInt32();
        npc.Sprite = packetReader.ReadInt32();

        for (var i = 0; i < StatCount; i++)
        {
            npc.Stat[i] = packetReader.ReadByte();
        }

        for (var i = 0; i < Variables.MaxNpcSkills; i++)
        {
            npc.Skill[i] = packetReader.ReadByte();
        }

        npc.Level = packetReader.ReadByte();
        npc.Damage = packetReader.ReadInt32();

        npc.DeathSwitch = packetReader.ReadInt32();
        npc.DeathVariable = packetReader.ReadInt32();
        npc.DeathSwitchValue = packetReader.ReadInt32();
        npc.DeathVariableValue = packetReader.ReadInt32();

        npc.CommonEventType = packetReader.ReadByte();
        npc.CommonEventData1 = packetReader.ReadInt32();
        npc.CommonEventData2 = packetReader.ReadInt32();

        Npc.Instance.Add(npc);

        if ((n + 1) == Variables.MaxNpcs)
        {
            if (GameState.InitNpcEditor)
            {
                GameState.MyEditorType = EditorType.Npc;
                GameState.EditorIndex = 0;
                WindowManager.ShowWindow("winNpcEditor");
                GameState.InitNpcEditor = false;
                Client.Game.UI.Windows.WinNpcEditor.Init();
            }
        }
    }

    private static void Packet_UpdateSkill(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var n = packetReader.ReadInt32();

        if (n == 0)
            Skill.Instance.Clear();

        var skill = new Skill();
        skill.AccessReq = packetReader.ReadInt32();
        skill.AoE = packetReader.ReadInt32();
        skill.CastAnim = packetReader.ReadInt32();
        skill.CastTime = packetReader.ReadInt32();
        skill.CdTime = packetReader.ReadInt32();
        skill.JobReq = packetReader.ReadInt32();
        skill.Dir = packetReader.ReadByte();
        skill.Duration = packetReader.ReadInt32();
        skill.Icon = packetReader.ReadInt32();
        skill.Interval = packetReader.ReadInt32();
        skill.IsAoE = packetReader.ReadBoolean();
        skill.LevelReq = packetReader.ReadInt32();
        skill.Map = packetReader.ReadInt32();
        skill.MpCost = packetReader.ReadInt32();
        skill.Name = packetReader.ReadString();
        skill.Range = packetReader.ReadInt32();
        skill.SkillAnim = packetReader.ReadInt32();
        skill.StunDuration = packetReader.ReadInt32();
        skill.Type = packetReader.ReadByte();
        skill.Vital = packetReader.ReadInt32();
        skill.X = packetReader.ReadInt32();
        skill.Y = packetReader.ReadInt32();
        skill.IsProjectile = packetReader.ReadInt32();
        skill.Projectile = packetReader.ReadInt32();
        skill.KnockBack = packetReader.ReadByte();
        skill.KnockBackTiles = packetReader.ReadByte();
        skill.MultiDirMask = packetReader.ReadInt32();
        skill.ChainOnHitSkillId = packetReader.ReadInt32();
        skill.CommonEventType = packetReader.ReadByte();
        skill.CommonEventData1 = packetReader.ReadInt32();
        skill.CommonEventData2 = packetReader.ReadInt32();

        skill.MoveSpeedMultiplier = packetReader.ReadSingle();

        skill.SpCost = packetReader.ReadInt32();

        // Update the skill
        Skill.Instance.Add(skill);

        if ((n + 1) == Core.Globals.Variables.MaxSkills)
        {
            if (GameState.InitSkillEditor)
            {
                GameState.MyEditorType = EditorType.Skill;
                GameState.EditorIndex = 0;
                WindowManager.ShowWindow("winSkillEditor");
                GameState.InitSkillEditor = false;
                Client.Game.UI.Windows.WinSkillEditor.Init();
            }
        }
    }

    private static void Packet_Skills(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        for (var i = 0; i < Variables.MaxPlayerSkills; i++)
        {
            var skillNum = packetReader.ReadInt32();
            if (GameState.MyIndex >= 0 && i >= 0 && i < Variables.MaxPlayerSkills)
            {
                SetPlayerSkill(GameState.MyIndex, i, skillNum);
            }
        }
    }

    private static void Packet_LeftMap(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        Player.OnClear(packetReader.ReadInt32());
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


        GameLogic.CreateActionMessage(message, color, (byte) tmpType, x, y);
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
            MapNpc.Instance[mapNpcNum].Vital[i] = packetReader.ReadInt32();
        }
    }

    private static void Packet_Cooldown(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var slot = packetReader.ReadInt32();
        if (slot >= 0 && slot < Variables.MaxPlayerSkills && GameState.MyIndex >= 0)
        {
            SetPlayerSkillCd(GameState.MyIndex, slot, General.GetTickCount());
        }
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

            SetPlayerPaperdoll(playerNum, itemNum, (Equipment) i);
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

        var player = Player.Instance[playerIndex];

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

        // Guard against invalid player index or hotbar size
        if (GameState.MyIndex < 0 || GameState.MyIndex >= Player.Instance.Count)
        {
            // Consume payload to keep stream aligned even if we skip applying
            for (var i = 0; i < Variables.MaxHotbar; i++)
            {
                _ = buffer.ReadInt32();
                _ = buffer.ReadByte();
            }
            return;
        }

        for (var i = 0; i < Variables.MaxHotbar; i++)
        {
            Player.Instance[GameState.MyIndex].Hotbar[i].Slot = buffer.ReadInt32();
            Player.Instance[GameState.MyIndex].Hotbar[i].SlotType = buffer.ReadByte();
        }
    }

    public static void Packet_EditMoral(ReadOnlyMemory<byte> data)
    {
        GameState.InitMoralEditor = true;
    }

    public static void Packet_UpdateMoral(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var n = packetReader.ReadInt32();

        if (n == 0)
            Moral.Instance.Clear();

        var moral = new Moral();

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

        // Update the moral
        Moral.Instance.Add(moral);

        if ((n + 1) == Variables.MaxMorals)
        {
            if (GameState.InitMoralEditor)
            {
                GameState.MyEditorType = EditorType.Moral;
                GameState.EditorIndex = 0;
                WindowManager.ShowWindow("winMoralEditor");
                GameState.InitMoralEditor = false;
                Client.Game.UI.Windows.WinMoralEditor.Init();
            }
        }
    }

    public static void Packet_UpdateItem(ReadOnlyMemory<byte> data)
    {
        int n;
        int i;
        var buffer = new PacketReader(data);

        n = buffer.ReadInt32();

        if (n == 0)
            Item.Instance.Clear();

        var item = new Item();
        item.AccessReq = buffer.ReadInt32();

        int statCount = System.Enum.GetValues(typeof(Stat)).Length;
        for (i = 0; i < statCount; i++)
            item.AddStat[i] = buffer.ReadInt32();

        item.Animation = buffer.ReadInt32();
        item.BindType = buffer.ReadByte();
        item.JobReq = buffer.ReadInt32();
        item.Data1 = buffer.ReadInt32();
        item.Data2 = buffer.ReadInt32();
        item.Data3 = buffer.ReadInt32();
        item.LevelReq = buffer.ReadInt32();
        item.Mastery = buffer.ReadByte();
        item.Name = buffer.ReadString();
        item.Paperdoll = buffer.ReadInt32();
        item.Icon = buffer.ReadInt32();
        item.Price = buffer.ReadInt32();
        item.Rarity = buffer.ReadByte();
        item.Speed = buffer.ReadInt32();

        item.Stackable = buffer.ReadByte();
        item.Description = buffer.ReadString();

        for (i = 0; i < statCount; i++)
            item.StatReq[i] = buffer.ReadInt32();

        item.Type = buffer.ReadByte();
        item.SubType = buffer.ReadByte();
        item.ItemLevel = buffer.ReadByte();

        item.KnockBack = buffer.ReadByte();
        item.KnockBackTiles = buffer.ReadByte();

        item.Projectile = buffer.ReadInt32();
        item.Ammo = buffer.ReadInt32();

        // Common event trigger (0=None; 1.. = CommonEventTrigger + 1)
        item.CommonEventType = buffer.ReadByte();
        item.CommonEventData1 = buffer.ReadInt32();
        item.CommonEventData2 = buffer.ReadInt32();

        if (n == GameState.DescLastItem)
        {
            GameState.DescLastType = 0;
            GameState.DescLastItem = 0L;
        }

        // Update the item
        Item.Instance.Add(item);

        if ((n + 1) == Core.Globals.Variables.MaxItems)
        {
            if (GameState.InitItemEditor)
            {
                GameState.MyEditorType = EditorType.Item;
                GameState.EditorIndex = 0;
                WindowManager.ShowWindow("winItemEditor");
                GameState.InitItemEditor = false;
                Client.Game.UI.Windows.WinItemEditor.Init();
            }
        }
    }

    public static void Packet_UpdateAnimation(ReadOnlyMemory<byte> data)
    {
        int n;
        int i;
        var buffer = new PacketReader(data);

        n = buffer.ReadInt32();

        if (n == 0)
            Animation.Instance.Clear();
        
        var animation = new Animation();

        for (i = 0; i < animation.Frames.Length; i++)
            animation.Frames[i] = buffer.ReadInt32();

        for (i = 0; i < animation.LoopCount.Length; i++)
            animation.LoopCount[i] = buffer.ReadInt32();
        for (i = 0; i < animation.LoopTime.Length; i++)
            animation.LoopTime[i] = buffer.ReadInt32();

        animation.Name = buffer.ReadString();
        animation.Sound = buffer.ReadString();

        for (i = 0; i < animation.Sprite.Length; i++)
            animation.Sprite[i] = buffer.ReadInt32();

        Animation.Instance.Add(animation);

        if ((n + 1) == Variables.MaxAnimations)
        {
            if (GameState.InitAnimationEditor)
            {
                GameState.MyEditorType = EditorType.Animation;
                GameState.EditorIndex = 0;
                WindowManager.ShowWindow("winAnimationEditor");
                GameState.InitAnimationEditor = false;
                Client.Game.UI.Windows.WinAnimationEditor.Init();
            }
        }
    }

    public static void Packet_Animation(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        MapAnimation.Index = (byte)(MapAnimation.Index + 1);
        if (MapAnimation.Index >= byte.MaxValue)
            MapAnimation.Index = 1;
        {
            if (MapAnimation.Instance == null)
                MapAnimation.OnReset();

            if (MapAnimation.Instance == null)
                return;

            ref var instance = ref MapAnimation.Instance[MapAnimation.Index];
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
            var count = GameState.ResourceIndex;
            for (i = 0; i < count; i++)
            {
                Core.Objects.MapResource.Instance[i].State = buffer.ReadByte();
                Core.Objects.MapResource.Instance[i].X = buffer.ReadInt32();
                Core.Objects.MapResource.Instance[i].Y = buffer.ReadInt32();
            }

            GameState.ResourcesInit = true;
        }
    }

    public static void Packet_UpdateResource(ReadOnlyMemory<byte> data)
    {
        int n;
        var buffer = new PacketReader(data);
        n = buffer.ReadInt32();

        if (n == 0)
            Resource.Instance.Clear();

        var resource = new Resource();

        resource.Animation = buffer.ReadInt32();
        resource.EmptyMessage = buffer.ReadString();
        resource.ExhaustedImage = buffer.ReadInt32();
        resource.Health = buffer.ReadInt32();
        resource.ExperienceReward = buffer.ReadInt32();
        resource.ItemReward = buffer.ReadInt32();
        resource.Name = buffer.ReadString();
        resource.ResourceImage = buffer.ReadInt32();
        resource.ResourceType = buffer.ReadInt32();
        resource.RespawnTime = buffer.ReadInt32();
        resource.SuccessMessage = buffer.ReadString();
        resource.LvlRequired = buffer.ReadInt32();
        resource.ToolRequired = buffer.ReadInt32();
        resource.Walkthrough = buffer.ReadBoolean();

        resource.CommonEventType = buffer.ReadByte();
        resource.CommonEventData1 = buffer.ReadInt32();
        resource.CommonEventData2 = buffer.ReadInt32();

        // Update the resource
        Resource.Instance.Add(resource);

        if ((n + 1) == Variables.MaxResources)
        {
            if (GameState.InitResourceEditor)
            {
                GameState.MyEditorType = EditorType.Resource;
                GameState.EditorIndex = 0;
                WindowManager.ShowWindow("winResourceEditor");
                GameState.InitResourceEditor = false;
                Client.Game.UI.Windows.WinResourceEditor.Init();
            }
        }
    }

    public static void Packet_OpenShop(ReadOnlyMemory<byte> data)
    {
        int shopNum;
        var buffer = new PacketReader(data);

        shopNum = buffer.ReadInt32();

        GameLogic.OpenShop(shopNum);
    }

    public static void Packet_ResetShopAction(ReadOnlyMemory<byte> data)
    {
        GameState.ShopAction = 0;
    }

    public static void Packet_UpdateShop(ReadOnlyMemory<byte> data)
    {
        int n;
        var buffer = new PacketReader(data);
        n = buffer.ReadInt32();

        if (n == 0)
            Shop.Instance.Clear();

        var shop = new Shop();

        shop.BuyRate = buffer.ReadInt32();
        shop.Name = buffer.ReadString();

        for (int i = 0; i < Variables.MaxTrades; i++)
        {
            shop.TradeItem[i].CostItem = buffer.ReadInt32();
            shop.TradeItem[i].CostValue = buffer.ReadInt32();
            shop.TradeItem[i].Item = buffer.ReadInt32();
            shop.TradeItem[i].ItemValue = buffer.ReadInt32();
        }

        Shop.Instance.Add(shop);

        if ((n + 1) == Variables.MaxShops)
        {
            if (GameState.InitShopEditor)
            {
                GameState.MyEditorType = EditorType.Shop;
                GameState.EditorIndex = 0;
                WindowManager.ShowWindow("winShopEditor");
                GameState.InitShopEditor = false;
                Client.Game.UI.Windows.WinShopEditor.Init();
            }
        }
    }

    public static void Packet_TradeInvite(ReadOnlyMemory<byte> data)
    {
        int requester;
        var buffer = new PacketReader(data);

        requester = buffer.ReadInt32();
        GameLogic.Dialogue("Trade Invite", string.Format(LocalesManager.Get("Request"), Player.Instance[requester].Name), "", (byte)DialogueType.Trade, DialogueStyle.YesNo);
    }

    public static void Packet_Trade(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        Trade.InTrade = buffer.ReadInt32();

        GameLogic.ShowTrade();
    }

    public static void Packet_CloseTrade(ReadOnlyMemory<byte> data)
    {
        Trade.OnClose();
    }

    public static void Packet_TradeUpdate(ReadOnlyMemory<byte> data)
    {
        int datatype;
        var buffer = new PacketReader(data);

        datatype = buffer.ReadInt32();

        if (datatype == 0) // ours!
        {
            for (int i = 0; i < Variables.MaxInventory; i++)
            {
                Data.TradeYourOffer[i].Num = buffer.ReadInt32();
                Data.TradeYourOffer[i].Value = buffer.ReadInt32();
            }
            Trade.YourWorth = buffer.ReadInt32().ToString();
            if (WindowManager.TryGetControl("winTrade", "lblYourValue", out var lblYourValue))
            {
                lblYourValue!.Text = Trade.YourWorth + "g";
            }
        }
        else if (datatype == 1) // theirs
        {
            for (int i = 0; i < Variables.MaxInventory; i++)
            {
                Data.TradeTheirOffer[i].Num = buffer.ReadInt32();
                Data.TradeTheirOffer[i].Value = buffer.ReadInt32();
            }
            Trade.TheirWorth = buffer.ReadInt32().ToString();
            if (WindowManager.TryGetControl("winTrade", "lblTheirValue", out var lblTheirValue))
            {
                lblTheirValue!.Text = Trade.TheirWorth + "g";
            }
        }
    }

    public static void Packet_TradeStatus(ReadOnlyMemory<byte> data)
    {
        int tradestatus;
        var buffer = new PacketReader(data);

        tradestatus = buffer.ReadInt32();

        switch (tradestatus)
        {
            case 0: // clear
                {
                    if (WindowManager.TryGetControl("winTrade", "lblStatus", out var lblStatus)) lblStatus!.Text = "Choose items to offer.";
                    break;
                }
            case 1: // they've accepted
                {
                    if (WindowManager.TryGetControl("winTrade", "lblStatus", out var lblStatus)) lblStatus!.Text = "Other player has accepted.";
                    break;
                }
            case 2: // you've accepted
                {
                    if (WindowManager.TryGetControl("winTrade", "lblStatus", out var lblStatus)) lblStatus!.Text = "Waiting for other player to accept.";
                    break;
                }
            case 3: // no room
                {
                    if (WindowManager.TryGetControl("winTrade", "lblStatus", out var lblStatus)) lblStatus!.Text = "Not enough inventory space.";
                    break;
                }
        }
    }


    public static void Packet_PlayerHP(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        SetPlayerVital(GameState.MyIndex, Core.Globals.Vital.Health, buffer.ReadInt32());
        SetPlayerMaxVital(GameState.MyIndex, Core.Globals.Vital.Health, buffer.ReadInt32());

        // set max width
        if (GetPlayerVital(GameState.MyIndex, Core.Globals.Vital.Health) > 0)
        {
            GameState.BarWidthGuiHPMax = (int)Math.Round(GetPlayerVital(GameState.MyIndex, Core.Globals.Vital.Health) / 209d / (GetPlayerMaxVital(GameState.MyIndex, Core.Globals.Vital.Health) / 209d) * 209d);
        }
        else
        {
            GameState.BarWidthGuiHPMax = 0;
        }

        WinCharacter.Update();
    }

    public static void Packet_PlayerMP(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        SetPlayerVital(GameState.MyIndex, Core.Globals.Vital.Mana, buffer.ReadInt32());
        SetPlayerMaxVital(GameState.MyIndex, Core.Globals.Vital.Mana, buffer.ReadInt32());

        // set max width
        if (GetPlayerVital(GameState.MyIndex, Core.Globals.Vital.Mana) > 0)
        {
            GameState.BarWidthGuiMPMax = (int)Math.Round(GetPlayerVital(GameState.MyIndex, Core.Globals.Vital.Mana) / 209d / (GetPlayerMaxVital(GameState.MyIndex, Core.Globals.Vital.Mana) / 209d) * 209d);
        }
        else
        {
            GameState.BarWidthGuiMPMax = 0;
        }

        WinCharacter.Update();
    }

    public static void Packet_PlayerSP(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        SetPlayerVital(GameState.MyIndex, Core.Globals.Vital.Stamina, buffer.ReadInt32());
        SetPlayerMaxVital(GameState.MyIndex, Core.Globals.Vital.Stamina, buffer.ReadInt32());

        // set max width
        if (GetPlayerVital(GameState.MyIndex, Core.Globals.Vital.Stamina) > 0)
        {
            GameState.BarWidthGuiSPMax = (int)Math.Round(GetPlayerVital(GameState.MyIndex, Core.Globals.Vital.Stamina) / 209d / (GetPlayerMaxVital(GameState.MyIndex, Core.Globals.Vital.Stamina) / 209d) * 209d);
        }
        else
        {
            GameState.BarWidthGuiSPMax = 0;
        }

        WinCharacter.Update();
    }

    public static void Packet_PlayerStats(ReadOnlyMemory<byte> data)
    {
        int i;
        int index;
        var buffer = new PacketReader(data);

        index = buffer.ReadInt32();

        int statCount = Enum.GetValues(typeof(Stat)).Length;
        for (i = 0; i < statCount; i++)
            SetPlayerStat(index, (Stat)i, buffer.ReadInt32());
    }

    public static void Packet_PlayerData(ReadOnlyMemory<byte> data)
    {
        int i;
        int x;
        var buffer = new PacketReader(data);

        i = buffer.ReadInt32();

        for (int n = 0; n <= i; n++)
        {
            if (Player.Instance.Count <= n)
                Player.Instance.Add(new Player());
        }
        SetPlayerName(i, buffer.ReadString());
        SetPlayerJob(i, buffer.ReadInt32());
        SetPlayerLevel(i, buffer.ReadInt32());
        SetPlayerPoints(i, buffer.ReadInt32());
        SetPlayerSprite(i, buffer.ReadInt32());
        SetPlayerMap(i, buffer.ReadInt32());
        SetPlayerAccess(i, buffer.ReadByte());
        SetPlayerPk(i, buffer.ReadBoolean());
        Player.Instance[i].Moving = 0;

        int statCount = Enum.GetValues(typeof(Stat)).Length;
        for (x = 0; x < statCount; x++)
            SetPlayerStat(i, (Stat)x, buffer.ReadInt32());

        int resourceSkillCount = Enum.GetValues(typeof(ResourceSkill)).Length;
        for (x = 0; x < resourceSkillCount; x++)
        {
            Player.Instance[i].GatherSkills[x].SkillLevel = buffer.ReadInt32();
            Player.Instance[i].GatherSkills[x].SkillCurExperience = buffer.ReadInt32();
            Player.Instance[i].GatherSkills[x].SkillNextLevelExperience = buffer.ReadInt32();
        }

        // Check if the player is the client player
        if (i == GameState.MyIndex)
        {
            // Reset directions
            GameState.DirUp = false;
            GameState.DirDown = false;
            GameState.DirLeft = false;
            GameState.DirRight = false;

            // set form
            {
                var instance = WindowManager.Windows[WindowManager.GetWindowIndex("winCharacter")];
                instance.Controls[WindowManager.GetControlIndex("winCharacter", "lblName")].Text = "Name";
                instance.Controls[WindowManager.GetControlIndex("winCharacter", "lblJob")].Text = "Job";
                instance.Controls[WindowManager.GetControlIndex("winCharacter", "lblLevel")].Text = "Level";
                instance.Controls[WindowManager.GetControlIndex("winCharacter", "lblGuild")].Text = "Guild";
                instance.Controls[WindowManager.GetControlIndex("winCharacter", "lblName2")].Text = GetPlayerName(GameState.MyIndex);
                instance.Controls[WindowManager.GetControlIndex("winCharacter", "lblJob2")].Text = Job.Instance[GetPlayerJob(GameState.MyIndex)].Name;
                instance.Controls[WindowManager.GetControlIndex("winCharacter", "lblLevel2")].Text = GetPlayerLevel(GameState.MyIndex).ToString();
                instance.Controls[WindowManager.GetControlIndex("winCharacter", "lblGuild2")].Text = "None";
                WinCharacter.Update();

                // stats
                for (x = 0; x < statCount; x++)
                    instance.Controls[WindowManager.GetControlIndex("winCharacter", "lblStat_" + (x + 1))].Text = GetPlayerStat(GameState.MyIndex, (Stat)x).ToString();

                // points
                instance.Controls[WindowManager.GetControlIndex("winCharacter", "lblPoints")].Text = GetPlayerPoints(GameState.MyIndex).ToString();

                // grey out buttons
                if (GetPlayerPoints(GameState.MyIndex) == 0)
                {
                    for (x = 0; x < statCount; x++)
                        instance.Controls[WindowManager.GetControlIndex("winCharacter", "btnGreyStat_" + (x + 1))].Visible = true;
                }
                else
                {
                    for (x = 0; x < statCount; x++)
                        instance.Controls[WindowManager.GetControlIndex("winCharacter", "btnGreyStat_" + (x + 1))].Visible = false;
                }
            }
            GameState.PlayerData = true;
        }
    }

    public static void Packet_StopPlayerMove(ReadOnlyMemory<byte> data)
    {
        int i;
        var buffer = new PacketReader(data);

        i = buffer.ReadInt32();

        // Make sure the player is in range
        if (i < 0 || i >= Variables.MaxPlayers)
            return;

        // Stop the player from moving
        Player.Instance[i].Moving = 0;
        Player.Instance[i].IsMoving = false; // ensure per-pixel movement halts client-side
    }

    public static void Packet_PlayerDir(ReadOnlyMemory<byte> data)
    {
        int dir;
        int i;
        var buffer = new PacketReader(data);

        i = buffer.ReadInt32();
        dir = buffer.ReadByte();

        SetPlayerDir(i, dir);

        // Do not reset local player's movement state on our own echoed dir packets; this causes micro-stutters
        if (i != GameState.MyIndex)
        {
            var instance = Player.Instance[i];
            instance.Moving = 0;
        }
    }

    public static void Packet_PlayerExp(ReadOnlyMemory<byte> data)
    {
        int index;
        int tnl;
        var buffer = new PacketReader(data);
        int maxLevel = 0;

        index = buffer.ReadInt32();
        maxLevel = buffer.ReadInt32();
        GameState.MaxLevel = maxLevel;
        SetPlayerExperience(index, buffer.ReadInt32());

        tnl = buffer.ReadInt32();
        GameState.NextlevelExp = tnl;

        // set max width
        if (GetPlayerLevel(GameState.MyIndex) < GameState.MaxLevel)
        {
            if (GetPlayerExperience(GameState.MyIndex) > 0)
            {
                GameState.BarWidthGuiExpMax = (int)Math.Round(GetPlayerExperience(GameState.MyIndex) / 209d / (tnl / 209d) * 209d);
            }
            else
            {
                GameState.BarWidthGuiExpMax = 0;
            }
        }
        else
        {
            GameState.BarWidthGuiExpMax = 209;
        }

        // Update GUI if local player index is valid
        if (GameState.MyIndex >= 0 && GameState.MyIndex < Player.Instance.Count)
        {
            WinCharacter.Update();
        }
    }

    public static void Packet_PlayerXY(ReadOnlyMemory<byte> data)
    {
        int x;
        int y;
        int dir;
        int index;
        byte moving;
        var buffer = new PacketReader(data);

        index = buffer.ReadInt32();
        x = buffer.ReadInt32();
        y = buffer.ReadInt32();
        dir = buffer.ReadByte();
        moving = buffer.ReadByte();

        // Ensure player array has the target index before applying
        if (index >= 0 && index < Player.Instance.Count)
        {
            SetPlayerX(index, x);
            SetPlayerY(index, y);
            SetPlayerDir(index, dir);
            Player.Instance[index].Moving = moving;
            Player.Instance[index].IsMoving = buffer.ReadBoolean();

            // Active movement speed multiplier for smooth stepping.
            if (index >= 0 && index < Data.TempPlayer.Length)
            {
                var mult = buffer.ReadSingle();
                if (mult <= 0) mult = 1.0f;
                Data.TempPlayer[index].MoveSpeedMultiplier = mult;
            }
            else
            {
                _ = buffer.ReadSingle();
            }
        }
        else
        {
            // Consume boolean to keep reader aligned
            _ = buffer.ReadBoolean();

            // Consume multiplier
            _ = buffer.ReadSingle();
        }
    }


    public static void Packet_CheckMap(ReadOnlyMemory<byte> data)
    {
        int x;
        int y;
        int i;
        byte needMap;
        var buffer = new PacketReader(data);

        GameState.GettingMap = true;

        // Erase all players except self
        for (i = 0; i < Player.Instance.Count; i++)
        {
            if (i != GameState.MyIndex)
            {
                SetPlayerMap(i, 0);
            }
        }

        // Erase all temporary tile values
        for (i = 0; i < Variables.MaxMapNpcs; i++)
        {
            MapNpc.OnClear(i);
        }
        
        Blood.OnReset();
        Map.OnClear();
        ChatBubble.OnReset();
        MapAnimation.OnReset();

        GameState.ResourceIndex = 0;

        // Get map num
        x = buffer.ReadInt32();

        // Get revision
        y = buffer.ReadInt32();

        needMap = 1;

        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CNeedMap);
        packetWriter.WriteInt32(needMap);

        Network.Send(packetWriter);
    }

    public static void Packet_MapData(ReadOnlyMemory<byte> data)
    {
        int x;
        int y;
        int i;
        int j;
        int map;
        var buffer = new PacketReader(data);

        GameState.MapData = false;

        for (int n = 0; n <= GetPlayerMap(GameState.MyIndex); n++)
        {
            if (Client.Map.Instance.Count <= n)
                Client.Map.Instance.Add(new Map());
        }

        if (buffer.ReadInt32() == 1)
        {
            map = buffer.ReadInt32();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Name = buffer.ReadString();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Music = buffer.ReadString();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Revision = buffer.ReadInt32();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Moral = buffer.ReadByte();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tileset = buffer.ReadInt32();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Up = buffer.ReadInt32();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Down = buffer.ReadInt32();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Left = buffer.ReadInt32();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Right = buffer.ReadInt32();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].BootMap = buffer.ReadInt32();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].BootX = buffer.ReadByte();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].BootY = buffer.ReadByte();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX = buffer.ReadByte();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY = buffer.ReadByte();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Weather = buffer.ReadByte();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Fog = buffer.ReadInt32();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].WeatherIntensity = buffer.ReadInt32();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].FogOpacity = buffer.ReadByte();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].FogSpeed = buffer.ReadByte();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTint = buffer.ReadBoolean();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintR = buffer.ReadByte();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintG = buffer.ReadByte();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintB = buffer.ReadByte();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintA = buffer.ReadByte();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Panorama = buffer.ReadByte();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Parallax = buffer.ReadByte();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Brightness = buffer.ReadByte();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].NoRespawn = buffer.ReadBoolean();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Indoors = buffer.ReadBoolean();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Shop = buffer.ReadInt32();

            // Per-map camera zoom bounds
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MinZoom = buffer.ReadSingle();
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxZoom = buffer.ReadSingle();

            // Apply min zoom on map load (and keep zoom within bounds)
            var mapZoomMin = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MinZoom;
            var mapZoomMax = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxZoom;
            if (mapZoomMin <= 0) mapZoomMin = 0.5f;
            if (mapZoomMax <= 0) mapZoomMax = 4.0f;
            if (mapZoomMax < mapZoomMin) mapZoomMax = mapZoomMin;
            GameState.CameraZoom = Math.Clamp(mapZoomMin, mapZoomMin, mapZoomMax);

            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile = new Core.Globals.Type.Tile[Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX, Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY];
            Data.TileHistory = new Core.Globals.Type.TileHistory[GameState.MaxTileHistory];

            for (i = 0; i < GameState.MaxTileHistory; i++)
            {
                Data.TileHistory[i].Tile = new Core.Globals.Type.Tile[Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX, Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY];
            }

            int layerCount = Enum.GetValues(typeof(MapLayer)).Length;

            // Initialize Layer arrays for MyMap tiles
            for (int xx = 0; xx < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX; xx++)
            {
                for (int yy = 0; yy < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY; yy++)
                {
                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[xx, yy].Layer = new Core.Globals.Type.Layer[layerCount];

                    for (int l = 0; l < layerCount; l++)
                    {
                        Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[xx, yy].Layer[l] = new Core.Globals.Type.Layer
                        {
                            Tileset = 0,
                            X = 0,
                            Y = 0,
                            AutoTile = 0,
                        };

                        for (int t = 0; t < GameState.MaxTileHistory; t++)
                        {
                            Data.TileHistory[t].Tile[xx, yy].Layer = new Core.Globals.Type.Layer[layerCount];

                            Data.TileHistory[t].Tile[xx, yy].Layer[l] = new Core.Globals.Type.Layer
                            {
                                Tileset = 0,
                                X = 0,
                                Y = 0,
                                AutoTile = 0
                            };
                        }
                    }
                }
            }

            for (x = 0; x < Variables.MaxMapNpcs; x++)
                Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Npc[x] = buffer.ReadInt32();

            var count = (int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX;
            for (x = 0; x < count; x++)
            {
                var count2 = (int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY;
                for (y = 0; y < count2; y++)
                {
                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data1 = buffer.ReadInt32();
                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data2 = buffer.ReadInt32();
                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data3 = buffer.ReadInt32();
                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data1_2 = buffer.ReadInt32();
                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data2_2 = buffer.ReadInt32();
                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data3_2 = buffer.ReadInt32();
                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].DirBlock = buffer.ReadByte();

                    for (j = 0; j < GameState.MaxTileHistory; j++)
                    {
                        Data.TileHistory[j].Tile[x, y].Data1 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data1;
                        Data.TileHistory[j].Tile[x, y].Data2 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data2;
                        Data.TileHistory[j].Tile[x, y].Data3 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data3;
                        Data.TileHistory[j].Tile[x, y].Data1_2 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data1_2;
                        Data.TileHistory[j].Tile[x, y].Data2_2 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data2_2;
                        Data.TileHistory[j].Tile[x, y].Data3_2 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data3_2;
                        Data.TileHistory[j].Tile[x, y].DirBlock = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].DirBlock;
                        Data.TileHistory[j].Tile[x, y].Type = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Type;
                        Data.TileHistory[j].Tile[x, y].Type2 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Type2;
                    }

                    for (i = 0; i < layerCount; i++)
                    {
                        Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].Tileset = buffer.ReadInt32();
                        Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].X = buffer.ReadInt32();
                        Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].Y = buffer.ReadInt32();
                        Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].AutoTile = buffer.ReadByte();

                        for (j = 0; j < GameState.MaxTileHistory; j++)
                        {
                            Data.TileHistory[j].Tile[x, y].Layer[i].Tileset = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].Tileset;
                            Data.TileHistory[j].Tile[x, y].Layer[i].X = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].X;
                            Data.TileHistory[j].Tile[x, y].Layer[i].Y = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].Y;
                            Data.TileHistory[j].Tile[x, y].Layer[i].AutoTile = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].AutoTile;
                        }
                    }

                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Type = (TileType)buffer.ReadInt32();
                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Type2 = (TileType)buffer.ReadInt32();
                }
            }

            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount = buffer.ReadInt32();

            if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount > 0)
            {
                Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event = new Core.Globals.Type.Event[Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount];
                var count2 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount;
                for (i = 0; i < count2; i++)
                {               
                    ref var instance = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i];
                    instance.Name = buffer.ReadString();
                    instance.Globals = buffer.ReadByte();
                    instance.X = buffer.ReadInt32();
                    instance.Y = buffer.ReadInt32();
                    instance.PageCount = buffer.ReadInt32();
                
                    if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].PageCount > 0)
                    {
                        Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages = new Core.Globals.Type.EventPage[Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].PageCount];
                        var count3 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].PageCount;
                        for (x = 0; x < count3; x++)
                        {
                            {
                                ref var instance1 = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x];
                                instance1.ChkVariable = buffer.ReadInt32();
                                instance1.VariableIndex = buffer.ReadInt32();
                                instance1.VariableCondition = buffer.ReadInt32();
                                instance1.VariableCompare = buffer.ReadInt32();

                                instance1.ChkSwitch = buffer.ReadInt32();
                                instance1.SwitchIndex = buffer.ReadInt32();
                                instance1.SwitchCompare = buffer.ReadInt32();

                                instance1.ChkHasItem = buffer.ReadInt32();
                                instance1.HasItemIndex = buffer.ReadInt32();
                                instance1.HasItemAmount = buffer.ReadInt32();

                                instance1.ChkSelfSwitch = buffer.ReadInt32();
                                instance1.SelfSwitchIndex = buffer.ReadInt32();
                                instance1.SelfSwitchCompare = buffer.ReadInt32();

                                instance1.GraphicType = buffer.ReadByte();
                                instance1.Graphic = buffer.ReadInt32();
                                instance1.GraphicX = buffer.ReadInt32();
                                instance1.GraphicY = buffer.ReadInt32();
                                instance1.GraphicX2 = buffer.ReadInt32();
                                instance1.GraphicY2 = buffer.ReadInt32();

                                instance1.MoveType = buffer.ReadByte();
                                instance1.MoveSpeed = buffer.ReadByte();
                                instance1.MoveFreq = buffer.ReadByte();
                                instance1.MoveRouteCount = buffer.ReadInt32();
                                instance1.IgnoreMoveRoute = buffer.ReadInt32();
                                instance1.RepeatMoveRoute = buffer.ReadInt32();

                                if (instance1.MoveRouteCount > 0)
                                {
                                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].MoveRoute = new Core.Globals.Type.MoveRoute[instance1.MoveRouteCount];
                                    var count4 = instance1.MoveRouteCount;
                                    for (y = 0; y < count4; y++)
                                    {
                                        instance1.MoveRoute[y].Index = buffer.ReadInt32();
                                        instance1.MoveRoute[y].Data1 = buffer.ReadInt32();
                                        instance1.MoveRoute[y].Data2 = buffer.ReadInt32();
                                        instance1.MoveRoute[y].Data3 = buffer.ReadInt32();
                                        instance1.MoveRoute[y].Data4 = buffer.ReadInt32();
                                        instance1.MoveRoute[y].Data5 = buffer.ReadInt32();
                                        instance1.MoveRoute[y].Data6 = buffer.ReadInt32();
                                    }
                                }

                                instance1.IdleAnim = buffer.ReadInt32();
                                instance1.DirFix = buffer.ReadInt32();
                                instance1.WalkThrough = buffer.ReadInt32();
                                instance1.ShowName = buffer.ReadInt32();
                                instance1.Trigger = buffer.ReadByte();
                                instance1.CommandListCount = buffer.ReadInt32();
                                instance1.Position = buffer.ReadByte();
                            }

                            if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandListCount > 0)
                            {
                                Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList = new Core.Globals.Type.CommandList[Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandListCount];
                                var count5 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandListCount;
                                for (y = 0; y < count5; y++)
                                {
                                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount = buffer.ReadInt32();
                                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].ParentList = buffer.ReadInt32();
                                    if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount > 0)
                                    {
                                        Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].Commands = new Core.Globals.Type.EventCommand[Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount];
                                        for (int z = 0, count6 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount; z < count6; z++)
                                        {
                                            {
                                                ref var instance2 = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].Commands[z];
                                                instance2.Index = buffer.ReadInt32();
                                                instance2.Text1 = buffer.ReadString();
                                                instance2.Text2 = buffer.ReadString();
                                                instance2.Text3 = buffer.ReadString();
                                                instance2.Text4 = buffer.ReadString();
                                                instance2.Text5 = buffer.ReadString();
                                                instance2.Data1 = buffer.ReadInt32();
                                                instance2.Data2 = buffer.ReadInt32();
                                                instance2.Data3 = buffer.ReadInt32();
                                                instance2.Data4 = buffer.ReadInt32();
                                                instance2.Data5 = buffer.ReadInt32();
                                                instance2.Data6 = buffer.ReadInt32();
                                                instance2.ConditionalBranch.CommandList = buffer.ReadInt32();
                                                instance2.ConditionalBranch.Condition = buffer.ReadInt32();
                                                instance2.ConditionalBranch.Data1 = buffer.ReadInt32();
                                                instance2.ConditionalBranch.Data2 = buffer.ReadInt32();
                                                instance2.ConditionalBranch.Data3 = buffer.ReadInt32();
                                                instance2.ConditionalBranch.ElseCommandList = buffer.ReadInt32();
                                                instance2.MoveRouteCount = buffer.ReadInt32();
                                                if (instance2.MoveRouteCount > 0)
                                                {
                                                    Array.Resize(ref instance2.MoveRoute, instance2.MoveRouteCount);
                                                    for (int w = 0, count7 = instance2.MoveRouteCount; w < count7; w++)
                                                    {
                                                        instance2.MoveRoute[w].Index = buffer.ReadInt32();
                                                        instance2.MoveRoute[w].Data1 = buffer.ReadInt32();
                                                        instance2.MoveRoute[w].Data2 = buffer.ReadInt32();
                                                        instance2.MoveRoute[w].Data3 = buffer.ReadInt32();
                                                        instance2.MoveRoute[w].Data4 = buffer.ReadInt32();
                                                        instance2.MoveRoute[w].Data5 = buffer.ReadInt32();
                                                        instance2.MoveRoute[w].Data6 = buffer.ReadInt32();
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
        }

        for (i = 0; i < Variables.MaxMapItems; i++)
        {
            MapItem.Instance[i].Num = buffer.ReadInt32();
            MapItem.Instance[i].Value = buffer.ReadInt32();
            MapItem.Instance[i].X = buffer.ReadInt32();
            MapItem.Instance[i].Y = buffer.ReadInt32();
        }

        int vitalCount = Enum.GetValues(typeof(Vital)).Length;

        for (i = 0; i < Variables.MaxMapNpcs; i++)
        {
            MapNpc.Instance[i].Num = buffer.ReadInt32();
            MapNpc.Instance[i].X = buffer.ReadInt32();
            MapNpc.Instance[i].Y = buffer.ReadInt32();
            MapNpc.Instance[i].Dir = buffer.ReadByte();
            for (int n = 0; n < vitalCount; n++)
                MapNpc.Instance[i].Vital[n] = buffer.ReadInt32();
        }

        if (buffer.ReadInt32() == 1)
        {
            GameState.ResourceIndex = buffer.ReadInt32();
            GameState.ResourcesInit = false;

            if (GameState.ResourceIndex > 0)
            {
                var count = GameState.ResourceIndex;
                for (i = 0; i < count; i++)
                {
                    Core.Objects.MapResource.Instance[i].State = buffer.ReadByte();
                    Core.Objects.MapResource.Instance[i].X = buffer.ReadInt32();
                    Core.Objects.MapResource.Instance[i].Y = buffer.ReadInt32();
                }

                GameState.ResourcesInit = true;
            }
        }

        Autotile.InitAutotiles();

        GameState.MapData = true;

        for (i = 0; i < byte.MaxValue; i++)
            GameLogic.ClearActionMessage((byte)i);

        GameState.CurrentWeather = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Weather;
        GameState.CurrentWeatherIntensity = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].WeatherIntensity;
        GameState.CurrentFog = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Fog;
        GameState.CurrentFogSpeed = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].FogSpeed;
        GameState.CurrentFogOpacity = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].FogOpacity;
        GameState.CurrentTintR = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintR;
        GameState.CurrentTintG = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintG;
        GameState.CurrentTintB = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintB;
        GameState.CurrentTintA = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintA;

        GameLogic.UpdateDrawMapName();

        GameState.GettingMap = false;
        GameState.CanMoveNow = true;
    }

    public static void Packet_MapItemData(ReadOnlyMemory<byte> data)
    {
        int i;
        var buffer = new PacketReader(data);

        i = buffer.ReadByte();
        ref var instance = ref MapItem.Instance[i];
        instance.Num = buffer.ReadInt32();
        instance.Value = buffer.ReadInt32();
        instance.X = buffer.ReadInt32();
        instance.Y = buffer.ReadInt32();

    }

    public static void Packet_MapItemsData(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        for (int i = 0; i < Variables.MaxMapItems; i++)
        {
            ref var instance = ref MapItem.Instance[i];
            instance.Num = buffer.ReadInt32();
            instance.Value = buffer.ReadInt32();
            instance.X = buffer.ReadInt32();
            instance.Y = buffer.ReadInt32();
        }

    }

    public static void Packet_MapNpcData(ReadOnlyMemory<byte> data)
    {
        int i;
        var buffer = new PacketReader(data);

        for (i = 0; i < Variables.MaxMapNpcs; i++)
        {
            ref var instance = ref MapNpc.Instance[i];
            instance.Num = buffer.ReadInt32();
            instance.X = buffer.ReadInt32();
            instance.Y = buffer.ReadInt32();
            instance.Dir = buffer.ReadByte();
        }
    }

    public static void Packet_MapNpcUpdate(ReadOnlyMemory<byte> data)
    {
        int npcNum;
        var buffer = new PacketReader(data);

        npcNum = buffer.ReadInt32();

        ref var instance = ref MapNpc.Instance[npcNum];
        instance.Num = buffer.ReadInt32();
        instance.X = buffer.ReadInt32();
        instance.Y = buffer.ReadInt32();
        instance.Dir = buffer.ReadByte();
    }

    public static void Packet_EditMap(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);
        GameState.InitMapEditor = true;
        if (GameState.InitMapEditor)
        {
            GameState.MyEditorType = EditorType.Map;
            GameState.EditorIndex = 0;
            WindowManager.HideWindows();
            WindowManager.ShowWindow("winMapEditor");
            Client.Game.UI.Windows.WinMapEditor.OnLoad();
            GameState.CameraZoom = 1.0f;
            GameState.InitMapEditor = false;
        }
    }

    public static void Packet_SpawnEvent(ReadOnlyMemory<byte> data)
    {
        int id;
        var buffer = new PacketReader(data);

        GameState.CurrentEvents = buffer.ReadInt32();
        Array.Resize(ref Data.MapEvents, GameState.CurrentEvents);

        for (int i = 0; i < GameState.CurrentEvents; i++)
        {
            id = buffer.ReadInt32();

            if (id >= GameState.CurrentEvents)
                break;

            ref var instance = ref Data.MapEvents[id];
            instance.Name = buffer.ReadString();
            instance.Dir = buffer.ReadInt32();
            instance.ShowDir = instance.Dir;
            instance.GraphicType = buffer.ReadByte();
            instance.Graphic = buffer.ReadInt32();
            instance.GraphicX = buffer.ReadInt32();
            instance.GraphicX2 = buffer.ReadInt32();
            instance.GraphicY = buffer.ReadInt32();
            instance.GraphicY2 = buffer.ReadInt32();
            instance.MovementSpeed = buffer.ReadInt32();
            instance.Moving = 0;
            instance.X = buffer.ReadInt32();
            instance.Y = buffer.ReadInt32();
            instance.Position = buffer.ReadByte();
            instance.Visible = buffer.ReadBoolean();
            instance.IdleAnim = buffer.ReadInt32();
            instance.DirFix = buffer.ReadInt32();
            instance.WalkThrough = buffer.ReadInt32();
            instance.ShowName = buffer.ReadInt32();
        }
    }

    public static void Packet_EventMove(ReadOnlyMemory<byte> data)
    {
        int id;
        int x;
        int y;
        int dir;
        int showDir;
        int movementSpeed;
        var buffer = new PacketReader(data);

        id = buffer.ReadInt32();
        // Server sends event move coordinates in tile units; client stores/draws them in world pixels.
        x = buffer.ReadInt32() * Constants.TileSize;
        y = buffer.ReadInt32() * Constants.TileSize;
        dir = buffer.ReadInt32();
        showDir = buffer.ReadInt32();
        movementSpeed = buffer.ReadInt32();

        if (id > GameState.CurrentEvents)
            return;

        {
            if (Data.MapEvents == null)
                return;
            ref var instance = ref Data.MapEvents[id];
            instance.X = x;
            instance.Y = y;
            instance.Dir = dir;
            instance.Moving = 1;
            instance.ShowDir = showDir;
            instance.MovementSpeed = movementSpeed;
        }
    }

    public static void Packet_EventDir(ReadOnlyMemory<byte> data)
    {
        int i;
        byte dir;
        var buffer = new PacketReader(data);
        i = buffer.ReadInt32();
        dir = (byte)buffer.ReadInt32();

        if (i > GameState.CurrentEvents)
            return;
        {
            if (Data.MapEvents == null)
                return;
            ref var instance = ref Data.MapEvents[i];
            instance.Dir = dir;
            instance.ShowDir = dir;
            instance.Moving = 0;
        }
    }

    public static void Packet_SwitchesAndVariables(ReadOnlyMemory<byte> data)
    {
        int i;
        var buffer = new PacketReader(data);

        for (i = 0; i < Core.Globals.Variables.MaxSwitches; i++)
            Event.Switches[i] = buffer.ReadString();

        for (i = 0; i < Core.Globals.Variables.MaxVariables; i++)
            Event.Variables[i] = buffer.ReadString();
    }

    public static void Packet_MapEventData(ReadOnlyMemory<byte> data)
    {
        int i;
        int x;
        int y;
        int z;
        int w;
        var buffer = new PacketReader(data);

        Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount = buffer.ReadInt32();

        if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount > 0)
        {
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event = new Core.Globals.Type.Event[Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount];
            var count = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount;
            for (i = 0; i < count; i++)
            {                
                ref var instance = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i];
                instance.Name = buffer.ReadString();
                instance.Globals = buffer.ReadByte();
                instance.X = buffer.ReadInt32();
                instance.Y = buffer.ReadInt32();
                instance.PageCount = buffer.ReadInt32();

                if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].PageCount > 0)
                {
                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages = new Core.Globals.Type.EventPage[Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].PageCount];
                    var count2 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].PageCount;
                    for (x = 0; x < count2; x++)
                    {
                        ref var instance1 = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x];
                        instance1.ChkVariable = buffer.ReadInt32();
                        instance1.VariableIndex = buffer.ReadInt32();
                        instance1.VariableCondition = buffer.ReadInt32();
                        instance1.VariableCompare = buffer.ReadInt32();
                        instance1.ChkSwitch = buffer.ReadInt32();
                        instance1.SwitchIndex = buffer.ReadInt32();
                        instance1.SwitchCompare = buffer.ReadInt32();
                        instance1.ChkHasItem = buffer.ReadInt32();
                        instance1.HasItemIndex = buffer.ReadInt32();
                        instance1.HasItemAmount = buffer.ReadInt32();
                        instance1.ChkSelfSwitch = buffer.ReadInt32();
                        instance1.SelfSwitchIndex = buffer.ReadInt32();
                        instance1.SelfSwitchCompare = buffer.ReadInt32();
                        instance1.GraphicType = buffer.ReadByte();
                        instance1.Graphic = buffer.ReadInt32();
                        instance1.GraphicX = buffer.ReadInt32();
                        instance1.GraphicY = buffer.ReadInt32();
                        instance1.GraphicX2 = buffer.ReadInt32();
                        instance1.GraphicY2 = buffer.ReadInt32();

                        instance1.MoveType = buffer.ReadByte();
                        instance1.MoveSpeed = buffer.ReadByte();
                        instance1.MoveFreq = buffer.ReadByte();
                        instance1.MoveRouteCount = buffer.ReadInt32();
                        instance1.IgnoreMoveRoute = buffer.ReadInt32();
                        instance1.RepeatMoveRoute = buffer.ReadInt32();

                        if (instance1.MoveRouteCount > 0)
                        {
                            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].MoveRoute = new Core.Globals.Type.MoveRoute[instance1.MoveRouteCount];
                            var count3 = instance1.MoveRouteCount;
                            for (y = 0; y < count3; y++)
                            {
                                instance1.MoveRoute[y].Index = buffer.ReadInt32();
                                instance1.MoveRoute[y].Data1 = buffer.ReadInt32();
                                instance1.MoveRoute[y].Data2 = buffer.ReadInt32();
                                instance1.MoveRoute[y].Data3 = buffer.ReadInt32();
                                instance1.MoveRoute[y].Data4 = buffer.ReadInt32();
                                instance1.MoveRoute[y].Data5 = buffer.ReadInt32();
                                instance1.MoveRoute[y].Data6 = buffer.ReadInt32();
                            }
                        }

                        instance1.IdleAnim = buffer.ReadInt32();
                        instance1.DirFix = buffer.ReadInt32();
                        instance1.WalkThrough = buffer.ReadInt32();
                        instance1.ShowName = buffer.ReadInt32();
                        instance1.Trigger = buffer.ReadByte();
                        instance1.CommandListCount = buffer.ReadInt32();
                        instance1.Position = buffer.ReadByte();

                        if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandListCount > 0)
                        {
                            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList = new Core.Globals.Type.CommandList[Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandListCount];
                            var count4 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandListCount;
                            for (y = 0; y < count4; y++)
                            {
                                Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount = buffer.ReadInt32();
                                Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].ParentList = buffer.ReadInt32();
                                if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount > 0)
                                {
                                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].Commands = new Core.Globals.Type.EventCommand[Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount];
                                    var count5 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount;
                                    for (z = 0; z < count5; z++)
                                    {
                                        {
                                            ref var instance2 = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].Commands[z];
                                            instance2.Index = buffer.ReadInt32();
                                            instance2.Text1 = buffer.ReadString();
                                            instance2.Text2 = buffer.ReadString();
                                            instance2.Text3 = buffer.ReadString();
                                            instance2.Text4 = buffer.ReadString();
                                            instance2.Text5 = buffer.ReadString();
                                            instance2.Data1 = buffer.ReadInt32();
                                            instance2.Data2 = buffer.ReadInt32();
                                            instance2.Data3 = buffer.ReadInt32();
                                            instance2.Data4 = buffer.ReadInt32();
                                            instance2.Data5 = buffer.ReadInt32();
                                            instance2.Data6 = buffer.ReadInt32();
                                            instance2.ConditionalBranch.CommandList = buffer.ReadInt32();
                                            instance2.ConditionalBranch.Condition = buffer.ReadInt32();
                                            instance2.ConditionalBranch.Data1 = buffer.ReadInt32();
                                            instance2.ConditionalBranch.Data2 = buffer.ReadInt32();
                                            instance2.ConditionalBranch.Data3 = buffer.ReadInt32();
                                            instance2.ConditionalBranch.ElseCommandList = buffer.ReadInt32();
                                            instance2.MoveRouteCount = buffer.ReadInt32();

                                            if (instance2.MoveRouteCount > 0)
                                            {
                                                instance2.MoveRoute = new Core.Globals.Type.MoveRoute[instance2.MoveRouteCount];
                                                var count6 = instance2.MoveRouteCount;
                                                for (w = 0; w < count6; w++)
                                                {
                                                    instance2.MoveRoute[w].Index = buffer.ReadInt32();
                                                    instance2.MoveRoute[w].Data1 = buffer.ReadInt32();
                                                    instance2.MoveRoute[w].Data2 = buffer.ReadInt32();
                                                    instance2.MoveRoute[w].Data3 = buffer.ReadInt32();
                                                    instance2.MoveRoute[w].Data4 = buffer.ReadInt32();
                                                    instance2.MoveRoute[w].Data5 = buffer.ReadInt32();
                                                    instance2.MoveRoute[w].Data6 = buffer.ReadInt32();
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
    }

    public static void Packet_EventChat(ReadOnlyMemory<byte> data)
    {
        int i;
        int choices;
        var buffer = new PacketReader(data);
        Event.EventReplyId = buffer.ReadInt32();
        Event.EventReplyPage = buffer.ReadInt32();
        Event.EventChatFace = buffer.ReadInt32();
        Event.EventText = buffer.ReadString();
        if (string.IsNullOrEmpty(Event.EventText))
            Event.EventText = " ";
        Event.EventChat = true;
        Event.ShowEventLbl = true;
        choices = buffer.ReadInt32();

        for (i = 0; i < Core.Globals.Variables.MaxEventChoices; i++)
        {
            Event.EventChoices[i] = "";
            Event.EventChoiceVisible[i] = false;
        }

        Event.EventChatType = 0;
        if (choices == 0)
        {
        }
        else
        {
            Event.EventChatType = 1;
            var count = choices;
            for (i = 0; i < count; i++)
            {
                Event.EventChoices[i] = buffer.ReadString();
                Event.EventChoiceVisible[i] = true;
            }
        }

        Event.AnotherChat = buffer.ReadInt32();
    }

    public static void Packet_EventStart(ReadOnlyMemory<byte> data)
    {
        Event.InEvent = true;
    }

    public static void Packet_EventEnd(ReadOnlyMemory<byte> data)
    {
        Event.InEvent = false;
    }

    public static void Packet_Picture(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);
        int picIndex;
        int spriteType;
        int xOffset;
        int yOffset;
        int eventid;

        eventid = buffer.ReadInt32();
        picIndex = buffer.ReadByte();

        if (picIndex == 0)
        {
            Event.Picture.Index = 0;
            Event.Picture.EventId = 0;
            Event.Picture.SpriteType = 0;
            Event.Picture.XOffset = 0;
            Event.Picture.YOffset = 0;
            return;
        }

        spriteType = buffer.ReadByte();
        xOffset = buffer.ReadByte();
        yOffset = buffer.ReadByte();

        Event.Picture.Index = (byte)picIndex;
        Event.Picture.EventId = eventid;
        Event.Picture.SpriteType = (byte)spriteType;
        Event.Picture.XOffset = (byte)xOffset;
        Event.Picture.YOffset = (byte)yOffset;
    }

    public static void Packet_HidePicture(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        Event.Picture = default;
    }

    public static void Packet_HoldPlayer(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);
        if (buffer.ReadInt32() == 0)
        {
            Event.HoldPlayer = true;
        }
        else
        {
            Event.HoldPlayer = false;
        }
    }

    public static void Packet_PlayBGM(ReadOnlyMemory<byte> data)
    {
        string music;
        var buffer = new PacketReader(data);

        music = buffer.ReadString();
        Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Music = music;
    }

    public static void Packet_FadeOutBGM(ReadOnlyMemory<byte> data)
    {
        Audio.CurrentMusic = "";
        Audio.FadeOutSwitch = true;
    }

    public static void Packet_PlaySound(ReadOnlyMemory<byte> data)
    {
        string sound;
        var buffer = new PacketReader(data);
        int x;
        int y;

        sound = buffer.ReadString();
        x = buffer.ReadInt32();
        y = buffer.ReadInt32();

        Audio.PlaySound(sound, x, y);
    }

    public static void Packet_StopSound(ReadOnlyMemory<byte> data)
    {
        Audio.StopSound();
    }

    public static void Packet_SpecialEffect(ReadOnlyMemory<byte> data)
    {
        int effectType;
        var buffer = new PacketReader(data);
        effectType = buffer.ReadInt32();

        switch (effectType)
        {
            case GameState.EffectTypeFadein:
                {
                    GameState.UseFade = true;
                    GameState.FadeType = 1;
                    GameState.FadeAmount = 0;
                    break;
                }
            case GameState.EffectTypeFadeout:
                {
                    GameState.UseFade = true;
                    GameState.FadeType = 0;
                    GameState.FadeAmount = 255;
                    break;
                }
            case GameState.EffectTypeFlash:
                {
                    GameState.FlashTimer = General.GetTickCount() + 150;
                    break;
                }
            case GameState.EffectTypeFog:
                {
                    GameState.CurrentFog = buffer.ReadInt32();
                    GameState.CurrentFogSpeed = buffer.ReadInt32();
                    GameState.CurrentFogOpacity = buffer.ReadInt32();
                    break;
                }
            case GameState.EffectTypeWeather:
                {
                    GameState.CurrentWeather = buffer.ReadInt32();
                    GameState.CurrentWeatherIntensity = buffer.ReadInt32();
                    break;
                }
            case GameState.EffectTypeTint:
                {
                    Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTint = true;
                    GameState.CurrentTintR = buffer.ReadInt32();
                    GameState.CurrentTintG = buffer.ReadInt32();
                    GameState.CurrentTintB = buffer.ReadInt32();
                    GameState.CurrentTintA = buffer.ReadInt32();
                    break;
                }
        }
    }

    public static void Packet_UpdateProjectile(ReadOnlyMemory<byte> data)
    {
        int n;
        var buffer = new PacketReader(data);
        n = buffer.ReadInt32();

        if (n == 0)
        {
            Projectile.Instance.Clear();
        }

        var projectile = new Projectile();



        projectile.Name = buffer.ReadString();
        projectile.Sprite = buffer.ReadInt32();
        projectile.Range = (byte)buffer.ReadInt32();
        projectile.Speed = buffer.ReadInt32();
        projectile.Damage = buffer.ReadInt32();
        projectile.Animation = buffer.ReadInt32();

        // Update the projectile
        Projectile.Instance.Add(projectile);

        if ((n + 1) == Core.Globals.Variables.MaxProjectiles)
        {
            if (GameState.InitProjectileEditor)
            {
                GameState.MyEditorType = EditorType.Projectile;
                GameState.EditorIndex = 0;
                WindowManager.ShowWindow("winProjectileEditor");
                GameState.InitProjectileEditor = false;
                Client.Game.UI.Windows.WinProjectileEditor.Init();
            }
        }
    }

    public static void Packet_MapProjectile(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);
        int i = buffer.ReadInt32();

        {
            ref var instance = ref Data.MapProjectile[Player.Instance[GameState.MyIndex].Map, i];
            instance.ProjectileNum = buffer.ReadInt32();
            instance.Owner = buffer.ReadInt32();
            instance.OwnerType = buffer.ReadByte();
            instance.Dir = buffer.ReadByte();
            instance.X = buffer.ReadInt32();
            instance.Y = buffer.ReadInt32();
            // New free-aim fields
            instance.Vx = buffer.ReadInt16();
            instance.Vy = buffer.ReadInt16();
            instance.FreeAim = buffer.ReadByte();
            instance.Range = 0;
            instance.Timer = General.GetTickCount() + 60000;
        }
    }

    public static void Packet_PartyInvite(ReadOnlyMemory<byte> data)
    {
        string name;
        var buffer = new PacketReader(data);

        name = buffer.ReadString();
        GameLogic.Dialogue("Party Invite", name + " has invited you to a party.", "Would you like to join?", DialogueType.PartyInvite, DialogueStyle.YesNo);
    }

    public static void Packet_PartyUpdate(ReadOnlyMemory<byte> data)
    {
        int i;
        int inParty;
        var buffer = new PacketReader(data);

        inParty = buffer.ReadInt32();

        // exit out if we're not in a party
        if (inParty == -1)
        {
            Party.OnClear();
            WinParty.Update();
            // exit out early
            return;
        }

        // carry on otherwise
        Data.MyParty.Leader = buffer.ReadInt32();
        for (i = 0; i < Variables.MaxPartyMembers; i++)
            Data.MyParty.Member[i] = buffer.ReadInt32();
        Data.MyParty.MemberCount = buffer.ReadInt32();

        WinParty.Update();
    }

    public static void Packet_PartyVitals(ReadOnlyMemory<byte> data)
    {
        int playerNum;
        var partyindex = -1;
        var buffer = new PacketReader(data);

        // which player?
        playerNum = buffer.ReadInt32();

        // find the party number
        for (int i = 0; i < Variables.MaxPartyMembers; i++)
        {
            if (Data.MyParty.Member[i] == playerNum)
            {
                partyindex = i;
            }
        }

        // exit out if wrong data
        if (partyindex < 0 | partyindex >= Variables.MaxPartyMembers)
            return;

        // set vitals
        var vitalCount = Enum.GetNames(typeof(Vital)).Length;
        for (int i = 0; i < vitalCount; i++)
            Player.Instance[playerNum].Vital[i] = buffer.ReadInt32();

        GameLogic.UpdatePartyBars();
    }


    public static void Packet_OpenBank(ReadOnlyMemory<byte> data)
    {
        int i;
        var buffer = new PacketReader(data);

        Bank.OnReset();
        for (i = 0; i <= GameState.MyIndex; i++)
        {
            Bank.Instance.Add(new Bank());
        }

        for (i = 0; i < Variables.MaxBank; i++)
        {
            SetBank(GameState.MyIndex, (byte)i, buffer.ReadInt32());
            SetBankValue(GameState.MyIndex, (byte)i, buffer.ReadInt32());
        }

        GameState.InBank = true;

        if (!(WindowManager.Windows[WindowManager.GetWindowIndex("winBank")].Visible == true))
        {
            WindowManager.ShowWindow("winBank", resetPosition: false);
        }
    }


    public static void Packet_EditScript(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var nextChunk = packetReader.ReadInt32();
        var lineOffset = packetReader.ReadInt32();
        var numberOfLinesTotal = packetReader.ReadInt32();
        var numberOfLinesReceived = packetReader.ReadInt32();

        Array.Resize(ref Data.Script.Code, numberOfLinesTotal);

        for (var i = 0; i < numberOfLinesReceived; i++)
        {
            Data.Script.Code[lineOffset + i] = packetReader.ReadString();
        }

        if (nextChunk != -1) /* Request the next chunk if there is more data... */
        {
            var packetWriter = new PacketWriter(8);

            packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditScript);
            packetWriter.WriteInt32(nextChunk);

            Network.Send(packetWriter);

            return;
        }

        GameState.InitScriptEditor = true;
    }
}