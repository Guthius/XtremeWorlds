using Client.Game.UI.Controls;
using Core.Globals;
using Microsoft.Xna.Framework;
using System;

namespace Client.Game.UI.Windows;

public class WinNpcEditor
{
    public static int SelectedIndex = 0;
    public static bool IsLoading = false;
    private static Core.Globals.Type.Npc? _history = null;

    public static void Init()
    {
        if (!WindowManager.TryGetControl("winNpcEditor", "lstIndex", out _))
            return; // window not present yet

        PopulateCombos();
        SelectedIndex = Math.Clamp(SelectedIndex, 0, Variables.MaxNpcs - 1);
        RefreshList();
        OnLoad(SelectedIndex);
    }

    public static void RefreshList()
    {
        if (!WindowManager.TryGetControl("winNpcEditor", "lstIndex", out var lstCtrl) || lstCtrl is not ListBox lst)
            return;

        int prevIndex = SelectedIndex;
        int prevScroll = lst.ScrollOffset;

        lst.Clear();
        for (int i = 0; i < Variables.MaxNpcs; i++)
        {
            string name = Strings.Trim(Data.Npc[i].Name);
            if (string.IsNullOrWhiteSpace(name)) name = "None";
            lst.AddItem($"{i + 1}: {name}");
        }

        // Restore selection and scroll
        if (prevIndex >= 0 && prevIndex < lst.Items.Count)
        {
            lst.SelectedIndex = prevIndex;
            SelectedIndex = prevIndex;
            lst.EnsureVisible(prevIndex);
        }

        // Sync scrollbar max/value if present
        if (WindowManager.TryGetControl("winNpcEditor", "sldList", out var sldCtrl) && sldCtrl is ScrollBar sb)
        {
            int visible = lst.GetVisibleCount();
            int max = Math.Max(0, lst.Items.Count - visible);
            sb.Min = 0;
            sb.Max = max;
            sb.Value = Math.Max(0, Math.Min(prevScroll, max));
        }
    }

    public static void OnDrawSprite()
    {
        var win = WindowManager.GetWindowByName("winNpcEditor");
        if (win is null) return;

        if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxNpcs) return;
        var npc = Data.Npc[SelectedIndex];

        int spriteIndex = npc.Sprite;

        if (spriteIndex < 1 || spriteIndex > GameState.NumCharacters)
            return;

        var spritePath = System.IO.Path.Combine(DataPath.Characters, spriteIndex.ToString());
        var sprite = GameClient.GetGfxInfo(spritePath);
        if (sprite is null) return;

        // Compute frame size similarly to character creator
        int frameCount = Core.Configurations.SettingsManager.Instance.RunFrames +
                         Core.Configurations.SettingsManager.Instance.IdleFrames +
                         Core.Configurations.SettingsManager.Instance.AttackFrames;
        if (frameCount <= 0) frameCount = 1;
        int w = sprite.Width / frameCount;
        int dirs = Math.Max(1, Core.Configurations.SettingsManager.Instance.SpriteDirections);
        if (sprite.Height % dirs != 0) dirs = 4; // fallback
        int h = sprite.Height / (dirs == 0 ? 1 : dirs);

        // Center inside picSprite
        if (!WindowManager.TryGetControl("winNpcEditor", "picSprite", out var ctrl) || ctrl is not PictureBox pic)
            return;

        int drawX = win.X + pic.X + (pic.Width - w) / 2;
        int drawY = win.Y + pic.Y + (pic.Height - h) / 2;

