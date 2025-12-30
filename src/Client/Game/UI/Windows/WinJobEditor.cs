using Client.Game.UI;
using Client.Game.UI.Controls;
using Core.Globals;
using System;

namespace Client.Game.UI.Windows
{
    public class WinJobEditor
    {
        public static int SelectedIndex = 0;
        private static Job? _history = null;

        public static void Init()
        {
            if (!WindowManager.TryGetControl("winJobEditor", "lstIndex", out _))
                return; // window not present yet

            SelectedIndex = 0;
            RefreshList();
            PopulateStaticCombos();
            OnLoad(SelectedIndex);
        }

        // Picture previews
        public static void OnDrawMaleSprite()
        {
            int idx = SelectedIndex;
            if (idx < 0 || idx >= Variables.MaxJobs) return;
            var job = Job.Instance[idx];
            int sprite = job?.MaleSprite ?? 0;
            DrawSpritePreview("picMale", sprite);
        }
        public static void OnDrawFemaleSprite()
        {
            int idx = SelectedIndex;
            if (idx < 0 || idx >= Variables.MaxJobs) return;
            var job = Job.Instance[idx];
            int sprite = job?.FemaleSprite ?? 0;
            DrawSpritePreview("picFemale", sprite);
        }

        private static void DrawSpritePreview(string picName, int spriteIndex)
        {
            var win = WindowManager.GetWindowByName("winJobEditor");
            if (win is null) return;
            if (!WindowManager.TryGetControl("winJobEditor", picName, out var ctrl) || ctrl is not PictureBox pic) return;

            if (spriteIndex < 1 || spriteIndex > GameState.NumCharacters) return;

            var spritePath = System.IO.Path.Combine(DataPath.Characters, spriteIndex.ToString());
            var spriteTexture = GameClient.GetGfxInfo(spritePath);
            if (spriteTexture is null) return;

            // Direction rows and columns segmentation like other editors
            int configuredDirs = Core.Configurations.SettingsManager.Instance.SpriteDirections <= 0 ? 4 : Core.Configurations.SettingsManager.Instance.SpriteDirections;
            configuredDirs = Math.Max(1, configuredDirs);
            int directionRows;
            if (spriteTexture.Height % configuredDirs == 0) directionRows = configuredDirs;
            else if (configuredDirs != 8 && spriteTexture.Height % 8 == 0) directionRows = 8;
            else if (configuredDirs != 4 && spriteTexture.Height % 4 == 0) directionRows = 4;
            else directionRows = 1;

            int frameHeight = Math.Max(1, spriteTexture.Height / directionRows);

            int idleFrames = Math.Max(1, Core.Configurations.SettingsManager.Instance.IdleFrames);
            int runFrames = Math.Max(1, Core.Configurations.SettingsManager.Instance.RunFrames);
            int attackFrames = Math.Max(1, Core.Configurations.SettingsManager.Instance.AttackFrames);
            int expectedCols = idleFrames + runFrames + attackFrames;
            bool segmented = expectedCols > 0 && spriteTexture.Width % expectedCols == 0;

            int frameWidth;
            if (segmented)
                frameWidth = spriteTexture.Width / expectedCols;
            else if (frameHeight > 0 && spriteTexture.Width % frameHeight == 0)
            {
                int approxCols = spriteTexture.Width / frameHeight;
                frameWidth = approxCols > 0 ? spriteTexture.Width / approxCols : spriteTexture.Width;
            }
            else frameWidth = spriteTexture.Width;

            int destX = win.X + pic.X + (pic.Width - frameWidth) / 2;
            int destY = win.Y + pic.Y + (pic.Height - frameHeight) / 2;

            GameClient.RenderTexture(ref spritePath, destX, destY, 0, 0, frameWidth, frameHeight, frameWidth, frameHeight);
        }

