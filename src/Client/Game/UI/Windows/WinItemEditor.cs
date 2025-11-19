using Client.Game.UI;
using Client.Game.UI.Controls;
using Core.Globals;
using System;

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

		// Job requirements
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemJobReq", out var jobCtrl) && jobCtrl is ComboBox cmbJob)
		{
			cmbJob.Items.Clear();
			for (int i = 0; i < Variables.MaxJobs; i++)
				cmbJob.Items.Add(Data.Job[i].Name);
		}

		// Access requirements
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemAccessReq", out var accCtrl) && accCtrl is ComboBox cmbAcc)
		{
			cmbAcc.Items.Clear();
			cmbAcc.Items.Add("None");
			cmbAcc.Items.Add("Moderator");
			cmbAcc.Items.Add("Administrator");
		}

		// Tool list
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemTool", out var toolCtrl) && toolCtrl is ComboBox cmbTool)
		{
			cmbTool.Items.Clear();
			cmbTool.Items.Add("None");
			for (int i = 0; i < Variables.MaxResources; i++)
				cmbTool.Items.Add(Data.Resource[i].Name);
		}

		// Knockback tiles choices (0..10)
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemKnockBackTiles", out var kbCtrl) && kbCtrl is ComboBox cmbKb)
		{
			cmbKb.Items.Clear();
			for (int i = 0; i <= 10; i++)
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
				cmbProj.Items.Add(Data.Projectile[i].Name);
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
		if (WindowManager.TryGetControl("winItemEditor", "txtItemIcon", out var iconCtrl) && iconCtrl is TextBox txtIcon)
			txtIcon.Text = item.Icon.ToString();
		if (WindowManager.TryGetControl("winItemEditor", "txtItemPaperdoll", out var pdCtrl) && pdCtrl is TextBox txtPd)
			txtPd.Text = item.Paperdoll.ToString();

		if (WindowManager.TryGetControl("winItemEditor", "cmbItemType", out var typeCtrl) && typeCtrl is ComboBox cmbType)
			cmbType.Value = Math.Clamp(item.Type, 0, cmbType.Items.Count - 1);
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemSubType", out var subCtrl) && subCtrl is ComboBox cmbSub)
			cmbSub.Value = item.SubType;
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemAnimation", out var animCtrl) && animCtrl is ComboBox cmbAnim)
			cmbAnim.Value = Math.Clamp(item.Animation, 0, cmbAnim.Items.Count - 1);
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemBind", out var bindCtrl) && bindCtrl is ComboBox cmbBind)
			cmbBind.Value = Math.Clamp(item.BindType, 0, cmbBind.Items.Count - 1);

		if (WindowManager.TryGetControl("winItemEditor", "txtItemLevel", out var lvlCtrl) && lvlCtrl is TextBox txtLvl)
			txtLvl.Text = item.ItemLevel.ToString();
		if (WindowManager.TryGetControl("winItemEditor", "txtItemPrice", out var priceCtrl) && priceCtrl is TextBox txtPrice)
			txtPrice.Text = item.Price.ToString();
		if (WindowManager.TryGetControl("winItemEditor", "txtItemRarity", out var rarCtrl) && rarCtrl is TextBox txtRarity)
			txtRarity.Text = item.Rarity.ToString();
		if (WindowManager.TryGetControl("winItemEditor", "chkItemStackable", out var stackCtrl) && stackCtrl is CheckBox chkStack)
			chkStack.Value = item.Stackable != 0 ? 1 : 0;

		// Equipment & stats
		if (WindowManager.TryGetControl("winItemEditor", "txtItemDamage", out var dmgCtrl) && dmgCtrl is TextBox txtDmg)
			txtDmg.Text = item.Data2.ToString();
		if (WindowManager.TryGetControl("winItemEditor", "txtItemSpeed", out var spdCtrl) && spdCtrl is TextBox txtSpeed)
			txtSpeed.Text = item.Speed.ToString();
		if (WindowManager.TryGetControl("winItemEditor", "chkItemKnockBack", out var kbCtrl2) && kbCtrl2 is CheckBox chkKb)
			chkKb.Value = item.KnockBack != 0 ? 1 : 0;
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemTool", out var toolCtrl) && toolCtrl is ComboBox cmbTool)
			cmbTool.Value = Math.Clamp(item.Data3, 0, cmbTool.Items.Count - 1);
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemKnockBackTiles", out var kbtCtrl) && kbtCtrl is ComboBox cmbKbTiles)
			cmbKbTiles.Value = Math.Clamp(item.KnockBackTiles, 0, cmbKbTiles.Items.Count - 1);

		if (WindowManager.TryGetControl("winItemEditor", "txtAddStr", out var aStrCtrl) && aStrCtrl is TextBox txtAStr)
			txtAStr.Text = item.AddStat[(int)Stat.Strength].ToString();
		if (WindowManager.TryGetControl("winItemEditor", "txtAddVit", out var aVitCtrl) && aVitCtrl is TextBox txtAVit)
			txtAVit.Text = item.AddStat[(int)Stat.Vitality].ToString();
		if (WindowManager.TryGetControl("winItemEditor", "txtAddLuck", out var aLuckCtrl) && aLuckCtrl is TextBox txtALuck)
			txtALuck.Text = item.AddStat[(int)Stat.Luck].ToString();
		if (WindowManager.TryGetControl("winItemEditor", "txtAddInt", out var aIntCtrl) && aIntCtrl is TextBox txtAInt)
			txtAInt.Text = item.AddStat[(int)Stat.Intelligence].ToString();
		if (WindowManager.TryGetControl("winItemEditor", "txtAddSpr", out var aSprCtrl) && aSprCtrl is TextBox txtASpr)
			txtASpr.Text = item.AddStat[(int)Stat.Spirit].ToString();

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
		if (WindowManager.TryGetControl("winItemEditor", "txtReqLevel", out var rLvlCtrl) && rLvlCtrl is TextBox txtRLvl)
			txtRLvl.Text = item.LevelReq.ToString();
		if (WindowManager.TryGetControl("winItemEditor", "txtReqStr", out var rStrCtrl) && rStrCtrl is TextBox txtRStr)
			txtRStr.Text = item.StatReq[(int)Stat.Strength].ToString();
		if (WindowManager.TryGetControl("winItemEditor", "txtReqVit", out var rVitCtrl) && rVitCtrl is TextBox txtRVit)
			txtRVit.Text = item.StatReq[(int)Stat.Vitality].ToString();
		if (WindowManager.TryGetControl("winItemEditor", "txtReqLuck", out var rLuckCtrl) && rLuckCtrl is TextBox txtRLuck)
			txtRLuck.Text = item.StatReq[(int)Stat.Luck].ToString();
		if (WindowManager.TryGetControl("winItemEditor", "txtReqInt", out var rIntCtrl) && rIntCtrl is TextBox txtRInt)
			txtRInt.Text = item.StatReq[(int)Stat.Intelligence].ToString();
		if (WindowManager.TryGetControl("winItemEditor", "txtReqSpr", out var rSprCtrl) && rSprCtrl is TextBox txtRSpr)
			txtRSpr.Text = item.StatReq[(int)Stat.Spirit].ToString();

		if (WindowManager.TryGetControl("winItemEditor", "cmbItemJobReq", out var jCtrl2) && jCtrl2 is ComboBox cmbJob2)
			cmbJob2.Value = Math.Clamp(item.JobReq, 0, cmbJob2.Items.Count - 1);
		if (WindowManager.TryGetControl("winItemEditor", "cmbItemAccessReq", out var aCtrl2) && aCtrl2 is ComboBox cmbAcc2)
			cmbAcc2.Value = Math.Clamp(item.AccessReq, 0, cmbAcc2.Items.Count - 1);
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

