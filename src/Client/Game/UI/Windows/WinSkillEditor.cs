using Client.Game.UI;
using Client.Game.UI.Controls;
using Client.Net;
using Core.Globals;
using System;
using System.IO;

namespace Client.Game.UI.Windows;

public static class WinSkillEditor
{
    public static int SelectedIndex = 0;
    private static Core.Globals.Type.Skill? _history;

    public static void Init()
    {
        if (!WindowManager.TryGetControl("winSkillEditor", "lstIndex", out _))
            return;

        PopulateCombos();
        SelectedIndex = Math.Clamp(GameState.EditorIndex, 0, Variables.MaxSkills - 1);
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
        for (int i = 0; i < Variables.MaxSkills; i++)
        {
            string name = Strings.Trim(Data.Skill[i].Name);
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
            for (int i = 0; i < Variables.MaxAnimations; i++)
            {
                var raw = Data.Animation[i].Name ?? string.Empty;
                var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                cmbAnim.Items.Add($"{i + 1}: {name}");
            }
        }

        // Projectile list (0=None)
        if (WindowManager.TryGetControl("winSkillEditor", "cmbProjectile", out var projCtrl) && projCtrl is ComboBox cmbProj)
        {
            cmbProj.Items.Clear();
            cmbProj.Items.Add("None");
            for (int i = 0; i < Variables.MaxProjectiles; i++)
                cmbProj.Items.Add($"{i + 1}: {Data.Projectile[i].Name}");
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
    }

    public static void OnListMouseDown()
    {
        if (!WindowManager.TryGetControl("winSkillEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list) return;
        var win = WindowManager.GetWindowByName("winSkillEditor");
        if (win is null) return;
        int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
        int index = list.GetItemIndexAtPosition(relY);
        if (index < 0 || index >= Variables.MaxSkills) return;
        SelectedIndex = index;
        GameState.EditorIndex = index;
        list.SelectedIndex = index;
        list.EnsureVisible(index);
        OnLoad(index);
    }

    public static void OnLoad(int index)
    {
        if (index < 0 || index >= Variables.MaxSkills) return;
        SelectedIndex = index;
        GameState.EditorIndex = index;
        var s = Data.Skill[index];

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

        // Damage/Vital amount
        if (WindowManager.TryGetControl("winSkillEditor", "sldDamage", out var dmgCtrl) && dmgCtrl is ScrollBar sldDmg)
        {
            sldDmg.Min = 0; sldDmg.Max = 100000;
            sldDmg.Value = Math.Clamp(s.Vital, sldDmg.Min, sldDmg.Max);
        }

        // MP cost
        if (WindowManager.TryGetControl("winSkillEditor", "sldMpCost", out var mpCtrl) && mpCtrl is ScrollBar sldMp)
        {
            sldMp.Min = 0; sldMp.Max = 1024;
            sldMp.Value = Math.Clamp(s.MpCost, sldMp.Min, sldMp.Max);
        }

        // Cooldown
        if (WindowManager.TryGetControl("winSkillEditor", "sldCooldown", out var cdCtrl) && cdCtrl is ScrollBar sldCd)
        {
            sldCd.Min = 0; sldCd.Max = 60000;
            sldCd.Value = Math.Clamp(s.CdTime, sldCd.Min, sldCd.Max);
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

        // AoE
        if (WindowManager.TryGetControl("winSkillEditor", "sldAoE", out var aoeCtrl) && aoeCtrl is ScrollBar sldAoE)
        {
            sldAoE.Min = 0; sldAoE.Max = 12;
            sldAoE.Value = Math.Clamp(s.AoE, sldAoE.Min, sldAoE.Max);
        }

        // Type
        if (WindowManager.TryGetControl("winSkillEditor", "cmbType", out var typeCtrl) && typeCtrl is ComboBox cmbType)
            cmbType.Value = Math.Clamp(s.Type, 0, cmbType.Items.Count - 1);

        // Animation (bind to SkillAnim)
        if (WindowManager.TryGetControl("winSkillEditor", "cmbAnimation", out var animCtrl) && animCtrl is ComboBox cmbAnim)
            cmbAnim.Value = Math.Clamp(s.SkillAnim, 0, cmbAnim.Items.Count - 1);

        // Projectile (0=None => IsProjectile=0)
        if (WindowManager.TryGetControl("winSkillEditor", "cmbProjectile", out var projCtrl) && projCtrl is ComboBox cmbProj)
        {
            int val = s.Projectile < 0 ? 0 : s.Projectile + 1;
            cmbProj.Value = Math.Clamp(val, 0, cmbProj.Items.Count - 1);
        }

        // Preview draws
        if (WindowManager.TryGetControl("winSkillEditor", "picIcon", out var iconPicCtrl) && iconPicCtrl is PictureBox picIcon)
            picIcon.OnDraw = OnDrawIcon;
    }

    public static void OnDrawIcon()
    {
        var win = WindowManager.GetWindowByName("winSkillEditor");
        if (win is null) return;

        if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxSkills) return;
        var s = Data.Skill[SelectedIndex];

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

    public static void OnCopyOrPaste()
    {
        if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxSkills) return;

        if (_history is null)
        {
            _history = Data.Skill[SelectedIndex];
            if (WindowManager.TryGetControl("winSkillEditor", "btnCopy", out var btn) && btn is Button b)
                b.Text = "Paste";
            return;
        }

        Data.Skill[SelectedIndex] = _history.Value;
        GameState.SkillChanged[SelectedIndex] = true;
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
        GameState.SkillChanged[SelectedIndex] = true;
        OnLoad(SelectedIndex);
        RefreshList();
    }

    public static void OnCopy() => OnCopyOrPaste();
}