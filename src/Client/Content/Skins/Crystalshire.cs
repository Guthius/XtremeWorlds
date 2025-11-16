#nullable disable

using System;
using Client;
using Client.Game.UI;
using Client.Game.UI.Controls;
using Client.Game.UI.Windows;
using Client.Net;
using Core.Configurations;
using Core.Globals;
using static Core.Globals.Command;

public class Crystalshire
{
    public void UpdateWindow_Login()
    {
        var window = WindowLoader.FromLayout("winLogin");

        var userName = SettingsManager.Instance.SaveUsername ? SettingsManager.Instance.Username : string.Empty;

        window.GetChild("btnClose").CallBack[(int) ControlState.MouseDown] = Client.General.DestroyGame;
        window.GetChild("txtUsername").Text = userName;
        window.GetChild("chkSaveUsername").Value = SettingsManager.Instance.SaveUsername ? 1 : 0;
        window.GetChild("btnAccept").CallBack[(int) ControlState.MouseDown] = WinLogin.OnLogin;
        window.GetChild("btnExit").CallBack[(int) ControlState.MouseDown] = WinLogin.OnExit;
        window.GetChild("btnRegister").CallBack[(int) ControlState.MouseDown] = WinLogin.OnRegister;

        WindowManager.SetActiveControl(window, userName.Length == 0 ? "txtUsername" : "txtPassword");
    }

    public void UpdateWindow_Register()
    {
        var window = WindowLoader.FromLayout("winRegister");

        window.GetChild("btnClose").CallBack[(int) ControlState.MouseDown] = WinRegister.OnClose;
        window.GetChild("btnAccept").CallBack[(int) ControlState.MouseDown] = WinRegister.OnRegister;
        window.GetChild("btnExit").CallBack[(int) ControlState.MouseDown] = WinRegister.OnClose;

        WindowManager.SetActiveControl(window, "txtUsername");
    }

    public void UpdateWindow_NewChar()
    {
        var window = WindowLoader.FromLayout("winNewChar");

        window.GetChild("btnClose").CallBack[(int) ControlState.MouseDown] = WinNewChar.OnCancel;
        window.GetChild("btnAccept").CallBack[(int) ControlState.MouseDown] = WinNewChar.OnAccept;
        window.GetChild("btnCancel").CallBack[(int) ControlState.MouseDown] = WinNewChar.OnCancel;
        window.GetChild("picScene").OnDraw = WinNewChar.OnDrawSprite;
        window.GetChild("btnLeft").CallBack[(int) ControlState.MouseDown] = WinNewChar.OnLeftClick;
        window.GetChild("btnRight").CallBack[(int) ControlState.MouseDown] = WinNewChar.OnRightClick;
        window.GetChild("chkMale").CallBack[(int) ControlState.MouseDown] = WinNewChar.OnMaleChecked;
        window.GetChild("chkFemale").CallBack[(int) ControlState.MouseDown] = WinNewChar.OnFemaleChecked;

        WindowManager.SetActiveControl(window, "txtName");
    }

    public void UpdateWindow_Chars()
    {
        var window = WindowLoader.FromLayout("winChars");

        window.GetChild("btnClose").CallBack[(int) ControlState.MouseDown] = WinChars.OnClose;
        window.GetChild("picScene_3").OnDraw = WinChars.OnDraw;
        window.GetChild("btnSelectChar_1").CallBack[(int) ControlState.MouseDown] = WinChars.OnSelectCharacter1Click;
        window.GetChild("btnCreateChar_1").CallBack[(int) ControlState.MouseDown] = WinChars.OnCreateCharacter1Click;
        window.GetChild("btnDelChar_1").CallBack[(int) ControlState.MouseDown] = WinChars.OnDeleteCharacter1Click;
        window.GetChild("btnSelectChar_2").CallBack[(int) ControlState.MouseDown] = WinChars.OnSelectCharacter2Click;
        window.GetChild("btnCreateChar_2").CallBack[(int) ControlState.MouseDown] = WinChars.OnCreateCharacter2Click;
        window.GetChild("btnDelChar_2").CallBack[(int) ControlState.MouseDown] = WinChars.OnDeleteCharacter2Click;
        window.GetChild("btnSelectChar_3").CallBack[(int) ControlState.MouseDown] = WinChars.OnSelectCharacter3Click;
        window.GetChild("btnCreateChar_3").CallBack[(int) ControlState.MouseDown] = WinChars.OnCreateCharacter3Click;
        window.GetChild("btnDelChar_3").CallBack[(int) ControlState.MouseDown] = WinChars.OnDeleteCharacter3Click;
    }

    public void UpdateWindow_Jobs()
    {
        var window = WindowLoader.FromLayout("winJobs");

        window.GetChild("btnClose").CallBack[(int) ControlState.MouseDown] = WinJobs.OnClose;
        window.GetChild("picParchment").OnDraw = WinJobs.OnDrawSprite;
        window.GetChild("btnLeft").CallBack[(int) ControlState.MouseDown] = WinJobs.OnLeftClick;
        window.GetChild("btnRight").CallBack[(int) ControlState.MouseDown] = WinJobs.OnRightClick;
        window.GetChild("btnAccept").CallBack[(int) ControlState.MouseDown] = WinJobs.OnAccept;
        window.GetChild("picOverlay").CallBack[(int) ControlState.MouseDown] = WinJobs.OnClose;
        window.GetChild("picOverlay").OnDraw = WinJobs.OnDrawDescription;
    }

    public void UpdateWindow_Dialogue()
    {
        var window = WindowLoader.FromLayout("winDialogue");

        window.GetChild("btnClose").CallBack[(int) ControlState.MouseDown] = WinDialogue.OnClose;
        window.GetChild("btnYes").CallBack[(int) ControlState.MouseDown] = WinDialogue.OnYes;
        window.GetChild("btnNo").CallBack[(int) ControlState.MouseDown] = WinDialogue.OnNo;
        window.GetChild("btnOkay").CallBack[(int) ControlState.MouseDown] = WinDialogue.OnOkay;
        WindowManager.SetActiveControl(window, "txtInput");
    }

