using Client.Game.UI.Controls;
using Core.Globals;
using Core.Objects;
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
    private static readonly System.Collections.Generic.List<int> _varSwitchIndexMap = new();
    private static int _lastVarSwitchTypeValue;

    private static readonly string[] _commandTextControlNames =
    [
        "txtCmdText1",
        "txtCmdText2",
        "txtCmdText3",
        "txtCmdText4",
        "txtCmdText5",
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

    private static readonly System.Collections.Generic.List<int> _cmdPick1ValueMap = new();
    private static readonly System.Collections.Generic.List<int> _cmdPick2ValueMap = new();
    private static string _cmdPick1TargetTextBox = "txtCmdData1";
    private static string _cmdPick2TargetTextBox = "txtCmdData2";

    private static int _dataTargetListIndex;
    private static int _dataTargetCommandIndex;
    private static bool _dataTargetIsNew;
    private static Core.Globals.Type.EventCommand _dataHistoryCommand;
    private static bool _dataHasHistory;

    private static readonly string[] _moveRouteControlNames =
    [
        "lblMoveRoute",
        "lstMoveRoute",
        "sldMoveRoute",
        "btnRouteUp",
        "btnRouteDown",
        "btnRouteLeft",
        "btnRouteRight",
        "btnRouteRemove",
        "btnRouteClear",
    ];

    private const int MaxPageButtons = 30;

    private static bool TryGetEventCommandIndex(int rawIndex, out EventCommand index)
    {
        try
        {
            index = (EventCommand)rawIndex;
            return true;
        }
        catch
        {
            index = default;
            return false;
        }
    }

    private static bool IsConditionalBranch(Core.Globals.Type.EventCommand cmd)
    {
        return TryGetEventCommandIndex(cmd.Index, out var index) && index == EventCommand.ConditionalBranch;
    }

    private static bool IsShowChatBubble(Core.Globals.Type.EventCommand cmd)
    {
        return TryGetEventCommandIndex(cmd.Index, out var index) && index == EventCommand.ShowChatBubble;
    }

    private static bool IsShowChoices(Core.Globals.Type.EventCommand cmd)
    {
        return TryGetEventCommandIndex(cmd.Index, out var index) && index == EventCommand.ShowChoices;
    }

    private static void ResetCommandDataLabels(string windowName)
    {
        if (WindowManager.TryGetControl(windowName, "lblCmdText1", out var t1) && t1 is Label lt1)
            lt1.Text = "Text1";
        if (WindowManager.TryGetControl(windowName, "lblCmdText2", out var t2) && t2 is Label lt2)
            lt2.Text = "Text2";
        if (WindowManager.TryGetControl(windowName, "lblCmdText3", out var t3) && t3 is Label lt3)
            lt3.Text = "Text3";
        if (WindowManager.TryGetControl(windowName, "lblCmdText4", out var t4) && t4 is Label lt4)
            lt4.Text = "Text4";
        if (WindowManager.TryGetControl(windowName, "lblCmdText5", out var t5) && t5 is Label lt5)
            lt5.Text = "Text5";

        if (WindowManager.TryGetControl(windowName, "lblCmdData1", out var d1) && d1 is Label ld1)
            ld1.Text = "D1";
        if (WindowManager.TryGetControl(windowName, "lblCmdData2", out var d2) && d2 is Label ld2)
            ld2.Text = "D2";
        if (WindowManager.TryGetControl(windowName, "lblCmdData3", out var d3) && d3 is Label ld3)
            ld3.Text = "D3";
        if (WindowManager.TryGetControl(windowName, "lblCmdData4", out var d4) && d4 is Label ld4)
            ld4.Text = "D4";
        if (WindowManager.TryGetControl(windowName, "lblCmdData5", out var d5) && d5 is Label ld5)
            ld5.Text = "D5";
        if (WindowManager.TryGetControl(windowName, "lblCmdData6", out var d6) && d6 is Label ld6)
            ld6.Text = "D6";
    }

    private static void ConfigureConditionalBranchLabels(string windowName)
    {
        if (WindowManager.TryGetControl(windowName, "lblCmdData1", out var d1) && d1 is Label ld1)
            ld1.Text = "Cond";
        if (WindowManager.TryGetControl(windowName, "lblCmdData2", out var d2) && d2 is Label ld2)
            ld2.Text = "A";
        if (WindowManager.TryGetControl(windowName, "lblCmdData3", out var d3) && d3 is Label ld3)
            ld3.Text = "B";
        if (WindowManager.TryGetControl(windowName, "lblCmdData4", out var d4) && d4 is Label ld4)
            ld4.Text = "C";
        if (WindowManager.TryGetControl(windowName, "lblCmdData5", out var d5) && d5 is Label ld5)
            ld5.Text = "IfLst";
        if (WindowManager.TryGetControl(windowName, "lblCmdData6", out var d6) && d6 is Label ld6)
            ld6.Text = "Else";
    }

    private static void ConfigureShowChoicesLabels(string windowName)
    {
        if (WindowManager.TryGetControl(windowName, "lblCmdData1", out var d1) && d1 is Label ld1)
            ld1.Text = "Grp 1";
        if (WindowManager.TryGetControl(windowName, "lblCmdData2", out var d2) && d2 is Label ld2)
            ld2.Text = "Grp 2";
        if (WindowManager.TryGetControl(windowName, "lblCmdData3", out var d3) && d3 is Label ld3)
            ld3.Text = "Grp 3";
        if (WindowManager.TryGetControl(windowName, "lblCmdData4", out var d4) && d4 is Label ld4)
            ld4.Text = "Grp 4";
    }

    private static string FormatCommandDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Friendly label for the editor: keep it short.
        if (string.Equals(name, nameof(EventCommand.ConditionalBranch), StringComparison.Ordinal))
            return "Conditional Branch";

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

            // Ensure we have up-to-date variable/switch names for pickers + rename UI.
            Client.Net.Sender.SendRequestSwitchesAndVariables();

            // Snapshot original event for Cancel
            _history = Client.Event.Instance;
            _hasHistory = true;

            PopulateCombos();

            PopulateVarSwitchTypeCombo();
            RefreshVarSwitchList();

            SelectedPage = 0;
            ClampSelectedPage();
            LoadEventToControls();
            LoadPageToControls();
            RefreshMoveRouteControls();
            RefreshCommandsList();
        }
        finally
        {
            _isLoading = false;
            RefreshPageButtons();
        }
    }

    private static string FormatMoveRouteStep(Core.Globals.Type.MoveRoute route)
    {
        return route.Index switch
        {
            1 => "Up",
            2 => "Down",
            3 => "Left",
            4 => "Right",
            _ => $"{route.Index}",
        };
    }

    private static void SetMoveRouteControlsEnabled(bool enabled)
    {
        foreach (var name in _moveRouteControlNames)
        {
            if (WindowManager.TryGetControl("winEventEditor", name, out var ctrl) && ctrl is not null)
                ctrl.Enabled = enabled;
        }
    }

    private static void SetMoveRouteControlsVisible(bool visible)
    {
        foreach (var name in _moveRouteControlNames)
        {
            if (WindowManager.TryGetControl("winEventEditor", name, out var ctrl) && ctrl is not null)
                ctrl.Visible = visible;
        }
    }

    private static void RefreshMoveRouteControls()
    {
        if (!TryGetCurrentPage(out var page))
            return;

        bool isRoute = page.MoveType == 2;
        SetMoveRouteControlsVisible(isRoute);
        SetMoveRouteControlsEnabled(isRoute);

        if (!WindowManager.TryGetControl("winEventEditor", "lstMoveRoute", out var listCtrl) || listCtrl is not ListBox list)
            return;

        int prevSelected = list.SelectedIndex;
        int prevScroll = list.ScrollOffset;

        list.Clear();

        int count = Math.Max(0, page.MoveRouteCount);
        var route = page.MoveRoute ?? Array.Empty<Core.Globals.Type.MoveRoute>();

        for (int i = 0; i < count && i < route.Length; i++)
        {
            list.AddItem($"{i + 1}: {FormatMoveRouteStep(route[i])}");
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

        // Sync scrollbar
        if (WindowManager.TryGetControl("winEventEditor", "sldMoveRoute", out var sldCtrl) && sldCtrl is ScrollBar sb)
        {
            int visible = list.GetVisibleCount();
            int max = Math.Max(0, list.Items.Count - visible);
            sb.Min = 0;
            sb.Max = max;
            sb.Value = Math.Clamp(list.ScrollOffset, sb.Min, sb.Max);
        }
    }

    private static void AddMoveRouteStep(int index)
    {
        if (_isLoading) return;
        if (!TryGetCurrentPage(out var page)) return;

        int count = Math.Max(0, page.MoveRouteCount);
        var route = page.MoveRoute ?? Array.Empty<Core.Globals.Type.MoveRoute>();
        if (route.Length < count)
            Array.Resize(ref route, count);

        Array.Resize(ref route, count + 1);
        route[count] = new Core.Globals.Type.MoveRoute { Index = index };

        page.MoveRoute = route;
        page.MoveRouteCount = count + 1;
        SetCurrentPage(page);
        RefreshMoveRouteControls();
    }

    private static void RemoveMoveRouteStep()
    {
        if (_isLoading) return;
        if (!TryGetCurrentPage(out var page)) return;

        int count = Math.Max(0, page.MoveRouteCount);
        if (count <= 0) return;

        var route = page.MoveRoute ?? Array.Empty<Core.Globals.Type.MoveRoute>();
        if (route.Length < count)
            Array.Resize(ref route, count);

        int removeAt = count - 1;
        if (WindowManager.TryGetControl("winEventEditor", "lstMoveRoute", out var ctrl) && ctrl is ListBox list)
        {
            int selected = list.SelectedIndex;
            if (selected >= 0 && selected < count)
                removeAt = selected;
        }

        for (int i = removeAt; i < count - 1; i++)
            route[i] = route[i + 1];

        if (count - 1 <= 0)
        {
            page.MoveRouteCount = 0;
            page.MoveRoute = Array.Empty<Core.Globals.Type.MoveRoute>();
        }
        else
        {
            Array.Resize(ref route, count - 1);
            page.MoveRoute = route;
            page.MoveRouteCount = count - 1;
        }

        SetCurrentPage(page);
        RefreshMoveRouteControls();
    }

    private static void ClearMoveRoute()
    {
        if (_isLoading) return;
        if (!TryGetCurrentPage(out var page)) return;

        page.MoveRouteCount = 0;
        page.MoveRoute = Array.Empty<Core.Globals.Type.MoveRoute>();
        SetCurrentPage(page);
        RefreshMoveRouteControls();
    }

    public static void OnMoveRouteAddUp() => AddMoveRouteStep(1);
    public static void OnMoveRouteAddDown() => AddMoveRouteStep(2);
    public static void OnMoveRouteAddLeft() => AddMoveRouteStep(3);
    public static void OnMoveRouteAddRight() => AddMoveRouteStep(4);
    public static void OnMoveRouteRemove() => RemoveMoveRouteStep();
    public static void OnMoveRouteClear() => ClearMoveRoute();

    public static void OnMoveRouteListMouseDown()
    {
        if (!WindowManager.TryGetControl("winEventEditor", "lstMoveRoute", out var ctrl) || ctrl is not ListBox list) return;
        var win = WindowManager.GetWindowByName("winEventEditor");
        if (win is null) return;

        int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
        int index = list.GetItemIndexAtPosition(relY);
        if (index < 0 || index >= list.Items.Count) return;

        list.SelectedIndex = index;
        list.EnsureVisible(index);

        if (WindowManager.TryGetControl("winEventEditor", "sldMoveRoute", out var sldCtrl) && sldCtrl is ScrollBar sb)
        {
            int visible = list.GetVisibleCount();
            int max = Math.Max(0, list.Items.Count - visible);
            sb.Min = 0;
            sb.Max = max;
            sb.Value = Math.Clamp(list.ScrollOffset, sb.Min, sb.Max);
        }
    }

    public static void OnMoveRouteListMouseWheel()
    {
        if (!WindowManager.TryGetControl("winEventEditor", "lstMoveRoute", out var ctrl) || ctrl is not ListBox list) return;

        int visible = list.GetVisibleCount();
        int max = Math.Max(0, list.Items.Count - visible);
        int delta = GameClient.GetMouseScrollDelta();
        int step = (delta > 0) ? -1 : 1;

        list.ScrollOffset = Math.Clamp(list.ScrollOffset + step, 0, max);
        if (WindowManager.TryGetControl("winEventEditor", "sldMoveRoute", out var sldCtrl) && sldCtrl is ScrollBar sb)
            sb.Value = Math.Clamp(list.ScrollOffset, sb.Min, sb.Max);
    }

    public static void OnMoveRouteScrollBarMove()
    {
        if (!WindowManager.TryGetControl("winEventEditor", "lstMoveRoute", out var ctrl) || ctrl is not ListBox list) return;
        if (!WindowManager.TryGetControl("winEventEditor", "sldMoveRoute", out var sldCtrl) || sldCtrl is null) return;

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

    private static void PopulateVarSwitchTypeCombo()
    {
        if (!WindowManager.TryGetControl("winEventEditor", "cmbVarSwitchType", out var ctrl) || ctrl is not ComboBox cmb)
            return;

        if (cmb.Items.Count == 0)
        {
            cmb.Items.Add("Switches");
            cmb.Items.Add("Variables");
        }

        cmb.Value = Math.Clamp(cmb.Value, 0, Math.Max(0, cmb.Items.Count - 1));
        _lastVarSwitchTypeValue = cmb.Value;
    }

    private static void SetVarSwitchIndexLabel(int? id)
    {
        if (!WindowManager.TryGetControl("winEventEditor", "lblVarSwitchIndex", out var ctrl) || ctrl is not Label lbl)
            return;
        lbl.Text = id is null ? "Index: -" : $"Index: {id.Value}";
    }

    private static void RefreshVarSwitchList()
    {
        if (!WindowManager.TryGetControl("winEventEditor", "lstVarSwitchNames", out var listCtrl) || listCtrl is not ListBox list)
            return;

        int mode = 0;
        if (WindowManager.TryGetControl("winEventEditor", "cmbVarSwitchType", out var typeCtrl) && typeCtrl is ComboBox cmb)
            mode = Math.Clamp(cmb.Value, 0, 1);

        list.Items.Clear();
        _varSwitchIndexMap.Clear();

        int count = mode == 0 ? Variables.MaxSwitches : Variables.MaxVariables;
        for (int i = 0; i < count; i++)
        {
            string name = mode == 0
                ? (i >= 0 && i < Client.Event.Switches.Length ? (Client.Event.Switches[i] ?? string.Empty) : string.Empty)
                : (i >= 0 && i < Client.Event.Variables.Length ? (Client.Event.Variables[i] ?? string.Empty) : string.Empty);

            name = name.Trim();
            list.Items.Add(string.IsNullOrWhiteSpace(name) ? $"{i}" : $"{i}: {name}");
            _varSwitchIndexMap.Add(i);
        }

        list.SelectedIndex = Math.Clamp(list.SelectedIndex, 0, Math.Max(0, list.Items.Count - 1));
        list.ScrollOffset = Math.Clamp(list.ScrollOffset, 0, Math.Max(0, list.Items.Count - list.GetVisibleCount()));

        if (WindowManager.TryGetControl("winEventEditor", "sldVarSwitchNames", out var sldCtrl) && sldCtrl is ScrollBar sb)
        {
            int visible = list.GetVisibleCount();
            int max = Math.Max(0, list.Items.Count - visible);
            sb.Min = 0;
            sb.Max = max;
            sb.Value = Math.Clamp(list.ScrollOffset, sb.Min, sb.Max);
        }

        if (list.SelectedIndex >= 0 && list.SelectedIndex < _varSwitchIndexMap.Count)
            SetVarSwitchIndexLabel(_varSwitchIndexMap[list.SelectedIndex]);
        else
            SetVarSwitchIndexLabel(null);
    }

    public static void OnVarSwitchTypeChanged()
    {
        if (_isLoading) return;
        if (WindowManager.TryGetControl("winEventEditor", "cmbVarSwitchType", out var ctrl) && ctrl is ComboBox cmb)
        {
            int current = Math.Clamp(cmb.Value, 0, 1);
            if (current == _lastVarSwitchTypeValue)
                return;
            _lastVarSwitchTypeValue = current;
        }

        RefreshVarSwitchList();
    }

    public static void OnVarSwitchListMouseDown()
    {
        if (_isLoading) return;
        if (!WindowManager.TryGetControl("winEventEditor", "lstVarSwitchNames", out var ctrl) || ctrl is not ListBox list) return;
        var win = WindowManager.GetWindowByName("winEventEditor");
        if (win is null) return;

        int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
        int index = list.GetItemIndexAtPosition(relY);
        if (index < 0 || index >= list.Items.Count) return;

        list.SelectedIndex = index;
        list.EnsureVisible(index);

        if (WindowManager.TryGetControl("winEventEditor", "sldVarSwitchNames", out var sldCtrl) && sldCtrl is ScrollBar sb)
        {
            int visible = list.GetVisibleCount();
            int max = Math.Max(0, list.Items.Count - visible);
            sb.Min = 0;
            sb.Max = max;
            sb.Value = Math.Clamp(list.ScrollOffset, sb.Min, sb.Max);
        }

        if (index >= 0 && index < _varSwitchIndexMap.Count)
            SetVarSwitchIndexLabel(_varSwitchIndexMap[index]);
    }

    public static void OnVarSwitchListMouseWheel()
    {
        if (_isLoading) return;
        if (!WindowManager.TryGetControl("winEventEditor", "lstVarSwitchNames", out var ctrl) || ctrl is not ListBox list) return;

        int visible = list.GetVisibleCount();
        int max = Math.Max(0, list.Items.Count - visible);
        int delta = GameClient.GetMouseScrollDelta();
        int step = (delta > 0) ? -1 : 1;

        list.ScrollOffset = Math.Clamp(list.ScrollOffset + step, 0, max);

        if (WindowManager.TryGetControl("winEventEditor", "sldVarSwitchNames", out var sldCtrl) && sldCtrl is ScrollBar sb)
        {
            sb.Min = 0;
            sb.Max = max;
            sb.Value = Math.Clamp(list.ScrollOffset, sb.Min, sb.Max);
        }
    }

    public static void OnVarSwitchScrollBarMove()
    {
        if (_isLoading) return;
        if (!WindowManager.TryGetControl("winEventEditor", "lstVarSwitchNames", out var listCtrl) || listCtrl is not ListBox list) return;
        if (!WindowManager.TryGetControl("winEventEditor", "sldVarSwitchNames", out var sldCtrl) || sldCtrl is not ScrollBar sb) return;

        int visible = list.GetVisibleCount();
        int max = Math.Max(0, list.Items.Count - visible);
        sb.Min = 0;
        sb.Max = max;
        list.ScrollOffset = Math.Clamp(sb.Value, 0, max);
    }

    public static void OnRenameVarSwitch()
    {
        if (_isLoading) return;
        if (!WindowManager.TryGetControl("winEventEditor", "lstVarSwitchNames", out var ctrl) || ctrl is not ListBox list) return;

        int selected = list.SelectedIndex;
        if (selected < 0 || selected >= _varSwitchIndexMap.Count)
            return;

        int id = _varSwitchIndexMap[selected];
        int mode = 0;
        if (WindowManager.TryGetControl("winEventEditor", "cmbVarSwitchType", out var typeCtrl) && typeCtrl is ComboBox cmb)
            mode = Math.Clamp(cmb.Value, 0, 1);

        string kind = mode == 0 ? "switch" : "variable";
        GameLogic.Dialogue("Rename", $"Enter a new name for {kind} {id}.", "", DialogueType.RenameVarSwitch, DialogueStyle.Input, mode, id);
    }

    public static void ApplyVarSwitchRenameFromDialogue(string name, int mode, int id)
    {
        var trimmed = (name ?? string.Empty).Trim();
        mode = Math.Clamp(mode, 0, 1);

        if (mode == 0)
        {
            if (id < 0 || id >= Client.Event.Switches.Length) return;
            Client.Event.Switches[id] = trimmed;
        }
        else
        {
            if (id < 0 || id >= Client.Event.Variables.Length) return;
            Client.Event.Variables[id] = trimmed;
        }

        Client.Net.Sender.SendSwitchesAndVariables();
        RefreshVarSwitchList();

        // Restore selection to the renamed id if present.
        if (WindowManager.TryGetControl("winEventEditor", "lstVarSwitchNames", out var ctrl) && ctrl is ListBox list)
        {
            int idx = _varSwitchIndexMap.IndexOf(id);
            if (idx >= 0 && idx < list.Items.Count)
            {
                list.SelectedIndex = idx;
                list.EnsureVisible(idx);
                SetVarSwitchIndexLabel(id);
            }
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

            // Reset labels each load; specific commands can override.
            ResetCommandDataLabels("winEventEditor");
            if (IsConditionalBranch(cmd))
                ConfigureConditionalBranchLabels("winEventEditor");
            if (IsShowChoices(cmd))
                ConfigureShowChoicesLabels("winEventEditor");

            if (WindowManager.TryGetControl("winEventEditor", "txtCmdText1", out var t1) && t1 is TextBox tb1)
                tb1.Text = cmd.Text1 ?? string.Empty;
            if (WindowManager.TryGetControl("winEventEditor", "txtCmdText2", out var t2) && t2 is TextBox tb2)
                tb2.Text = cmd.Text2 ?? string.Empty;
            if (WindowManager.TryGetControl("winEventEditor", "txtCmdText3", out var t3) && t3 is TextBox tb3)
                tb3.Text = cmd.Text3 ?? string.Empty;
            if (WindowManager.TryGetControl("winEventEditor", "txtCmdText4", out var t4) && t4 is TextBox tb4)
                tb4.Text = cmd.Text4 ?? string.Empty;
            if (WindowManager.TryGetControl("winEventEditor", "txtCmdText5", out var t5) && t5 is TextBox tb5)
                tb5.Text = cmd.Text5 ?? string.Empty;

            if (IsConditionalBranch(cmd))
            {
                if (WindowManager.TryGetControl("winEventEditor", "txtCmdData1", out var d1) && d1 is TextBox db1)
                    db1.Text = cmd.ConditionalBranch.Condition.ToString();
                if (WindowManager.TryGetControl("winEventEditor", "txtCmdData2", out var d2) && d2 is TextBox db2)
                    db2.Text = cmd.ConditionalBranch.Data1.ToString();
                if (WindowManager.TryGetControl("winEventEditor", "txtCmdData3", out var d3) && d3 is TextBox db3)
                    db3.Text = cmd.ConditionalBranch.Data2.ToString();
                if (WindowManager.TryGetControl("winEventEditor", "txtCmdData4", out var d4) && d4 is TextBox db4)
                    db4.Text = cmd.ConditionalBranch.Data3.ToString();
                if (WindowManager.TryGetControl("winEventEditor", "txtCmdData5", out var d5) && d5 is TextBox db5)
                    db5.Text = cmd.ConditionalBranch.CommandList.ToString();
                if (WindowManager.TryGetControl("winEventEditor", "txtCmdData6", out var d6) && d6 is TextBox db6)
                    db6.Text = cmd.ConditionalBranch.ElseCommandList.ToString();
            }
            else
            {
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
            return GetLiveText(tb) ?? string.Empty;

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
        cmd.Text4 = ReadStringTextBox("txtCmdText4");
        cmd.Text5 = ReadStringTextBox("txtCmdText5");

        if (IsConditionalBranch(cmd))
        {
            cmd.ConditionalBranch.Condition = ReadIntTextBox("txtCmdData1", cmd.ConditionalBranch.Condition);
            cmd.ConditionalBranch.Data1 = ReadIntTextBox("txtCmdData2", cmd.ConditionalBranch.Data1);
            cmd.ConditionalBranch.Data2 = ReadIntTextBox("txtCmdData3", cmd.ConditionalBranch.Data2);
            cmd.ConditionalBranch.Data3 = ReadIntTextBox("txtCmdData4", cmd.ConditionalBranch.Data3);
            cmd.ConditionalBranch.CommandList = ReadIntTextBox("txtCmdData5", cmd.ConditionalBranch.CommandList);
            cmd.ConditionalBranch.ElseCommandList = ReadIntTextBox("txtCmdData6", cmd.ConditionalBranch.ElseCommandList);

            // Mirror into Data fields for quick preview in the simple list view.
            cmd.Data1 = cmd.ConditionalBranch.Condition;
            cmd.Data2 = cmd.ConditionalBranch.Data1;
            cmd.Data3 = cmd.ConditionalBranch.Data2;
            cmd.Data4 = cmd.ConditionalBranch.Data3;
            cmd.Data5 = cmd.ConditionalBranch.CommandList;
            cmd.Data6 = cmd.ConditionalBranch.ElseCommandList;
        }
        else
        {
            cmd.Data1 = ReadIntTextBox("txtCmdData1", cmd.Data1);
            cmd.Data2 = ReadIntTextBox("txtCmdData2", cmd.Data2);
            cmd.Data3 = ReadIntTextBox("txtCmdData3", cmd.Data3);
            cmd.Data4 = ReadIntTextBox("txtCmdData4", cmd.Data4);
            cmd.Data5 = ReadIntTextBox("txtCmdData5", cmd.Data5);
            cmd.Data6 = ReadIntTextBox("txtCmdData6", cmd.Data6);
        }

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
        var committed = tb.Text;
        var live = ReferenceEquals(WindowManager.ActiveWindow?.ActiveControl, tb)
            ? committed + (GameState.ChatShowLine)
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
        // Reset labels each load; specific commands can override.
        ResetCommandDataLabels(windowName);
        if (IsConditionalBranch(cmd))
            ConfigureConditionalBranchLabels(windowName);

        if (IsShowChoices(cmd))
            ConfigureShowChoicesLabels(windowName);

        if (TryGetEventCommandIndex(cmd.Index, out var idx) && idx == EventCommand.ShowChoices)
        {
            if (WindowManager.TryGetControl(windowName, "lblCmdText1", out var lbl1Ctrl) && lbl1Ctrl is Label lt1)
                lt1.Text = "Prompt";
            if (WindowManager.TryGetControl(windowName, "lblCmdText2", out var lbl2Ctrl) && lbl2Ctrl is Label lt2)
                lt2.Text = "Choice 1";
            if (WindowManager.TryGetControl(windowName, "lblCmdText3", out var lbl3Ctrl) && lbl3Ctrl is Label lt3)
                lt3.Text = "Choice 2";
            if (WindowManager.TryGetControl(windowName, "lblCmdText4", out var lbl4Ctrl) && lbl4Ctrl is Label lt4)
                lt4.Text = "Choice 3";
            if (WindowManager.TryGetControl(windowName, "lblCmdText5", out var lbl5Ctrl) && lbl5Ctrl is Label lt5)
                lt5.Text = "Choice 4";
        }
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
        if (WindowManager.TryGetControl(windowName, "txtCmdText4", out var t4) && t4 is TextBox tb4)
            tb4.Text = cmd.Text4 ?? string.Empty;
        if (WindowManager.TryGetControl(windowName, "txtCmdText5", out var t5) && t5 is TextBox tb5)
            tb5.Text = cmd.Text5 ?? string.Empty;

        if (IsConditionalBranch(cmd))
        {
            if (WindowManager.TryGetControl(windowName, "txtCmdData1", out var d1) && d1 is TextBox db1)
                db1.Text = cmd.ConditionalBranch.Condition.ToString();
            if (WindowManager.TryGetControl(windowName, "txtCmdData2", out var d2) && d2 is TextBox db2)
                db2.Text = cmd.ConditionalBranch.Data1.ToString();
            if (WindowManager.TryGetControl(windowName, "txtCmdData3", out var d3) && d3 is TextBox db3)
                db3.Text = cmd.ConditionalBranch.Data2.ToString();
            if (WindowManager.TryGetControl(windowName, "txtCmdData4", out var d4) && d4 is TextBox db4)
                db4.Text = cmd.ConditionalBranch.Data3.ToString();
            if (WindowManager.TryGetControl(windowName, "txtCmdData5", out var d5) && d5 is TextBox db5)
                db5.Text = cmd.ConditionalBranch.CommandList.ToString();
            if (WindowManager.TryGetControl(windowName, "txtCmdData6", out var d6) && d6 is TextBox db6)
                db6.Text = cmd.ConditionalBranch.ElseCommandList.ToString();
        }
        else
        {
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

        if (WindowManager.TryGetControl("winEventCommandData", "cmbCmdText1", out var text1PickCtrl) && text1PickCtrl is ComboBox cmbText1Pick)
        {
            int last = cmbText1Pick.Value;
            cmbText1Pick.CallBack[(int)ControlState.MouseMove] = () =>
            {
                if (_isLoading)
                    return;

                if (cmbText1Pick.Value == last)
                    return;

                last = cmbText1Pick.Value;

                if (WindowManager.TryGetControl("winEventCommandData", "txtCmdText1", out var d1) && d1 is TextBox tb)
                    tb.Text = GetComboSelectedName(cmbText1Pick);
            };
        }

        void WireDataPicker(string comboName, System.Collections.Generic.List<int> valueMap, Func<string> targetTextBox)
        {
            if (WindowManager.TryGetControl("winEventCommandData", comboName, out var pickCtrl) && pickCtrl is ComboBox cmbPick)
            {
                int last = cmbPick.Value;
                cmbPick.CallBack[(int)ControlState.MouseMove] = () =>
                {
                    if (_isLoading)
                        return;

                    if (cmbPick.Value == last)
                        return;

                    last = cmbPick.Value;

                    int mapped = cmbPick.Value;
                    if (cmbPick.Value >= 0 && cmbPick.Value < valueMap.Count)
                        mapped = valueMap[cmbPick.Value];

                    var target = targetTextBox();
                    if (!string.IsNullOrWhiteSpace(target) && WindowManager.TryGetControl("winEventCommandData", target, out var d1) && d1 is TextBox tb)
                        tb.Text = mapped.ToString();

                    // Conditional Branch: when the Type changes, refresh the Value picker items/target.
                    if (comboName == "cmbCmdPick1")
                    {
                        if (TryGetCommandAt(_dataTargetListIndex, _dataTargetCommandIndex, out var cmd) && (IsConditionalBranch(cmd) || IsShowChatBubble(cmd)))
                        {
                            // Build a lightweight "live" view of the command based on current textbox state.
                            var live = cmd;
                            if (IsConditionalBranch(cmd))
                            {
                                live.ConditionalBranch.Condition = ReadIntTextBox("winEventCommandData", "txtCmdData1", live.ConditionalBranch.Condition);
                                live.ConditionalBranch.Data1 = ReadIntTextBox("winEventCommandData", "txtCmdData2", live.ConditionalBranch.Data1);
                                live.ConditionalBranch.Data2 = ReadIntTextBox("winEventCommandData", "txtCmdData3", live.ConditionalBranch.Data2);
                                live.ConditionalBranch.Data3 = ReadIntTextBox("winEventCommandData", "txtCmdData4", live.ConditionalBranch.Data3);
                            }
                            else
                            {
                                // ShowChatBubble: target type drives which secondary picker we show.
                                live.Data1 = ReadIntTextBox("winEventCommandData", "txtCmdData1", live.Data1);
                                live.Data2 = ReadIntTextBox("winEventCommandData", "txtCmdData2", live.Data2);
                            }

                            _isLoading = true;
                            try
                            {
                                ConfigureCommandDataPicker(live);
                            }
                            finally
                            {
                                _isLoading = false;
                            }
                        }
                    }
                };
            }
        }

        WireDataPicker("cmbCmdPick1", _cmdPick1ValueMap, () => _cmdPick1TargetTextBox);
        WireDataPicker("cmbCmdPick2", _cmdPick2ValueMap, () => _cmdPick2TargetTextBox);
    }

    private static void ConfigureCommandDataPickerRow(
        string labelName,
        string comboName,
        System.Collections.Generic.List<int> valueMap,
        string? pickLabel,
        (int value, string name)[]? items,
        string targetTextBox,
        int currentValue)
    {
        if (!WindowManager.TryGetControl("winEventCommandData", labelName, out var lblCtrl) || lblCtrl is not Label lbl)
            return;
        if (!WindowManager.TryGetControl("winEventCommandData", comboName, out var cmbCtrl) || cmbCtrl is not ComboBox cmb)
            return;

        if (string.IsNullOrWhiteSpace(pickLabel) || items is null || items.Length <= 0)
        {
            lbl.Visible = false;
            cmb.Visible = false;
            valueMap.Clear();
            return;
        }

        lbl.Text = pickLabel;
        lbl.Visible = true;
        cmb.Visible = true;

        valueMap.Clear();
        cmb.Items.Clear();

        for (int i = 0; i < items.Length; i++)
        {
            var (value, name) = items[i];
            var display = string.IsNullOrWhiteSpace(name) ? "None" : name;
            cmb.Items.Add(display);
            valueMap.Add(value);
        }

        // Select current value.
        int selected = 0;
        for (int i = 0; i < valueMap.Count; i++)
        {
            if (valueMap[i] == currentValue)
            {
                selected = i;
                break;
            }
        }
        cmb.Value = Math.Clamp(selected, 0, Math.Max(0, cmb.Items.Count - 1));

        if (comboName == "cmbCmdPick1")
            _cmdPick1TargetTextBox = targetTextBox;
        else if (comboName == "cmbCmdPick2")
            _cmdPick2TargetTextBox = targetTextBox;
    }

    private static void ConfigureCommandDataPicker(Core.Globals.Type.EventCommand cmd)
    {
        // Default: hide both pickers.
        ConfigureCommandDataPickerRow("lblCmdPick1", "cmbCmdPick1", _cmdPick1ValueMap, null, null, "txtCmdData1", 0);
        ConfigureCommandDataPickerRow("lblCmdPick2", "cmbCmdPick2", _cmdPick2ValueMap, null, null, "txtCmdData2", 0);

        string? pick1Label = null;
        string? pick2Label = null;
        (int value, string name)[]? pick1Items = null;
        (int value, string name)[]? pick2Items = null;
        string pick1Target = "txtCmdData1";
        string pick2Target = "txtCmdData2";
        int pick1CurrentValue = cmd.Data1;
        int pick2CurrentValue = cmd.Data2;

        EventCommand index;
        try { index = (EventCommand)cmd.Index; }
        catch { index = default; }

        int playerMap = Commands.GetPlayerMap(Client.GameState.MyIndex);
        bool hasPlayerMap = playerMap >= 0 && playerMap < Client.Map.Instance.Count;

        switch (index)
        {
            case EventCommand.AddText:
                pick1Label = "Color";
                pick1Items = BuildEnumItems<ColorName>();
                break;

            case EventCommand.ShowChatBubble:
                pick1Label = "Target";
                pick1Target = "txtCmdData1";
                pick1Items =
                [
                    ((int)TargetType.Player, "Player"),
                    ((int)TargetType.Npc, "Npc"),
                    ((int)TargetType.Event, "Event"),
                ];

                // Data2 means different things based on target.
                // - Npc: runtime npc index (map slot)
                // - Event: map event index
                if ((byte)cmd.Data1 == (byte)TargetType.Npc)
                {
                    pick2Label = "Npc";
                    pick2Target = "txtCmdData2";
                    pick2Items = BuildIndexItems(
                        hasPlayerMap && Client.Map.Instance[playerMap].Npc != null ? Client.Map.Instance[playerMap].Npc.Length : 0,
                        slot =>
                        {
                            if (!hasPlayerMap)
                                return null;

                            var map = Client.Map.Instance[playerMap];
                            if (map.Npc == null || slot < 0 || slot >= map.Npc.Length)
                                return null;

                            int npcNum = map.Npc[slot];
                            if (npcNum < 0)
                                return "None";

                            var npcName = npcNum < NpcBase.Instance.Count ? (NpcBase.Instance[npcNum].Name ?? string.Empty) : string.Empty;
                            npcName = string.IsNullOrWhiteSpace(npcName) ? "None" : npcName.Trim();
                            return $"{npcName} ({npcNum})";
                        });
                }
                else if ((byte)cmd.Data1 == (byte)TargetType.Event)
                {
                    pick2Label = "Event";
                    pick2Target = "txtCmdData2";
                    pick2Items = BuildIndexItems(
                        hasPlayerMap && Client.Map.Instance[playerMap].Event != null ? Client.Map.Instance[playerMap].Event.Length : 0,
                        i =>
                        {
                            if (!hasPlayerMap)
                                return null;

                            var map = Client.Map.Instance[playerMap];
                            if (map.Event == null || i < 0 || i >= map.Event.Length)
                                return null;

                            var name = map.Event[i].Name ?? string.Empty;
                            name = string.IsNullOrWhiteSpace(name) ? "None" : name.Trim();
                            return name;
                        });
                }
                break;
            case EventCommand.OpenShop:
                pick1Label = "Shop";
                pick1Items = BuildIndexItems(Variables.MaxShops, i => i >= 0 && i < Shop.Instance.Count ? Shop.Instance[i].Name : null);
                break;
            case EventCommand.PlayAnimation:
                pick1Label = "Animation";
                pick1Items = BuildIndexItems(Variables.MaxAnimations, i => i >= 0 && i < AnimationBase.Instance.Count ? AnimationBase.Instance[i].Name : null);
                break;
            case EventCommand.ChangeItems:
                pick1Label = "Item";
                pick1Items = BuildIndexItems(Variables.MaxItems, i => i >= 0 && i < ItemBase.Instance.Count ? ItemBase.Instance[i].Name : null);
                break;
            case EventCommand.ChangeSkills:
                pick1Label = "Skill";
                pick1Items = BuildIndexItems(Variables.MaxSkills, i => i >= 0 && i < SkillBase.Instance.Count ? SkillBase.Instance[i].Name : null);
                break;
            case EventCommand.ChangeJob:
                pick1Label = "Job";
                pick1Items = BuildIndexItems(Variables.MaxJobs, i => i >= 0 && i < JobBase.Instance.Count ? JobBase.Instance[i].Name : null);
                break;

            case EventCommand.ChangeSex:
                pick1Label = "Sex";
                pick1Items = BuildEnumItems<Sex>();
                break;

            case EventCommand.ChangeSprite:
                pick1Label = "Sprite";
                pick1Target = "txtCmdData1";
                pick1Items = BuildIndexItems(Math.Max(0, Client.GameState.NumCharacters + 1), i => i == 0 ? "None" : $"{i}");
                pick1CurrentValue = cmd.Data1;
                break;

            case EventCommand.SetAccessLevel:
                pick1Label = "Access";
                pick1Items = BuildEnumItems<AccessLevel>();
                break;

            case EventCommand.SpawnNpc:
                pick1Label = "Npc";
                pick1Items = BuildIndexItems(
                    hasPlayerMap && Client.Map.Instance[playerMap].Npc != null ? Client.Map.Instance[playerMap].Npc.Length : 0,
                    slot =>
                    {
                        if (!hasPlayerMap)
                            return null;

                        var map = Client.Map.Instance[playerMap];
                        if (map.Npc == null || slot < 0 || slot >= map.Npc.Length)
                            return null;

                        int npcNum = map.Npc[slot];
                        if (npcNum < 0)
                            return "(empty)";

                        var npcName = npcNum < NpcBase.Instance.Count ? (NpcBase.Instance[npcNum].Name ?? string.Empty) : string.Empty;
                        npcName = string.IsNullOrWhiteSpace(npcName) ? "(unnamed)" : npcName.Trim();
                        return $"{npcName} (npc {npcNum})";
                    });
                break;

            case EventCommand.SetFog:
                pick1Label = "Fog";
                pick1Items = BuildIndexItems(Math.Max(0, Client.GameState.NumFogs + 1), i => i == 0 ? "None" : $"{i}");
                break;

            case EventCommand.ShowPicture:
                pick1Label = "Picture";
                pick1Items = BuildIndexItems(Math.Max(0, Client.GameState.NumPictures + 1), i => i == 0 ? "None" : $"{i}");
                break;

            case EventCommand.Variable:
                pick1Label = "Variable";
                pick1Target = "txtCmdData1";
                pick1Items = BuildIndexItems(Variables.MaxVariables, i => $"{i}");

                pick2Label = "Value";
                pick2Target = "txtCmdData2";
                pick2Items =
                [
                    (0, "Set"),
                    (1, "Add"),
                    (2, "Subtract"),
                    (3, "Random"),
                ];
                pick2CurrentValue = cmd.Data2;
                break;

            case EventCommand.Switch:
                pick1Label = "Switch";
                pick1Target = "txtCmdData1";
                pick1Items = BuildIndexItems(Variables.MaxSwitches, i => $"{i}");

                pick2Label = "Value";
                pick2Target = "txtCmdData2";
                pick2Items =
                [
                    (0, "Off"),
                    (1, "On"),
                ];
                pick2CurrentValue = cmd.Data2;
                break;

            case EventCommand.SelfSwitch:
                pick1Label = "Self-Switch";
                pick1Target = "txtCmdData1";
                pick1Items =
                [
                    (0, "A"),
                    (1, "B"),
                    (2, "C"),
                    (3, "D"),
                ];

                pick2Label = "Value";
                pick2Target = "txtCmdData2";
                pick2Items =
                [
                    (0, "Off"),
                    (1, "On"),
                ];
                pick2CurrentValue = cmd.Data2;
                break;

            case EventCommand.ConditionalBranch:
                pick1Label = "Type";
                pick1Target = "txtCmdData1";
                pick1CurrentValue = cmd.ConditionalBranch.Condition;
                pick1Items =
                [
                    (0, "Variable"),
                    (1, "Switch"),
                    (2, "Item"),
                    (3, "Job"),
                    (4, "Skill"),
                    (5, "Level"),
                    (6, "Self Switch"),
                    (7, "Timer"),
                    (8, "Gender"),
                    (9, "Time of Day"),
                ];

                // Make the most common Conditional Branch values easier to pick.
                // Mapping (server-side):
                // - Variable: Data1=VarId, Data2=Operator, Data3=CompareValue
                // - Switch:   Data1=SwitchId, Data2=RequiredState (0=On, 1=Off)
                // - Item:     Data1=ItemId, Data2=Amount
                // - Job:      Data1=JobId
                // - Skill:    Data1=SkillId
                // - Level:    Data1=CompareLevel, Data2=Operator
                // - SelfSw:   Data1=SelfSwitch(A-D), Data2=RequiredState (0=On, 1=Off)
                // - Gender:   Data1=Sex
                // - Time:     Data1=TimeOfDay
                switch (cmd.ConditionalBranch.Condition)
                {
                    case 2: // Item
                        pick2Label = "Item";
                        pick2Target = "txtCmdData2";
                        pick2CurrentValue = cmd.ConditionalBranch.Data1;
                        pick2Items = BuildIndexItems(Variables.MaxItems, i => i >= 0 && i < ItemBase.Instance.Count ? ItemBase.Instance[i].Name : null);
                        break;

                    case 3: // Job
                        pick2Label = "Job";
                        pick2Target = "txtCmdData2";
                        pick2CurrentValue = cmd.ConditionalBranch.Data1;
                        pick2Items = BuildIndexItems(Variables.MaxJobs, i => i >= 0 && i < JobBase.Instance.Count ? JobBase.Instance[i].Name : null);
                        break;

                    case 4: // Skill
                        pick2Label = "Skill";
                        pick2Target = "txtCmdData2";
                        pick2CurrentValue = cmd.ConditionalBranch.Data1;
                        pick2Items = BuildIndexItems(Variables.MaxSkills, i => i >= 0 && i < SkillBase.Instance.Count ? SkillBase.Instance[i].Name : null);
                        break;

                    case 0: // Variable
                    case 5: // Level
                        pick2Label = "Operator";
                        pick2Target = "txtCmdData3";
                        pick2CurrentValue = cmd.ConditionalBranch.Data2;
                        pick2Items =
                        [
                            (0, "=="),
                            (1, ">="),
                            (2, "<="),
                            (3, ">"),
                            (4, "<"),
                            (5, "!="),
                        ];
                        break;

                    case 1: // Switch
                        pick2Label = "Value";
                        pick2Target = "txtCmdData3";
                        pick2CurrentValue = cmd.ConditionalBranch.Data2;
                        pick2Items =
                        [
                            (0, "On"),
                            (1, "Off"),
                        ];
                        break;

                    case 6: // Self Switch
                        pick2Label = "Value";
                        pick2Target = "txtCmdData2";
                        pick2CurrentValue = cmd.ConditionalBranch.Data1;
                        pick2Items =
                        [
                            (0, "A"),
                            (1, "B"),
                            (2, "C"),
                            (3, "D"),
                        ];
                        break;

                    case 8: // Gender
                        pick2Label = "Value";
                        pick2Target = "txtCmdData2";
                        pick2CurrentValue = cmd.ConditionalBranch.Data1;
                        pick2Items = BuildEnumItems<Sex>();
                        break;

                    case 9: // Time of Day
                        pick2Label = "Value";
                        pick2Target = "txtCmdData2";
                        pick2CurrentValue = cmd.ConditionalBranch.Data1;
                        pick2Items = BuildEnumItems<Core.TimeOfDay>();
                        break;
                }
                break;
        }

        ConfigureCommandDataPickerRow(
            "lblCmdPick1",
            "cmbCmdPick1",
            _cmdPick1ValueMap,
            pick1Label,
            pick1Items,
            pick1Target,
            pick1CurrentValue);

        ConfigureCommandDataPickerRow(
            "lblCmdPick2",
            "cmbCmdPick2",
            _cmdPick2ValueMap,
            pick2Label,
            pick2Items,
            pick2Target,
            pick2CurrentValue);
    }

    private static (int value, string name)[] BuildEnumItems<T>() where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var result = new (int value, string name)[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            int v = Convert.ToInt32(values[i]);
            result[i] = (v, Enum.GetName(typeof(T), values[i]) ?? v.ToString());
        }
        return result;
    }

    private static (int value, string name)[] BuildIndexItems(int count, Func<int, string?> nameFor)
    {
        if (count <= 0)
            return Array.Empty<(int value, string name)>();

        var items = new (int value, string name)[count];
        for (int i = 0; i < count; i++)
        {
            var name = nameFor(i);
            if (string.IsNullOrWhiteSpace(name))
                name = "None";
            items[i] = (i, $"{i}: {name}");
        }
        return items;
    }

    private static string GetComboSelectedName(ComboBox cmb)
    {
        if (cmb.Value < 0 || cmb.Value >= cmb.Items.Count)
            return string.Empty;

        var display = cmb.Items[cmb.Value] ?? string.Empty;
        if (string.Equals(display, "None", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        var sep = display.IndexOf(": ", StringComparison.Ordinal);
        return sep >= 0 ? display.Substring(sep + 2) : display;
    }

    private static void ConfigureCommandText1Picker(Core.Globals.Type.EventCommand cmd)
    {
        if (!WindowManager.TryGetControl("winEventCommandData", "lblCmdText1", out var lblCtrl) || lblCtrl is not Label lbl)
            return;
        if (!WindowManager.TryGetControl("winEventCommandData", "txtCmdText1", out var txtCtrl) || txtCtrl is not TextBox txt)
            return;
        if (!WindowManager.TryGetControl("winEventCommandData", "cmbCmdText1", out var cmbCtrl) || cmbCtrl is not ComboBox cmb)
            return;

        EventCommand index;
        try { index = (EventCommand)cmd.Index; }
        catch { index = default; }

        bool useMusic = index == EventCommand.PlayBgm;
        bool useSound = index == EventCommand.PlaySound;
        if (!useMusic && !useSound)
        {
            lbl.Text = "Text1";
            txt.Visible = true;
            cmb.Visible = false;
            return;
        }

        lbl.Text = useMusic ? "Music" : "Sound";
        txt.Visible = false;
        cmb.Visible = true;

        cmb.Items.Clear();
        cmb.Items.Add("None");

        try
        {
            if (useMusic)
            {
                General.CacheMusic();
                for (int i = 0; i < Audio.MusicCache.Length; i++)
                {
                    var name = Audio.MusicCache[i] ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name))
                        cmb.Items.Add($"{i + 1}: {name}");
                }
            }
            else
            {
                General.CacheSound();
                for (int i = 0; i < Audio.SoundCache.Length; i++)
                {
                    var name = Audio.SoundCache[i] ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name))
                        cmb.Items.Add($"{i + 1}: {name}");
                }
            }
        }
        catch
        {
            // If caches fail (missing folders, etc.), keep the combo minimal.
        }

        // Select the current command.Text1 if it exists in the list.
        int found = 0;
        if (!string.IsNullOrWhiteSpace(cmd.Text1))
        {
            for (int i = 0; i < cmb.Items.Count; i++)
            {
                var itemName = i == 0 ? string.Empty : GetComboSelectedNameFromItem(cmb.Items[i]);
                if (string.Equals(itemName, cmd.Text1, StringComparison.OrdinalIgnoreCase))
                {
                    found = i;
                    break;
                }
            }
        }
        cmb.Value = Math.Clamp(found, 0, Math.Max(0, cmb.Items.Count - 1));

        // Keep Text1 textbox consistent so existing code paths still work.
        txt.Text = GetComboSelectedName(cmb);
    }

    private static string GetComboSelectedNameFromItem(string? display)
    {
        var s = display ?? string.Empty;
        if (string.Equals(s, "None", StringComparison.OrdinalIgnoreCase))
            return string.Empty;
        var sep = s.IndexOf(": ", StringComparison.Ordinal);
        return sep >= 0 ? s.Substring(sep + 2) : s;
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
        {
            _isLoading = true;
            try
            {
                LoadCommandToWindow("winEventCommandData", cmd);
                ConfigureCommandDataPicker(cmd);
                ConfigureCommandText1Picker(cmd);
            }
            finally
            {
                _isLoading = false;
            }
        }
    }

    public static void OnCommandDataOk()
    {
        int listIndex = _dataTargetListIndex;
        int commandIndex = _dataTargetCommandIndex;

        if (!TryGetCommandAt(listIndex, commandIndex, out var cmd) || cmd.Index < 0)
            return;

        // For sound/music commands we prefer the combo box selection (if visible).
        cmd.Text1 = ReadStringTextBox("winEventCommandData", "txtCmdText1");
        if (WindowManager.TryGetControl("winEventCommandData", "cmbCmdText1", out var cmbCtrl) && cmbCtrl is ComboBox cmb && cmb.Visible)
        {
            cmd.Text1 = GetComboSelectedName(cmb);
        }
        cmd.Text2 = ReadStringTextBox("winEventCommandData", "txtCmdText2");
        cmd.Text3 = ReadStringTextBox("winEventCommandData", "txtCmdText3");
        cmd.Text4 = ReadStringTextBox("winEventCommandData", "txtCmdText4");
        cmd.Text5 = ReadStringTextBox("winEventCommandData", "txtCmdText5");

        if (IsConditionalBranch(cmd))
        {
            int cond = ReadIntTextBox("winEventCommandData", "txtCmdData1", cmd.ConditionalBranch.Condition);
            int a = ReadIntTextBox("winEventCommandData", "txtCmdData2", cmd.ConditionalBranch.Data1);
            int b = ReadIntTextBox("winEventCommandData", "txtCmdData3", cmd.ConditionalBranch.Data2);
            int c = ReadIntTextBox("winEventCommandData", "txtCmdData4", cmd.ConditionalBranch.Data3);
            int ifList = ReadIntTextBox("winEventCommandData", "txtCmdData5", cmd.ConditionalBranch.CommandList);
            int elseList = ReadIntTextBox("winEventCommandData", "txtCmdData6", cmd.ConditionalBranch.ElseCommandList);

            cmd.ConditionalBranch.Condition = cond;
            cmd.ConditionalBranch.Data1 = a;
            cmd.ConditionalBranch.Data2 = b;
            cmd.ConditionalBranch.Data3 = c;
            cmd.ConditionalBranch.CommandList = ifList;
            cmd.ConditionalBranch.ElseCommandList = elseList;

            // Mirror for preview.
            cmd.Data1 = cond;
            cmd.Data2 = a;
            cmd.Data3 = b;
            cmd.Data4 = c;
            cmd.Data5 = ifList;
            cmd.Data6 = elseList;
        }
        else
        {
            cmd.Data1 = ReadIntTextBox("winEventCommandData", "txtCmdData1", cmd.Data1);
            cmd.Data2 = ReadIntTextBox("winEventCommandData", "txtCmdData2", cmd.Data2);
            cmd.Data3 = ReadIntTextBox("winEventCommandData", "txtCmdData3", cmd.Data3);
            cmd.Data4 = ReadIntTextBox("winEventCommandData", "txtCmdData4", cmd.Data4);
            cmd.Data5 = ReadIntTextBox("winEventCommandData", "txtCmdData5", cmd.Data5);
            cmd.Data6 = ReadIntTextBox("winEventCommandData", "txtCmdData6", cmd.Data6);
        }

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

        // Page conditions
        if (WindowManager.TryGetControl("winEventEditor", "cmbVariableCompare", out var vcCtrl) && vcCtrl is ComboBox cmbVarCompare)
        {
            if (cmbVarCompare.Items.Count == 0)
            {
                cmbVarCompare.Items.Clear();
                cmbVarCompare.Items.Add("==");
                cmbVarCompare.Items.Add(">=");
                cmbVarCompare.Items.Add("<=");
                cmbVarCompare.Items.Add(">");
                cmbVarCompare.Items.Add("<");
                cmbVarCompare.Items.Add("!=");
            }
        }

        if (WindowManager.TryGetControl("winEventEditor", "cmbSwitchIndex", out var siCtrl) && siCtrl is ComboBox cmbSwitchIndex)
        {
            if (cmbSwitchIndex.Items.Count == 0)
            {
                cmbSwitchIndex.Items.Clear();
                var items = BuildIndexItems(
                    Variables.MaxSwitches,
                    i => i >= 0 && Client.Event.Switches != null && i < Client.Event.Switches.Length ? Client.Event.Switches[i] : null);
                for (int i = 0; i < items.Length; i++)
                    cmbSwitchIndex.Items.Add(items[i].name);
            }
        }

        if (WindowManager.TryGetControl("winEventEditor", "cmbVariableIndex", out var viCtrl) && viCtrl is ComboBox cmbVariableIndex)
        {
            if (cmbVariableIndex.Items.Count == 0)
            {
                cmbVariableIndex.Items.Clear();
                var items = BuildIndexItems(
                    Variables.MaxVariables,
                    i => i >= 0 && Client.Event.Variables != null && i < Client.Event.Variables.Length ? Client.Event.Variables[i] : null);
                for (int i = 0; i < items.Length; i++)
                    cmbVariableIndex.Items.Add(items[i].name);
            }
        }

        if (WindowManager.TryGetControl("winEventEditor", "cmbHasItemIndex", out var hiCtrl) && hiCtrl is ComboBox cmbHasItemIndex)
        {
            if (cmbHasItemIndex.Items.Count == 0)
            {
                cmbHasItemIndex.Items.Clear();
                var items = BuildIndexItems(Variables.MaxItems, i => i >= 0 && i < ItemBase.Instance.Count ? ItemBase.Instance[i].Name : null);
                for (int i = 0; i < items.Length; i++)
                    cmbHasItemIndex.Items.Add(items[i].name);
            }
        }

        if (WindowManager.TryGetControl("winEventEditor", "cmbSwitchCompare", out var scCtrl) && scCtrl is ComboBox cmbSwitchCompare)
        {
            if (cmbSwitchCompare.Items.Count == 0)
            {
                cmbSwitchCompare.Items.Clear();
                cmbSwitchCompare.Items.Add("Off");
                cmbSwitchCompare.Items.Add("On");
            }
        }

        if (WindowManager.TryGetControl("winEventEditor", "cmbSelfSwitchCompare", out var sscCtrl) && sscCtrl is ComboBox cmbSelfSwitchCompare)
        {
            if (cmbSelfSwitchCompare.Items.Count == 0)
            {
                cmbSelfSwitchCompare.Items.Clear();
                cmbSelfSwitchCompare.Items.Add("Off");
                cmbSelfSwitchCompare.Items.Add("On");
            }
        }

        if (WindowManager.TryGetControl("winEventEditor", "cmbSelfSwitchIndex", out var ssIdxCtrl) && ssIdxCtrl is ComboBox cmbSelfSwitchIndex)
        {
            if (cmbSelfSwitchIndex.Items.Count == 0)
            {
                cmbSelfSwitchIndex.Items.Clear();
                cmbSelfSwitchIndex.Items.Add("A");
                cmbSelfSwitchIndex.Items.Add("B");
                cmbSelfSwitchIndex.Items.Add("C");
                cmbSelfSwitchIndex.Items.Add("D");
            }
        }
    }

    private static void RefreshPageButtons()
    {
        int pageCount = Math.Clamp(Math.Max(1, Client.Event.Instance.PageCount), 1, MaxPageButtons);
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
        int pageCount = Math.Clamp(Math.Max(1, Client.Event.Instance.PageCount), 1, MaxPageButtons);
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

        // Positioning
        if (WindowManager.TryGetControl("winEventEditor", "cmbPositioning", out var posCtrl) && posCtrl is ComboBox cmbPos)
            cmbPos.Value = Math.Clamp(page.Position, 0, cmbPos.Items.Count - 1);

        // Move settings
        if (WindowManager.TryGetControl("winEventEditor", "cmbMoveType", out var mtCtrl) && mtCtrl is ComboBox cmbMoveType)
            cmbMoveType.Value = Math.Clamp(page.MoveType, 0, cmbMoveType.Items.Count - 1);

        if (WindowManager.TryGetControl("winEventEditor", "cmbMoveSpeed", out var msCtrl) && msCtrl is ComboBox cmbMoveSpeed)
            cmbMoveSpeed.Value = Math.Clamp(page.MoveSpeed, 0, cmbMoveSpeed.Items.Count - 1);

        if (WindowManager.TryGetControl("winEventEditor", "cmbMoveFreq", out var mfCtrl) && mfCtrl is ComboBox cmbMoveFreq)
            cmbMoveFreq.Value = Math.Clamp(page.MoveFreq, 0, cmbMoveFreq.Items.Count - 1);

        // Flags
        if (WindowManager.TryGetControl("winEventEditor", "chkWalkAnim", out var waCtrl) && waCtrl is CheckBox chkWalkAnim)
            chkWalkAnim.Value = page.IdleAnim == 1 ? 1 : 0;

        if (WindowManager.TryGetControl("winEventEditor", "chkDirFix", out var dfCtrl) && dfCtrl is CheckBox chkDirFix)
            chkDirFix.Value = page.DirFix == 1 ? 1 : 0;

        if (WindowManager.TryGetControl("winEventEditor", "chkWalkThrough", out var wtCtrl) && wtCtrl is CheckBox chkWalkThrough)
            chkWalkThrough.Value = page.WalkThrough == 1 ? 1 : 0;

        if (WindowManager.TryGetControl("winEventEditor", "chkShowName", out var snCtrl) && snCtrl is CheckBox chkShowName)
            chkShowName.Value = page.ShowName == 1 ? 1 : 0;

        // Conditions
        if (WindowManager.TryGetControl("winEventEditor", "chkPageHasItem", out var hiCtrl) && hiCtrl is CheckBox chkHasItem)
            chkHasItem.Value = page.ChkHasItem == 1 ? 1 : 0;
        if (WindowManager.TryGetControl("winEventEditor", "cmbHasItemIndex", out var hiiCtrl) && hiiCtrl is ComboBox cmbHasItemIndex)
            cmbHasItemIndex.Value = Math.Clamp(page.HasItemIndex, 0, Math.Max(0, cmbHasItemIndex.Items.Count - 1));
        if (WindowManager.TryGetControl("winEventEditor", "txtHasItemValue", out var hivCtrl) && hivCtrl is TextBox txtHasItemValue)
            txtHasItemValue.Text = page.HasItemAmount.ToString();

        if (WindowManager.TryGetControl("winEventEditor", "chkPageSwitch", out var swCtrl) && swCtrl is CheckBox chkSwitch)
            chkSwitch.Value = page.ChkSwitch == 1 ? 1 : 0;
        if (WindowManager.TryGetControl("winEventEditor", "cmbSwitchIndex", out var swiCtrl) && swiCtrl is ComboBox cmbSwitchIndex)
            cmbSwitchIndex.Value = Math.Clamp(page.SwitchIndex, 0, Math.Max(0, cmbSwitchIndex.Items.Count - 1));
        if (WindowManager.TryGetControl("winEventEditor", "cmbSwitchCompare", out var swcCtrl) && swcCtrl is ComboBox cmbSwitchCompare)
            cmbSwitchCompare.Value = Math.Clamp(page.SwitchCompare, 0, Math.Max(0, cmbSwitchCompare.Items.Count - 1));

        if (WindowManager.TryGetControl("winEventEditor", "chkPageVariable", out var vCtrl) && vCtrl is CheckBox chkVariable)
            chkVariable.Value = page.ChkVariable == 1 ? 1 : 0;
        if (WindowManager.TryGetControl("winEventEditor", "cmbVariableIndex", out var viCtrl) && viCtrl is ComboBox cmbVariableIndex)
            cmbVariableIndex.Value = Math.Clamp(page.VariableIndex, 0, Math.Max(0, cmbVariableIndex.Items.Count - 1));
        if (WindowManager.TryGetControl("winEventEditor", "cmbVariableCompare", out var vcCtrl) && vcCtrl is ComboBox cmbVariableCompare)
            cmbVariableCompare.Value = Math.Clamp(page.VariableCompare, 0, Math.Max(0, cmbVariableCompare.Items.Count - 1));
        if (WindowManager.TryGetControl("winEventEditor", "txtVariableValue", out var vvCtrl) && vvCtrl is TextBox txtVariableValue)
            txtVariableValue.Text = page.VariableCondition.ToString();

        if (WindowManager.TryGetControl("winEventEditor", "chkPageSelfSwitch", out var ssCtrl) && ssCtrl is CheckBox chkSelfSwitch)
            chkSelfSwitch.Value = page.ChkSelfSwitch == 1 ? 1 : 0;
        if (WindowManager.TryGetControl("winEventEditor", "cmbSelfSwitchIndex", out var ssiCtrl) && ssiCtrl is ComboBox cmbSelfSwitchIndex)
            cmbSelfSwitchIndex.Value = Math.Clamp(page.SelfSwitchIndex, 0, Math.Max(0, cmbSelfSwitchIndex.Items.Count - 1));
        if (WindowManager.TryGetControl("winEventEditor", "cmbSelfSwitchCompare", out var sscCtrl) && sscCtrl is ComboBox cmbSelfSwitchCompare)
            cmbSelfSwitchCompare.Value = Math.Clamp(page.SelfSwitchCompare, 0, Math.Max(0, cmbSelfSwitchCompare.Items.Count - 1));

        // Graphic (per-page)
        SyncGraphicControlsFromPage(page);

        RefreshMoveRouteControls();

        UpdatePageLabel();
    }

    private static void UpdatePageLabel()
    {
        if (WindowManager.TryGetControl("winEventEditor", "lblPage", out var pageCtrl) && pageCtrl is Label lblPage)
        {
            int pageCount = Math.Clamp(Math.Max(1, Client.Event.Instance.PageCount), 1, MaxPageButtons);
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

        string GetListLabel(int listIndex)
        {
            // If this list is a ShowChoices sub-list, display it as "Choice N".
            for (int parentListIndex = 0; parentListIndex < page.CommandListCount && parentListIndex < page.CommandList.Length; parentListIndex++)
            {
                var parentList = page.CommandList[parentListIndex];
                if (parentList.CommandCount <= 0 || parentList.Commands == null)
                    continue;

                for (int parentCmdIndex = 0; parentCmdIndex < parentList.CommandCount && parentCmdIndex < parentList.Commands.Length; parentCmdIndex++)
                {
                    var parentCmd = parentList.Commands[parentCmdIndex];
                    if (!IsShowChoices(parentCmd))
                        continue;

                    string WithText(int n, int data, string text)
                    {
                        // Show the underlying DataN (list index) since that's what the engine uses.
                        var prefix = $"Choice {n} (D{n}:{data})";
                        if (!string.IsNullOrWhiteSpace(text))
                            return $"{prefix}: {text}";
                        return prefix;
                    }

                    // ShowChoices stores choices in Text2..Text5 (Text1 is prompt).
                    if (parentCmd.Data1 == listIndex) return WithText(1, parentCmd.Data1, parentCmd.Text2);
                    if (parentCmd.Data2 == listIndex) return WithText(2, parentCmd.Data2, parentCmd.Text3);
                    if (parentCmd.Data3 == listIndex) return WithText(3, parentCmd.Data3, parentCmd.Text4);
                    if (parentCmd.Data4 == listIndex) return WithText(4, parentCmd.Data4, parentCmd.Text5);
                }
            }

            return listIndex.ToString();
        }

        for (int listIndex = 0; listIndex < page.CommandListCount && listIndex < page.CommandList.Length; listIndex++)
        {
            var cmdList = page.CommandList[listIndex];
            string listLabel = GetListLabel(listIndex);
            if (cmdList.CommandCount <= 0 || cmdList.Commands == null)
            {
                list.AddItem($"{listLabel}:(empty)");
                _commandIndexMap.Add((listIndex, -1));
                continue;
            }

            bool any = false;

            for (int cmdIndex = 0; cmdIndex < cmdList.CommandCount && cmdIndex < cmdList.Commands.Length; cmdIndex++)
            {
                var cmd = cmdList.Commands[cmdIndex];
                if (cmd.Index < 0) continue;

                any = true;

                string name;
                try { name = ((EventCommand)cmd.Index).ToString(); }
                catch { name = cmd.Index.ToString(); }

                string preview = cmd.Text1;
                if (!string.IsNullOrWhiteSpace(preview))
                    preview = preview.Length > 24 ? preview.Substring(0, 24) + "..." : preview;

                var data = $"(D1:{cmd.Data1} D2:{cmd.Data2} D3:{cmd.Data3} D4:{cmd.Data4} D5:{cmd.Data5} D6:{cmd.Data6})";

                var line = string.IsNullOrWhiteSpace(preview)
                    ? $"{listLabel}:{cmdIndex} {name} {data}"
                    : $"{listLabel}:{cmdIndex} {name} {data} - {preview}";

                list.AddItem(line);
                _commandIndexMap.Add((listIndex, cmdIndex));
            }

            if (!any)
            {
                list.AddItem($"{listLabel}:(empty)");
                _commandIndexMap.Add((listIndex, -1));
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
        if (commandIndex < 0) return;
        OpenCommandPicker(listIndex, commandIndex, isNew: false);
    }

    public static void OnEditCommand()
    {
        if (_isLoading) return;
        if (!WindowManager.TryGetControl("winEventEditor", "lstCommands", out var ctrl) || ctrl is not ListBox list) return;

        int selectedIndex = list.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex >= _commandIndexMap.Count) return;

        var (listIndex, commandIndex) = _commandIndexMap[selectedIndex];
        if (commandIndex < 0) return;
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

        // Append to the currently selected list (so nested lists are editable).
        int targetList = 0;
        if (WindowManager.TryGetControl("winEventEditor", "lstCommands", out var listCtrl) && listCtrl is ListBox lb)
        {
            int selectedIndex = lb.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < _commandIndexMap.Count)
            {
                var (selectedListIndex, selectedCommandIndex) = _commandIndexMap[selectedIndex];
                targetList = Math.Clamp(selectedListIndex, 0, Math.Max(0, listCount - 1));

                // If user has the ShowChoices command selected, default Add to Choice 1 list.
                if (selectedCommandIndex >= 0 && TryGetCommandAt(selectedListIndex, selectedCommandIndex, out var selectedCmd) && IsShowChoices(selectedCmd))
                {
                    int choice1List = selectedCmd.Data1;
                    if (choice1List >= 0 && choice1List < listCount)
                        targetList = choice1List;
                }
            }
        }

        var cmdList = lists[targetList];
        int cmdCount = Math.Max(0, cmdList.CommandCount);
        var cmds = cmdList.Commands ?? Array.Empty<Core.Globals.Type.EventCommand>();
        Array.Resize(ref cmds, cmdCount + 1);
        cmds[cmdCount].Index = -1;

        cmdList.Commands = cmds;
        cmdList.CommandCount = cmdCount + 1;
        lists[targetList] = cmdList;

        page.CommandList = lists;
        page.CommandListCount = listCount;

        SetCurrentPage(page);

        RefreshCommandsList();
        OpenCommandPicker(targetList, cmdCount, isNew: true);
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

        // If this is a Conditional Branch, auto-create/assign the If/Else command lists.
        if (TryGetEventCommandIndex(pickedIndex, out var idx) && idx == EventCommand.ConditionalBranch)
        {
            int nextListIndex = Math.Max(0, listCount);
            int required = nextListIndex + 2;

            if (lists.Length < required)
                Array.Resize(ref lists, required);

            // Ensure the page knows it has these lists.
            listCount = Math.Max(listCount, required);

            int ifList = nextListIndex;
            int elseList = nextListIndex + 1;

            if (lists[ifList].Commands == null || lists[ifList].Commands.Length == 0)
            {
                lists[ifList].ParentList = li;
                lists[ifList].CommandCount = 1;
                lists[ifList].Commands = new Core.Globals.Type.EventCommand[1];
                lists[ifList].Commands[0].Index = -1;
            }

            if (lists[elseList].Commands == null || lists[elseList].Commands.Length == 0)
            {
                lists[elseList].ParentList = li;
                lists[elseList].CommandCount = 1;
                lists[elseList].Commands = new Core.Globals.Type.EventCommand[1];
                lists[elseList].Commands[0].Index = -1;
            }

            ref var cmd = ref cmds[ci];
            cmd.ConditionalBranch.CommandList = ifList;
            cmd.ConditionalBranch.ElseCommandList = elseList;

            // Mirror into Data for preview.
            cmd.Data5 = ifList;
            cmd.Data6 = elseList;
        }

        // If this is ShowChoices, auto-create/assign 4 choice sub-groups.
        if (TryGetEventCommandIndex(pickedIndex, out var idx2) && idx2 == EventCommand.ShowChoices)
        {
            int nextListIndex = Math.Max(0, listCount);
            int required = nextListIndex + 4;

            if (lists.Length < required)
                Array.Resize(ref lists, required);

            listCount = Math.Max(listCount, required);

            for (int k = 0; k < 4; k++)
            {
                int listId = nextListIndex + k;
                if (lists[listId].Commands == null || lists[listId].Commands.Length == 0)
                {
                    lists[listId].ParentList = li;
                    lists[listId].CommandCount = 1;
                    lists[listId].Commands = new Core.Globals.Type.EventCommand[1];
                    lists[listId].Commands[0].Index = -1;
                }
            }

            ref var cmd = ref cmds[ci];
            cmd.Data1 = nextListIndex;
            cmd.Data2 = nextListIndex + 1;
            cmd.Data3 = nextListIndex + 2;
            cmd.Data4 = nextListIndex + 3;
        }

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

    public static void OnClearCommands()
    {
        if (_isLoading) return;
        if (!TryGetCurrentPage(out var page)) return;

        // Reset to a single empty list with a single placeholder command.
        page.CommandListCount = 1;
        page.CommandList = new Core.Globals.Type.CommandList[1];
        page.CommandList[0].CommandCount = 1;
        page.CommandList[0].Commands = new Core.Globals.Type.EventCommand[1];
        page.CommandList[0].Commands[0].Index = -1;

        SetCurrentPage(page);
        RefreshCommandsList();
    }

    public static void OnAddPage()
    {
        if (_isLoading) return;

        var ev = Client.Event.Instance;
        int pageCount = Math.Max(1, ev.PageCount);

        // UI only supports 30 pages (buttons 1..30).
        if (pageCount >= MaxPageButtons)
            return;

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
            page.IdleAnim = (byte)(chkWalkAnim.Value == 1 ? 1 : 0);

        if (WindowManager.TryGetControl("winEventEditor", "chkDirFix", out var dfCtrl) && dfCtrl is CheckBox chkDirFix)
            page.DirFix = (byte)(chkDirFix.Value == 1 ? 1 : 0);

        if (WindowManager.TryGetControl("winEventEditor", "chkWalkThrough", out var wtCtrl) && wtCtrl is CheckBox chkWalkThrough)
            page.WalkThrough = wtCtrl.Value == 1 ? 1 : 0;

        if (WindowManager.TryGetControl("winEventEditor", "chkShowName", out var snCtrl) && snCtrl is CheckBox chkShowName)
            page.ShowName = snCtrl.Value == 1 ? 1 : 0;

        // Conditions
        if (WindowManager.TryGetControl("winEventEditor", "chkPageHasItem", out var hiCtrl) && hiCtrl is CheckBox chkHasItem)
            page.ChkHasItem = chkHasItem.Value == 1 ? 1 : 0;
        if (WindowManager.TryGetControl("winEventEditor", "cmbHasItemIndex", out var hiiCtrl) && hiiCtrl is ComboBox cmbHasItemIndex)
            page.HasItemIndex = Math.Clamp(cmbHasItemIndex.Value, 0, Math.Max(0, cmbHasItemIndex.Items.Count - 1));
        page.HasItemAmount = ReadIntTextBox("txtHasItemValue", page.HasItemAmount);

        if (WindowManager.TryGetControl("winEventEditor", "chkPageSwitch", out var swCtrl) && swCtrl is CheckBox chkSwitch)
            page.ChkSwitch = chkSwitch.Value == 1 ? 1 : 0;
        if (WindowManager.TryGetControl("winEventEditor", "cmbSwitchIndex", out var swiCtrl) && swiCtrl is ComboBox cmbSwitchIndex)
            page.SwitchIndex = Math.Clamp(cmbSwitchIndex.Value, 0, Math.Max(0, cmbSwitchIndex.Items.Count - 1));
        if (WindowManager.TryGetControl("winEventEditor", "cmbSwitchCompare", out var swcCtrl) && swcCtrl is ComboBox cmbSwitchCompare)
            page.SwitchCompare = Math.Clamp(cmbSwitchCompare.Value, 0, Math.Max(0, cmbSwitchCompare.Items.Count - 1));

        if (WindowManager.TryGetControl("winEventEditor", "chkPageVariable", out var vCtrl) && vCtrl is CheckBox chkVariable)
            page.ChkVariable = chkVariable.Value == 1 ? 1 : 0;
        if (WindowManager.TryGetControl("winEventEditor", "cmbVariableIndex", out var viCtrl) && viCtrl is ComboBox cmbVariableIndex)
            page.VariableIndex = Math.Clamp(cmbVariableIndex.Value, 0, Math.Max(0, cmbVariableIndex.Items.Count - 1));
        page.VariableCondition = ReadIntTextBox("txtVariableValue", page.VariableCondition);
        if (WindowManager.TryGetControl("winEventEditor", "cmbVariableCompare", out var vcCtrl) && vcCtrl is ComboBox cmbVariableCompare)
            page.VariableCompare = Math.Clamp(cmbVariableCompare.Value, 0, Math.Max(0, cmbVariableCompare.Items.Count - 1));

        if (WindowManager.TryGetControl("winEventEditor", "chkPageSelfSwitch", out var ssCtrl) && ssCtrl is CheckBox chkSelfSwitch)
            page.ChkSelfSwitch = chkSelfSwitch.Value == 1 ? 1 : 0;
        if (WindowManager.TryGetControl("winEventEditor", "cmbSelfSwitchIndex", out var ssiCtrl) && ssiCtrl is ComboBox cmbSelfSwitchIndex)
            page.SelfSwitchIndex = Math.Clamp(cmbSelfSwitchIndex.Value, 0, Math.Max(0, cmbSelfSwitchIndex.Items.Count - 1));
        if (WindowManager.TryGetControl("winEventEditor", "cmbSelfSwitchCompare", out var sscCtrl) && sscCtrl is ComboBox cmbSelfSwitchCompare)
            page.SelfSwitchCompare = Math.Clamp(cmbSelfSwitchCompare.Value, 0, Math.Max(0, cmbSelfSwitchCompare.Items.Count - 1));

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
        RefreshMoveRouteControls();
    }
}
