using Client.Game.UI;
using Client.Game.UI.Controls;
using Core.Globals;
using System;
using System.IO;

namespace Client.Game.UI.Windows;

public static class WinItemEditor
{
	public static int SelectedIndex = 0;
	private static Core.Globals.Type.Item? _clipboardItem;

	public static void Init()
	{
		if (!WindowManager.TryGetControl("winItemEditor", "lstItemIndex", out _))
			return;

		PopulateStaticCombos();
		SelectedIndex = Math.Clamp(GameState.EditorIndex, 0, Variables.MaxItems - 1);
		RefreshList();
		LoadItem(SelectedIndex);
		// Ensure subtype list is built on init
		BuildSubtypeList();
	}

	private static void RefreshList()
	{
		if (!WindowManager.TryGetControl("winItemEditor", "lstItemIndex", out var ctrl) || ctrl is not ListBox list)
			return;

		int prevIndex = SelectedIndex;
		int prevScroll = list.ScrollOffset;

		list.Clear();
		for (int i = 0; i < Variables.MaxItems; i++)
		{
			string name = Strings.Trim(Data.Item[i].Name);
			if (string.IsNullOrWhiteSpace(name)) name = "None";
			list.AddItem($"{i + 1}: {name}");
		}

		if (prevIndex >= 0 && prevIndex < list.Items.Count)
		{
			list.SelectedIndex = prevIndex;
			list.EnsureVisible(prevIndex);
		}

		if (WindowManager.TryGetControl("winItemEditor", "sldItemList", out var sldCtrl) && sldCtrl is ScrollBar sb)
		{
			int visible = list.GetVisibleCount();
			int max = Math.Max(0, list.Items.Count - visible);
			sb.Min = 0;
			sb.Max = max;
			sb.Value = Math.Clamp(prevScroll, sb.Min, sb.Max);
		}
	}

	private static void PopulateStaticCombos()
	{
		// Type
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemType", out var typeCtrl) && typeCtrl is ComboBox cmbType)
		{
			cmbType.Items.Clear();
			foreach (var name in Enum.GetNames(typeof(ItemCategory)))
				cmbType.Items.Add(name);
		}

		// Item level
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemLevel", out var lvlComboCtrl) && lvlComboCtrl is ComboBox cmbItemLevel)
		{
			cmbItemLevel.Items.Clear();
			for (int i = 1; i <= GameState.MaxLevel; i++)
				cmbItemLevel.Items.Add(i.ToString());
		}

		// Bind
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemBind", out var bindCtrl) && bindCtrl is ComboBox cmbBind)
		{
			cmbBind.Items.Clear();
			cmbBind.Items.Add("None");
			cmbBind.Items.Add("Bind On Equip");
			cmbBind.Items.Add("Bind On Pickup");
		}

		// Animation
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemAnimation", out var animCtrl) && animCtrl is ComboBox cmbAnim)
		{
			cmbAnim.Items.Clear();
			for (int i = 0; i < Variables.MaxAnimations; i++)
			{
				var raw = Data.Animation[i].Name ?? string.Empty;
				var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
				cmbAnim.Items.Add($"{i + 1}: {name}");
			}
		}

		// Rarity (1-6)
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemRarity", out var rarComboCtrl) && rarComboCtrl is ComboBox cmbRarity)
		{
			cmbRarity.Items.Clear();
			for (int i = 1; i <= 6; i++)
				cmbRarity.Items.Add(i.ToString());
		}

		// Job requirements
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemJobReq", out var jobCtrl) && jobCtrl is ComboBox cmbJob)
		{
			cmbJob.Items.Clear();
			for (int i = 0; i < Variables.MaxJobs; i++)
				cmbJob.Items.Add(Data.Job[i].Name);
		}

		// Access requirements (use AccessLevel enum with spaced names)
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemAccessReq", out var accCtrl) && accCtrl is ComboBox cmbAcc)
		{
			cmbAcc.Items.Clear();
			foreach (var name in Enum.GetNames(typeof(AccessLevel)))
			{
				string display = System.Text.RegularExpressions.Regex.Replace(name, "(?<!^)([A-Z])", " $1");
				cmbAcc.Items.Add(display);
			}
		}

		// Tool list (resource names)
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemTool", out var toolCtrl) && toolCtrl is ComboBox cmbTool)
		{
			cmbTool.Items.Clear();
			cmbTool.Items.Add("None");
			for (int i = 0; i < Variables.MaxResources; i++)
				cmbTool.Items.Add(Data.Resource[i].Name);
		}

		// Knockback tiles choices
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemKnockBackTiles", out var kbCtrl) && kbCtrl is ComboBox cmbKb)
		{
			cmbKb.Items.Clear();
			for (int i = 0; i < 6; i++)
				cmbKb.Items.Add(i.ToString());
		}

		// Skill list
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemSkill", out var skillCtrl) && skillCtrl is ComboBox cmbSkill)
		{
			cmbSkill.Items.Clear();
			cmbSkill.Items.Add("None");
			for (int i = 0; i < Variables.MaxSkills; i++)
				cmbSkill.Items.Add($"{i + 1}: {Data.Skill[i].Name}");
		}

		// Projectile list
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemProjectile", out var projCtrl) && projCtrl is ComboBox cmbProj)
		{
			cmbProj.Items.Clear();
			cmbProj.Items.Add("None");
			for (int i = 0; i < Variables.MaxProjectiles; i++)
				cmbProj.Items.Add($"{i + 1}: {Data.Projectile[i].Name}");
		}

		// Ammo list (0 = None, then items)
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemAmmo", out var ammoCtrl) && ammoCtrl is ComboBox cmbAmmo)
		{
			cmbAmmo.Items.Clear();
			cmbAmmo.Items.Add("None");
			for (int i = 0; i < Variables.MaxItems; i++)
			{
				var n = Data.Item[i].Name ?? string.Empty;
				if (string.IsNullOrWhiteSpace(n)) n = "None";
				cmbAmmo.Items.Add($"{i + 1}: {n.Trim()}");
			}
		}
	}

