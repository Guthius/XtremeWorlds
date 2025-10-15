using System;
using System.IO;
using Client.Net;
using Core;
using Eto.Forms;
using System;
using System.IO;
using Core.Globals;
using Eto.Drawing;

namespace Client
{
    public class EditorSkill : Form
    {
        // Singleton access for legacy usage
        private static EditorSkill? _instance;
        public static EditorSkill Instance => _instance ??= new EditorSkill();
        private bool _suppressIndexChanged;
        // Copy/Paste clipboard
        private Core.Globals.Type.Skill _clipboardSkill;
        private bool _hasClipboardSkill;
        public ListBox lstIndex = new ListBox{ Width = 200 };
        public TextBox txtName = new TextBox { Width = 200 };
        public ComboBox cmbType = new ComboBox();
        public NumericStepper nudMp = new NumericStepper { MinValue = 0 };
        public NumericStepper nudLevel = new NumericStepper { MinValue = 0 };
        public ComboBox cmbAccessReq = new ComboBox();
        public ComboBox cmbJob = new ComboBox();
        public NumericStepper nudCast = new NumericStepper { MinValue = 0 };
        public NumericStepper nudCool = new NumericStepper { MinValue = 0 };
        public NumericStepper nudIcon = new NumericStepper { MinValue = 0 };
        public NumericStepper nudMap = new NumericStepper { MinValue = 0 };
        public NumericStepper nudX = new NumericStepper { MinValue = 0 };
        public NumericStepper nudY = new NumericStepper { MinValue = 0 };
        public ComboBox cmbDir = new ComboBox();
        public NumericStepper nudVital = new NumericStepper { MinValue = 0 };
        public NumericStepper nudDuration = new NumericStepper { MinValue = 0 };
        public NumericStepper nudInterval = new NumericStepper { MinValue = 0 };
        public NumericStepper nudRange = new NumericStepper { MinValue = 0 };
        public CheckBox chkAoE = new CheckBox { Text = "AoE" };
        public NumericStepper nudAoE = new NumericStepper { MinValue = 0 };
        public ComboBox cmbAnimCast = new ComboBox();
        public ComboBox cmbAnim = new ComboBox();
        public NumericStepper nudStun = new NumericStepper { MinValue = 0 };
        public CheckBox chkProjectile = new CheckBox { Text = "Projectile" };
        public ComboBox cmbProjectile = new ComboBox();
        public CheckBox chkKnockBack = new CheckBox { Text = "Knockback" };
        public ComboBox cmbKnockBackTiles = new ComboBox();
        public ComboBox cmbChainOnHit = new ComboBox();
        public ComboBox cmbCommonEventType = new ComboBox();
        public NumericStepper nudCommonEventData1 = new NumericStepper { MinValue = 0 };
        public NumericStepper nudCommonEventData2 = new NumericStepper { MinValue = 0 };
        // Multi-direction casting UI
        public CheckBox[] chkMultiDirs = new CheckBox[8];
        public Button btnSave = new Button { Text = "Save" };
        public Button btnDelete = new Button { Text = "Delete" };
        public Button btnCopy = new Button { Text = "Copy" };
        public Button btnCancel = new Button { Text = "Cancel" };
        public Button btnLearn = new Button { Text = "Learn" };
        public Drawable picSprite = new Drawable { Size = new Size(64, 64) };

        public EditorSkill()
        {
            _instance = this;
            Title = "Skill Editor";
            ClientSize = new Size(900, 560);
            Padding = 10;
            InitializeComponent();
            Editors.AutoSizeWindow(this, 820, 520);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (ReferenceEquals(_instance, this)) _instance = null;
            Editors.SkillEditorCancel();
        }