        public static void PopulateStaticCombos()
        {
            // Items combo: 0=None, then 1..MaxItems with names
            if (WindowManager.TryGetControl("winJobEditor", "cmbItem", out var itemCtrl) && itemCtrl is ComboBox cmbItem)
            {
                int prev = cmbItem.Value;
                cmbItem.Items.Clear();
                cmbItem.Items.Add("None");
                for (int i = 0; i < Core.Globals.Variables.MaxItems; i++)
                {
                    var raw = Item.Instance[i].Name ?? string.Empty;
                    var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                    cmbItem.Items.Add($"{i + 1}: {name}");
                }
                cmbItem.Value = (prev >= 0 && prev < cmbItem.Items.Count) ? prev : 0;
            }
            // Skills combo: 0=None, then 1..MaxSkills with names
            if (WindowManager.TryGetControl("winJobEditor", "cmbSkill", out var skCtrl) && skCtrl is ComboBox cmbSkill)
            {
                int prev = cmbSkill.Value;
                cmbSkill.Items.Clear();
                cmbSkill.Items.Add("None");
                for (int i = 0; i < Variables.MaxSkills; i++)
                {
                    var raw = Skill.Instance[i].Name ?? string.Empty;
                    var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                    cmbSkill.Items.Add($"{i + 1}: {name}");
                }
                cmbSkill.Value = (prev >= 0 && prev < cmbSkill.Items.Count) ? prev : 0;
            }
        }