	public static void BuildSubtypeList()
	{
		if (!WindowManager.TryGetControl("winItemEditor", "cmbItemSubType", out var subCtrl) || subCtrl is not ComboBox cmbSub)
			return;
		cmbSub.Items.Clear();
		var type = (ItemCategory)Math.Clamp(Data.Item[SelectedIndex].Type, 0, int.MaxValue);
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
			var sub = Math.Clamp(Data.Item[SelectedIndex].SubType, 0, cmbSub.Items.Count - 1);
			cmbSub.Value = sub;
		}
	}

	public static void LoadItem(int index)
	{
		if (index < 0 || index >= Variables.MaxItems) return;
		SelectedIndex = index;
		GameState.EditorIndex = index;
		var item = Data.Item[index];

		// Basics
		if (WindowManager.TryGetControl("winItemEditor", "txtItemName", out var nameCtrl) && nameCtrl is TextBox txtName)
			txtName.Text = item.Name ?? string.Empty;
		if (WindowManager.TryGetControl("winItemEditor", "txtItemDescription", out var descCtrl) && descCtrl is TextBox txtDesc)
			txtDesc.Text = item.Description ?? string.Empty;
		
		// icon index is driven by scrollbar; no direct text box anymore
		if (WindowManager.TryGetControl("winItemEditor", "sldItemIcon", out var iconScrollCtrl) && iconScrollCtrl is ScrollBar sldIcon)
		{
			// Max icons is NumItems icons (mirror how item icons are stored: per item index)
			sldIcon.Min = 0;
			sldIcon.Max = Math.Max(0, GameState.NumItems);
			sldIcon.Value = Math.Clamp(item.Icon, sldIcon.Min, sldIcon.Max);
		}
		if (WindowManager.TryGetControl("winItemEditor", "txtItemPaperdoll", out var pdCtrl) && pdCtrl is TextBox txtPd)
			txtPd.Text = item.Paperdoll.ToString();
		// Paperdoll scrollbar range and value
		if (WindowManager.TryGetControl("winItemEditor", "sldItemPaperdoll", out var pdScrollCtrl) && pdScrollCtrl is ScrollBar sldPd)
		{
			sldPd.Min = 0;
			sldPd.Max = Math.Max(0, GameState.NumPaperdolls);
			sldPd.Value = Math.Clamp(item.Paperdoll, sldPd.Min, sldPd.Max);
		}
		// Hook paperdoll draw if not already (safe to set repeatedly)
		if (WindowManager.TryGetControl("winItemEditor", "picItemPaperdoll", out var pdPicCtrl) && pdPicCtrl is PictureBox pdPic)
			pdPic.OnDraw = OnDrawPaperdoll;

		if (WindowManager.TryGetControl("winItemEditor", "cmbItemType", out var typeCtrl) && typeCtrl is ComboBox cmbType)
			cmbType.Value = Math.Clamp(item.Type, 0, cmbType.Items.Count - 1);
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemSubType", out var subCtrl) && subCtrl is ComboBox cmbSub)
			cmbSub.Value = item.SubType;
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemAnimation", out var animCtrl) && animCtrl is ComboBox cmbAnim)
			cmbAnim.Value = Math.Clamp(item.Animation, 0, cmbAnim.Items.Count - 1);
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemBind", out var bindCtrl) && bindCtrl is ComboBox cmbBind)
			cmbBind.Value = Math.Clamp(item.BindType, 0, cmbBind.Items.Count - 1);

		if (WindowManager.TryGetControl("winItemEditor", "cmbItemLevel", out var lvlCtrl) && lvlCtrl is ComboBox cmbItemLevel)
			cmbItemLevel.Value = Math.Clamp(item.ItemLevel - 1, 0, cmbItemLevel.Items.Count - 1);
		if (WindowManager.TryGetControl("winItemEditor", "txtItemPrice", out var priceCtrl) && priceCtrl is TextBox txtPrice)
			txtPrice.Text = item.Price.ToString();
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemRarity", out var rarCtrl) && rarCtrl is ComboBox cmbRarity)
			cmbRarity.Value = Math.Clamp(item.Rarity - 1, 0, cmbRarity.Items.Count - 1);
		if (WindowManager.TryGetControl("winItemEditor", "chkItemStackable", out var stackCtrl) && stackCtrl is CheckBox chkStack)
			chkStack.Value = item.Stackable != 0 ? 1 : 0;

		// Equipment & stats
		if (WindowManager.TryGetControl("winItemEditor", "txtItemDamage", out var dmgText) && dmgText is TextBox txtDmg)
		{
			txtDmg.Text = item.Data2.ToString();
		}
		if (WindowManager.TryGetControl("winItemEditor", "txtItemSpeed", out var spdText) && spdText is TextBox txtSpd)
		{
			txtSpd.Text = item.Speed.ToString();
		}
		if (WindowManager.TryGetControl("winItemEditor", "chkItemKnockBack", out var kbCtrl2) && kbCtrl2 is CheckBox chkKb)
			chkKb.Value = item.KnockBack != 0 ? 1 : 0;
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemTool", out var toolCtrl) && toolCtrl is ComboBox cmbTool)
			cmbTool.Value = Math.Clamp(item.Data3, 0, cmbTool.Items.Count - 1);
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemKnockBackTiles", out var kbtCtrl) && kbtCtrl is ComboBox cmbKbTiles)
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
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemSkill", out var sCtrl) && sCtrl is ComboBox cmbSkill)
			cmbSkill.Value = Math.Clamp(item.Data1, 0, cmbSkill.Items.Count - 1);
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemProjectile", out var pCtrl) && pCtrl is ComboBox cmbProj)
			cmbProj.Value = Math.Clamp(item.Projectile + 1, 0, cmbProj.Items.Count - 1);
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemAmmo", out var ammoCtrl) && ammoCtrl is ComboBox cmbAmmo)
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

		if (WindowManager.TryGetControl("winItemEditor", "cmbItemJobReq", out var jCtrl2) && jCtrl2 is ComboBox cmbJob2)
			cmbJob2.Value = Math.Clamp(item.JobReq, 0, cmbJob2.Items.Count - 1);
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemAccessReq", out var aCtrl2) && aCtrl2 is ComboBox cmbAcc2)
			cmbAcc2.Value = Math.Clamp(item.AccessReq, 0, cmbAcc2.Items.Count - 1);

		// Rebuild subtype list now that type is applied
		BuildSubtypeList();
	}

	public static void OnDrawIcon()
	{
		var win = WindowManager.GetWindowByName("winItemEditor");
		if (win is null) return;

		if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxItems) return;
		var item = Data.Item[SelectedIndex];

		if (item.Icon < 1 || item.Icon > GameState.NumItems) return;

		if (!WindowManager.TryGetControl("winItemEditor", "picItemIcon", out var iconCtrl) || iconCtrl is not PictureBox pic)
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
		var item = Data.Item[SelectedIndex];
		if (item.Paperdoll < 1 || item.Paperdoll > GameState.NumPaperdolls) return;
		if (!WindowManager.TryGetControl("winItemEditor", "picItemPaperdoll", out var ctrl) || ctrl is not PictureBox pic)
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
		GameClient.RenderTexture(ref texturePath, drawX, drawY, 0, 0, srcW, srcH, destW, destH);
	}

	public static void OnListMouseDown()
	{
		if (!WindowManager.TryGetControl("winItemEditor", "lstItemIndex", out var ctrl) || ctrl is not ListBox list) return;
		var win = WindowManager.GetWindowByName("winItemEditor");
		if (win is null) return;
		int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
		int index = list.GetItemIndexAtPosition(relY);
		if (index < 0 || index >= Variables.MaxItems) return;
		SelectedIndex = index;
		GameState.EditorIndex = index;
		list.SelectedIndex = index;
		list.EnsureVisible(index);
		LoadItem(index);
	}

	public static void UpdateName(string newName)
	{
		if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxItems) return;
		Data.Item[SelectedIndex].Name = Strings.Trim(newName ?? string.Empty);
		GameState.ItemChanged[SelectedIndex] = true;
		RefreshList();
	}

	public static void OnCopyOrPaste()
	{
		if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxItems) return;

		if (_clipboardItem is null)
		{
			_clipboardItem = Data.Item[SelectedIndex];
			if (WindowManager.TryGetControl("winItemEditor", "btnItemCopy", out var btn) && btn is Button b)
				b.Text = "Paste";
			return;
		}

		Data.Item[SelectedIndex] = _clipboardItem.Value;
		GameState.ItemChanged[SelectedIndex] = true;
		LoadItem(SelectedIndex);
		RefreshList();
	}
}

