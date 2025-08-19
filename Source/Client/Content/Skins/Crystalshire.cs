using System;
using Client.Game.UI;
using Client.Game.UI.Windows;
using Core.Configurations;
using Core.Globals;

public class Crystalshire
{
    public void UpdateWindow_Login()
    {
        var window = WindowLoader.FromLayout("winLogin");

        var username = SettingsManager.Instance.SaveUsername ? SettingsManager.Instance.Username : string.Empty;

        window.GetChild("btnClose").Click += WinLogin.OnClose;
        window.GetChild("txtUsername").Text = username;
        window.GetChild("chkSaveUsername").Value = SettingsManager.Instance.SaveUsername ? 1 : 0;
        window.GetChild("btnAccept").Click += WinLogin.OnAccept;
        window.GetChild("btnExit").Click += WinLogin.OnExit;
        window.GetChild("btnRegister").Click += WinLogin.OnRegister;

        Gui.SetActiveControl(window, username.Length == 0 ? "txtUsername" : "txtPassword");
    }

    public void UpdateWindow_Register()
    {
        var window = WindowLoader.FromLayout("winRegister");

        window.GetChild("btnClose").Click += WinRegister.OnClose;
        window.GetChild("btnAccept").Click += WinRegister.OnRegister;
        window.GetChild("btnExit").Click += WinRegister.OnClose;

        Gui.SetActiveControl(window, "txtUsername");
    }

    public void UpdateWindow_NewChar()
    {
        var window = WindowLoader.FromLayout("winNewChar");

        window.GetChild("btnClose").Click += WinNewChar.OnCancel;
        window.GetChild("btnAccept").Click += WinNewChar.OnAccept;
        window.GetChild("btnCancel").Click += WinNewChar.OnCancel;
        window.GetChild("picScene").Draw += WinNewChar.OnDrawSprite;
        window.GetChild("btnLeft").Click += WinNewChar.OnLeftClick;
        window.GetChild("btnRight").Click += WinNewChar.OnRightClick;

        Gui.SetActiveControl(window, "txtName");
    }

    public void UpdateWindow_Chars()
    {
        var window = WindowLoader.FromLayout("winChars");

        window.GetChild("btnClose").Click += WinChars.OnClose;
        window.GetChild("picScene_3").Draw += WinChars.OnDraw;
        window.GetChild("btnSelectChar_1").Click += WinChars.OnSelectCharacter1Click;
        window.GetChild("btnCreateChar_1").Click += WinChars.OnCreateCharacter1Click;
        window.GetChild("btnDelChar_1").Click += WinChars.OnDeleteCharacter1Click;
        window.GetChild("btnSelectChar_2").Click += WinChars.OnSelectCharacter2Click;
        window.GetChild("btnCreateChar_2").Click += WinChars.OnCreateCharacter2Click;
        window.GetChild("btnDelChar_2").Click += WinChars.OnDeleteCharacter2Click;
        window.GetChild("btnSelectChar_3").Click += WinChars.OnSelectCharacter3Click;
        window.GetChild("btnCreateChar_3").Click += WinChars.OnCreateCharacter3Click;
        window.GetChild("btnDelChar_3").Click += WinChars.OnDeleteCharacter3Click;
    }

    public void UpdateWindow_Jobs()
    {
        var window = WindowLoader.FromLayout("winJobs");

        window.GetChild("btnClose").Click += WinJobs.OnClose;
        window.GetChild("picParchment").Draw += WinJobs.OnDrawSprite;
        window.GetChild("btnLeft").Click += WinJobs.OnLeftClick;
        window.GetChild("btnRight").Click += WinJobs.OnRightClick;
        window.GetChild("btnAccept").Click += WinJobs.OnAccept;
        window.GetChild("picOverlay").Click += WinJobs.OnClose;
        window.GetChild("picOverlay").Draw += WinJobs.OnDrawDescription;
    }

    public void UpdateWindow_Dialogue()
    {
        var window = WindowLoader.FromLayout("winDialogue");

        window.GetChild("btnClose").Click += WinDialogue.OnClose;
        window.GetChild("btnYes").Click += WinDialogue.OnYes;
        window.GetChild("btnNo").Click += WinDialogue.OnNo;
        window.GetChild("btnOkay").Click += WinDialogue.OnOkay;

        Gui.SetActiveControl(window, "txtInput");
    }

    public void UpdateWindow_Party()
    {
        WindowLoader.FromLayout("winParty");
    }