        public static void OnListMouseDown()
        {
            if (!WindowManager.TryGetControl("winJobEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list) return;
            var win = WindowManager.GetWindowByName("winJobEditor");
            if (win is null) return;
            int relY = GameState.CurMouseY - (win.Y + ctrl.Y);
            int index = list.GetItemIndexAtPosition(relY);
            if (index < 0 || index >= Variables.MaxJobs) return;
            SelectedIndex = index;
            GameState.EditorIndex = index;
            list.SelectedIndex = index;
            list.EnsureVisible(index);
            OnLoad(index);
        }

        public static void RefreshList()
        {
            if (!WindowManager.TryGetControl("winJobEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list)
                return;

            int prevIndex = SelectedIndex;
            int prevScroll = list.ScrollOffset;

            list.Clear();
            for (int i = 0; i < Variables.MaxJobs; i++)
            {
                var job = Job.Instance[i];
                string name = job?.Name?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name)) name = "None";
                list.AddItem($"{i + 1}: {name}");
            }

            if (prevIndex >= 0 && prevIndex < list.Items.Count)
            {
                list.SelectedIndex = prevIndex;
                list.EnsureVisible(prevIndex);
            }

            if (WindowManager.TryGetControl("winJobEditor", "sldList", out var sldCtrl) && sldCtrl is ScrollBar sb)
            {
                int visible = list.GetVisibleCount();
                int max = Math.Max(0, list.Items.Count - visible);
                sb.Min = 0;
                sb.Max = max;
                sb.Value = Math.Clamp(prevScroll, sb.Min, sb.Max);
            }
        }

        public static void OnLoad(int index)
        {
            if (index < 0 || index >= Variables.MaxJobs) return;
            var job = Job.Instance[index];
            if (job is null) return;

            SelectedIndex = index;
            GameState.EditorIndex = index;

            if (WindowManager.TryGetControl("winJobEditor", "txtName", out var nameCtrl) && nameCtrl is TextBox txtName)
                txtName.Text = job.Name ?? string.Empty;
            if (WindowManager.TryGetControl("winJobEditor", "txtDesc", out var descCtrl) && descCtrl is TextBox txtDesc)
                txtDesc.Text = job.Desc ?? string.Empty;

            if (WindowManager.TryGetControl("winJobEditor", "txtStartMap", out var sm)) sm.Text = job.StartMap.ToString();
            if (WindowManager.TryGetControl("winJobEditor", "txtStartX", out var sx)) sx.Text = job.StartX.ToString();
            if (WindowManager.TryGetControl("winJobEditor", "txtStartY", out var sy)) sy.Text = job.StartY.ToString();
            if (WindowManager.TryGetControl("winJobEditor", "txtMoveSpeed", out var ms))
                ms.Text = (job.MoveSpeed <= 0 ? 1.0f : job.MoveSpeed).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

            // Base stats
            if (job.Stat != null)
            {
                if (WindowManager.TryGetControl("winJobEditor", "txtStr", out var st) && st is TextBox tbStr)
                    tbStr.Text = job.Stat[(int)Stat.Strength].ToString();
                if (WindowManager.TryGetControl("winJobEditor", "txtVit", out var vt) && vt is TextBox tbVit)
                    tbVit.Text = job.Stat[(int)Stat.Vitality].ToString();
                if (WindowManager.TryGetControl("winJobEditor", "txtInt", out var it) && it is TextBox tbInt)
                    tbInt.Text = job.Stat[(int)Stat.Intelligence].ToString();
                if (WindowManager.TryGetControl("winJobEditor", "txtLuck", out var lk) && lk is TextBox tbLuck)
                    tbLuck.Text = job.Stat[(int)Stat.Luck].ToString();
                if (WindowManager.TryGetControl("winJobEditor", "txtSpi", out var sp) && sp is TextBox tbSpi)
                    tbSpi.Text = job.Stat[(int)Stat.Spirit].ToString();
            }

            // Sprite sliders reflect current values
            if (WindowManager.TryGetControl("winJobEditor", "sldMaleSprite", out var msCtrl) && msCtrl is ScrollBar sbMale)
            {
                sbMale.Max = Math.Max(0, GameState.NumCharacters);
                sbMale.Value = Math.Clamp(job.MaleSprite, sbMale.Min, sbMale.Max);
            }
            if (WindowManager.TryGetControl("winJobEditor", "sldFemaleSprite", out var fsCtrl) && fsCtrl is ScrollBar sbFemale)
            {
                sbFemale.Max = Math.Max(0, GameState.NumCharacters);
                sbFemale.Value = Math.Clamp(job.FemaleSprite, sbFemale.Min, sbFemale.Max);
            }

            // Start items list
            if (WindowManager.TryGetControl("winJobEditor", "lstStartItems", out var liCtrl) && liCtrl is ListBox lstItems)
            {
                lstItems.Clear();
                for (int i = 0; i < Variables.MaxStartItems; i++)
                {
                    int id = job.StartItem[i];
                    int amt = job.StartValue[i];
                    string name = id >= 0 && id < Core.Globals.Variables.MaxItems ? Item.Instance[id].Name : "(None)";
                    lstItems.AddItem($"{i + 1}: {name} x {amt}");
                }
                lstItems.SelectedIndex = 0;
            }

            // Start skills list
            if (WindowManager.TryGetControl("winJobEditor", "lstStartSkills", out var lsCtrl) && lsCtrl is ListBox lstSkills)
            {
                lstSkills.Clear();
                for (int i = 0; i < Variables.MaxStartSkills; i++)
                {
                    int sid = job.StartSkill[i];
                    string sname = sid >= 0 && sid < Variables.MaxSkills ? Skill.Instance[sid].Name : "(None)";
                    lstSkills.AddItem($"{i + 1}: {sname}");
                }
                lstSkills.SelectedIndex = 0;
            }

            // Sync list scrollbar
            if (WindowManager.TryGetControl("winJobEditor", "sldList", out var sldCtrl) && sldCtrl is ScrollBar sb)
            {
                if (WindowManager.TryGetControl("winJobEditor", "lstIndex", out var lc) && lc is ListBox lb)
                {
                    int visible = lb.GetVisibleCount();
                    int max = Math.Max(0, lb.Items.Count - visible);
                    sb.Min = 0; sb.Max = max;
                    sb.Value = Math.Clamp(lb.ScrollOffset, sb.Min, sb.Max);
                }
            }
        }

        public static void OnCopyOrPaste()
        {
            if (SelectedIndex < 0 || SelectedIndex >= Variables.MaxJobs) return;
            if (_history is null)
            {
                var s = Job.Instance[SelectedIndex];
                if (s.Stat != null) s.Stat = (int[])s.Stat.Clone();
                if (s.StartItem != null) s.StartItem = (int[])s.StartItem.Clone();
                if (s.StartValue != null) s.StartValue = (int[])s.StartValue.Clone();
                if (s.StartSkill != null) s.StartSkill = (int[])s.StartSkill.Clone();
                _history = (Job)s;
                if (WindowManager.TryGetControl("winJobEditor", "btnCopy", out var btn)) btn.Text = "Paste";
                return;
            }

            var pasted = _history;
            Job.Instance[SelectedIndex] = pasted;
            Job.IsChanged[SelectedIndex] = true;
            OnLoad(SelectedIndex);
            RefreshList();
        }

        public static void OnSave()
        {
            Editors.JobEditorOK();
            WindowManager.HideWindow("winJobEditor");
        }

        public static void OnCancel()
        {
            Editors.JobEditorCancel();
            WindowManager.HideWindow("winJobEditor");
        }

        public static void OnDelete()
        {
            Job.OnClear(SelectedIndex);
            Job.IsChanged[SelectedIndex] = true;
            OnLoad(SelectedIndex);
            RefreshList();
        }

        public static void OnCopy()
        {
            OnCopyOrPaste();
        }
    }
}