    public void UpdateWindow_Party()
    {
        var window = WindowLoader.FromLayout("winParty");
    }

    public void UpdateWindow_Trade()
    {
        var window = WindowLoader.FromLayout("winTrade");
        window.OnDraw = WinTrade.OnDraw;
        window.GetChild("btnClose").CallBack[(int) ControlState.MouseDown] = WinTrade.OnClose;
        window.GetChild("btnAccept").CallBack[(int) ControlState.MouseDown] = WinTrade.OnAccept;
        window.GetChild("btnDecline").CallBack[(int) ControlState.MouseDown] = WinTrade.OnClose;

        // Wire interactive picture boxes for trade regions
        window.GetChild("picYour").CallBack[(int) ControlState.MouseDown] = WinTrade.OnYourTradeMouseMove;
        window.GetChild("picYour").CallBack[(int) ControlState.MouseMove] = WinTrade.OnYourTradeMouseMove;
        window.GetChild("picYour").CallBack[(int) ControlState.DoubleClick] = WinTrade.OnYourTradeClick;

        window.GetChild("picTheir").CallBack[(int) ControlState.MouseDown] = WinTrade.OnTheirTradeMouseMove;
        window.GetChild("picTheir").CallBack[(int) ControlState.MouseMove] = WinTrade.OnTheirTradeMouseMove;
        window.GetChild("picTheir").CallBack[(int) ControlState.DoubleClick] = WinTrade.OnTheirTradeMouseMove;
    }

