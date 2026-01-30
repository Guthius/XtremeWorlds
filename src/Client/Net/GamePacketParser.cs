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
    public GamePacketParser()
    {
        Bind(Packets.ServerPackets.SAes, Aes);
        Bind(Packets.ServerPackets.SAlertMsg, AlertMessage);
        Bind(Packets.ServerPackets.SVariables, Variables);
        Bind(Packets.ServerPackets.SLoginOk, LoginOk);
        Bind(Packets.ServerPackets.SPlayerCharacters, PlayerCharacters);
        Bind(Packets.ServerPackets.SUpdateJob, UpdateJob);
        Bind(Packets.ServerPackets.SJobData, JobData);
        Bind(Packets.ServerPackets.SInGame, InGame);
        Bind(Packets.ServerPackets.SInventory, Inventory);
        Bind(Packets.ServerPackets.SInventoryUpdate, InventoryUpdate);
        Bind(Packets.ServerPackets.SPlayerWornEq, PlayerWornEquipment);
        Bind(Packets.ServerPackets.SPlayerHP, PlayerHP);
        Bind(Packets.ServerPackets.SPlayerMP, PlayerMP);
        Bind(Packets.ServerPackets.SPlayerSP, PlayerSP);
        Bind(Packets.ServerPackets.SPlayerStats, PlayerStats);
        Bind(Packets.ServerPackets.SPlayerData, PlayerData);
        Bind(Packets.ServerPackets.SNpcMove, NpcMove);
        Bind(Packets.ServerPackets.SPlayerDir, PlayerDir);
        Bind(Packets.ServerPackets.SNpcDir, NpcDir);
        Bind(Packets.ServerPackets.SPlayerXY, PlayerXY);
        Bind(Packets.ServerPackets.SAttack, Attack);
        Bind(Packets.ServerPackets.SNpcAttack, NpcAttack);
        Bind(Packets.ServerPackets.SCheckForMap, CheckMap);
        Bind(Packets.ServerPackets.SMapData, MapData);
        Bind(Packets.ServerPackets.SMapItemData, MapItemData);
        Bind(Packets.ServerPackets.SMapItemsData, MapItemsData);
        Bind(Packets.ServerPackets.SMapNpcData, MapNpcData);
        Bind(Packets.ServerPackets.SMapNpcUpdate, MapNpcUpdate);
        Bind(Packets.ServerPackets.SGlobalMsg, GlobalMessage);
        Bind(Packets.ServerPackets.SSendAdminMessage, SendAdminMessage);
        Bind(Packets.ServerPackets.SPlayerMsg, PlayerMessage);
        Bind(Packets.ServerPackets.SSendMapMessage, SendMapMessage);
        Bind(Packets.ServerPackets.SSpawnItem, SpawnItem);
        Bind(Packets.ServerPackets.SUpdateItem, UpdateItem);
        Bind(Packets.ServerPackets.SSpawnNpc, SpawnNpc);
        Bind(Packets.ServerPackets.SNpcDead, NpcDead);
        Bind(Packets.ServerPackets.SPlayerDead, PlayerDead);
        Bind(Packets.ServerPackets.SUpdateNpc, UpdateNpc);
        Bind(Packets.ServerPackets.SEditMap, EditMap);
        Bind(Packets.ServerPackets.SUpdateShop, UpdateShop);
        Bind(Packets.ServerPackets.SUpdateSkill, UpdateSkill);
        Bind(Packets.ServerPackets.SSkills, Skills);
        Bind(Packets.ServerPackets.SLeftMap, LeftMap);
        Bind(Packets.ServerPackets.SMapResource, MapResource);
        Bind(Packets.ServerPackets.SUpdateResource, UpdateResource);
        Bind(Packets.ServerPackets.SSendPing, Ping);
        Bind(Packets.ServerPackets.SActionMessage, ActionMessage);
        Bind(Packets.ServerPackets.SPlayerExp, PlayerExp);
        Bind(Packets.ServerPackets.SBlood, Blood);
        Bind(Packets.ServerPackets.SUpdateAnimation, UpdateAnimation);
        Bind(Packets.ServerPackets.SAnimation, Animation);
        Bind(Packets.ServerPackets.SMapNpcVitals, NpcVitals);
        Bind(Packets.ServerPackets.SCooldown, Cooldown);
        Bind(Packets.ServerPackets.SClearSkillBuffer, ClearSkillBuffer);
        Bind(Packets.ServerPackets.SStartSkillBuffer, StartSkillBuffer);
        Bind(Packets.ServerPackets.SSayMessage, SayMessage);
        Bind(Packets.ServerPackets.SOpenShop, OpenShop);
        Bind(Packets.ServerPackets.SResetShopAction, ResetShopAction);
        Bind(Packets.ServerPackets.SStunned, Stunned);
        Bind(Packets.ServerPackets.SMapWornEq, MapWornEquipment);
        Bind(Packets.ServerPackets.SBank, OpenBank);
        Bind(Packets.ServerPackets.SLeftGame, LeftGame);
        Bind(Packets.ServerPackets.STradeInvite, TradeInvite);
        Bind(Packets.ServerPackets.STrade, Trade);
        Bind(Packets.ServerPackets.SCloseTrade, CloseTrade);
        Bind(Packets.ServerPackets.STradeUpdate, TradeUpdate);
        Bind(Packets.ServerPackets.STradeStatus, TradeStatus);
        Bind(Packets.ServerPackets.SMapReport, MapReport);
        Bind(Packets.ServerPackets.STarget, Target);
        Bind(Packets.ServerPackets.SAdmin, Admin);
        Bind(Packets.ServerPackets.SCritical, Critical);
        Bind(Packets.ServerPackets.SrClick, RClick);
        Bind(Packets.ServerPackets.SHotbar, Hotbar);
        Bind(Packets.ServerPackets.SSpawnEvent, SpawnEvent);
        Bind(Packets.ServerPackets.SEventMove, EventMove);
        Bind(Packets.ServerPackets.SEventDir, EventDir);
        Bind(Packets.ServerPackets.SEventChat, EventChat);
        Bind(Packets.ServerPackets.SEventStart, EventStart);
        Bind(Packets.ServerPackets.SEventEnd, EventEnd);
        Bind(Packets.ServerPackets.SPlayBgm, PlayBGM);
        Bind(Packets.ServerPackets.SPlaySound, PlaySound);
        Bind(Packets.ServerPackets.SFadeoutBgm, FadeOutBGM);
        Bind(Packets.ServerPackets.SStopSound, StopSound);
        Bind(Packets.ServerPackets.SSwitchesAndVariables, SwitchesAndVariables);
        Bind(Packets.ServerPackets.SMapEventData, MapEventData);
        Bind(Packets.ServerPackets.SChatBubble, ChatBubble);
        Bind(Packets.ServerPackets.SSpecialEffect, SpecialEffect);
        Bind(Packets.ServerPackets.SPic, Picture);
        Bind(Packets.ServerPackets.SHoldPlayer, HoldPlayer);
        Bind(Packets.ServerPackets.SUpdateProjectile, UpdateProjectile);
        Bind(Packets.ServerPackets.SMapProjectile, MapProjectile);
        Bind(Packets.ServerPackets.SEmote, Emote);
        Bind(Packets.ServerPackets.SPartyInvite, PartyInvite);
        Bind(Packets.ServerPackets.SPartyUpdate, PartyUpdate);
        Bind(Packets.ServerPackets.SPartyVitals, PartyVitals);
        Bind(Packets.ServerPackets.SClock, Clock);
        Bind(Packets.ServerPackets.STime, Time);
        Bind(Packets.ServerPackets.SScriptEditor, EditScript);
        Bind(Packets.ServerPackets.SItemEditor, EditItem);
        Bind(Packets.ServerPackets.SNpcEditor, NpcEditor);
        Bind(Packets.ServerPackets.SShopEditor, EditShop);
        Bind(Packets.ServerPackets.SSkillEditor, EditSkill);
        Bind(Packets.ServerPackets.SResourceEditor, ResourceEditor);
        Bind(Packets.ServerPackets.SAnimationEditor, AnimationEditor);
        Bind(Packets.ServerPackets.SProjectileEditor, HandleProjectileEditor);
        Bind(Packets.ServerPackets.SJobEditor, JobEditor);
        Bind(Packets.ServerPackets.SUpdateMoral, UpdateMoral);
        Bind(Packets.ServerPackets.SMoralEditor, EditMoral);
    }

    private static async ValueTask Aes(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var keyLength = packetReader.ReadByte();
        var key = packetReader.ReadBlock(keyLength).ToArray();

        var ivLength = packetReader.ReadByte();
        var iv = packetReader.ReadBlock(ivLength).ToArray();

        General.AesKey = key;
        General.AesIV = iv;
    }

    private static async ValueTask Variables(ReadOnlyMemory<byte> data)
    {
        var r = new PacketReader(data);

        Core.Globals.Variables.MaxAnimations = r.ReadInt32();
        Core.Globals.Variables.MaxItems = r.ReadInt32();
        Core.Globals.Variables.MaxMaps = r.ReadInt32();
        Core.Globals.Variables.MaxNpcs = r.ReadInt32();
        Core.Globals.Variables.MaxParty = r.ReadInt32();
        Core.Globals.Variables.MaxPartyMembers = r.ReadInt32();
        Core.Globals.Variables.MaxPlayers = r.ReadInt32();
        Core.Globals.Variables.MaxResources = r.ReadInt32();
        Core.Globals.Variables.MaxShops = r.ReadInt32();
        Core.Globals.Variables.MaxSkills = r.ReadInt32();
        Core.Globals.Variables.MaxProjectiles = r.ReadInt32();
        Core.Globals.Variables.MaxSwitches = r.ReadInt32();
        Core.Globals.Variables.MaxVariables = r.ReadInt32();
        Core.Globals.Variables.ChatLines = r.ReadInt32();
        Core.Globals.Variables.MaxEvents = r.ReadInt32();
        Core.Globals.Variables.TileSize = r.ReadInt32();
        Core.Globals.Variables.MaxWeatherParticles = r.ReadInt32();

        Core.Globals.Variables.MaxBank = r.ReadByte();
        Core.Globals.Variables.MaxJobs = r.ReadByte();
        Core.Globals.Variables.MaxMorals = r.ReadByte();
        Core.Globals.Variables.MaxInventory = r.ReadByte();
        Core.Globals.Variables.MaxMapItems = r.ReadByte();
        
        Core.Globals.Variables.MaxMapNpcs = r.ReadInt32();

        Core.Globals.Variables.MaxNpcSkills = r.ReadByte();
        Core.Globals.Variables.MaxPlayerSkills = r.ReadByte();
        Core.Globals.Variables.MaxTrades = r.ReadByte();
        Core.Globals.Variables.NameLength = r.ReadByte();
        Core.Globals.Variables.MinimumNameLength = r.ReadByte();
        Core.Globals.Variables.ChatLength = r.ReadByte();
        Core.Globals.Variables.MaxHotbar = r.ReadByte();
        Core.Globals.Variables.MaxMapX = r.ReadByte();
        Core.Globals.Variables.MaxMapY = r.ReadByte();
        Core.Globals.Variables.MaxDropItems = r.ReadByte();
        Core.Globals.Variables.MaxStartItems = r.ReadByte();
        Core.Globals.Variables.MaxStartSkills = r.ReadByte();
        Core.Globals.Variables.MaxCharacters = r.ReadByte();
        Core.Globals.Variables.MaxStats = r.ReadByte();
        Core.Globals.Variables.MaxQuests = r.ReadByte();
        Core.Globals.Variables.MaxGuilds = r.ReadByte();
        Core.Globals.Variables.MaxEventChoices = r.ReadByte();
        Core.Globals.Variables.MaxLevel = r.ReadByte();
        Core.Globals.Variables.MaxPoints = r.ReadInt32();

        General.ClearGameData();  
    }

    private static async ValueTask AlertMessage(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        var dialogue = buffer.ReadByte();
        var menuReset = buffer.ReadByte();
        var kick = buffer.ReadBoolean();

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
        else if (kick || GameState.InGame)
        {
            GameLogic.LogoutGame();
        }
        GameLogic.DialogueAlert(dialogue);
    }

    private static async ValueTask LoginOk(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        GameState.MyIndex = packetReader.ReadInt32();

        // Reset per-character transient death/hold state on (re)entering the game session slot.
        // This prevents a death timer from one character leaking into another character on the same account.
        if (GameState.MyIndex >= 0 && GameState.MyIndex < Player.Instance.Count)
        {
            Player.Instance[GameState.MyIndex].DeathTimer = 0;
            Player.Instance[GameState.MyIndex].Dead = false;
        }
        Event.HoldPlayer = false;
    }

    public static async ValueTask PlayerCharacters(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var isSlotEmpty = new bool[Core.Globals.Variables.MaxCharacters];

        if (WindowManager.TryGetControl("winLogin", "txtUsername", out var usernameCtrl))
        {
            SettingsManager.Instance.Username = usernameCtrl!.Text;
        }
        SettingsManager.Save();

        for (var i = 0; i < Core.Globals.Variables.MaxCharacters; i++)
        {
            GameState.CharName[i] = packetReader.ReadString();
            GameState.Charactersprite[i] = packetReader.ReadInt32();
            GameState.CharAccess[i] = packetReader.ReadInt32();
            GameState.CharJob[i] = packetReader.ReadInt32();
            
            var equipmentCount = Enum.GetValues<Equipment>().Length;
            for (var j = 0; j < equipmentCount; j++)
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

        long winNum = WindowManager.GetWindow("WinCharacters");
        for (var i = 0L; i < Core.Globals.Variables.MaxCharacters; i++)
        {
            long conNum = WindowManager.GetControl("WinCharacters", "lblCharName_" + (i + 1));
            {
                var control = WindowManager.Windows[winNum].Controls[(int) conNum];

                control.Text = !isSlotEmpty[(int) i] ? (GameState.CharName[(int) i] ?? string.Empty) : "Blank Slot";
            }

            if (isSlotEmpty[(int) i])
            {
                // create button
                conNum = WindowManager.GetControl("WinCharacters", "btnCreateChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = true;

                // select button
                conNum = WindowManager.GetControl("WinCharacters", "btnSelectChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = false;

                // delete button
                conNum = WindowManager.GetControl("WinCharacters", "btnDelChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = false;
            }
            else
            {
                // create button
                conNum = WindowManager.GetControl("WinCharacters", "btnCreateChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = false;

                // select button
                conNum = WindowManager.GetControl("WinCharacters", "btnSelectChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = true;

                // delete button
                conNum = WindowManager.GetControl("WinCharacters", "btnDelChar_" + (i + 1));
                WindowManager.Windows[winNum].Controls[(int) conNum].Visible = true;
            }
        }
    }

    public static async ValueTask JobData(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);
        Job.Instance.Clear();

        for (var n = 0; n < Core.Globals.Variables.MaxJobs; n++)
        {
            var job = new Job();

            job.Name = packetReader.ReadString();
            job.Desc = packetReader.ReadString();
            job.MaleSprite = packetReader.ReadInt32();
            job.FemaleSprite = packetReader.ReadInt32();

            var statCount = Enum.GetValues<Stat>().Length;
            for (var i = 0; i < statCount; i++)
            {
                job.Stat[i] = packetReader.ReadInt32();
            }

            for (var i = 0; i < Core.Globals.Variables.MaxStartItems; i++)
            {
                job.StartItem[i] = packetReader.ReadInt32();
                job.StartValue[i] = packetReader.ReadInt32();
            }

            for (var i = 0; i < Core.Globals.Variables.MaxStartSkills; i++)
            {
                job.StartSkill[i] = packetReader.ReadInt32();
            }

            job.StartMap = packetReader.ReadInt32();
            job.StartX = packetReader.ReadByte();
            job.StartY = packetReader.ReadByte();
            job.BaseExp = packetReader.ReadInt32();
            job.MoveSpeed = packetReader.ReadSingle();

            Job.Instance.Add(job);

            if ((n + 1) == Core.Globals.Variables.MaxJobs)
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

    public static async ValueTask UpdateJob(ReadOnlyMemory<byte> data)
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

        var statCount = Enum.GetValues<Stat>().Length;
        for (var i = 0; i < statCount; i++)
        {
            job.Stat[i] = packetReader.ReadInt32();
        }

        for (var i = 0; i < Core.Globals.Variables.MaxStartItems; i++)
        {
            job.StartItem[i] = packetReader.ReadInt32();
            job.StartValue[i] = packetReader.ReadInt32();
        }

        for (var i = 0; i < Core.Globals.Variables.MaxStartSkills; i++)
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

        if ((n + 1) == Core.Globals.Variables.MaxJobs)
        {
            if (GameState.InitJobEditor)
            {
                GameState.MyEditorType = EditorType.Job;
                GameState.EditorIndex = 0;
                WindowManager.ShowWindow("winJobEditor");
                GameState.InitJobEditor = false;
                WinJobEditor.Init();
            }
        }
    }

    private static async ValueTask InGame(ReadOnlyMemory<byte> data)
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
        WinChat.Hide();

        General.GameInit();
    }

    private static async ValueTask Inventory(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        for (var i = 0; i < Core.Globals.Variables.MaxInventory; i++)
        {
            var item = packetReader.ReadInt32();
            var amount = packetReader.ReadInt32();
            var durability = packetReader.ReadInt32();

            // Guard against invalid indices
            if (i >= 0 && i < Core.Globals.Variables.MaxInventory && GameState.MyIndex >= 0)
            {
                SetInv(GameState.MyIndex, i, item);
                SetInvValue(GameState.MyIndex, i, amount);
                SetInvDurability(GameState.MyIndex, i, durability);
            }
        }

        GameLogic.SetGoldLabel();
    }

    private static async ValueTask InventoryUpdate(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);
        var inv = packetReader.ReadInt32();
        var item = packetReader.ReadInt32();
        var amount = packetReader.ReadInt32();
        var durability = packetReader.ReadInt32();

        if (inv >= 0 && inv < Core.Globals.Variables.MaxInventory && GameState.MyIndex >= 0)
        {
            SetInv(GameState.MyIndex, inv, item);
            SetInvValue(GameState.MyIndex, inv, amount);
            SetInvDurability(GameState.MyIndex, inv, durability);
        }

        GameLogic.SetGoldLabel();
    }

    private static async ValueTask PlayerWornEquipment(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);
        var equipmentCount = Enum.GetValues<Equipment>().Length;

        for (var i = 0; i < equipmentCount; i++)
        {
            var item = packetReader.ReadInt32();
            var durability = packetReader.ReadInt32();

            SetPaperdoll(GameState.MyIndex, item, (Equipment)i);
            if (GameState.MyIndex >= 0 && GameState.MyIndex < Player.Instance.Count
                && Player.Instance[GameState.MyIndex].Paperdoll is not null
                && i >= 0 && i < Player.Instance[GameState.MyIndex].Paperdoll.Length)
            {
                Player.Instance[GameState.MyIndex].Paperdoll[i].Durability = durability;
            }
            Item.OnStream(item);
        }
    }

    private static async ValueTask NpcMove(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var npc = packetReader.ReadInt32();
        var x = packetReader.ReadInt32();
        var y = packetReader.ReadInt32();
        var dir = packetReader.ReadByte();
        var movement = packetReader.ReadByte();

        ref var mapNpc = ref MapNpc.Instance[npc];

        // Server signals start of a 1-tile move. Keep the authoritative starting position,
        // initialize client-side step bookkeeping, and set moving state/dir.
        mapNpc.X = x;
        mapNpc.Y = y;
        mapNpc.Dir = dir;
        mapNpc.Moving = movement;
        Client.Npc.StartStep(npc, x, y, dir);
    }

    private static async ValueTask NpcDir(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);
        var npc = packetReader.ReadInt32();
        var dir = packetReader.ReadByte();

        ref var mapNpc = ref MapNpc.Instance[npc];

        mapNpc.Dir = dir;

        // Ensure we finish at the exact destination for the last step
        Client.Npc.SnapToDest(npc);
        mapNpc.Moving = 0;
        
        // Mark movement stop so renderer may finish the run cycle visually
        Client.Npc.MarkMoveStop(npc);
    }

    private static async ValueTask Attack(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var index = packetReader.ReadInt32();

        Player.Instance[index].Attacking = 1;
        Player.Instance[index].AttackTimer = General.GetTickCount();
    }

    private static async ValueTask NpcAttack(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);
        var npc = packetReader.ReadInt32();

        MapNpc.Instance[npc].Attacking = 1;
        MapNpc.Instance[npc].AttackTimer = General.GetTickCount();
    }

    private static async ValueTask GlobalMessage(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);
        var message = packetReader.ReadString();

        TextRenderer.AddText(message, (int) ColorName.Yellow, channel: (byte) ChatChannel.Broadcast);
    }

    private static async ValueTask SendMapMessage(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);
        var message = packetReader.ReadString();

        TextRenderer.AddText(message, (int) ColorName.White, channel: (byte) ChatChannel.Map);
    }

    private static async ValueTask SendAdminMessage(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);
        var message = packetReader.ReadString();

        TextRenderer.AddText(message, (int) ColorName.BrightCyan, channel: (byte) ChatChannel.Broadcast);
    }

    private static async ValueTask PlayerMessage(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var message = packetReader.ReadString();
        var color = packetReader.ReadInt32();

        TextRenderer.AddText(message, color, channel: (byte) ChatChannel.Private);
    }

    private static async ValueTask SpawnItem(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);
        var item = packetReader.ReadInt32();

        ref var mapItem = ref MapItem.Instance[item];

        mapItem.Num = packetReader.ReadInt32();
        mapItem.Value = packetReader.ReadInt32();
        mapItem.X = packetReader.ReadInt32();
        mapItem.Y = packetReader.ReadInt32();
        mapItem.Durability = packetReader.ReadInt32();
    }

    private static async ValueTask SpawnNpc(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);
        var i = packetReader.ReadInt32();
        ref var mapNpc = ref MapNpc.Instance[i];

        mapNpc.Num = packetReader.ReadInt32();
        mapNpc.X = packetReader.ReadInt32();
        mapNpc.Y = packetReader.ReadInt32();
        mapNpc.Dir = packetReader.ReadByte();

        // Server sends remaining ms until respawn (0 if alive)
        var deathTimer = packetReader.ReadInt32();
        mapNpc.DeathTimer = deathTimer > 0 ? Client.General.GetTickCount() + deathTimer : 0;

        var vitalCount = Enum.GetValues<Vital>().Length;
        for (i = 0; i < vitalCount; i++)
        {
            mapNpc.Vital[i] = packetReader.ReadInt32();
        }

        mapNpc.Moving = 0;
    }

    private static async ValueTask NpcDead(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var timer = packetReader.ReadInt32(); // milliseconds until respawn
        var npc = packetReader.ReadInt32();

        // Keep the corpse visible until the timer expires.
        ref var mapNpc = ref MapNpc.Instance[npc];
        mapNpc.DeathTimer = timer > 0 ? Client.General.GetTickCount() + timer : 0;
        mapNpc.Attacking = 0;
        mapNpc.AttackTimer = 0;
        mapNpc.Moving = 0;
    }

    private static async ValueTask PlayerDead(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);
        var timer = packetReader.ReadInt32(); // milliseconds until respawn
        var playerId = packetReader.ReadInt32();

        // Timer from server is remaining ms until respawn (0 if alive). Only convert to an absolute expiry when > 0.
        Player.Instance[playerId].DeathTimer = timer > 0 ? Client.General.GetTickCount() + timer : 0;

        // If we just died, hard-stop movement immediately.
        if (playerId == GameState.MyIndex && timer > 0)
        {
            Player.Instance[playerId].Moving = 0;
            Player.Instance[playerId].IsMoving = false;

            GameState.DirUp = false;
            GameState.DirDown = false;
            GameState.DirLeft = false;
            GameState.DirRight = false;

            Sender.StopPlayerMove();
        }
    }

    private static async ValueTask UpdateNpc(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var n = packetReader.ReadInt32();

        if (n == 0)
        {
            Npc.Instance.Clear();
        }

        var npc = new Npc
        {
            Animation = packetReader.ReadInt32(),
            AttackSay = packetReader.ReadString(),
            Behavior = packetReader.ReadByte(),
        };

        for (var i = 0; i < Core.Globals.Variables.MaxDropItems; i++)
        {
            npc.DropChance[i] = packetReader.ReadInt32();
            npc.DropItem[i] = packetReader.ReadInt32();
            npc.DropItemValue[i] = packetReader.ReadInt32();
        }

        npc.Experience = packetReader.ReadInt32();
        npc.Faction = packetReader.ReadByte();
        npc.Hp = packetReader.ReadInt32();
        npc.Name = packetReader.ReadString();
        npc.Range = packetReader.ReadByte();
        npc.SpawnTime = packetReader.ReadByte();
        npc.SpawnSecs = packetReader.ReadInt32();
        npc.Sprite = packetReader.ReadInt32();

        var statCount = Enum.GetValues<Stat>().Length;
        for (var i = 0; i < statCount; i++)
        {
            npc.Stat[i] = packetReader.ReadByte();
        }

        for (var i = 0; i < Core.Globals.Variables.MaxNpcSkills; i++)
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

        if ((n + 1) == Core.Globals.Variables.MaxNpcs)
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

    private static async ValueTask UpdateSkill(ReadOnlyMemory<byte> data)
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

        skill.MoveSpeed = packetReader.ReadSingle();

        // Optional trailing fields (backward compatible)
        skill.MoveCast = packetReader.ReadBoolean();

        skill.SpCost = packetReader.ReadInt32();

        skill.NextRank = packetReader.ReadInt32();
        skill.NextUses = packetReader.ReadInt32();

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

    private static async ValueTask Skills(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        for (var i = 0; i < Core.Globals.Variables.MaxPlayerSkills; i++)
        {
            var skill = packetReader.ReadInt32();
            var uses = packetReader.ReadInt32();
            if (GameState.MyIndex >= 0 && i >= 0 && i < Core.Globals.Variables.MaxPlayerSkills)
            {
                SetSkill(GameState.MyIndex, i, skill);
                SetSkillUses(GameState.MyIndex, i, uses);
            }
        }
    }

    private static async ValueTask LeftMap(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        Player.OnClear(packetReader.ReadInt32());
    }

    private static async ValueTask Ping(ReadOnlyMemory<byte> data)
    {
        GameState.PingEnd = General.GetTickCount();
        GameState.Ping = GameState.PingEnd - GameState.PingStart;
    }

    private static async ValueTask ActionMessage(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var message = packetReader.ReadString();
        var color = packetReader.ReadInt32();
        var tmpType = packetReader.ReadInt32();
        var x = packetReader.ReadInt32();
        var y = packetReader.ReadInt32();

        GameLogic.CreateActionMessage(message, color, (byte) tmpType, x, y);
    }

    private static async ValueTask Blood(ReadOnlyMemory<byte> data)
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

    private static async ValueTask NpcVitals(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var npc = packetReader.ReadInt32();
        var vitalCount = Enum.GetValues<Vital>().Length;

        for (var i = 0; i < vitalCount; i++)
        {
            MapNpc.Instance[npc].Vital[i] = packetReader.ReadInt32();
        }
    }

    private static async ValueTask Cooldown(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var slot = packetReader.ReadInt32();
        if (slot >= 0 && slot < Core.Globals.Variables.MaxPlayerSkills && GameState.MyIndex >= 0)
        {
            SetSkillCd(GameState.MyIndex, slot, General.GetTickCount());
        }
    }

    private static async ValueTask ClearSkillBuffer(ReadOnlyMemory<byte> data)
    {
        GameState.SkillBuffer = -1;
        GameState.SkillBufferTimer = 0;
    }

    private static async ValueTask StartSkillBuffer(ReadOnlyMemory<byte> data)
    {
        var reader = new PacketReader(data);
        int slot = reader.ReadInt32();
        GameState.SkillBuffer = slot;
        GameState.SkillBufferTimer = General.GetTickCount(); // could offset with serverStart if clock sync later
    }

    private static async ValueTask SayMessage(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var name = packetReader.ReadString();
        var access = (Access) packetReader.ReadInt32();
        var pk = packetReader.ReadBoolean();
        var message = packetReader.ReadString();
        var header = packetReader.ReadString();

        // Check access level
        var color = access switch
        {
            Access.Player => (byte) ColorName.White,
            Access.Moderator => (byte) ColorName.Cyan,
            Access.Mapper => (byte) ColorName.Green,
            Access.Developer => (byte) ColorName.BrightBlue,
            Access.Owner => (byte) ColorName.Yellow,
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

    private static async ValueTask Stunned(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        GameState.StunDuration = packetReader.ReadInt32();
    }

    private static async ValueTask MapWornEquipment(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);
        var player = packetReader.ReadInt32();
        var equipmentCount = Enum.GetValues<Equipment>().Length;

        for (var i = 0; i < equipmentCount; i++)
        {
            var item = packetReader.ReadInt32();
            var durability = packetReader.ReadInt32();

            SetPaperdoll(player, item, (Equipment) i);

            if (player >= 0 && player < Player.Instance.Count
                && Player.Instance[player].Paperdoll is not null
                && i >= 0 && i < Player.Instance[player].Paperdoll.Length)
            {
                Player.Instance[player].Paperdoll[i].Durability = durability;
            }
        }
    }

    private static async ValueTask Target(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        GameState.MyTarget = packetReader.ReadInt32();
        GameState.MyTargetType = packetReader.ReadInt32();
    }

    private static async ValueTask MapReport(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        for (var i = 0; i < Core.Globals.Variables.MaxMaps; i++)
        {
            GameState.MapNames[i] = packetReader.ReadString();
        }

        GameState.InitMapReport = true;
    }

    private static async ValueTask Admin(ReadOnlyMemory<byte> data)
    {
        GameState.InitAdminForm = true;
    }

    private static async ValueTask Critical(ReadOnlyMemory<byte> data)
    {
        GameState.ShakeTimerEnabled = true;
        GameState.ShakeTimer = General.GetTickCount();
    }

    private static async ValueTask RClick(ReadOnlyMemory<byte> data)
    {

    }

    private static async ValueTask Emote(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        var playerIndex = packetReader.ReadInt32();

        var player = Player.Instance[playerIndex];

        player.Emote = packetReader.ReadInt32();
        player.EmoteTimer = General.GetTickCount() + 5000;
    }

    private static async ValueTask ChatBubble(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        GameLogic.AddChatBubble(
            target: packetReader.ReadInt32(),
            targetType: (byte) packetReader.ReadInt32(),
            message: packetReader.ReadString(),
            color: packetReader.ReadInt32());
    }

    private static async ValueTask LeftGame(ReadOnlyMemory<byte> data)
    {
        GameLogic.LogoutGame();
    }

    private static async ValueTask AnimationEditor(ReadOnlyMemory<byte> data)
    {
        GameState.InitAnimationEditor = true;
    }

    private static async ValueTask JobEditor(ReadOnlyMemory<byte> data)
    {
        GameState.InitJobEditor = true;
    }

    public static async ValueTask EditItem(ReadOnlyMemory<byte> data)
    {
        GameState.InitItemEditor = true;
    }

    private static async ValueTask NpcEditor(ReadOnlyMemory<byte> data)
    {
        GameState.InitNpcEditor = true;
    }

    private static async ValueTask ResourceEditor(ReadOnlyMemory<byte> data)
    {
        GameState.InitResourceEditor = true;
    }

    public static void HandleProjectileEditor(ReadOnlyMemory<byte> data)
    {
        GameState.InitProjectileEditor = true;
    }

    private static async ValueTask EditShop(ReadOnlyMemory<byte> data)
    {
        GameState.InitShopEditor = true;
    }

    private static async ValueTask EditSkill(ReadOnlyMemory<byte> data)
    {
        GameState.InitSkillEditor = true;
    }

    private static async ValueTask Clock(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        Core.Clock.Instance.GameSpeed = packetReader.ReadInt32();
        Core.Clock.Instance.Time = new DateTime(BitConverter.ToInt64(packetReader.ReadBytes().ToArray(), 0));
    }

    private static async ValueTask Time(ReadOnlyMemory<byte> data)
    {
        var packetReader = new PacketReader(data);

        Core.Clock.Instance.TimeOfDay = (TimeOfDay) packetReader.ReadByte();

        switch (Core.Clock.Instance.TimeOfDay)
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

    public static async ValueTask Hotbar(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        // Guard against invalid player index or hotbar size
        if (GameState.MyIndex < 0 || GameState.MyIndex >= Player.Instance.Count)
        {
            // Consume payload to keep stream aligned even if we skip applying
            for (var i = 0; i < Core.Globals.Variables.MaxHotbar; i++)
            {
                _ = buffer.ReadInt32();
                _ = buffer.ReadByte();
            }
            return;
        }

        for (var i = 0; i < Core.Globals.Variables.MaxHotbar; i++)
        {
            Player.Instance[GameState.MyIndex].Hotbar[i].Slot = buffer.ReadInt32();
            Player.Instance[GameState.MyIndex].Hotbar[i].SlotType = buffer.ReadByte();
        }
    }

    public static async ValueTask EditMoral(ReadOnlyMemory<byte> data)
    {
        GameState.InitMoralEditor = true;
    }

    public static async ValueTask UpdateMoral(ReadOnlyMemory<byte> data)
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
        moral.CanUseItem = packetReader.ReadBoolean();
        moral.CanPk = packetReader.ReadBoolean();
        moral.DropItems = packetReader.ReadBoolean();
        moral.LoseExp = packetReader.ReadBoolean();

        // Update the moral
        Moral.Instance.Add(moral);

        if ((n + 1) == Core.Globals.Variables.MaxMorals)
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

    public static async ValueTask UpdateItem(ReadOnlyMemory<byte> data)
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
        item.AttackSpeed = buffer.ReadInt32();
        item.MovementSpeed = buffer.ReadSingle();

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

        item.MaxDurability = buffer.ReadInt32();

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

    public static async ValueTask UpdateAnimation(ReadOnlyMemory<byte> data)
    {
        int n;
        int i;
        var buffer = new PacketReader(data);

        n = buffer.ReadInt32();
    
        if (n == 0)
            Client.Animation.Instance.Clear();
        
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

        Client.Animation.Instance.Add(animation);

        if ((n + 1) == Core.Globals.Variables.MaxAnimations)
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

    public static async ValueTask Animation(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        MapAnimation.Index = (byte)(MapAnimation.Index + 1);
        if (MapAnimation.Index >= byte.MaxValue)
            MapAnimation.Index = 1;
        {
            if (MapAnimation.Instance == null)
                MapAnimation.OnClear();

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


    public static async ValueTask MapResource(ReadOnlyMemory<byte> data)
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

    public static async ValueTask UpdateResource(ReadOnlyMemory<byte> data)
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

        if ((n + 1) == Core.Globals.Variables.MaxResources)
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

    public static async ValueTask OpenShop(ReadOnlyMemory<byte> data)
    {
        int shopNum;
        var buffer = new PacketReader(data);

        shopNum = buffer.ReadInt32();

        GameLogic.OpenShop(shopNum);
    }

    public static async ValueTask ResetShopAction(ReadOnlyMemory<byte> data)
    {
        GameState.ShopAction = 0;
    }

    public static async ValueTask UpdateShop(ReadOnlyMemory<byte> data)
    {
        int n;
        var buffer = new PacketReader(data);
        n = buffer.ReadInt32();

        if (n == 0)
            Shop.Instance.Clear();

        var shop = new Shop();

        shop.BuyRate = buffer.ReadInt32();
        shop.Name = buffer.ReadString();

        for (int i = 0; i < Core.Globals.Variables.MaxTrades; i++)
        {
            shop.TradeItem[i].CostItem = buffer.ReadInt32();
            shop.TradeItem[i].CostValue = buffer.ReadInt32();
            shop.TradeItem[i].Item = buffer.ReadInt32();
            shop.TradeItem[i].ItemValue = buffer.ReadInt32();
        }

        Shop.Instance.Add(shop);

        if ((n + 1) == Core.Globals.Variables.MaxShops)
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

    public static async ValueTask TradeInvite(ReadOnlyMemory<byte> data)
    {
        int requester;
        var buffer = new PacketReader(data);

        requester = buffer.ReadInt32();
        GameLogic.Dialogue("Trade Invite", string.Format(LocalesManager.Get("Request"), Player.Instance[requester].Name), "", (byte)DialogueType.Trade, DialogueStyle.YesNo);
    }

    public static async ValueTask Trade(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        Client.Trade.InTrade = buffer.ReadInt32();

        GameLogic.ShowTrade();
    }

    public static async ValueTask CloseTrade(ReadOnlyMemory<byte> data)
    {
        Client.Trade.OnClose();
    }

    public static async ValueTask TradeUpdate(ReadOnlyMemory<byte> data)
    {
        int datatype;
        var buffer = new PacketReader(data);

        datatype = buffer.ReadInt32();

        if (datatype == 0) // ours!
        {
            for (int i = 0; i < Core.Globals.Variables.MaxInventory; i++)
            {
                Data.TradeYourOffer[i].Num = buffer.ReadInt32();
                Data.TradeYourOffer[i].Value = buffer.ReadInt32();
                Data.TradeYourOffer[i].Durability = buffer.ReadInt32();
            }
            Client.Trade.YourWorth = buffer.ReadInt32().ToString();
            if (WindowManager.TryGetControl("winTrade", "lblYourValue", out var lblYourValue))
            {
                lblYourValue!.Text = Client.Trade.YourWorth + "g";
            }
        }
        else if (datatype == 1) // theirs
        {
            for (int i = 0; i < Core.Globals.Variables.MaxInventory; i++)
            {
                Data.TradeTheirOffer[i].Num = buffer.ReadInt32();
                Data.TradeTheirOffer[i].Value = buffer.ReadInt32();
                Data.TradeTheirOffer[i].Durability = buffer.ReadInt32();
            }
            Client.Trade.TheirWorth = buffer.ReadInt32().ToString();
            if (WindowManager.TryGetControl("winTrade", "lblTheirValue", out var lblTheirValue))
            {
                lblTheirValue!.Text = Client.Trade.TheirWorth + "g";
            }
        }
    }

    public static async ValueTask TradeStatus(ReadOnlyMemory<byte> data)
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


    public static async ValueTask PlayerHP(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        SetVital(GameState.MyIndex, Core.Globals.Vital.Health, buffer.ReadInt32());
        SetMaxVital(GameState.MyIndex, Core.Globals.Vital.Health, buffer.ReadInt32());

        // set max width
        if (GetVital(GameState.MyIndex, Core.Globals.Vital.Health) > 0)
        {
            GameState.BarWidthGuiHPMax = (int)Math.Round(GetVital(GameState.MyIndex, Core.Globals.Vital.Health) / 209d / (GetMaxVital(GameState.MyIndex, Core.Globals.Vital.Health) / 209d) * 209d);
        }
        else
        {
            GameState.BarWidthGuiHPMax = 0;
        }

        WinCharacter.OnUpdate();
    }

    public static async ValueTask PlayerMP(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        SetVital(GameState.MyIndex, Core.Globals.Vital.Mana, buffer.ReadInt32());
        SetMaxVital(GameState.MyIndex, Core.Globals.Vital.Mana, buffer.ReadInt32());

        // set max width
        if (GetVital(GameState.MyIndex, Core.Globals.Vital.Mana) > 0)
        {
            GameState.BarWidthGuiMPMax = (int)Math.Round(GetVital(GameState.MyIndex, Core.Globals.Vital.Mana) / 209d / (GetMaxVital(GameState.MyIndex, Core.Globals.Vital.Mana) / 209d) * 209d);
        }
        else
        {
            GameState.BarWidthGuiMPMax = 0;
        }

        WinCharacter.OnUpdate();
    }

    public static async ValueTask PlayerSP(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        SetVital(GameState.MyIndex, Core.Globals.Vital.Stamina, buffer.ReadInt32());
        SetMaxVital(GameState.MyIndex, Core.Globals.Vital.Stamina, buffer.ReadInt32());

        // set max width
        if (GetVital(GameState.MyIndex, Core.Globals.Vital.Stamina) > 0)
        {
            GameState.BarWidthGuiSPMax = (int)Math.Round(GetVital(GameState.MyIndex, Core.Globals.Vital.Stamina) / 209d / (GetMaxVital(GameState.MyIndex, Core.Globals.Vital.Stamina) / 209d) * 209d);
        }
        else
        {
            GameState.BarWidthGuiSPMax = 0;
        }

        WinCharacter.OnUpdate();
    }

    public static async ValueTask PlayerStats(ReadOnlyMemory<byte> data)
    {
        int i;
        int index;
        var buffer = new PacketReader(data);

        index = buffer.ReadInt32();

        int statCount = Enum.GetValues(typeof(Stat)).Length;
        for (i = 0; i < statCount; i++)
            SetStat(index, (Stat)i, buffer.ReadInt32());
    }

    public static async ValueTask PlayerData(ReadOnlyMemory<byte> data)
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
       
        SetName(i, buffer.ReadString());
        SetJob(i, buffer.ReadByte());
        SetLevel(i, buffer.ReadInt32());
        SetPoints(i, buffer.ReadInt32());
        SetSprite(i, buffer.ReadInt32());
        SetMap(i, buffer.ReadInt32());
        SetAccess(i, buffer.ReadByte());
        SetPk(i, buffer.ReadBoolean());
        Player.Instance[i].Moving = 0;

        int statCount = Enum.GetValues(typeof(Stat)).Length;
        for (x = 0; x < statCount; x++)
            SetStat(i, (Stat)x, buffer.ReadInt32());

        int resourceSkillCount = Enum.GetValues(typeof(ResourceSkill)).Length;
        for (x = 0; x < resourceSkillCount; x++)
        {
            Player.Instance[i].GatherSkills[x].Level = buffer.ReadInt32();
            Player.Instance[i].GatherSkills[x].Exp = buffer.ReadInt32();
            Player.Instance[i].GatherSkills[x].MaxExp = buffer.ReadInt32();
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
                var instance = WindowManager.Windows[WindowManager.GetWindow("winCharacter")];
                instance.Controls[WindowManager.GetControl("winCharacter", "lblName")].Text = "Name";
                instance.Controls[WindowManager.GetControl("winCharacter", "lblJob")].Text = "Job";
                instance.Controls[WindowManager.GetControl("winCharacter", "lblLevel")].Text = "Level";
                instance.Controls[WindowManager.GetControl("winCharacter", "lblGuild")].Text = "Guild";
                instance.Controls[WindowManager.GetControl("winCharacter", "lblName2")].Text = GetName(GameState.MyIndex);
                instance.Controls[WindowManager.GetControl("winCharacter", "lblJob2")].Text = Job.Instance[GetJob(GameState.MyIndex)].Name;
                instance.Controls[WindowManager.GetControl("winCharacter", "lblLevel2")].Text = GetLevel(GameState.MyIndex).ToString();
                instance.Controls[WindowManager.GetControl("winCharacter", "lblGuild2")].Text = "None";
                WinCharacter.OnUpdate();

                // stats
                for (x = 0; x < statCount; x++)
                    instance.Controls[WindowManager.GetControl("winCharacter", "lblStat_" + (x + 1))].Text = GetStat(GameState.MyIndex, (Stat)x).ToString();

                // points
                instance.Controls[WindowManager.GetControl("winCharacter", "lblPoints")].Text = GetPoints(GameState.MyIndex).ToString();

                // grey out buttons
                if (GetPoints(GameState.MyIndex) == 0)
                {
                    for (x = 0; x < statCount; x++)
                        instance.Controls[WindowManager.GetControl("winCharacter", "btnGreyStat_" + (x + 1))].Visible = true;
                }
                else
                {
                    for (x = 0; x < statCount; x++)
                        instance.Controls[WindowManager.GetControl("winCharacter", "btnGreyStat_" + (x + 1))].Visible = false;
                }
            }
            GameState.PlayerData = true;
        }
    }

    public static async ValueTask StopPlayerMove(ReadOnlyMemory<byte> data)
    {
        int i;
        var buffer = new PacketReader(data);

        i = buffer.ReadInt32();

        // Make sure the player is in range
        if (i < 0 || i >= Core.Globals.Variables.MaxPlayers)
            return;

        // Stop the player from moving
        Player.Instance[i].Moving = 0;
        Player.Instance[i].IsMoving = false; // ensure per-pixel movement halts client-side
    }

    public static async ValueTask PlayerDir(ReadOnlyMemory<byte> data)
    {
        int dir;
        int i;
        var buffer = new PacketReader(data);

        i = buffer.ReadInt32();
        dir = buffer.ReadByte();

        SetDir(i, dir);

        // Do not reset local player's movement state on our own echoed dir packets; this causes micro-stutters
        if (i != GameState.MyIndex)
        {
            var instance = Player.Instance[i];
            instance.Moving = 0;
        }
    }

    public static async ValueTask PlayerExp(ReadOnlyMemory<byte> data)
    {
        int index;
        int tnl;
        var buffer = new PacketReader(data);

        index = buffer.ReadInt32();
        SetExp(index, buffer.ReadInt32());

        tnl = buffer.ReadInt32();
        GameState.NextlevelExp = tnl;

        // set max width
        if (GetLevel(GameState.MyIndex) < Core.Globals.Variables.MaxLevel)
        {
            if (GetExp(GameState.MyIndex) > 0)
            {
                GameState.BarWidthGuiExpMax = (int)Math.Round(GetExp(GameState.MyIndex) / 209d / (tnl / 209d) * 209d);
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
            WinCharacter.OnUpdate();
        }
    }

    public static async ValueTask PlayerXY(ReadOnlyMemory<byte> data)
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
            SetX(index, x);
            SetY(index, y);
            SetDir(index, dir);
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


    public static async ValueTask CheckMap(ReadOnlyMemory<byte> data)
    {
        int x;
        int y;
        int i;
        int needMap;
        var buffer = new PacketReader(data);

        GameState.GettingMap = true;

        // Erase all players except self
        for (i = 0; i < Player.Instance.Count; i++)
        {
            if (i != GameState.MyIndex)
            {
                SetMap(i, 0);
            }
        }

        // Erase all temporary tile values
        for (i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
        {
            MapNpc.OnClear(i);
        }
        
        global::Client.Blood.OnClear();
        Map.OnClear();
        global::ChatBubble.OnClear();
        MapAnimation.OnClear();

        GameState.ResourceIndex = 0;

        // Get map num
        x = buffer.ReadInt32();

        // Get revision
        y = buffer.ReadInt32();

        // Critical: update our local player's map immediately so subsequent SMapData is stored
        // and rendered from the correct map index (SPlayerXY does not include map).
        if (GameState.MyIndex >= 0 && GameState.MyIndex < Player.Instance.Count)
        {
            SetMap(GameState.MyIndex, x);
        }

        needMap = 1;

        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CNeedMap);
        packetWriter.WriteInt32(needMap);

        Network.Send(packetWriter);
    }

    public static async ValueTask MapData(ReadOnlyMemory<byte> data)
    {
        int x;
        int y;
        int i;
        int j;
        int map;
        var buffer = new PacketReader(data);

        GameState.MapData = false;

        for (int n = 0; n <= GetMap(GameState.MyIndex); n++)
        {
            if (Client.Map.Instance.Count <= n)
                Client.Map.Instance.Add(new Map());
        }

        if (buffer.ReadInt32() == 1)
        {
            map = buffer.ReadInt32();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Name = buffer.ReadString();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Music = buffer.ReadString();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Revision = buffer.ReadInt32();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Moral = buffer.ReadByte();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Tileset = buffer.ReadInt32();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Up = buffer.ReadInt32();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Down = buffer.ReadInt32();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Left = buffer.ReadInt32();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Right = buffer.ReadInt32();
            Client.Map.Instance[GetMap(GameState.MyIndex)].BootMap = buffer.ReadInt32();
            Client.Map.Instance[GetMap(GameState.MyIndex)].BootX = buffer.ReadByte();
            Client.Map.Instance[GetMap(GameState.MyIndex)].BootY = buffer.ReadByte();
            Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX = buffer.ReadByte();
            Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY = buffer.ReadByte();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Weather = buffer.ReadByte();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Fog = buffer.ReadInt32();
            Client.Map.Instance[GetMap(GameState.MyIndex)].WeatherIntensity = buffer.ReadInt32();
            Client.Map.Instance[GetMap(GameState.MyIndex)].FogOpacity = buffer.ReadByte();
            Client.Map.Instance[GetMap(GameState.MyIndex)].FogSpeed = buffer.ReadByte();
            Client.Map.Instance[GetMap(GameState.MyIndex)].MapTint = buffer.ReadBoolean();
            Client.Map.Instance[GetMap(GameState.MyIndex)].MapTintR = buffer.ReadByte();
            Client.Map.Instance[GetMap(GameState.MyIndex)].MapTintG = buffer.ReadByte();
            Client.Map.Instance[GetMap(GameState.MyIndex)].MapTintB = buffer.ReadByte();
            Client.Map.Instance[GetMap(GameState.MyIndex)].MapTintA = buffer.ReadByte();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Panorama = buffer.ReadByte();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Parallax = buffer.ReadByte();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Brightness = buffer.ReadByte();
            Client.Map.Instance[GetMap(GameState.MyIndex)].NoRespawn = buffer.ReadBoolean();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Indoors = buffer.ReadBoolean();
            Client.Map.Instance[GetMap(GameState.MyIndex)].Shop = buffer.ReadInt32();

            // Per-map camera zoom bounds
            Client.Map.Instance[GetMap(GameState.MyIndex)].MinZoom = buffer.ReadSingle();
            Client.Map.Instance[GetMap(GameState.MyIndex)].MaxZoom = buffer.ReadSingle();

            // Apply zoom bounds. Only force min zoom during actual map loads.
            var mapZoomMin = Client.Map.Instance[GetMap(GameState.MyIndex)].MinZoom;
            var mapZoomMax = Client.Map.Instance[GetMap(GameState.MyIndex)].MaxZoom;
            if (mapZoomMin <= 0) mapZoomMin = 0.5f;
            if (mapZoomMax <= 0) mapZoomMax = 2.0f;
            if (mapZoomMax < mapZoomMin) mapZoomMax = mapZoomMin;
            if (GameState.GettingMap)
            {
                GameState.CameraZoom = Math.Clamp(mapZoomMin, mapZoomMin, mapZoomMax);
            }
            else
            {
                var currentZoom = GameState.CameraZoom <= 0 ? 1.0f : GameState.CameraZoom;
                GameState.CameraZoom = Math.Clamp(currentZoom, mapZoomMin, mapZoomMax);
            }

            Client.Map.Instance[GetMap(GameState.MyIndex)].Tile = new Core.Globals.Type.Tile[Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX, Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY];
            Data.TileHistory = new Core.Globals.Type.TileHistory[GameState.MaxTileHistory];

            for (i = 0; i < GameState.MaxTileHistory; i++)
            {
                Data.TileHistory[i].Tile = new Core.Globals.Type.Tile[Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX, Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY];
            }

            int layerCount = Enum.GetValues(typeof(MapLayer)).Length;

            // Initialize Layer arrays for MyMap tiles
            for (int xx = 0; xx < Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX; xx++)
            {
                for (int yy = 0; yy < Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY; yy++)
                {
                    Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[xx, yy].Layer = new Core.Globals.Type.Layer[layerCount];

                    for (int l = 0; l < layerCount; l++)
                    {
                        Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[xx, yy].Layer[l] = new Core.Globals.Type.Layer
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

            for (x = 0; x < Core.Globals.Variables.MaxMapNpcs; x++)
                Client.Map.Instance[GetMap(GameState.MyIndex)].Npc[x] = buffer.ReadInt32();

            var count = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX;
            for (x = 0; x < count; x++)
            {
                var count2 = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY;
                for (y = 0; y < count2; y++)
                {
                    Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Data1 = buffer.ReadInt32();
                    Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Data2 = buffer.ReadInt32();
                    Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Data3 = buffer.ReadInt32();
                    Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Data1_2 = buffer.ReadInt32();
                    Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Data2_2 = buffer.ReadInt32();
                    Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Data3_2 = buffer.ReadInt32();
                    Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].DirBlock = buffer.ReadByte();

                    for (j = 0; j < GameState.MaxTileHistory; j++)
                    {
                        Data.TileHistory[j].Tile[x, y].Data1 = Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Data1;
                        Data.TileHistory[j].Tile[x, y].Data2 = Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Data2;
                        Data.TileHistory[j].Tile[x, y].Data3 = Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Data3;
                        Data.TileHistory[j].Tile[x, y].Data1_2 = Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Data1_2;
                        Data.TileHistory[j].Tile[x, y].Data2_2 = Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Data2_2;
                        Data.TileHistory[j].Tile[x, y].Data3_2 = Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Data3_2;
                        Data.TileHistory[j].Tile[x, y].DirBlock = Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].DirBlock;
                        Data.TileHistory[j].Tile[x, y].Type = Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Type;
                        Data.TileHistory[j].Tile[x, y].Type2 = Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Type2;
                    }

                    for (i = 0; i < layerCount; i++)
                    {
                        Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[i].Tileset = buffer.ReadInt32();
                        Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[i].X = buffer.ReadInt32();
                        Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[i].Y = buffer.ReadInt32();
                        Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[i].AutoTile = buffer.ReadByte();

                        for (j = 0; j < GameState.MaxTileHistory; j++)
                        {
                            Data.TileHistory[j].Tile[x, y].Layer[i].Tileset = Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[i].Tileset;
                            Data.TileHistory[j].Tile[x, y].Layer[i].X = Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[i].X;
                            Data.TileHistory[j].Tile[x, y].Layer[i].Y = Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[i].Y;
                            Data.TileHistory[j].Tile[x, y].Layer[i].AutoTile = Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[i].AutoTile;
                        }
                    }

                    Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Type = (TileType)buffer.ReadInt32();
                    Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Type2 = (TileType)buffer.ReadInt32();
                }
            }

            Client.Map.Instance[GetMap(GameState.MyIndex)].EventCount = buffer.ReadInt32();

            if (Client.Map.Instance[GetMap(GameState.MyIndex)].EventCount > 0)
            {
                Client.Map.Instance[GetMap(GameState.MyIndex)].Event = new Core.Globals.Type.Event[Client.Map.Instance[GetMap(GameState.MyIndex)].EventCount];
                var count2 = Client.Map.Instance[GetMap(GameState.MyIndex)].EventCount;
                for (i = 0; i < count2; i++)
                {               
                    ref var instance = ref Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i];
                    instance.Name = buffer.ReadString();
                    instance.Globals = buffer.ReadByte();
                    instance.X = buffer.ReadInt32();
                    instance.Y = buffer.ReadInt32();
                    instance.PageCount = buffer.ReadInt32();
                
                    if (Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].PageCount > 0)
                    {
                        Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages = new Core.Globals.Type.EventPage[Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].PageCount];
                        var count3 = Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].PageCount;
                        for (x = 0; x < count3; x++)
                        {
                            {
                                ref var instance1 = ref Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x];
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
                                    Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].MoveRoute = new Core.Globals.Type.MoveRoute[instance1.MoveRouteCount];
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

                                instance1.IdleAnim = buffer.ReadByte();
                                instance1.DirFix = buffer.ReadByte();
                                instance1.WalkThrough = buffer.ReadInt32();
                                instance1.ShowName = buffer.ReadInt32();
                                instance1.Trigger = buffer.ReadByte();
                                instance1.CommandListCount = buffer.ReadInt32();
                                instance1.Position = buffer.ReadByte();
                            }

                            if (Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandListCount > 0)
                            {
                                Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList = new Core.Globals.Type.CommandList[Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandListCount];
                                var count5 = Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandListCount;
                                for (y = 0; y < count5; y++)
                                {
                                    Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount = buffer.ReadInt32();
                                    Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].ParentList = buffer.ReadInt32();
                                    if (Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount > 0)
                                    {
                                        Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].Commands = new Core.Globals.Type.EventCommand[Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount];
                                        for (int z = 0, count6 = Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount; z < count6; z++)
                                        {
                                            {
                                                ref var instance2 = ref Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].Commands[z];
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

        for (i = 0; i < Core.Globals.Variables.MaxMapItems; i++)
        {
            MapItem.Instance[i].Num = buffer.ReadInt32();
            MapItem.Instance[i].Value = buffer.ReadInt32();
            MapItem.Instance[i].X = buffer.ReadInt32();
            MapItem.Instance[i].Y = buffer.ReadInt32();
            MapItem.Instance[i].Durability = buffer.ReadInt32();
        }

        int vitalCount = Enum.GetValues(typeof(Vital)).Length;

        for (i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
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

        GameState.CurrentWeather = Client.Map.Instance[GetMap(GameState.MyIndex)].Weather;
        GameState.CurrentWeatherIntensity = Client.Map.Instance[GetMap(GameState.MyIndex)].WeatherIntensity;
        GameState.CurrentFog = Client.Map.Instance[GetMap(GameState.MyIndex)].Fog;
        GameState.CurrentFogSpeed = Client.Map.Instance[GetMap(GameState.MyIndex)].FogSpeed;
        GameState.CurrentFogOpacity = Client.Map.Instance[GetMap(GameState.MyIndex)].FogOpacity;
        GameState.CurrentTintR = Client.Map.Instance[GetMap(GameState.MyIndex)].MapTintR;
        GameState.CurrentTintG = Client.Map.Instance[GetMap(GameState.MyIndex)].MapTintG;
        GameState.CurrentTintB = Client.Map.Instance[GetMap(GameState.MyIndex)].MapTintB;
        GameState.CurrentTintA = Client.Map.Instance[GetMap(GameState.MyIndex)].MapTintA;

        GameLogic.UpdateDrawMapName();

        GameState.GettingMap = false;
        GameState.CanMoveNow = true;
    }

    public static async ValueTask MapItemData(ReadOnlyMemory<byte> data)
    {
        int i;
        var buffer = new PacketReader(data);

        i = buffer.ReadByte();
        ref var instance = ref MapItem.Instance[i];
        instance.Num = buffer.ReadInt32();
        instance.Value = buffer.ReadInt32();
        instance.X = buffer.ReadInt32();
        instance.Y = buffer.ReadInt32();
        instance.Durability = buffer.ReadInt32();

    }

    public static async ValueTask MapItemsData(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        for (int i = 0; i < Core.Globals.Variables.MaxMapItems; i++)
        {
            ref var instance = ref MapItem.Instance[i];
            instance.Num = buffer.ReadInt32();
            instance.Value = buffer.ReadInt32();
            instance.X = buffer.ReadInt32();
            instance.Y = buffer.ReadInt32();
            instance.Durability = buffer.ReadInt32();
        }

    }

    public static async ValueTask MapNpcData(ReadOnlyMemory<byte> data)
    {
        int i;
        var buffer = new PacketReader(data);

        for (i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
        {
            ref var instance = ref MapNpc.Instance[i];
            instance.Num = buffer.ReadInt32();
            instance.X = buffer.ReadInt32();
            instance.Y = buffer.ReadInt32();
            instance.Dir = buffer.ReadByte();

            // Server sends remaining ms until respawn (0 if alive)
            var deathTimer = buffer.ReadInt32();
            instance.DeathTimer = deathTimer > 0 ? Client.General.GetTickCount() + deathTimer : 0;
        }
    }

    public static async ValueTask MapNpcUpdate(ReadOnlyMemory<byte> data)
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

    public static async ValueTask EditMap(ReadOnlyMemory<byte> data)
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

    public static async ValueTask SpawnEvent(ReadOnlyMemory<byte> data)
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
            instance.Dir = buffer.ReadByte();
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
            instance.IdleAnim = buffer.ReadByte();
            instance.DirFix = buffer.ReadByte();
            instance.WalkThrough = buffer.ReadInt32();
            instance.ShowName = buffer.ReadInt32();
        }
    }

    public static async ValueTask EventMove(ReadOnlyMemory<byte> data)
    {
        int id;
        int x;
        int y;
        byte dir;
        byte showDir;
        int movementSpeed;
        var buffer = new PacketReader(data);

        id = buffer.ReadInt32();
        // Server sends start-of-step coordinates in tile units; client stores/draws them in world pixels.
        x = buffer.ReadInt32() * Constants.TileSize;
        y = buffer.ReadInt32() * Constants.TileSize;
        dir = buffer.ReadByte();
        showDir = buffer.ReadByte();
        movementSpeed = buffer.ReadInt32();

        if (id > GameState.CurrentEvents)
            return;

        if (Data.MapEvents == null)
            return;
        ref var instance = ref Data.MapEvents[id];
        instance.X = x;
        instance.Y = y;
        instance.Dir = dir;
        instance.Moving = 1;
        instance.ShowDir = showDir;
        instance.MovementSpeed = movementSpeed;

        // Begin a 1-tile (32px) client-side step like NPCs.
        Client.Event.StartStep(id, x, y, (byte)dir);
    
    }

    public static async ValueTask EventDir(ReadOnlyMemory<byte> data)
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

            // Ensure we finish at the exact destination for the last step.
            Client.Event.SnapToDest(i);
        }
    }

    public static async ValueTask SwitchesAndVariables(ReadOnlyMemory<byte> data)
    {
        int i;
        var buffer = new PacketReader(data);

        for (i = 0; i < Core.Globals.Variables.MaxSwitches; i++)
            Event.Switches[i] = buffer.ReadString();

        for (i = 0; i < Core.Globals.Variables.MaxVariables; i++)
            Event.Variables[i] = buffer.ReadString();
    }

    public static async ValueTask MapEventData(ReadOnlyMemory<byte> data)
    {
        int i;
        int x;
        int y;
        int z;
        int w;
        var buffer = new PacketReader(data);

        for (int n = 0; n <= GetMap(GameState.MyIndex); n++)
        {
            if (Client.Map.Instance.Count <= n)
                Client.Map.Instance.Add(new Map());
        }

        Client.Map.Instance[GetMap(GameState.MyIndex)].EventCount = buffer.ReadInt32();

        if (Client.Map.Instance[GetMap(GameState.MyIndex)].EventCount > 0)
        {
            Client.Map.Instance[GetMap(GameState.MyIndex)].Event = new Core.Globals.Type.Event[Client.Map.Instance[GetMap(GameState.MyIndex)].EventCount];
            var count = Client.Map.Instance[GetMap(GameState.MyIndex)].EventCount;
            for (i = 0; i < count; i++)
            {                
                ref var instance = ref Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i];
                instance.Name = buffer.ReadString();
                instance.Globals = buffer.ReadByte();
                instance.X = buffer.ReadInt32();
                instance.Y = buffer.ReadInt32();
                instance.PageCount = buffer.ReadInt32();

                if (Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].PageCount > 0)
                {
                    Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages = new Core.Globals.Type.EventPage[Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].PageCount];
                    var count2 = Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].PageCount;
                    for (x = 0; x < count2; x++)
                    {
                        ref var instance1 = ref Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x];
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
                            Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].MoveRoute = new Core.Globals.Type.MoveRoute[instance1.MoveRouteCount];
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

                        instance1.IdleAnim = buffer.ReadByte();
                        instance1.DirFix = buffer.ReadByte();
                        instance1.WalkThrough = buffer.ReadInt32();
                        instance1.ShowName = buffer.ReadInt32();
                        instance1.Trigger = buffer.ReadByte();
                        instance1.CommandListCount = buffer.ReadInt32();
                        instance1.Position = buffer.ReadByte();

                        if (Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandListCount > 0)
                        {
                            Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList = new Core.Globals.Type.CommandList[Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandListCount];
                            var count4 = Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandListCount;
                            for (y = 0; y < count4; y++)
                            {
                                Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount = buffer.ReadInt32();
                                Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].ParentList = buffer.ReadInt32();
                                if (Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount > 0)
                                {
                                    Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].Commands = new Core.Globals.Type.EventCommand[Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount];
                                    var count5 = Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount;
                                    for (z = 0; z < count5; z++)
                                    {
                                        {
                                            ref var instance2 = ref Client.Map.Instance[GetMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].Commands[z];
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

    public static async ValueTask EventChat(ReadOnlyMemory<byte> data)
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

        if (choices == 0)
        {
            Event.EventChatType = 0;
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

        WinEventChat.Show();
    }

    public static async ValueTask EventStart(ReadOnlyMemory<byte> data)
    {
        Event.InEvent = true;
    }

    public static async ValueTask EventEnd(ReadOnlyMemory<byte> data)
    {
        Event.InEvent = false;

        WinEventChat.OnEventEnded();
    }

    public static async ValueTask Picture(ReadOnlyMemory<byte> data)
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

    public static async ValueTask HidePicture(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);

        Event.Picture = default;
    }

    public static async ValueTask HoldPlayer(ReadOnlyMemory<byte> data)
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

    public static async ValueTask PlayBGM(ReadOnlyMemory<byte> data)
    {
        string music;
        var buffer = new PacketReader(data);

        music = buffer.ReadString();
        Client.Map.Instance[GetMap(GameState.MyIndex)].Music = music;
    }

    public static async ValueTask FadeOutBGM(ReadOnlyMemory<byte> data)
    {
        Audio.CurrentMusic = "";
        Audio.FadeOutSwitch = true;
    }

    public static async ValueTask PlaySound(ReadOnlyMemory<byte> data)
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

    public static async ValueTask StopSound(ReadOnlyMemory<byte> data)
    {
        Audio.StopSound();
    }

    public static async ValueTask SpecialEffect(ReadOnlyMemory<byte> data)
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
                    Client.Map.Instance[GetMap(GameState.MyIndex)].MapTint = true;
                    GameState.CurrentTintR = buffer.ReadInt32();
                    GameState.CurrentTintG = buffer.ReadInt32();
                    GameState.CurrentTintB = buffer.ReadInt32();
                    GameState.CurrentTintA = buffer.ReadInt32();
                    break;
                }
        }
    }

    public static async ValueTask UpdateProjectile(ReadOnlyMemory<byte> data)
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
        projectile.Range = buffer.ReadByte();
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

    public static async ValueTask MapProjectile(ReadOnlyMemory<byte> data)
    {
        var buffer = new PacketReader(data);
        int i = buffer.ReadInt32();

        {
            ref var instance = ref Data.MapProjectile[Player.Instance[GameState.MyIndex].Map, i];
            instance.Index = buffer.ReadInt32();
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

            // If server is clearing the slot, don't keep it alive client-side.
            if (instance.Index < 0)
            {
                instance.Timer = 0;
            }
            else
            {
                instance.Timer = General.GetTickCount() + 60000;
            }
        }
    }

    public static async ValueTask PartyInvite(ReadOnlyMemory<byte> data)
    {
        string name;
        var buffer = new PacketReader(data);

        name = buffer.ReadString();
        GameLogic.Dialogue("Party Invite", name + " has invited you to a party.", "Would you like to join?", DialogueType.PartyInvite, DialogueStyle.YesNo);
    }

    public static async ValueTask PartyUpdate(ReadOnlyMemory<byte> data)
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
        for (i = 0; i < Core.Globals.Variables.MaxPartyMembers; i++)
            Data.MyParty.Member[i] = buffer.ReadInt32();
        Data.MyParty.MemberCount = buffer.ReadInt32();

        WinParty.Update();
    }

    public static async ValueTask PartyVitals(ReadOnlyMemory<byte> data)
    {
        int playerNum;
        var partyindex = -1;
        var buffer = new PacketReader(data);

        // which player?
        playerNum = buffer.ReadInt32();

        // find the party number
        for (int i = 0; i < Core.Globals.Variables.MaxPartyMembers; i++)
        {
            if (Data.MyParty.Member[i] == playerNum)
            {
                partyindex = i;
            }
        }

        // exit out if wrong data
        if (partyindex < 0 | partyindex >= Core.Globals.Variables.MaxPartyMembers)
            return;

        // set vitals
        var vitalCount = Enum.GetNames(typeof(Vital)).Length;
        for (int i = 0; i < vitalCount; i++)
            Player.Instance[playerNum].Vital[i] = buffer.ReadInt32();

        GameLogic.UpdatePartyBars();
    }


    public static async ValueTask OpenBank(ReadOnlyMemory<byte> data)
    {
        int i;
        var buffer = new PacketReader(data);

        Bank.OnClear();
        for (i = 0; i <= GameState.MyIndex; i++)
        {
            Bank.Instance.Add(new Bank());
        }

        for (i = 0; i < Core.Globals.Variables.MaxBank; i++)
        {
            SetBank(GameState.MyIndex, (byte)i, buffer.ReadInt32());
            SetBankValue(GameState.MyIndex, (byte)i, buffer.ReadInt32());
            SetBankDurability(GameState.MyIndex, (byte)i, buffer.ReadInt32());
        }

        GameState.InBank = true;

        if (!(WindowManager.Windows[WindowManager.GetWindow("winBank")].Visible == true))
        {
            WindowManager.ShowWindow("winBank", resetPosition: false);
        }
    }


    public static async ValueTask EditScript(ReadOnlyMemory<byte> data)
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