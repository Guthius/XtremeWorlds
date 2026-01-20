using Client.Game.UI;
using Client.Game.UI.Controls;
using Client.Net;
using Core.Globals;
using Core.Objects;
using System;
using System.IO;

namespace Client.Game.UI.Windows;

public class WinSkillEditor
{
    public static int SelectedIndex = 0;
    private static Skill? _history;

    public static void Init()
    {
        if (!WindowManager.TryGetControl("winSkillEditor", "lstIndex", out _))
            return;

        PopulateCombos();
        SelectedIndex = 0;
        RefreshList();
        OnLoad(SelectedIndex);
    }

    public static void RefreshList()
    {
        if (!WindowManager.TryGetControl("winSkillEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list)
            return;

        int prevIndex = SelectedIndex;
        int prevScroll = list.ScrollOffset;

        list.Clear();
        for (int i = 0; i < Core.Globals.Variables.MaxSkills; i++)
        {
            string name = Strings.Trim(Skill.Instance[i].Name);
            if (string.IsNullOrWhiteSpace(name)) name = "None";
            list.AddItem($"{i + 1}: {name}");
        }

        if (prevIndex >= 0 && prevIndex < list.Items.Count)
        {
            list.SelectedIndex = prevIndex;
            list.EnsureVisible(prevIndex);
        }

        if (WindowManager.TryGetControl("winSkillEditor", "sldList", out var sldCtrl) && sldCtrl is ScrollBar sb)
        {
            int visible = list.GetVisibleCount();
            int max = Math.Max(0, list.Items.Count - visible);
            sb.Min = 0;
            sb.Max = max;
            sb.Value = Math.Clamp(prevScroll, sb.Min, sb.Max);
        }
    }

    private static void PopulateCombos()
    {
        // Type (SkillEffect enum display)
        if (WindowManager.TryGetControl("winSkillEditor", "cmbType", out var typeCtrl) && typeCtrl is ComboBox cmbType)
        {
            cmbType.Items.Clear();
            foreach (var name in Enum.GetNames(typeof(SkillEffect)))
            {
                string display = System.Text.RegularExpressions.Regex.Replace(name, "(?<!^)([A-Z])", " $1");
                cmbType.Items.Add(display);
            }
        }

        // Skill animation (use Animation list)
        if (WindowManager.TryGetControl("winSkillEditor", "cmbAnimation", out var animCtrl) && animCtrl is ComboBox cmbAnim)
        {
            cmbAnim.Items.Clear();
            for (int i = 0; i < Core.Globals.Variables.MaxAnimations; i++)
            {
                var raw = Animation.Instance[i].Name ?? string.Empty;
                var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                cmbAnim.Items.Add($"{i + 1}: {name}");
            }
        }

        // Chain on hit skill (0=None, otherwise 1..MaxSkills)
        if (WindowManager.TryGetControl("winSkillEditor", "cmbChainOnHit", out var chainCtrl) && chainCtrl is ComboBox cmbChain)
        {
            cmbChain.Items.Clear();
            cmbChain.Items.Add("None");
            for (int i = 0; i < Core.Globals.Variables.MaxSkills; i++)
            {
                var raw = Skill.Instance[i].Name ?? string.Empty;
                var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                cmbChain.Items.Add($"{i + 1}: {name}");
            }
        }

        // Projectile list (0=None)
        if (WindowManager.TryGetControl("winSkillEditor", "cmbProjectile", out var projCtrl) && projCtrl is ComboBox cmbProj)
        {
            cmbProj.Items.Clear();
            cmbProj.Items.Add("None");
            for (int i = 0; i < Core.Globals.Variables.MaxProjectiles; i++)
                cmbProj.Items.Add($"{i + 1}: {Projectile.Instance[i].Name}");
        }

        // Optional sound list (not persisted by Type.Skill; populated for parity)
        if (WindowManager.TryGetControl("winSkillEditor", "cmbSound", out var soundCtrl) && soundCtrl is ComboBox cmbSound)
        {
            cmbSound.Items.Clear();
            cmbSound.Items.Add("None");
            General.CacheSound();
            foreach (var s in Audio.SoundCache)
            {
                if (!string.IsNullOrWhiteSpace(s)) cmbSound.Items.Add(s);
            }
        }

        // Custom Script / Common Event type (0=None, otherwise 1..CommonEventTrigger)
        if (WindowManager.TryGetControl("winSkillEditor", "cmbCommonEventType", out var ceCtrl) && ceCtrl is ComboBox cmbCe)
        {
            cmbCe.Items.Clear();
            cmbCe.Items.Add("None");
            foreach (var name in Enum.GetNames(typeof(CommonEventTrigger)))
            {
                string display = System.Text.RegularExpressions.Regex.Replace(name, "(?<!^)([A-Z])", " $1");
                cmbCe.Items.Add(display);
            }
        }
    }

    public static void OnListMouseDown()
    {
        if (!WindowManager.TryGetControl("winSkillEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list) return;
        var win = WindowManager.GetWindowByName("winSkillEditor");
        if (win is null) return;
        int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
        int index = list.GetItemIndexAtPosition(relY);
        if (index < 0 || index >= Core.Globals.Variables.MaxSkills) return;
        SelectedIndex = index;
        GameState.EditorIndex = index;
        list.SelectedIndex = index;
        list.EnsureVisible(index);
        OnLoad(index);
    }

    public static void OnLoad(int index)
    {
        if (index < 0 || index >= Core.Globals.Variables.MaxSkills) return;
        SelectedIndex = index;
        GameState.EditorIndex = index;
        var s = Skill.Instance[index];

        // Name
        if (WindowManager.TryGetControl("winSkillEditor", "txtName", out var nameCtrl) && nameCtrl is TextBox txtName)
            txtName.Text = s.Name ?? string.Empty;

        // Icon slider range/value
        if (WindowManager.TryGetControl("winSkillEditor", "sldIcon", out var iconCtrl) && iconCtrl is ScrollBar sldIcon)
        {
            sldIcon.Min = 0;
            sldIcon.Max = Math.Max(0, GameState.NumSkills);
            sldIcon.Value = Math.Clamp(s.Icon, sldIcon.Min, sldIcon.Max);
        }

        // MP cost
        if (WindowManager.TryGetControl("winSkillEditor", "sldMpCost", out var mpCtrl) && mpCtrl is ScrollBar sldMp)
        {
            sldMp.Min = 0; sldMp.Max = 1024;
            sldMp.Value = Math.Clamp(s.MpCost, sldMp.Min, sldMp.Max);
        }
        if (WindowManager.TryGetControl("winSkillEditor", "txtMpCost", out var mpTxtCtrl) && mpTxtCtrl is TextBox txtMp)
        {
            txtMp.Text = s.MpCost.ToString();
        }

        // SP Cost
        if (WindowManager.TryGetControl("winSkillEditor", "txtSpCost", out var spTxtCtrl) && spTxtCtrl is TextBox txtSp)
        {
            txtSp.Text = s.SpCost.ToString();
        }

        // Cooldown
        if (WindowManager.TryGetControl("winSkillEditor", "sldCooldown", out var cdCtrl) && cdCtrl is ScrollBar sldCd)
        {
            sldCd.Min = 0; sldCd.Max = 60000;
            sldCd.Value = Math.Clamp(s.CdTime, sldCd.Min, sldCd.Max);
        }
        if (WindowManager.TryGetControl("winSkillEditor", "txtCooldown", out var cdTxtCtrl) && cdTxtCtrl is TextBox txtCd)
        {
            txtCd.Text = s.CdTime.ToString();
        }

        // Range
        if (WindowManager.TryGetControl("winSkillEditor", "sldRange", out var rngCtrl) && rngCtrl is ScrollBar sldRange)
        {
            sldRange.Min = 0; sldRange.Max = 255;
            sldRange.Value = Math.Clamp(s.Range, sldRange.Min, sldRange.Max);
        }

        // Cast time
        if (WindowManager.TryGetControl("winSkillEditor", "sldCastTime", out var ctCtrl) && ctCtrl is ScrollBar sldCast)
        {
            sldCast.Min = 0; sldCast.Max = 10000;
            sldCast.Value = Math.Clamp(s.CastTime, sldCast.Min, sldCast.Max);
        }
        if (WindowManager.TryGetControl("winSkillEditor", "txtCastTime", out var ctTxtCtrl) && ctTxtCtrl is TextBox txtCast)
        {
            txtCast.Text = s.CastTime.ToString();
        }

        // Damage / Vital
        if (WindowManager.TryGetControl("winSkillEditor", "sldDamage", out var dmgCtrl) && dmgCtrl is ScrollBar sldDmg)
        {
            sldDmg.Min = 0;
            sldDmg.Max = 100000;
            sldDmg.Value = Math.Clamp(s.Vital, sldDmg.Min, sldDmg.Max);
        }
        if (WindowManager.TryGetControl("winSkillEditor", "txtDamage", out var dmgTxtCtrl) && dmgTxtCtrl is TextBox txtDmg)
        {
            txtDmg.Text = s.Vital.ToString();
        }

        // AoE
        if (WindowManager.TryGetControl("winSkillEditor", "sldAoE", out var aoeCtrl) && aoeCtrl is ScrollBar sldAoE)
        {
            sldAoE.Min = 0; sldAoE.Max = 32;
            sldAoE.Value = Math.Clamp(s.AoE, sldAoE.Min, sldAoE.Max);
        }

        // Type
        if (WindowManager.TryGetControl("winSkillEditor", "cmbType", out var typeCtrl) && typeCtrl is ComboBox cmbType)
            cmbType.Value = Math.Clamp(s.Type, 0, cmbType.Items.Count - 1);

        // Animation (bind to SkillAnim)
        if (WindowManager.TryGetControl("winSkillEditor", "cmbAnimation", out var animCtrl) && animCtrl is ComboBox cmbAnim)
            cmbAnim.Value = Math.Clamp(s.SkillAnim, 0, cmbAnim.Items.Count - 1);

        // Chain on hit (0=None => ChainOnHitSkillId=-1)
        if (WindowManager.TryGetControl("winSkillEditor", "cmbChainOnHit", out var chainCtrl) && chainCtrl is ComboBox cmbChain)
        {
            int val = s.ChainOnHitSkillId < 0 ? 0 : s.ChainOnHitSkillId + 1;
            cmbChain.Value = Math.Clamp(val, 0, cmbChain.Items.Count - 1);
        }

        // Multi-direction mask (8 directions; bit order matches server usage)
        static void SetDirCheck(string name, int mask, int bit)
        {
            if (WindowManager.TryGetControl("winSkillEditor", name, out var c) && c is CheckBox chk)
                chk.Value = (mask & (1 << bit)) != 0 ? 1 : 0;
        }

        int m = s.MultiDirMask;
        SetDirCheck("chkDirDown", m, 0);
        SetDirCheck("chkDirRight", m, 1);
        SetDirCheck("chkDirLeft", m, 2);
        SetDirCheck("chkDirUp", m, 3);
        SetDirCheck("chkDirDownRight", m, 4);
        SetDirCheck("chkDirDownLeft", m, 5);
        SetDirCheck("chkDirUpRight", m, 6);
        SetDirCheck("chkDirUpLeft", m, 7);

        // Projectile (0=None => IsProjectile=0)
        if (WindowManager.TryGetControl("winSkillEditor", "cmbProjectile", out var projCtrl) && projCtrl is ComboBox cmbProj)
        {
            int val = s.IsProjectile == 0 ? 0 : (s.Projectile < 0 ? 0 : s.Projectile + 1);
            cmbProj.Value = Math.Clamp(val, 0, cmbProj.Items.Count - 1);
        }

        // IsProjectile checkbox
        if (WindowManager.TryGetControl("winSkillEditor", "chkProjectile", out var chkCtrl) && chkCtrl is CheckBox chk)
            chk.Value = s.IsProjectile == 1 ? 1 : 0;

        // Custom Script / Common Event
        if (WindowManager.TryGetControl("winSkillEditor", "cmbCommonEventType", out var ceCtrl) && ceCtrl is ComboBox cmbCe)
            cmbCe.Value = Math.Clamp(s.CommonEventType, 0, cmbCe.Items.Count - 1);

        if (WindowManager.TryGetControl("winSkillEditor", "txtCommonEventData1", out var ce1Ctrl) && ce1Ctrl is TextBox tb1)
            tb1.Text = s.CommonEventData1.ToString();

        if (WindowManager.TryGetControl("winSkillEditor", "txtCommonEventData2", out var ce2Ctrl) && ce2Ctrl is TextBox tb2)
            tb2.Text = s.CommonEventData2.ToString();

        if (WindowManager.TryGetControl("winSkillEditor", "txtMoveSpeed", out var msmCtrl) && msmCtrl is TextBox tbMs)
            tbMs.Text = s.MoveSpeed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        if (WindowManager.TryGetControl("winSkillEditor", "chkMoveCast", out var mwcCtrl) && mwcCtrl is CheckBox chkMoveCast)
            chkMoveCast.Value = s.MoveCast ? 1 : 0;

        // Preview draws
        if (WindowManager.TryGetControl("winSkillEditor", "picIcon", out var iconPicCtrl) && iconPicCtrl is PictureBox picIcon)
            picIcon.OnDraw = OnDrawIcon;
    }

    public static void OnDrawIcon()
    {
        var win = WindowManager.GetWindowByName("winSkillEditor");
        if (win is null) return;

        if (SelectedIndex < 0 || SelectedIndex >= Core.Globals.Variables.MaxSkills) return;
        var s = Skill.Instance[SelectedIndex];

        if (s.Icon < 1 || s.Icon > GameState.NumSkills) return;

        if (!WindowManager.TryGetControl("winSkillEditor", "picIcon", out var iconCtrl) || iconCtrl is not PictureBox pic)
            return;

        string texturePath = Path.Combine(DataPath.Skills, s.Icon.ToString());
        var tex = GameClient.GetGfxInfo(texturePath);
        if (tex is null || tex.Width == 0 || tex.Height == 0) return;

        int iconSize = 32;
        int drawX = win.X + pic.X + (pic.Width - iconSize) / 2;
        int drawY = win.Y + pic.Y + (pic.Height - iconSize) / 2;

        GameClient.RenderTexture(ref texturePath, drawX, drawY, 0, 0, iconSize, iconSize, iconSize, iconSize);
    }

    public static void OnDrawPreview()
    {
        var win = WindowManager.GetWindowByName("winSkillEditor");
        if (win is null) return;

        if (SelectedIndex < 0 || SelectedIndex >= Core.Globals.Variables.MaxSkills) return;
        if (Skill.Instance.Count <= SelectedIndex) return;
        var s = Skill.Instance[SelectedIndex];

        if (!WindowManager.TryGetControl("winSkillEditor", "picIcon", out var previewCtrl) || previewCtrl is not PictureBox pic)
            return;

        if (s.Icon < 1 || s.Icon > GameState.NumSkills) return;

        string texturePath = Path.Combine(DataPath.Skills, s.Icon.ToString());
        var tex = GameClient.GetGfxInfo(texturePath);
        if (tex is null || tex.Width == 0 || tex.Height == 0) return;

        int srcSize = 32;
        int destSize = Math.Min(Math.Min(pic.Width, pic.Height), 128);
        if (destSize <= 0) return;

        int drawX = win.X + pic.X + (pic.Width - destSize) / 2;
        int drawY = win.Y + pic.Y + (pic.Height - destSize) / 2;

        GameClient.RenderTexture(ref texturePath, drawX, drawY, 0, 0, destSize, destSize, srcSize, srcSize);
    }

    public static void OnCopyOrPaste()
    {
        if (SelectedIndex < 0 || SelectedIndex >= Core.Globals.Variables.MaxSkills) return;

        if (_history is null)
        {
            _history = (Skill)Skill.Instance[SelectedIndex];
            if (WindowManager.TryGetControl("winSkillEditor", "btnCopy", out var btn) && btn is Button b)
                b.Text = "Paste";
            return;
        }

        Skill.Instance[SelectedIndex] = _history;
        Skill.IsChanged[SelectedIndex] = true;
        OnLoad(SelectedIndex);
        RefreshList();
    }

    public static void OnSave()
    {
        Editors.SkillEditorOK();
        WindowManager.HideWindow("winSkillEditor");
    }

    public static void OnCancel()
    {
        Editors.SkillEditorCancel();
        WindowManager.HideWindow("winSkillEditor");
    }

    public static void OnDelete()
    {
        Skill.OnClear(SelectedIndex);
        Skill.IsChanged[SelectedIndex] = true;
        OnLoad(SelectedIndex);
        RefreshList();
    }

    public static void OnCopy() => OnCopyOrPaste();

    public static void OnLearn()
    {
        int i = SelectedIndex;
        if (i < 0 || i >= Core.Globals.Variables.MaxSkills) return;
        Sender.LearnSkill(i);
    }
}