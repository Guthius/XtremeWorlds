using Client.Game.UI.Controls;
using Core.Globals;
using System;
using System.IO;

namespace Client.Game.UI.Windows;

public static class WinEventEditor
{
    public static int SelectedPage { get; private set; }
    private static bool _isLoading;
    private static Core.Globals.Type.Event _history;
    private static bool _hasHistory;
    private static readonly System.Collections.Generic.List<(int listIndex, int commandIndex)> _commandIndexMap = new();

    private static readonly string[] _commandTextControlNames =
    [
        "txtCmdText1",
        "txtCmdText2",
        "txtCmdText3",
    ];

    private static readonly string[] _commandDataControlNames =
    [
        "txtCmdData1",
        "txtCmdData2",
        "txtCmdData3",
        "txtCmdData4",
        "txtCmdData5",
        "txtCmdData6",
    ];

    private static readonly System.Collections.Generic.List<int> _commandPickerValueMap = new();
    private static int _pickerTargetListIndex;
    private static int _pickerTargetCommandIndex;
    private static bool _pickerTargetIsNew;

    private static int _dataTargetListIndex;
    private static int _dataTargetCommandIndex;
    private static bool _dataTargetIsNew;
    private static Core.Globals.Type.EventCommand _dataHistoryCommand;
    private static bool _dataHasHistory;

    private const int MaxPageButtons = 28;

    private static string FormatCommandDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Turn PascalCase/camelCase into spaced words: "ShowText" -> "Show Text".
        // Also handles digit boundaries: "ShowText2" -> "Show Text 2".
        var sb = new System.Text.StringBuilder(name.Length + 8);
        sb.Append(name[0]);

        for (int i = 1; i < name.Length; i++)
        {
            char c = name[i];
            char prev = name[i - 1];
            char? next = i + 1 < name.Length ? name[i + 1] : null;

            bool isUpper = char.IsUpper(c);
            bool prevIsLower = char.IsLower(prev);
            bool prevIsDigit = char.IsDigit(prev);
            bool isDigit = char.IsDigit(c);

            // Insert a space between lower->upper (aB), digit->letter (2A), letter->digit (A2),
            // and acronym->word (ABc -> A Bc at the last upper before a lower).
            bool acronymBoundary = isUpper && next is not null && char.IsLower(next.Value) && char.IsUpper(prev);

            if ((isUpper && prevIsLower) || (isDigit && !prevIsDigit) || (!isDigit && prevIsDigit) || acronymBoundary)
                sb.Append(' ');

            sb.Append(c);
        }

