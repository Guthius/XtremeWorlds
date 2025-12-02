using Client.Game.UI;
using Client.Game.UI.Controls;
using Core.Globals;
using System;
using System.IO;

namespace Client.Game.UI.Windows;

public static class WinProjectileEditor
{
    public static int SelectedIndex = 0;
    private static Core.Globals.Type.Projectile? _history;

    public static void Init()
    {
        if (!WindowManager.TryGetControl("winProjectileEditor", "lstIndex", out _))
            return;

        SelectedIndex = Math.Clamp(SelectedIndex, 0, Variables.MaxProjectiles - 1);
        RefreshList();
        PopulateCombos();
        OnLoad(SelectedIndex);
    }

    public static void RefreshList()
    {
        if (!WindowManager.TryGetControl("winProjectileEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list)
            return;

        int prevIndex = SelectedIndex;
        int prevScroll = list.ScrollOffset;

        list.Clear();
        for (int i = 0; i < Variables.MaxProjectiles; i++)
        {
            string name = Strings.Trim(Data.Projectile[i].Name);
            if (string.IsNullOrWhiteSpace(name)) name = "None";
            list.AddItem($"{i + 1}: {name}");
        }

        if (prevIndex >= 0 && prevIndex < list.Items.Count)
        {
            list.SelectedIndex = prevIndex;
            list.EnsureVisible(prevIndex);
        }

        if (WindowManager.TryGetControl("winProjectileEditor", "sldList", out var sldCtrl) && sldCtrl is ScrollBar sb)
        {
            int visible = list.GetVisibleCount();
            int max = Math.Max(0, list.Items.Count - visible);
            sb.Min = 0; sb.Max = max;
            sb.Value = Math.Clamp(prevScroll, sb.Min, sb.Max);
        }
    }

    private static void PopulateCombos()
    {
        if (WindowManager.TryGetControl("winProjectileEditor", "cmbAnimation", out var animCtrl) && animCtrl is ComboBox cmbAnim)
        {
            if (cmbAnim.Items.Count == 0)
            {
                cmbAnim.Items.Add("None");
                for (int i = 0; i < Variables.MaxAnimations; i++)
                {
                    var raw = Data.Animation[i].Name ?? string.Empty;
                    var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                    cmbAnim.Items.Add($"{i + 1}: {name}");
                }
            }
        }
    }

    public static void OnListMouseDown()
    {
        if (!WindowManager.TryGetControl("winProjectileEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list) return;
        var win = WindowManager.GetWindowByName("winProjectileEditor");
        if (win is null) return;
        int relY = GameState.CurMouseY - (win.Y + list.Y);
        int index = list.GetItemIndexAtPosition(relY);
        if (index < 0 || index >= Variables.MaxProjectiles) return;
        SelectedIndex = index;
        GameState.EditorIndex = index;
        list.SelectedIndex = index;
        list.EnsureVisible(index);
        OnLoad(index);
    }

    public static void OnLoad(int index)
    {
        if (index < 0 || index >= Variables.MaxProjectiles) return;
        SelectedIndex = index;
        GameState.EditorIndex = index;
        ref var p = ref Data.Projectile[index];

        if (WindowManager.TryGetControl("winProjectileEditor", "txtName", out var nameCtrl) && nameCtrl is TextBox txtName)
            txtName.Text = p.Name ?? string.Empty;
        if (WindowManager.TryGetControl("winProjectileEditor", "sldSprite", out var picCtrl) && picCtrl is ScrollBar sldSprite)
        {
            sldSprite.Min = 0; sldSprite.Max = Math.Max(0, GameState.NumProjectiles);
            sldSprite.Value = Math.Clamp(p.Sprite, sldSprite.Min, sldSprite.Max);
        }
        if (WindowManager.TryGetControl("winProjectileEditor", "sldRange", out var rangeCtrl) && rangeCtrl is ScrollBar sldRange)
        {
            sldRange.Min = 0; sldRange.Max = 255; sldRange.Value = Math.Clamp(p.Range, sldRange.Min, sldRange.Max);
            if (WindowManager.TryGetControl("winProjectileEditor", "lblRangeVal", out var lblR) && lblR is Label lr) lr.Text = sldRange.Value.ToString();
        }
        if (WindowManager.TryGetControl("winProjectileEditor", "sldSpeed", out var speedCtrl) && speedCtrl is ScrollBar sldSpeed)
        {
            sldSpeed.Min = 0; sldSpeed.Max = 1000; sldSpeed.Value = Math.Clamp(p.Speed, sldSpeed.Min, sldSpeed.Max);
            if (WindowManager.TryGetControl("winProjectileEditor", "lblSpeedVal", out var lblS) && lblS is Label ls) ls.Text = sldSpeed.Value.ToString();
        }
        if (WindowManager.TryGetControl("winProjectileEditor", "sldDamage", out var dmgCtrl) && dmgCtrl is ScrollBar sldDamage)
        {
            sldDamage.Min = 0; sldDamage.Max = 100000; sldDamage.Value = Math.Clamp(p.Damage, sldDamage.Min, sldDamage.Max);
            if (WindowManager.TryGetControl("winProjectileEditor", "lblDamageVal", out var lblD) && lblD is Label ld) ld.Text = sldDamage.Value.ToString();
        }
        if (WindowManager.TryGetControl("winProjectileEditor", "cmbAnimation", out var animCtrl) && animCtrl is ComboBox cmbAnim)
        {
            int val = p.Animation < 0 ? 0 : p.Animation + 1; // 0 = None
            cmbAnim.Value = Math.Clamp(val, 0, cmbAnim.Items.Count - 1);
        }
        // Assign preview draw handler
        if (WindowManager.TryGetControl("winProjectileEditor", "picSprite", out var picCtrl2) && picCtrl2 is PictureBox pic)
            pic.OnDraw = OnDrawSprite;
    }

    public static void OnCopyOrPaste()
    {
        if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxProjectiles) return;
        if (_history is null)
        {
            _history = Data.Projectile[SelectedIndex];
            if (WindowManager.TryGetControl("winProjectileEditor", "btnCopy", out var btn) && btn is Button b) b.Text = "Paste";
            return;
        }
        Data.Projectile[SelectedIndex] = _history.Value;
        GameState.ProjectileChanged[SelectedIndex] = true;
        _history = null;
        if (WindowManager.TryGetControl("winProjectileEditor", "btnCopy", out var btn2) && btn2 is Button b2) b2.Text = "Copy";
        OnLoad(SelectedIndex);
        RefreshList();
    }

    public static void OnSave()
    {
        Editors.ProjectileEditorOK();
        WindowManager.HideWindow("winProjectileEditor");
    }

    public static void OnCancel()
    {
        Editors.ProjectileEditorCancel();
        WindowManager.HideWindow("winProjectileEditor");
    }

    public static void OnDelete()
    {
        Projectile.OnClear(GameState.EditorIndex);
        GameState.ProjectileChanged[SelectedIndex] = true;
        OnLoad(GameState.EditorIndex);
        RefreshList();
    }

    public static void OnCopy() => OnCopyOrPaste();

    public static void OnDrawSprite()
    {
        var win = WindowManager.GetWindowByName("winProjectileEditor");
        if (win is null) return;
        if (!WindowManager.TryGetControl("winProjectileEditor", "picSprite", out var ctrl) || ctrl is not PictureBox pic) return;
        if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxProjectiles) return;
        var pr = Data.Projectile[SelectedIndex];
        int spriteIndex = pr.Sprite;
        if (spriteIndex < 1 || spriteIndex > GameState.NumProjectiles) return;
        var path = Path.Combine(DataPath.Projectiles, spriteIndex + GameState.GfxExt);
        var tex = GameClient.GetGfxInfo(path);
        if (tex is null || tex.Width == 0 || tex.Height == 0) return;
        int fw = tex.Width / 4; // assume 4 frames horizontally
        int fh = tex.Height;
        int drawX = win.X + pic.X + (pic.Width - fw) / 2;
        int drawY = win.Y + pic.Y + (pic.Height - fh) / 2;
        GameClient.RenderTexture(ref path, drawX, drawY, 0, 0, fw, fh, fw, fh);
    }
}