    public void UpdateWindow_EditorMap()
    {
        var window = WindowLoader.FromLayout("winEditorMap");

        // Close button
        if (WindowManager.TryGetControl("winEditorMap", "btnClose", out var btnClose))
        {
            btnClose.CallBack[(int)ControlState.MouseDown] = () => WindowManager.HideWindow("winEditorMap");
        }

        // Footer Close
        if (WindowManager.TryGetControl("winEditorMap", "btnCloseMap", out var btnCloseMap))
        {
            btnCloseMap.CallBack[(int)ControlState.MouseDown] = () => WindowManager.HideWindow("winEditorMap");
        }

        // Save - reuse existing map save pipeline
        if (WindowManager.TryGetControl("winEditorMap", "btnSaveMap", out var btnSave))
        {
            btnSave.CallBack[(int)ControlState.MouseDown] = () => { Map.SendMap(); };
        }

        // Discard: cancel map edit and close
        if (WindowManager.TryGetControl("winEditorMap", "btnDiscard", out var btnDiscard))
        {
            btnDiscard.CallBack[(int)ControlState.MouseDown] = () => { EditorMap.MapEditorCancel(); WindowManager.HideWindow("winEditorMap"); };
        }

        // Layer buttons: update current layer in GameState
        void BindLayer(string ctrl, int layer)
        {
            if (WindowManager.TryGetControl("winEditorMap", ctrl, out var c))
            {
                c.CallBack[(int)ControlState.MouseDown] = () => { GameState.CurLayer = (byte)layer; };
            }
        }
        BindLayer("btnLayer0", 0);
        BindLayer("btnLayer1", 1);
        BindLayer("btnLayer2", 2);
        BindLayer("btnLayer3", 3);
        BindLayer("btnLayer4", 4);
        BindLayer("btnLayer5", 5);

        // Tools: map to existing editor actions
        if (WindowManager.TryGetControl("winEditorMap", "btnToolPencil", out var btnPencil))
            btnPencil.CallBack[(int)ControlState.MouseDown] = () => { GameState.EyeDropper = false; };
        if (WindowManager.TryGetControl("winEditorMap", "btnToolFill", out var btnFill))
            btnFill.CallBack[(int)ControlState.MouseDown] = () => { EditorMap.MapEditorFillLayer((MapLayer)GameState.CurLayer, (byte)GameState.CurAutotileType, (byte)GameState.EditorTileX, (byte)GameState.EditorTileY); };
        if (WindowManager.TryGetControl("winEditorMap", "btnToolEraser", out var btnErase))
            btnErase.CallBack[(int)ControlState.MouseDown] = () => { EditorMap.MapEditorClearLayer((MapLayer)GameState.CurLayer); };

        // Toolbar buttons
        if (WindowManager.TryGetControl("winEditorMap", "btnGrid", out var btnGrid))
            btnGrid.CallBack[(int)ControlState.MouseDown] = () => { GameState.MapGrid = !GameState.MapGrid; };
        if (WindowManager.TryGetControl("winEditorMap", "btnEyeDropper", out var btnEye))
            btnEye.CallBack[(int)ControlState.MouseDown] = () => { GameState.EyeDropper = !GameState.EyeDropper; };
        if (WindowManager.TryGetControl("winEditorMap", "btnUndo", out var btnUndo))
            btnUndo.CallBack[(int)ControlState.MouseDown] = () => { EditorMap.Undo(); };
        if (WindowManager.TryGetControl("winEditorMap", "btnRedo", out var btnRedo))
            btnRedo.CallBack[(int)ControlState.MouseDown] = () => { EditorMap.Redo(); };

        // Quick actions: call into existing helpers if available
        if (WindowManager.TryGetControl("winEditorMap", "btnFillLayer", out var btnFillLayer))
            btnFillLayer.CallBack[(int)ControlState.MouseDown] = () => { EditorMap.MapEditorFillLayer((MapLayer)GameState.CurLayer, (byte)GameState.CurAutotileType, (byte)GameState.EditorTileX, (byte)GameState.EditorTileY); };
        if (WindowManager.TryGetControl("winEditorMap", "btnClearLayer", out var btnClearLayer))
            btnClearLayer.CallBack[(int)ControlState.MouseDown] = () => { EditorMap.MapEditorClearLayer((MapLayer)GameState.CurLayer); };
        if (WindowManager.TryGetControl("winEditorMap", "btnCopyMap", out var btnCopy))
            btnCopy.CallBack[(int)ControlState.MouseDown] = () => { EditorMap.MapEditorCopyMap(); };
        if (WindowManager.TryGetControl("winEditorMap", "btnPasteMap", out var btnPaste))
            btnPaste.CallBack[(int)ControlState.MouseDown] = () => { TextRenderer.AddText("Paste not available", (int)ColorName.BrightRed); };

        // Tileset selector wiring
        string[] autotileNames = new[]{"None","Autotile","Fake Autotile","Animated","Cliff","Waterfall"};

        // Populate Layer and Autotile combos and set defaults
        if (WindowManager.TryGetControl("winEditorMap", "cmbLayer", out var cmbLayerCtrl) && cmbLayerCtrl is ComboBox cmbLayer)
        {
            cmbLayer.Items.Clear();
            foreach (var name in Enum.GetNames(typeof(MapLayer)))
            {
                // Insert spaces before capital letters (except the first letter)
                string displayName = System.Text.RegularExpressions.Regex.Replace(name, "(?<!^)([A-Z])", " $1");
                cmbLayer.Items.Add(displayName);
            }
            
            cmbLayer.Value = Math.Clamp(GameState.CurLayer, 0, cmbLayer.Items.Count - 1);
            // Update GameState when selection changes
            cmbLayer.CallBack[(int)ControlState.MouseMove] = () => { GameState.CurLayer = (byte)Math.Clamp(cmbLayer.Value, 0, 5); };
        }
        
        if (WindowManager.TryGetControl("winEditorMap", "cmbAutotile", out var cmbAutoCtrl) && cmbAutoCtrl is ComboBox cmbAuto)
        {
            cmbAuto.Items.Clear();
            foreach (var n in autotileNames) cmbAuto.Items.Add(n);
            cmbAuto.Value = Math.Clamp(GameState.CurAutotileType, 0, autotileNames.Length - 1);
            cmbAuto.CallBack[(int)ControlState.MouseMove] = () => { GameState.CurAutotileType = (cmbAuto.Value >= 0 && cmbAuto.Value < autotileNames.Length) ? cmbAuto.Value : 0; };
        }

        // Attributes: mode combo (maps to GameState Opt* flags) and actions
        string[] attrModes = new[] { "Blocked", "Warp", "Item", "Npc Avoid", "Resource", "Npc Spawn", "Shop", "Bank", "Heal", "Trap", "Animation", "No Crossing", "Info" };

        // Helpers to set/clear current attribute flags
        void ClearAttrFlags()
        {
            GameState.OptBlocked = false;
            GameState.OptWarp = false;
            GameState.OptItem = false;
            GameState.OptNpcAvoid = false;
            GameState.OptResource = false;
            GameState.OptNpcSpawn = false;
            GameState.OptShop = false;
            GameState.OptBank = false;
            GameState.OptHeal = false;
            GameState.OptTrap = false;
            GameState.OptAnimation = false;
            GameState.OptNoCrossing = false;
            GameState.OptInfo = false;
        }

        void SetAttrFlags(int index)
        {
            ClearAttrFlags();
            GameState.OptBlocked = index == 0;
            GameState.OptWarp = index == 1;
            GameState.OptItem = index == 2;
            GameState.OptNpcAvoid = index == 3;
            GameState.OptResource = index == 4;
            GameState.OptNpcSpawn = index == 5;
            GameState.OptShop = index == 6;
            GameState.OptBank = index == 7;
            GameState.OptHeal = index == 8;
            GameState.OptTrap = index == 9;
            GameState.OptAnimation = index == 10;
            GameState.OptNoCrossing = index == 11;
            GameState.OptInfo = index == 12;
        }

        if (WindowManager.TryGetControl("winEditorMap", "cmbAttrMode", out var cmbAttrCtrl) && cmbAttrCtrl is ComboBox cmbAttr)
        {
            cmbAttr.Items.Clear();
            foreach (var n in attrModes) cmbAttr.Items.Add(n);
        }

        // Apply sets the current attribute to the selected mode; Cancel clears all attribute modes
        if (WindowManager.TryGetControl("winEditorMap", "btnAttrApply", out var btnAttrApply))
            btnAttrApply.CallBack[(int)ControlState.MouseDown] = () =>
            {
                if (WindowManager.TryGetControl("winEditorMap", "cmbAttrMode", out var attrCtrl) && attrCtrl is ComboBox c)
                {
                    var idx = Math.Clamp(c.Value, 0, attrModes.Length - 1);
                    SetAttrFlags(idx);
                }
            };
        if (WindowManager.TryGetControl("winEditorMap", "btnAttrClear", out var btnAttrClear))
            btnAttrClear.CallBack[(int)ControlState.MouseDown] = () => { ClearAttrFlags(); };

        void UpdateTilesetLabel()
        {
            if (WindowManager.TryGetControl("winEditorMap", "lblTileset", out var c))
                c.Text = GameState.CurTileset.ToString();
        }
        void UpdateAutotileLabel() { }
        // Initialize labels on open
        if (GameState.CurTileset <= 0) GameState.CurTileset = Math.Max(1, Data.MyMap.Tileset);
        if (Data.MyMap.Tileset <= 0) Data.MyMap.Tileset = GameState.CurTileset;
        UpdateTilesetLabel();
        UpdateAutotileLabel();

        // Horizontal tileset scrollbar selects the tileset number
        if (WindowManager.TryGetControl("winEditorMap", "sldTileset", out var sldTilesetCtrl) && sldTilesetCtrl is Client.Game.UI.Controls.ScrollBar sldTileset)
        {
            sldTileset.Min = 1;
            sldTileset.Max = Math.Max(1, GameState.NumTileSets);
            sldTilesetCtrl.Value = Math.Clamp(GameState.CurTileset, sldTileset.Min, sldTileset.Max);
            sldTileset.CallBack[(int)ControlState.MouseMove] = () =>
            {
                GameState.CurTileset = Math.Clamp(sldTilesetCtrl.Value, sldTileset.Min, sldTileset.Max);
                Data.MyMap.Tileset = GameState.CurTileset;
                UpdateTilesetLabel();
            };
        }

        // Horizontal viewport scroll (for wide tilesets)
        if (WindowManager.TryGetControl("winEditorMap", "sldTilesetH", out var sldTilesetHCtrl) && sldTilesetHCtrl is Client.Game.UI.Controls.ScrollBar sldTilesetH)
        {
            // Range is updated during drawing; still add a callback to trigger redraw behavior on change
            sldTilesetH.CallBack[(int)ControlState.MouseMove] = () => { /* no-op: OnDraw reads Value */ };
        }

        if (WindowManager.TryGetControl("winEditorMap", "btnTilesetPrev", out var btnTsPrev))
            btnTsPrev.CallBack[(int)ControlState.MouseDown] = () => { GameState.CurTileset = Math.Max(1, GameState.CurTileset - 1); Data.MyMap.Tileset = GameState.CurTileset; UpdateTilesetLabel(); };
        if (WindowManager.TryGetControl("winEditorMap", "btnTilesetNext", out var btnTsNext))
            btnTsNext.CallBack[(int)ControlState.MouseDown] = () => { var maxTs = Math.Max(1, GameState.NumTileSets); GameState.CurTileset = Math.Min(maxTs, GameState.CurTileset + 1); Data.MyMap.Tileset = GameState.CurTileset; UpdateTilesetLabel(); };

        if (WindowManager.TryGetControl("winEditorMap", "btnAutoPrev", out var btnAutoPrev))
            btnAutoPrev.CallBack[(int)ControlState.MouseDown] = () => { GameState.CurAutotileType = (GameState.CurAutotileType + autotileNames.Length - 1) % autotileNames.Length; UpdateAutotileLabel(); };
        if (WindowManager.TryGetControl("winEditorMap", "btnAutoNext", out var btnAutoNext))
            btnAutoNext.CallBack[(int)ControlState.MouseDown] = () => { GameState.CurAutotileType = (GameState.CurAutotileType + 1) % autotileNames.Length; UpdateAutotileLabel(); };

        if (WindowManager.TryGetControl("winEditorMap", "btnTileApply", out var btnTileApply))
            btnTileApply.CallBack[(int)ControlState.MouseDown] = () =>
            {
                if (WindowManager.TryGetControl("winEditorMap", "txtTileX", out var tbx) && WindowManager.TryGetControl("winEditorMap", "txtTileY", out var tby))
                {
                    int x = int.TryParse(tbx.Text?.Trim(), out var ix) ? ix : 0;
                    int y = int.TryParse(tby.Text?.Trim(), out var iy) ? iy : 0;
                    x = Math.Max(0, x); y = Math.Max(0, y);
                    GameState.EditorTileX = x; GameState.EditorTileY = y;
                    EditorMap.MapEditorChooseTile(1, x * GameState.SizeX, y * GameState.SizeY);
                }
            };

        // Faux header tabs like Admin
        var winIndex = WindowManager.GetWindowIndex("winEditorMap");
        void MakeTabButton(string name, string text, int x, string tabKey)
        {
            WindowManager.CreateButton(
                windowIndex: winIndex,
                name: name,
                left: x,
                top: 70,
                width: 100,
                height: 22,
                text: text,
                font: Font.Arial,
                designNorm: Design.Red,
                designHover: Design.RedHover,
                designMousedown: Design.RedClick,
                callbackMousedown: () => ShowTab(tabKey)
            );
        }

        void SetVisible(bool visible, params string[] names)
        {
            foreach (var n in names)
            {
                if (WindowManager.TryGetControl("winEditorMap", n, out var c)) c.Visible = visible;
            }
        }

        void ShowTab(string tab)
        {
            var tools = new[]{
                "picTilesBG",
                "lblTilesetCaption","sldTileset","lblTileset",
                "lblLayerCaption","cmbLayer",
                "lblAutotileCaption","cmbAutotile",
                "picTileset","sldTilesetV","sldTilesetH"
            };
            var attrs = new[]{"picAttrBG","lblAttr","lblAttrMode","cmbAttrMode","btnAttrApply","btnAttrClear"};
            var npcs = new[]{"picNpcsBG","lblNpcs","lblNpcsHint"};
            var settings = new[]{"picSettingsBG","lblSettings","lblSettingsHint"};
            var dirblock = new[]{"picDirBG","lblDir","btnDirUp","btnDirDown","btnDirLeft","btnDirRight","btnDirClear"};
            var eventsTab = new[]{"picEventsBG","lblEvents","lblEventsHint"};
            var effects = new[]{"picEffectsBG","lblEffects","lblEffectsHint"};

            SetVisible(false, tools);
            SetVisible(false, attrs);
            SetVisible(false, npcs);
            SetVisible(false, settings);
            SetVisible(false, dirblock);
            SetVisible(false, eventsTab);
            SetVisible(false, effects);
            switch (tab)
            {
                case "Tools":
                    SetVisible(true, tools);
                    GameState.MapEditorTab = (int)MapEditorTab.Tiles;
                    break;
                case "Attributes":
                    SetVisible(true, attrs);
                    GameState.MapEditorTab = (int)MapEditorTab.Attributes;
                    break;
                case "Npcs":
                    SetVisible(true, npcs);
                    GameState.MapEditorTab = (int)MapEditorTab.Npcs;
                    break;
                case "Settings":
                    SetVisible(true, settings);
                    GameState.MapEditorTab = (int)MapEditorTab.Settings;
                    break;
                case "DirBlock":
                    SetVisible(true, dirblock);
                    GameState.MapEditorTab = (int)MapEditorTab.Directions;
                    break;
                case "Events":
                    SetVisible(true, eventsTab);
                    GameState.MapEditorTab = (int)MapEditorTab.Events;
                    break;
                case "Effects":
                    SetVisible(true, effects);
                    GameState.MapEditorTab = (int)MapEditorTab.Effects;
                    break;
            }
        }

        MakeTabButton("btnTabTools","Tools",10,"Tools");
        MakeTabButton("btnTabAttrs","Attributes",120,"Attributes");
        MakeTabButton("btnTabNpcs","Npcs",230,"Npcs");
        MakeTabButton("btnTabSettings","Settings",340,"Settings");
        MakeTabButton("btnTabDirBlock","Dir Block",450,"DirBlock");
        MakeTabButton("btnTabEvents","Events",560,"Events");
        MakeTabButton("btnTabEffects","Effects",670,"Effects");
        ShowTab("Tools");

        // Wire tileset preview draw
        if (WindowManager.TryGetControl("winEditorMap", "picTileset", out var picTileset))
        {
            picTileset.OnDraw = WinEditorMap.OnDrawTileset;
        }

        // Simple helpers for Dir Block tab: instruct how to toggle on map
        void DirHint() => TextRenderer.AddText("Click map arrows to toggle blocked directions.", (int)ColorName.Yellow);
        if (WindowManager.TryGetControl("winEditorMap", "btnDirUp", out var btnDirUp)) btnDirUp.CallBack[(int)ControlState.MouseDown] = DirHint;
        if (WindowManager.TryGetControl("winEditorMap", "btnDirDown", out var btnDirDown)) btnDirDown.CallBack[(int)ControlState.MouseDown] = DirHint;
        if (WindowManager.TryGetControl("winEditorMap", "btnDirLeft", out var btnDirLeft)) btnDirLeft.CallBack[(int)ControlState.MouseDown] = DirHint;
        if (WindowManager.TryGetControl("winEditorMap", "btnDirRight", out var btnDirRight)) btnDirRight.CallBack[(int)ControlState.MouseDown] = DirHint;
        if (WindowManager.TryGetControl("winEditorMap", "btnDirClear", out var btnDirClear)) btnDirClear.CallBack[(int)ControlState.MouseDown] = DirHint;
    }

