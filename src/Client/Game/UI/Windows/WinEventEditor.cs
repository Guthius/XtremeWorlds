using Client.Game.UI.Controls;
using Core.Globals;
using Microsoft.VisualBasic;
using System;

namespace Client.Game.UI.Windows;

public static class WinEventEditor
{
    public static int SelectedPage { get; private set; }
    private static bool _isLoading;
    private static Core.Globals.Type.Event _history;
    private static bool _hasHistory;
    private static readonly System.Collections.Generic.List<(int listIndex, int commandIndex)> _commandIndexMap = new();

    public static void Init()
    {
        if (!WindowManager.TryGetControl("winEventEditor", "txtName", out _))
            return;

        _isLoading = true;
        try
        {
            // Initialize the editor backing data
            global::Client.Event.EventEditorInit();

            // Snapshot original event for Cancel
            _history = global::Client.Event.Instance;
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
        }
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
    }

    private static void ClampSelectedPage()
    {
        int pageCount = Math.Max(1, global::Client.Event.Instance.PageCount);
        SelectedPage = Math.Clamp(SelectedPage, 0, pageCount - 1);
        global::Client.Event.CurPageNum = SelectedPage;
    }

    private static bool TryGetCurrentPage(out Core.Globals.Type.EventPage page)
    {
        page = default;
        if (global::Client.Event.Instance.Pages == null || global::Client.Event.Instance.Pages.Length == 0)
            return false;

        ClampSelectedPage();
        if (SelectedPage < 0 || SelectedPage >= global::Client.Event.Instance.Pages.Length)
            return false;

        page = global::Client.Event.Instance.Pages[SelectedPage];
        return true;
    }

    private static void SetCurrentPage(Core.Globals.Type.EventPage page)
    {
        if (global::Client.Event.Instance.Pages == null || global::Client.Event.Instance.Pages.Length == 0)
            return;

        ClampSelectedPage();
        global::Client.Event.Instance.Pages[SelectedPage] = page;
    }

