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
using Type = Core.Globals.Type;

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

    public void UpdateWindow_Editors()
    {
        var window = WindowLoader.FromLayout("winEditors");

        // Close button
        if (WindowManager.TryGetControl("winEditors", "btnClose", out var btnClose))
        {
            btnClose.CallBack[(int)ControlState.MouseDown] = () => WindowManager.HideWindow("winEditors");
        }

        // Footer Close
        if (WindowManager.TryGetControl("winEditors", "btnCloseMap", out var btnCloseMap))
        {
            btnCloseMap.CallBack[(int)ControlState.MouseDown] = () => WindowManager.HideWindow("winEditors");
        }

        // Save: apply Settings values and mirror Editors.UpdateMap flow
        if (WindowManager.TryGetControl("winEditors", "btnSaveMap", out var btnSave))
        {
            btnSave.CallBack[(int)ControlState.MouseDown] = () =>
            {
                // Helper readers
                static int ReadIntSafe(Control c, int min, int max, int fallback = 0)
                {
                    var t = c.Text?.Trim();
                    if (!int.TryParse(t, out var n)) n = fallback;
                    if (n < min) n = min; if (n > max) n = max;
                    return n;
                }
                int maxMaps = Variables.MaxMaps;
                // Name & Music & Shop & Moral
                if (WindowManager.TryGetControl("winEditors", "txtName", out var txtNameCtrl))
                    Data.MyMap.Name = txtNameCtrl.Text?.Trim() ?? string.Empty;
                if (WindowManager.TryGetControl("winEditors", "cmbMusic", out var musicCtrl) && musicCtrl is ComboBox cmbMusic)
                {
                    var idx = Math.Clamp(cmbMusic.Value, 0, cmbMusic.Items.Count - 1);
                    Data.MyMap.Music = idx <= 0 ? string.Empty : cmbMusic.Items[idx];
                }
                if (WindowManager.TryGetControl("winEditors", "lstShop", out var shopCtrl) && shopCtrl is ComboBox lstShop)
                    Data.MyMap.Shop = lstShop.Value <= 0 ? -1 : lstShop.Value - 1;
                if (WindowManager.TryGetControl("winEditors", "lstMoral", out var moralCtrl) && moralCtrl is ComboBox lstMoral)
                    Data.MyMap.Moral = (byte)Math.Clamp(lstMoral.Value, 0, Variables.MaxMorals - 1);

                // Links
                if (WindowManager.TryGetControl("winEditors", "txtUp", out var txtUp)) Data.MyMap.Up = (short)ReadIntSafe(txtUp, 0, maxMaps, 0);
                if (WindowManager.TryGetControl("winEditors", "txtDown", out var txtDown)) Data.MyMap.Down = (short)ReadIntSafe(txtDown, 0, maxMaps, 0);
                if (WindowManager.TryGetControl("winEditors", "txtLeft", out var txtLeft)) Data.MyMap.Left = (short)ReadIntSafe(txtLeft, 0, maxMaps, 0);
                if (WindowManager.TryGetControl("winEditors", "txtRight", out var txtRight)) Data.MyMap.Right = (short)ReadIntSafe(txtRight, 0, maxMaps, 0);

                // Boot
                if (WindowManager.TryGetControl("winEditors", "txtBootMap", out var txtBootMap)) Data.MyMap.BootMap = (short)ReadIntSafe(txtBootMap, 0, maxMaps, 0);
                if (WindowManager.TryGetControl("winEditors", "txtBootX", out var txtBootX)) Data.MyMap.BootX = (byte)ReadIntSafe(txtBootX, 0, Math.Max((byte)0, Data.MyMap.MaxX), 0);
                if (WindowManager.TryGetControl("winEditors", "txtBootY", out var txtBootY)) Data.MyMap.BootY = (byte)ReadIntSafe(txtBootY, 0, Math.Max((byte)0, Data.MyMap.MaxY), 0);

                // Flags
                if (WindowManager.TryGetControl("winEditors", "chkNoMapRespawn", out var chkNoMapRespawn))
                    Data.MyMap.NoRespawn = chkNoMapRespawn.Value == 1;
                if (WindowManager.TryGetControl("winEditors", "chkIndoors", out var chkIndoors))
                    Data.MyMap.Indoors = chkIndoors.Value == 1;

                // Resize map (mirror Editors.UpdateMap)
                var tempArr = (Type.Tile[,])Data.MyMap.Tile.Clone();
                int prevMaxX = Data.MyMap.MaxX;
                int prevMaxY = Data.MyMap.MaxY;

                if (WindowManager.TryGetControl("winEditors", "txtMaxX", out var txtMaxX))
                    Data.MyMap.MaxX = (byte)ReadIntSafe(txtMaxX, 1, Variables.MaxMapX, Data.MyMap.MaxX);
                if (WindowManager.TryGetControl("winEditors", "txtMaxY", out var txtMaxY))
                    Data.MyMap.MaxY = (byte)ReadIntSafe(txtMaxY, 1, Variables.MaxMapY, Data.MyMap.MaxY);

                Data.MyMap.Tile = new Type.Tile[(Data.MyMap.MaxX), (Data.MyMap.MaxY)];
                for (int i = 0; i < GameState.MaxTileHistory; i++)
                {
                    if (Data.TileHistory![i].Tile == null)
                        Data.TileHistory![i].Tile = new Type.Tile[(Data.MyMap.MaxX), (Data.MyMap.MaxY)];
                    else if (Data.TileHistory![i].Tile.GetLength(0) != Data.MyMap.MaxX || Data.TileHistory![i].Tile.GetLength(1) != Data.MyMap.MaxY)
                        Data.TileHistory![i].Tile = new Type.Tile[(Data.MyMap.MaxX), (Data.MyMap.MaxY)];
                }
                Data.Autotile = new Type.Autotile[(Data.MyMap.MaxX), (Data.MyMap.MaxY)];

                int x2 = prevMaxX > Data.MyMap.MaxX ? Data.MyMap.MaxX : prevMaxX;
                int y2 = prevMaxY > Data.MyMap.MaxY ? Data.MyMap.MaxY : prevMaxY;
                int layerCount = System.Enum.GetValues(typeof(MapLayer)).Length;
                for (int x = 0; x < Data.MyMap.MaxX; x++)
                {
                    for (int y = 0; y < Data.MyMap.MaxY; y++)
                    {
                        Data.MyMap.Tile[x, y].Layer = new Type.Layer[layerCount];
                        Data.Autotile[x, y].Layer = new Type.QuarterTile[layerCount];
                        for (int i = 0; i < GameState.MaxTileHistory; i++)
                        {
                            if (Data.TileHistory![i].Tile?[x, y].Layer == null || Data.TileHistory![i].Tile[x, y].Layer.Length != layerCount)
                                Data.TileHistory![i].Tile![x, y].Layer = new Type.Layer[layerCount];
                        }
                        if (x < x2 && y < y2)
                        {
                            Data.MyMap.Tile[x, y] = tempArr[x, y];
                        }
                    }
                }

                // Send map and close
                Editors.MapEditorSend();
                WindowManager.HideWindow("winEditors");
            };
        }

        // Discard: cancel map edit and close
        if (WindowManager.TryGetControl("winEditors", "btnDiscard", out var btnDiscard))
        {
            btnDiscard.CallBack[(int)ControlState.MouseDown] = () => { Editors.MapEditorCancel(); WindowManager.HideWindow("winEditors"); };
        }

        // Layer buttons: update current layer in GameState
        void BindLayer(string ctrl, int layer)
        {
            if (WindowManager.TryGetControl("winEditors", ctrl, out var c))
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
        if (WindowManager.TryGetControl("winEditors", "btnToolPencil", out var btnPencil))
            btnPencil.CallBack[(int)ControlState.MouseDown] = () => { GameState.EyeDropper = false; };
        
        if (WindowManager.TryGetControl("winEditors", "btnToolFill", out var btnFill))
            btnFill.CallBack[(int)ControlState.MouseDown] = () =>
            {
                // Contextual fill: Attributes -> fill attributes (confirm), Directions -> not applicable, otherwise fill current tiles layer
                if (GameState.MapEditorTab == (int)MapEditorTab.Attributes)
                {
                    GameLogic.Dialogue("Map Editor", "Fill Attributes", "Are you sure you wish to fill attributes?", DialogueType.FillAttributes, DialogueStyle.YesNo);
                }
                else if (GameState.MapEditorTab == (int)MapEditorTab.Directions)
                {
                    TextRenderer.AddText("Fill not available", (int)ColorName.BrightRed);
                }
                else
                {
                    WinEditors.OnFillLayerClick();
                }
            };
        if (WindowManager.TryGetControl("winEditors", "btnToolEraser", out var btnErase))
            btnErase.CallBack[(int)ControlState.MouseDown] = () =>
            {
                // Contextual clear: Directions -> clear dir blocks (confirm), Attributes -> clear attributes (confirm), otherwise clear current tiles layer
                if (GameState.MapEditorTab == (int)MapEditorTab.Directions)
                {
                    WinEditors.OnDirClearClick();
                }
                else if (GameState.MapEditorTab == (int)MapEditorTab.Attributes)
                {
                    GameLogic.Dialogue("Map Editor", "Clear Attributes", "Are you sure you wish to clear attributes?", DialogueType.ClearAttributes, DialogueStyle.YesNo);
                }
                else
                {
                    Editors.MapEditorClearLayer((MapLayer)GameState.CurLayer);
                }
            };

        // Toolbar buttons
        if (WindowManager.TryGetControl("winEditors", "btnGrid", out var btnGrid))
            btnGrid.CallBack[(int)ControlState.MouseDown] = () => { GameState.MapGrid = !GameState.MapGrid; };
        if (WindowManager.TryGetControl("winEditors", "btnEyeDropper", out var btnEye))
            btnEye.CallBack[(int)ControlState.MouseDown] = () => { GameState.EyeDropper = !GameState.EyeDropper; };
        if (WindowManager.TryGetControl("winEditors", "btnUndo", out var btnUndo))
            btnUndo.CallBack[(int)ControlState.MouseDown] = () => { Editors.Undo(); };
        if (WindowManager.TryGetControl("winEditors", "btnRedo", out var btnRedo))
            btnRedo.CallBack[(int)ControlState.MouseDown] = () => { Editors.Redo(); };

        // Quick actions: call into existing helpers if available
        if (WindowManager.TryGetControl("winEditors", "btnFillLayer", out var btnFillLayer))
            btnFillLayer.CallBack[(int)ControlState.MouseDown] = () => { WinEditors.OnFillLayerClick(); };
        if (WindowManager.TryGetControl("winEditors", "btnClearLayer", out var btnClearLayer))
            btnClearLayer.CallBack[(int)ControlState.MouseDown] = () => { Editors.MapEditorClearLayer((MapLayer)GameState.CurLayer); };
        if (WindowManager.TryGetControl("winEditors", "btnCopyMap", out var btnCopy))
            btnCopy.CallBack[(int)ControlState.MouseDown] = () => { Editors.MapEditorCopyMap(); };
        if (WindowManager.TryGetControl("winEditors", "btnPasteMap", out var btnPaste))
            btnPaste.CallBack[(int)ControlState.MouseDown] = () => { Editors.MapEditorPasteMap(); };
        if (WindowManager.TryGetControl("winEditors", "btnDeleteMap", out var btnDeleteMap))
            btnDeleteMap.CallBack[(int)ControlState.MouseDown] = () =>
            {
                GameLogic.Dialogue("Map Editor", "Delete Map: ", "Are you sure you want to clear this map?", DialogueType.DeleteMap, DialogueStyle.YesNo);
            };

        // Tileset selector wiring
        string[] autotileNames = new[]{"None","Autotile","Fake Autotile","Animated","Cliff","Waterfall"};

        // Populate Layer and Autotile combos and set defaults
        if (WindowManager.TryGetControl("winEditors", "cmbLayer", out var cmbLayerCtrl) && cmbLayerCtrl is ComboBox cmbLayer)
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
        
        if (WindowManager.TryGetControl("winEditors", "cmbAutotile", out var cmbAutoCtrl) && cmbAutoCtrl is ComboBox cmbAuto)
        {
            cmbAuto.Items.Clear();
            foreach (var n in autotileNames) cmbAuto.Items.Add(n);
            cmbAuto.Value = Math.Clamp(GameState.CurAutotileType, 0, autotileNames.Length - 1);
            void ApplyAutotileSelectionSize()
            {
                int t = Math.Clamp(GameState.CurAutotileType, 0, autotileNames.Length - 1);
                switch (t)
                {
                    case 1: GameState.EditorTileWidth = 2; GameState.EditorTileHeight = 3; break; // autotile
                    case 2: GameState.EditorTileWidth = 1; GameState.EditorTileHeight = 1; break; // fake autotile
                    case 3: GameState.EditorTileWidth = 6; GameState.EditorTileHeight = 3; break; // animated
                    case 4: GameState.EditorTileWidth = 2; GameState.EditorTileHeight = 2; break; // cliff
                    case 5: GameState.EditorTileWidth = 2; GameState.EditorTileHeight = 3; break; // waterfall
                    default: GameState.EditorTileWidth = 1; GameState.EditorTileHeight = 1; break; // none
                }
                // Update selection rectangle immediately
                int x = Math.Max(0, GameState.EditorTileX);
                int y = Math.Max(0, GameState.EditorTileY);
                GameState.EditorTileSelStart = new Microsoft.Xna.Framework.Point(x, y);
                GameState.EditorTileSelEnd = new Microsoft.Xna.Framework.Point(x + GameState.EditorTileWidth, y + GameState.EditorTileHeight);
            }
            ApplyAutotileSelectionSize();
            cmbAuto.CallBack[(int)ControlState.MouseMove] = () =>
            {
                GameState.CurAutotileType = (cmbAuto.Value >= 0 && cmbAuto.Value < autotileNames.Length) ? cmbAuto.Value : 0;
                ApplyAutotileSelectionSize();
            };
        }

        // Attributes: mode combo (maps to GameState Opt* flags) and actions
        string[] attrModes = new[] { "Blocked", "Warp", "Item", "Npc Avoid", "Resource", "Npc Spawn", "Shop", "Bank", "Heal", "Trap", "Animation", "No Crossing" };

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
            GameState.OptInfo = false; // Info handled via dedicated button
        }

        void UpdateAttrVisibility(int idx)
        {
            // Show only relevant attribute configuration controls per selection (mirror Eto behavior)
            bool showWarp = idx == 1; // Warp
            string[] warpCtrls = new[]{"lblWarp","lblWarpMap","sldMapWarpMap","lblWarpX","sldMapWarpX","lblWarpY","sldMapWarpY","btnMapWarp"};
            foreach (var n in warpCtrls)
            {
                if (WindowManager.TryGetControl("winEditors", n, out var c)) c.Visible = showWarp;
            }

            if (showWarp)
            {
                void SetWarpLabel(string name, string caption, int value)
                {
                    if (WindowManager.TryGetControl("winEditors", name, out var l)) l.Text = $"{caption}: {value}";
                }
                
                if (WindowManager.TryGetControl("winEditors", "sldMapWarpMap", out var wMapCtrl) && wMapCtrl is Client.Game.UI.Controls.ScrollBar sbMap)
                {
                    sbMap.Min = 1;
                    sbMap.Max = Variables.MaxMaps;
                    wMapCtrl.Value = Math.Clamp(wMapCtrl.Value, sbMap.Min, sbMap.Max);
                    if (wMapCtrl.Value < 1) wMapCtrl.Value = 1;
                    SetWarpLabel("lblWarpMap", "Map", wMapCtrl.Value);
                    sbMap.CallBack[(int)ControlState.MouseMove] = () =>
                    {
                        wMapCtrl.Value = Math.Clamp(wMapCtrl.Value, sbMap.Min, sbMap.Max);
                        SetWarpLabel("lblWarpMap", "Map", wMapCtrl.Value);
                    };
                }

                if (WindowManager.TryGetControl("winEditors", "sldMapWarpX", out var wXCtrl) && wXCtrl is Client.Game.UI.Controls.ScrollBar sbX)
                {
                    sbX.Min = 0; sbX.Max = Math.Max(0, Data.MyMap.MaxX - 1);
                    wXCtrl.Value = Math.Clamp(wXCtrl.Value, sbX.Min, sbX.Max);
                    SetWarpLabel("lblWarpX", "X", wXCtrl.Value);
                    sbX.CallBack[(int)ControlState.MouseMove] = () =>
                    {
                        wXCtrl.Value = Math.Clamp(wXCtrl.Value, sbX.Min, sbX.Max);
                        SetWarpLabel("lblWarpX", "X", wXCtrl.Value);
                    };
                }

                if (WindowManager.TryGetControl("winEditors", "sldMapWarpY", out var wYCtrl) && wYCtrl is Client.Game.UI.Controls.ScrollBar sbY)
                {
                    sbY.Min = 0; sbY.Max = Math.Max(0, Data.MyMap.MaxY - 1);
                    wYCtrl.Value = Math.Clamp(wYCtrl.Value, sbY.Min, sbY.Max);
                    SetWarpLabel("lblWarpY", "Y", wYCtrl.Value);
                    sbY.CallBack[(int)ControlState.MouseMove] = () =>
                    {
                        wYCtrl.Value = Math.Clamp(wYCtrl.Value, sbY.Min, sbY.Max);
                        SetWarpLabel("lblWarpY", "Y", wYCtrl.Value);
                    };
                }

                if (WindowManager.TryGetControl("winEditors", "btnMapWarp", out var btnMapWarp))
                {
                    btnMapWarp.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        if (WindowManager.TryGetControl("winEditors", "sldMapWarpMap", out var cMap) &&
                            WindowManager.TryGetControl("winEditors", "sldMapWarpX", out var cX) &&
                            WindowManager.TryGetControl("winEditors", "sldMapWarpY", out var cY))
                        {
                            GameState.EditorWarpMap = cMap.Value;
                            GameState.EditorWarpX = cX.Value;
                            GameState.EditorWarpY = cY.Value;
                        }
                    };
                }
            }

            // Item
            bool showItem = idx == 2;
            string[] itemCtrls = new[]{"lblItem","cmbMapItem","lblItemValue","sldMapItemValue","btnMapItem"};
            foreach (var n in itemCtrls)
            {
                if (WindowManager.TryGetControl("winEditors", n, out var c)) c.Visible = showItem;
            }

            if (showItem)
            {
                if (WindowManager.TryGetControl("winEditors", "cmbMapItem", out var cItem) && cItem is ComboBox cmb)
                {
                    if (cmb.Items.Count == 0)
                    {
                        for (int i = 0; i < Variables.MaxItems; i++)
                        {
                            var name = (i < Data.Item.Length) ? (Data.Item[i].Name ?? string.Empty) : string.Empty;
                            cmb.Items.Add(string.IsNullOrWhiteSpace(name) ? $"{i + 1}" : $"{i + 1}: {name.Trim()}" );
                        }
                        cmb.Value = 0;
                    }
                }
                if (WindowManager.TryGetControl("winEditors", "sldMapItemValue", out var sItemCtrl) && sItemCtrl is Client.Game.UI.Controls.ScrollBar sbItem)
                {
                    sbItem.Min = 1; sbItem.Max = 1024;
                    if (WindowManager.TryGetControl("winEditors", "lblItemValue", out var l)) l.Text = $"Amount: {sItemCtrl.Value}";
                    sbItem.CallBack[(int)ControlState.MouseMove] = () =>
                    {
                        if (WindowManager.TryGetControl("winEditors", "lblItemValue", out var li)) li.Text = $"Amount: {sItemCtrl.Value}";
                    };
                }
                if (WindowManager.TryGetControl("winEditors", "btnMapItem", out var btn))
                {
                    btn.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        if (WindowManager.TryGetControl("winEditors", "cmbMapItem", out var c) && c is ComboBox cb)
                            GameState.ItemEditorNum = Math.Clamp(cb.Value, 0, Variables.MaxItems - 1);
                        if (WindowManager.TryGetControl("winEditors", "sldMapItemValue", out var s))
                            GameState.ItemEditorValue = Math.Clamp(s.Value, 1, 1024);
                    };
                }
            }

            // Resource
            bool showResource = idx == 4;
            string[] resCtrls = new[]{"lblResource","cmbResource","btnResourceOk"};
            foreach (var n in resCtrls)
            {
                if (WindowManager.TryGetControl("winEditors", n, out var c)) c.Visible = showResource;
            }
            if (showResource)
            {
                if (WindowManager.TryGetControl("winEditors", "cmbResource", out var cRes) && cRes is ComboBox cmb)
                {
                    if (cmb.Items.Count == 0)
                    {
                        for (int i = 0; i < Variables.MaxResources; i++)
                        {
                            var raw = (i < Data.Resource.Length) ? (Data.Resource[i].Name ?? string.Empty) : string.Empty;
                            var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                            cmb.Items.Add($"{i + 1}: {name}");
                        }
                        cmb.Value = 0;
                    }
                    cmb.CallBack[(int)ControlState.MouseMove] = () => { GameState.ResourceEditorNum = Math.Clamp(cmb.Value, 0, Variables.MaxResources - 1); };
                }
                if (WindowManager.TryGetControl("winEditors", "btnResourceOk", out var btn))
                {
                    btn.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        if (WindowManager.TryGetControl("winEditors", "cmbResource", out var c) && c is ComboBox cb)
                            GameState.ResourceEditorNum = Math.Clamp(cb.Value, 0, Variables.MaxResources - 1);
                    };
                }
            }

            // NPC Spawn
            bool showSpawn = idx == 5;
            string[] spawnCtrls = new[]{"lblNpcSpawn","lblNpcSpawnSlot","cmbNpcSpawnSlot","lblNpcDir","sldNpcDir","btnNpcSpawn"};
            foreach (var n in spawnCtrls)
            {
                if (WindowManager.TryGetControl("winEditors", n, out var c)) c.Visible = showSpawn;
            }

            if (showSpawn)
            {
                // Populate spawn slot combo from current map NPC slots
                if (WindowManager.TryGetControl("winEditors", "cmbNpcSpawnSlot", out var cSpawn) && cSpawn is ComboBox cmb)
                {
                    cmb.Items.Clear();
                    cmb.Items.Add("None");
                    if (Data.MyMap.Npc != null && Data.Npc != null)
                    {
                        int max = Math.Min(Variables.MaxMapNpcs, Data.MyMap.Npc.Length);
                        for (int slot = 1; slot < max; slot++)
                        {
                            int npcIndex = Data.MyMap.Npc[slot];
                            string name = (npcIndex >= 0 && npcIndex < Variables.MaxNpcs && npcIndex < Data.Npc.Length) ? (Data.Npc[npcIndex].Name ?? string.Empty).Trim() : "None";
                            cmb.Items.Add($"{slot}: {name}");
                        }
                    }
                    cmb.Value = 0;
                }
                if (WindowManager.TryGetControl("winEditors", "sldNpcDir", out var sDirCtrl) && sDirCtrl is Client.Game.UI.Controls.ScrollBar sbDir)
                {
                    sbDir.Min = 0; sbDir.Max = 3;
                    Action updateDir = () =>
                    {
                        string text = sDirCtrl.Value switch { 0 => "Up", 1 => "Down", 2 => "Left", 3 => "Right", _ => "Up" };
                        if (WindowManager.TryGetControl("winEditors", "lblNpcDir", out var l)) l.Text = $"Direction: {text}";
                    };
                    updateDir();
                    sbDir.CallBack[(int)ControlState.MouseMove] = () => updateDir();
                }
                if (WindowManager.TryGetControl("winEditors", "btnNpcSpawn", out var btn))
                {
                    btn.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        int slot = 0;
                        if (WindowManager.TryGetControl("winEditors", "cmbNpcSpawnSlot", out var c) && c is ComboBox cb)
                            slot = Math.Max(0, cb.Value); // index maps to slot (0=None, 1..)
                        if (WindowManager.TryGetControl("winEditors", "sldNpcDir", out var s))
                            GameState.SpawnNpcDir = Math.Clamp(s.Value, 0, 3);
                        GameState.SpawnNpcNum = slot;
                    };
                }
            }

            // Shop
            bool showShop = idx == 6;
            string[] shopCtrls = new[]{"lblShopAttr","cmbShopAttr","btnShop"};
            foreach (var n in shopCtrls)
            {
                if (WindowManager.TryGetControl("winEditors", n, out var c)) c.Visible = showShop;
            }
            if (showShop)
            {
                if (WindowManager.TryGetControl("winEditors", "cmbShopAttr", out var cShop) && cShop is ComboBox cmb)
                {
                    if (cmb.Items.Count == 0)
                    {
                        for (int i = 0; i < Variables.MaxShops; i++)
                        {
                            var raw = (i < Data.Shop.Length) ? (Data.Shop[i].Name ?? string.Empty) : string.Empty;
                            var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                            cmb.Items.Add($"{i + 1}: {name}");
                        }
                        cmb.Value = 0;
                    }
                }
                if (WindowManager.TryGetControl("winEditors", "btnShop", out var btn))
                {
                    btn.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        if (WindowManager.TryGetControl("winEditors", "cmbShopAttr", out var c) && c is ComboBox cb)
                            GameState.EditorShop = Math.Clamp(cb.Value, 0, Variables.MaxShops - 1);
                    };
                }
            }

            // Heal
            bool showHeal = idx == 8;
            string[] healCtrls = new[]{"lblHeal","cmbHeal","lblHealAmount","sldHeal","btnHeal"};
            foreach (var n in healCtrls)
            {
                if (WindowManager.TryGetControl("winEditors", n, out var c)) c.Visible = showHeal;
            }
            if (showHeal)
            {
                if (WindowManager.TryGetControl("winEditors", "cmbHeal", out var cHeal) && cHeal is ComboBox cmb)
                {
                    if (cmb.Items.Count == 0)
                    {
                        cmb.Items.Add("Hp");
                        cmb.Items.Add("Mp");
                        cmb.Items.Add("Sp");
                        cmb.Value = 0;
                    }
                }
                if (WindowManager.TryGetControl("winEditors", "sldHeal", out var sHealCtrl) && sHealCtrl is Client.Game.UI.Controls.ScrollBar sbHeal)
                {
                    sbHeal.Min = 1; sbHeal.Max = 1024;
                    if (WindowManager.TryGetControl("winEditors", "lblHealAmount", out var l)) l.Text = $"Amount: {sHealCtrl.Value}";
                    sbHeal.CallBack[(int)ControlState.MouseMove] = () =>
                    {
                        if (WindowManager.TryGetControl("winEditors", "lblHealAmount", out var li)) li.Text = $"Amount: {sHealCtrl.Value}";
                    };
                }
                if (WindowManager.TryGetControl("winEditors", "btnHeal", out var btn))
                {
                    btn.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        if (WindowManager.TryGetControl("winEditors", "cmbHeal", out var c) && c is ComboBox cb)
                            GameState.MapEditorHealType = Math.Clamp(cb.Value, 0, 2);
                        if (WindowManager.TryGetControl("winEditors", "sldHeal", out var s))
                            GameState.MapEditorHealAmount = Math.Clamp(s.Value, 1, 1024);
                    };
                }
            }

            // Trap
            bool showTrap = idx == 9;
            string[] trapCtrls = new[]{"lblTrap","lblTrapVital","cmbTrapVital","lblTrapAmount","sldTrap","btnTrap"};
            foreach (var n in trapCtrls)
            {
                if (WindowManager.TryGetControl("winEditors", n, out var c)) c.Visible = showTrap;
            }
            if (showTrap)
            {
                if (WindowManager.TryGetControl("winEditors", "cmbTrapVital", out var cTrap) && cTrap is ComboBox cmb)
                {
                    if (cmb.Items.Count == 0)
                    {
                        cmb.Items.Add("Hp");
                        cmb.Items.Add("Mp");
                        cmb.Items.Add("Sp");
                        cmb.Value = 0;
                    }
                }
                if (WindowManager.TryGetControl("winEditors", "sldTrap", out var sTrapCtrl) && sTrapCtrl is Client.Game.UI.Controls.ScrollBar sbTrap)
                {
                    sbTrap.Min = 1; sbTrap.Max = 1024;
                    if (WindowManager.TryGetControl("winEditors", "lblTrapAmount", out var l)) l.Text = $"Amount: {sTrapCtrl.Value}";
                    sbTrap.CallBack[(int)ControlState.MouseMove] = () =>
                    {
                        if (WindowManager.TryGetControl("winEditors", "lblTrapAmount", out var li)) li.Text = $"Amount: {sTrapCtrl.Value}";
                    };
                }
                if (WindowManager.TryGetControl("winEditors", "btnTrap", out var btn))
                {
                    btn.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        if (WindowManager.TryGetControl("winEditors", "sldTrap", out var s))
                            GameState.MapEditorHealAmount = Math.Clamp(s.Value, 1, 1024);
                        if (WindowManager.TryGetControl("winEditors", "cmbTrapVital", out var c) && c is ComboBox cb)
                            GameState.MapEditorTrapVital = Math.Clamp(cb.Value, 0, 2);
                    };
                }
            }

            // Animation
            bool showAnimation = idx == 10;
            string[] animCtrls = new[]{"lblAnimation","cmbAnimation","btnAnimation"};
            foreach (var n in animCtrls)
            {
                if (WindowManager.TryGetControl("winEditors", n, out var c)) c.Visible = showAnimation;
            }
            if (showAnimation)
            {
                if (WindowManager.TryGetControl("winEditors", "cmbAnimation", out var cAnim) && cAnim is ComboBox cmb)
                {
                    if (cmb.Items.Count == 0)
                    {
                        for (int i = 0; i < Variables.MaxAnimations; i++)
                        {
                            var raw = (i < Data.Animation.Length) ? (Data.Animation[i].Name ?? string.Empty) : string.Empty;
                            var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                            cmb.Items.Add($"{i + 1}: {name}");
                        }
                        cmb.Value = 0;
                    }
                }
                if (WindowManager.TryGetControl("winEditors", "btnAnimation", out var btn))
                {
                    btn.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        if (WindowManager.TryGetControl("winEditors", "cmbAnimation", out var c) && c is ComboBox cb)
                            GameState.EditorAnimation = Math.Clamp(cb.Value, 0, Variables.MaxAnimations - 1);
                    };
                }
            }
        }

        if (WindowManager.TryGetControl("winEditors", "cmbAttrMode", out var cmbAttrCtrl) && cmbAttrCtrl is ComboBox cmbAttr)
        {
            cmbAttr.Items.Clear();
            foreach (var n in attrModes) cmbAttr.Items.Add(n);
            cmbAttr.Value = 0; // default to Blocked
            SetAttrFlags(0);
            UpdateAttrVisibility(0);
            // Immediate apply on selection change
            cmbAttr.CallBack[(int)ControlState.MouseMove] = () =>
            {
                var idx = Math.Clamp(cmbAttr.Value, 0, attrModes.Length - 1);
                SetAttrFlags(idx);
                UpdateAttrVisibility(idx);
            };
        }

        // Separate Info button wiring
        if (WindowManager.TryGetControl("winEditors", "btnAttrInfo", out var btnAttrInfo))
        {
            btnAttrInfo.CallBack[(int)ControlState.MouseDown] = () =>
            {
                ClearAttrFlags();
                GameState.OptInfo = true;
                // Hide configurable groups when Info mode is active
                UpdateAttrVisibility(-1);
            };
        }

        // Attribute layer (1 or 2) builder
        if (WindowManager.TryGetControl("winEditors", "cmbAttribute", out var cmbAttributeCtrl) && cmbAttributeCtrl is ComboBox cmbAttribute)
        {
            cmbAttribute.Items.Clear();
            cmbAttribute.Items.Add("1");
            cmbAttribute.Items.Add("2");
            var currentLayer = GameState.EditorAttribute;
            if (currentLayer < 1 || currentLayer > 2) currentLayer = 1;
            cmbAttribute.Value = currentLayer - 1; // zero-based
            cmbAttribute.CallBack[(int)ControlState.MouseMove] = () =>
            {
                GameState.EditorAttribute = (byte)Math.Clamp(cmbAttribute.Value + 1, 1, 2);
            };
        }
        
        void UpdateAutotileLabel() { }
        // Initialize labels on open
        if (GameState.CurTileset <= 0) GameState.CurTileset = Math.Max(1, Data.MyMap.Tileset);
        if (Data.MyMap.Tileset <= 0) Data.MyMap.Tileset = GameState.CurTileset;
        UpdateAutotileLabel();

        // Horizontal tileset scrollbar selects the tileset number
        if (WindowManager.TryGetControl("winEditors", "sldTileset", out var sldTilesetCtrl) && sldTilesetCtrl is Client.Game.UI.Controls.ScrollBar sldTileset)
        {
            sldTileset.Min = 1;
            sldTileset.Max = Math.Max(1, GameState.NumTileSets);
            sldTilesetCtrl.Value = Math.Clamp(GameState.CurTileset, sldTileset.Min, sldTileset.Max);
            sldTileset.CallBack[(int)ControlState.MouseMove] = () =>
            {
                GameState.CurTileset = Math.Clamp(sldTilesetCtrl.Value, sldTileset.Min, sldTileset.Max);
                Data.MyMap.Tileset = GameState.CurTileset;
            };
        }

        // Horizontal viewport scroll (for wide tilesets)
        if (WindowManager.TryGetControl("winEditors", "sldTilesetH", out var sldTilesetHCtrl) && sldTilesetHCtrl is Client.Game.UI.Controls.ScrollBar sldTilesetH)
        {
            // Range is updated during drawing; still add a callback to trigger redraw behavior on change
            sldTilesetH.CallBack[(int)ControlState.MouseMove] = () => { /* no-op: OnDraw reads Value */ };
        }

        if (WindowManager.TryGetControl("winEditors", "btnTilesetPrev", out var btnTsPrev))
            btnTsPrev.CallBack[(int)ControlState.MouseDown] = () => { GameState.CurTileset = Math.Max(1, GameState.CurTileset - 1); Data.MyMap.Tileset = GameState.CurTileset; };
        if (WindowManager.TryGetControl("winEditors", "btnTilesetNext", out var btnTsNext))
            btnTsNext.CallBack[(int)ControlState.MouseDown] = () => { var maxTs = Math.Max(1, GameState.NumTileSets); GameState.CurTileset = Math.Min(maxTs, GameState.CurTileset + 1); Data.MyMap.Tileset = GameState.CurTileset; };

        if (WindowManager.TryGetControl("winEditors", "btnAutoPrev", out var btnAutoPrev))
            btnAutoPrev.CallBack[(int)ControlState.MouseDown] = () => { GameState.CurAutotileType = (GameState.CurAutotileType + autotileNames.Length - 1) % autotileNames.Length; UpdateAutotileLabel(); };
        if (WindowManager.TryGetControl("winEditors", "btnAutoNext", out var btnAutoNext))
            btnAutoNext.CallBack[(int)ControlState.MouseDown] = () => { GameState.CurAutotileType = (GameState.CurAutotileType + 1) % autotileNames.Length; UpdateAutotileLabel(); };

        if (WindowManager.TryGetControl("winEditors", "btnTileApply", out var btnTileApply))
            btnTileApply.CallBack[(int)ControlState.MouseDown] = () =>
            {
                if (WindowManager.TryGetControl("winEditors", "txtTileX", out var tbx) && WindowManager.TryGetControl("winEditors", "txtTileY", out var tby))
                {
                    int x = int.TryParse(tbx.Text?.Trim(), out var ix) ? ix : 0;
                    int y = int.TryParse(tby.Text?.Trim(), out var iy) ? iy : 0;
                    x = Math.Max(0, x); y = Math.Max(0, y);
                    GameState.EditorTileX = x; GameState.EditorTileY = y;
                    Editors.MapEditorChooseTile(x * GameState.SizeX, y * GameState.SizeY);
                }
            };

         // Faux header tabs like Admin
        var winIndex = WindowManager.GetWindowIndex("winEditors");
       
        void SetVisible(bool visible, params string[] names)
        {
            foreach (var n in names)
            {
                if (WindowManager.TryGetControl("winEditors", n, out var c)) c.Visible = visible;
            }
        }

        void ShowTab(string tab)
        {
            var tools = new[]{
                "picTilesBG",
                "sldTileset","lblTileset",
                "cmbLayer",
                "cmbAutotile",
                "picTileset","sldTilesetV","sldTilesetH"
            };

            var attrs = new[]{
                "picAttrBG","lblAttributes","cmbAttrMode","btnAttrInfo",
                "lblAttrLayer","cmbAttribute",
                // Warp
                "lblWarp","lblWarpMap","sldMapWarpMap","lblWarpX","sldMapWarpX","lblWarpY","sldMapWarpY","btnMapWarp",
                // Item
                "lblItem","cmbMapItem","lblItemValue","sldMapItemValue","btnMapItem",
                // Resource
                "lblResource","cmbResource","btnResourceOk",
                // NPC Spawn
                "lblNpcSpawn","lblNpcSpawnSlot","cmbNpcSpawnSlot","lblNpcDir","sldNpcDir","btnNpcSpawn",
                // Shop
                "lblShopAttr","cmbShopAttr","btnShop",
                // Heal
                "lblHeal","cmbHeal","lblHealAmount","sldHeal","btnHeal",
                // Trap
                "lblTrap","lblTrapVital","cmbTrapVital","lblTrapAmount","sldTrap","btnTrap",
                // Animation
                "lblAnimation","cmbAnimation","btnAnimation"
            };

            var npcs = new[]{
                "picNpcsBG","lblNpcs","lblNpcsHint",
                // Left list
                "lstNpcs","sldNpcList",
                // Right selection
                "lblNpc","cmbNpcList"
            };

            var settings = new[]{
                // Parchment and header
                "picNpcsBG","lblSettings",
                // Section nav
                "btnGoTiles","btnGoAttributes","btnGoNpcs","btnGoDirBlock","btnGoEvents","btnGoEffects",
                // Core fields
                "lblMapName","txtName","lblMoral","lstMoral","lblShop","lstShop",
                "lblMusic","cmbMusic","btnMusicPreview",
                // Links
                "lblLinks","lblLinkUp","txtUp","lblLinkDown","txtDown","lblLinkLeft","txtLeft","lblLinkRight","txtRight",
                // Boot
                "lblBoot","lblBootMap","txtBootMap","lblBootX","txtBootX","lblBootY","txtBootY",
                // Flags
                "chkNoMapRespawn","chkIndoors",
                // Sizes
                "lblMaxX","txtMaxX","lblMaxY","txtMaxY"
            };

            var dirblock = new[]{
                "picDirBG","lblDir"
            };

            var eventsTab = new[]{
                "picEventsBG","lblEvents",
                "lblCopyCaption","lblCopyMode","btnCopyEvent",
                "lblPasteCaption","lblPasteMode","btnPasteEvent"
            };

            var effects = new[]{
                "picEffectsBG","lblEffects",
                // Weather
                "lblWeather","cmbWeather","lblIntensity","sldIntensity",
                // Fog
                "lblFog","sldFog","lblFogOpacity","sldFogOpacity","lblFogSpeed","sldFogSpeed",
                // Tint
                "chkTint","lblTintR","sldMapRed","lblTintG","sldMapGreen","lblTintB","sldMapBlue","lblTintA","sldMapAlpha",
                // Panorama/Parallax/Brightness
                "lblPanorama","cmbPanorama","lblParallax","cmbParallax","lblBrightness","sldMapBrightness"
            };

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
                    // Ensure attribute group visibility matches current mode when tab opens
                    if (WindowManager.TryGetControl("winEditors", "cmbAttrMode", out var attrModeCtrl) && attrModeCtrl is ComboBox attrCmb)
                    {
                        var idx = Math.Clamp(attrCmb.Value, 0, 12);
                        UpdateAttrVisibility(idx);
                    }
                    break;
                case "Npcs":
                    SetVisible(true, npcs);
                    InitNpcList();
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

        // Wire toolbar Settings to open Settings section
        if (WindowManager.TryGetControl("winEditors", "btnToolbarSettings", out var btnToolbarSettings))
        {
            btnToolbarSettings.CallBack[(int)ControlState.MouseDown] = () => ShowTab("Settings");
        }

        // Initialize Settings controls (Name, Moral, Shop, Music, flags)
        void InitSettingsLists()
        {
            // Name
            if (WindowManager.TryGetControl("winEditors", "txtName", out var txtName))
            {
                txtName.Text = Data.MyMap.Name?.Trim() ?? string.Empty;
            }
            // Moral list
            if (WindowManager.TryGetControl("winEditors", "lstMoral", out var moralCtrl) && moralCtrl is ComboBox lstMoral)
            {
                lstMoral.Items.Clear();
                for (int i = 0; i < Variables.MaxMorals; i++)
                {
                    var raw = (i < Data.Moral.Length) ? (Data.Moral[i].Name ?? string.Empty) : string.Empty;
                    var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                    lstMoral.Items.Add($"{i + 1}: {name}");
                }
                lstMoral.Value = Math.Clamp(Data.MyMap.Moral, 0, lstMoral.Items.Count - 1);
                lstMoral.CallBack[(int)ControlState.MouseMove] = () =>
                {
                    Data.MyMap.Moral = (byte)Math.Clamp(lstMoral.Value, 0, Variables.MaxMorals - 1);
                };
            }

            // Shop list: index 0 = None, then shops 0..MaxShops-1 shifted by +1, display "index: Name" with None fallback
            if (WindowManager.TryGetControl("winEditors", "lstShop", out var shopCtrl) && shopCtrl is ComboBox lstShop)
            {
                lstShop.Items.Clear();
                lstShop.Items.Add("None");
                for (int i = 0; i < Variables.MaxShops; i++)
                {
                    var raw = (i < Data.Shop.Length) ? (Data.Shop[i].Name ?? string.Empty) : string.Empty;
                    var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                    lstShop.Items.Add($"{i + 1}: {name}");
                }
                var shopIndex = Data.MyMap.Shop >= 0 ? Data.MyMap.Shop + 1 : 0;
                lstShop.Value = Math.Clamp(shopIndex, 0, lstShop.Items.Count - 1);
                lstShop.CallBack[(int)ControlState.MouseMove] = () =>
                {
                    Data.MyMap.Shop = lstShop.Value <= 0 ? -1 : lstShop.Value - 1;
                };
            }

            // Music list: index 0 = None, then cache as "index: name"
            if (WindowManager.TryGetControl("winEditors", "cmbMusic", out var musicCtrl) && musicCtrl is ComboBox cmbMusic)
            {
                cmbMusic.Items.Clear();
                cmbMusic.Items.Add("None");
                General.CacheMusic();
                for (int i = 0; i < Sound.MusicCache.Length; i++)
                {
                    var name = Sound.MusicCache[i] ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name)) cmbMusic.Items.Add($"{i + 1}: {name}");
                }
                // Select current map music if present
                int found = 0;
                if (!string.IsNullOrEmpty(Data.MyMap.Music))
                {
                    for (int i = 0; i < cmbMusic.Items.Count; i++)
                    {
                        var display = cmbMusic.Items[i];
                        var sep = display.IndexOf(": ", StringComparison.Ordinal);
                        var justName = sep >= 0 ? display.Substring(sep + 2) : display;
                        if (string.Equals(justName, Data.MyMap.Music, StringComparison.OrdinalIgnoreCase))
                        {
                            found = i; break;
                        }
                    }
                }
                cmbMusic.Value = Math.Clamp(found, 0, cmbMusic.Items.Count - 1);
                cmbMusic.CallBack[(int)ControlState.MouseMove] = () =>
                {
                    var idx = Math.Clamp(cmbMusic.Value, 0, cmbMusic.Items.Count - 1);
                    if (idx <= 0)
                    {
                        Data.MyMap.Music = string.Empty;
                    }
                    else
                    {
                        var display = cmbMusic.Items[idx];
                        var sep = display.IndexOf(": ", StringComparison.Ordinal);
                        Data.MyMap.Music = sep >= 0 ? display.Substring(sep + 2) : display;
                    }
                };

                if (WindowManager.TryGetControl("winEditors", "btnMusicPreview", out var btnMusicPreview))
                {
                    btnMusicPreview.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        var idx = Math.Clamp(cmbMusic.Value, 0, cmbMusic.Items.Count - 1);
                        string file = string.Empty;
                        if (idx > 0)
                        {
                            var display = cmbMusic.Items[idx];
                            var sep = display.IndexOf(": ", StringComparison.Ordinal);
                            file = sep >= 0 ? display.Substring(sep + 2) : display;
                        }
                        if (string.IsNullOrWhiteSpace(file)) return;
                        var ext = System.IO.Path.GetExtension(file)?.ToLowerInvariant();
                        if (ext == ".mid")
                        {
                            Sound.PlayMidi(System.IO.Path.Combine(DataPath.Music, file));
                        }
                        else
                        {
                            Sound.PlayMusic(file);
                        }
                    };
                }
            }

            // Settings checkboxes
            if (WindowManager.TryGetControl("winEditors", "chkNoMapRespawn", out var chkNoMapRespawn))
            {
                chkNoMapRespawn.Value = Data.MyMap.NoRespawn ? 1 : 0;
                chkNoMapRespawn.CallBack[(int)ControlState.MouseDown] = () =>
                {
                    chkNoMapRespawn.Value = chkNoMapRespawn.Value == 0 ? 1 : 0;
                    Data.MyMap.NoRespawn = chkNoMapRespawn.Value == 1;
                };
            }
            if (WindowManager.TryGetControl("winEditors", "chkIndoors", out var chkIndoors))
            {
                chkIndoors.Value = Data.MyMap.Indoors ? 1 : 0;
                chkIndoors.CallBack[(int)ControlState.MouseDown] = () =>
                {
                    chkIndoors.Value = chkIndoors.Value == 0 ? 1 : 0;
                    Data.MyMap.Indoors = chkIndoors.Value == 1;
                };
            }

            // Links / Boot / Sizes textboxes (load existing values)
            if (WindowManager.TryGetControl("winEditors", "txtUp", out var tUp)) tUp.Text = Data.MyMap.Up.ToString();
            if (WindowManager.TryGetControl("winEditors", "txtDown", out var tDown)) tDown.Text = Data.MyMap.Down.ToString();
            if (WindowManager.TryGetControl("winEditors", "txtLeft", out var tLeft)) tLeft.Text = Data.MyMap.Left.ToString();
            if (WindowManager.TryGetControl("winEditors", "txtRight", out var tRight)) tRight.Text = Data.MyMap.Right.ToString();

            if (WindowManager.TryGetControl("winEditors", "txtBootMap", out var tBMap)) tBMap.Text = Data.MyMap.BootMap.ToString();
            if (WindowManager.TryGetControl("winEditors", "txtBootX", out var tBX)) tBX.Text = Data.MyMap.BootX.ToString();
            if (WindowManager.TryGetControl("winEditors", "txtBootY", out var tBY)) tBY.Text = Data.MyMap.BootY.ToString();

            if (WindowManager.TryGetControl("winEditors", "txtMaxX", out var tMaxX)) tMaxX.Text = Data.MyMap.MaxX.ToString();
            if (WindowManager.TryGetControl("winEditors", "txtMaxY", out var tMaxY)) tMaxY.Text = Data.MyMap.MaxY.ToString();
        }

        // Initialize Effects controls: wire combos, sliders, and live labels
        void InitEffectsControls()
        {
            void SetLabel(string name, string text)
            {
                if (WindowManager.TryGetControl("winEditors", name, out var l)) l.Text = text;
            }

            // Weather combo
            if (WindowManager.TryGetControl("winEditors", "cmbWeather", out var wCtrl) && wCtrl is ComboBox cmbWeather)
            {
                cmbWeather.Items.Clear();
                cmbWeather.Items.Add("None");
                cmbWeather.Items.Add("Rain");
                cmbWeather.Items.Add("Snow");
                cmbWeather.Items.Add("Storm");
                cmbWeather.Value = Math.Clamp(Data.MyMap.Weather, 0, cmbWeather.Items.Count - 1);
                cmbWeather.CallBack[(int)ControlState.MouseMove] = () =>
                {
                    Data.MyMap.Weather = (byte)Math.Clamp(cmbWeather.Value, 0, 3);
                };
            }

            // Helper to bind a horizontal scrollbar to a value + label
            void BindSlider(string barName, int value, int min, int max, Action<int> setter, string labelName = null, string caption = null)
            {
                if (WindowManager.TryGetControl("winEditors", barName, out var sCtrl) && sCtrl is Client.Game.UI.Controls.ScrollBar sb)
                {
                    sb.Min = min; sb.Max = max;
                    sCtrl.Value = Math.Clamp(value, min, max);
                    void UpdateLabel()
                    {
                        if (!string.IsNullOrEmpty(labelName) && !string.IsNullOrEmpty(caption))
                        {
                            SetLabel(labelName, $"{caption}: {Math.Clamp(sCtrl.Value, sb.Min, sb.Max)}");
                        }
                    }
                    UpdateLabel();
                    sb.CallBack[(int)ControlState.MouseMove] = () =>
                    {
                        int v = Math.Clamp(sCtrl.Value, sb.Min, sb.Max);
                        setter(v);
                        UpdateLabel();
                    };
                }
            }

            // Weather intensity
            BindSlider("sldIntensity", Data.MyMap.WeatherIntensity, 0, 100, v => Data.MyMap.WeatherIntensity = v, "lblIntensity", "Intensity");

            // Fog
            BindSlider("sldFog", Data.MyMap.Fog, 0, 100, v => Data.MyMap.Fog = v, "lblFog", "Fog");
            BindSlider("sldFogOpacity", Data.MyMap.FogOpacity, 0, 255, v => Data.MyMap.FogOpacity = (byte)v, "lblFogOpacity", "Opacity");
            BindSlider("sldFogSpeed", Data.MyMap.FogSpeed, 0, 255, v => Data.MyMap.FogSpeed = (byte)v, "lblFogSpeed", "Speed");

            // Tint toggle
            if (WindowManager.TryGetControl("winEditors", "chkTint", out var chkTint))
            {
                chkTint.Value = Data.MyMap.MapTint ? 1 : 0;
                chkTint.CallBack[(int)ControlState.MouseDown] = () =>
                {
                    chkTint.Value = chkTint.Value == 0 ? 1 : 0;
                    Data.MyMap.MapTint = chkTint.Value == 1;
                };
            }
            // Tint sliders RGBA
            BindSlider("sldMapRed", Data.MyMap.MapTintR, 0, 255, v => Data.MyMap.MapTintR = (byte)v, "lblTintR", "Red");
            BindSlider("sldMapGreen", Data.MyMap.MapTintG, 0, 255, v => Data.MyMap.MapTintG = (byte)v, "lblTintG", "Green");
            BindSlider("sldMapBlue", Data.MyMap.MapTintB, 0, 255, v => Data.MyMap.MapTintB = (byte)v, "lblTintB", "Blue");
            BindSlider("sldMapAlpha", Data.MyMap.MapTintA, 0, 255, v => Data.MyMap.MapTintA = (byte)v, "lblTintA", "Alpha");

            // Panorama
            if (WindowManager.TryGetControl("winEditors", "cmbPanorama", out var panoCtrl) && panoCtrl is ComboBox cmbPanorama)
            {
                cmbPanorama.Items.Clear();
                cmbPanorama.Items.Add("None");
                General.CheckPanoramas();
                for (int i = 1; i <= GameState.NumPanoramas; i++) cmbPanorama.Items.Add(i.ToString());
                cmbPanorama.Value = Math.Clamp(Data.MyMap.Panorama, 0, cmbPanorama.Items.Count - 1);
                cmbPanorama.CallBack[(int)ControlState.MouseMove] = () =>
                {
                    Data.MyMap.Panorama = (byte)Math.Clamp(cmbPanorama.Value, 0, Math.Max(0, GameState.NumPanoramas));
                };
            }

            // Parallax
            if (WindowManager.TryGetControl("winEditors", "cmbParallax", out var paraCtrl) && paraCtrl is ComboBox cmbParallax)
            {
                cmbParallax.Items.Clear();
                cmbParallax.Items.Add("None");
                General.CheckParallax();
                for (int i = 1; i <= GameState.NumParallax; i++) cmbParallax.Items.Add(i.ToString());
                cmbParallax.Value = Math.Clamp(Data.MyMap.Parallax, 0, cmbParallax.Items.Count - 1);
                cmbParallax.CallBack[(int)ControlState.MouseMove] = () =>
                {
                    Data.MyMap.Parallax = (byte)Math.Clamp(cmbParallax.Value, 0, Math.Max(0, GameState.NumParallax));
                };
            }

            // Brightness
            BindSlider("sldMapBrightness", Data.MyMap.Brightness, 0, 100, v => Data.MyMap.Brightness = (byte)v, "lblBrightness", "Brightness");
        }
        
        void InitNpcList()
        {
            if (WindowManager.TryGetControl("winEditors", "cmbNpcList", out var npcCtrl) && npcCtrl is ComboBox cmbNpc)
            {
                int prev = cmbNpc.Value;
                cmbNpc.Items.Clear();
                cmbNpc.Items.Add("None");
                var npcArr = Data.Npc;
                if (npcArr != null)
                {
                    for (int i = 0; i < npcArr.Length; i++)
                    {
                        var raw = npcArr[i].Name ?? string.Empty;
                        var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                        cmbNpc.Items.Add($"{i + 1}: {name}");
                    }
                }
                cmbNpc.Value = (prev >= 0 && prev < cmbNpc.Items.Count) ? prev : 0;
                cmbNpc.CallBack[(int)ControlState.MouseDown] = () =>
                {
                    // Assign selected NPC to the currently selected map NPC slot
                    var slot = WinEditors.NpcSelectedSlot;
                    if (Data.MyMap.Npc != null && slot >= 0 && slot < Data.MyMap.Npc.Length)
                    {
                        int idx = cmbNpc.Value - 1; // 0 = None; value maps to array index
                        Data.MyMap.Npc[slot] = idx;
                        // Immediately refresh the listbox display for this slot
                        if (WindowManager.TryGetControl("winEditors", "lstNpcs", out var lstCtrl) && lstCtrl is ListBox lst)
                        {
                            int npcIndex = idx;
                            string name = "None";
                            if (npcIndex >= 0 && npcIndex < (Data.Npc?.Length ?? 0))
                            {
                                var rawName = Data.Npc[npcIndex].Name ?? string.Empty;
                                if (!string.IsNullOrWhiteSpace(rawName)) name = rawName.Trim();
                            }

                            int displayIndex = slot;
                            if (displayIndex >= 0 && displayIndex < lst.Items.Count)
                            {
                                lst.Items[displayIndex] = $"{slot + 1}: {name}";
                            }
                        }
                    }
                };
            }
        }

        // Opacity checkbox on Tiles page: toggles GameState.HideLayers
        if (WindowManager.TryGetControl("winEditors", "chkOpacity", out var chkOpacity))
        {
            chkOpacity.Value = GameState.HideLayers ? 1 : 0;
            chkOpacity.CallBack[(int)ControlState.MouseDown] = () =>
            {
                chkOpacity.Value = chkOpacity.Value == 0 ? 1 : 0;
                GameState.HideLayers = chkOpacity.Value == 1;
            };
        }
        
        InitSettingsLists();
        InitEffectsControls();

        // Wire Settings page section toggles
        if (WindowManager.TryGetControl("winEditors", "btnGoTiles", out var btnGoTiles))
            btnGoTiles.CallBack[(int)ControlState.MouseDown] = () => ShowTab("Tools");
        if (WindowManager.TryGetControl("winEditors", "btnGoAttributes", out var btnGoAttributes))
            btnGoAttributes.CallBack[(int)ControlState.MouseDown] = () => ShowTab("Attributes");
        if (WindowManager.TryGetControl("winEditors", "btnGoNpcs", out var btnGoNpcs))
            btnGoNpcs.CallBack[(int)ControlState.MouseDown] = () => ShowTab("Npcs");
        if (WindowManager.TryGetControl("winEditors", "btnGoDirBlock", out var btnGoDirBlock))
            btnGoDirBlock.CallBack[(int)ControlState.MouseDown] = () => ShowTab("DirBlock");
        if (WindowManager.TryGetControl("winEditors", "btnGoEvents", out var btnGoEvents))
            btnGoEvents.CallBack[(int)ControlState.MouseDown] = () => ShowTab("Events");
        if (WindowManager.TryGetControl("winEditors", "btnGoEffects", out var btnGoEffects))
            btnGoEffects.CallBack[(int)ControlState.MouseDown] = () => ShowTab("Effects");

        // Events page: copy/paste toggles and label updates
        void UpdateEventLabels()
        {
            if (WindowManager.TryGetControl("winEditors", "lblCopyMode", out var lblCopy))
                lblCopy.Text = Event.EventCopy ? "Copy Mode On" : "Copy Mode Off";
            if (WindowManager.TryGetControl("winEditors", "lblPasteMode", out var lblPaste))
                lblPaste.Text = Event.EventPaste ? "Paste Mode On" : "Paste Mode Off";
        }
        if (WindowManager.TryGetControl("winEditors", "btnCopyEvent", out var btnCopyEvent))
            btnCopyEvent.CallBack[(int)ControlState.MouseDown] = () => { Event.EventCopy = !Event.EventCopy; if (Event.EventCopy) Event.EventPaste = false; UpdateEventLabels(); };
        if (WindowManager.TryGetControl("winEditors", "btnPasteEvent", out var btnPasteEvent))
            btnPasteEvent.CallBack[(int)ControlState.MouseDown] = () => { Event.EventPaste = !Event.EventPaste; if (Event.EventPaste) Event.EventCopy = false; UpdateEventLabels(); };
        UpdateEventLabels();

        // Default section
        ShowTab("Tools");

        // Wire tileset preview draw
        if (WindowManager.TryGetControl("winEditors", "picTileset", out var picTileset))
        {
            picTileset.OnDraw = WinEditors.OnDrawTileset;
            picTileset.CallBack[(int)ControlState.MouseDown] = WinEditors.OnTilesetMouseDown;
            picTileset.CallBack[(int)ControlState.MouseMove] = WinEditors.OnTilesetMouseMove;
            picTileset.CallBack[(int)ControlState.MouseUp] = WinEditors.OnTilesetMouseUp;
            picTileset.CallBack[(int)ControlState.MouseScroll] = WinEditors.OnTilesetMouseWheel;
        }

        // Wire NPC list drawing and interactions (ListBox)
        if (WindowManager.TryGetControl("winEditors", "lstNpcs", out var lstNpcs) && lstNpcs is ListBox list)
        {
            list.OnDraw = WinEditors.OnDrawNpcList;
            list.CallBack[(int)ControlState.MouseDown] = WinEditors.OnNpcListMouseDown;
            // Use MouseScroll (enum) for wheel events
            list.CallBack[(int)ControlState.MouseScroll] = WinEditors.OnNpcListMouseWheel;
        }
        if (WindowManager.TryGetControl("winEditors", "sldNpcList", out var sldNpcList))
        {
            sldNpcList.CallBack[(int)ControlState.MouseMove] = WinEditors.OnNpcScrollBarMove;
        }

        // Dir Block: confirmation + clear via WinEditors helper
        if (WindowManager.TryGetControl("winEditors", "btnDirClear", out var btnDirClear))
        {
            btnDirClear.CallBack[(int)ControlState.MouseDown] = WinEditors.OnDirClearClick;
        }
    }

    public void UpdateWindow_EditorNpc()
    {
        var window = WindowLoader.FromLayout("winNpcEditor");
        // Close button
        if (WindowManager.TryGetControl("winNpcEditor", "btnClose", out var btnClose))
        {
            btnClose.CallBack[(int)ControlState.MouseDown] = () => WindowManager.HideWindow("winNpcEditor");
        }

        // List interactions (NPC index + scrollbar)
        ListBox? npcList = null;
        if (WindowManager.TryGetControl("winNpcEditor", "lstNpcIndex", out var lstCtrl) && lstCtrl is ListBox list)
        {
            npcList = list;
            list.CallBack[(int)ControlState.MouseDown] = WinNpcEditor.OnListMouseDown;
        }
        if (WindowManager.TryGetControl("winNpcEditor", "sldNpcList", out var sldNpcList) && sldNpcList is ScrollBar sbNpc)
        {
            sbNpc.CallBack[(int)ControlState.MouseMove] = () =>
            {
                if (npcList != null)
                {
                    npcList.ScrollOffset = sbNpc.Value;
                }
            };
        }

        // Text fields
        if (WindowManager.TryGetControl("winNpcEditor", "txtNpcName", out var txtNameCtrl) && txtNameCtrl is TextBox txtName)
        {
            txtName.CallBack[(int)ControlState.KeyUp] = () => WinNpcEditor.UpdateName(txtName.Text ?? string.Empty);
        }
        
        if (WindowManager.TryGetControl("winNpcEditor", "txtNpcAttackSay", out var atkCtrl) && atkCtrl is TextBox txtAtk)
        {
            txtAtk.CallBack[(int)ControlState.KeyUp] = () =>
            {
                if (WinNpcEditor.SelectedIndex >= 0 && WinNpcEditor.SelectedIndex < Variables.MaxNpcs)
                {
                    Data.Npc[WinNpcEditor.SelectedIndex].AttackSay = txtAtk.Text ?? string.Empty;
                    GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
                }
            };
        }

        // Simple helper for combo apply on click
        void BindCombo(string name, Action<int> apply)
        {
            if (WindowManager.TryGetControl("winNpcEditor", name, out var c) && c is ComboBox combo)
            {
                combo.CallBack[(int)ControlState.MouseDown] = () =>
                {
                    int v = Math.Max(0, combo.Value);
                    apply(v);
                };
            }
        }

        BindCombo("cmbNpcBehavior", v => { if (WinNpcEditor.SelectedIndex >= 0) { Data.Npc[WinNpcEditor.SelectedIndex].Behavior = (byte)v; GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true; } });
        BindCombo("cmbNpcFaction", v => { if (WinNpcEditor.SelectedIndex >= 0) { Data.Npc[WinNpcEditor.SelectedIndex].Faction = (byte)v; GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true; } });
        BindCombo("cmbNpcSpawnPeriod", v => { if (WinNpcEditor.SelectedIndex >= 0) { Data.Npc[WinNpcEditor.SelectedIndex].SpawnTime = (byte)v; GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true; } });
        BindCombo("cmbNpcAnimation", v => { if (WinNpcEditor.SelectedIndex >= 0) { Data.Npc[WinNpcEditor.SelectedIndex].Animation = (byte)v; GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true; } });

        // Skills 1..6
        void BindSkill(string ctrlName, int idx)
        {
            BindCombo(ctrlName, v =>
            {
                if (WinNpcEditor.SelectedIndex >= 0)
                {
                    if (Data.Npc[WinNpcEditor.SelectedIndex].Skill == null || Data.Npc[WinNpcEditor.SelectedIndex].Skill.Length <= idx)
                        return;
                    Data.Npc[WinNpcEditor.SelectedIndex].Skill[idx] = (byte)Math.Clamp(v, 0, Variables.MaxSkills - 1);
                    GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
                }
            });
        }
        BindSkill("cmbNpcSkill1", 0);
        BindSkill("cmbNpcSkill2", 1);
        BindSkill("cmbNpcSkill3", 2);
        BindSkill("cmbNpcSkill4", 3);
        BindSkill("cmbNpcSkill5", 4);
        BindSkill("cmbNpcSkill6", 5);

        // Drop slot change reloads fields
        if (WindowManager.TryGetControl("winNpcEditor", "cmbNpcDropSlot", out var dropSlotCtrl) && dropSlotCtrl is ComboBox cmbSlot)
        {
            cmbSlot.CallBack[(int)ControlState.MouseDown] = () => WinNpcEditor.LoadNpc(WinNpcEditor.SelectedIndex);
        }

        // Drop item combo
        if (WindowManager.TryGetControl("winNpcEditor", "cmbNpcDropItem", out var dropItemCtrl) && dropItemCtrl is ComboBox cmbItem)
        {
            cmbItem.CallBack[(int)ControlState.MouseDown] = () =>
            {
                int slot = 0;
                if (WindowManager.TryGetControl("winNpcEditor", "cmbNpcDropSlot", out var ds) && ds is ComboBox s) slot = Math.Clamp(s.Value, 0, 5);
                if (WinNpcEditor.SelectedIndex >= 0)
                {
                    if (Data.Npc[WinNpcEditor.SelectedIndex].DropItem != null && slot < Data.Npc[WinNpcEditor.SelectedIndex].DropItem.Length)
                    {
                        Data.Npc[WinNpcEditor.SelectedIndex].DropItem[slot] = Math.Clamp(cmbItem.Value, 0, Variables.MaxItems - 1);
                        GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
                    }
                }
            };
        }

        // Amount / Chance textboxes
        void BindIntText(string name, Action<int> apply, int min, int max)
        {
            if (WindowManager.TryGetControl("winNpcEditor", name, out var t) && t is TextBox tb)
            {
                tb.CallBack[(int)ControlState.KeyUp] = () =>
                {
                    var s = tb.Text?.Trim();
                    if (!int.TryParse(s, out var v)) v = min;
                    v = Math.Clamp(v, min, max);
                    apply(v);
                };
            }
        }
        BindIntText("nudNpcAmount", v =>
        {
            int slot = 0;
            if (WindowManager.TryGetControl("winNpcEditor", "cmbNpcDropSlot", out var ds) && ds is ComboBox s) slot = Math.Clamp(s.Value, 0, 5);
            if (WinNpcEditor.SelectedIndex >= 0 && Data.Npc[WinNpcEditor.SelectedIndex].DropItemValue != null && slot < Data.Npc[WinNpcEditor.SelectedIndex].DropItemValue.Length)
            {
                Data.Npc[WinNpcEditor.SelectedIndex].DropItemValue[slot] = v;
                GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
            }
        }, 0, 1000000);

        BindIntText("nudNpcChance", v =>
        {
            int slot = 0;
            if (WindowManager.TryGetControl("winNpcEditor", "cmbNpcDropSlot", out var ds) && ds is ComboBox s) slot = Math.Clamp(s.Value, 0, 5);
            if (WinNpcEditor.SelectedIndex >= 0 && Data.Npc[WinNpcEditor.SelectedIndex].DropChance != null && slot < Data.Npc[WinNpcEditor.SelectedIndex].DropChance.Length)
            {
                Data.Npc[WinNpcEditor.SelectedIndex].DropChance[slot] = v;
                GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
            }
        }, 0, 1000000);

        // Basic stats text boxes
        BindIntText("txtNpcHp", v =>
        {
            if (WinNpcEditor.SelectedIndex >= 0 && WinNpcEditor.SelectedIndex < Variables.MaxNpcs)
            {
                Data.Npc[WinNpcEditor.SelectedIndex].Hp = v;
                GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
            }
        }, 0, 100000000);

        BindIntText("txtNpcExp", v =>
        {
            if (WinNpcEditor.SelectedIndex >= 0 && WinNpcEditor.SelectedIndex < Variables.MaxNpcs)
            {
                Data.Npc[WinNpcEditor.SelectedIndex].Exp = v;
                GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
            }
        }, 0, 100000000);

        BindIntText("txtNpcLevel", v =>
        {
            if (WinNpcEditor.SelectedIndex >= 0 && WinNpcEditor.SelectedIndex < Variables.MaxNpcs)
            {
                Data.Npc[WinNpcEditor.SelectedIndex].Level = (byte)Math.Clamp(v, 0, 255);
                GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
            }
        }, 0, 255);

        BindIntText("txtNpcDamage", v =>
        {
            if (WinNpcEditor.SelectedIndex >= 0 && WinNpcEditor.SelectedIndex < Variables.MaxNpcs)
            {
                Data.Npc[WinNpcEditor.SelectedIndex].Damage = v;
                GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
            }
        }, 0, 100000000);

        BindIntText("txtNpcRange", v =>
        {
            if (WinNpcEditor.SelectedIndex >= 0 && WinNpcEditor.SelectedIndex < Variables.MaxNpcs)
            {
                Data.Npc[WinNpcEditor.SelectedIndex].Range = (byte)Math.Clamp(v, 0, 255);
                GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
            }
        }, 0, 255);

        // Buttons
        if (WindowManager.TryGetControl("winNpcEditor", "btnSave", out var btnSave))
        {
            btnSave.CallBack[(int)ControlState.MouseDown] = () => { Editors.NpcEditorOK(); WindowManager.HideWindow("winNpcEditor"); };
        }
        if (WindowManager.TryGetControl("winNpcEditor", "btnCancel", out var btnCancel))
        {
            btnCancel.CallBack[(int)ControlState.MouseDown] = () => { Editors.NpcEditorCancel(); WindowManager.HideWindow("winNpcEditor"); };
        }
        if (WindowManager.TryGetControl("winNpcEditor", "btnDelete", out var btnDelete))
        {
            btnDelete.CallBack[(int)ControlState.MouseDown] = () =>
            {
                Database.ClearNpc(GameState.EditorIndex);
                WinNpcEditor.LoadNpc(GameState.EditorIndex);
            };
        }
        if (WindowManager.TryGetControl("winNpcEditor", "btnNpcCopy", out var btnCopy))
        {
            btnCopy.CallBack[(int)ControlState.MouseDown] = WinNpcEditor.OnCopyOrPaste;
        }

        // Initialize lists and populate controls
        WinNpcEditor.Init();
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