        private void InitializeComponent()
        {
            // Subscribe Load first
            Load += (s, e) => Editor_Skill_Load();

            cmbType.Items.Clear();
            foreach (var name in Enum.GetNames(typeof(SkillEffect)))
            {
                // Insert spaces before capital letters (except the first letter)
                string displayName = System.Text.RegularExpressions.Regex.Replace(name, "(?<!^)([A-Z])", " $1");
                cmbType.Items.Add(displayName);
            }

            foreach (var name in Enum.GetNames(typeof(AccessLevel)))
            {
                cmbAccessReq.Items.Add(name);
            }

            // Populate with all 8 directions in enum declaration order so SelectedIndex maps to Direction value
            cmbDir.Items.Clear();
            foreach (var name in Enum.GetNames(typeof(Direction)))
            {
                cmbDir.Items.Add(name);
            }

            cmbKnockBackTiles.Items.Add("0");
            cmbKnockBackTiles.Items.Add("1");
            cmbKnockBackTiles.Items.Add("2");
            cmbKnockBackTiles.Items.Add("3");
            cmbKnockBackTiles.Items.Add("4");
            cmbKnockBackTiles.Items.Add("5");

            // Wiring events
            lstIndex.SelectedIndexChanged += (s, e) =>
            {
                if (_suppressIndexChanged) return;
                if (lstIndex.SelectedIndex >= 0)
                    GameState.EditorIndex = lstIndex.SelectedIndex;
                LstIndex_Click();
            };
            txtName.TextChanged += (s, e) => TxtName_TextChanged();
            cmbType.SelectedIndexChanged += (s, e) => CmbType_SelectedIndexChanged();
            nudMp.ValueChanged += (s, e) => NudMp_ValueChanged();
            nudLevel.ValueChanged += (s, e) => NudLevel_ValueChanged();
            cmbAccessReq.SelectedIndexChanged += (s, e) => CmbAccessReq_SelectedIndexChanged();
            cmbJob.SelectedIndexChanged += (s, e) => CmbJob_SelectedIndexChanged();
            nudCast.ValueChanged += (s, e) => NudCast_ValueChanged();
            nudCool.ValueChanged += (s, e) => NudCool_ValueChanged();
            nudIcon.ValueChanged += (s, e) => NudIcon_ValueChanged();
            nudMap.ValueChanged += (s, e) => NudMap_ValueChanged();
            cmbDir.SelectedIndexChanged += (s, e) => CmbDir_SelectedIndexChanged();
            nudX.ValueChanged += (s, e) => NudX_ValueChanged();
            nudY.ValueChanged += (s, e) => NudY_ValueChanged();
            nudVital.ValueChanged += (s, e) => NudVital_ValueChanged();
            nudDuration.ValueChanged += (s, e) => NudDuration_ValueChanged();
            nudInterval.ValueChanged += (s, e) => NudInterval_ValueChanged();
            nudRange.ValueChanged += (s, e) => NudRange_ValueChanged();
            chkAoE.CheckedChanged += (s, e) => ChkAoE_CheckedChanged();
            nudAoE.ValueChanged += (s, e) => NudAoE_ValueChanged();
            cmbAnimCast.SelectedIndexChanged += (s, e) => CmbAnimCast_SelectedIndexChanged();
            cmbAnim.SelectedIndexChanged += (s, e) => CmbAnim_SelectedIndexChanged();
            nudStun.ValueChanged += (s, e) => NudStun_ValueChanged();
            chkProjectile.CheckedChanged += (s, e) => ChkProjectile_CheckedChanged();
            cmbProjectile.SelectedIndexChanged += (s, e) => CmbProjectile_SelectedIndexChanged();
            chkKnockBack.CheckedChanged += (s, e) => ChkKnockBack_CheckedChanged();
            cmbKnockBackTiles.SelectedIndexChanged += (s, e) => CmbKnockBackTiles_SelectedIndexChanged();
            cmbChainOnHit.SelectedIndexChanged += (s, e) => CmbChainOnHit_SelectedIndexChanged();
            cmbCommonEventType.SelectedIndexChanged += (s, e) => { Data.Skill[GameState.EditorIndex].CommonEventType = (byte)Math.Max(0, cmbCommonEventType.SelectedIndex); };
            nudCommonEventData1.ValueChanged += (s, e) => { Data.Skill[GameState.EditorIndex].CommonEventData1 = (int)Math.Round(nudCommonEventData1.Value); };
            nudCommonEventData2.ValueChanged += (s, e) => { Data.Skill[GameState.EditorIndex].CommonEventData2 = (int)Math.Round(nudCommonEventData2.Value); };
            btnSave.Click += (s, e) => BtnSave_Click();
            btnDelete.Click += (s, e) => BtnDelete_Click();
            btnCancel.Click += (s, e) => BtnCancel_Click();
            btnCopy.Click += (s, e) => CopyOrPasteSkill();
            btnLearn.Click += (s, e) => BtnLearn_Click();

            picSprite.Paint += (s, e) => DrawIcon(e.Graphics, (int)Math.Round(nudIcon.Value));

            // Layouts
            var listLayout = new DynamicLayout { Spacing = new Size(5,5) };
            listLayout.AddRow(new Label { Text = "Skills", Font = SystemFonts.Bold(12) });
            listLayout.Add(lstIndex, yscale: true);

            var general = new DynamicLayout { Spacing = new Size(4,4) };
            // Make layout taller (more rows) and less wide by limiting items per row
            general.AddRow("Name:", txtName, "Type:", cmbType);
            general.AddRow("Access:", cmbAccessReq, "Job:", cmbJob);
            general.AddRow("MP:", nudMp, "Level:", nudLevel);
            general.AddRow("Cast:", nudCast, "Cooldown:", nudCool);
            general.AddRow("Icon:", nudIcon, picSprite);
            general.AddRow("Map:", nudMap, "Direction:", cmbDir);
            general.AddRow("X:", nudX, "Y:", nudY);
            general.AddRow("Vital:", nudVital, "Duration:", nudDuration);
            general.AddRow("Interval:", nudInterval, "Range:", nudRange);
            general.AddRow(chkAoE, "AoE Size:", nudAoE);
            general.AddRow("Cast Animation:", cmbAnimCast, "Skill Anim:", cmbAnim);
            general.AddRow("Stun:", nudStun);
            general.AddRow(chkProjectile, "Projectile:", cmbProjectile);
            // Multi-direction casting: 8 direction checkboxes
            var dirPanel1 = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 6 };
            var dirPanel2 = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 6 };
            var dirNames = Enum.GetNames(typeof(Direction));
            var dirCount = System.Enum.GetValues<Direction>().Length;
            for (int i = 0; i < dirCount; i++)
            {
                var cb = new CheckBox { Text = dirNames[i] };
                int bit = i;
                cb.CheckedChanged += (s,e)=>
                {
                    int mask = Data.Skill[GameState.EditorIndex].MultiDirMask;
                    if (cb.Checked == true) mask |= (1 << bit); else mask &= ~(1 << bit);
                    Data.Skill[GameState.EditorIndex].MultiDirMask = mask;
                };
                chkMultiDirs[i] = cb;
                (i < 4 ? dirPanel1.Items : dirPanel2.Items).Add(cb);
            }
            general.AddRow(new Label { Text = "Multi-cast Directions:" });
            general.AddRow(dirPanel1);
            general.AddRow(dirPanel2);
            general.AddRow(chkKnockBack, "Distance:", cmbKnockBackTiles);
            general.AddRow(new Label{ Text = "Chain on Hit:" }, cmbChainOnHit);
            // Common Event (like items)
            var ceRow1 = new DynamicLayout { Spacing = new Size(4,4) };
            ceRow1.AddRow(new Label{ Text = "Event Type:" }, cmbCommonEventType);
            ceRow1.AddRow(new Label{ Text = "Data 1:" }, nudCommonEventData1, new Label{ Text = "Data 2:" }, nudCommonEventData2);
            general.AddRow(new Label{ Text = "Common Event:" });
            general.AddRow(ceRow1);