    public void UpdateWindow_EscMenu()
    {
        var window = WindowLoader.FromLayout("winEscMenu");
        window.GetChild("btnReturn").CallBack[(int) ControlState.MouseDown] = WinEscMenu.OnClose;
        window.GetChild("btnOptions").CallBack[(int) ControlState.MouseDown] = WinEscMenu.OnOptionsClick;
        window.GetChild("btnMainMenu").CallBack[(int) ControlState.MouseDown] = WinEscMenu.OnMainMenuClick;
        window.GetChild("btnExit").CallBack[(int) ControlState.MouseDown] = WinEscMenu.OnExitClick;
    }

    public void UpdateWindow_Bars()
    {
        var window = WindowLoader.FromLayout("winBars");
        window.GetChild("picOverlay").OnDraw = WinBars.OnDraw;
    }

    public void UpdateWindow_Chat()
    {
        var window = WindowLoader.FromLayout("winChat");

        // Buttons
        window.GetChild("btnChat").CallBack[(int)ControlState.Normal] = WinChat.OnSayClick;
        window.GetChild("btnUp").CallBack[(int)ControlState.MouseDown] = WinChat.OnUpButtonMouseDown;
        window.GetChild("btnDown").CallBack[(int)ControlState.MouseDown] = WinChat.OnDownButtonMouseDown;
        window.GetChild("btnUp").CallBack[(int)ControlState.MouseUp] = WinChat.OnUpButtonMouseUp;
        window.GetChild("btnDown").CallBack[(int)ControlState.MouseUp] = WinChat.OnDownButtonMouseUp;

        // Checkboxes
        window.GetChild("chkGame").CallBack[(int)ControlState.MouseDown] = WinChat.OnGameChannelClicked;
        window.GetChild("chkMap").CallBack[(int)ControlState.MouseDown] = WinChat.OnMapChannelClicked;
        window.GetChild("chkGlobal").CallBack[(int)ControlState.MouseDown] = WinChat.OnBroadcastChannelClicked;
        window.GetChild("chkParty").CallBack[(int)ControlState.MouseDown] = WinChat.OnPartyChannelClicked;
        window.GetChild("chkGuild").CallBack[(int)ControlState.MouseDown] = WinChat.OnGuildChannelClicked;
        window.GetChild("chkPlayer").CallBack[(int)ControlState.MouseDown] = WinChat.OnPrivateChannelClicked;

        WindowManager.SetActiveControl(window, "txtChat");

        // Initialize checkbox states
        window.GetChild("chkGame").Value = SettingsManager.Instance.ChannelState[(int)ChatChannel.Game];
        window.GetChild("chkMap").Value = SettingsManager.Instance.ChannelState[(int)ChatChannel.Map];
        window.GetChild("chkGlobal").Value = SettingsManager.Instance.ChannelState[(int)ChatChannel.Broadcast];
        window.GetChild("chkParty").Value = SettingsManager.Instance.ChannelState[(int)ChatChannel.Party];
        window.GetChild("chkGuild").Value = SettingsManager.Instance.ChannelState[(int)ChatChannel.Guild];
        window.GetChild("chkPlayer").Value = SettingsManager.Instance.ChannelState[(int)ChatChannel.Private];
        window.GetChild("picNull").OnDraw = WinChat.OnDraw;
    }

