using Client.Game.UI;
using Client.Game.UI.Controls;
using Core.Globals;
using System;
using System;

namespace Client.Game.UI.Windows;
public class WinMoralEditor
{
    public static int SelectedIndex = 0;
    public static Moral? _history = null;

    // Initialize window (called after layout is loaded)
    public static void Init()
    {
        if (!WindowManager.TryGetControl("winMoralEditor", "lstIndex", out _))
            return; // window not present yet
        SelectedIndex = 0;
        RefreshList();
        OnLoad(SelectedIndex);
    }

    // List click handler (window-relative like other editors)
    public static void OnListMouseDown()
    {
        if (!WindowManager.TryGetControl("winMoralEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list) return;
        var win = WindowManager.GetWindowByName("winMoralEditor");
        if (win is null) return;
        int relY = GameClient.CurrentMouseState.Y - (win.Y + list.Y);
        int index = list.GetItemIndexAtPosition(relY);
        if (index < 0 || index >= Variables.MaxMorals) return;
        SelectedIndex = index;
        GameState.EditorIndex = index;
        list.SelectedIndex = index;
        list.EnsureVisible(index);
        OnLoad(index);
    }

    public static void OnLoad(int id)
    {
        if (id < 0 || id >= Variables.MaxMorals) return;
        SelectedIndex = id;
        GameState.EditorIndex = id;
        var m = Moral.Instance[id];

        // Text box
        if (WindowManager.TryGetControl("winMoralEditor", "txtName", out var txtName) && txtName is TextBox tbName)
            tbName.Text = m.Name ?? string.Empty;

        // Color combo
        if (WindowManager.TryGetControl("winMoralEditor", "cmbColor", out var cmbColorCtrl) && cmbColorCtrl is ComboBox cmbColor)
        {
            if (cmbColor.Items.Count == 0)
            {
                foreach (var name in Enum.GetNames(typeof(ColorName))) cmbColor.Items.Add(name);
            }
            cmbColor.Value = Math.Clamp(m.Color, 0, Math.Max(0, cmbColor.Items.Count - 1));
        }

        // Helper to set checkbox value
        void SetChk(string name, bool value)
        {
            if (WindowManager.TryGetControl("winMoralEditor", name, out var chkCtrl) && chkCtrl is CheckBox cb)
                cb.Value = value ? 1 : 0;
        }
        SetChk("chkCanCast", m.CanCast);
        SetChk("chkCanPK", m.CanPk);
        SetChk("chkCanPickupItem", m.CanPickupItem);
        SetChk("chkCanDropItem", m.CanDropItem);
        SetChk("chkCanUseItem", m.CanUseItem);
        SetChk("chkDropItems", m.DropItems);
        SetChk("chkLoseExp", m.LoseExp);
        SetChk("chkPlayerBlock", m.PlayerBlock);
        SetChk("chkNpcBlock", m.NpcBlock);
    }

    // Refresh list display names
    public static void RefreshList()
    {
        if (WindowManager.TryGetControl("winMoralEditor", "lstIndex", out var lstCtrl) && lstCtrl is ListBox lst)
        {
            int sel = SelectedIndex;
            lst.Items.Clear();
            for (int i = 0; i < Variables.MaxMorals; i++)
            {
                var name = Moral.Instance[i].Name ?? string.Empty;
                lst.Items.Add($"{i + 1}: {name}");
            }
            if (sel < 0 || sel >= lst.Items.Count) sel = 0;
            lst.SelectedIndex = sel;
            lst.EnsureVisible(sel);
        }
    }

    // Toggle Copy -> Paste on subsequent clicks. Paste overwrites current SelectedIndex.
    public static void OnCopyOrPaste()
    {
        if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxMorals) return;
        if (_history is null)
        {
            var s = Moral.Instance[SelectedIndex];
            var n = s; // struct copy
            _history = (Moral)n;
            if (WindowManager.TryGetControl("winMoralEditor", "btnCopy", out var btn) && btn is Button b) b.Text = "Paste";
            return;
        }

        // Paste clipboard into current slot
        var pasted = _history;
        Moral.Instance[SelectedIndex] = pasted;
        Moral.IsChanged[SelectedIndex] = true;
        _history = null;
        if (WindowManager.TryGetControl("winMoralEditor", "btnCopy", out var btn2) && btn2 is Button b2) b2.Text = "Copy";
        OnLoad(SelectedIndex);
        RefreshList();
    }

    // Unified handlers for callbacks
    public static void OnSave()
    {
        Editors.MoralEditorOK();
        WindowManager.HideWindow("winMoralEditor");
    }

    public static void OnCancel()
    {
        Editors.MoralEditorCancel();
        WindowManager.HideWindow("winMoralEditor");
    }

    public static void OnDelete()
    {
        Moral.OnClear(GameState.EditorIndex);
        Moral.IsChanged[GameState.EditorIndex] = true;
        OnLoad(GameState.EditorIndex);
        RefreshList();
    }

    public static void OnCopy()
    {
        OnCopyOrPaste();
    }
}
