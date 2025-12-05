using Client;
using Client.Game.UI;
using Client.Game.UI.Controls;
using Core.Globals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Client.Game.UI.Windows;
public class WinResourceEditor
{
    public static int SelectedIndex = 0;
    public static bool IsLoading = false;
    public static Core.Globals.Type.Resource? _history = null;

    // Initialize window (called after layout is loaded)
    public static void Init()
    {
        if (!WindowManager.TryGetControl("winResourceEditor", "lstIndex", out _))
            return; // window not present yet
        SelectedIndex = Math.Clamp(SelectedIndex, 0, Variables.MaxResources - 1);
        RefreshList();
        OnLoad(SelectedIndex);
    }

    // List click handler (window-relative like other editors)
    public static void OnListMouseDown()
    {
        if (!WindowManager.TryGetControl("winResourceEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list) return;
        var win = WindowManager.GetWindowByName("winResourceEditor");
        if (win is null) return;
        int relY = GameClient.CurrentMouseState.Y - (win.Y + list.Y);
        int index = list.GetItemIndexAtPosition(relY);
        if (index < 0 || index >= Variables.MaxResources) return;
        SelectedIndex = index;
        GameState.EditorIndex = index;
        list.SelectedIndex = index;
        list.EnsureVisible(index);
        OnLoad(index);
    }

    public static void OnLoad(int idx)
    {
        if (idx < 0 || idx >= Variables.MaxResources) return;
        SelectedIndex = idx;
        GameState.EditorIndex = idx;
        ref var r = ref Data.Resource[idx];

        // Text boxes
        if (WindowManager.TryGetControl("winResourceEditor", "txtName", out var txtName) && txtName is TextBox tbName)
            tbName.Text = r.Name ?? string.Empty;
        if (WindowManager.TryGetControl("winResourceEditor", "txtMessage", out var txtMsg) && txtMsg is TextBox tbMsg)
            tbMsg.Text = r.SuccessMessage ?? string.Empty;
        if (WindowManager.TryGetControl("winResourceEditor", "txtMessage2", out var txtMsg2) && txtMsg2 is TextBox tbMsg2)
            tbMsg2.Text = r.EmptyMessage ?? string.Empty;

        // Combos
        if (WindowManager.TryGetControl("winResourceEditor", "cmbType", out var cmbTypeCtrl) && cmbTypeCtrl is ComboBox cmbType)
        {
            if (cmbType.Items.Count == 0)
            {
                foreach (var name in Enum.GetNames(typeof(ResourceSkill))) cmbType.Items.Add(name);
            }
            cmbType.Value = Math.Clamp(r.ResourceType, 0, cmbType.Items.Count - 1);
        }
        if (WindowManager.TryGetControl("winResourceEditor", "cmbRewardItem", out var cmbRewardCtrl) && cmbRewardCtrl is ComboBox cmbReward)
        {
            if (cmbReward.Items.Count == 0)
            {
                for (int i = 0; i < Variables.MaxItems; i++)
                {
                    var nm = Data.Item[i].Name ?? string.Empty;
                    cmbReward.Items.Add($"{i + 1}: {nm}");
                }
            }
            cmbReward.Value = Math.Clamp(r.ItemReward, 0, cmbReward.Items.Count - 1);
        }
        if (WindowManager.TryGetControl("winResourceEditor", "cmbTool", out var cmbToolCtrl) && cmbToolCtrl is ComboBox cmbTool)
        {
            if (cmbTool.Items.Count == 0)
            {
                foreach (var name in Enum.GetNames(typeof(ToolType))) cmbTool.Items.Add(name);
            }
            cmbTool.Value = Math.Clamp(r.ToolRequired, 0, cmbTool.Items.Count - 1);
        }
        if (WindowManager.TryGetControl("winResourceEditor", "cmbAnimation", out var cmbAnimCtrl) && cmbAnimCtrl is ComboBox cmbAnim)
        {
            if (cmbAnim.Items.Count == 0)
            {
                for (int i = 0; i < Variables.MaxAnimations; i++)
                {
                    var nm = Data.Animation[i].Name ?? string.Empty;
                    cmbAnim.Items.Add($"{i + 1}: {nm}");
                }
            }
            cmbAnim.Value = Math.Clamp(r.Animation, 0, cmbAnim.Items.Count - 1);
        }

        // Scrollbars + labels helper
        void SetBar(string barName, string labelName, int value, int min, int max)
        {
            if (WindowManager.TryGetControl("winResourceEditor", barName, out var barCtrl) && barCtrl is ScrollBar sb)
            {
                sb.Min = min; sb.Max = max; sb.Value = Math.Clamp(value, min, max);
                if (WindowManager.TryGetControl("winResourceEditor", labelName, out var lblCtrl) && lblCtrl is Label lbl)
                    lbl.Text = Math.Clamp(value, min, max).ToString();
            }
        }
        SetBar("sldLvlReq", "lblLvlReqVal", r.LvlRequired, 0, GameState.MaxLevel);
        SetBar("sldNormalPic", "lblNormalPicVal", r.ResourceImage, 0, GameState.NumResources);
        SetBar("sldExhaustedPic", "lblExhaustedPicVal", r.ExhaustedImage, 0, GameState.NumResources);
    }

    // Refresh list display names
    public static void RefreshList()
    {
        if (WindowManager.TryGetControl("winResourceEditor", "lstIndex", out var lstCtrl) && lstCtrl is ListBox lst)
        {
            int sel = SelectedIndex;
            lst.Items.Clear();
            for (int i = 0; i < Variables.MaxResources; i++)
            {
                var name = Data.Resource[i].Name ?? string.Empty;
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
        if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxResources) return;
        if (_history is null)
        {
            var s = Data.Resource[SelectedIndex];
            var n = s; // struct copy (clone arrays if any added later)
            _history = n;
            if (WindowManager.TryGetControl("winResourceEditor", "btnCopy", out var btn) && btn is Button b) b.Text = "Paste";
            return;
        }

        // Paste clipboard into current slot
        var pasted = _history.Value;
        Data.Resource[SelectedIndex] = pasted;
        GameState.ResourceChanged[SelectedIndex] = true;
        _history = null;
        if (WindowManager.TryGetControl("winResourceEditor", "btnCopy", out var btn2) && btn2 is Button b2) b2.Text = "Copy";
        OnLoad(SelectedIndex);
        RefreshList();
    }

    // Unified handlers for callbacks
    public static void OnSave()
    {
        Editors.ResourceEditorOK();
        WindowManager.HideWindow("winResourceEditor");
    }

    public static void OnCancel()
    {
        Editors.ResourceEditorCancel();
        WindowManager.HideWindow("winResourceEditor");
    }

    public static void OnDelete()
    {
        MapResource.OnClear(GameState.EditorIndex);
        GameState.ResourceChanged[GameState.EditorIndex] = true;
        OnLoad(GameState.EditorIndex);
        RefreshList();
    }

    public static void OnCopy()
    {
        OnCopyOrPaste();
    }

    // Draw resource images into preview boxes
    public static void OnDrawNormal()
    {
        if (!WindowManager.TryGetWindow("winResourceEditor", out var win) || win is null) return;
        if (!WindowManager.TryGetControl("winResourceEditor", "picNormal", out var ctrl) || ctrl is not PictureBox pic) return;
        int img = Math.Max(0, Data.Resource[SelectedIndex].ResourceImage);
        if (img <= 0 || img > GameState.NumResources) return;
        var path = Path.Combine(DataPath.Resources, img + GameState.GfxExt);
        if (!File.Exists(path)) return;
        GameClient.RenderTexture(ref path, win.X + pic.X, win.Y + pic.Y, 0, 0, pic.Width, pic.Height, pic.Width, pic.Height);
    }

    public static void OnDrawExhausted()
    {
        if (!WindowManager.TryGetWindow("winResourceEditor", out var win) || win is null) return;
        if (!WindowManager.TryGetControl("winResourceEditor", "picExhausted", out var ctrl) || ctrl is not PictureBox pic) return;
        int img = Math.Max(0, Data.Resource[SelectedIndex].ExhaustedImage);
        if (img <= 0 || img > GameState.NumResources) return;
        var path = Path.Combine(DataPath.Resources, img + GameState.GfxExt);
        if (!File.Exists(path)) return;
        GameClient.RenderTexture(ref path, win.X + pic.X, win.Y + pic.Y, 0, 0, pic.Width, pic.Height, pic.Width, pic.Height);
    }
}