    public void UpdateWindow_ChatSmall()
    {
        var window = WindowLoader.FromLayout("winChatSmall");
        window.OnDraw = WinChat.OnDrawSmall;
    }

    public void UpdateWindow_Hotbar()
    {
        var window = WindowLoader.FromLayout("winHotbar");
        window.OnDraw = WinHotBar.OnDraw;
        window.CallBack[(int) ControlState.MouseMove] = WinHotBar.OnMouseMove;
        window.CallBack[(int) ControlState.MouseDown] = WinHotBar.OnMouseDown;
        window.CallBack[(int) ControlState.DoubleClick] = WinHotBar.OnDoubleClick;
    }

    public void UpdateWindow_Menu()
    {
        var window = WindowLoader.FromLayout("winMenu");
        window.GetChild("btnChar").CallBack[(int) ControlState.MouseDown] = WinMenu.OnCharacterClick;
        window.GetChild("btnInv").CallBack[(int) ControlState.MouseDown] = WinMenu.OnInventoryClick;
        window.GetChild("btnSkills").CallBack[(int) ControlState.MouseDown] = WinMenu.OnSkillsClick;
        window.GetChild("btnMap").CallBack[(int) ControlState.MouseDown] = WinMenu.OnMapClick;
        window.GetChild("btnGuild").CallBack[(int) ControlState.MouseDown] = WinMenu.OnGuildClick;
        window.GetChild("btnQuest").CallBack[(int) ControlState.MouseDown] = WinMenu.OnQuestClick;
    }

