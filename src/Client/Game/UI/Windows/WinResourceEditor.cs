using Client;
using Client.Game.UI;
using Client.Game.UI.Controls;
using Core.Globals;
using Core.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Client.Game.UI.Windows;
public class WinResourceEditor
{
    public static int SelectedIndex = 0;
    public static bool IsLoading = false;
    public static Resource? _history = null;

    // Initialize window (called after layout is loaded)
    public static void Init()
    {
        if (!WindowManager.TryGetControl("winResourceEditor", "lstIndex", out _))
            return; // window not present yet
        SelectedIndex = 0;
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
        if (index < 0 || index >= Core.Globals.Variables.MaxResources) return;
        SelectedIndex = index;
        GameState.EditorIndex = index;
        list.SelectedIndex = index;
        list.EnsureVisible(index);
        OnLoad(index);
    }

    public static void OnLoad(int id)
    {
        if (id < 0 || id >= Core.Globals.Variables.MaxResources) return;
        SelectedIndex = id;
        GameState.EditorIndex = id;
        var r = Resource.Instance[id];

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
                for (int i = 0; i < Core.Globals.Variables.MaxItems; i++)
                {
                    var nm = Item.Instance[i].Name ?? string.Empty;
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
                for (int i = 0; i < Core.Globals.Variables.MaxAnimations; i++)
                {
                    var nm = Animation.Instance[i].Name ?? string.Empty;
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
        SetBar("sldLvlReq", "lblLvlReqVal", r.LvlRequired, 0, Core.Globals.Variables.MaxLevel);
        SetBar("sldNormalPic", "lblNormalPicVal", r.ResourceImage, 0, GameState.NumResources);
        SetBar("sldExhaustedPic", "lblExhaustedPicVal", r.ExhaustedImage, 0, GameState.NumResources);

        // Common event trigger (0=None; 1.. = CommonEventTrigger)
        if (WindowManager.TryGetControl("winResourceEditor", "cmbCommonEventType", out var ceCtrl) && ceCtrl is ComboBox cmbCe)
        {
            if (cmbCe.Items.Count == 0)
            {
                cmbCe.Items.Add("None");
                foreach (var name in Enum.GetNames(typeof(CommonEventTrigger)))
                    cmbCe.Items.Add(name);
            }

            cmbCe.Value = Math.Clamp(r.CommonEventType, 0, Math.Max(0, cmbCe.Items.Count - 1));
        }

        if (WindowManager.TryGetControl("winResourceEditor", "txtCommonEventData1", out var ce1Ctrl) && ce1Ctrl is TextBox txtCe1)
            txtCe1.Text = r.CommonEventData1.ToString();
        if (WindowManager.TryGetControl("winResourceEditor", "txtCommonEventData2", out var ce2Ctrl) && ce2Ctrl is TextBox txtCe2)
            txtCe2.Text = r.CommonEventData2.ToString();
    }

    // Refresh list display names
    public static void RefreshList()
    {
        if (WindowManager.TryGetControl("winResourceEditor", "lstIndex", out var lstCtrl) && lstCtrl is ListBox lst)
        {
            int sel = SelectedIndex;
            lst.Items.Clear();
            for (int i = 0; i < Core.Globals.Variables.MaxResources; i++)
            {
                var name = Resource.Instance[i].Name ?? string.Empty;
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
        if (SelectedIndex < 0 || SelectedIndex >= Core.Globals.Variables.MaxResources) return;
        if (_history is null)
        {
            var s = Resource.Instance[SelectedIndex];
            var n = s; // struct copy (clone arrays if any added later)
            _history = (Resource)n;
            if (WindowManager.TryGetControl("winResourceEditor", "btnCopy", out var btn) && btn is Button b) b.Text = "Paste";
            return;
        }

        // Paste clipboard into current slot
        var pasted = _history;
        Resource.Instance[SelectedIndex] = pasted;
        Resource.IsChanged[SelectedIndex] = true;
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
        Resource.IsChanged[GameState.EditorIndex] = true;
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
        int img = Math.Max(0, Resource.Instance[SelectedIndex].ResourceImage);
        if (img <= 0 || img > GameState.NumResources) return;
        var path = Path.Combine(DataPath.Resources, img + GameState.GfxExt);
        if (!File.Exists(path)) return;
        GameClient.RenderTexture(ref path, win.X + pic.X, win.Y + pic.Y, 0, 0, pic.Width, pic.Height, pic.Width, pic.Height);
    }

    public static void OnDrawExhausted()
    {
        if (!WindowManager.TryGetWindow("winResourceEditor", out var win) || win is null) return;
        if (!WindowManager.TryGetControl("winResourceEditor", "picExhausted", out var ctrl) || ctrl is not PictureBox pic) return;
        int img = Math.Max(0, Resource.Instance[SelectedIndex].ExhaustedImage);
        if (img <= 0 || img > GameState.NumResources) return;
        var path = Path.Combine(DataPath.Resources, img + GameState.GfxExt);
        if (!File.Exists(path)) return;
        GameClient.RenderTexture(ref path, win.X + pic.X, win.Y + pic.Y, 0, 0, pic.Width, pic.Height, pic.Width, pic.Height);
    }
}
