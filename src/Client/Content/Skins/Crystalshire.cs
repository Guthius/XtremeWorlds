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
        window.GetChild("nudAdminMap").Text = window.GetChild("nudAdminMap").Text?.Length > 0 ? window.GetChild("nudAdminMap").Text : "1";
        window.GetChild("nudAdminSprite").Text = window.GetChild("nudAdminSprite").Text?.Length > 0 ? window.GetChild("nudAdminSprite").Text : "0";

        // Wire Moderation actions
        window.GetChild("btnAdminWarpTo").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            var mapNum = ReadInt(window.GetChild("nudAdminMap"));
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
            var sprite = ReadInt(window.GetChild("nudAdminSprite"));
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
                "lblMapNumber","nudAdminMap","btnAdminWarpTo",
                "lblSprite","nudAdminSprite","btnAdminSetSprite",
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