    public void UpdateWindow_Inventory()
    {
        var window = WindowLoader.FromLayout("winInventory");
        window.OnDraw = WinInventory.OnDraw;
        window.CallBack[(int) ControlState.MouseMove] = WinInventory.OnMouseMove;
        window.CallBack[(int) ControlState.MouseDown] = WinInventory.OnMouseDown;
        window.CallBack[(int) ControlState.DoubleClick] = WinInventory.OnDoubleClick;
        window.GetChild("btnClose").CallBack[(int) ControlState.MouseDown] = WinMenu.OnInventoryClick;
    }

    public void UpdateWindow_Character()
    {
        var window = WindowLoader.FromLayout("winCharacter");
        window.OnDraw = WinCharacter.OnDrawCharacter;
        window.CallBack[(int) ControlState.MouseMove] = WinCharacter.OnMouseMove;
        window.CallBack[(int) ControlState.MouseDown] = WinCharacter.OnMouseMove;
        window.CallBack[(int) ControlState.DoubleClick] = WinCharacter.OnDoubleClick;
        window.GetChild("btnClose").CallBack[(int) ControlState.MouseDown] = WinMenu.OnCharacterClick;
        // Stat buttons may exist in layout; wire them if present
        window.GetChild("btnStat_1").CallBack[(int)ControlState.MouseDown] = WinCharacter.OnSpendPoint1;
        window.GetChild("btnStat_2").CallBack[(int)ControlState.MouseDown] = WinCharacter.OnSpendPoint2;
        window.GetChild("btnStat_3").CallBack[(int)ControlState.MouseDown] = WinCharacter.OnSpendPoint3;
        window.GetChild("btnStat_4").CallBack[(int)ControlState.MouseDown] = WinCharacter.OnSpendPoint4;
        window.GetChild("btnStat_5").CallBack[(int)ControlState.MouseDown] = WinCharacter.OnSpendPoint5;
    }

    public void UpdateWindow_Description()
    {
        var window = WindowLoader.FromLayout("winDescription");
        window.GetChild("picSprite").OnDraw = WinDescription.OnDraw;
    }

    public void UpdateWindow_RightClick()
    {
        var window = WindowLoader.FromLayout("winRightClickBG");
        window.CallBack[(int) ControlState.MouseDown] = WinPlayerMenu.OnClose;
    }

    public void UpdateWindow_PlayerMenu()
    {
        var window = WindowLoader.FromLayout("winPlayerMenu");
        window.CallBack[(int) ControlState.MouseDown] = WinPlayerMenu.OnClose;
        window.GetChild("btnName").CallBack[(int) ControlState.MouseDown] = WinPlayerMenu.OnClose;
        window.GetChild("btnParty").CallBack[(int) ControlState.MouseDown] = WinPlayerMenu.OnPartyInvite;
        window.GetChild("btnTrade").CallBack[(int) ControlState.MouseDown] = WinPlayerMenu.OnTradeRequest;
        window.GetChild("btnGuild").CallBack[(int) ControlState.MouseDown] = WinPlayerMenu.OnGuildInvite;
        window.GetChild("btnPM").CallBack[(int) ControlState.MouseDown] = WinPlayerMenu.OnPrivateMessage;
    }

    public void UpdateWindow_DragBox()
    {
        var window = WindowLoader.FromLayout("winDragBox");
        window.OnDraw = WinDragBox.OnDraw;
        window.CallBack[(int) ControlState.MouseUp] = WinDragBox.DragBox_Check;
    }

    public void UpdateWindow_Options()
    {
        var window = WindowLoader.FromLayout("winOptions");
        // Wire Confirm button
        window.GetChild("btnConfirm").CallBack[(int) ControlState.MouseDown] = WinOptions.OnConfirm;

        // Wire checkboxes to toggle their value on click (do not call OnConfirm)
        void ToggleCheckbox(string name)
        {
            var cb = window.GetChild(name);
            cb.CallBack[(int)ControlState.MouseDown] = () => { cb.Value = cb.Value == 0 ? 1 : 0; };
        }
        ToggleCheckbox("chkMusic");
        ToggleCheckbox("chkSound");
        ToggleCheckbox("chkAutotile");
        ToggleCheckbox("chkFullscreen");
        ToggleCheckbox("chkVsync");

        // Ensure options controls are visible (some skins may hide by default)
        window.GetChild("cmbRes").Visible = true;
        window.GetChild("chkVsync").Visible = true;

        Client.GameLogic.SetOptionsScreen();
    }

    public void UpdateWindow_Combobox()
    {
        var bg = WindowLoader.FromLayout("winComboMenuBG");
        bg.CallBack[(int) ControlState.DoubleClick] = WinComboMenu.Close;

        WindowLoader.FromLayout("winComboMenu");
    }

