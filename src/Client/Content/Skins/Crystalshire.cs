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
    // - Prefers loader-recorded child range (FirstChildIndex..LastChildIndex).
    // - Falls back to rectangle intersection (not just containment) for overlapping layouts.
    // - Ensures common UI controls within the group region follow visibility.
    private int SetGroupBoxVisible(string windowName, string groupBoxName, bool visible, bool includeGroupBoxFrame = true)
    {
        if (!WindowManager.TryGetWindow(windowName, out var win) || win is null) return 0;
        if (!WindowManager.TryGetControl(windowName, groupBoxName, out var ctrl) || ctrl is not GroupBox gb) return 0;

        int changed = 0;

        // Optionally toggle the group box frame
        if (includeGroupBoxFrame && gb.Visible != visible)
        {
            gb.Visible = visible;
            changed++;
        }

        // Prefer loader-defined children (exact membership)
        if (gb.FirstChildIndex >= 0 && gb.LastChildIndex >= gb.FirstChildIndex && gb.LastChildIndex < win.Controls.Count)
        {
            for (int i = gb.FirstChildIndex; i <= gb.LastChildIndex; i++)
            {
                var c = win.Controls[i];
                if (ReferenceEquals(c, gb)) continue;
                if (c.Visible != visible)
                {
                    c.Visible = visible;
                    changed++;
                }
            }
            return changed;
        }

        // Fallback: intersection-based (handles overlapping groups and partial overlaps)
        int gx = gb.X, gy = gb.Y, gw = gb.Width, gh = gb.Height;
        int gxr = gx + gw, gyr = gy + gh;

        // Toggle all sibling controls contained within the group box rectangle
        foreach (var c in win.Controls)
        {
            if (ReferenceEquals(c, gb)) continue;

            // Rectangle intersection (any overlap counts as belonging to the group region)
            bool intersects =
                c.X < gxr && (c.X + c.Width) > gx &&
                c.Y < gyr && (c.Y + c.Height) > gy;

            if (!intersects) continue;

            // Respect common UI control classes; skip nested windows
            if (c is Label || c is ComboBox || c is TextBox || c is PictureBox || c is ScrollBar || c is ListBox || c is Button || c is CheckBox)
            {
                if (c.Visible != visible)
                {
                    c.Visible = visible;
                    changed++;
                }
            }
        }

        return changed;
    }

    public void UpdateWindow_Login()
    {
        var window = WindowLoader.FromLayout("winLogin");

        var userName = SettingsManager.Instance.SaveUsername ? SettingsManager.Instance.Username : string.Empty;

        window.GetChild("btnClose").CallBack[(int)ControlState.MouseDown] = Client.General.DestroyGame;
        window.GetChild("txtUsername").Text = userName;
        window.GetChild("chkSaveUsername").Value = SettingsManager.Instance.SaveUsername ? 1 : 0;
        window.GetChild("btnAccept").CallBack[(int)ControlState.MouseDown] = WinLogin.OnLogin;
        window.GetChild("btnExit").CallBack[(int)ControlState.MouseDown] = WinLogin.OnExit;
        window.GetChild("btnRegister").CallBack[(int)ControlState.MouseDown] = WinLogin.OnRegister;

        WindowManager.SetActiveControl(window, userName.Length == 0 ? "txtUsername" : "txtPassword");
    }

    public void UpdateWindow_Register()
    {
        var window = WindowLoader.FromLayout("winRegister");

        window.GetChild("btnClose").CallBack[(int)ControlState.MouseDown] = WinRegister.OnClose;
        window.GetChild("btnAccept").CallBack[(int)ControlState.MouseDown] = WinRegister.OnRegister;
        window.GetChild("btnExit").CallBack[(int)ControlState.MouseDown] = WinRegister.OnClose;

        WindowManager.SetActiveControl(window, "txtUsername");
    }

    public void UpdateWindow_NewChar()
    {
        var window = WindowLoader.FromLayout("winNewChar");

        window.GetChild("btnClose").CallBack[(int)ControlState.MouseDown] = WinNewChar.OnCancel;
        window.GetChild("btnAccept").CallBack[(int)ControlState.MouseDown] = WinNewChar.OnAccept;
        window.GetChild("btnCancel").CallBack[(int)ControlState.MouseDown] = WinNewChar.OnCancel;
        window.GetChild("picScene").OnDraw = WinNewChar.OnDrawSprite;
        window.GetChild("btnLeft").CallBack[(int)ControlState.MouseDown] = WinNewChar.OnLeftClick;
        window.GetChild("btnRight").CallBack[(int)ControlState.MouseDown] = WinNewChar.OnRightClick;
        window.GetChild("chkMale").CallBack[(int)ControlState.MouseDown] = WinNewChar.OnMaleChecked;
        window.GetChild("chkFemale").CallBack[(int)ControlState.MouseDown] = WinNewChar.OnFemaleChecked;

        WindowManager.SetActiveControl(window, "txtName");
    }

    public void UpdateWindow_Chars()
    {
        var window = WindowLoader.FromLayout("winChars");

        window.GetChild("btnClose").CallBack[(int)ControlState.MouseDown] = WinChars.OnClose;
        window.GetChild("picScene_3").OnDraw = WinChars.OnDraw;
        window.GetChild("btnSelectChar_1").CallBack[(int)ControlState.MouseDown] = WinChars.OnSelectCharacter1Click;
        window.GetChild("btnCreateChar_1").CallBack[(int)ControlState.MouseDown] = WinChars.OnCreateCharacter1Click;
        window.GetChild("btnDelChar_1").CallBack[(int)ControlState.MouseDown] = WinChars.OnDeleteCharacter1Click;
        window.GetChild("btnSelectChar_2").CallBack[(int)ControlState.MouseDown] = WinChars.OnSelectCharacter2Click;
        window.GetChild("btnCreateChar_2").CallBack[(int)ControlState.MouseDown] = WinChars.OnCreateCharacter2Click;
        window.GetChild("btnDelChar_2").CallBack[(int)ControlState.MouseDown] = WinChars.OnDeleteCharacter2Click;
        window.GetChild("btnSelectChar_3").CallBack[(int)ControlState.MouseDown] = WinChars.OnSelectCharacter3Click;
        window.GetChild("btnCreateChar_3").CallBack[(int)ControlState.MouseDown] = WinChars.OnCreateCharacter3Click;
        window.GetChild("btnDelChar_3").CallBack[(int)ControlState.MouseDown] = WinChars.OnDeleteCharacter3Click;
    }

    public void UpdateWindow_Jobs()
    {
        var window = WindowLoader.FromLayout("winJobs");

        // Close
        window.GetChild("btnClose").CallBack[(int)ControlState.MouseDown] = WinJobs.OnClose;

        // Bind nav arrows
        window.GetChild("btnLeft").CallBack[(int)ControlState.MouseDown] = WinJobs.OnLeftClick;
        window.GetChild("btnRight").CallBack[(int)ControlState.MouseDown] = WinJobs.OnRightClick;

        // Accept
        window.GetChild("btnAccept").CallBack[(int)ControlState.MouseDown] = WinJobs.OnAccept;

        // Description overlay click-to-close
        window.GetChild("picOverlay").CallBack[(int)ControlState.MouseDown] = WinJobs.OnClose;
        if (WindowManager.TryGetControl("winJobs", "picBackground", out var picBackground) && picBackground is PictureBox bg)
        {
            bg.OnDraw = WinJobs.OnDrawSprite;
        }
        if (WindowManager.TryGetControl("winJobs", "picOverlay", out var picOverlay) && picOverlay is PictureBox ov)
        {
            ov.OnDraw = WinJobs.OnDrawDescription;
        }
    }

    public void UpdateWindow_Dialogue()
    {
        var window = WindowLoader.FromLayout("winDialogue");

        window.GetChild("btnClose").CallBack[(int)ControlState.MouseDown] = WinDialogue.OnClose;
        window.GetChild("btnYes").CallBack[(int)ControlState.MouseDown] = WinDialogue.OnYes;
        window.GetChild("btnNo").CallBack[(int)ControlState.MouseDown] = WinDialogue.OnNo;
        window.GetChild("btnOkay").CallBack[(int)ControlState.MouseDown] = WinDialogue.OnOkay;
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
        window.GetChild("btnClose").CallBack[(int)ControlState.MouseDown] = WinTrade.OnClose;
        window.GetChild("btnAccept").CallBack[(int)ControlState.MouseDown] = WinTrade.OnAccept;
        window.GetChild("btnDecline").CallBack[(int)ControlState.MouseDown] = WinTrade.OnClose;

        // Wire interactive picture boxes for trade regions
        window.GetChild("picYour").CallBack[(int)ControlState.MouseDown] = WinTrade.OnYourTradeMouseMove;
        window.GetChild("picYour").CallBack[(int)ControlState.MouseMove] = WinTrade.OnYourTradeMouseMove;
        window.GetChild("picYour").CallBack[(int)ControlState.DoubleClick] = WinTrade.OnYourTradeClick;

        window.GetChild("picTheir").CallBack[(int)ControlState.MouseDown] = WinTrade.OnTheirTradeMouseMove;
        window.GetChild("picTheir").CallBack[(int)ControlState.MouseMove] = WinTrade.OnTheirTradeMouseMove;
        window.GetChild("picTheir").CallBack[(int)ControlState.DoubleClick] = WinTrade.OnTheirTradeMouseMove;
    }

    public void UpdateWindow_MapEditor()
    {
        var window = WindowLoader.FromLayout("winMapEditor");

        // Wire scrollable lists
        WireScrollableList("winMapEditor", "lstIndex", "sldNpcList");

        // Close button should discard changes (same as Discard)
        if (WindowManager.TryGetControl("winMapEditor", "btnClose", out var btnClose))
        {
            btnClose.CallBack[(int)ControlState.MouseDown] = () =>
            {
                Editors.MapEditorCancel();
                WindowManager.HideWindow("winMapEditor");
            };
        }

        // Save: apply Settings values and mirror Editors.UpdateMap flow
        if (WindowManager.TryGetControl("winMapEditor", "btnSaveMap", out var btnSave))
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
                if (WindowManager.TryGetControl("winMapEditor", "txtName", out var txtNameCtrl))
                    Data.MyMap.Name = txtNameCtrl.Text?.Trim() ?? string.Empty;
                if (WindowManager.TryGetControl("winMapEditor", "cmbMusic", out var musicCtrl) && musicCtrl is ComboBox cmbMusic)
                {
                    var idx = Math.Clamp(cmbMusic.Value, 0, cmbMusic.Items.Count - 1);
                    Data.MyMap.Music = idx <= 0 ? string.Empty : cmbMusic.Items[idx];
                }
                if (WindowManager.TryGetControl("winMapEditor", "lstShop", out var shopCtrl) && shopCtrl is ComboBox lstShop)
                    Data.MyMap.Shop = lstShop.Value <= 0 ? -1 : lstShop.Value - 1;
                if (WindowManager.TryGetControl("winMapEditor", "lstMoral", out var moralCtrl) && moralCtrl is ComboBox lstMoral)
                    Data.MyMap.Moral = (byte)Math.Clamp(lstMoral.Value, 0, Variables.MaxMorals - 1);

                // Links
                if (WindowManager.TryGetControl("winMapEditor", "txtUp", out var txtUp)) Data.MyMap.Up = (short)ReadIntSafe(txtUp, 0, maxMaps, 0);
                if (WindowManager.TryGetControl("winMapEditor", "txtDown", out var txtDown)) Data.MyMap.Down = (short)ReadIntSafe(txtDown, 0, maxMaps, 0);
                if (WindowManager.TryGetControl("winMapEditor", "txtLeft", out var txtLeft)) Data.MyMap.Left = (short)ReadIntSafe(txtLeft, 0, maxMaps, 0);
                if (WindowManager.TryGetControl("winMapEditor", "txtRight", out var txtRight)) Data.MyMap.Right = (short)ReadIntSafe(txtRight, 0, maxMaps, 0);

                // Boot
                if (WindowManager.TryGetControl("winMapEditor", "txtBootMap", out var txtBootMap)) Data.MyMap.BootMap = (short)ReadIntSafe(txtBootMap, 0, maxMaps, 0);
                if (WindowManager.TryGetControl("winMapEditor", "txtBootX", out var txtBootX)) Data.MyMap.BootX = (byte)ReadIntSafe(txtBootX, 0, Math.Max((byte)0, Data.MyMap.MaxX), 0);
                if (WindowManager.TryGetControl("winMapEditor", "txtBootY", out var txtBootY)) Data.MyMap.BootY = (byte)ReadIntSafe(txtBootY, 0, Math.Max((byte)0, Data.MyMap.MaxY), 0);

                // Flags
                if (WindowManager.TryGetControl("winMapEditor", "chkNoMapRespawn", out var chkNoMapRespawn))
                    Data.MyMap.NoRespawn = chkNoMapRespawn.Value == 1;
                if (WindowManager.TryGetControl("winMapEditor", "chkIndoors", out var chkIndoors))
                    Data.MyMap.Indoors = chkIndoors.Value == 1;

                // Resize map
                var tempArr = (Type.Tile[,])Data.MyMap.Tile.Clone();
                int prevMaxX = Data.MyMap.MaxX;
                int prevMaxY = Data.MyMap.MaxY;

                if (WindowManager.TryGetControl("winMapEditor", "txtMaxX", out var txtMaxX))
                    Data.MyMap.MaxX = (byte)ReadIntSafe(txtMaxX, 1, Variables.MaxMapX, Data.MyMap.MaxX);
                if (WindowManager.TryGetControl("winMapEditor", "txtMaxY", out var txtMaxY))
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
                WindowManager.HideWindow("winMapEditor");
            };
        }

        // Discard: cancel map edit and close
        if (WindowManager.TryGetControl("winMapEditor", "btnDiscard", out var btnDiscard))
        {
            btnDiscard.CallBack[(int)ControlState.MouseDown] = () => { Editors.MapEditorCancel(); WindowManager.HideWindow("winMapEditor"); };
        }

        // Layer buttons: update current layer in GameState
        void BindLayer(string ctrl, int layer)
        {
            if (WindowManager.TryGetControl("winMapEditor", ctrl, out var c))
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
        if (WindowManager.TryGetControl("winMapEditor", "btnToolPencil", out var btnPencil))
            btnPencil.CallBack[(int)ControlState.MouseDown] = () => { GameState.EyeDropper = false; };

        if (WindowManager.TryGetControl("winMapEditor", "btnToolFill", out var btnFill))
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
                    WinMapEditor.OnFillLayerClick();
                }
            };
        if (WindowManager.TryGetControl("winMapEditor", "btnToolEraser", out var btnErase))
            btnErase.CallBack[(int)ControlState.MouseDown] = () =>
            {
                // Contextual clear: Directions -> clear dir blocks (confirm), Attributes -> clear attributes (confirm), otherwise clear current tiles layer
                if (GameState.MapEditorTab == (int)MapEditorTab.Directions)
                {
                    WinMapEditor.OnDirClearClick();
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
        if (WindowManager.TryGetControl("winMapEditor", "btnGrid", out var btnGrid))
            btnGrid.CallBack[(int)ControlState.MouseDown] = () => { GameState.MapGrid = !GameState.MapGrid; };
        if (WindowManager.TryGetControl("winMapEditor", "btnEyeDropper", out var btnEye))
            btnEye.CallBack[(int)ControlState.MouseDown] = () => { GameState.EyeDropper = !GameState.EyeDropper; };
        if (WindowManager.TryGetControl("winMapEditor", "btnUndo", out var btnUndo))
            btnUndo.CallBack[(int)ControlState.MouseDown] = () => { Editors.Undo(); };
        if (WindowManager.TryGetControl("winMapEditor", "btnRedo", out var btnRedo))
            btnRedo.CallBack[(int)ControlState.MouseDown] = () => { Editors.Redo(); };

        // Quick actions: call into existing helpers if available
        if (WindowManager.TryGetControl("winMapEditor", "btnFillLayer", out var btnFillLayer))
            btnFillLayer.CallBack[(int)ControlState.MouseDown] = () => { WinMapEditor.OnFillLayerClick(); };
        if (WindowManager.TryGetControl("winMapEditor", "btnClearLayer", out var btnClearLayer))
            btnClearLayer.CallBack[(int)ControlState.MouseDown] = () => { Editors.MapEditorClearLayer((MapLayer)GameState.CurLayer); };
        if (WindowManager.TryGetControl("winMapEditor", "btnCopyMap", out var btnCopy))
            btnCopy.CallBack[(int)ControlState.MouseDown] = () => { Editors.MapEditorCopyMap(); };
        if (WindowManager.TryGetControl("winMapEditor", "btnPasteMap", out var btnPaste))
            btnPaste.CallBack[(int)ControlState.MouseDown] = () => { Editors.MapEditorPasteMap(); };
        if (WindowManager.TryGetControl("winMapEditor", "btnDeleteMap", out var btnDeleteMap))
            btnDeleteMap.CallBack[(int)ControlState.MouseDown] = () =>
            {
                GameLogic.Dialogue("Map Editor", "Delete Map: ", "Are you sure you want to clear this map?", DialogueType.DeleteMap, DialogueStyle.YesNo);
            };

        // Tileset selector wiring
        string[] autotileNames = new[] { "None", "Autotile", "Fake Autotile", "Animated", "Cliff", "Waterfall" };

        // Populate Layer and Autotile combos and set defaults
        if (WindowManager.TryGetControl("winMapEditor", "cmbLayer", out var cmbLayerCtrl) && cmbLayerCtrl is ComboBox cmbLayer)
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

        if (WindowManager.TryGetControl("winMapEditor", "cmbAutotile", out var cmbAutoCtrl) && cmbAutoCtrl is ComboBox cmbAuto)
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
            string[] warpCtrls = new[] { "lblWarp", "lblWarpMap", "sldMapWarp", "lblWarpX", "sldMapWarpX", "lblWarpY", "sldMapWarpY", "btnMapWarp" }; ;
            foreach (var n in warpCtrls)
            {
                if (WindowManager.TryGetControl("winMapEditor", n, out var c)) c.Visible = showWarp;
            }

            if (showWarp)
            {
                void SetWarpLabel(string name, string caption, int value)
                {
                    if (WindowManager.TryGetControl("winMapEditor", name, out var l)) l.Text = $"{caption}: {value}";
                }

                if (WindowManager.TryGetControl("winMapEditor", "sldMapWarp", out var wMapCtrl) && wMapCtrl is Client.Game.UI.Controls.ScrollBar sbMap)
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

                if (WindowManager.TryGetControl("winMapEditor", "sldMapWarpX", out var wXCtrl) && wXCtrl is Client.Game.UI.Controls.ScrollBar sbX)
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

                if (WindowManager.TryGetControl("winMapEditor", "sldMapWarpY", out var wYCtrl) && wYCtrl is Client.Game.UI.Controls.ScrollBar sbY)
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

                if (WindowManager.TryGetControl("winMapEditor", "btnMapWarp", out var btnMapWarp))
                {
                    btnMapWarp.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        if (WindowManager.TryGetControl("winAdmin", "lstMaps", out var listCtrl) && listCtrl is ListBox lst)
                        {
                            var lines = (lst.Text ?? string.Empty).Split('\n');
                            if (lines.Length == 0) return;
                            var start = Math.Clamp(lst.Value, 0, Math.Max(0, lines.Length - 1));
                            var line = lines[start];
                            if (string.IsNullOrWhiteSpace(line)) return;
                            var colon = line.IndexOf(':');
                            if (colon > 0 && int.TryParse(line.AsSpan(0, colon), out var mapNum))
                            {
                                var target = Math.Max(0, mapNum - 1); // convert 1-based display to 0-based map id
                                Sender.WarpTo(target);
                            }
                        }
                    };
                }
            }

            // Item
            bool showItem = idx == 2;
            string[] itemCtrls = new[] { "lblItem", "cmbMapItem", "lblItemValue", "sldMapItemValue", "btnMapItem" };
            foreach (var n in itemCtrls)
            {
                if (WindowManager.TryGetControl("winMapEditor", n, out var c)) c.Visible = showItem;
            }

            if (showItem)
            {
                if (WindowManager.TryGetControl("winMapEditor", "cmbMapItem", out var cItem) && cItem is ComboBox cmb)
                {
                    if (cmb.Items.Count == 0)
                    {
                        for (int i = 0; i < Variables.MaxItems; i++)
                        {
                            var name = (i < Data.Item.Length) ? (Data.Item[i].Name ?? string.Empty) : string.Empty;
                            cmb.Items.Add(string.IsNullOrWhiteSpace(name) ? $"{i + 1}" : $"{i + 1}: {name.Trim()}");
                        }
                        cmb.Value = 0;
                    }
                }
                if (WindowManager.TryGetControl("winMapEditor", "sldMapItemValue", out var sItemCtrl) && sItemCtrl is Client.Game.UI.Controls.ScrollBar sbItem)
                {
                    sbItem.Min = 1; sbItem.Max = 1024;
                    if (WindowManager.TryGetControl("winMapEditor", "lblItemValue", out var l)) l.Text = $"Amount: {sItemCtrl.Value}";
                    sbItem.CallBack[(int)ControlState.MouseMove] = () =>
                    {
                        if (WindowManager.TryGetControl("winMapEditor", "lblItemValue", out var li)) li.Text = $"Amount: {sItemCtrl.Value}";
                    };
                }
                if (WindowManager.TryGetControl("winMapEditor", "btnMapItem", out var btn))
                {
                    btn.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        if (WindowManager.TryGetControl("winMapEditor", "cmbMapItem", out var c) && c is ComboBox cb)
                            GameState.ItemEditorNum = Math.Clamp(cb.Value, 0, Variables.MaxItems - 1);
                        if (WindowManager.TryGetControl("winMapEditor", "sldMapItemValue", out var s))
                            GameState.ItemEditorValue = Math.Clamp(s.Value, 1, 1024);
                    };
                }
            }

            // Resource
            bool showResource = idx == 4;
            string[] resCtrls = new[] { "lblResource", "cmbResource", "btnResourceOk" };
            foreach (var n in resCtrls)
            {
                if (WindowManager.TryGetControl("winMapEditor", n, out var c)) c.Visible = showResource;
            }
            if (showResource)
            {
                if (WindowManager.TryGetControl("winMapEditor", "cmbResource", out var cRes) && cRes is ComboBox cmb)
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
                if (WindowManager.TryGetControl("winMapEditor", "btnResourceOk", out var btn))
                {
                    btn.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        if (WindowManager.TryGetControl("winMapEditor", "cmbResource", out var c) && c is ComboBox cb)
                            GameState.ResourceEditorNum = Math.Clamp(cb.Value, 0, Variables.MaxResources - 1);
                    };
                }
            }

            // NPC Spawn
            bool showSpawn = idx == 5;
            string[] spawnCtrls = new[] { "lblNpcSpawn", "lblNpcSpawnSlot", "cmbNpcSpawnSlot", "lblNpcDir", "sldNpcDir", "btnNpcSpawn" };
            foreach (var n in spawnCtrls)
            {
                if (WindowManager.TryGetControl("winMapEditor", n, out var c)) c.Visible = showSpawn;
            }

            if (showSpawn)
            {
                // Populate spawn slot combo from current map NPC slots
                if (WindowManager.TryGetControl("winMapEditor", "cmbNpcSpawnSlot", out var cSpawn) && cSpawn is ComboBox cmb)
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
                if (WindowManager.TryGetControl("winMapEditor", "sldNpcDir", out var sDirCtrl) && sDirCtrl is Client.Game.UI.Controls.ScrollBar sbDir)
                {
                    sbDir.Min = 0; sbDir.Max = 3;
                    Action updateDir = () =>
                    {
                        string text = sDirCtrl.Value switch { 0 => "Up", 1 => "Down", 2 => "Left", 3 => "Right", _ => "Up" };
                        if (WindowManager.TryGetControl("winMapEditor", "lblNpcDir", out var l)) l.Text = $"Direction: {text}";
                    };
                    updateDir();
                    sbDir.CallBack[(int)ControlState.MouseMove] = () => updateDir();
                }
                if (WindowManager.TryGetControl("winMapEditor", "btnNpcSpawn", out var btn))
                {
                    btn.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        int slot = 0;
                        if (WindowManager.TryGetControl("winMapEditor", "cmbNpcSpawnSlot", out var c) && c is ComboBox cb)
                            slot = Math.Max(0, cb.Value); // index maps to slot (0=None, 1..)
                        if (WindowManager.TryGetControl("winMapEditor", "sldNpcDir", out var s))
                            GameState.SpawnNpcDir = Math.Clamp(s.Value, 0, 3);
                        GameState.SpawnNpcNum = slot;
                    };
                }
            }

            // Shop
            bool showShop = idx == 6;
            string[] shopCtrls = new[] { "lblShopAttr", "cmbShopAttr", "btnShop" };
            foreach (var n in shopCtrls)
            {
                if (WindowManager.TryGetControl("winMapEditor", n, out var c)) c.Visible = showShop;
            }
            if (showShop)
            {
                if (WindowManager.TryGetControl("winMapEditor", "cmbShopAttr", out var cShop) && cShop is ComboBox cmb)
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
                if (WindowManager.TryGetControl("winMapEditor", "btnShop", out var btn))
                {
                    btn.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        if (WindowManager.TryGetControl("winMapEditor", "cmbShopAttr", out var c) && c is ComboBox cb)
                            GameState.EditorShop = Math.Clamp(cb.Value, 0, Variables.MaxShops - 1);
                    };
                }
            }

            // Heal
            bool showHeal = idx == 8;
            string[] healCtrls = new[] { "lblHeal", "cmbHeal", "lblHealAmount", "sldHeal", "btnHeal" };
            foreach (var n in healCtrls)
            {
                if (WindowManager.TryGetControl("winMapEditor", n, out var c)) c.Visible = showHeal;
            }
            if (showHeal)
            {
                if (WindowManager.TryGetControl("winMapEditor", "cmbHeal", out var cHeal) && cHeal is ComboBox cmb)
                {
                    if (cmb.Items.Count == 0)
                    {
                        cmb.Items.Add("Hp");
                        cmb.Items.Add("Mp");
                        cmb.Items.Add("Sp");
                        cmb.Value = 0;
                    }
                }
                if (WindowManager.TryGetControl("winMapEditor", "sldHeal", out var sHealCtrl) && sHealCtrl is Client.Game.UI.Controls.ScrollBar sbHeal)
                {
                    sbHeal.Min = 1; sbHeal.Max = 1024;
                    if (WindowManager.TryGetControl("winMapEditor", "lblHealAmount", out var l)) l.Text = $"Amount: {sHealCtrl.Value}";
                    sbHeal.CallBack[(int)ControlState.MouseMove] = () =>
                    {
                        if (WindowManager.TryGetControl("winMapEditor", "lblHealAmount", out var li)) li.Text = $"Amount: {sHealCtrl.Value}";
                    };
                }
                if (WindowManager.TryGetControl("winMapEditor", "btnHeal", out var btn))
                {
                    btn.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        if (WindowManager.TryGetControl("winMapEditor", "cmbHeal", out var c) && c is ComboBox cb)
                            GameState.MapEditorHealType = Math.Clamp(cb.Value, 0, 2);
                        if (WindowManager.TryGetControl("winMapEditor", "sldHeal", out var s))
                            GameState.MapEditorHealAmount = Math.Clamp(s.Value, 1, 1024);
                    };
                }
            }

            // Trap
            bool showTrap = idx == 9;
            string[] trapCtrls = new[] { "lblTrap", "lblTrapVital", "cmbTrapVital", "lblTrapAmount", "sldTrap", "btnTrap" };
            foreach (var n in trapCtrls)
            {
                if (WindowManager.TryGetControl("winMapEditor", n, out var c)) c.Visible = showTrap;
            }
            if (showTrap)
            {
                if (WindowManager.TryGetControl("winMapEditor", "cmbTrapVital", out var cTrap) && cTrap is ComboBox cmb)
                {
                    if (cmb.Items.Count == 0)
                    {
                        cmb.Items.Add("Hp");
                        cmb.Items.Add("Mp");
                        cmb.Items.Add("Sp");
                        cmb.Value = 0;
                    }
                }
                if (WindowManager.TryGetControl("winMapEditor", "sldTrap", out var sTrapCtrl) && sTrapCtrl is Client.Game.UI.Controls.ScrollBar sbTrap)
                {
                    sbTrap.Min = 1; sbTrap.Max = 1024;
                    if (WindowManager.TryGetControl("winMapEditor", "lblTrapAmount", out var l)) l.Text = $"Amount: {sTrapCtrl.Value}";
                    sbTrap.CallBack[(int)ControlState.MouseMove] = () =>
                    {
                        if (WindowManager.TryGetControl("winMapEditor", "lblTrapAmount", out var li)) li.Text = $"Amount: {sTrapCtrl.Value}";
                    };
                }
                if (WindowManager.TryGetControl("winMapEditor", "btnTrap", out var btn))
                {
                    btn.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        if (WindowManager.TryGetControl("winMapEditor", "sldTrap", out var s))
                            GameState.MapEditorHealAmount = Math.Clamp(s.Value, 1, 1024);
                        if (WindowManager.TryGetControl("winMapEditor", "cmbTrapVital", out var c) && c is ComboBox cb)
                            GameState.MapEditorTrapVital = Math.Clamp(cb.Value, 0, 2);
                    };
                }
            }

            // Animation
            bool showAnimation = idx == 10;
            string[] animCtrls = new[] { "lblAnimation", "cmbAnimation", "btnAnimation" };
            foreach (var n in animCtrls)
            {
                if (WindowManager.TryGetControl("winMapEditor", n, out var c)) c.Visible = showAnimation;
            }
            if (showAnimation)
            {
                if (WindowManager.TryGetControl("winMapEditor", "cmbAnimation", out var cAnim) && cAnim is ComboBox cmb)
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
                if (WindowManager.TryGetControl("winMapEditor", "btnAnimation", out var btn))
                {
                    btn.CallBack[(int)ControlState.MouseDown] = () =>
                    {
                        if (WindowManager.TryGetControl("winMapEditor", "cmbAnimation", out var c) && c is ComboBox cb)
                            GameState.EditorAnimation = Math.Clamp(cb.Value, 0, Variables.MaxAnimations - 1);
                    };
                }
            }
        }

        if (WindowManager.TryGetControl("winMapEditor", "cmbAttrMode", out var cmbAttrCtrl) && cmbAttrCtrl is ComboBox cmbAttr)
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
        if (WindowManager.TryGetControl("winMapEditor", "btnAttrInfo", out var btnAttrInfo))
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
        if (WindowManager.TryGetControl("winMapEditor", "cmbAttribute", out var cmbAttributeCtrl) && cmbAttributeCtrl is ComboBox cmbAttribute)
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
        if (WindowManager.TryGetControl("winMapEditor", "sldTileset", out var sldTilesetCtrl) && sldTilesetCtrl is Client.Game.UI.Controls.ScrollBar sldTileset)
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
        if (WindowManager.TryGetControl("winMapEditor", "sldTilesetH", out var sldTilesetHCtrl) && sldTilesetHCtrl is Client.Game.UI.Controls.ScrollBar sldTilesetH)
        {
            // Range is updated during drawing; still add a callback to trigger redraw behavior on change
            sldTilesetH.CallBack[(int)ControlState.MouseMove] = () => { /* no-op: OnDraw reads Value */ };
        }

        if (WindowManager.TryGetControl("winMapEditor", "btnTilesetPrev", out var btnTsPrev))
            btnTsPrev.CallBack[(int)ControlState.MouseDown] = () => { GameState.CurTileset = Math.Max(1, GameState.CurTileset - 1); Data.MyMap.Tileset = GameState.CurTileset; };
        if (WindowManager.TryGetControl("winMapEditor", "btnTilesetNext", out var btnTsNext))
            btnTsNext.CallBack[(int)ControlState.MouseDown] = () => { var maxTs = Math.Max(1, GameState.NumTileSets); GameState.CurTileset = Math.Min(maxTs, GameState.CurTileset + 1); Data.MyMap.Tileset = GameState.CurTileset; };

        if (WindowManager.TryGetControl("winMapEditor", "btnAutoPrev", out var btnAutoPrev))
            btnAutoPrev.CallBack[(int)ControlState.MouseDown] = () => { GameState.CurAutotileType = (GameState.CurAutotileType + autotileNames.Length - 1) % autotileNames.Length; UpdateAutotileLabel(); };
        if (WindowManager.TryGetControl("winMapEditor", "btnAutoNext", out var btnAutoNext))
            btnAutoNext.CallBack[(int)ControlState.MouseDown] = () => { GameState.CurAutotileType = (GameState.CurAutotileType + 1) % autotileNames.Length; UpdateAutotileLabel(); };

        if (WindowManager.TryGetControl("winMapEditor", "btnTileApply", out var btnTileApply))
            btnTileApply.CallBack[(int)ControlState.MouseDown] = () =>
            {
                if (WindowManager.TryGetControl("winMapEditor", "txtTileX", out var tbx) && WindowManager.TryGetControl("winMapEditor", "txtTileY", out var tby))
                {
                    int x = int.TryParse(tbx.Text?.Trim(), out var ix) ? ix : 0;
                    int y = int.TryParse(tby.Text?.Trim(), out var iy) ? iy : 0;
                    x = Math.Max(0, x); y = Math.Max(0, y);
                    GameState.EditorTileX = x; GameState.EditorTileY = y;
                    Editors.MapEditorChooseTile(x * GameState.SizeX, y * GameState.SizeY);
                }
            };

        // Faux header tabs like Admin
        var winIndex = WindowManager.GetWindowIndex("winMapEditor");

        void ShowTab(string tab)
        {
            SetGroupBoxVisible("winMapEditor", "grpTiles", false);
            SetGroupBoxVisible("winMapEditor", "grpAttributes", false);
            SetGroupBoxVisible("winMapEditor", "grpNpcs", false);
            SetGroupBoxVisible("winMapEditor", "grpSettings", false);
            SetGroupBoxVisible("winMapEditor", "grpDirBlock", false);
            SetGroupBoxVisible("winMapEditor", "grpEvents", false);
            SetGroupBoxVisible("winMapEditor", "grpEffects", false);

            switch (tab)
            {
                case "Tools":
                    SetGroupBoxVisible("winMapEditor", "grpTiles", true);
                    GameState.MapEditorTab = (int)MapEditorTab.Tiles;
                    break;
                case "Attributes":
                    SetGroupBoxVisible("winMapEditor", "grpAttributes", true);
                    GameState.MapEditorTab = (int)MapEditorTab.Attributes;

                    // Ensure attribute group visibility matches current mode when tab opens
                    if (WindowManager.TryGetControl("winMapEditor", "cmbAttrMode", out var attrModeCtrl) && attrModeCtrl is ComboBox attrCmb)
                    {
                        var idx = Math.Clamp(attrCmb.Value, 0, 12);
                        UpdateAttrVisibility(idx);
                    }
                    break;
                case "Npcs":
                    SetGroupBoxVisible("winMapEditor", "grpNpcs", true);
                    WinMapEditor.InitNpcList();
                    WinMapEditor.RefreshMapNpcList();
                    GameState.MapEditorTab = (int)MapEditorTab.Npcs;
                    break;
                case "Settings":
                    SetGroupBoxVisible("winMapEditor", "grpSettings", true);
                    GameState.MapEditorTab = (int)MapEditorTab.Settings;
                    break;
                case "DirBlock":
                    SetGroupBoxVisible("winMapEditor", "grpDirBlock", true);
                    GameState.MapEditorTab = (int)MapEditorTab.Directions;
                    break;
                case "Events":
                    SetGroupBoxVisible("winMapEditor", "grpEvents", true);
                    GameState.MapEditorTab = (int)MapEditorTab.Events;
                    break;
                case "Effects":
                    SetGroupBoxVisible("winMapEditor", "grpEffects", true);
                    GameState.MapEditorTab = (int)MapEditorTab.Effects;
                    break;
            }
        }

        // Wire toolbar Settings to open Settings section
        if (WindowManager.TryGetControl("winMapEditor", "btnToolbarSettings", out var btnToolbarSettings))
        {
            btnToolbarSettings.CallBack[(int)ControlState.MouseDown] = () => ShowTab("Settings");
        }

        // Initialize Settings controls (Name, Moral, Shop, Music, flags)
        void InitSettingsLists()
        {
            // Name
            if (WindowManager.TryGetControl("winMapEditor", "txtName", out var txtName))
            {
                txtName.Text = Data.MyMap.Name?.Trim() ?? string.Empty;
            }
            // Moral list
            if (WindowManager.TryGetControl("winMapEditor", "lstMoral", out var moralCtrl) && moralCtrl is ComboBox lstMoral)
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
            if (WindowManager.TryGetControl("winMapEditor", "lstShop", out var shopCtrl) && shopCtrl is ComboBox lstShop)
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
            if (WindowManager.TryGetControl("winMapEditor", "cmbMusic", out var musicCtrl) && musicCtrl is ComboBox cmbMusic)
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

                if (WindowManager.TryGetControl("winMapEditor", "btnMusicPreview", out var btnMusicPreview))
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
            if (WindowManager.TryGetControl("winMapEditor", "chkNoMapRespawn", out var chkNoMapRespawn))
            {
                chkNoMapRespawn.Value = Data.MyMap.NoRespawn ? 1 : 0;
                chkNoMapRespawn.CallBack[(int)ControlState.MouseDown] = () =>
                {
                    chkNoMapRespawn.Value = chkNoMapRespawn.Value == 0 ? 1 : 0;
                    Data.MyMap.NoRespawn = chkNoMapRespawn.Value == 1;
                };
            }
            if (WindowManager.TryGetControl("winMapEditor", "chkIndoors", out var chkIndoors))
            {
                chkIndoors.Value = Data.MyMap.Indoors ? 1 : 0;
                chkIndoors.CallBack[(int)ControlState.MouseDown] = () =>
                {
                    chkIndoors.Value = chkIndoors.Value == 0 ? 1 : 0;
                    Data.MyMap.Indoors = chkIndoors.Value == 1;
                };
            }

            // Links / Boot / Sizes textboxes (load existing values)
            if (WindowManager.TryGetControl("winMapEditor", "txtUp", out var tUp)) tUp.Text = Data.MyMap.Up.ToString();
            if (WindowManager.TryGetControl("winMapEditor", "txtDown", out var tDown)) tDown.Text = Data.MyMap.Down.ToString();
            if (WindowManager.TryGetControl("winMapEditor", "txtLeft", out var tLeft)) tLeft.Text = Data.MyMap.Left.ToString();
            if (WindowManager.TryGetControl("winMapEditor", "txtRight", out var tRight)) tRight.Text = Data.MyMap.Right.ToString();

            if (WindowManager.TryGetControl("winMapEditor", "txtBootMap", out var tBMap)) tBMap.Text = Data.MyMap.BootMap.ToString();
            if (WindowManager.TryGetControl("winMapEditor", "txtBootX", out var tBX)) tBX.Text = Data.MyMap.BootX.ToString();
            if (WindowManager.TryGetControl("winMapEditor", "txtBootY", out var tBY)) tBY.Text = Data.MyMap.BootY.ToString();

            if (WindowManager.TryGetControl("winMapEditor", "txtMaxX", out var tMaxX)) tMaxX.Text = Data.MyMap.MaxX.ToString();
            if (WindowManager.TryGetControl("winMapEditor", "txtMaxY", out var tMaxY)) tMaxY.Text = Data.MyMap.MaxY.ToString();
        }

        // Initialize Effects controls: wire combos, sliders, and live labels
        void InitEffectsControls()
        {
            void SetLabel(string name, string text)
            {
                if (WindowManager.TryGetControl("winMapEditor", name, out var l)) l.Text = text;
            }

            // Weather combo
            if (WindowManager.TryGetControl("winMapEditor", "cmbWeather", out var wCtrl) && wCtrl is ComboBox cmbWeather)
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
                if (WindowManager.TryGetControl("winMapEditor", barName, out var sCtrl) && sCtrl is Client.Game.UI.Controls.ScrollBar sb)
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
            if (WindowManager.TryGetControl("winMapEditor", "chkTint", out var chkTint))
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
            if (WindowManager.TryGetControl("winMapEditor", "cmbPanorama", out var panoCtrl) && panoCtrl is ComboBox cmbPanorama)
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
            if (WindowManager.TryGetControl("winMapEditor", "cmbParallax", out var paraCtrl) && paraCtrl is ComboBox cmbParallax)
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

        // Opacity checkbox on Tiles page: toggles GameState.HideLayers
        if (WindowManager.TryGetControl("winMapEditor", "chkOpacity", out var chkOpacity))
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
        if (WindowManager.TryGetControl("winMapEditor", "btnGoTiles", out var btnGoTiles))
        {
            btnGoTiles.CallBack[(int)ControlState.MouseDown] = () => ShowTab("Tools");
        }
        if (WindowManager.TryGetControl("winMapEditor", "btnGoAttributes", out var btnGoAttributes))
        {
            btnGoAttributes.CallBack[(int)ControlState.MouseDown] = () => ShowTab("Attributes");
        }
        if (WindowManager.TryGetControl("winMapEditor", "btnGoNpcs", out var btnGoNpcs))
        {
            btnGoNpcs.CallBack[(int)ControlState.MouseDown] = () => ShowTab("Npcs");
        }
        if (WindowManager.TryGetControl("winMapEditor", "btnGoDirBlock", out var btnGoDirBlock))
        {
            btnGoDirBlock.CallBack[(int)ControlState.MouseDown] = () => ShowTab("DirBlock");
        }
        if (WindowManager.TryGetControl("winMapEditor", "btnGoEvents", out var btnGoEvents))
        {
            btnGoEvents.CallBack[(int)ControlState.MouseDown] = () => ShowTab("Events");
        }
        if (WindowManager.TryGetControl("winMapEditor", "btnGoEffects", out var btnGoEffects))
        {
            btnGoEffects.CallBack[(int)ControlState.MouseDown] = () => ShowTab("Effects");
        }

        // Events page: copy/paste toggles and label updates
        void UpdateEventLabels()
        {
            if (WindowManager.TryGetControl("winMapEditor", "lblCopyMode", out var lblCopy))
                lblCopy.Text = Event.EventCopy ? "Copy Mode On" : "Copy Mode Off";
            if (WindowManager.TryGetControl("winMapEditor", "lblPasteMode", out var lblPaste))
                lblPaste.Text = Event.EventPaste ? "Paste Mode On" : "Paste Mode Off";
        }
        if (WindowManager.TryGetControl("winMapEditor", "btnCopyEvent", out var btnCopyEvent))
            btnCopyEvent.CallBack[(int)ControlState.MouseDown] = () => { Event.EventCopy = !Event.EventCopy; if (Event.EventCopy) Event.EventPaste = false; UpdateEventLabels(); };
        if (WindowManager.TryGetControl("winMapEditor", "btnPasteEvent", out var btnPasteEvent))
            btnPasteEvent.CallBack[(int)ControlState.MouseDown] = () => { Event.EventPaste = !Event.EventPaste; if (Event.EventPaste) Event.EventCopy = false; UpdateEventLabels(); };
        UpdateEventLabels();

        // Default section
        ShowTab("Tools");

        // Wire tileset preview draw
        if (WindowManager.TryGetControl("winMapEditor", "picTileset", out var picTileset))
        {
            picTileset.OnDraw = WinMapEditor.OnDrawTileset;
            picTileset.CallBack[(int)ControlState.MouseDown] = WinMapEditor.OnTilesetMouseDown;
            picTileset.CallBack[(int)ControlState.MouseMove] = WinMapEditor.OnTilesetMouseMove;
            picTileset.CallBack[(int)ControlState.MouseUp] = WinMapEditor.OnTilesetMouseUp;
            picTileset.CallBack[(int)ControlState.MouseScroll] = WinMapEditor.OnTilesetMouseWheel;
        }

        // Npc list drawing and interactions
        if (WindowManager.TryGetControl("winMapEditor", "lstIndex", out var lstIndex) && lstIndex is ListBox list)
        {
            list.CallBack[(int)ControlState.MouseDown] = WinMapEditor.OnNpcListMouseDown;
            // Use MouseScroll (enum) for wheel events
            list.CallBack[(int)ControlState.MouseScroll] = WinMapEditor.OnNpcListMouseWheel;
        }
        if (WindowManager.TryGetControl("winMapEditor", "sldNpcList", out var sldNpcList))
        {
            sldNpcList.CallBack[(int)ControlState.MouseMove] = WinMapEditor.OnNpcScrollBarMove;
        }

        // Dir Block: confirmation + clear via WinEditorMap helper
        if (WindowManager.TryGetControl("winMapEditor", "btnDirClear", out var btnDirClear))
        {
            btnDirClear.CallBack[(int)ControlState.MouseDown] = WinMapEditor.OnDirClearClick;
        }
    }

    public void UpdateWindow_NpcEditor()
    {
        var window = WindowLoader.FromLayout("winNpcEditor");
        // Close button
        if (WindowManager.TryGetControl("winNpcEditor", "btnClose", out var btnClose))
        {
            btnClose.CallBack[(int)ControlState.MouseDown] = () => WindowManager.HideWindow("winNpcEditor");
        }

        // Sprite preview picture box draws NPC sprite each frame
        if (WindowManager.TryGetControl("winNpcEditor", "picSprite", out var picSpriteCtrl) && picSpriteCtrl is PictureBox picSprite)
        {
            picSprite.OnDraw = WinNpcEditor.OnDrawSprite;
        }

        // List interactions
        ListBox npcList = null;
        if (WindowManager.TryGetControl("winNpcEditor", "lstIndex", out var lstCtrl) && lstCtrl is ListBox list)
        {
            npcList = list;
            list.CallBack[(int)ControlState.MouseDown] = WinNpcEditor.OnListMouseDown;
            list.CallBack[(int)ControlState.MouseScroll] = () =>
            {
                int delta = GameClient.CurrentMouseState.ScrollWheelValue - GameClient.PreviousMouseState.ScrollWheelValue;
                if (delta != 0)
                {
                    int step = delta > 0 ? -1 : 1; // wheel up scrolls up
                    list.ScrollBy(step);

                    // Keep scrollbar in sync if present
                    if (WindowManager.TryGetControl("winNpcEditor", "sldList", out var sldNpc) && sldNpc is ScrollBar sbSync)
                    {
                        sbSync.Value = list.ScrollOffset;
                    }
                }
            };
        }
        if (WindowManager.TryGetControl("winNpcEditor", "sldList", out var sldNpcList) && sldNpcList is ScrollBar sbNpc)
        {
            sbNpc.CallBack[(int)ControlState.MouseMove] = () =>
            {
                if (npcList != null)
                {
                    npcList.ScrollOffset = sbNpc.Value;
                }
            };
        }

        // Hide redundant amount / chance textboxes: both are slider-only now.
        if (WindowManager.TryGetControl("winNpcEditor", "nudChance", out var chanceText))
        {
            chanceText.Visible = false;
        }
        if (WindowManager.TryGetControl("winNpcEditor", "nudAmount", out var amountText))
        {
            amountText.Visible = false;
        }

        // Text fields
        if (WindowManager.TryGetControl("winNpcEditor", "txtName", out var txtNameCtrl) && txtNameCtrl is TextBox txtName)
        {
            txtName.CallBack[(int)ControlState.KeyUp] = () => WinNpcEditor.UpdateName(txtName.Text ?? string.Empty);
        }

        if (WindowManager.TryGetControl("winNpcEditor", "txtAttackSay", out var atkCtrl) && atkCtrl is TextBox txtAtk)
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

        // Simple helper for combo apply on selection change (mirror map editor pattern)
        void BindCombo(string name, Action<int> apply)
        {
            if (WindowManager.TryGetControl("winNpcEditor", name, out var c) && c is ComboBox combo)
            {
                // Use MouseMove so we apply after the dropdown selection actually changes.
                combo.CallBack[(int)ControlState.MouseMove] = () =>
                {
                    int v = Math.Max(0, combo.Value);
                    apply(v);
                };
            }
        }

        BindCombo("cmbBehavior", v => { if (WinNpcEditor.SelectedIndex >= 0) { Data.Npc[WinNpcEditor.SelectedIndex].Behavior = (byte)v; GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true; } });
        BindCombo("cmbFaction", v => { if (WinNpcEditor.SelectedIndex >= 0) { Data.Npc[WinNpcEditor.SelectedIndex].Faction = (byte)v; GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true; } });
        BindCombo("cmbSpawnPeriod", v => { if (WinNpcEditor.SelectedIndex >= 0) { Data.Npc[WinNpcEditor.SelectedIndex].SpawnTime = (byte)v; GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true; } });
        BindCombo("cmbAnimation", v => { if (WinNpcEditor.SelectedIndex >= 0) { Data.Npc[WinNpcEditor.SelectedIndex].Animation = (byte)v; GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true; } });

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

        // Drop slot change reloads fields and immediately applies the stored item/amount/chance
        if (WindowManager.TryGetControl("winNpcEditor", "cmbNpcDropSlot", out var dropSlotCtrl) && dropSlotCtrl is ComboBox cmbSlot)
        {
            // When the user changes the slot selection in the dropdown, reload UI for that slot.
            cmbSlot.CallBack[(int)ControlState.MouseMove] = () =>
            {
                if (WinNpcEditor.SelectedIndex >= 0)
                {
                    WinNpcEditor.LoadNpc(WinNpcEditor.SelectedIndex);
                }
            };
        }

        // Sprite scrollbar: bind like other sliders so drag/wheel work
        BindScrollBar(
            "sldSprite",
            () => WinNpcEditor.SelectedIndex >= 0 ? Data.Npc[WinNpcEditor.SelectedIndex].Sprite : 1,
            v =>
            {
                if (WinNpcEditor.SelectedIndex >= 0 && WinNpcEditor.SelectedIndex < Variables.MaxNpcs)
                {
                    Data.Npc[WinNpcEditor.SelectedIndex].Sprite = v;
                    GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
                }
            });

        // Drop item combo - save selection for current slot when it changes
        if (WindowManager.TryGetControl("winNpcEditor", "cmbNpcDropItem", out var dropItemCtrl) && dropItemCtrl is ComboBox cmbItem)
        {
            int lastItemValue = cmbItem.Value;

            cmbItem.CallBack[(int)ControlState.MouseMove] = () =>
            {
                if (WinNpcEditor.IsLoading)
                    return;

                // Only act when the selected item actually changes
                if (cmbItem.Value == lastItemValue)
                    return;

                lastItemValue = cmbItem.Value;

                int slot = 0;
                if (WindowManager.TryGetControl("winNpcEditor", "cmbNpcDropSlot", out var ds) && ds is ComboBox s)
                    slot = Math.Clamp(s.Value, 0, 5);

                if (WinNpcEditor.SelectedIndex >= 0 &&
                    Data.Npc[WinNpcEditor.SelectedIndex].DropItem != null &&
                    slot < Data.Npc[WinNpcEditor.SelectedIndex].DropItem.Length)
                {
                    Data.Npc[WinNpcEditor.SelectedIndex].DropItem[slot] = Math.Clamp(cmbItem.Value, 0, Variables.MaxItems - 1);
                    GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
                }
            };
        }

        // Generic int textbox binder (used only when there is no paired slider)
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

        // Helper to bind a scrollbar (used as slider) to an int field and an optional textbox.
        void BindScrollBar(string name, Func<int> get, Action<int> apply, string textBoxName = null)
        {
            if (WindowManager.TryGetControl("winNpcEditor", name, out var c) && c is ScrollBar sb)
            {
                // Initialize from current data within existing XML-defined range
                int initial = get();
                int min = sb.Min;
                int max = sb.Max;
                sb.Value = Math.Clamp(initial, min, max);

                sb.CallBack[(int)ControlState.MouseMove] = () =>
                {
                    int v = Math.Clamp(sb.Value, min, max);
                    apply(v);

                    if (!string.IsNullOrEmpty(textBoxName) && WindowManager.TryGetControl("winNpcEditor", textBoxName, out var t) && t is TextBox tb)
                    {
                        tb.Text = v.ToString();
                    }
                };
            }
        }
        // Amount: textbox only
        BindIntText("txtNpcAmount", v =>
        {
            if (WinNpcEditor.IsLoading)
                return;

            int slot = 0;
            if (WindowManager.TryGetControl("winNpcEditor", "cmbNpcDropSlot", out var ds) && ds is ComboBox s)
                slot = Math.Clamp(s.Value, 0, 5);
            if (WinNpcEditor.SelectedIndex >= 0 &&
                Data.Npc[WinNpcEditor.SelectedIndex].DropItemValue != null &&
                slot < Data.Npc[WinNpcEditor.SelectedIndex].DropItemValue.Length)
            {
                Data.Npc[WinNpcEditor.SelectedIndex].DropItemValue[slot] = v;
                GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
            }
        }, 0, 999999);

        // Chance: slider only (no textbox sync).
        BindScrollBar(
            "sldNpcChance",
            () =>
            {
                if (WinNpcEditor.IsLoading)
                    return 0;

                if (WinNpcEditor.SelectedIndex < 0) return 0;
                int slot = 0;
                if (WindowManager.TryGetControl("winNpcEditor", "cmbNpcDropSlot", out var ds) && ds is ComboBox s)
                    slot = Math.Clamp(s.Value, 0, 5);
                var npc = Data.Npc[WinNpcEditor.SelectedIndex];
                return npc.DropChance != null && slot < npc.DropChance.Length ? npc.DropChance[slot] : 0;
            },
            v =>
            {
                if (WinNpcEditor.IsLoading)
                    return;

                int slot = 0;
                if (WindowManager.TryGetControl("winNpcEditor", "cmbNpcDropSlot", out var ds) && ds is ComboBox s)
                    slot = Math.Clamp(s.Value, 0, 5);
                if (WinNpcEditor.SelectedIndex >= 0 &&
                    Data.Npc[WinNpcEditor.SelectedIndex].DropChance != null &&
                    slot < Data.Npc[WinNpcEditor.SelectedIndex].DropChance.Length)
                {
                    Data.Npc[WinNpcEditor.SelectedIndex].DropChance[slot] = v;
                    GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
                }
            });

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

        // Sliders for stats (HP uses textbox only; no slider binding)

        BindScrollBar(
            "sldNpcDamage",
            () => WinNpcEditor.SelectedIndex >= 0 ? Data.Npc[WinNpcEditor.SelectedIndex].Damage : 0,
            v =>
            {
                if (WinNpcEditor.SelectedIndex >= 0 && WinNpcEditor.SelectedIndex < Variables.MaxNpcs)
                {
                    Data.Npc[WinNpcEditor.SelectedIndex].Damage = v;
                    GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
                }
            },
            "txtNpcDamage");

        // Range: slider-only, no textbox sync.
        BindScrollBar(
            "sldNpcRange",
            () => WinNpcEditor.SelectedIndex >= 0 ? Data.Npc[WinNpcEditor.SelectedIndex].Range : 0,
            v =>
            {
                if (WinNpcEditor.SelectedIndex >= 0 && WinNpcEditor.SelectedIndex < Variables.MaxNpcs)
                {
                    Data.Npc[WinNpcEditor.SelectedIndex].Range = (byte)v;
                    GameState.NpcChanged[WinNpcEditor.SelectedIndex] = true;
                }
            });

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
        if (WindowManager.TryGetControl("winNpcEditor", "btnCopy", out var btnCopy))
        {
            btnCopy.CallBack[(int)ControlState.MouseDown] = WinNpcEditor.OnCopyOrPaste;
        }
    }

    public void UpdateWindow_ItemEditor()
    {
        var window = WindowLoader.FromLayout("winItemEditor");

        // Close button
        if (WindowManager.TryGetControl("winItemEditor", "btnClose", out var btnClose))
        {
            btnClose.CallBack[(int)ControlState.MouseDown] = () => WindowManager.HideWindow("winItemEditor");
        }

        // Ensure item editor combos and icon picture are visible (some skins may hide them)
        // Sprite preview picture box draws item icon each frame
        if (WindowManager.TryGetControl("winItemEditor", "picIcon", out var picIconCtrl) && picIconCtrl is PictureBox picIcon)
        {
            picIcon.OnDraw = WinItemEditor.OnDrawIcon;
        }

        // Item list + mouse wheel
        ListBox itemList = null;
        if (WindowManager.TryGetControl("winItemEditor", "lstIndex", out var lstCtrl) && lstCtrl is ListBox list)
        {
            itemList = list;
            list.CallBack[(int)ControlState.MouseDown] = WinItemEditor.OnListMouseDown;
            list.CallBack[(int)ControlState.MouseScroll] = () =>
            {
                int delta = GameClient.CurrentMouseState.ScrollWheelValue - GameClient.PreviousMouseState.ScrollWheelValue;
                if (delta != 0)
                {
                    int step = delta > 0 ? -1 : 1;
                    list.ScrollBy(step);

                    // Keep scrollbar in sync if present
                    if (WindowManager.TryGetControl("winItemEditor", "sldList", out var sldCtrl) && sldCtrl is ScrollBar sbSync)
                    {
                        sbSync.Value = list.ScrollOffset;
                    }
                }
            };
        }
        if (WindowManager.TryGetControl("winItemEditor", "sldList", out var sldList) && sldList is ScrollBar sbList)
        {
            sbList.CallBack[(int)ControlState.MouseMove] = () =>
            {
                if (itemList != null)
                {
                    itemList.ScrollOffset = sbList.Value;
                }
            };
        }

        // Name textbox updates list entry
        if (WindowManager.TryGetControl("winItemEditor", "txtName", out var txtNameCtrl) && txtNameCtrl is TextBox txtName)
        {
            txtName.CallBack[(int)ControlState.KeyUp] = () => WinItemEditor.UpdateName(txtName.Text ?? string.Empty);
        }

        // Int binder
        void BindIntText(string name, Action<int> apply, int min, int max)
        {
            if (WindowManager.TryGetControl("winItemEditor", name, out var c) && c is TextBox tb)
            {
                tb.Enabled = true;
                tb.CallBack[(int)ControlState.KeyUp] = () =>
                {
                    var s = tb.Text?.Trim();
                    if (!int.TryParse(s, out var v)) v = min;
                    v = Math.Clamp(v, min, max);
                    apply(v);
                };
            }
        }

        // Basics
        if (WindowManager.TryGetControl("winItemEditor", "txtDesc", out var descCtrl) && descCtrl is TextBox txtDesc)
        {
            txtDesc.CallBack[(int)ControlState.KeyUp] = () =>
            {
                if (WinItemEditor.SelectedIndex >= 0 && WinItemEditor.SelectedIndex < Variables.MaxItems)
                {
                    Data.Item[WinItemEditor.SelectedIndex].Description = txtDesc.Text ?? string.Empty;
                    GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
                }
            };
        }

        // Icon scrollbar: update icon and redraw preview
        if (WindowManager.TryGetControl("winItemEditor", "sldIcon", out var iconScrollCtrl) && iconScrollCtrl is ScrollBar sldIcon)
        {
            sldIcon.CallBack[(int)ControlState.MouseMove] = () =>
            {
                if (WinItemEditor.SelectedIndex >= 0 && WinItemEditor.SelectedIndex < Variables.MaxItems)
                {
                    Data.Item[WinItemEditor.SelectedIndex].Icon = (short)sldIcon.Value;
                    GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
                }
            };
        }

        BindIntText("txtItemPaperdoll", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].Paperdoll = (short)v;
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        }, 0, GameState.NumPaperdolls);

        BindIntText("txtItemLevel", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].ItemLevel = (byte)Math.Clamp(v, 0, 255);
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        }, 0, 255);

        BindIntText("txtPrice", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].Price = v;
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        }, 0, int.MaxValue);

        BindIntText("txtItemRarity", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].Rarity = (byte)v;
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        }, 0, 5);

        if (WindowManager.TryGetControl("winItemEditor", "chkStackable", out var stackCtrl) && stackCtrl is CheckBox chkStack)
        {
            chkStack.CallBack[(int)ControlState.MouseDown] = () =>
            {
                if (WinItemEditor.SelectedIndex >= 0)
                {
                    Data.Item[WinItemEditor.SelectedIndex].Stackable = chkStack.Value == 1 ? (byte)1 : (byte)0;
                    GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
                }
            };
        }

        // Equipment & stats via sliders
        if (WindowManager.TryGetControl("winItemEditor", "sldDamage", out var dmgCtrl) && dmgCtrl is Client.Game.UI.Controls.ScrollBar sldDmg)
        {
            sldDmg.CallBack[(int)ControlState.MouseMove] = () =>
            {
                if (WinItemEditor.SelectedIndex >= 0 && WinItemEditor.SelectedIndex < Variables.MaxItems)
                {
                    Data.Item[WinItemEditor.SelectedIndex].Data2 = sldDmg.Value;
                    GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
                }
            };
        }

        if (WindowManager.TryGetControl("winItemEditor", "sldSpeed", out var spdCtrl) && spdCtrl is Client.Game.UI.Controls.ScrollBar sldSpeed)
        {
            sldSpeed.CallBack[(int)ControlState.MouseMove] = () =>
            {
                if (WinItemEditor.SelectedIndex >= 0 && WinItemEditor.SelectedIndex < Variables.MaxItems)
                {
                    Data.Item[WinItemEditor.SelectedIndex].Speed = sldSpeed.Value;
                    GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
                }
            };
        }

        if (WindowManager.TryGetControl("winItemEditor", "chkKnockback", out var kbCtrl) && kbCtrl is CheckBox chkKb)
        {
            chkKb.CallBack[(int)ControlState.MouseDown] = () =>
            {
                if (WinItemEditor.SelectedIndex >= 0)
                {
                    Data.Item[WinItemEditor.SelectedIndex].KnockBack = chkKb.Value == 1 ? (byte)1 : (byte)0;
                    GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
                }
            };
        }

        // Stat bonuses via sliders
        void BindStatSlider(string name, Func<byte> get, Action<byte> apply)
        {
            if (WindowManager.TryGetControl("winItemEditor", name, out var ctrl) && ctrl is Client.Game.UI.Controls.ScrollBar sld)
            {
                sld.CallBack[(int)ControlState.MouseMove] = () =>
                {
                    if (WinItemEditor.SelectedIndex >= 0 && WinItemEditor.SelectedIndex < Variables.MaxItems)
                    {
                        apply((byte)sld.Value);
                        GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
                    }
                };
            }
        }
        BindStatSlider("sldStr", () => Data.Item[WinItemEditor.SelectedIndex].AddStat[(int)Stat.Strength], v => Data.Item[WinItemEditor.SelectedIndex].AddStat[(int)Stat.Strength] = v);
        BindStatSlider("sldVit", () => Data.Item[WinItemEditor.SelectedIndex].AddStat[(int)Stat.Vitality], v => Data.Item[WinItemEditor.SelectedIndex].AddStat[(int)Stat.Vitality] = v);
        BindStatSlider("sldLuck", () => Data.Item[WinItemEditor.SelectedIndex].AddStat[(int)Stat.Luck], v => Data.Item[WinItemEditor.SelectedIndex].AddStat[(int)Stat.Luck] = v);
        BindStatSlider("sldInt", () => Data.Item[WinItemEditor.SelectedIndex].AddStat[(int)Stat.Intelligence], v => Data.Item[WinItemEditor.SelectedIndex].AddStat[(int)Stat.Intelligence] = v);
        BindStatSlider("sldSpr", () => Data.Item[WinItemEditor.SelectedIndex].AddStat[(int)Stat.Spirit], v => Data.Item[WinItemEditor.SelectedIndex].AddStat[(int)Stat.Spirit] = v);

        // Vital mod (Data1 for consumables)    
        BindIntText("txtVitalMod", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].Data1 = v;
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        }, -1000, 1000);

        // Event id / value reuse Data1 / Data2
        BindIntText("txtEventId", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].Data1 = v;
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        }, 0, int.MaxValue);

        BindIntText("txtEventValue", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].Data2 = v;
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        }, 0, int.MaxValue);

        // Requirements via sliders
        void BindReqStatSlider(string name, Func<byte> get, Action<byte> apply)
        {
            if (WindowManager.TryGetControl("winItemEditor", name, out var ctrl) && ctrl is Client.Game.UI.Controls.ScrollBar sld)
            {
                sld.CallBack[(int)ControlState.MouseMove] = () =>
                {
                    if (WinItemEditor.SelectedIndex >= 0 && WinItemEditor.SelectedIndex < Variables.MaxItems)
                    {
                        apply((byte)sld.Value);
                        GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
                    }
                };
            }
        }

        if (WindowManager.TryGetControl("winItemEditor", "sldReqLevel", out var rLvlCtrl) && rLvlCtrl is Client.Game.UI.Controls.ScrollBar sldReqLevel)
        {
            sldReqLevel.CallBack[(int)ControlState.MouseMove] = () =>
            {
                if (WinItemEditor.SelectedIndex >= 0 && WinItemEditor.SelectedIndex < Variables.MaxItems)
                {
                    Data.Item[WinItemEditor.SelectedIndex].LevelReq = (byte)sldReqLevel.Value;
                    GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
                }
            };
        }

        BindReqStatSlider("sldReqStr", () => Data.Item[WinItemEditor.SelectedIndex].StatReq[(int)Stat.Strength], v => Data.Item[WinItemEditor.SelectedIndex].StatReq[(int)Stat.Strength] = v);
        BindReqStatSlider("sldReqVit", () => Data.Item[WinItemEditor.SelectedIndex].StatReq[(int)Stat.Vitality], v => Data.Item[WinItemEditor.SelectedIndex].StatReq[(int)Stat.Vitality] = v);
        BindReqStatSlider("sldReqLuck", () => Data.Item[WinItemEditor.SelectedIndex].StatReq[(int)Stat.Luck], v => Data.Item[WinItemEditor.SelectedIndex].StatReq[(int)Stat.Luck] = v);
        BindReqStatSlider("sldReqInt", () => Data.Item[WinItemEditor.SelectedIndex].StatReq[(int)Stat.Intelligence], v => Data.Item[WinItemEditor.SelectedIndex].StatReq[(int)Stat.Intelligence] = v);
        BindReqStatSlider("sldReqSpr", () => Data.Item[WinItemEditor.SelectedIndex].StatReq[(int)Stat.Spirit], v => Data.Item[WinItemEditor.SelectedIndex].StatReq[(int)Stat.Spirit] = v);

        // Combos: Type, SubType, Animation, Bind, Tool, Knockback tiles, Skill, Projectile, Ammo, JobReq, AccessReq
        void BindCombo(string name, Action<int> apply)
        {
            if (WindowManager.TryGetControl("winItemEditor", name, out var c) && c is ComboBox combo)
            {
                combo.Enabled = true;
                combo.CallBack[(int)ControlState.MouseMove] = () =>
                {
                    int v = Math.Max(0, combo.Value);
                    apply(v);
                };
            }
        }

        BindCombo("cmbItemType", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].Type = (byte)v;
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        });

        BindCombo("cmbSubType", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].SubType = (byte)v;
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        });

        BindCombo("cmbAnimation", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].Animation = (byte)v;
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        });

        BindCombo("cmbBind", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].BindType = (byte)v;
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        });

        BindCombo("cmbTool", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].Data3 = (byte)v;
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        });

        BindCombo("cmbKnockBackTiles", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].KnockBackTiles = (byte)v;
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        });

        BindCombo("cmbSkill", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].Data1 = v;
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        });

        BindCombo("cmbProjectile", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].Projectile = (short)(v - 1);
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        });

        BindCombo("cmbAmmo", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].Ammo = (short)(v - 1);
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        });

        BindCombo("cmbJobReq", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].JobReq = (byte)v;
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        });

        BindCombo("cmbAccessReq", v =>
        {
            if (WinItemEditor.SelectedIndex >= 0)
            {
                Data.Item[WinItemEditor.SelectedIndex].AccessReq = (byte)v;
                GameState.ItemChanged[WinItemEditor.SelectedIndex] = true;
            }
        });

        // Buttons
        if (WindowManager.TryGetControl("winItemEditor", "btnSave", out var btnSave))
        {
            btnSave.CallBack[(int)ControlState.MouseDown] = () => { Editors.ItemEditorOK(); WindowManager.HideWindow("winItemEditor"); };
        }
        if (WindowManager.TryGetControl("winItemEditor", "btnCancel", out var btnCancel))
        {
            btnCancel.CallBack[(int)ControlState.MouseDown] = () => { Editors.ItemEditorCancel(); WindowManager.HideWindow("winItemEditor"); };
        }
        if (WindowManager.TryGetControl("winItemEditor", "btnDelete", out var btnDelete))
        {
            btnDelete.CallBack[(int)ControlState.MouseDown] = () =>
            {
                Item.ClearItem(GameState.EditorIndex);
                WinItemEditor.LoadItem(GameState.EditorIndex);
            };
        }
        if (WindowManager.TryGetControl("winItemEditor", "btnCopy", out var btnCopy))
        {
            btnCopy.CallBack[(int)ControlState.MouseDown] = WinItemEditor.OnCopyOrPaste;
        }
        if (WindowManager.TryGetControl("winItemEditor", "btnSpawn", out var btnSpawn))
        {
            btnSpawn.CallBack[(int)ControlState.MouseDown] = () =>
            {
                if (GameState.MyIndex > 0)
                {
                    // Reuse existing spawn packet
                    Sender.SendSpawnItem(GameState.EditorIndex, 1);
                }
            };
        }
    }

    public void UpdateWindow_EscMenu()
    {
        var window = WindowLoader.FromLayout("winEscMenu");
        window.GetChild("btnReturn").CallBack[(int)ControlState.MouseDown] = WinEscMenu.OnClose;
        window.GetChild("btnOptions").CallBack[(int)ControlState.MouseDown] = WinEscMenu.OnOptionsClick;
        window.GetChild("btnMainMenu").CallBack[(int)ControlState.MouseDown] = WinEscMenu.OnMainMenuClick;
        window.GetChild("btnExit").CallBack[(int)ControlState.MouseDown] = WinEscMenu.OnExitClick;
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
        window.CallBack[(int)ControlState.MouseMove] = WinHotBar.OnMouseMove;
        window.CallBack[(int)ControlState.MouseDown] = WinHotBar.OnMouseDown;
        window.CallBack[(int)ControlState.DoubleClick] = WinHotBar.OnDoubleClick;
    }

    public void UpdateWindow_Menu()
    {
        var window = WindowLoader.FromLayout("winMenu");
        window.GetChild("btnChar").CallBack[(int)ControlState.MouseDown] = WinMenu.OnCharacterClick;
        window.GetChild("btnInv").CallBack[(int)ControlState.MouseDown] = WinMenu.OnInventoryClick;
        window.GetChild("btnSkills").CallBack[(int)ControlState.MouseDown] = WinMenu.OnSkillsClick;
        window.GetChild("btnMap").CallBack[(int)ControlState.MouseDown] = WinMenu.OnMapClick;
        window.GetChild("btnGuild").CallBack[(int)ControlState.MouseDown] = WinMenu.OnGuildClick;
        window.GetChild("btnQuest").CallBack[(int)ControlState.MouseDown] = WinMenu.OnQuestClick;
    }

    public void UpdateWindow_Inventory()
    {
        var window = WindowLoader.FromLayout("winInventory");
        window.OnDraw = WinInventory.OnDraw;
        window.CallBack[(int)ControlState.MouseMove] = WinInventory.OnMouseMove;
        window.CallBack[(int)ControlState.MouseDown] = WinInventory.OnMouseDown;
        window.CallBack[(int)ControlState.DoubleClick] = WinInventory.OnDoubleClick;
        window.GetChild("btnClose").CallBack[(int)ControlState.MouseDown] = WinMenu.OnInventoryClick;
    }

    public void UpdateWindow_Character()
    {
        var window = WindowLoader.FromLayout("winCharacter");
        window.OnDraw = WinCharacter.OnDrawCharacter;
        window.CallBack[(int)ControlState.MouseMove] = WinCharacter.OnMouseMove;
        window.CallBack[(int)ControlState.MouseDown] = WinCharacter.OnMouseMove;
        window.CallBack[(int)ControlState.DoubleClick] = WinCharacter.OnDoubleClick;
        window.GetChild("btnClose").CallBack[(int)ControlState.MouseDown] = WinMenu.OnCharacterClick;
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
        window.CallBack[(int)ControlState.MouseDown] = WinPlayerMenu.OnClose;
    }

    public void UpdateWindow_PlayerMenu()
    {
        var window = WindowLoader.FromLayout("winPlayerMenu");
        window.CallBack[(int)ControlState.MouseDown] = WinPlayerMenu.OnClose;
        window.GetChild("btnName").CallBack[(int)ControlState.MouseDown] = WinPlayerMenu.OnClose;
        window.GetChild("btnParty").CallBack[(int)ControlState.MouseDown] = WinPlayerMenu.OnPartyInvite;
        window.GetChild("btnTrade").CallBack[(int)ControlState.MouseDown] = WinPlayerMenu.OnTradeRequest;
        window.GetChild("btnGuild").CallBack[(int)ControlState.MouseDown] = WinPlayerMenu.OnGuildInvite;
        window.GetChild("btnPM").CallBack[(int)ControlState.MouseDown] = WinPlayerMenu.OnPrivateMessage;
    }

    public void UpdateWindow_DragBox()
    {
        var window = WindowLoader.FromLayout("winDragBox");
        window.OnDraw = WinDragBox.OnDraw;
        window.CallBack[(int)ControlState.MouseUp] = WinDragBox.DragBox_Check;
    }

    public void UpdateWindow_Options()
    {
        var window = WindowLoader.FromLayout("winOptions");
        // Wire Confirm button
        window.GetChild("btnConfirm").CallBack[(int)ControlState.MouseDown] = WinOptions.OnConfirm;

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
        bg.CallBack[(int)ControlState.DoubleClick] = WinComboMenu.Close;

        WindowLoader.FromLayout("winComboMenu");
    }

    public void UpdateWindow_Skills()
    {
        var window = WindowLoader.FromLayout("winSkills");
        window.OnDraw = WinSkills.OnDraw;
        window.CallBack[(int)ControlState.MouseMove] = WinSkills.OnMouseMove;
        window.CallBack[(int)ControlState.MouseDown] = WinSkills.OnMouseDown;
        window.CallBack[(int)ControlState.DoubleClick] = WinSkills.OnDoubleClick;
        window.GetChild("btnClose").CallBack[(int)ControlState.MouseDown] = WinMenu.OnSkillsClick;
    }

    public void UpdateWindow_Bank()
    {
        var window = WindowLoader.FromLayout("winBank");
        window.OnDraw = WinBank.OnDraw;
        window.CallBack[(int)ControlState.MouseMove] = WinBank.OnMouseMove;
        window.CallBack[(int)ControlState.MouseDown] = WinBank.OnMouseDown;
        window.CallBack[(int)ControlState.DoubleClick] = WinBank.OnDoubleClick;
        window.GetChild("btnClose").CallBack[(int)ControlState.MouseDown] = WinBank.OnClose;
    }

    public void UpdateWindow_Shop()
    {
        var window = WindowLoader.FromLayout("winShop");

        window.OnDraw = WinShop.OnDrawBackground;
        window.CallBack[(int)ControlState.MouseMove] = WinShop.OnMouseMove;
        window.CallBack[(int)ControlState.MouseDown] = WinShop.OnMouseDown;
        window.GetChild("btnClose").CallBack[(int)ControlState.MouseDown] = WinShop.OnClose;
        window.GetChild("picParchment").OnDraw = WinShop.OnDraw;
        window.GetChild("btnBuy").CallBack[(int)ControlState.MouseDown] = WinShop.OnBuy;
        window.GetChild("btnSell").CallBack[(int)ControlState.MouseDown] = WinShop.OnSell;
        window.GetChild("CheckboxBuying").CallBack[(int)ControlState.MouseDown] = WinShop.OnBuyingChecked;
        window.GetChild("CheckboxSelling").CallBack[(int)ControlState.MouseDown] = WinShop.OnSellingChecked;
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
        var playerName = GetPlayerName(GameState.MyIndex);
        var txtName = window.GetChild("txtName");
        txtName.Text = playerName;

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
        window.GetChild("txtSprite").Text = window.GetChild("txtSprite").Text?.Length > 0 ? window.GetChild("txtSprite").Text : "0";

        // Wire Moderation actions
        window.GetChild("btnWarpTo").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            var mapNum = ReadInt(window.GetChild("txtAdminMap")) + 1;
            Sender.WarpTo(mapNum);
        };

        window.GetChild("btnBan").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            var name = txtName.Text?.Trim() ?? string.Empty;
            Sender.SendBan(name);
        };

        window.GetChild("btnKick").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            var name = txtName.Text?.Trim() ?? string.Empty;
            Sender.SendKick(name);
        };

        window.GetChild("btnWarp2Me").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            var name = txtName.Text?.Trim() ?? string.Empty;
            if (!IsNumeric(name)) Sender.WarpToMe(name);
        };

        window.GetChild("btnWarpMe2").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            var name = txtName.Text?.Trim() ?? string.Empty;
            if (!IsNumeric(name)) Sender.WarpMeTo(name);
        };

        window.GetChild("btnSetAccess").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Owner)) { ShowDenied(); return; }
            var name = txtName.Text?.Trim() ?? string.Empty;
            if (IsNumeric(name)) return;
            if (window.GetChild("cmbAccess") is ComboBox combo && combo.Value >= 0)
            {
                Sender.SendSetAccess(name, (byte)(combo.Value + 1));
            }
        };

        window.GetChild("btnSetSprite").CallBack[(int)ControlState.MouseDown] = () =>
        {
            if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
            var sprite = ReadInt(window.GetChild("txtSprite"));
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

        WireScrollableList("winAdmin", "lstMaps", "sldMapList");

        if (WindowManager.TryGetControl("winAdmin", "lstMaps", out var lstCtrl) && lstCtrl is ListBox lstMaps)
        {
            // Double-click to warp to selected map
            lstMaps.CallBack[(int)ControlState.DoubleClick] = () =>
            {
                if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
                int idx = lstMaps.SelectedIndex;
                if (idx < 0 || idx >= lstMaps.Items.Count) return;
                var line = lstMaps.Items[idx] ?? string.Empty;
                var colon = line.IndexOf(':');
                if (colon > 0 && int.TryParse(line.AsSpan(0, colon), out var mapNum))
                {
                    var target = Math.Max(0, mapNum - 1); // display is 1-based; engine expects 0-based
                    Sender.WarpTo(target);
                }
            };
        }

        // Warp button: prefer selected item; fallback to top visible line
        if (WindowManager.TryGetControl("winAdmin", "btnMapWarp", out var btnMapWarp2))
        {
            btnMapWarp2.CallBack[(int)ControlState.MouseDown] = () =>
            {
                if (!HasAccess(AccessLevel.Mapper)) { ShowDenied(); return; }
                if (WindowManager.TryGetControl("winAdmin", "lstMaps", out var listCtrl) && listCtrl is ListBox lst)
                {
                    string line = string.Empty;
                    if (lst.SelectedIndex >= 0 && lst.SelectedIndex < lst.Items.Count)
                    {
                        line = lst.Items[lst.SelectedIndex] ?? string.Empty;
                    }
                    else
                    {
                        var lines = (lst.Text ?? string.Empty).Split('\n');
                        if (lines.Length == 0) return;
                        var start = Math.Clamp(lst.Value, 0, Math.Max(0, lines.Length - 1));
                        line = lines[start];
                    }
                    if (string.IsNullOrWhiteSpace(line)) return;
                    var colon = line.IndexOf(':');
                    if (colon > 0 && int.TryParse(line.AsSpan(0, colon), out var mapNum))
                    {
                        var target = Math.Max(0, mapNum - 1); // convert 1-based display to 0-based map id
                        Sender.WarpTo(target);
                    }
                }
            };
        }

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

        void ShowTab(string tab)
        {
            SetGroupBoxVisible("winAdmin", "grpModeration", false);
            SetGroupBoxVisible("winAdmin", "grpMapList", false);
            SetGroupBoxVisible("winAdmin", "grpMapTools", false);
            SetGroupBoxVisible("winAdmin", "grpEditors", false);

            // Show selected
            switch (tab)
            {
                case "Moderation": SetGroupBoxVisible("winAdmin", "grpModeration", true); break;
                case "MapList": SetGroupBoxVisible("winAdmin", "grpMapList", true); break;
                case "MapTools": SetGroupBoxVisible("winAdmin", "grpMapTools", true); break;
                case "Editors": SetGroupBoxVisible("winAdmin", "grpEditors", true); break;
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
                font: Font.Georgia,
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
    public void UpdateWindow_ShopEditor()
    {
        var window = WindowLoader.FromLayout("winShopEditor");

        // List interactions
        if (WindowManager.TryGetControl("winShopEditor", "lstIndex", out var lstCtrl) && lstCtrl is ListBox lstIndex)
        {
            lstIndex.CallBack[(int)ControlState.MouseDown] = WinShopEditor.OnListMouseDown;
            // Wheel scroll sync with scrollbar
            if (WindowManager.TryGetControl("winShopEditor", "sldList", out var sldTradeList) && sldTradeList is ScrollBar sbSync)
            {
                lstIndex.CallBack[(int)ControlState.MouseScroll] = () =>
                {
                    int delta = GameClient.CurrentMouseState.ScrollWheelValue - GameClient.PreviousMouseState.ScrollWheelValue;
                    if (delta == 0) return;
                    int step = delta > 0 ? -1 : 1;
                    sbSync.Value = Math.Clamp(sbSync.Value + step, sbSync.Min, sbSync.Max);
                    lstIndex.ScrollOffset = sbSync.Value;
                    lstIndex.EnsureVisible(lstIndex.SelectedIndex);
                };
                sbSync.CallBack[(int)ControlState.MouseMove] = () =>
                {
                    lstIndex.ScrollOffset = sbSync.Value;
                    lstIndex.EnsureVisible(lstIndex.SelectedIndex);
                };
            }
        }

        // Trade list interactions
        WireScrollableList("winShopEditor", "lstIndex", "sldList");
        WireScrollableList("winShopEditor", "lstTradeItem", "sldTradeList");

        // Name textbox
        if (WindowManager.TryGetControl("winShopEditor", "txtName", out var nameCtrl) && nameCtrl is TextBox txtName)
        {
            txtName.CallBack[(int)ControlState.KeyUp] = () =>
            {
                if (WinShopEditor.SelectedIndex < 0 || WinShopEditor.SelectedIndex >= Variables.MaxShops) return;
                Data.Shop[WinShopEditor.SelectedIndex].Name = txtName.Text ?? string.Empty;
                GameState.ShopChanged[WinShopEditor.SelectedIndex] = true;
                WinShopEditor.RefreshList();
            };
        }

        // Buy rate textbox
        if (WindowManager.TryGetControl("winShopEditor", "txtBuy", out var buyCtrl) && buyCtrl is TextBox txtBuy)
        {
            txtBuy.CallBack[(int)ControlState.KeyUp] = () =>
            {
                if (WinShopEditor.SelectedIndex < 0 || WinShopEditor.SelectedIndex >= Variables.MaxShops) return;
                if (int.TryParse(txtBuy.Text, out var rate))
                {
                    Data.Shop[WinShopEditor.SelectedIndex].BuyRate = rate;
                    GameState.ShopChanged[WinShopEditor.SelectedIndex] = true;
                }
            };
        }

        // Update trade button
        if (WindowManager.TryGetControl("winShopEditor", "btnUpdateTrade", out var updCtrl) && updCtrl is Button btnUpdate)
        {
            btnUpdate.CallBack[(int)ControlState.MouseDown] = () =>
            {
                if (WinShopEditor.SelectedIndex < 0 || WinShopEditor.SelectedIndex >= Variables.MaxShops) return;
                if (!WindowManager.TryGetControl("winShopEditor", "lstTradeItem", out var tradeListCtrl) || tradeListCtrl is not ListBox lst) return;
                int idx = lst.SelectedIndex;
                if (idx < 0 || idx >= Variables.MaxTrades) return;
                ref var trade = ref Data.Shop[WinShopEditor.SelectedIndex].TradeItem[idx];
                if (WindowManager.TryGetControl("winShopEditor", "cmbItem", out var itCtrl) && itCtrl is ComboBox ci)
                    trade.Item = Math.Clamp(ci.Value, -1, Variables.MaxItems - 1);
                if (WindowManager.TryGetControl("winShopEditor", "cmbCostItem", out var cCtrl) && cCtrl is ComboBox cc)
                    trade.CostItem = Math.Clamp(cc.Value, -1, Variables.MaxItems - 1);
                if (WindowManager.TryGetControl("winShopEditor", "txtItemValue", out var iqCtrl) && iqCtrl is TextBox txtIQ && int.TryParse(txtIQ.Text, out var itemQty))
                    trade.ItemValue = Math.Max(0, itemQty);
                if (WindowManager.TryGetControl("winShopEditor", "txtCostValue", out var cqCtrl) && cqCtrl is TextBox txtCQ && int.TryParse(txtCQ.Text, out var costQty))
                    trade.CostValue = Math.Max(0, costQty);
                GameState.ShopChanged[WinShopEditor.SelectedIndex] = true;
                WinShopEditor.LoadShop(WinShopEditor.SelectedIndex);
            };
        }

        // Delete trade button
        if (WindowManager.TryGetControl("winShopEditor", "btnDeleteTrade", out var delCtrl) && delCtrl is Button btnDeleteTrade)
        {
            btnDeleteTrade.CallBack[(int)ControlState.MouseDown] = () =>
            {
                if (WinShopEditor.SelectedIndex < 0 || WinShopEditor.SelectedIndex >= Variables.MaxShops) return;
                if (!WindowManager.TryGetControl("winShopEditor", "lstTradeItem", out var tradeListCtrl) || tradeListCtrl is not ListBox lst) return;
                int idx = lst.SelectedIndex;
                if (idx < 0 || idx >= Variables.MaxTrades) return;
                ref var trade = ref Data.Shop[WinShopEditor.SelectedIndex].TradeItem[idx];
                trade.Item = -1; trade.ItemValue = 0; trade.CostItem = -1; trade.CostValue = 0;
                GameState.ShopChanged[WinShopEditor.SelectedIndex] = true;
                WinShopEditor.LoadShop(WinShopEditor.SelectedIndex);
            };
        }

        // Copy button
        if (WindowManager.TryGetControl("winShopEditor", "btnCopy", out var copyCtrl) && copyCtrl is Button btnCopy)
        {
            btnCopy.CallBack[(int)ControlState.MouseDown] = () => { WinShopEditor.OnCopyOrPaste(); };
        }

        // Save / Cancel / Delete / Close
        if (WindowManager.TryGetControl("winShopEditor", "btnSave", out var saveCtrl) && saveCtrl is Button btnSave)
        {
            btnSave.CallBack[(int)ControlState.MouseDown] = () => { Editors.ShopEditorOK(); WindowManager.HideWindow("winShopEditor"); };
        }
        if (WindowManager.TryGetControl("winShopEditor", "btnCancel", out var cancelCtrl) && cancelCtrl is Button btnCancel)
        {
            btnCancel.CallBack[(int)ControlState.MouseDown] = () => { Editors.ShopEditorCancel(); WindowManager.HideWindow("winShopEditor"); };
        }
        if (WindowManager.TryGetControl("winShopEditor", "btnDelete", out var delShopCtrl) && delShopCtrl is Button btnDelete)
        {
            btnDelete.CallBack[(int)ControlState.MouseDown] = () =>
            {
                Shop.ClearShop(WinShopEditor.SelectedIndex);
                GameState.ShopChanged[WinShopEditor.SelectedIndex] = true;
                WinShopEditor.LoadShop(WinShopEditor.SelectedIndex);
                WinShopEditor.RefreshList();
            };
        }
        if (WindowManager.TryGetControl("winShopEditor", "btnClose", out var closeCtrl) && closeCtrl is Button btnClose)
        {
            btnClose.CallBack[(int)ControlState.MouseDown] = () => Editors.ShopEditorCancel(); WindowManager.HideWindow("winShopEditor");
        }
    }

    public void UpdateWindow_JobEditor()
    {
        var window = WindowLoader.FromLayout("winJobEditor");

        // Close
        if (WindowManager.TryGetControl("winJobEditor", "btnClose", out var btnClose))
            btnClose.CallBack[(int)ControlState.MouseDown] = () => WindowManager.HideWindow("winJobEditor");

        // Sprite previews
        if (WindowManager.TryGetControl("winJobEditor", "picMale", out var picMaleCtrl) && picMaleCtrl is PictureBox picMale)
            picMale.OnDraw = WinJobEditor.OnDrawMaleSprite;
        if (WindowManager.TryGetControl("winJobEditor", "picFemale", out var picFemaleCtrl) && picFemaleCtrl is PictureBox picFemale)
            picFemale.OnDraw = WinJobEditor.OnDrawFemaleSprite;

        // Set item slot
        if (WindowManager.TryGetControl("winJobEditor", "lstStartItems", out var lsCtrl) && lsCtrl is ListBox lstStartItems)
        {
            if (WindowManager.TryGetControl("winJobEditor", "btnSetItem", out var btnSetItem))
            {
                btnSetItem.Enabled = true;
                btnSetItem.CallBack[(int)ControlState.MouseDown] = () =>
                {
                    if (lstStartItems is null) return;
                    int slot = lstStartItems.SelectedIndex;
                    if (slot < 0 || slot >= Variables.MaxStartItems) return;
                    int item = -1, amt = 1;
                    if (WindowManager.TryGetControl("winJobEditor", "cmbItem", out var ic) && ic is ComboBox cmb)
                        item = cmb.Value <= 0 ? -1 : Math.Clamp(cmb.Value - 1, 0, Variables.MaxItems - 1);
                    if (WindowManager.TryGetControl("winJobEditor", "txtItemValue", out var ac) && ac is TextBox t && int.TryParse(t.Text, out var parsed))
                        amt = Math.Max(1, parsed);
                    if (WinJobEditor.SelectedIndex >= 0)
                    {
                        Data.Job[WinJobEditor.SelectedIndex].StartItem[slot] = item;
                        Data.Job[WinJobEditor.SelectedIndex].StartValue[slot] = amt;
                        GameState.JobChanged[WinJobEditor.SelectedIndex] = true;
                        WinJobEditor.LoadJob(WinJobEditor.SelectedIndex);
                    }
                };
            }
        }

        if (WindowManager.TryGetControl("winJobEditor", "lstStartSkills", out var lstStartSkillsCtrl) && lstStartSkillsCtrl is ListBox lstSkills)
        {
            // Set skill slot
            if (WindowManager.TryGetControl("winJobEditor", "btnSetSkill", out var btnSetSkill))
            {
                btnSetSkill.Enabled = true;
                btnSetSkill.CallBack[(int)ControlState.MouseDown] = () =>
                {
                    if (lstSkills is null) return;
                    int slot = lstSkills.SelectedIndex;
                    if (slot < 0 || slot >= Variables.MaxStartSkills) return;
                    int skill = -1;
                    if (WindowManager.TryGetControl("winJobEditor", "cmbSkill", out var skc) && skc is ComboBox cs)
                        skill = cs.Value <= 0 ? -1 : Math.Clamp(cs.Value - 1, 0, Variables.MaxSkills - 1);
                    if (WinJobEditor.SelectedIndex >= 0)
                    {
                        Data.Job[WinJobEditor.SelectedIndex].StartSkill[slot] = skill;
                        GameState.JobChanged[WinJobEditor.SelectedIndex] = true;
                        WinJobEditor.LoadJob(WinJobEditor.SelectedIndex);
                    }
                };
            }
        }

        // Start Items / Start Skills / lstIndex: unified wiring
        WireScrollableList("winJobEditor", "lstIndex", "sldList");
        WireScrollableList("winJobEditor", "lstStartItems", "sldStartItems");
        WireScrollableList("winJobEditor", "lstStartSkills", "sldStartSkills");

        // Index list: keep Job-specific click, unify wheel/drag via helper
        if (WindowManager.TryGetControl("winJobEditor", "lstIndex", out var lstCtrl) && lstCtrl is ListBox lstIndex)
            lstIndex.CallBack[(int)ControlState.MouseDown] = WinJobEditor.OnListMouseDown;

        // Text boxes
        if (WindowManager.TryGetControl("winJobEditor", "txtName", out var txtNameCtrl) && txtNameCtrl is TextBox txtName)
        {
            txtName.Enabled = true;
            txtName.CallBack[(int)ControlState.KeyUp] = () =>
            {
                if (WinJobEditor.SelectedIndex < 0 || WinJobEditor.SelectedIndex >= Variables.MaxJobs) return;
                Data.Job[WinJobEditor.SelectedIndex].Name = txtName.Text ?? string.Empty;
                GameState.JobChanged[WinJobEditor.SelectedIndex] = true;
                WinJobEditor.RefreshList();
            };
        }

        if (WindowManager.TryGetControl("winJobEditor", "txtDesc", out var txtDescCtrl) && txtDescCtrl is TextBox txtDesc)
        {
            txtDesc.Enabled = true;
            txtDesc.CallBack[(int)ControlState.KeyUp] = () =>
            {
                if (WinJobEditor.SelectedIndex < 0 || WinJobEditor.SelectedIndex >= Variables.MaxJobs) return;
                Data.Job[WinJobEditor.SelectedIndex].Desc = txtDesc.Text ?? string.Empty;
                GameState.JobChanged[WinJobEditor.SelectedIndex] = true;
            };
        }

        // Int binder
        void BindIntText(string name, Action<int> apply, int min, int max)
        {
            if (WindowManager.TryGetControl("winJobEditor", name, out var c) && c is TextBox tb)
            {
                tb.Enabled = true;
                tb.CallBack[(int)ControlState.KeyUp] = () =>
                {
                    var s = tb.Text?.Trim();
                    if (!int.TryParse(s, out var v)) v = min;
                    v = Math.Clamp(v, min, max);
                    apply(v);
                };
            }
        }
        BindIntText("txtStartMap", v => { if (WinJobEditor.SelectedIndex >= 0) { Data.Job[WinJobEditor.SelectedIndex].StartMap = v; GameState.JobChanged[WinJobEditor.SelectedIndex] = true; } }, 0, int.MaxValue);
        BindIntText("txtStartX", v => { if (WinJobEditor.SelectedIndex >= 0) { Data.Job[WinJobEditor.SelectedIndex].StartX = (byte)Math.Clamp(v, 0, 255); GameState.JobChanged[WinJobEditor.SelectedIndex] = true; } }, 0, 255);
        BindIntText("txtStartY", v => { if (WinJobEditor.SelectedIndex >= 0) { Data.Job[WinJobEditor.SelectedIndex].StartY = (byte)Math.Clamp(v, 0, 255); GameState.JobChanged[WinJobEditor.SelectedIndex] = true; } }, 0, 255);

        // Sprite bars
        void BindSpriteBar(string name, Func<int> get, Action<int> apply)
        {
            if (WindowManager.TryGetControl("winJobEditor", name, out var c) && c is ScrollBar sb)
            {
                sb.Enabled = true;
                sb.Max = Math.Max(sb.Max, GameState.NumCharacters);
                sb.Value = Math.Clamp(get(), sb.Min, sb.Max);
                sb.CallBack[(int)ControlState.MouseMove] = () =>
                {
                    int v = Math.Clamp(sb.Value, sb.Min, sb.Max);
                    apply(v);
                    if (WinJobEditor.SelectedIndex >= 0) GameState.JobChanged[WinJobEditor.SelectedIndex] = true;
                };
            }
        }
        BindSpriteBar("sldMaleSprite", () => WinJobEditor.SelectedIndex >= 0 ? Data.Job[WinJobEditor.SelectedIndex].MaleSprite : 0,
            v => { if (WinJobEditor.SelectedIndex >= 0) Data.Job[WinJobEditor.SelectedIndex].MaleSprite = v; });
        BindSpriteBar("sldFemaleSprite", () => WinJobEditor.SelectedIndex >= 0 ? Data.Job[WinJobEditor.SelectedIndex].FemaleSprite : 0,
            v => { if (WinJobEditor.SelectedIndex >= 0) Data.Job[WinJobEditor.SelectedIndex].FemaleSprite = v; });

        // Remaining buttons
        if (WindowManager.TryGetControl("winJobEditor", "btnSave", out var btnSave))
            btnSave.CallBack[(int)ControlState.MouseDown] = () => { Editors.JobEditorOK(); WindowManager.HideWindow("winJobEditor"); };
        if (WindowManager.TryGetControl("winJobEditor", "btnCancel", out var btnCancel))
            btnCancel.CallBack[(int)ControlState.MouseDown] = () => { Editors.JobEditorCancel(); WindowManager.HideWindow("winJobEditor"); };
        if (WindowManager.TryGetControl("winJobEditor", "btnDelete", out var btnDelete))
        {
            btnDelete.CallBack[(int)ControlState.MouseDown] = () =>
            {
                if (WinJobEditor.SelectedIndex < 0) return;
                Database.ClearJob(WinJobEditor.SelectedIndex);
                GameState.JobChanged[WinJobEditor.SelectedIndex] = true;
                WinJobEditor.LoadJob(WinJobEditor.SelectedIndex);
                WinJobEditor.RefreshList();
            };
        }
        if (WindowManager.TryGetControl("winJobEditor", "btnCopy", out var btnCopy))
            btnCopy.CallBack[(int)ControlState.MouseDown] = WinJobEditor.OnCopyOrPaste;
    }

    public void WireScrollableList(string windowName, string listName, string barName)
    {
        if (!WindowManager.TryGetControl(windowName, listName, out var listCtrl) || listCtrl is not ListBox lb)
            return;

        ScrollBar sb = null;
        if (WindowManager.TryGetControl(windowName, barName, out var barCtrl) && barCtrl is ScrollBar sbar)
            sb = sbar;

        // Click selects item (window-relative). Only clicks change selection.
        lb.CallBack[(int)ControlState.MouseDown] = () =>
        {
            var win = WindowManager.GetWindowByName(windowName);
            if (win is null) return;
            int relY = GameClient.CurrentMouseState.Y - (win.Y + lb.Y);
            int idx = lb.GetItemIndexAtPosition(relY);
            if (idx >= 0 && idx < lb.Items.Count)
            {
                lb.SelectedIndex = idx;
                lb.EnsureVisible(idx);

                // Keep scrollbar aligned after EnsureVisible
                if (sb != null)
                {
                    int visible = lb.GetVisibleCount();
                    int maxScroll = Math.Max(0, lb.Items.Count - visible);
                    sb.Min = 0; sb.Max = maxScroll;
                    sb.Value = Math.Clamp(lb.ScrollOffset, sb.Min, sb.Max);
                }
            }
        };

        // Mouse wheel: scroll list, then sync scrollbar range/value. Do NOT change SelectedIndex.
        lb.CallBack[(int)ControlState.MouseScroll] = () =>
        {
            int delta = GameClient.CurrentMouseState.ScrollWheelValue - GameClient.PreviousMouseState.ScrollWheelValue;
            if (delta == 0) return;
            int step = delta > 0 ? -1 : 1;

            lb.ScrollBy(step);

            if (sb != null)
            {
                int visible = lb.GetVisibleCount();
                int maxScroll = Math.Max(0, lb.Items.Count - visible);
                sb.Min = 0; sb.Max = maxScroll;
                sb.Value = Math.Clamp(lb.ScrollOffset, sb.Min, sb.Max);
            }
        };

        // Scrollbar drag: clamp within range and assign to list. Do NOT change SelectedIndex.
        if (sb != null)
        {
            sb.CallBack[(int)ControlState.MouseMove] = () =>
            {
                int visible = lb.GetVisibleCount();
                int maxScroll = Math.Max(0, lb.Items.Count - visible);
                sb.Min = 0; sb.Max = maxScroll;
                sb.Value = Math.Clamp(sb.Value, sb.Min, sb.Max);
                lb.ScrollOffset = sb.Value;
            };

            // Initial sync on wire-up
            int visible0 = lb.GetVisibleCount();
            int max0 = Math.Max(0, lb.Items.Count - visible0);
            sb.Min = 0; sb.Max = max0;
            sb.Value = Math.Clamp(lb.ScrollOffset, sb.Min, sb.Max);
        }
    }
}