            var buttons = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { btnSave, btnDelete, btnCopy, btnCancel, btnLearn } }; // order enforced

            var rightLayout = new DynamicLayout { Spacing = new Size(6,6) };
            rightLayout.Add(general);
            // buttons moved to right layout bottom (main control view)
            rightLayout.Add(buttons);

            var scroll = new Scrollable
            {
                Content = rightLayout,
                ExpandContentWidth = true,
                ExpandContentHeight = false
            };
            Content = new TableLayout
            {
                Spacing = new Size(10,10),
                Rows = { new TableRow(new TableCell(listLayout, true), new TableCell(scroll, true)) }
            };
        }

        private void Editor_Skill_Load()
        {
            _suppressIndexChanged = true;
            try
            {
                lstIndex.Items.Clear();
                for (int i = 0; i < Constant.MaxSkills; i++)
                    lstIndex.Items.Add($"{i + 1}: {Strings.Trim(Data.Skill[i].Name)}");
                lstIndex.SelectedIndex = GameState.EditorIndex >= 0 ? GameState.EditorIndex : 0;

                cmbAnimCast.Items.Clear();
                cmbAnim.Items.Clear();
                for (int i = 0; i < Constant.MaxAnimations; i++)
                {
                    cmbAnimCast.Items.Add($"{i + 1}: {Data.Animation[i].Name}");
                    cmbAnim.Items.Add($"{i + 1}: {Data.Animation[i].Name}");
                }

                cmbProjectile.Items.Clear();
                for (int i = 0; i < Constant.MaxProjectiles; i++)
                {
                    cmbProjectile.Items.Add($"{i}: {Data.Projectile[i].Name}");
                }

                cmbChainOnHit.Items.Clear();
                cmbChainOnHit.Items.Add("None");
                for (int i = 0; i < Constant.MaxSkills; i++)
                {
                    var nm = Strings.Trim(Data.Skill[i].Name);
                    cmbChainOnHit.Items.Add($"{i}: {nm}");
                }

                cmbCommonEventType.Items.Clear();
                foreach (var name in Enum.GetNames(typeof(CommonEventTrigger)))
                {
                    // display names already human-friendly in enum
                    cmbCommonEventType.Items.Add(name);
                }

                cmbJob.Items.Clear();
                for (int i = 0; i < Constant.MaxJobs; i++)
                    cmbJob.Items.Add($"{i + 1}: {Data.Job[i].Name.Trim()}");
            }
            finally { _suppressIndexChanged = false; }

            Editors.SkillEditorInit();
        }

        private void LstIndex_Click() => Editors.SkillEditorInit();
        private void BtnSave_Click() { Editors.SkillEditorOK(); Close(); }
        private void BtnCancel_Click() { Editors.SkillEditorCancel(); Close(); }
        private void BtnDelete_Click()
        {
            int tmpindex = lstIndex.SelectedIndex;
            Database.ClearSkill(GameState.EditorIndex);
            _suppressIndexChanged = true;
            try
            {
                lstIndex.Items.RemoveAt(GameState.EditorIndex);
                lstIndex.Items.Insert(GameState.EditorIndex, new ListItem { Text = $"{GameState.EditorIndex + 1}: {Data.Skill[GameState.EditorIndex].Name}" });
                lstIndex.SelectedIndex = tmpindex;
            }
            finally { _suppressIndexChanged = false; }
            Editors.SkillEditorInit();
        }
        private void BtnLearn_Click() => Sender.SendLearnSkill(GameState.EditorIndex);

        private void TxtName_TextChanged()
        {
            if (lstIndex.SelectedIndex < 0) return;
            int tmpindex = lstIndex.SelectedIndex;
            Data.Skill[GameState.EditorIndex].Name = Strings.Trim(txtName.Text);
            _suppressIndexChanged = true;
            try
            {
                lstIndex.Items.RemoveAt(GameState.EditorIndex);
                lstIndex.Items.Insert(GameState.EditorIndex, new ListItem { Text = $"{GameState.EditorIndex + 1}: {Data.Skill[GameState.EditorIndex].Name}" });
                lstIndex.SelectedIndex = tmpindex;
            }
            finally { _suppressIndexChanged = false; }
        }
        private void CmbType_SelectedIndexChanged() => Data.Skill[GameState.EditorIndex].Type = (byte)cmbType.SelectedIndex;
        private void NudMp_ValueChanged() => Data.Skill[GameState.EditorIndex].MpCost = (int)Math.Round(nudMp.Value);
        private void NudLevel_ValueChanged() => Data.Skill[GameState.EditorIndex].LevelReq = (int)Math.Round(nudLevel.Value);
        private void CmbAccessReq_SelectedIndexChanged() => Data.Skill[GameState.EditorIndex].AccessReq = cmbAccessReq.SelectedIndex;
        private void CmbJob_SelectedIndexChanged() => Data.Skill[GameState.EditorIndex].JobReq = cmbJob.SelectedIndex;
        private void NudCast_ValueChanged() => Data.Skill[GameState.EditorIndex].CastTime = (int)Math.Round(nudCast.Value);
        private void NudCool_ValueChanged() => Data.Skill[GameState.EditorIndex].CdTime = (int)Math.Round(nudCool.Value);
        private void NudIcon_ValueChanged() { Data.Skill[GameState.EditorIndex].Icon = (int)Math.Round(nudIcon.Value); picSprite.Invalidate(); }
        private void NudMap_ValueChanged() => Data.Skill[GameState.EditorIndex].Map = (int)Math.Round(nudMap.Value);
        private void CmbDir_SelectedIndexChanged() => Data.Skill[GameState.EditorIndex].Dir = (byte)cmbDir.SelectedIndex;
        private void NudX_ValueChanged() => Data.Skill[GameState.EditorIndex].X = (int)Math.Round(nudX.Value);
        private void NudY_ValueChanged() => Data.Skill[GameState.EditorIndex].Y = (int)Math.Round(nudY.Value);
        private void NudVital_ValueChanged() => Data.Skill[GameState.EditorIndex].Vital = (int)Math.Round(nudVital.Value);
        private void NudDuration_ValueChanged() => Data.Skill[GameState.EditorIndex].Duration = (int)Math.Round(nudDuration.Value);
        private void NudInterval_ValueChanged() => Data.Skill[GameState.EditorIndex].Interval = (int)Math.Round(nudInterval.Value);
        private void NudRange_ValueChanged() => Data.Skill[GameState.EditorIndex].Range = (int)Math.Round(nudRange.Value);
        private void ChkAoE_CheckedChanged() => Data.Skill[GameState.EditorIndex].IsAoE = chkAoE.Checked == true;
        private void NudAoE_ValueChanged() => Data.Skill[GameState.EditorIndex].AoE = (int)Math.Round(nudAoE.Value);
        private void CmbAnimCast_SelectedIndexChanged() => Data.Skill[GameState.EditorIndex].CastAnim = cmbAnimCast.SelectedIndex;
        private void CmbAnim_SelectedIndexChanged() => Data.Skill[GameState.EditorIndex].SkillAnim = cmbAnim.SelectedIndex;
        private void NudStun_ValueChanged() => Data.Skill[GameState.EditorIndex].StunDuration = (int)Math.Round(nudStun.Value);
        private void ChkProjectile_CheckedChanged() => Data.Skill[GameState.EditorIndex].IsProjectile = chkProjectile.Checked == true ? 1 : 0;
        private void CmbProjectile_SelectedIndexChanged() => Data.Skill[GameState.EditorIndex].Projectile = cmbProjectile.SelectedIndex;
        private void ChkKnockBack_CheckedChanged() => Data.Skill[GameState.EditorIndex].KnockBack = (byte)(chkKnockBack.Checked == true ? 1 : 0);
        private void CmbKnockBackTiles_SelectedIndexChanged() => Data.Skill[GameState.EditorIndex].KnockBackTiles = (byte)cmbKnockBackTiles.SelectedIndex;
        private void CmbChainOnHit_SelectedIndexChanged()
        {
            var idx = cmbChainOnHit.SelectedIndex;
            Data.Skill[GameState.EditorIndex].ChainOnHitSkillId = idx <= 0 ? -1 : idx - 1;
        }
        public void SyncMultiDirMask()
        {
            int mask = Data.Skill[GameState.EditorIndex].MultiDirMask;
            var dirCount = System.Enum.GetValues<Direction>().Length;
            for (int i = 0; i < dirCount; i++)
            {
                chkMultiDirs[i].Checked = (mask & (1 << i)) != 0;
            }
        }

        private void DrawIcon(Graphics g, int iconNum)
        {
            if (iconNum < 1 || iconNum > GameState.NumSkills)
            {
                g.Clear(Colors.Transparent);
                return;
            }
            var path = System.IO.Path.Combine(DataPath.Skills, iconNum + GameState.GfxExt);
            if (!File.Exists(path)) { g.Clear(Colors.Transparent); return; }
            try
            {
                using (var bmp = new Bitmap(path))
                {
                    // Assume 2 icons side by side
                    int fw = bmp.Width / 2;
                    int fh = bmp.Height;
                    picSprite.Size = new Size(fw * 2, fh);
                    // Draw both icons
                    g.DrawImage(bmp, new RectangleF(0,0,fw,fh), new Rectangle(0,0,fw,fh));
                    g.DrawImage(bmp, new RectangleF(fw,0,fw,fh), new Rectangle(fw,0,fw,fh));
                }
            }
            catch { g.Clear(Colors.Transparent); }
        }

        public void DrawIcon() => picSprite.Invalidate();

        private void CopyOrPasteSkill()
        {
            int src = GameState.EditorIndex;
            if (!_hasClipboardSkill)
            {
                if (src < 0 || src >= Constant.MaxSkills) return;
                _clipboardSkill = Data.Skill[src]; // struct copy (no arrays)
                _hasClipboardSkill = true;
                btnCopy.Text = "Paste";
                return;
            }

            int def = GameState.EditorIndex + 1;
            var oneBased = Editors.PromptIndex(this, "Paste Skill", $"Paste skill into index (1..{Constant.MaxSkills}):", 1, Constant.MaxSkills, def);
            if (oneBased == null) return;
            int dst = oneBased.Value - 1;
            var n = _clipboardSkill; // struct copy
            Data.Skill[dst] = n;
            GameState.SkillChanged[dst] = true;

            if (lstIndex != null && dst >= 0 && dst < lstIndex.Items.Count)
            {
                _suppressIndexChanged = true;
                try
                {
                    lstIndex.Items.RemoveAt(dst);
                    lstIndex.Items.Insert(dst, new ListItem { Text = $"{dst + 1}: {Data.Skill[dst].Name}" });
                    lstIndex.SelectedIndex = dst;
                }
                finally { _suppressIndexChanged = false; }
            }
            GameState.EditorIndex = dst;
            Editors.SkillEditorInit();
        }
    }
}