    public void UpdateWindow_Skills()
    {
        var window = WindowLoader.FromLayout("winSkills");
        window.OnDraw = WinSkills.OnDraw;
        window.CallBack[(int) ControlState.MouseMove] = WinSkills.OnMouseMove;
        window.CallBack[(int) ControlState.MouseDown] = WinSkills.OnMouseDown;
        window.CallBack[(int) ControlState.DoubleClick] = WinSkills.OnDoubleClick;
        window.GetChild("btnClose").CallBack[(int) ControlState.MouseDown] = WinMenu.OnSkillsClick;
        }

    public void UpdateWindow_Bank()
    {
        var window = WindowLoader.FromLayout("winBank");
        window.OnDraw = WinBank.OnDraw;
        window.CallBack[(int) ControlState.MouseMove] = WinBank.OnMouseMove;
        window.CallBack[(int) ControlState.MouseDown] = WinBank.OnMouseDown;
        window.CallBack[(int) ControlState.DoubleClick] = WinBank.OnDoubleClick;
        window.GetChild("btnClose").CallBack[(int) ControlState.MouseDown] = WinBank.OnClose;
    }

    public void UpdateWindow_Shop()
    {
        var window = WindowLoader.FromLayout("winShop");
        window.OnDraw = WinShop.OnDrawBackground;
        window.CallBack[(int) ControlState.MouseMove] = WinShop.OnMouseMove;
        window.CallBack[(int) ControlState.MouseDown] = WinShop.OnMouseDown;
        window.GetChild("btnClose").CallBack[(int) ControlState.MouseDown] = WinShop.OnClose;
        window.GetChild("picParchment").OnDraw = WinShop.OnDraw;
        window.GetChild("btnBuy").CallBack[(int) ControlState.MouseDown] = WinShop.OnBuy;
        window.GetChild("btnSell").CallBack[(int) ControlState.MouseDown] = WinShop.OnSell;
        window.GetChild("CheckboxBuying").CallBack[(int) ControlState.MouseDown] = WinShop.OnBuyingChecked;
        window.GetChild("CheckboxSelling").CallBack[(int) ControlState.MouseDown] = WinShop.OnSellingChecked;
    }

    public void UpdateWindow_Admin()
    {
        var window = WindowLoader.FromLayout("winAdmin");

        // Helpers
        void ShowDenied() => TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
        bool HasAccess(AccessLevel min) => GetPlayerAccess(GameState.MyIndex) >= (int)min;
        static bool IsNumeric(string s) => int.TryParse(s, out _);
        static int ReadInt(Control c, int fallback = 0) => int.TryParse(c.Text?.Trim(), out var n) ? n : fallback;

        // Close button (if present): hide panel and clear flag
        if (WindowManager.TryGetControl("winAdmin", "btnClose", out var btnClose))
        {
            btnClose.CallBack[(int)ControlState.MouseDown] = () =>
            {
                WindowManager.HideWindow("winAdmin");
                GameState.AdminPanel = false;
            };
        }

        // Defaults
        var txtName = window.GetChild("txtAdminName");
        txtName.Text = GetPlayerName(GameState.MyIndex);

        if (window.GetChild("cmbAccess") is ComboBox cmb)
        {
            foreach (var name in Enum.GetNames(typeof(AccessLevel)))
            {
                cmb.Items.Add(name);
            }
            cmb.Value = 0; // default selection index
        }

        // Moderation: numeric inputs default
        window.GetChild("txtAdminMap").Text = window.GetChild("txtAdminMap").Text?.Length > 0 ? window.GetChild("txtAdminMap").Text : "1";
        window.GetChild("txtAdminSprite").Text = window.GetChild("txtAdminSprite").Text?.Length > 0 ? window.GetChild("txtAdminSprite").Text : "0";

        // Wire Moderation actions
        window.GetChild("btnAdminWarpTo").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            var mapNum = ReadInt(window.GetChild("txtAdminMap"));
            Sender.WarpTo(mapNum);
        };