        GameClient.RenderTexture(ref spritePath, drawX, drawY, 0, 0, w, h, w, h);
    }

    private static void PopulateCombos()
    {
        // Animation list
        if (WindowManager.TryGetControl("winNpcEditor", "cmbAnimation", out var animCtrl) && animCtrl is ComboBox cmbAnim)
        {
            cmbAnim.Items.Clear();
            for (int i = 0; i < Variables.MaxAnimations; i++)
            {
                var raw = Animation.Instance[i].Name ?? string.Empty;
                var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                cmbAnim.Items.Add($"{i + 1}: {name}");
            }
        }

        // Skills (reusable population helper)
        void FillSkills(string ctrlName)
        {
            if (WindowManager.TryGetControl("winNpcEditor", ctrlName, out var skillCtrl) && skillCtrl is ComboBox cmb)
            {
                cmb.Items.Clear();
                for (int i = 0; i < Variables.MaxSkills; i++)
                {
                    var raw = Data.Skill[i].Name ?? string.Empty;
                    var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                    cmb.Items.Add($"{i + 1}: {name}");
                }
            }
        }
        FillSkills("cmbSkill1");
        FillSkills("cmbSkill2");
        FillSkills("cmbSkill3");
        FillSkills("cmbSkill4");
        FillSkills("cmbSkill5");
        FillSkills("cmbSkill6");

        // Items for drop slot
        if (WindowManager.TryGetControl("winNpcEditor", "cmbDropItem", out var itemCtrl) && itemCtrl is ComboBox cmbItem)
        {
            cmbItem.Items.Clear();
            for (int i = 0; i < Variables.MaxItems; i++)
            {
                var raw = Item.Instance[i].Name ?? string.Empty;
                var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                cmbItem.Items.Add($"{i + 1}: {name}");
            }
        }

        // Behavior
        if (WindowManager.TryGetControl("winNpcEditor", "cmbBehavior", out var behCtrl) && behCtrl is ComboBox cmbBeh)
        {
            cmbBeh.Items.Clear();
            cmbBeh.Items.Add("Aggressive");
            cmbBeh.Items.Add("Roam");
            cmbBeh.Items.Add("Stationary");
        }

        // Faction
        if (WindowManager.TryGetControl("winNpcEditor", "cmbFaction", out var facCtrl) && facCtrl is ComboBox cmbFac)
        {
            cmbFac.Items.Clear();
            cmbFac.Items.Add("Neutral");
            cmbFac.Items.Add("Friendly");
            cmbFac.Items.Add("Hostile");
        }

        // Spawn period
        if (WindowManager.TryGetControl("winNpcEditor", "cmbSpawnPeriod", out var perCtrl) && perCtrl is ComboBox cmbPeriod)
        {
            cmbPeriod.Items.Clear();
            cmbPeriod.Items.Add("Any");
            cmbPeriod.Items.Add("Day");
            cmbPeriod.Items.Add("Night");
        }

        // Drop slot selector (1..6)
        if (WindowManager.TryGetControl("winNpcEditor", "cmbDropSlot", out var dropSlotCtrl) && dropSlotCtrl is ComboBox cmbDropSlot)
        {
            if (cmbDropSlot.Items.Count == 0)
            {
                for (int i = 0; i < 6; i++) cmbDropSlot.Items.Add((i + 1).ToString());
                cmbDropSlot.Value = 0;
            }
        }
    }

    // Load selected NPC data into UI controls.
    public static void OnLoad(int index)
    {
        if (index < 0 || index >= Variables.MaxNpcs) return;
        IsLoading = true;
        SelectedIndex = index;
        var npc = Data.Npc[index];

        // Name
        if (WindowManager.TryGetControl("winNpcEditor", "txtName", out var nameCtrl) && nameCtrl is TextBox txtName)
        {
            txtName.Text = npc.Name ?? string.Empty;
        }
        // Attack say
        if (WindowManager.TryGetControl("winNpcEditor", "txtAttackSay", out var atkCtrl) && atkCtrl is TextBox txtAtk)
        {
            txtAtk.Text = npc.AttackSay ?? string.Empty;
        }
        // Behavior
        if (WindowManager.TryGetControl("winNpcEditor", "cmbBehavior", out var behCtrl) && behCtrl is ComboBox cmbBeh)
        {
            cmbBeh.Value = Math.Clamp(npc.Behavior, 0, cmbBeh.Items.Count - 1);
        }
        // Faction
        if (WindowManager.TryGetControl("winNpcEditor", "cmbFaction", out var facCtrl) && facCtrl is ComboBox cmbFac)
        {
            cmbFac.Value = Math.Clamp(npc.Faction, 0, cmbFac.Items.Count - 1);
        }
        // Spawn period
        if (WindowManager.TryGetControl("winNpcEditor", "cmbSpawnPeriod", out var periodCtrl) && periodCtrl is ComboBox cmbPeriod)
        {
            cmbPeriod.Value = Math.Clamp(npc.SpawnTime, 0, cmbPeriod.Items.Count - 1);
        }
        // Animation
        if (WindowManager.TryGetControl("winNpcEditor", "cmbAnimation", out var animCtrl) && animCtrl is ComboBox cmbAnim)
        {
            cmbAnim.Value = Math.Clamp(npc.Animation, 0, cmbAnim.Items.Count - 1);
        }
        // Sprite scrollbar (sldSprite) reflects npc.Sprite; drawing happens in the UI draw phase
        if (WindowManager.TryGetControl("winNpcEditor", "sldSprite", out var spriteCtrl) && spriteCtrl is ScrollBar sbSprite)
        {
            sbSprite.Max = Math.Max(0, GameState.NumCharacters);

            var spriteIndex = npc.Sprite;
            sbSprite.Value = Math.Clamp(spriteIndex, sbSprite.Min, sbSprite.Max);
        }
        // Basic stats
        if (WindowManager.TryGetControl("winNpcEditor", "txtHp", out var hpCtrl) && hpCtrl is TextBox txtHp)
        {
            txtHp.Text = npc.Hp.ToString();
        }
        if (WindowManager.TryGetControl("winNpcEditor", "txtExp", out var expCtrl) && expCtrl is TextBox txtExp)
        {
            txtExp.Text = npc.Exp.ToString();
        }
        if (WindowManager.TryGetControl("winNpcEditor", "txtLevel", out var lvlCtrl) && lvlCtrl is TextBox txtLvl)
        {
            txtLvl.Text = npc.Level.ToString();
        }
        if (WindowManager.TryGetControl("winNpcEditor", "txtDamage", out var dmgCtrl) && dmgCtrl is TextBox txtDmg)
        {
            txtDmg.Text = npc.Damage.ToString();
        }
        if (WindowManager.TryGetControl("winNpcEditor", "txtRange", out var rangeCtrl) && rangeCtrl is TextBox txtRange)
        {
            txtRange.Text = npc.Range.ToString();
        }
        // Skills
        void SetSkill(string ctrlName, int skillIdx)
        {
            if (WindowManager.TryGetControl("winNpcEditor", ctrlName, out var sCtrl) && sCtrl is ComboBox cmb && npc.Skill != null && skillIdx < npc.Skill.Length)
            {
                cmb.Value = Math.Clamp(npc.Skill[skillIdx], 0, Math.Max(0, cmb.Items.Count - 1));
            }
        }
        SetSkill("cmbSkill1", 0);
        SetSkill("cmbSkill2", 1);
        SetSkill("cmbSkill3", 2);
        SetSkill("cmbSkill4", 3);
        SetSkill("cmbSkill5", 4);
        SetSkill("cmbSkill6", 5);

        // Drop slot fields sync: use the selected slot to show that slot's item
        int slot = 0;
        if (WindowManager.TryGetControl("winNpcEditor", "cmbDropSlot", out var dsCtrl) && dsCtrl is ComboBox cmbDropSlot)
        {
            // Clamp to valid slot range 0-5
            cmbDropSlot.Value = Math.Clamp(cmbDropSlot.Value, 0, 5);
            slot = cmbDropSlot.Value;
        }

        // Item combo always reflects the stored item index for the current slot
        if (WindowManager.TryGetControl("winNpcEditor", "cmbDropItem", out var diCtrl) && diCtrl is ComboBox cmbItem)
        {
            if (npc.DropItem != null && slot < npc.DropItem.Length)
            {
                var storedIndex = Math.Clamp(npc.DropItem[slot], 0, Variables.MaxItems - 1);
                cmbItem.Value = Math.Clamp(storedIndex, 0, Math.Max(0, cmbItem.Items.Count - 1));
            }
            else
            {
                cmbItem.Value = 0;
            }
        }

        // Amount textbox reflects DropItemValue for current slot
        if (WindowManager.TryGetControl("winNpcEditor", "txtAmount", out var amtCtrl) && amtCtrl is TextBox txtAmt)
        {
            int amount = 0;
            if (npc.DropItemValue != null && slot < npc.DropItemValue.Length)
                amount = npc.DropItemValue[slot];
            txtAmt.Text = amount.ToString();
        }

        // Chance slider reflects DropChance for current slot (within existing range)
        if (WindowManager.TryGetControl("winNpcEditor", "sldChance", out var chanceCtrl) && chanceCtrl is ScrollBar sbChance)
        {
            int min = sbChance.Min;
            int max = sbChance.Max;
            int chance = 0;
            if (npc.DropChance != null && slot < npc.DropChance.Length)
                chance = npc.DropChance[slot];
            sbChance.Value = Math.Clamp(chance, min, max);
        }

        RefreshList();
        IsLoading = false;
    }

    // Handle list click (mouse down) to select NPC.
    public static void OnListMouseDown()
    {
        if (!WindowManager.TryGetControl("winNpcEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list) return;
        var win = WindowManager.GetWindowByName("winNpcEditor");
        if (win is null) return;
        int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
        int index = list.GetItemIndexAtPosition(relY);
        if (index < 0 || index >= Variables.MaxNpcs) return;
        SelectedIndex = index;
        list.SelectedIndex = index;
        list.EnsureVisible(index);
        PopulateCombos();
        OnLoad(index);
    }

    public static void OnCopyOrPaste()
    {
        if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxNpcs) return;
        if (_history is null)
        {
            // Copy current NPC (deep copy for arrays)
            var s = Data.Npc[SelectedIndex];
            var n = s; // struct copy
            if (s.Stat != null) { n.Stat = new byte[s.Stat.Length]; Array.Copy(s.Stat, n.Stat, s.Stat.Length); }
            if (s.Skill != null) { n.Skill = new byte[s.Skill.Length]; Array.Copy(s.Skill, n.Skill, s.Skill.Length); }
            if (s.DropItem != null) { n.DropItem = new int[s.DropItem.Length]; Array.Copy(s.DropItem, n.DropItem, s.DropItem.Length); }
            if (s.DropItemValue != null) { n.DropItemValue = new int[s.DropItemValue.Length]; Array.Copy(s.DropItemValue, n.DropItemValue, s.DropItemValue.Length); }
            if (s.DropChance != null) { n.DropChance = new int[s.DropChance.Length]; Array.Copy(s.DropChance, n.DropChance, s.DropChance.Length); }
            _history = n;
            if (WindowManager.TryGetControl("winNpcEditor", "btnCopy", out var btn)) btn.Text = "Paste";
            return;
        }

        // Paste clipboard into current slot
        var pasted = _history.Value;
        Data.Npc[SelectedIndex] = pasted;
        GameState.NpcChanged[SelectedIndex] = true;
        // Refresh UI to reflect pasted data
        OnLoad(SelectedIndex);
        RefreshList();
    }

    // Unified handlers
    public static void OnSave()
    {
        Editors.NpcEditorOK();
        WindowManager.HideWindow("winNpcEditor");
    }

    public static void OnCancel()
    {
        Editors.NpcEditorCancel();
        WindowManager.HideWindow("winNpcEditor");
    }

    public static void OnDelete()
    {
        Npc.OnClear(GameState.EditorIndex);
        GameState.NpcChanged[SelectedIndex] = true;
        OnLoad(GameState.EditorIndex);
        RefreshList();
    }

    public static void OnCopy()
    {
        OnCopyOrPaste();
    }
}
