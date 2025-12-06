using Client.Game.UI;
using Client.Game.UI.Controls;
using Client.Net;
using Core.Globals;
using System;
using System.IO;

namespace Client.Game.UI.Windows;

public class WinItemEditor
{
    public static int SelectedIndex = 0;
    private static Item? _history;

    public static void Init()
    {
        if (!WindowManager.TryGetControl("winItemEditor", "lstIndex", out _))
            return;

        PopulateCombos();
        SelectedIndex = 0;
        RefreshList();
        OnLoad(SelectedIndex);

        // Ensure subtype list is built on init
        BuildSubtypeList();

        // Wire critical sliders to update item data immediately when moved
        if (WindowManager.TryGetControl("winItemEditor", "sldIcon", out var iconScrollCtrl) && iconScrollCtrl is ScrollBar sldIcon)
        {
            sldIcon.CallBack[(int)ControlState.MouseMove] = () =>
            {
                if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxItems) return;
                Item.Instance[SelectedIndex].Icon = (short)Math.Clamp(sldIcon.Value, sldIcon.Min, sldIcon.Max);
                Item.IsChanged[SelectedIndex] = true;
            };
        }
        if (WindowManager.TryGetControl("winItemEditor", "sldPaperdoll", out var pdScrollCtrl) && pdScrollCtrl is ScrollBar sldPd)
        {
            sldPd.CallBack[(int)ControlState.MouseMove] = () =>
            {
                if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxItems) return;
                Item.Instance[SelectedIndex].Paperdoll = (short)Math.Clamp(sldPd.Value, sldPd.Min, sldPd.Max);
                // keep textbox in sync if present
                if (WindowManager.TryGetControl("winItemEditor", "txtItemPaperdoll", out var pdCtrl) && pdCtrl is TextBox txtPd)
                    txtPd.Text = Item.Instance[SelectedIndex].Paperdoll.ToString();
                Item.IsChanged[SelectedIndex] = true;
            };
        }

        // Wire type ComboBox to toggle group visibility and dependent controls
        if (WindowManager.TryGetControl("winItemEditor", "cmbType", out var typeCtrl) && typeCtrl is ComboBox cmbType)
        {
            cmbType.CallBack[(int)ControlState.MouseMove] = () =>
            {
                if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxItems) return;
                Item.Instance[SelectedIndex].Type = (byte)Math.Clamp(cmbType.Value, 0, byte.MaxValue);
                BuildSubtypeList();
                ToggleTypeSections();
                Item.IsChanged[SelectedIndex] = true;
            };
            // Apply initial visibility state
            ToggleTypeSections();
        }

        // Wire Stackable checkbox toggle to update item
        if (WindowManager.TryGetControl("winItemEditor", "chkStackable", out var stackCtrl) && stackCtrl is CheckBox chkStack)
        {
            chkStack.CallBack[(int)ControlState.MouseDown] = () =>
            {
                if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxItems) return;
                // Toggle value locally then push to Data
                chkStack.Value = chkStack.Value == 0 ? 1 : 0;
                Item.Instance[SelectedIndex].Stackable = chkStack.Value != 0 ? (byte)1 : (byte)0;
                Item.IsChanged[SelectedIndex] = true;
            };
        }

        // Wire Knockback checkbox toggle to update item
        if (WindowManager.TryGetControl("winItemEditor", "chkKnockback", out var kbCtrl) && kbCtrl is CheckBox chkKb)
        {
            chkKb.CallBack[(int)ControlState.MouseDown] = () =>
            {
                if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxItems) return;
                chkKb.Value = chkKb.Value == 0 ? 1 : 0;
                Item.Instance[SelectedIndex].KnockBack = chkKb.Value != 0 ? (byte)1 : (byte)0;
                Item.IsChanged[SelectedIndex] = true;
            };
        }

        // Keep knockback tiles ComboBox writing back
        if (WindowManager.TryGetControl("winItemEditor", "cmbKnockBackTiles", out var kbtCtrl) && kbtCtrl is ComboBox cmbKbTiles)
        {
            cmbKbTiles.CallBack[(int)ControlState.MouseMove] = () =>
            {
                if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxItems) return;
                Item.Instance[SelectedIndex].KnockBackTiles = (byte)Math.Clamp(cmbKbTiles.Value, 0, byte.MaxValue);
                Item.IsChanged[SelectedIndex] = true;
            };
        }
    }

    public static void RefreshList()
    {
        if (!WindowManager.TryGetControl("winItemEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list)
            return;

        int prevIndex = SelectedIndex;
        int prevScroll = list.ScrollOffset;

        list.Clear();
        for (int i = 0; i < Item.Instance.Count; i++)
        {
            string name = Strings.Trim(Item.Instance[i].Name);
            if (string.IsNullOrWhiteSpace(name)) name = "None";
            list.AddItem($"{i + 1}: {name}");
        }

        if (prevIndex >= 0 && prevIndex < list.Items.Count)
        {
            list.SelectedIndex = prevIndex;
            list.EnsureVisible(prevIndex);
        }

        if (WindowManager.TryGetControl("winItemEditor", "sldList", out var sldCtrl) && sldCtrl is ScrollBar sb)
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
        // Type
        if (WindowManager.TryGetControl("winItemEditor", "cmbType", out var typeCtrl) && typeCtrl is ComboBox cmbType)
        {
            cmbType.Items.Clear();
            foreach (var name in Enum.GetNames(typeof(ItemCategory)))
                cmbType.Items.Add(name);
        }

        // Item level
        if (WindowManager.TryGetControl("winItemEditor", "cmbLevel", out var lvlComboCtrl) && lvlComboCtrl is ComboBox cmbLevel)
        {
            cmbLevel.Items.Clear();
            for (int i = 1; i <= GameState.MaxLevel; i++)
                cmbLevel.Items.Add(i.ToString());
        }

        // Bind
        if (WindowManager.TryGetControl("winItemEditor", "cmbBind", out var bindCtrl) && bindCtrl is ComboBox cmbBind)
        {
            cmbBind.Items.Clear();
            cmbBind.Items.Add("None");
            cmbBind.Items.Add("Bind On Equip");
            cmbBind.Items.Add("Bind On Pickup");
        }

        // Animation
        if (WindowManager.TryGetControl("winItemEditor", "cmbAnimation", out var animCtrl) && animCtrl is ComboBox cmbAnim)
        {
            cmbAnim.Items.Clear();
            for (int i = 0; i < Animation.Instance.Count; i++)
            {
                var raw = Animation.Instance[i].Name ?? string.Empty;
                var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                cmbAnim.Items.Add($"{i + 1}: {name}");
            }
        }

        // Rarity (1-6)
        if (WindowManager.TryGetControl("winItemEditor", "cmbRarity", out var rarComboCtrl) && rarComboCtrl is ComboBox cmbRarity)
        {
            cmbRarity.Items.Clear();
            for (int i = 1; i <= 6; i++)
                cmbRarity.Items.Add(i.ToString());
        }

        // Job requirements
        if (WindowManager.TryGetControl("winItemEditor", "cmbJobReq", out var jobCtrl) && jobCtrl is ComboBox cmbJob)
        {
            cmbJob.Items.Clear();
            for (int i = 0; i < Variables.MaxJobs; i++)
                cmbJob.Items.Add(Data.Job[i].Name);
        }

        // Access requirements (use AccessLevel enum with spaced names)
        if (WindowManager.TryGetControl("winItemEditor", "cmbAccessReq", out var accCtrl) && accCtrl is ComboBox cmbAcc)
        {
            cmbAcc.Items.Clear();
            foreach (var name in Enum.GetNames(typeof(AccessLevel)))
            {
                string display = System.Text.RegularExpressions.Regex.Replace(name, "(?<!^)([A-Z])", " $1");
                cmbAcc.Items.Add(display);
            }
        }

        // Tool list (resource names)
        if (WindowManager.TryGetControl("winItemEditor", "cmbTool", out var toolCtrl) && toolCtrl is ComboBox cmbTool)
        {
            cmbTool.Items.Clear();
            cmbTool.Items.Add("None");
            for (int i = 0; i < Variables.MaxResources; i++)
                cmbTool.Items.Add(Data.Resource[i].Name);
        }

        // Knockback tiles choices
        if (WindowManager.TryGetControl("winItemEditor", "cmbKnockBackTiles", out var kbCtrl) && kbCtrl is ComboBox cmbKb)
        {
            cmbKb.Items.Clear();
            for (int i = 0; i < 6; i++)
                cmbKb.Items.Add(i.ToString());
        }

        // Skill list
        if (WindowManager.TryGetControl("winItemEditor", "cmbSkill", out var skillCtrl) && skillCtrl is ComboBox cmbSkill)
        {
            cmbSkill.Items.Clear();
            cmbSkill.Items.Add("None");
            for (int i = 0; i < Variables.MaxSkills; i++)
                cmbSkill.Items.Add($"{i + 1}: {Data.Skill[i].Name}");
        }

        // Projectile list
        if (WindowManager.TryGetControl("winItemEditor", "cmbProjectile", out var projCtrl) && projCtrl is ComboBox cmbProj)
        {
            cmbProj.Items.Clear();
            cmbProj.Items.Add("None");
            for (int i = 0; i < Variables.MaxProjectiles; i++)
                cmbProj.Items.Add($"{i + 1}: {Data.Projectile[i].Name}");
        }

        // Ammo list (0 = None, then items)
        if (WindowManager.TryGetControl("winItemEditor", "cmbAmmo", out var ammoCtrl) && ammoCtrl is ComboBox cmbAmmo)
        {
            cmbAmmo.Items.Clear();
            cmbAmmo.Items.Add("None");
            for (int i = 0; i < Item.Instance.Count; i++)
            {
                var n = Item.Instance[i].Name ?? string.Empty;
                if (string.IsNullOrWhiteSpace(n)) n = "None";
                cmbAmmo.Items.Add($"{i + 1}: {n.Trim()}");
            }
        }
    }

    public static void BuildSubtypeList()
    {
        if (!WindowManager.TryGetControl("winItemEditor", "cmbSubType", out var subCtrl) || subCtrl is not ComboBox cmbSub)
            return;
        cmbSub.Items.Clear();
        var type = (ItemCategory)Item.Instance[SelectedIndex].Type;
        switch (type)
        {
            case ItemCategory.Equipment:
                cmbSub.Items.Add("Weapon");
                cmbSub.Items.Add("Armor");
                cmbSub.Items.Add("Helmet");
                cmbSub.Items.Add("Shield");
                break;
            case ItemCategory.Consumable:
                cmbSub.Items.Add("HP");
                cmbSub.Items.Add("MP");
                cmbSub.Items.Add("SP");
                cmbSub.Items.Add("Exp");
                break;
            case ItemCategory.Event:
                cmbSub.Items.Add("Switches");
                cmbSub.Items.Add("Variables");
                cmbSub.Items.Add("Key");
                cmbSub.Items.Add("Custom Script");
                break;
            default:
                // no subtype for other categories
                break;
        }
        if (cmbSub.Items.Count > 0)
        {
            var sub = 0;
            cmbSub.Value = sub;
        }
    }

    private static void ToggleTypeSections()
    {
        var type = (ItemCategory)Item.Instance[SelectedIndex].Type;

        static void SetVisible(string name, bool vis)
        {
            if (WindowManager.TryGetControl("winItemEditor", name, out var c) && c is not null)
                c.Visible = vis;
        }
        if (WindowManager.TryGetControl("winItemEditor", "grpEquip", out var eqCtrl) && eqCtrl is GroupBox grpEquip && WindowManager.TryGetWindow("winItemEditor", out var win))
        {
            bool eq = type == ItemCategory.Equipment;
            WindowManager.SetGroupVisible(win!, grpEquip, eq);
            // Within equipment group, toggle individual equipment-only controls as extra safety
            SetVisible("lblDamage", eq);
            SetVisible("txtDamage", eq);
            SetVisible("lblSpeed", eq);
            SetVisible("txtSpeed", eq);
            SetVisible("chkKnockback", eq);
            SetVisible("lblTool", eq);
            SetVisible("cmbTool", eq);
            SetVisible("lblKnockbackTiles", eq);
            SetVisible("cmbKnockBackTiles", eq);
            SetVisible("lblPaperdollEquip", eq);
            SetVisible("sldPaperdoll", eq);
            SetVisible("picPaperdoll", eq);
        }

        if (WindowManager.TryGetControl("winItemEditor", "grpItemUse", out var useCtrl) && useCtrl is GroupBox grpUse && WindowManager.TryGetWindow("winItemEditor", out var win2))
        {
            WindowManager.SetGroupVisible(win2!, grpUse, type is ItemCategory.Consumable or ItemCategory.Skill or ItemCategory.Projectile or ItemCategory.Event);
        }

        bool isConsumable = type == ItemCategory.Consumable;
        bool isSkill = type == ItemCategory.Skill;
        bool isProjectile = type == ItemCategory.Projectile;
        bool isEvent = type == ItemCategory.Event;

        // Consumable controls
        SetVisible("lblVitalMod", isConsumable);
        SetVisible("txtVitalMod", isConsumable);

        // Skill controls
        SetVisible("lblSkillHeader", isSkill);
        SetVisible("cmbSkill", isSkill);

        // Projectile controls
        SetVisible("lblProjectileHeader", isProjectile);
        SetVisible("cmbProjectile", isProjectile);
        SetVisible("lblAmmo", isProjectile);
        SetVisible("cmbAmmo", isProjectile);

        // Event controls
        SetVisible("lblEventHeader", isEvent);
        SetVisible("lblEventId", isEvent);
        SetVisible("txtEventId", isEvent);
        SetVisible("lblEventValue", isEvent);
        SetVisible("txtEventValue", isEvent);
    }

    public static void OnLoad(int index)
    {
        if (index < 0 || index >= Item.Instance.Count) return;
        SelectedIndex = index;
        GameState.EditorIndex = index;
        var item = Item.Instance[index];

        // Basics
        if (WindowManager.TryGetControl("winItemEditor", "txtName", out var nameCtrl) && nameCtrl is TextBox txtName)
            txtName.Text = item.Name ?? string.Empty;
        if (WindowManager.TryGetControl("winItemEditor", "txtDesc", out var descCtrl) && descCtrl is TextBox txtDesc)
            txtDesc.Text = item.Description ?? string.Empty;
        
        // icon index is driven by scrollbar; no direct text box anymore
        if (WindowManager.TryGetControl("winItemEditor", "sldIcon", out var iconScrollCtrl) && iconScrollCtrl is ScrollBar sldIcon)
        {
            // Max icons is NumItems icons (mirror how item icons are stored: per item index)
            sldIcon.Min = 0;
            sldIcon.Max = Math.Max(0, GameState.NumItems);
            sldIcon.Value = Math.Clamp(item.Icon, sldIcon.Min, sldIcon.Max);
        }
        if (WindowManager.TryGetControl("winItemEditor", "txtItemPaperdoll", out var pdCtrl) && pdCtrl is TextBox txtPd)
            txtPd.Text = item.Paperdoll.ToString();

        // Paperdoll scrollbar range and value
        if (WindowManager.TryGetControl("winItemEditor", "sldPaperdoll", out var pdScrollCtrl) && pdScrollCtrl is ScrollBar sldPd)
        {
            sldPd.Min = 0;
            sldPd.Max = Math.Max(0, GameState.NumPaperdolls);
            sldPd.Value = Math.Clamp(item.Paperdoll, sldPd.Min, sldPd.Max);
        }
        // Hook paperdoll draw if not already (safe to set repeatedly)
        if (WindowManager.TryGetControl("winItemEditor", "picPaperdoll", out var pdPicCtrl) && pdPicCtrl is PictureBox pdPic)
            pdPic.OnDraw = OnDrawPaperdoll;

        if (WindowManager.TryGetControl("winItemEditor", "cmbType", out var typeCtrl) && typeCtrl is ComboBox cmbType)
            cmbType.Value = Math.Clamp(item.Type, 0, cmbType.Items.Count - 1);
        if (WindowManager.TryGetControl("winItemEditor", "cmbSubType", out var subCtrl) && subCtrl is ComboBox cmbSub)
            cmbSub.Value = item.SubType;
        if (WindowManager.TryGetControl("winItemEditor", "cmbAnimation", out var animCtrl) && animCtrl is ComboBox cmbAnim)
            cmbAnim.Value = Math.Clamp(item.Animation, 0, cmbAnim.Items.Count - 1);
        if (WindowManager.TryGetControl("winItemEditor", "cmbBind", out var bindCtrl) && bindCtrl is ComboBox cmbBind)
            cmbBind.Value = Math.Clamp(item.BindType, 0, cmbBind.Items.Count - 1);

        if (WindowManager.TryGetControl("winItemEditor", "cmbLevel", out var lvlCtrl) && lvlCtrl is ComboBox cmbLevel)
            cmbLevel.Value = Math.Clamp(item.ItemLevel - 1, 0, cmbLevel.Items.Count - 1);
        if (WindowManager.TryGetControl("winItemEditor", "txtPrice", out var priceCtrl) && priceCtrl is TextBox txtPrice)
            txtPrice.Text = item.Price.ToString();
        if (WindowManager.TryGetControl("winItemEditor", "cmbRarity", out var rarCtrl) && rarCtrl is ComboBox cmbRarity)
            cmbRarity.Value = Math.Clamp(item.Rarity - 1, 0, cmbRarity.Items.Count - 1);
        if (WindowManager.TryGetControl("winItemEditor", "chkStackable", out var stackCtrl) && stackCtrl is CheckBox chkStack)
            chkStack.Value = item.Stackable != 0 ? 1 : 0;

        // Equipment & stats
        if (WindowManager.TryGetControl("winItemEditor", "txtDamage", out var dmgText) && dmgText is TextBox txtDmg)
        {
            txtDmg.Text = item.Data2.ToString();
        }
        if (WindowManager.TryGetControl("winItemEditor", "txtSpeed", out var spdText) && spdText is TextBox txtSpd)
        {
            txtSpd.Text = item.Speed.ToString();
        }
        if (WindowManager.TryGetControl("winItemEditor", "chkKnockback", out var kbCtrl2) && kbCtrl2 is CheckBox chkKb)
            chkKb.Value = item.KnockBack != 0 ? 1 : 0;
        if (WindowManager.TryGetControl("winItemEditor", "cmbTool", out var toolCtrl) && toolCtrl is ComboBox cmbTool)
            cmbTool.Value = Math.Clamp(item.Data3, 0, cmbTool.Items.Count - 1);
        if (WindowManager.TryGetControl("winItemEditor", "cmbKnockBackTiles", out var kbtCtrl) && kbtCtrl is ComboBox cmbKbTiles)
            cmbKbTiles.Value = Math.Clamp(item.KnockBackTiles, 0, cmbKbTiles.Items.Count - 1);

        if (WindowManager.TryGetControl("winItemEditor", "sldStr", out var aStrCtrl) && aStrCtrl is ScrollBar sldAStr)
        {
            sldAStr.Min = -Variables.MaxStats;
            sldAStr.Max = Variables.MaxStats;
            sldAStr.Value = Math.Clamp(item.AddStat[(int)Stat.Strength], sldAStr.Min, sldAStr.Max);
        }
        if (WindowManager.TryGetControl("winItemEditor", "sldVit", out var aVitCtrl) && aVitCtrl is ScrollBar sldAVit)
        {
            sldAVit.Min = -Variables.MaxStats;
            sldAVit.Max = Variables.MaxStats;
            sldAVit.Value = Math.Clamp(item.AddStat[(int)Stat.Vitality], sldAVit.Min, sldAVit.Max);
        }
        if (WindowManager.TryGetControl("winItemEditor", "sldLuck", out var aLuckCtrl) && aLuckCtrl is ScrollBar sldALuck)
        {
            sldALuck.Min = -Variables.MaxStats;
            sldALuck.Max = Variables.MaxStats;
            sldALuck.Value = Math.Clamp(item.AddStat[(int)Stat.Luck], sldALuck.Min, sldALuck.Max);
        }
        if (WindowManager.TryGetControl("winItemEditor", "sldInt", out var aIntCtrl) && aIntCtrl is ScrollBar sldAInt)
        {
            sldAInt.Min = -Variables.MaxStats;
            sldAInt.Max = Variables.MaxStats;
            sldAInt.Value = Math.Clamp(item.AddStat[(int)Stat.Intelligence], sldAInt.Min, sldAInt.Max);
        }
        if (WindowManager.TryGetControl("winItemEditor", "sldSpr", out var aSprCtrl) && aSprCtrl is ScrollBar sldASpr)
        {
            sldASpr.Min = -Variables.MaxStats;
            sldASpr.Max = Variables.MaxStats;
            sldASpr.Value = Math.Clamp(item.AddStat[(int)Stat.Spirit], sldASpr.Min, sldASpr.Max);
        }

        // Consumable / skill / projectile / event
        if (WindowManager.TryGetControl("winItemEditor", "txtVitalMod", out var vCtrl) && vCtrl is TextBox txtVital)
            txtVital.Text = item.Data1.ToString();
        if (WindowManager.TryGetControl("winItemEditor", "cmbSkill", out var sCtrl) && sCtrl is ComboBox cmbSkill)
            cmbSkill.Value = Math.Clamp(item.Data1, 0, cmbSkill.Items.Count - 1);
        if (WindowManager.TryGetControl("winItemEditor", "cmbProjectile", out var pCtrl) && pCtrl is ComboBox cmbProj)
            cmbProj.Value = Math.Clamp(item.Projectile + 1, 0, cmbProj.Items.Count - 1);
        if (WindowManager.TryGetControl("winItemEditor", "cmbAmmo", out var ammoCtrl) && ammoCtrl is ComboBox cmbAmmo)
            cmbAmmo.Value = Math.Clamp(item.Ammo + 1, 0, cmbAmmo.Items.Count - 1);
        if (WindowManager.TryGetControl("winItemEditor", "txtEventId", out var eIdCtrl) && eIdCtrl is TextBox txtEId)
            txtEId.Text = item.Data1.ToString();
        if (WindowManager.TryGetControl("winItemEditor", "txtEventValue", out var eValCtrl) && eValCtrl is TextBox txtEVal)
            txtEVal.Text = item.Data2.ToString();

        // Requirements
        if (WindowManager.TryGetControl("winItemEditor", "sldReqLevel", out var rLvlCtrl) && rLvlCtrl is ScrollBar sldReqLevel)
        {
            sldReqLevel.Min = 1;
            sldReqLevel.Max = GameState.MaxLevel;
            sldReqLevel.Value = Math.Clamp(item.LevelReq, sldReqLevel.Min, sldReqLevel.Max);
        }
        if (WindowManager.TryGetControl("winItemEditor", "sldReqStr", out var rStrCtrl) && rStrCtrl is ScrollBar sldReqStr)
        {
            sldReqStr.Min = 0;
            sldReqStr.Max = Variables.MaxStats;
            sldReqStr.Value = Math.Clamp(item.StatReq[(int)Stat.Strength], sldReqStr.Min, sldReqStr.Max);
        }
        if (WindowManager.TryGetControl("winItemEditor", "sldReqVit", out var rVitCtrl) && rVitCtrl is ScrollBar sldReqVit)
        {
            sldReqVit.Min = 0;
            sldReqVit.Max = Variables.MaxStats;
            sldReqVit.Value = Math.Clamp(item.StatReq[(int)Stat.Vitality], sldReqVit.Min, sldReqVit.Max);
        }
        if (WindowManager.TryGetControl("winItemEditor", "sldReqLuck", out var rLuckCtrl) && rLuckCtrl is ScrollBar sldReqLuck)
        {
            sldReqLuck.Min = 0;
            sldReqLuck.Max = Variables.MaxStats;
            sldReqLuck.Value = Math.Clamp(item.StatReq[(int)Stat.Luck], sldReqLuck.Min, sldReqLuck.Max);
        }
        if (WindowManager.TryGetControl("winItemEditor", "sldReqInt", out var rIntCtrl) && rIntCtrl is ScrollBar sldReqInt)
        {
            sldReqInt.Min = 0;
            sldReqInt.Max = Variables.MaxStats;
            sldReqInt.Value = Math.Clamp(item.StatReq[(int)Stat.Intelligence], sldReqInt.Min, sldReqInt.Max);
        }
        if (WindowManager.TryGetControl("winItemEditor", "sldReqSpr", out var rSprCtrl) && rSprCtrl is ScrollBar sldReqSpr)
        {
            sldReqSpr.Min = 0;
            sldReqSpr.Max = Variables.MaxStats;
            sldReqSpr.Value = Math.Clamp(item.StatReq[(int)Stat.Spirit], sldReqSpr.Min, sldReqSpr.Max);
        }

        if (WindowManager.TryGetControl("winItemEditor", "cmbJobReq", out var jCtrl2) && jCtrl2 is ComboBox cmbJob2)
            cmbJob2.Value = Math.Clamp(item.JobReq, 0, cmbJob2.Items.Count - 1);
        if (WindowManager.TryGetControl("winItemEditor", "cmbAccessReq", out var aCtrl2) && aCtrl2 is ComboBox cmbAcc2)
            cmbAcc2.Value = Math.Clamp(item.AccessReq, 0, cmbAcc2.Items.Count - 1);

        // Rebuild subtype list now that type is applied
        BuildSubtypeList();
        // Apply visibility after loading
        ToggleTypeSections();
    }

    public static void OnDrawIcon()
    {
        var win = WindowManager.GetWindowByName("winItemEditor");
        if (win is null) return;

        if (SelectedIndex < 0 || SelectedIndex >= Item.Instance.Count) return;
        var item = Item.Instance[SelectedIndex];

        if (item.Icon < 1 || item.Icon > GameState.NumItems) return;

        if (!WindowManager.TryGetControl("winItemEditor", "picIcon", out var iconCtrl) || iconCtrl is not PictureBox pic)
            return;

        string texturePath = Path.Combine(DataPath.Items, item.Icon.ToString());
        var tex = GameClient.GetGfxInfo(texturePath);
        if (tex is null || tex.Width == 0 || tex.Height == 0) return;

        int iconSize = 32;
        int drawX = win.X + pic.X + (pic.Width - iconSize) / 2;
        int drawY = win.Y + pic.Y + (pic.Height - iconSize) / 2;

        GameClient.RenderTexture(ref texturePath, drawX, drawY, 0, 0, iconSize, iconSize, iconSize, iconSize);
    }

    public static void OnDrawPaperdoll()
    {
        var win = WindowManager.GetWindowByName("winItemEditor");
        if (win is null) return;
        if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxItems) return;
        var item = Item.Instance[SelectedIndex];
        if (item.Paperdoll < 1 || item.Paperdoll > GameState.NumPaperdolls) return;
        if (!WindowManager.TryGetControl("winItemEditor", "picPaperdoll", out var ctrl) || ctrl is not PictureBox pic)
            return;
        string texturePath = Path.Combine(DataPath.Paperdolls, item.Paperdoll.ToString());
        var tex = GameClient.GetGfxInfo(texturePath);
        if (tex is null || tex.Width == 0 || tex.Height == 0) return;
        // Draw top-left frame cropped into 64x64 dest
        int srcW = Math.Min(64, tex.Width);
        int srcH = Math.Min(64, tex.Height);
        int destW = pic.Width;
        int destH = pic.Height;
        int drawX = win.X + pic.X + (pic.Width - destW) / 2;
        int drawY = win.Y + pic.Y + (pic.Height - destH) / 2;
        // Correct parameter order: destination size first, then source size
        GameClient.RenderTexture(ref texturePath, drawX, drawY, 0, 0, destW, destH, srcW, srcH);
    }

    public static void OnListMouseDown()
    {
        if (!WindowManager.TryGetControl("winItemEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list) return;
        var win = WindowManager.GetWindowByName("winItemEditor");
        if (win is null) return;
        int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
        int index = list.GetItemIndexAtPosition(relY);
        if (index < 0 || index >= Variables.MaxItems) return;
        SelectedIndex = index;
        GameState.EditorIndex = index;
        list.SelectedIndex = index;
        list.EnsureVisible(index);
        OnLoad(index);
    }

    public static void OnCopyOrPaste()
    {
        if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxItems) return;

        if (_history is null)
        {
            _history = (Item?)Item.Instance[SelectedIndex];
            if (WindowManager.TryGetControl("winItemEditor", "btnCopy", out var btn) && btn is Button b)
                b.Text = "Paste";
            return;
        }

        Item.Instance[SelectedIndex] = _history;
        Item.IsChanged[SelectedIndex] = true;
        OnLoad(SelectedIndex);
        RefreshList();
    }

    // Add unified callback handlers for wiring from Crystalshire
    public static void OnSave()
    {
        Editors.ItemEditorOK();
        WindowManager.HideWindow("winItemEditor");
    }

    public static void OnCancel()
    {
        Editors.ItemEditorCancel();
        WindowManager.HideWindow("winItemEditor");
    }

    public static void OnDelete()
    {
        Item.OnClear(GameState.EditorIndex);
        if (SelectedIndex >= 0 && SelectedIndex < Item.IsChanged.Length)
            Item.IsChanged[SelectedIndex] = true;
        OnLoad(GameState.EditorIndex);
        RefreshList();
    }

    public static void OnCopy()
    {
        OnCopyOrPaste();
    }

    public static void OnSpawn()
    {
        if (GameState.MyIndex > 0)
        {
            Sender.SendSpawnItem(GameState.EditorIndex, 1);
        }
    }

    public static void OnClose()
    {
        Editors.ItemEditorCancel();
        WindowManager.HideWindow("winItemEditor");
    }
}