    public void UpdateWindow_Trade()
    {
        var windowIndex = Gui.CreateWindow(
            name: "winTrade",
            caption: "Trading with [Name]",
            font: Font.Georgia,
            left: 0, top: 0, width: 412, height: 386,
            icon: 112,
            visible: false,
            xOffset: 2, yOffset: 5,
            designNorm: Design.WindowEmpty,
            designHover: Design.WindowEmpty,
            designMousedown: Design.WindowEmpty,
            onDraw: WinTrade.OnDraw);

        Gui.MoveToCenterScreen(windowIndex);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnClose",
            x: Gui.Windows[windowIndex].Width - 19, y: 5, width: 36, height: 36,
            image: 8,
            imageHover: 9,
            imageMouseDown: 10,
            callbackMousedown: WinTrade.OnClose);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picParchment",
            x: 10, y: 312, width: 392, height: 66,
            design: Design.Parchment,
            designHover: Design.Parchment,
            designMousedown: Design.Parchment);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picShadow",
            x: 36, y: 30, width: 142, height: 9,
            design: Design.Parchment,
            designHover: Design.Parchment,
            designMousedown: Design.Parchment);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblYourTrade",
            x: 36, y: 27, width: 142, height: 9,
            text: "Robin's Offer",
            font: Font.Georgia,
            align: Alignment.Center);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picShadow",
            x: 236, y: 30, width: 142, height: 9,
            design: Design.Parchment,
            designHover: Design.Parchment,
            designMousedown: Design.Parchment);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblTheirTrade",
            x: 236, y: 27, width: 142, height: 9,
            text: "Richard's Offer",
            font: Font.Georgia,
            align: Alignment.Center);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnAccept",
            x: 134, y: 340, width: 68, height: 24,
            text: "Accept",
            design: Design.Green,
            designHover: Design.GreenHover,
            designMouseDown: Design.GreenClick,
            callbackMousedown: WinTrade.OnAccept);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnDecline",
            x: 210, y: 340, width: 68, height: 24,
            text: "Decline",
            design: Design.Red,
            designHover: Design.RedHover,
            designMouseDown: Design.RedClick,
            callbackMousedown: WinTrade.OnClose);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblStatus",
            x: 114, y: 322, width: 184, height: 10,
            text: "",
            font: Font.Georgia,
            align: Alignment.Center);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblBlank",
            x: 25, y: 330, width: 100, height: 10,
            text: "Total Value",
            font: Font.Georgia,
            align: Alignment.Center);

        Gui.CreateLabel(
            windowIndex, "lblBlank",
            285, 330, 100, 10,
            "Total Value",
            Font.Georgia,
            Alignment.Center);

        Gui.CreateLabel(
            windowIndex,
            "lblYourValue",
            25, 344, 100, 10,
            "52,812g",
            Font.Georgia,
            Alignment.Center);

        Gui.CreateLabel(
            windowIndex, "lblTheirValue",
            285, 344, 100, 10,
            "12,531g",
            Font.Georgia,
            Alignment.Center);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picYour", x: 14, y: 46, width: 184, height: 260,
            callbackMousedown: WinTrade.OnYourTradeMouseMove,
            callbackMousemove: WinTrade.OnYourTradeMouseMove,
            callbackDblclick: WinTrade.OnYourTradeClick,
            onDraw: Gui.DrawYourTrade);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picTheir",
            x: 214, y: 46, width: 184, height: 260,
            callbackMousedown: WinTrade.OnTheirTradeMouseMove,
            callbackMousemove: WinTrade.OnTheirTradeMouseMove,
            callbackDblclick: WinTrade.OnTheirTradeMouseMove,
            onDraw: Gui.DrawTheirTrade);
    }

    public void UpdateWindow_EscMenu()
    {
        var window = WindowLoader.FromLayout("winEscMenu");

        window.GetChild("btnReturn").Click += WinEscMenu.OnClose;
        window.GetChild("btnOptions").Click += WinEscMenu.OnOptionsClick;
        window.GetChild("btnMainMenu").Click += WinEscMenu.OnMainMenuClick;
        window.GetChild("btnExit").Click += WinEscMenu.OnExitClick;
    }

    public void UpdateWindow_Bars()
    {
        var window = WindowLoader.FromLayout("winBars");

        window.GetChild("picBlank").Draw += WinBars.OnDraw;
    }

    public void UpdateWindow_Chat()
    {
        var windowIndex = Gui.CreateWindow(
            name: "winChat",
            caption: "",
            font: Font.Georgia,
            left: 8, top: Client.GameState.ResolutionHeight - 178, width: 352, height: 152,
            icon: 0,
            visible: false,
            canDrag: false);
        
        Gui.CreateCheckBox(
            windowIndex: windowIndex,
            name: "chkGame",
            x: 10, y: 2, width: 49, height: 23,
            text: "Game",
            font: Font.Arial,
            design: Design.CheckboxChat,
            callbackMousedown: WinChat.OnGameChannelClicked);

        Gui.CreateCheckBox(
            windowIndex: windowIndex,
            name: "chkMap",
            x: 60, y: 2, width: 49, height: 23,
            text: "Map",
            font: Font.Arial,
            design: Design.CheckboxChat,
            callbackMousedown: WinChat.OnMapChannelClicked);

        Gui.CreateCheckBox(
            windowIndex: windowIndex,
            name: "chkGlobal",
            x: 110, y: 2, width: 49, height: 23,
            text: "Global",
            font: Font.Arial,
            design: Design.CheckboxChat,
            callbackMousedown: WinChat.OnBroadcastChannelClicked);

        Gui.CreateCheckBox(
            windowIndex: windowIndex,
            name: "chkParty",
            x: 160, y: 2, width: 49, height: 23,
            text: "Party",
            font: Font.Arial,
            design: Design.CheckboxChat,
            callbackMousedown: WinChat.OnPartyChannelClicked);

        Gui.CreateCheckBox(
            windowIndex: windowIndex,
            name: "chkGuild",
            x: 210, y: 2, width: 49, height: 23,
            text: "Guild",
            font: Font.Arial,
            design: Design.CheckboxChat,
            callbackMousedown: WinChat.OnGuildChannelClicked);

        Gui.CreateCheckBox(
            windowIndex: windowIndex,
            name: "chkPlayer",
            x: 260, y: 2, width: 49, height: 23,
            text: "Player",
            font: Font.Arial,
            design: Design.CheckboxChat,
            callbackMousedown: WinChat.OnPrivateChannelClicked);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picNull",
            x: 0, y: 0, width: 0, height: 0,
            onDraw: WinChat.OnDraw);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnChat",
            x: 296, y: 140, width: 48, height: 20,
            text: "Say",
            font: Font.Arial,
            design: Design.Green,
            designHover: Design.GreenHover,
            designMouseDown: Design.GreenClick,
            callbackNorm: WinChat.OnSayClick);

        Gui.CreateTextbox(
            windowIndex: windowIndex,
            name: "txtChat",
            x: 12, y: 143, width: 352, height: 25,
            visible: false);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnUp",
            x: 328, y: 28, width: 10, height: 13,
            image: 4,
            imageHover: 52,
            imageMouseDown: 4,
            callbackMousedown: WinChat.OnUpButtonMouseDown);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnDown",
            x: 327, y: 122, width: 10, height: 13,
            image: 5,
            imageHover: 53,
            imageMouseDown: 5,
            callbackMousedown: WinChat.OnDownButtonMouseDown);

        var window = Gui.Windows[windowIndex];

        window.GetChild("btnUp").MouseUp += WinChat.OnUpButtonMouseUp;
        window.GetChild("btnDown").MouseUp += WinChat.OnDownButtonMouseUp;

        Gui.SetActiveControl(window, "txtChat");

        window.GetChild("chkGame").Value = SettingsManager.Instance.ChannelState[(int) ChatChannel.Game];
        window.GetChild("chkMap").Value = SettingsManager.Instance.ChannelState[(int) ChatChannel.Map];
        window.GetChild("chkGlobal").Value = SettingsManager.Instance.ChannelState[(int) ChatChannel.Broadcast];
        window.GetChild("chkParty").Value = SettingsManager.Instance.ChannelState[(int) ChatChannel.Party];
        window.GetChild("chkGuild").Value = SettingsManager.Instance.ChannelState[(int) ChatChannel.Guild];
        window.GetChild("chkPlayer").Value = SettingsManager.Instance.ChannelState[(int) ChatChannel.Private];
    }

    public void UpdateWindow_ChatSmall()
    {
        var windowIndex = Gui.CreateWindow(
            name: "winChatSmall",
            caption: "",
            font: Font.Georgia,
            left: 8, top: 0, width: 0, height: 0,
            icon: 0,
            visible: false,
            onDraw: WinChat.OnDrawSmall,
            canDrag: false,
            clickthrough: true);
        
        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblMsg",
            x: 12, y: 140, width: 286, height: 25,
            text: "Press 'Enter' to open chat",
            font: Font.Georgia);
    }

    public void UpdateWindow_Hotbar()
    {
        Gui.CreateWindow(
            name: "winHotbar",
            caption: "",
            font: Font.Georgia,
            left: 432, top: 10, width: 418, height: 36, icon: 0,
            visible: false,
            callbackMousemove: WinHotBar.OnMouseMove,
            callbackMousedown: WinHotBar.OnMouseDown,
            callbackDblclick: WinHotBar.OnDoubleClick,
            onDraw: WinHotBar.OnDraw,
            canDrag: false,
            canFocus: false);
    }

    public void UpdateWindow_Menu()
    {
        var windowIndex = Gui.CreateWindow(
            name: "winMenu", caption: "",
            font: Font.Georgia,
            left: Client.GameState.ResolutionWidth - 229,
            top: Client.GameState.ResolutionHeight - 31,
            width: 229, height: 30,
            icon: 0,
            visible: false,
            canDrag: false,
            clickthrough: true);
        
        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picWood",
            x: 0, y: 5, width: 228, height: 20,
            design: Design.Wood,
            designHover: Design.Wood,
            designMousedown: Design.Wood);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnChar",
            x: 8, y: 0, width: 29, height: 29,
            icon: 108,
            design: Design.Green,
            designHover: Design.GreenHover,
            designMouseDown: Design.GreenClick,
            callbackMousedown: WinMenu.OnCharacterClick,
            xOffset: -1, yOffset: -2,
            tooltip: "Character (C)");

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnInv",
            x: 44, y: 0, width: 29, height: 29,
            icon: 1,
            design: Design.Green,
            designHover: Design.GreenHover,
            designMouseDown: Design.GreenClick,
            callbackMousedown: WinMenu.OnInventoryClick,
            xOffset: -1, yOffset: -2,
            tooltip: "Inventory (I)");

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnSkills",
            x: 82, y: 0, width: 29, height: 29,
            icon: 109,
            design: Design.Green,
            designHover: Design.GreenHover,
            designMouseDown: Design.GreenClick,
            callbackMousedown: WinMenu.OnSkillsClick,
            xOffset: -1, yOffset: -2,
            tooltip: "Skills (K)");

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnMap",
            x: 119, y: 0, width: 29, height: 29,
            icon: 106,
            design: Design.Grey,
            designHover: Design.Grey,
            designMouseDown: Design.Grey,
            callbackMousedown: WinMenu.OnMapClick,
            xOffset: -1, yOffset: -2);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnClient.Guild",
            x: 155, y: 0, width: 29, height: 29,
            icon: 107,
            design: Design.Grey,
            designHover: Design.Grey,
            designMouseDown: Design.Grey,
            callbackMousedown: WinMenu.OnGuildClick,
            xOffset: -1, yOffset: -1);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnQuest",
            x: 190, y: 0, width: 29, height: 29,
            icon: 23,
            design: Design.Grey,
            designHover: Design.Grey,
            designMouseDown: Design.Grey,
            callbackNorm: null,
            callbackHover: null,
            callbackMousedown: WinMenu.OnQuestClick,
            callbackMousemove: null,
            callbackDblclick: null,
            xOffset: -1, yOffset: -2);
    }

    public void UpdateWindow_Inventory()
    {
        var windowIndex = Gui.CreateWindow(
            name: "winInventory",
            caption: "Inventory",
            font: Font.Georgia,
            left: 0, top: 0, width: 202, height: 319,
            icon: 1,
            visible: false,
            xOffset: 2, yOffset: 7,
            designNorm: Design.WindowEmpty,
            designHover: Design.WindowEmpty,
            designMousedown: Design.WindowEmpty,
            callbackMousemove: WinInventory.OnMouseMove,
            callbackMousedown: WinInventory.OnMouseDown,
            callbackDblclick: WinInventory.OnDoubleClick,
            onDraw: WinInventory.OnDraw);

        Gui.MoveToCenterScreen(windowIndex);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnClose",
            x: Gui.Windows[windowIndex].Width - 19, y: 5, width: 16, height: 16,
            image: 8,
            imageHover: 9,
            imageMouseDown: 10,
            callbackMousedown: WinMenu.OnInventoryClick);

        Gui.CreatePictureBox(
            windowIndex,
            "picBlank",
            8, 293, 186, 18,
            image: 67,
            imageHover: 67,
            imageMousedown: 67);
    }

    public void UpdateWindow_Character()
    {
        var windowIndex = Gui.CreateWindow(
            name: "winCharacter",
            caption: "Character",
            font: Font.Georgia,
            left: 0, top: 0, width: 174, height: 356,
            icon: 62,
            visible: false,
            xOffset: 2, yOffset: 6,
            designNorm: Design.WindowEmpty,
            designHover: Design.WindowEmpty,
            designMousedown: Design.WindowEmpty,
            callbackMousemove: WinCharacter.OnMouseMove,
            callbackMousedown: WinCharacter.OnMouseMove,
            callbackDblclick: WinCharacter.OnDoubleClick,
            onDraw: WinCharacter.OnDrawCharacter);

        Gui.MoveToCenterScreen(windowIndex);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnClose",
            x: Gui.Windows[windowIndex].Width - 19, y: 5, width: 16, height: 16,
            image: 8,
            imageHover: 9,
            imageMouseDown: 10,
            callbackMousedown: WinMenu.OnCharacterClick);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picParchment",
            x: 6, y: 26, width: 162, height: 287,
            design: Design.Parchment,
            designHover: Design.Parchment,
            designMousedown: Design.Parchment);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picWhiteBox",
            x: 13, y: 34, width: 148, height: 19,
            design: Design.TextWhite,
            designHover: Design.TextWhite,
            designMousedown: Design.TextWhite);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picWhiteBox",
            x: 13, y: 54, width: 148, height: 19,
            design: Design.TextWhite,
            designHover: Design.TextWhite,
            designMousedown: Design.TextWhite);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picWhiteBox",
            x: 13, y: 74, width: 148, height: 19,
            design: Design.TextWhite,
            designHover: Design.TextWhite,
            designMousedown: Design.TextWhite);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picWhiteBox",
            x: 13, y: 94, width: 148, height: 19,
            design: Design.TextWhite,
            designHover: Design.TextWhite,
            designMousedown: Design.TextWhite);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picWhiteBox",
            x: 13, y: 114, width: 148, height: 19,
            design: Design.TextWhite,
            designHover: Design.TextWhite,
            designMousedown: Design.TextWhite);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picWhiteBox",
            x: 13, y: 134, width: 148, height: 19,
            design: Design.TextWhite,
            designHover: Design.TextWhite,
            designMousedown: Design.TextWhite);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picWhiteBox",
            x: 13, y: 154, width: 148, height: 19,
            design: Design.TextWhite,
            designHover: Design.TextWhite,
            designMousedown: Design.TextWhite);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblName",
            x: 18, y: 36, width: 147, height: 10,
            text: "Name",
            font: Font.Arial);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblJob",
            x: 18, y: 56, width: 147, height: 10,
            text: "Job",
            font: Font.Arial);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblLevel",
            x: 18, y: 76, width: 147, height: 10,
            text: "Level",
            font: Font.Arial);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblGuild",
            x: 18, y: 96, width: 147, height: 10,
            text: "Guild",
            font: Font.Arial);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblHealth",
            x: 18, y: 116, width: 147, height: 10,
            text: "Health",
            font: Font.Arial);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblSpirit",
            x: 18, y: 136, width: 147, height: 10,
            text: "Spirit",
            font: Font.Arial);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblExperience",
            x: 18, y: 156, width: 147, height: 10,
            text: "Experience",
            font: Font.Arial);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblName2",
            x: 13, y: 36, width: 147, height: 10,
            text: "Name",
            font: Font.Arial,
            align: Alignment.Right);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblJob2",
            x: 13, y: 56, width: 147, height: 10,
            text: "",
            font: Font.Arial,
            align: Alignment.Right);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblLevel2",
            x: 13, y: 76, width: 147, height: 10,
            text: "Level",
            font: Font.Arial,
            align: Alignment.Right);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblGuild2",
            x: 13, y: 96, width: 147, height: 10,
            text: "Guild",
            font: Font.Arial,
            align: Alignment.Right);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblHealth2",
            x: 13, y: 116, width: 147, height: 10,
            text: "Health",
            font: Font.Arial,
            align: Alignment.Right);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblSpirit2",
            x: 13, y: 136, width: 147, height: 10,
            text: "Spirit",
            font: Font.Arial,
            align: Alignment.Right);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblExperience2",
            x: 13, y: 156, width: 147, height: 10,
            text: "Experience",
            font: Font.Arial,
            align: Alignment.Right);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picShadow",
            x: 18, y: 176, width: 138, height: 9,
            design: Design.BlackOval,
            designHover: Design.BlackOval,
            designMousedown: Design.BlackOval);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblLabel",
            x: 18, y: 173, width: 138, height: 10,
            text: "Attributes",
            font: Font.Arial,
            align: Alignment.Center);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picBlackBox",
            x: 13, y: 186, width: 148, height: 19,
            design: Design.TextBlack,
            designHover: Design.TextBlack,
            designMousedown: Design.TextBlack);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picBlackBox",
            x: 13, y: 206, width: 148, height: 19,
            design: Design.TextBlack,
            designHover: Design.TextBlack,
            designMousedown: Design.TextBlack);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picBlackBox",
            x: 13, y: 226, width: 148, height: 19,
            design: Design.TextBlack,
            designHover: Design.TextBlack,
            designMousedown: Design.TextBlack);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picBlackBox",
            x: 13, y: 246, width: 148, height: 19,
            design: Design.TextBlack,
            designHover: Design.TextBlack,
            designMousedown: Design.TextBlack);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picBlackBox",
            x: 13, y: 266, width: 148, height: 19,
            design: Design.TextBlack,
            designHover: Design.TextBlack,
            designMousedown: Design.TextBlack);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picBlackBox",
            x: 13, y: 286, width: 148, height: 19,
            design: Design.TextBlack,
            designHover: Design.TextBlack,
            designMousedown: Design.TextBlack);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblLabel",
            x: 18, y: 188, width: 138, height: 10,
            text: "Strength",
            font: Font.Arial);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblLabel",
            x: 18, y: 208, width: 138, height: 10,
            text: "Vitality",
            font: Font.Arial);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblLabel",
            x: 18, y: 228, width: 138, height: 10,
            text: "Intelligence",
            font: Font.Arial);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblLabel",
            x: 18, y: 248, width: 138, height: 10,
            text: "Luck",
            font: Font.Arial);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblLabel",
            x: 18, y: 268, width: 138, height: 10,
            text: "Spirit",
            font: Font.Arial);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblLabel",
            x: 18, y: 288, width: 138, height: 10,
            text: "Stat Points",
            font: Font.Arial);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnStat_1",
            x: 144, y: 188, width: 15, height: 15,
            image: 48,
            imageHover: 49,
            imageMouseDown: 50,
            callbackMousedown: WinCharacter.OnSpendPoint1);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnStat_2",
            x: 144, y: 208, width: 15, height: 15,
            image: 48,
            imageHover: 49,
            imageMouseDown: 50,
            callbackMousedown: WinCharacter.OnSpendPoint2);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnStat_3",
            x: 144, y: 228, width: 15, height: 15,
            image: 48,
            imageHover: 49,
            imageMouseDown: 50,
            callbackMousedown: WinCharacter.OnSpendPoint3);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnStat_4",
            x: 144, y: 248, width: 15, height: 15,
            image: 48,
            imageHover: 49,
            imageMouseDown: 50,
            callbackMousedown: WinCharacter.OnSpendPoint4);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnStat_5",
            x: 144, y: 268, width: 15, height: 15,
            image: 48,
            imageHover: 49,
            imageMouseDown: 50,
            callbackMousedown: WinCharacter.OnSpendPoint5);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "btnGreyStat_1",
            x: 144, y: 188, width: 15, height: 15,
            image: 47,
            imageHover: 47,
            imageMousedown: 47);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "btnGreyStat_2",
            x: 144, y: 208, width: 15, height: 15,
            image: 47,
            imageHover: 47,
            imageMousedown: 47);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "btnGreyStat_3",
            x: 144, y: 228, width: 15, height: 15,
            image: 47,
            imageHover: 47,
            imageMousedown: 47);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "btnGreyStat_4",
            x: 144, y: 248, width: 15, height: 15,
            image: 47,
            imageHover: 47,
            imageMousedown: 47);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "btnGreyStat_5",
            x: 144, y: 268, width: 15, height: 15,
            image: 47,
            imageHover: 47,
            imageMousedown: 47);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblStat_1",
            x: 42, y: 188, width: 100, height: 15,
            text: "255",
            font: Font.Arial,
            align: Alignment.Right);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblStat_2",
            x: 42, y: 208, width: 100, height: 15,
            text: "255",
            font: Font.Arial,
            align: Alignment.Right);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblStat_3",
            x: 42, y: 228, width: 100, height: 15,
            text: "255",
            font: Font.Arial,
            align: Alignment.Right);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblStat_4",
            x: 42, y: 248, width: 100, height: 15,
            text: "255",
            font: Font.Arial,
            align: Alignment.Right);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblStat_5",
            x: 42, y: 268, width: 100, height: 15,
            text: "255",
            font: Font.Arial,
            align: Alignment.Right);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblPoints",
            x: 57, y: 288, width: 100, height: 15,
            text: "255",
            font: Font.Arial,
            align: Alignment.Right);
    }

    public void UpdateWindow_Description()
    {
        var windowIndex = Gui.CreateWindow(
            name: "winDescription",
            caption: "",
            font: Font.Georgia,
            left: 0, top: 0, width: 193, height: 142,
            icon: 0,
            visible: false,
            designNorm: Design.WindowDescription,
            designHover: Design.WindowDescription,
            designMousedown: Design.WindowDescription,
            canDrag: false);
        
        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblName",
            x: 8, y: 12, width: 177, height: 10,
            text: "Flame Sword",
            font: Font.Arial,
            align: Alignment.Center);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picSprite",
            x: 18, y: 32, width: 68, height: 68,
            design: Design.DescriptionPicture,
            designHover: Design.DescriptionPicture,
            designMousedown: Design.DescriptionPicture,
            onDraw: WinDescription.OnDraw);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picSep",
            x: 96, y: 28, width: 0, height: 92,
            image: 44,
            imageHover: 44,
            imageMousedown: 44);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblJob",
            x: 5, y: 102, width: 92, height: 10,
            text: "Warrior",
            font: Font.Georgia,
            align: Alignment.Center);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblLevel",
            x: 5, y: 114, width: 92, height: 10,
            text: "Level 20",
            font: Font.Georgia,
            align: Alignment.Center);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picBar",
            x: 19, y: 114, width: 66, height: 12,
            visible: false,
            image: 45,
            imageHover: 45,
            imageMousedown: 45);
    }

    public void UpdateWindow_RightClick()
    {
        var windowIndex = Gui.CreateWindow(
            name: "winRightClickBG",
            caption: "",
            font: Font.Georgia,
            left: 0, top: 0, width: 800, height: 600,
            icon: 0,
            visible: false,
            callbackMousedown: WinPlayerMenu.OnClose,
            canDrag: false);

        Gui.MoveToCenterScreen(windowIndex);
    }

    public void UpdateWindow_PlayerMenu()
    {
        var windowIndex = Gui.CreateWindow(
            name: "winPlayerMenu",
            caption: "",
            font: Font.Georgia,
            left: 0, top: 0, width: 110, height: 106,
            icon: 0,
            visible: false,
            designNorm: Design.WindowDescription,
            designHover: Design.WindowDescription,
            designMousedown: Design.WindowDescription,
            callbackMousedown: WinPlayerMenu.OnClose,
            canDrag: false);

        Gui.MoveToCenterScreen(windowIndex);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnName",
            x: 8, y: 8, width: 94, height: 18,
            text: "[Name]",
            design: Design.MenuHeader,
            designHover: Design.MenuHeader,
            designMouseDown: Design.MenuHeader,
            callbackMousedown: WinPlayerMenu.OnClose);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnParty",
            x: 8, y: 26, width: 94, height: 18,
            text: "Invite to Party",
            designHover: Design.MenuOption,
            callbackMousedown: WinPlayerMenu.OnPartyInvite);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnTrade",
            x: 8, y: 44, width: 94, height: 18,
            text: "Request Trade",
            designHover: Design.MenuOption,
            callbackMousedown: WinPlayerMenu.OnTradeRequest);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnClient.Guild",
            x: 8, y: 62, width: 94, height: 18,
            text: "Invite to Client.Guild",
            design: Design.MenuOption,
            callbackMousedown: WinPlayerMenu.OnGuildInvite);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnPM",
            x: 8, y: 80, width: 94, height: 18,
            text: "Private Message",
            designHover: Design.MenuOption,
            callbackMousedown: WinPlayerMenu.OnPrivateMessage);
    }

    public void UpdateWindow_DragBox()
    {
        var windowIndex = Gui.CreateWindow(
            name: "winDragBox",
            caption: "",
            font: Font.Georgia,
            left: 0, top: 0, width: 32, height: 32,
            icon: 0,
            visible: false,
            onDraw: WinDragBox.OnDraw);

        Gui.Windows[windowIndex].CallBack[(int) ControlState.MouseUp] = WinDragBox.DragBox_Check;
    }

    public void UpdateWindow_Options()
    {
        var windowIndex = Gui.CreateWindow(
            name: "winOptions",
            caption: "",
            font: Font.Georgia,
            left: 0, top: 0, width: 210, height: 212,
            icon: 0,
            visible: false,
            designNorm: Design.WindowNoBar,
            designHover: Design.WindowNoBar,
            designMousedown: Design.WindowNoBar);

        Gui.MoveToCenterScreen(windowIndex);
   
        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picParchment",
            x: 6, y: 6, width: 198, height: 200,
            design: Design.Parchment,
            designHover: Design.Parchment,
            designMousedown: Design.Parchment);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picBlank",
            x: 35, y: 25, width: 140, height: 10,
            design: Design.Parchment,
            designHover: Design.Parchment,
            designMousedown: Design.Parchment);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblBlank",
            x: 35, y: 22, width: 140, height: 0,
            text: "General Options",
            font: Font.Georgia,
            align: Alignment.Center);

        Gui.CreateCheckBox(
            windowIndex: windowIndex,
            name: "chkMusic",
            x: 35, y: 40, width: 80,
            text: "Music",
            design: Design.CheckboxNormal);

        Gui.CreateCheckBox(
            windowIndex: windowIndex,
            name: "chkSound",
            x: 115, y: 40, width: 80,
            text: "Sound",
            design: Design.CheckboxNormal);

        Gui.CreateCheckBox(
            windowIndex: windowIndex,
            name: "chkAutotile",
            x: 35, y: 60, width: 80,
            text: "Autotile",
            design: Design.CheckboxNormal);

        Gui.CreateCheckBox(
            windowIndex: windowIndex,
            name: "chkFullscreen",
            x: 115, y: 60, width: 80,
            text: "Fullscreen",
            design: Design.CheckboxNormal);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picBlank",
            x: 35, y: 85, width: 140, height: 10,
            design: Design.Parchment,
            designHover: Design.Parchment,
            designMousedown: Design.Parchment);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblBlank",
            x: 35, y: 92, width: 140, height: 10,
            text: "Select Resolution",
            font: Font.Georgia,
            align: Alignment.Center);

        Gui.CreateComboBox(
            windowIndex: windowIndex,
            name: "cmbRes",
            x: 30, y: 100, width: 150, height: 18,
            design: Design.ComboBoxNormal);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnConfirm",
            x: 65, y: 168, width: 80, height: 22,
            text: "Confirm",
            design: Design.Green,
            designHover: Design.GreenHover,
            designMouseDown: Design.GreenClick,
            callbackMousedown: WinOptions.OnConfirm);

        Client.GameLogic.SetOptionsScreen();
    }

    public void UpdateWindow_Combobox()
    {
        Gui.CreateWindow(
            name: "winComboMenuBG",
            caption: "ComboMenuBG",
            font: Font.Georgia,
            left: 0, top: 0, width: 800, height: 600,
            icon: 0,
            visible: false,
            callbackDblclick: WinComboMenu.Close,
            canFocus: false);

        var windowIndex = Gui.CreateWindow(
            name: "winComboMenu",
            caption: "ComboMenu",
            font: Font.Georgia,
            left: 0, top: 0, width: 100, height: 100,
            icon: 0,
            visible: false,
            designNorm: Design.ComboMenuNormal,
            clickthrough: false);

        Gui.MoveToCenterScreen(windowIndex);
    }

    public void UpdateWindow_Skills()
    {
        var windowIndex = Gui.CreateWindow(
            name: "winSkills",
            caption: "Skills",
            font: Font.Georgia,
            left: 0, top: 0, width: 202, height: 297,
            icon: 109,
            visible: false,
            xOffset: 2, yOffset: 7,
            designNorm: Design.WindowEmpty,
            designHover: Design.WindowEmpty,
            designMousedown: Design.WindowEmpty,
            callbackMousemove: WinSkills.OnMouseMove,
            callbackMousedown: WinSkills.OnMouseDown,
            callbackDblclick: WinSkills.OnDoubleClick,
            onDraw: WinSkills.OnDraw);

        Gui.MoveToCenterScreen(windowIndex);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnClose",
            x: Gui.Windows[windowIndex].Width - 19, y: 5, width: 16, height: 16,
            image: 8,
            imageHover: 9,
            imageMouseDown: 10,
            callbackMousedown: WinMenu.OnSkillsClick);
    }

    public void UpdateWindow_Bank()
    {
        var windowIndex = Gui.CreateWindow(
            name: "winBank",
            caption: "Bank",
            font: Font.Georgia,
            left: 0, top: 0, width: 390, height: 373,
            icon: 0,
            visible: false,
            xOffset: 2, yOffset: 5,
            designNorm: Design.WindowEmpty,
            designHover: Design.WindowEmpty,
            designMousedown: Design.WindowEmpty,
            callbackMousemove: WinBank.OnMouseMove,
            callbackMousedown: WinBank.OnMouseDown,
            callbackDblclick: WinBank.OnDoubleClick,
            onDraw: WinBank.OnDraw);

        Gui.MoveToCenterScreen(windowIndex);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnClose",
            x: Gui.Windows[windowIndex].Width - 19, y: 5, width: 36, height: 36,
            image: 8,
            imageHover: 9,
            imageMouseDown: 10,
            callbackMousedown: WinBank.OnClose);
    }

    public void UpdateWindow_Shop()
    {
        var windowIndex = Gui.CreateWindow(
            name: "winShop",
            caption: "Shop",
            font: Font.Georgia,
            left: 0, top: 0, width: 278, height: 293,
            icon: 17,
            visible: false,
            xOffset: 2, yOffset: 5,
            designNorm: Design.WindowEmpty,
            designHover: Design.WindowEmpty,
            designMousedown: Design.WindowEmpty,
            callbackMousemove: WinShop.OnMouseMove,
            callbackMousedown: WinShop.OnMouseDown,
            onDraw: WinShop.OnDrawBackground);

        Gui.MoveToCenterScreen(windowIndex);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnClose",
            x: Gui.Windows[windowIndex].Width - 19, y: 6, width: 36, height: 36,
            image: 8,
            imageHover: 9,
            imageMouseDown: 10,
            callbackMousedown: WinShop.OnClose);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picParchment",
            x: 6, y: 215, width: 266, height: 50,
            design: Design.Parchment,
            designHover: Design.Parchment,
            designMousedown: Design.Parchment,
            onDraw: WinShop.OnDraw);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picItemBG",
            x: 13, y: 222, width: 36, height: 36,
            image: 30,
            imageHover: 30,
            imageMousedown: 30);

        Gui.CreatePictureBox(
            windowIndex: windowIndex,
            name: "picItem",
            x: 15, y: 224, width: 32, height: 32);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnBuy",
            x: 190, y: 228, width: 70, height: 24,
            text: "Buy", font: Font.Arial,
            design: Design.Green,
            designHover: Design.GreenHover,
            designMouseDown: Design.GreenClick,
            callbackMousedown: WinShop.OnBuy);

        Gui.CreateButton(
            windowIndex: windowIndex,
            name: "btnSell",
            x: 190, y: 228, width: 70, height: 24,
            text: "Sell",
            font: Font.Arial,
            visible: false,
            design: Design.Red,
            designHover: Design.RedHover,
            designMouseDown: Design.RedClick,
            callbackMousedown: WinShop.OnSell);

        Gui.CreateCheckBox(
            windowIndex: windowIndex,
            name: "CheckboxBuying",
            x: 173, y: 265, width: 49, height: 20,
            design: Design.CheckboxBuying,
            callbackMousedown: WinShop.OnBuyingChecked);

        Gui.CreateCheckBox(
            windowIndex,
            "CheckboxSelling",
            222, 265, 49, 20,
            design: Design.CheckboxSelling,
            callbackMousedown: WinShop.OnSellingChecked);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblName",
            x: 56, y: 226, width: 300, height: 10,
            text: "Test Item",
            font: Font.Arial);

        Gui.CreateLabel(
            windowIndex: windowIndex,
            name: "lblCost",
            x: 56, y: 240, width: 300, height: 10,
            text: "1000g",
            font: Font.Arial);
    }
}