    private static void LoadEventToControls()
    {
        // Name
        if (WindowManager.TryGetControl("winEventEditor", "txtName", out var nameCtrl) && nameCtrl is TextBox txtName)
        {
            txtName.Text = global::Client.Event.Instance.Name ?? string.Empty;
        }

        // Global
        if (WindowManager.TryGetControl("winEventEditor", "chkGlobal", out var globalCtrl) && globalCtrl is CheckBox chkGlobal)
        {
            chkGlobal.Value = global::Client.Event.Instance.Globals == 1 ? 1 : 0;
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

        // Move type/speed/freq
        if (WindowManager.TryGetControl("winEventEditor", "cmbMoveType", out var mtCtrl) && mtCtrl is ComboBox cmbMoveType)
            cmbMoveType.Value = Math.Clamp(page.MoveType, 0, cmbMoveType.Items.Count - 1);

        if (WindowManager.TryGetControl("winEventEditor", "cmbMoveSpeed", out var msCtrl) && msCtrl is ComboBox cmbMoveSpeed)
            cmbMoveSpeed.Value = Math.Clamp(page.MoveSpeed, 0, cmbMoveSpeed.Items.Count - 1);

        if (WindowManager.TryGetControl("winEventEditor", "cmbMoveFreq", out var mfCtrl) && mfCtrl is ComboBox cmbMoveFreq)
            cmbMoveFreq.Value = Math.Clamp(page.MoveFreq, 0, cmbMoveFreq.Items.Count - 1);

        // Flags
        if (WindowManager.TryGetControl("winEventEditor", "chkWalkAnim", out var waCtrl) && waCtrl is CheckBox chkWalkAnim)
            chkWalkAnim.Value = page.IdleAnim != 0 ? 1 : 0;

        if (WindowManager.TryGetControl("winEventEditor", "chkDirFix", out var dfCtrl) && dfCtrl is CheckBox chkDirFix)
            chkDirFix.Value = page.DirFix != 0 ? 1 : 0;

        if (WindowManager.TryGetControl("winEventEditor", "chkWalkThrough", out var wtCtrl) && wtCtrl is CheckBox chkWalkThrough)
            chkWalkThrough.Value = page.WalkThrough != 0 ? 1 : 0;

        if (WindowManager.TryGetControl("winEventEditor", "chkShowName", out var snCtrl) && snCtrl is CheckBox chkShowName)
            chkShowName.Value = page.ShowName != 0 ? 1 : 0;

        UpdatePageLabel();
    }

    private static void UpdatePageLabel()
    {
        if (WindowManager.TryGetControl("winEventEditor", "lblPage", out var pageCtrl) && pageCtrl is Label lblPage)
        {
            int pageCount = Math.Max(1, global::Client.Event.Instance.PageCount);
            int display = Math.Clamp(SelectedPage + 1, 1, pageCount);
            lblPage.Text = $"{display} / {pageCount}";
        }
    }

    public static void RefreshCommandsList()
    {
        if (!WindowManager.TryGetControl("winEventEditor", "lstCommands", out var lstCtrl) || lstCtrl is not ListBox list)
            return;

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

        // Sync scrollbar max/value if present
        if (WindowManager.TryGetControl("winEventEditor", "sldCommands", out var sldCtrl) && sldCtrl is ScrollBar sb)
        {
            int visible = list.GetVisibleCount();
            int max = Math.Max(0, list.Items.Count - visible);
            sb.Min = 0;
            sb.Max = max;
            sb.Value = Math.Clamp(list.ScrollOffset, sb.Min, sb.Max);
        }
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
        if (!WindowManager.TryGetControl("winEventEditor", "sldCommands", out var sldCtrl)) return;

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
        finally { _isLoading = false; }
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
        finally { _isLoading = false; }
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

        var ev = global::Client.Event.Instance;
        int pageCount = Math.Max(1, ev.PageCount);

        var pages = ev.Pages ?? Array.Empty<Core.Globals.Type.EventPage>();
        Array.Resize(ref pages, pageCount + 1);
        pages[pageCount] = CreateDefaultPage();

        ev.Pages = pages;
        ev.PageCount = pageCount + 1;
        global::Client.Event.Instance = ev;

        SelectedPage = pageCount;
        ClampSelectedPage();

        _isLoading = true;
        try
        {
            LoadEventToControls();
            LoadPageToControls();
            RefreshCommandsList();
        }
        finally { _isLoading = false; }
    }

    public static void OnDeletePage()
    {
        if (_isLoading) return;

        var ev = global::Client.Event.Instance;
        int pageCount = Math.Max(1, ev.PageCount);
        if (pageCount <= 1) return;
        if (ev.Pages == null || ev.Pages.Length < pageCount) return;

        int removeAt = Math.Clamp(SelectedPage, 0, pageCount - 1);

        for (int i = removeAt; i < pageCount - 1; i++)
            ev.Pages[i] = ev.Pages[i + 1];

        Array.Resize(ref ev.Pages, pageCount - 1);
        ev.PageCount = pageCount - 1;
        global::Client.Event.Instance = ev;

        SelectedPage = Math.Clamp(removeAt, 0, ev.PageCount - 1);
        ClampSelectedPage();

        _isLoading = true;
        try
        {
            LoadEventToControls();
            LoadPageToControls();
            RefreshCommandsList();
        }
        finally { _isLoading = false; }
    }

    public static void OnOk()
    {
        global::Client.Event.EventEditorOK();
        global::Client.Event.InEvent = false;
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

            global::Client.Event.Instance = _history;
        }

        global::Client.Event.InEvent = false;
        WindowManager.HideWindow("winEventEditor");
    }

    public static void SetEventNameFromControl(string? name)
    {
        if (_isLoading) return;
        global::Client.Event.Instance.Name = name ?? string.Empty;
    }

    public static void ToggleGlobalFromControl(int value)
    {
        if (_isLoading) return;
        global::Client.Event.Instance.Globals = (byte)(value == 1 ? 1 : 0);
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

        SetCurrentPage(page);
        UpdatePageLabel();
    }
}