        return sb.ToString();
    }

    public static void Init()
    {
        if (!WindowManager.TryGetControl("winEventEditor", "txtName", out _))
            return;
        _isLoading = true;
        try
        {
            // Initialize the editor backing data
            Client.Event.EventEditorInit();

            // Snapshot original event for Cancel
            _history = Client.Event.Instance;
            _hasHistory = true;

            PopulateCombos();

            SelectedPage = 0;
            ClampSelectedPage();
            LoadEventToControls();
            LoadPageToControls();
            RefreshCommandsList();
        }
        finally
        {
            _isLoading = false;
            RefreshPageButtons();
        }
    }

    private static void SetCommandDataControlsEnabled(bool enabled)
    {
        foreach (var name in _commandTextControlNames)
        {
            if (WindowManager.TryGetControl("winEventEditor", name, out var ctrl) && ctrl is not null)
                ctrl.Enabled = enabled;
        }

        foreach (var name in _commandDataControlNames)
        {
            if (WindowManager.TryGetControl("winEventEditor", name, out var ctrl) && ctrl is not null)
                ctrl.Enabled = enabled;
        }
    }

    private static void ClearCommandDataControls(string selectedLabel)
    {
        if (WindowManager.TryGetControl("winEventEditor", "lblCmdSelected", out var selectedCtrl) && selectedCtrl is Label lbl)
            lbl.Text = selectedLabel;

        foreach (var name in _commandTextControlNames)
        {
            if (WindowManager.TryGetControl("winEventEditor", name, out var ctrl) && ctrl is TextBox tb)
                tb.Text = string.Empty;
        }

        foreach (var name in _commandDataControlNames)
        {
            if (WindowManager.TryGetControl("winEventEditor", name, out var ctrl) && ctrl is TextBox tb)
                tb.Text = "0";
        }
    }

    private static bool TryGetSelectedCommand(out int listIndex, out int commandIndex, out Core.Globals.Type.EventCommand command)
    {
        listIndex = -1;
        commandIndex = -1;
        command = default;

        if (!WindowManager.TryGetControl("winEventEditor", "lstCommands", out var lstCtrl) || lstCtrl is not ListBox list)
            return false;

        int selectedIndex = list.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex >= _commandIndexMap.Count)
            return false;

        if (!TryGetCurrentPage(out var page))
            return false;

        (listIndex, commandIndex) = _commandIndexMap[selectedIndex];

        int listCount = Math.Max(1, page.CommandListCount);
        var lists = page.CommandList ?? Array.Empty<Core.Globals.Type.CommandList>();
        if (lists.Length < listCount)
            Array.Resize(ref lists, listCount);

        if (listIndex < 0 || listIndex >= lists.Length)
            return false;

        var cmdList = lists[listIndex];
        int cmdCount = Math.Max(0, cmdList.CommandCount);
        var cmds = cmdList.Commands ?? Array.Empty<Core.Globals.Type.EventCommand>();
        if (cmds.Length < cmdCount)
            Array.Resize(ref cmds, cmdCount);

        if (commandIndex < 0 || commandIndex >= cmdCount)
            return false;

        command = cmds[commandIndex];
        return true;
    }

    private static void SetCommand(int listIndex, int commandIndex, Core.Globals.Type.EventCommand command)
    {
        if (!TryGetCurrentPage(out var page))
            return;

        int listCount = Math.Max(1, page.CommandListCount);
        var lists = page.CommandList ?? Array.Empty<Core.Globals.Type.CommandList>();
        if (lists.Length < listCount)
            Array.Resize(ref lists, listCount);

        if (listIndex < 0 || listIndex >= lists.Length)
            return;

        var cmdList = lists[listIndex];
        int cmdCount = Math.Max(0, cmdList.CommandCount);
        var cmds = cmdList.Commands ?? Array.Empty<Core.Globals.Type.EventCommand>();
        if (cmds.Length < cmdCount)
            Array.Resize(ref cmds, cmdCount);

        if (commandIndex < 0 || commandIndex >= cmdCount)
            return;

        cmds[commandIndex] = command;
        cmdList.Commands = cmds;
        cmdList.CommandCount = cmds.Length;
        lists[listIndex] = cmdList;
        page.CommandList = lists;
        page.CommandListCount = listCount;
        SetCurrentPage(page);
    }

    public static void LoadSelectedCommandToControls()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        try
        {
            if (!TryGetSelectedCommand(out _, out _, out var cmd) || cmd.Index < 0)
            {
                ClearCommandDataControls("No command selected");
                SetCommandDataControlsEnabled(false);
                return;
            }

            string cmdName;
            try { cmdName = ((EventCommand)cmd.Index).ToString(); }
            catch { cmdName = cmd.Index.ToString(); }

            if (WindowManager.TryGetControl("winEventEditor", "lblCmdSelected", out var selectedCtrl) && selectedCtrl is Label lbl)
                lbl.Text = cmdName;

            SetCommandDataControlsEnabled(true);

            if (WindowManager.TryGetControl("winEventEditor", "txtCmdText1", out var t1) && t1 is TextBox tb1)
                tb1.Text = cmd.Text1 ?? string.Empty;
            if (WindowManager.TryGetControl("winEventEditor", "txtCmdText2", out var t2) && t2 is TextBox tb2)
                tb2.Text = cmd.Text2 ?? string.Empty;
            if (WindowManager.TryGetControl("winEventEditor", "txtCmdText3", out var t3) && t3 is TextBox tb3)
                tb3.Text = cmd.Text3 ?? string.Empty;

            if (WindowManager.TryGetControl("winEventEditor", "txtCmdData1", out var d1) && d1 is TextBox db1)
                db1.Text = cmd.Data1.ToString();
            if (WindowManager.TryGetControl("winEventEditor", "txtCmdData2", out var d2) && d2 is TextBox db2)
                db2.Text = cmd.Data2.ToString();
            if (WindowManager.TryGetControl("winEventEditor", "txtCmdData3", out var d3) && d3 is TextBox db3)
                db3.Text = cmd.Data3.ToString();
            if (WindowManager.TryGetControl("winEventEditor", "txtCmdData4", out var d4) && d4 is TextBox db4)
                db4.Text = cmd.Data4.ToString();
            if (WindowManager.TryGetControl("winEventEditor", "txtCmdData5", out var d5) && d5 is TextBox db5)
                db5.Text = cmd.Data5.ToString();
            if (WindowManager.TryGetControl("winEventEditor", "txtCmdData6", out var d6) && d6 is TextBox db6)
                db6.Text = cmd.Data6.ToString();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static int ReadIntTextBox(string controlName, int fallback = 0)
    {
        if (WindowManager.TryGetControl("winEventEditor", controlName, out var ctrl) && ctrl is TextBox tb)
        {
            var s = GetLiveText(tb).Trim();
            if (int.TryParse(s, out var v))
                return v;
        }
        return fallback;
    }

    private static string ReadStringTextBox(string controlName)
    {
        if (WindowManager.TryGetControl("winEventEditor", controlName, out var ctrl) && ctrl is TextBox tb)
            return GetLiveText(tb);
        return string.Empty;
    }

    public static void UpdateSelectedCommandFromControls()
    {
        if (_isLoading)
            return;

        if (!TryGetSelectedCommand(out var listIndex, out var commandIndex, out var cmd) || cmd.Index < 0)
            return;

        cmd.Text1 = ReadStringTextBox("txtCmdText1");
        cmd.Text2 = ReadStringTextBox("txtCmdText2");
        cmd.Text3 = ReadStringTextBox("txtCmdText3");
        cmd.Data1 = ReadIntTextBox("txtCmdData1", cmd.Data1);
        cmd.Data2 = ReadIntTextBox("txtCmdData2", cmd.Data2);
        cmd.Data3 = ReadIntTextBox("txtCmdData3", cmd.Data3);
        cmd.Data4 = ReadIntTextBox("txtCmdData4", cmd.Data4);
        cmd.Data5 = ReadIntTextBox("txtCmdData5", cmd.Data5);
        cmd.Data6 = ReadIntTextBox("txtCmdData6", cmd.Data6);

        SetCommand(listIndex, commandIndex, cmd);
        RefreshCommandsList();
    }

    private static int ReadIntTextBox(string windowName, string controlName, int fallback = 0)
    {
        if (WindowManager.TryGetControl(windowName, controlName, out var ctrl) && ctrl is TextBox tb)
        {
            var s = GetLiveText(tb).Trim();
            if (int.TryParse(s, out var v))
                return v;
        }
        return fallback;
    }

    private static string ReadStringTextBox(string windowName, string controlName)
    {
        if (WindowManager.TryGetControl(windowName, controlName, out var ctrl) && ctrl is TextBox tb)
            return GetLiveText(tb);
        return string.Empty;
    }

    private static string GetLiveText(TextBox tb)
    {
        // In this UI framework, TextBox.Render shows Text + GameState.ChatShowLine when active;
        // Text itself may not update until focus/commit. Read the same "live" value for bindings.
        var committed = tb.Text ?? string.Empty;
        var live = ReferenceEquals(WindowManager.ActiveWindow?.ActiveControl, tb)
            ? committed + (GameState.ChatShowLine ?? string.Empty)
            : committed;

        return live.Replace("\0", string.Empty);
    }

    private static bool TryGetCommandAt(int listIndex, int commandIndex, out Core.Globals.Type.EventCommand cmd)
    {
        cmd = default;
        if (!TryGetCurrentPage(out var page))
            return false;

        int listCount = Math.Max(1, page.CommandListCount);
        var lists = page.CommandList ?? Array.Empty<Core.Globals.Type.CommandList>();
        if (lists.Length < listCount)
            Array.Resize(ref lists, listCount);

        if (listIndex < 0 || listIndex >= lists.Length)
            return false;

        var cmdList = lists[listIndex];
        int cmdCount = Math.Max(0, cmdList.CommandCount);
        var cmds = cmdList.Commands ?? Array.Empty<Core.Globals.Type.EventCommand>();
        if (cmds.Length < cmdCount)
            Array.Resize(ref cmds, cmdCount);

        if (commandIndex < 0 || commandIndex >= cmdCount)
            return false;

        cmd = cmds[commandIndex];
        return true;
    }

    private static void LoadCommandToWindow(string windowName, Core.Globals.Type.EventCommand cmd)
    {
        string cmdName;
        try { cmdName = ((EventCommand)cmd.Index).ToString(); }
        catch { cmdName = cmd.Index.ToString(); }

        if (WindowManager.TryGetControl(windowName, "lblCmdSelected", out var selectedCtrl) && selectedCtrl is Label lbl)
            lbl.Text = cmdName;

        if (WindowManager.TryGetControl(windowName, "txtCmdText1", out var t1) && t1 is TextBox tb1)
            tb1.Text = cmd.Text1 ?? string.Empty;
        if (WindowManager.TryGetControl(windowName, "txtCmdText2", out var t2) && t2 is TextBox tb2)
            tb2.Text = cmd.Text2 ?? string.Empty;
        if (WindowManager.TryGetControl(windowName, "txtCmdText3", out var t3) && t3 is TextBox tb3)
            tb3.Text = cmd.Text3 ?? string.Empty;

        if (WindowManager.TryGetControl(windowName, "txtCmdData1", out var d1) && d1 is TextBox db1)
            db1.Text = cmd.Data1.ToString();
        if (WindowManager.TryGetControl(windowName, "txtCmdData2", out var d2) && d2 is TextBox db2)
            db2.Text = cmd.Data2.ToString();
        if (WindowManager.TryGetControl(windowName, "txtCmdData3", out var d3) && d3 is TextBox db3)
            db3.Text = cmd.Data3.ToString();
        if (WindowManager.TryGetControl(windowName, "txtCmdData4", out var d4) && d4 is TextBox db4)
            db4.Text = cmd.Data4.ToString();
        if (WindowManager.TryGetControl(windowName, "txtCmdData5", out var d5) && d5 is TextBox db5)
            db5.Text = cmd.Data5.ToString();
        if (WindowManager.TryGetControl(windowName, "txtCmdData6", out var d6) && d6 is TextBox db6)
            db6.Text = cmd.Data6.ToString();
    }

    private static void SelectCommandInList(int listIndex, int commandIndex)
    {
        if (!WindowManager.TryGetControl("winEventEditor", "lstCommands", out var ctrl) || ctrl is not ListBox list)
            return;

        int mappedIndex = -1;
        for (int i = 0; i < _commandIndexMap.Count; i++)
        {
            if (_commandIndexMap[i].listIndex == listIndex && _commandIndexMap[i].commandIndex == commandIndex)
            {
                mappedIndex = i;
                break;
            }
        }

        if (mappedIndex < 0 || mappedIndex >= list.Items.Count)
            return;

        list.SelectedIndex = mappedIndex;
        list.EnsureVisible(mappedIndex);
    }

    private static void RemoveCommandAt(int listIndex, int commandIndex)
    {
        if (!TryGetCurrentPage(out var page))
            return;

        int listCount = Math.Max(1, page.CommandListCount);
        var lists = page.CommandList ?? Array.Empty<Core.Globals.Type.CommandList>();
        if (lists.Length < listCount)
            Array.Resize(ref lists, listCount);

        if (listIndex < 0 || listIndex >= lists.Length)
            return;

        var cmdList = lists[listIndex];
        int cmdCount = Math.Max(0, cmdList.CommandCount);
        var cmds = cmdList.Commands ?? Array.Empty<Core.Globals.Type.EventCommand>();
        if (cmds.Length < cmdCount)
            Array.Resize(ref cmds, cmdCount);

        if (commandIndex < 0 || commandIndex >= cmdCount)
            return;

        for (int i = commandIndex; i < cmdCount - 1; i++)
            cmds[i] = cmds[i + 1];

        if (cmdCount - 1 <= 0)
        {
            cmdList.Commands = Array.Empty<Core.Globals.Type.EventCommand>();
            cmdList.CommandCount = 0;
        }
        else
        {
            Array.Resize(ref cmds, cmdCount - 1);
            cmdList.Commands = cmds;
            cmdList.CommandCount = cmdCount - 1;
        }

        lists[listIndex] = cmdList;
        page.CommandList = lists;
        page.CommandListCount = listCount;
        SetCurrentPage(page);
    }

    private static void WireCommandDataWindowControls()
    {
        if (WindowManager.TryGetControl("winEventCommandData", "btnClose", out var btnClose) && btnClose is not null)
            btnClose.CallBack[(int)ControlState.MouseDown] = OnCommandDataCancel;
        if (WindowManager.TryGetControl("winEventCommandData", "btnCancel", out var btnCancel) && btnCancel is not null)
            btnCancel.CallBack[(int)ControlState.MouseDown] = OnCommandDataCancel;
        if (WindowManager.TryGetControl("winEventCommandData", "btnOk", out var btnOk) && btnOk is not null)
            btnOk.CallBack[(int)ControlState.MouseDown] = OnCommandDataOk;
    }

    private static void OpenCommandDataEditor(int listIndex, int commandIndex, bool isNew)
    {
        _dataTargetListIndex = listIndex;
        _dataTargetCommandIndex = commandIndex;
        _dataTargetIsNew = isNew;

        _dataHasHistory = TryGetCommandAt(listIndex, commandIndex, out _dataHistoryCommand);

        WindowManager.ShowWindow("winEventCommandData", forced: true);
        WireCommandDataWindowControls();

        if (TryGetCommandAt(listIndex, commandIndex, out var cmd) && cmd.Index >= 0)
            LoadCommandToWindow("winEventCommandData", cmd);
    }

    public static void OnCommandDataOk()
    {
        int listIndex = _dataTargetListIndex;
        int commandIndex = _dataTargetCommandIndex;

        if (!TryGetCommandAt(listIndex, commandIndex, out var cmd) || cmd.Index < 0)
            return;

        cmd.Text1 = ReadStringTextBox("winEventCommandData", "txtCmdText1");
        cmd.Text2 = ReadStringTextBox("winEventCommandData", "txtCmdText2");
        cmd.Text3 = ReadStringTextBox("winEventCommandData", "txtCmdText3");
        cmd.Data1 = ReadIntTextBox("winEventCommandData", "txtCmdData1", cmd.Data1);
        cmd.Data2 = ReadIntTextBox("winEventCommandData", "txtCmdData2", cmd.Data2);
        cmd.Data3 = ReadIntTextBox("winEventCommandData", "txtCmdData3", cmd.Data3);
        cmd.Data4 = ReadIntTextBox("winEventCommandData", "txtCmdData4", cmd.Data4);
        cmd.Data5 = ReadIntTextBox("winEventCommandData", "txtCmdData5", cmd.Data5);
        cmd.Data6 = ReadIntTextBox("winEventCommandData", "txtCmdData6", cmd.Data6);

        SetCommand(listIndex, commandIndex, cmd);
        WindowManager.HideWindow("winEventCommandData");

        RefreshCommandsList();
        SelectCommandInList(listIndex, commandIndex);
        LoadSelectedCommandToControls();
    }

    public static void OnCommandDataCancel()
    {
        int listIndex = _dataTargetListIndex;
        int commandIndex = _dataTargetCommandIndex;

        if (_dataTargetIsNew)
        {
            RemoveCommandAt(listIndex, commandIndex);
        }
        else if (_dataHasHistory)
        {
            SetCommand(listIndex, commandIndex, _dataHistoryCommand);
        }

        WindowManager.HideWindow("winEventCommandData");
        RefreshCommandsList();
        LoadSelectedCommandToControls();
    }

    private static void PopulateCombos()
    {
        // Trigger
        if (WindowManager.TryGetControl("winEventEditor", "cmbTrigger", out var trigCtrl) && trigCtrl is ComboBox cmbTrigger)
        {
            if (cmbTrigger.Items.Count == 0)
            {
                cmbTrigger.Items.Clear();
                cmbTrigger.Items.Add("Action Button");
                cmbTrigger.Items.Add("Player Touch");
                cmbTrigger.Items.Add("Parallel");
            }
        }

        // Positioning
        if (WindowManager.TryGetControl("winEventEditor", "cmbPositioning", out var posCtrl) && posCtrl is ComboBox cmbPos)
        {
            if (cmbPos.Items.Count == 0)
            {
                cmbPos.Items.Clear();
                cmbPos.Items.Add("Below Player");
                cmbPos.Items.Add("Same as Player");
                cmbPos.Items.Add("Above Player");
            }
        }

        // Move type
        if (WindowManager.TryGetControl("winEventEditor", "cmbMoveType", out var mtCtrl) && mtCtrl is ComboBox cmbMoveType)
        {
            if (cmbMoveType.Items.Count == 0)
            {
                cmbMoveType.Items.Clear();
                cmbMoveType.Items.Add("Fixed");
                cmbMoveType.Items.Add("Random");
                cmbMoveType.Items.Add("Route");
            }
        }

        // Move speed
        if (WindowManager.TryGetControl("winEventEditor", "cmbMoveSpeed", out var msCtrl) && msCtrl is ComboBox cmbMoveSpeed)
        {
            if (cmbMoveSpeed.Items.Count == 0)
            {
                cmbMoveSpeed.Items.Clear();
                cmbMoveSpeed.Items.Add("8x Slower");
                cmbMoveSpeed.Items.Add("4x Slower");
                cmbMoveSpeed.Items.Add("2x Slower");
                cmbMoveSpeed.Items.Add("Normal");
                cmbMoveSpeed.Items.Add("2x Faster");
                cmbMoveSpeed.Items.Add("4x Faster");
            }
        }

        // Move frequency
        if (WindowManager.TryGetControl("winEventEditor", "cmbMoveFreq", out var mfCtrl) && mfCtrl is ComboBox cmbMoveFreq)
        {
            if (cmbMoveFreq.Items.Count == 0)
            {
                cmbMoveFreq.Items.Clear();
                cmbMoveFreq.Items.Add("Lowest");
                cmbMoveFreq.Items.Add("Lower");
                cmbMoveFreq.Items.Add("Normal");
                cmbMoveFreq.Items.Add("Higher");
                cmbMoveFreq.Items.Add("Highest");
            }
        }

        // Graphic type
        if (WindowManager.TryGetControl("winEventEditor", "cmbGraphicType", out var gtCtrl) && gtCtrl is ComboBox cmbGraphicType)
        {
            if (cmbGraphicType.Items.Count == 0)
            {
                cmbGraphicType.Items.Clear();
                cmbGraphicType.Items.Add("None");
                cmbGraphicType.Items.Add("Sprite");
                cmbGraphicType.Items.Add("Tileset");
                cmbGraphicType.Items.Add("Picture");
            }
        }
    }

    private static void RefreshPageButtons()
    {
        int pageCount = Math.Max(1, Client.Event.Instance.PageCount);
        int selected = Math.Clamp(SelectedPage, 0, pageCount - 1);

        for (int i = 1; i <= MaxPageButtons; i++)
        {
            if (!WindowManager.TryGetControl("winEventEditor", $"btnPage{i}", out var ctrl) || ctrl is not Button btn)
                continue;

            bool visible = i <= pageCount;
            btn.Visible = visible;
            if (!visible)
                continue;

            btn.Text = i.ToString();
            bool isActive = (i - 1) == selected;
            btn.Design = isActive ? Design.Green : Design.Orange;
            btn.DesignHover = isActive ? Design.GreenHover : Design.OrangeHover;
            btn.DesignMouseDown = isActive ? Design.GreenClick : Design.OrangeClick;
        }
    }

    private static void ClampSelectedPage()
    {
        int pageCount = Math.Max(1, Client.Event.Instance.PageCount);
        SelectedPage = Math.Clamp(SelectedPage, 0, pageCount - 1);
        Client.Event.CurPageNum = SelectedPage;
    }

    private static bool TryGetCurrentPage(out Core.Globals.Type.EventPage page)
    {
        page = default;
        if (Client.Event.Instance.Pages == null || Client.Event.Instance.Pages.Length == 0)
            return false;

        ClampSelectedPage();
        if (SelectedPage < 0 || SelectedPage >= Client.Event.Instance.Pages.Length)
            return false;

        page = Client.Event.Instance.Pages[SelectedPage];
        return true;
    }

    private static void SetCurrentPage(Core.Globals.Type.EventPage page)
    {
        if (Client.Event.Instance.Pages == null || Client.Event.Instance.Pages.Length == 0)
            return;

        ClampSelectedPage();
        Client.Event.Instance.Pages[SelectedPage] = page;
    }

    private static void LoadEventToControls()
    {
        // Name
        if (WindowManager.TryGetControl("winEventEditor", "txtName", out var nameCtrl) && nameCtrl is TextBox txtName)
            txtName.Text = Client.Event.Instance.Name ?? string.Empty;

        // Global
        if (WindowManager.TryGetControl("winEventEditor", "chkGlobal", out var globalCtrl) && globalCtrl is CheckBox chkGlobal)
        {
            chkGlobal.Value = Client.Event.Instance.Globals == 1 ? 1 : 0;
        }

        // Position label (event-level)
        if (WindowManager.TryGetControl("winEventEditor", "lblPosition", out var posCtrl) && posCtrl is Label lblPos)
        {
            lblPos.Text = $"({Client.Event.Instance.X}, {Client.Event.Instance.Y})";
        }

        UpdatePageLabel();
    }

    private static void LoadPageToControls()
    {
        if (!TryGetCurrentPage(out var page))
            return;

        // Trigger
        if (WindowManager.TryGetControl("winEventEditor", "cmbTrigger", out var trigCtrl) && trigCtrl is ComboBox cmbTrigger)
            cmbTrigger.Value = Math.Clamp(page.Trigger, 0, cmbTrigger.Items.Count - 1);

        // Graphic (per-page)
        SyncGraphicControlsFromPage(page);

        UpdatePageLabel();
    }

    private static void UpdatePageLabel()
    {
        if (WindowManager.TryGetControl("winEventEditor", "lblPage", out var pageCtrl) && pageCtrl is Label lblPage)
        {
            int pageCount = Math.Max(1, Client.Event.Instance.PageCount);
            int display = Math.Clamp(SelectedPage + 1, 1, pageCount);
            lblPage.Text = $"{display} / {pageCount}";
        }
    }

    public static void RefreshCommandsList()
    {
        if (!WindowManager.TryGetControl("winEventEditor", "lstCommands", out var lstCtrl) || lstCtrl is not ListBox list)
            return;

        int prevSelected = list.SelectedIndex;
        int prevScroll = list.ScrollOffset;

        list.Clear();
        _commandIndexMap.Clear();

        if (!TryGetCurrentPage(out var page))
            return;

        // Minimal, safe list view (does not replicate Eto editor's rich formatting)
        if (page.CommandListCount <= 0 || page.CommandList == null)
            return;

        for (int listIndex = 0; listIndex < page.CommandListCount && listIndex < page.CommandList.Length; listIndex++)
        {
            var cmdList = page.CommandList[listIndex];
            if (cmdList.CommandCount <= 0 || cmdList.Commands == null) continue;

            for (int cmdIndex = 0; cmdIndex < cmdList.CommandCount && cmdIndex < cmdList.Commands.Length; cmdIndex++)
            {
                var cmd = cmdList.Commands[cmdIndex];
                if (cmd.Index < 0) continue;

                string name;
                try { name = ((EventCommand)cmd.Index).ToString(); }
                catch { name = cmd.Index.ToString(); }

                string preview = cmd.Text1;
                if (!string.IsNullOrWhiteSpace(preview))
                    preview = preview.Length > 24 ? preview.Substring(0, 24) + "..." : preview;

                var line = string.IsNullOrWhiteSpace(preview)
                    ? $"{listIndex}:{cmdIndex} {name}"
                    : $"{listIndex}:{cmdIndex} {name} - {preview}";

                list.AddItem(line);
                _commandIndexMap.Add((listIndex, cmdIndex));
            }
        }

        // Restore selection/scroll if possible
        if (list.Items.Count > 0)
        {
            list.SelectedIndex = Math.Clamp(prevSelected, -1, list.Items.Count - 1);
            int visible = list.GetVisibleCount();
            int max = Math.Max(0, list.Items.Count - visible);
            list.ScrollOffset = Math.Clamp(prevScroll, 0, max);
        }
        else
        {
            list.SelectedIndex = -1;
            list.ScrollOffset = 0;
        }

        // Sync scrollbar max/value if present
        if (WindowManager.TryGetControl("winEventEditor", "sldCommands", out var sldCtrl) && sldCtrl is ScrollBar sb)
        {
            int visible = list.GetVisibleCount();
            int max = Math.Max(0, list.Items.Count - visible);
            sb.Min = 0;
            sb.Max = max;
            sb.Value = Math.Clamp(list.ScrollOffset, sb.Min, sb.Max);
        }

        LoadSelectedCommandToControls();
    }

    public static void OnCommandsListMouseDown()
    {
        if (!WindowManager.TryGetControl("winEventEditor", "lstCommands", out var ctrl) || ctrl is not ListBox list) return;
        var win = WindowManager.GetWindowByName("winEventEditor");
        if (win is null) return;

        int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
        int index = list.GetItemIndexAtPosition(relY);
        if (index < 0 || index >= list.Items.Count) return;

        list.SelectedIndex = index;
        list.EnsureVisible(index);

        if (WindowManager.TryGetControl("winEventEditor", "sldCommands", out var sldCtrl) && sldCtrl is ScrollBar sb)
        {
            int visible = list.GetVisibleCount();
            int max = Math.Max(0, list.Items.Count - visible);
            sb.Min = 0;
            sb.Max = max;
            sb.Value = Math.Clamp(list.ScrollOffset, sb.Min, sb.Max);
        }

        LoadSelectedCommandToControls();
    }

    public static void OnCommandsListDoubleClick()
    {
        if (_isLoading) return;
        if (!WindowManager.TryGetControl("winEventEditor", "lstCommands", out var ctrl) || ctrl is not ListBox list) return;
        int selectedIndex = list.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex >= _commandIndexMap.Count) return;

        var (listIndex, commandIndex) = _commandIndexMap[selectedIndex];
        OpenCommandPicker(listIndex, commandIndex, isNew: false);
    }

    public static void OnEditCommand()
    {
        if (_isLoading) return;
        if (!WindowManager.TryGetControl("winEventEditor", "lstCommands", out var ctrl) || ctrl is not ListBox list) return;

        int selectedIndex = list.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex >= _commandIndexMap.Count) return;

        var (listIndex, commandIndex) = _commandIndexMap[selectedIndex];
        if (!TryGetCommandAt(listIndex, commandIndex, out var cmd) || cmd.Index < 0)
            return;

        OpenCommandDataEditor(listIndex, commandIndex, isNew: false);
    }

    public static void OnCommandsListMouseWheel()
    {
        if (!WindowManager.TryGetControl("winEventEditor", "lstCommands", out var ctrl) || ctrl is not ListBox list) return;

        int visible = list.GetVisibleCount();
        int max = Math.Max(0, list.Items.Count - visible);
        int delta = GameClient.GetMouseScrollDelta();
        int step = (delta > 0) ? -1 : 1;

        list.ScrollOffset = Math.Clamp(list.ScrollOffset + step, 0, max);
        if (WindowManager.TryGetControl("winEventEditor", "sldCommands", out var sldCtrl) && sldCtrl is ScrollBar sb)
            sb.Value = Math.Clamp(list.ScrollOffset, sb.Min, sb.Max);
    }

    public static void OnCommandsScrollBarMove()
    {
        if (!WindowManager.TryGetControl("winEventEditor", "lstCommands", out var ctrl) || ctrl is not ListBox list) return;
        if (!WindowManager.TryGetControl("winEventEditor", "sldCommands", out var sldCtrl) || sldCtrl is null) return;

        int visible = list.GetVisibleCount();
        int max = Math.Max(0, list.Items.Count - visible);

        if (sldCtrl is ScrollBar sb)
        {
            sb.Min = 0;
            sb.Max = max;
            list.ScrollOffset = Math.Clamp(sb.Value, sb.Min, sb.Max);
        }
        else
        {
            list.ScrollOffset = Math.Clamp(sldCtrl.Value, 0, max);
        }
    }

    public static void OnPrevPage()
    {
        if (_isLoading) return;
        SelectedPage--;
        ClampSelectedPage();
        _isLoading = true;
        try
        {
            LoadPageToControls();
            RefreshCommandsList();
        }
        finally
        {
            _isLoading = false;
            RefreshPageButtons();
        }
    }

    public static void OnSelectPage(int pageIndex)
    {
        if (_isLoading) return;
        SelectedPage = pageIndex;
        ClampSelectedPage();
        _isLoading = true;
        try
        {
            LoadPageToControls();
            RefreshCommandsList();
        }
        finally
        {
            _isLoading = false;
            RefreshPageButtons();
        }
    }

    public static void OnNextPage()
    {
        if (_isLoading) return;
        SelectedPage++;
        ClampSelectedPage();
        _isLoading = true;
        try
        {
            LoadPageToControls();
            RefreshCommandsList();
        }
        finally
        {
            _isLoading = false;
            RefreshPageButtons();
        }
    }

    public static void OnAddCommand()
    {
        if (_isLoading) return;
        if (!TryGetCurrentPage(out var page)) return;

        // Ensure at least one command list exists
        int listCount = Math.Max(1, page.CommandListCount);
        var lists = page.CommandList ?? Array.Empty<Core.Globals.Type.CommandList>();
        if (lists.Length < listCount)
            Array.Resize(ref lists, listCount);

        // Append to first list (simple/default path) and open picker to choose a command type
        var list0 = lists[0];
        int cmdCount = Math.Max(0, list0.CommandCount);
        var cmds = list0.Commands ?? Array.Empty<Core.Globals.Type.EventCommand>();
        Array.Resize(ref cmds, cmdCount + 1);
        cmds[cmdCount].Index = -1;

        list0.Commands = cmds;
        list0.CommandCount = cmdCount + 1;
        lists[0] = list0;

        page.CommandList = lists;
        page.CommandListCount = listCount;

        SetCurrentPage(page);

        RefreshCommandsList();
        OpenCommandPicker(0, cmdCount, isNew: true);
    }

    public static void OnDeleteCommand()
    {
        if (_isLoading) return;
        if (!TryGetCurrentPage(out var page)) return;

        if (!WindowManager.TryGetControl("winEventEditor", "lstCommands", out var ctrl) || ctrl is not ListBox list)
            return;

        int selectedIndex = list.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex >= _commandIndexMap.Count)
            return;

        var (listIndex, commandIndex) = _commandIndexMap[selectedIndex];

        int listCount = Math.Max(1, page.CommandListCount);
        var lists = page.CommandList ?? Array.Empty<Core.Globals.Type.CommandList>();
        if (lists.Length < listCount)
            Array.Resize(ref lists, listCount);

        if (listIndex < 0 || listIndex >= lists.Length)
            return;

        var cmdList = lists[listIndex];
        int cmdCount = Math.Max(0, cmdList.CommandCount);
        var cmds = cmdList.Commands ?? Array.Empty<Core.Globals.Type.EventCommand>();
        if (cmds.Length < cmdCount)
            Array.Resize(ref cmds, cmdCount);

        if (commandIndex < 0 || commandIndex >= cmdCount)
            return;

        for (int i = commandIndex; i < cmdCount - 1; i++)
            cmds[i] = cmds[i + 1];

        if (cmdCount - 1 <= 0)
        {
            cmdList.Commands = Array.Empty<Core.Globals.Type.EventCommand>();
            cmdList.CommandCount = 0;
        }
        else
        {
            Array.Resize(ref cmds, cmdCount - 1);
            cmdList.Commands = cmds;
            cmdList.CommandCount = cmdCount - 1;
        }

        lists[listIndex] = cmdList;
        page.CommandList = lists;
        page.CommandListCount = listCount;
        SetCurrentPage(page);

        RefreshCommandsList();

        // Keep selection stable if possible
        if (list.Items.Count > 0)
            list.SelectedIndex = Math.Clamp(selectedIndex, 0, list.Items.Count - 1);
        else
            list.SelectedIndex = -1;
    }

    private static void OpenCommandPicker(int listIndex, int commandIndex, bool isNew)
    {
        _pickerTargetListIndex = listIndex;
        _pickerTargetCommandIndex = commandIndex;
        _pickerTargetIsNew = isNew;

        WindowManager.ShowWindow("winEventCommandSelect", forced: true);
        WireCommandPickerControls();
        RefreshCommandPickerList();
    }

    private static void WireCommandPickerControls()
    {
        if (WindowManager.TryGetControl("winEventCommandSelect", "btnClose", out var btnClose) && btnClose is not null)
            btnClose.CallBack[(int)ControlState.MouseDown] = OnCommandPickerCancel;
        if (WindowManager.TryGetControl("winEventCommandSelect", "btnCancel", out var btnCancel) && btnCancel is not null)
            btnCancel.CallBack[(int)ControlState.MouseDown] = OnCommandPickerCancel;
        if (WindowManager.TryGetControl("winEventCommandSelect", "btnOk", out var btnOk) && btnOk is not null)
            btnOk.CallBack[(int)ControlState.MouseDown] = OnCommandPickerOk;

        if (WindowManager.TryGetControl("winEventCommandSelect", "lstEventCommands", out var listCtrl) && listCtrl is ListBox list)
        {
            list.CallBack[(int)ControlState.MouseDown] = OnCommandPickerListMouseDown;
            list.CallBack[(int)ControlState.MouseScroll] = OnCommandPickerListMouseWheel;
            list.CallBack[(int)ControlState.DoubleClick] = OnCommandPickerOk;
        }

        if (WindowManager.TryGetControl("winEventCommandSelect", "sldEventCommands", out var sldCtrl) && sldCtrl is ScrollBar sb)
        {
            sb.CallBack[(int)ControlState.MouseMove] = OnCommandPickerScrollBarMove;
            sb.CallBack[(int)ControlState.MouseDown] = OnCommandPickerScrollBarMove;
        }
    }

    private static void RefreshCommandPickerList()
    {
        if (!WindowManager.TryGetControl("winEventCommandSelect", "lstEventCommands", out var listCtrl) || listCtrl is not ListBox list)
            return;

        list.Clear();
        _commandPickerValueMap.Clear();

        foreach (EventCommand cmd in Enum.GetValues(typeof(EventCommand)))
        {
            list.AddItem(FormatCommandDisplayName(cmd.ToString()));
            _commandPickerValueMap.Add((int)cmd);
        }

        if (list.Items.Count > 0 && list.SelectedIndex < 0)
            list.SelectedIndex = 0;

        // Sync scrollbar
        if (WindowManager.TryGetControl("winEventCommandSelect", "sldEventCommands", out var sldCtrl) && sldCtrl is ScrollBar sb)
        {
            int visible = list.GetVisibleCount();
            int max = Math.Max(0, list.Items.Count - visible);
            sb.Min = 0;
            sb.Max = max;
            sb.Value = Math.Clamp(list.ScrollOffset, sb.Min, sb.Max);
        }
    }

    private static void SetPickedCommandIndex(int pickedIndex)
    {
        if (!TryGetCurrentPage(out var page)) return;

        int listCount = Math.Max(1, page.CommandListCount);
        var lists = page.CommandList ?? Array.Empty<Core.Globals.Type.CommandList>();
        if (lists.Length < listCount)
            Array.Resize(ref lists, listCount);

        int li = Math.Clamp(_pickerTargetListIndex, 0, Math.Max(0, listCount - 1));
        var list0 = lists[li];
        int cmdCount = Math.Max(1, list0.CommandCount);
        var cmds = list0.Commands ?? Array.Empty<Core.Globals.Type.EventCommand>();
        if (cmds.Length < cmdCount)
            Array.Resize(ref cmds, cmdCount);

        int ci = Math.Clamp(_pickerTargetCommandIndex, 0, Math.Max(0, cmds.Length - 1));
        cmds[ci].Index = pickedIndex;

        list0.Commands = cmds;
        list0.CommandCount = cmds.Length;
        lists[li] = list0;

        page.CommandList = lists;
        page.CommandListCount = listCount;
        SetCurrentPage(page);
    }

    public static void OnCommandPickerOk()
    {
        if (!WindowManager.TryGetControl("winEventCommandSelect", "lstEventCommands", out var listCtrl) || listCtrl is not ListBox list)
            return;

        int selected = list.SelectedIndex;
        if (selected < 0 || selected >= _commandPickerValueMap.Count) return;

        SetPickedCommandIndex(_commandPickerValueMap[selected]);
        WindowManager.HideWindow("winEventCommandSelect");
        RefreshCommandsList();

        if (_pickerTargetIsNew)
            OpenCommandDataEditor(_pickerTargetListIndex, _pickerTargetCommandIndex, isNew: true);
    }

    public static void OnCommandPickerCancel()
    {
        // If we inserted a new placeholder command and cancel out, remove it again.
        if (_pickerTargetIsNew && TryGetCurrentPage(out var page))
        {
            int listCount = Math.Max(1, page.CommandListCount);
            var lists = page.CommandList ?? Array.Empty<Core.Globals.Type.CommandList>();
            if (lists.Length < listCount)
                Array.Resize(ref lists, listCount);

            int li = Math.Clamp(_pickerTargetListIndex, 0, Math.Max(0, listCount - 1));
            var cl = lists[li];
            var cmds = cl.Commands ?? Array.Empty<Core.Globals.Type.EventCommand>();
            int ci = _pickerTargetCommandIndex;

            if (ci >= 0 && ci < cmds.Length)
            {
                for (int i = ci; i < cmds.Length - 1; i++)
                    cmds[i] = cmds[i + 1];
                Array.Resize(ref cmds, Math.Max(0, cmds.Length - 1));

                // Keep at least one command slot like legacy behavior
                if (cmds.Length == 0)
                {
                    cmds = new Core.Globals.Type.EventCommand[1];
                    cmds[0].Index = -1;
                }

                cl.Commands = cmds;
                cl.CommandCount = cmds.Length;
                lists[li] = cl;
                page.CommandList = lists;
                page.CommandListCount = listCount;
                SetCurrentPage(page);
            }
        }

        WindowManager.HideWindow("winEventCommandSelect");
        RefreshCommandsList();
    }

    public static void OnCommandPickerListMouseDown()
    {
        if (!WindowManager.TryGetControl("winEventCommandSelect", "lstEventCommands", out var ctrl) || ctrl is not ListBox list) return;
        var win = WindowManager.GetWindowByName("winEventCommandSelect");
        if (win is null) return;

        int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
        int index = list.GetItemIndexAtPosition(relY);
        if (index < 0 || index >= list.Items.Count) return;

        list.SelectedIndex = index;
        list.EnsureVisible(index);

        if (WindowManager.TryGetControl("winEventCommandSelect", "sldEventCommands", out var sldCtrl) && sldCtrl is ScrollBar sb)
        {
            int visible = list.GetVisibleCount();
            int max = Math.Max(0, list.Items.Count - visible);
            sb.Min = 0;
            sb.Max = max;
            sb.Value = Math.Clamp(list.ScrollOffset, sb.Min, sb.Max);
        }
    }

    public static void OnCommandPickerListMouseWheel()
    {
        if (!WindowManager.TryGetControl("winEventCommandSelect", "lstEventCommands", out var ctrl) || ctrl is not ListBox list) return;

        int visible = list.GetVisibleCount();
        int max = Math.Max(0, list.Items.Count - visible);
        int delta = GameClient.GetMouseScrollDelta();
        int step = (delta > 0) ? -1 : 1;

        list.ScrollOffset = Math.Clamp(list.ScrollOffset + step, 0, max);
        if (WindowManager.TryGetControl("winEventCommandSelect", "sldEventCommands", out var sldCtrl) && sldCtrl is ScrollBar sb)
            sb.Value = Math.Clamp(list.ScrollOffset, sb.Min, sb.Max);
    }

    public static void OnCommandPickerScrollBarMove()
    {
        if (!WindowManager.TryGetControl("winEventCommandSelect", "lstEventCommands", out var ctrl) || ctrl is not ListBox list) return;
        if (!WindowManager.TryGetControl("winEventCommandSelect", "sldEventCommands", out var sldCtrl) || sldCtrl is null) return;

        int visible = list.GetVisibleCount();
        int max = Math.Max(0, list.Items.Count - visible);

        if (sldCtrl is ScrollBar sb)
        {
            sb.Min = 0;
            sb.Max = max;
            list.ScrollOffset = Math.Clamp(sb.Value, sb.Min, sb.Max);
        }
        else
        {
            list.ScrollOffset = Math.Clamp(sldCtrl.Value, 0, max);
        }
    }

    private static Core.Globals.Type.EventPage CreateDefaultPage()
    {
        var page = new Core.Globals.Type.EventPage();
        page.CommandListCount = 1;
        page.CommandList = new Core.Globals.Type.CommandList[1];
        page.CommandList[0].CommandCount = 1;
        page.CommandList[0].Commands = new Core.Globals.Type.EventCommand[1];
        page.CommandList[0].Commands[0].Index = -1;
        page.MoveRouteCount = 0;
        page.MoveRoute = Array.Empty<Core.Globals.Type.MoveRoute>();
        return page;
    }

    public static void OnAddPage()
    {
        if (_isLoading) return;

        var ev = Client.Event.Instance;
        int pageCount = Math.Max(1, ev.PageCount);

        var pages = ev.Pages ?? Array.Empty<Core.Globals.Type.EventPage>();
        Array.Resize(ref pages, pageCount + 1);
        pages[pageCount] = CreateDefaultPage();

        ev.Pages = pages;
        ev.PageCount = pageCount + 1;
        Client.Event.Instance = ev;

        SelectedPage = pageCount;
        ClampSelectedPage();

        _isLoading = true;
        try
        {
            LoadEventToControls();
            LoadPageToControls();
            RefreshCommandsList();
        }
        finally
        {
            _isLoading = false;
            RefreshPageButtons();
        }
    }

    public static void OnDeletePage()
    {
        if (_isLoading) return;

        var ev = Client.Event.Instance;
        int pageCount = Math.Max(1, ev.PageCount);
        if (pageCount <= 1) return;
        if (ev.Pages == null || ev.Pages.Length < pageCount) return;

        int removeAt = Math.Clamp(SelectedPage, 0, pageCount - 1);

        for (int i = removeAt; i < pageCount - 1; i++)
            ev.Pages[i] = ev.Pages[i + 1];

        Array.Resize(ref ev.Pages, pageCount - 1);
        ev.PageCount = pageCount - 1;
        Client.Event.Instance = ev;

        SelectedPage = Math.Clamp(removeAt, 0, ev.PageCount - 1);
        ClampSelectedPage();

        _isLoading = true;
        try
        {
            LoadEventToControls();
            LoadPageToControls();
            RefreshCommandsList();
        }
        finally
        {
            _isLoading = false;
            RefreshPageButtons();
        }
    }

    public static void OnOk()
    {
        Client.Event.EventEditorOK();
        Client.Event.InEvent = false;
        WindowManager.HideWindow("winEventEditor");
    }

    public static void OnCancel()
    {
        if (_hasHistory)
        {
            // Restore backing map event slot
            var map = Client.GameState.MyIndex >= 0 ? Client.GameState.MyIndex : Client.GameState.MyIndex;
            int playerMap = Commands.GetPlayerMap(Client.GameState.MyIndex);
            int eventNum = Client.GameState.EventNum;

            if (eventNum >= 0 && Client.Map.Instance[playerMap].Event != null && eventNum < Client.Map.Instance[playerMap].Event.Length)
            {
                Client.Map.Instance[playerMap].Event[eventNum] = _history;
            }

            Client.Event.Instance = _history;
        }

        Client.Event.InEvent = false;
        WindowManager.HideWindow("winEventEditor");
    }

    public static void SetEventNameFromControl(string? name)
    {
        if (_isLoading) return;
        // Ignore passed-in value; TextBox.Text may not include in-progress typing.
        Client.Event.Instance.Name = ReadStringTextBox("txtName");
    }

    public static void ToggleGlobalFromControl(int value)
    {
        if (_isLoading) return;
        Client.Event.Instance.Globals = (byte)(value == 1 ? 1 : 0);
    }

    private static int GetGraphicMaxForType(int graphicType)
    {
        return graphicType switch
        {
            1 => Math.Max(0, GameState.NumCharacters),
            2 => Math.Max(0, GameState.NumTileSets),
            3 => Math.Max(0, GameState.NumPictures),
            _ => 0,
        };
    }

    private static void SyncGraphicControlsFromPage(Core.Globals.Type.EventPage page)
    {
        int graphicType = page.GraphicType;

        if (WindowManager.TryGetControl("winEventEditor", "cmbGraphicType", out var gtCtrl) && gtCtrl is ComboBox cmbGraphicType)
        {
            graphicType = Math.Clamp(page.GraphicType, 0, Math.Max(0, cmbGraphicType.Items.Count - 1));
            cmbGraphicType.Value = graphicType;
        }

        int max = GetGraphicMaxForType(graphicType);
        int graphic = Math.Clamp(page.Graphic, 0, max);

        if (WindowManager.TryGetControl("winEventEditor", "sldGraphic", out var gCtrl) && gCtrl is ScrollBar sb)
        {
            sb.Min = 0;
            sb.Max = max;
            sb.Value = graphic;
        }

        if (WindowManager.TryGetControl("winEventEditor", "lblGraphicValue", out var gvCtrl) && gvCtrl is Label lbl)
            lbl.Text = graphic.ToString();

        if (WindowManager.TryGetControl("winEventEditor", "txtGraphicX", out var gxCtrl) && gxCtrl is TextBox txtX)
            txtX.Text = page.GraphicX.ToString();
        if (WindowManager.TryGetControl("winEventEditor", "txtGraphicY", out var gyCtrl) && gyCtrl is TextBox txtY)
            txtY.Text = page.GraphicY.ToString();
        if (WindowManager.TryGetControl("winEventEditor", "txtGraphicX2", out var gx2Ctrl) && gx2Ctrl is TextBox txtX2)
            txtX2.Text = page.GraphicX2.ToString();
        if (WindowManager.TryGetControl("winEventEditor", "txtGraphicY2", out var gy2Ctrl) && gy2Ctrl is TextBox txtY2)
            txtY2.Text = page.GraphicY2.ToString();
    }

    public static void OnDrawGraphicPreview()
    {
        var win = WindowManager.GetWindowByName("winEventEditor");
        if (win is null) return;

        if (!WindowManager.TryGetControl("winEventEditor", "picGraphic", out var ctrl) || ctrl is not PictureBox pic)
            return;

        if (!TryGetCurrentPage(out var page))
            return;

        int type = page.GraphicType;
        int graphic = page.Graphic;
        if (type <= 0 || graphic <= 0)
            return;

        int drawX = win.X + pic.X;
        int drawY = win.Y + pic.Y;

        switch (type)
        {
            case 1: // Sprite (character sheet)
            {
                if (graphic > GameState.NumCharacters) return;
                var spritePath = Path.Combine(DataPath.Characters, graphic.ToString());
                var sprite = GameClient.GetGfxInfo(spritePath);
                if (sprite is null) return;

                int frameCount = Core.Configurations.SettingsManager.Instance.RunFrames +
                                 Core.Configurations.SettingsManager.Instance.IdleFrames +
                                 Core.Configurations.SettingsManager.Instance.AttackFrames;
                if (frameCount <= 0) frameCount = 1;

                int w = sprite.Width / frameCount;
                int dirs = Math.Max(1, Core.Configurations.SettingsManager.Instance.SpriteDirections);
                if (sprite.Height % dirs != 0) dirs = 4;
                int h = sprite.Height / (dirs == 0 ? 1 : dirs);

                int cx = drawX + (pic.Width - w) / 2;
                int cy = drawY + (pic.Height - h) / 2;
                GameClient.RenderTexture(ref spritePath, cx, cy, 0, 0, w, h, w, h);
                return;
            }

            case 2: // Tileset (single tile preview)
            {
                if (graphic > GameState.NumTileSets) return;
                var tilesetPath = Path.Combine(DataPath.Tilesets, graphic.ToString());
                int tileSize = Core.Globals.Constants.TileSize;
                int srcX = Math.Max(0, page.GraphicX) * tileSize;
                int srcY = Math.Max(0, page.GraphicY) * tileSize;
                int tx = drawX + (pic.Width - tileSize) / 2;
                int ty = drawY + (pic.Height - tileSize) / 2;
                GameClient.RenderTexture(ref tilesetPath, tx, ty, srcX, srcY, tileSize, tileSize, tileSize, tileSize);
                return;
            }

            case 3: // Picture
            {
                if (graphic > GameState.NumPictures) return;
                var picPath = Path.Combine(DataPath.Pictures, graphic.ToString());
                GameClient.RenderTexture(ref picPath, drawX, drawY, 0, 0, pic.Width, pic.Height, pic.Width, pic.Height);
                return;
            }
        }
    }

    public static void UpdatePageSettingsFromControls()
    {
        if (_isLoading) return;
        if (!TryGetCurrentPage(out var page)) return;

        if (WindowManager.TryGetControl("winEventEditor", "cmbTrigger", out var trigCtrl) && trigCtrl is ComboBox cmbTrigger)
            page.Trigger = (byte)Math.Clamp(cmbTrigger.Value, 0, Math.Max(0, cmbTrigger.Items.Count - 1));

        if (WindowManager.TryGetControl("winEventEditor", "cmbPositioning", out var posCtrl) && posCtrl is ComboBox cmbPos)
            page.Position = (byte)Math.Clamp(cmbPos.Value, 0, Math.Max(0, cmbPos.Items.Count - 1));

        if (WindowManager.TryGetControl("winEventEditor", "cmbMoveType", out var mtCtrl) && mtCtrl is ComboBox cmbMoveType)
            page.MoveType = (byte)Math.Clamp(cmbMoveType.Value, 0, Math.Max(0, cmbMoveType.Items.Count - 1));

        if (WindowManager.TryGetControl("winEventEditor", "cmbMoveSpeed", out var msCtrl) && msCtrl is ComboBox cmbMoveSpeed)
            page.MoveSpeed = (byte)Math.Clamp(cmbMoveSpeed.Value, 0, Math.Max(0, cmbMoveSpeed.Items.Count - 1));

        if (WindowManager.TryGetControl("winEventEditor", "cmbMoveFreq", out var mfCtrl) && mfCtrl is ComboBox cmbMoveFreq)
            page.MoveFreq = (byte)Math.Clamp(cmbMoveFreq.Value, 0, Math.Max(0, cmbMoveFreq.Items.Count - 1));

        if (WindowManager.TryGetControl("winEventEditor", "chkWalkAnim", out var waCtrl) && waCtrl is CheckBox chkWalkAnim)
            page.IdleAnim = chkWalkAnim.Value == 1 ? 1 : 0;

        if (WindowManager.TryGetControl("winEventEditor", "chkDirFix", out var dfCtrl) && dfCtrl is CheckBox chkDirFix)
            page.DirFix = chkDirFix.Value == 1 ? 1 : 0;

        if (WindowManager.TryGetControl("winEventEditor", "chkWalkThrough", out var wtCtrl) && wtCtrl is CheckBox chkWalkThrough)
            page.WalkThrough = wtCtrl.Value == 1 ? 1 : 0;

        if (WindowManager.TryGetControl("winEventEditor", "chkShowName", out var snCtrl) && snCtrl is CheckBox chkShowName)
            page.ShowName = snCtrl.Value == 1 ? 1 : 0;

        // Graphic (per-page)
        int graphicType = page.GraphicType;
        if (WindowManager.TryGetControl("winEventEditor", "cmbGraphicType", out var gtCtrl) && gtCtrl is ComboBox cmbGraphicType)
            graphicType = Math.Clamp(cmbGraphicType.Value, 0, Math.Max(0, cmbGraphicType.Items.Count - 1));
        page.GraphicType = (byte)graphicType;

        int max = GetGraphicMaxForType(graphicType);
        if (WindowManager.TryGetControl("winEventEditor", "sldGraphic", out var gCtrl) && gCtrl is ScrollBar sb)
        {
            sb.Min = 0;
            sb.Max = max;
            page.Graphic = Math.Clamp(sb.Value, sb.Min, sb.Max);
        }
        else
        {
            page.Graphic = Math.Clamp(page.Graphic, 0, max);
        }

        if (WindowManager.TryGetControl("winEventEditor", "lblGraphicValue", out var gvCtrl) && gvCtrl is Label lbl)
            lbl.Text = page.Graphic.ToString();

        page.GraphicX = ReadIntTextBox("txtGraphicX", page.GraphicX);
        page.GraphicY = ReadIntTextBox("txtGraphicY", page.GraphicY);
        page.GraphicX2 = ReadIntTextBox("txtGraphicX2", page.GraphicX2);
        page.GraphicY2 = ReadIntTextBox("txtGraphicY2", page.GraphicY2);

        SetCurrentPage(page);
        UpdatePageLabel();
    }
}