        window.GetChild("btnAdminBan").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            var name = txtName.Text?.Trim() ?? string.Empty;
            Sender.SendBan(name);
        };

        window.GetChild("btnAdminKick").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            var name = txtName.Text?.Trim() ?? string.Empty;
            Sender.SendKick(name);
        };

        window.GetChild("btnAdminWarp2Me").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            var name = txtName.Text?.Trim() ?? string.Empty;
            if (!IsNumeric(name)) Sender.WarpToMe(name);
        };

        window.GetChild("btnAdminWarpMe2").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            var name = txtName.Text?.Trim() ?? string.Empty;
            if (!IsNumeric(name)) Sender.WarpMeTo(name);
        };

        window.GetChild("btnAdminSetAccess").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Owner)) { ShowDenied(); return; }
            var name = txtName.Text?.Trim() ?? string.Empty;
            if (IsNumeric(name)) return;
            if (window.GetChild("cmbAccess") is ComboBox combo && combo.Value >= 0)
            {
                // Mirror legacy behavior: SelectedIndex + 1
                Sender.SendSetAccess(name, (byte)(combo.Value + 1));
            }
        };

        window.GetChild("btnAdminSetSprite").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            var sprite = ReadInt(window.GetChild("txtAdminSprite"));
            Sender.SendSetSprite(sprite);
        };

        window.GetChild("btnLevelUp").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Developer)) { ShowDenied(); return; }
            Sender.SendRequestLevelUp();
        };

        // Map List / Tools
        window.GetChild("btnMapReport").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            Sender.SendRequestMapReport();
        };

        // Warp button: use top visible line in the list
        if (WindowManager.TryGetControl("winAdmin", "btnMapWarp", out var btnMapWarp2))
        {
            btnMapWarp2.CallBack[(int)ControlState.MouseDown] = () =>
            {
                if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
                    if (WindowManager.TryGetControl("winAdmin", "cmbMaps", out var cmbCtrl) && cmbCtrl is ComboBox cmb && cmb.Items.Count > 0 && cmb.Value >= 0 && cmb.Value < cmb.Items.Count)
                    {
                        var item = cmb.Items[cmb.Value];
                        var colon = item.IndexOf(':');
                        if (colon > 0 && int.TryParse(item.AsSpan(0, colon), out var mapNum)) { Sender.WarpTo(mapNum); }
                    }
            };
        }

        window.GetChild("btnRespawn").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            Map.SendMapRespawn();
        };

        window.GetChild("btnALoc").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            GameState.BLoc = !GameState.BLoc;
        };


        // Editors
        window.GetChild("btnAnimationEditor").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Developer)) { ShowDenied(); return; }
            Sender.SendRequestEditAnimation();
        };

        window.GetChild("btnJobEditor").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Developer)) { ShowDenied(); return; }
            Sender.SendRequestEditJob();
        };

        window.GetChild("btnItemEditor").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Developer)) { ShowDenied(); return; }
            Sender.SendRequestEditItem();
        };

        window.GetChild("btnMapEditor").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            Map.SendRequestEditMap();
        };

        window.GetChild("btnNpcEditor").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Developer)) { ShowDenied(); return; }
            Sender.SendRequestEditNpc();
        };

        window.GetChild("btnProjectiles").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Developer)) { ShowDenied(); return; }
            Projectile.SendRequestEditProjectiles();
        };

        window.GetChild("btnResourceEditor").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Developer)) { ShowDenied(); return; }
            Sender.SendRequestEditResource();
        };

        window.GetChild("btnShopEditor").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Developer)) { ShowDenied(); return; }
            Sender.SendRequestEditShop();
        };

        window.GetChild("btnSkillEditor").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Developer)) { ShowDenied(); return; }
            Sender.SendRequestEditSkill();
        };

        window.GetChild("btnMoralEditor").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Developer)) { ShowDenied(); return; }
            Sender.SendRequestEditMoral();
        };

        window.GetChild("btnScriptEditor").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Owner)) { ShowDenied(); return; }
            Sender.SendRequestEditScript(0);
        };

        // Provide tab-like buttons to switch visible groups (because TabPage is a transparent container in our loader)
        var winIndex = WindowManager.GetWindowIndex("winAdmin");

        void SetVisible(bool visible, params string[] names)
        {
            foreach (var n in names)
            {
                if (WindowManager.TryGetControl("winAdmin", n, out var c))
                {
                    c.Visible = visible;
                }
            }
        }

        void ShowTab(string tab)
        {
            // Moderation controls (include background + labels)
            var moderation = new[]
            {
                "picModerationBG",
                "lblPlayerName","txtAdminName",
                "lblAccessLevel","cmbAccess","btnAdminSetAccess",
                "lblMapNumber","txtAdminMap","btnAdminWarpTo",
                "lblSprite","txtAdminSprite","btnAdminSetSprite",
                "btnAdminBan","btnAdminKick","btnLevelUp",
                "btnAdminWarp2Me","btnAdminWarpMe2"
            };

            // Map List controls (combo only)
            var mapList = new[] { "picMapListBG", "cmbMaps", "btnMapWarp", "btnMapReport" };

            // Map Tools controls
            var mapTools = new[] { "picMapToolsBG", "btnRespawn", "btnALoc" };

            // Editor controls
            var editors = new[]
            {
                "picEditorsBG",
                "btnAnimationEditor","btnJobEditor","btnItemEditor","btnMapEditor",
                "btnNpcEditor","btnProjectiles","btnResourceEditor","btnShopEditor",
                "btnSkillEditor","btnMoralEditor","btnScriptEditor"
            };

            // Hide all groups first
            SetVisible(false, moderation);
            SetVisible(false, mapList);
            SetVisible(false, mapTools);
            SetVisible(false, editors);

            // Show selected
            switch (tab)
            {
                case "Moderation": SetVisible(true, moderation); break;
                case "MapList": SetVisible(true, mapList); break;
                case "MapTools": SetVisible(true, mapTools); break;
                case "Editors": SetVisible(true, editors); break;
            }
        }

        void MakeTabButton(string name, string text, int x, string tabKey)
        {
            WindowManager.CreateButton(
                windowIndex: winIndex,
                name: name,
                left: x,
                top: 26,
                width: 140,
                height: 22,
                text: text,
                font: Font.Arial,
                designNorm: Design.Red,
                designHover: Design.RedHover,
                designMousedown: Design.RedClick,
                callbackMousedown: () => ShowTab(tabKey)
            );
        }

        // Create faux tab buttons above the content area
        MakeTabButton("btnTabModeration", "Moderation", 10, "Moderation");
        MakeTabButton("btnTabMapList", "Map List", 170, "MapList");
        MakeTabButton("btnTabMapTools", "Map Tools", 330, "MapTools");
        MakeTabButton("btnTabEditors", "Editors", 490, "Editors");

        // Default tab
        ShowTab("Moderation");
    }

    // Draw the admin map list using the list control's Text and scroll offset (Value)
    private static void OnDrawMapList()
    {
        var win = WindowManager.GetWindowByName("winAdmin");
        if (win is null) return;
        var lst = win.GetChild("lstMaps");
        if (lst is null || string.IsNullOrEmpty(lst.Text)) return;
        // Draw a black background panel behind the list area for readability
        DesignRenderer.Render(Design.TextBlack,
            win.X + lst.X,
            win.Y + lst.Y,
            lst.Width,
            lst.Height);

        var lines = lst.Text.Split('\n');
        var x = win.X + lst.X + 6;
        var y = win.Y + lst.Y + 6;
        var lineHeight = 14;

        // visible capacity
        var maxLines = Math.Max(0, (lst.Height - 8) / lineHeight);
        var start = Math.Clamp(lst.Value, 0, Math.Max(0, lines.Length - maxLines));
        var count = Math.Min(maxLines, Math.Max(0, lines.Length - start));

        for (int i = 0; i < count; i++)
        {
            var line = lines[start + i];
            if (!string.IsNullOrEmpty(line))
            {
                TextRenderer.RenderText(line, x, y + i * lineHeight, Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.Black);
            }
        }
    }
}