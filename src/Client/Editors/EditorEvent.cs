using Core;
using System;
using System.IO;
using Core.Globals;
using Core.Configurations; // for SettingsManager.Instance
using Eto.Forms;
using Eto.Drawing;
using static Core.Globals.Type;
using EventCommand = Core.Globals.EventCommand;
using Type = Core.Globals.Type;
using Client.Net;

namespace Client
{
    public partial class EditorEvent : Form
    {
        // Singleton access for legacy usage
        private static EditorEvent? _instance;
        public static EditorEvent Instance => _instance ??= new EditorEvent();
        private int tmpGraphicIndex;
        private byte tmpGraphicType;
        // Guard to avoid feedback loops when syncing Graphic/Index controls
        private bool _syncingGraphic;
        // Guard to avoid firing change handlers during page UI programmatic updates
        private bool _syncingPageUI;
        public bool IsSyncingPageUI => _syncingPageUI;
        public void BeginPageSync() { _syncingPageUI = true; }
        public void EndPageSync() { _syncingPageUI = false; }

        public ComboBox cmbSwitch = new ComboBox();
        public ComboBox cmbVariable = new ComboBox();
        public ComboBox cmbChangeItemIndex = new ComboBox();
        public ComboBox cmbSetSelfSwitch = new ComboBox();
        public ComboBox cmbSetSelfSwitchTo = new ComboBox();
        public TextBox txtGoToLabel = new TextBox();
        public NumericStepper nudChangeItemsAmount = new NumericStepper();
        public CheckBox optChangeItemAdd = new CheckBox { Text = "Add" };
        public CheckBox optChangeItemSet = new CheckBox { Text = "Set" };
        public CheckBox optChangeItemRemove = new CheckBox { Text = "Remove" };
        public RadioButton optChangeSkillsAdd = new RadioButton { Text = "Add Skill" };
        public RadioButton optChangeSkillsRemove = new RadioButton { Text = "Remove Skill" };
        public RadioButton optCondition0 = new RadioButton { Text = "Player Variable" };
        public RadioButton optCondition1 = new RadioButton { Text = "Player Switch" };
        public RadioButton optCondition2 = new RadioButton { Text = "Has Item" };
        public RadioButton optCondition3 = new RadioButton { Text = "Self Switch" };
        public RadioButton optCondition4 = new RadioButton { Text = "Class Is" };
        public RadioButton optCondition5 = new RadioButton { Text = "Learnt Skill" };
        public RadioButton optCondition6 = new RadioButton { Text = "Level" };
        public RadioButton optCondition8 = new RadioButton { Text = "Gender" };
        public RadioButton optCondition9 = new RadioButton { Text = "Time" };
        // Additional controls referenced later
        public RadioButton optChangeSexMale = new RadioButton { Text = "Male" };
        public RadioButton optChangeSexFemale = new RadioButton { Text = "Female" };
        public ComboBox cmbSetPK = new ComboBox();
        public NumericStepper nudGiveExp = new NumericStepper();
        public NumericStepper nudWPX = new NumericStepper();
        public NumericStepper nudWPY = new NumericStepper();
        public ComboBox cmbWarpPlayerDir = new ComboBox();
        public ComboBox cmbMoveWait = new ComboBox();
        // Add Text scope options
        public RadioButton optAddText_Map = new RadioButton { Text = "Map" };
        public RadioButton optAddText_Global = new RadioButton { Text = "Global" };

        // Keep a handle to the right-side scroll view so we can bring frames into view
        private Scrollable? _rightScroll;

        private void ScrollRightPaneTop()
        {
            if (_rightScroll != null)
            {
                // Reset scroll to top-left; use Eto.Drawing namespace explicitly to avoid ambiguity
                _rightScroll.ScrollPosition = new Eto.Drawing.Point(0, 0);
            }
        }

        // Legacy sizing hook retained for compatibility with existing calls
        private void SyncOverlayChildSizes()
        {
            try { }
            catch { }
        }
        // Animation play / targeting controls
        public ComboBox cmbPlayAnimEvent = new ComboBox();
        public ComboBox cmbAnimTargetType = new ComboBox();
        public NumericStepper nudPlayAnimTileX = new NumericStepper();
        public NumericStepper nudPlayAnimTileY = new NumericStepper();
        public Label lblPlayAnimX = new Label();
        public Label lblPlayAnimY = new Label();
        // Fog / weather / tint controls referenced later
        public NumericStepper nudFogData1 = new NumericStepper();
        public NumericStepper nudFogData2 = new NumericStepper();
        public ComboBox CmbWeather = new ComboBox();
        public NumericStepper nudWeatherIntensity = new NumericStepper();
        public NumericStepper nudMapTintData0 = new NumericStepper();
        public NumericStepper nudMapTintData1 = new NumericStepper();
        public NumericStepper nudMapTintData2 = new NumericStepper();
        public NumericStepper nudMapTintData3 = new NumericStepper();
        public NumericStepper nudWaitAmount = new NumericStepper();
        public ComboBox cmbSetAccess = new ComboBox();
        public Panel fraOpenShop = new Panel();
        public NumericStepper nudPicOffsetX = new NumericStepper();
        public NumericStepper nudPicOffsetY = new NumericStepper();
        public ComboBox cmbMoveType = new ComboBox();
        public ComboBox cmbMoveSpeed = new ComboBox();
        public ComboBox cmbMoveFreq = new ComboBox();
        public ComboBox cmbPositioning = new ComboBox();
        public ComboBox cmbTrigger = new ComboBox();
        public Panel pnlVariableSwitches = new Panel();
        public ListBox lstSwitches = new ListBox();
        public ListBox lstVariables = new ListBox();
        public Panel FraRenaming = new Panel();
        public Panel fraLabeling = new Panel();
        public TextBox txtRename = new TextBox();
        public Label lblEditing = new Label();
        public ComboBox cmbCondition_PlayerVarIndex = new ComboBox();
        public ComboBox cmbCondition_PlayerVarCompare = new ComboBox();
        public ComboBox cmbPlayerSwitchSet = new ComboBox();
        public ComboBox cmbCondition_PlayerSwitch = new ComboBox();
        public ComboBox cmbCondtion_PlayerSwitchCondition = new ComboBox();
        public ComboBox cmbCondition_HasItem = new ComboBox();
        public ComboBox cmbCondition_JobIs = new ComboBox();
        public ComboBox cmbCondition_LearntSkill = new ComboBox();
        public ComboBox cmbCondition_LevelCompare = new ComboBox();
        public ComboBox cmbCondition_SelfSwitch = new ComboBox();
        public ComboBox cmbCondition_SelfSwitchCondition = new ComboBox();
        public ComboBox cmbCondition_Gender = new ComboBox();
        public ComboBox cmbCondition_Time = new ComboBox();
        public ComboBox cmbSwitchSet = new ComboBox();
        public Label txtLabelName = new Label();
        public NumericStepper nudChangeLevel = new NumericStepper();
        public ComboBox cmbChangeSkills = new ComboBox();
        public ComboBox cmbChangeJob = new ComboBox();
        public NumericStepper nudChangeSprite = new NumericStepper();
        public ComboBox cmbPlayAnim = new ComboBox();
        public ComboBox cmbPlayBGM = new ComboBox();
        public ComboBox cmbPlaySound = new ComboBox();
        public ComboBox cmbOpenShop = new ComboBox();
        public ListBox cmbSpawnNpc = new ListBox { Width = 200 };
        public NumericStepper nudFogData0 = new NumericStepper();
        public NumericStepper nudWPMap = new NumericStepper();
        public Panel fraDialogue = new Panel();
        public Panel fraMoveRoute = new Panel();
        public ComboBox cmbEvent = new ComboBox();
        public TabControl tabPages = new TabControl();
        public ComboBox cmbHasItem = new ComboBox();
        public ComboBox cmbPlayerVar = new ComboBox();
        public ComboBox cmbPlayerSwitch = new ComboBox();
        public ComboBox cmbSelfSwitch = new ComboBox();
        public Button btnDeletePage = new Button { Text = "Delete Page" };
        public Button btnPastePage = new Button { Text = "Paste Page" };
        public Button btnNewPage = new Button { Text = "New Page" };
        public Button btnCopyPage = new Button { Text = "Copy Page" };
        public NumericStepper nudShowPicture = new NumericStepper();
        public ComboBox cmbPicLoc = new ComboBox();
        public TextBox txtName = new TextBox { Width = 200 };
        public ImageView picGraphicSel = new ImageView();
        public ImageView picGraphic = new ImageView();
        public Panel fraGraphic = new Panel();
        public ComboBox cmbGraphic = new ComboBox();
        public NumericStepper nudGraphic = new NumericStepper();
        // Host panel for the full editor content inside the selected tab
        private Panel editorHost = new Panel();
        // Keep a reference to the main splitter so we can enforce sizes and adjust position
            private Splitter? mainSplit;
        // Host for active command frames
        private Panel frameHost = new Panel();
        private StackLayout frameDeck = new StackLayout { Orientation = Orientation.Vertical, Spacing = 6 };
        // Pages toolbar (New/Copy/Paste/Delete) lives inside the active tab so the tab fills the entire editor
        private StackLayout pagesBar = new StackLayout();
        // Single container that wraps pagesBar + editorHost and is moved between selected tabs
        private Panel tabContentHost = new Panel();
        private TabPage? hostedTab;
        // Keep a handle to the right-side container for invalidation
        private StackLayout? _rightStack;
        // Right side uses a simple vertical stack: frameHost (top) and fraCommands (bottom)

        // Additional controls referenced in logic (declare as needed)
        public ListBox lstCommands = new ListBox { Height = 200 };
        public Button btnAddCommand = new Button { Text = "Add" };
        public Button btnEditCommand = new Button { Text = "Edit" };
        public Button btnDeleteComand = new Button { Text = "Delete" };
        public Button btnClearCommand = new Button { Text = "Clear" };
        public TreeGridView tvCommands = new TreeGridView { Height = 300 };

        // Numerous frame panels placeholders (keep as Panel)
        public Panel fraShowText = new Panel();
        public Panel fraShowChoices = new Panel();
        public Panel fraAddText = new Panel();
        public Panel fraShowChatBubble = new Panel();
        // Command list/palette container – stays visible under frames
        // Command area; override Visible to prevent hiding
        public sealed class NonHideablePanel : Panel
        {
            // Allow true/false so we can hide the palette when a frame is active
            public new bool Visible
            {
                get => base.Visible;
                set { base.Visible = value; }
            }
        }
        public NonHideablePanel fraCommands = new NonHideablePanel();
        public Panel fraPlayerVariable = new Panel();
        public Panel fraPlayerSwitch = new Panel();
        public Panel fraSetSelfSwitch = new Panel();
        public Panel fraConditionalBranch = new Panel();
        public Panel fraCreateLabel = new Panel();
        public Panel fraGoToLabel = new Panel();
        public Panel fraChangeItems = new Panel();
        public Panel fraChangeLevel = new Panel();
        public Panel fraChangeSkills = new Panel();
        public Panel fraChangeJob = new Panel();
        public Panel fraChangeSprite = new Panel();
        public Panel fraChangeGender = new Panel();
        public Panel fraChangePK = new Panel();
        public Panel fraGiveExp = new Panel();
        public Panel fraPlayerWarp = new Panel();
        public Panel fraMoveRouteWait = new Panel();
        public Panel fraSpawnNpc = new Panel();
        public Panel fraPlayAnimation = new Panel();
        public Panel fraSetFog = new Panel();
        public Panel fraSetWeather = new Panel();
        public Panel fraMapTint = new Panel();
        public Panel fraPlayBGM = new Panel();
        public Panel fraPlaySound = new Panel();
        public Panel fraSetWait = new Panel();
        public Panel fraSetAccess = new Panel();
        public Panel fraShowPic = new Panel();
        public ImageView picShowPic = new ImageView();

        // Text/entry controls referenced in logic
        public TextArea txtShowText = new TextArea();
        public TextBox txtChoicePrompt = new TextBox();
        public TextBox txtChoices1 = new TextBox();
        public TextBox txtChoices2 = new TextBox();
        public TextBox txtChoices3 = new TextBox();
        public TextBox txtChoices4 = new TextBox();
        public TextArea txtAddText_Text = new TextArea();
        public TextBox txtChatbubbleText = new TextBox();
        public ComboBox cmbChatBubbleTargetType = new ComboBox();
        public ComboBox cmbChatBubbleTarget = new ComboBox();
        public NumericStepper nudVariableData0 = new NumericStepper();
        public NumericStepper nudVariableData1 = new NumericStepper();
        public NumericStepper nudVariableData2 = new NumericStepper();
        public NumericStepper nudVariableData3 = new NumericStepper();
        public NumericStepper nudVariableData4 = new NumericStepper();
        public CheckBox optAddText_Player = new CheckBox { Text = "Player" };
        public CheckBox optVariableAction0 = new CheckBox { Text = "Set" };
        public CheckBox optVariableAction1 = new CheckBox { Text = "Add" };
        public CheckBox optVariableAction2 = new CheckBox { Text = "Sub" };
        public CheckBox optVariableAction3 = new CheckBox { Text = "Random" };

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Event.InEvent = false;
            if (ReferenceEquals(_instance, this)) _instance = null;
        }

        // Condition numeric controls
        public NumericStepper nudCondition_PlayerVarCondition = new NumericStepper();
        public NumericStepper nudCondition_HasItem = new NumericStepper();
        public NumericStepper nudCondition_LevelAmount = new NumericStepper();

        // Additional references used later
        public ComboBox cmbVariableDataType = new ComboBox();
        public ComboBox cmbPlayerVarCompare = new ComboBox();
        public NumericStepper nudPlayerVariable = new NumericStepper();
        public CheckBox chkPlayerVar = new CheckBox { Text = "Player Variable" };
        public CheckBox chkPlayerSwitch = new CheckBox { Text = "Player Switch" };
        public ComboBox cmbPlayerSwitchCompare = new ComboBox();
        public CheckBox chkHasItem = new CheckBox { Text = "Has Item" };
        public CheckBox chkSelfSwitch = new CheckBox { Text = "Self Switch" };
        public ComboBox cmbSelfSwitchCompare = new ComboBox();
        public CheckBox chkWalkAnim = new CheckBox { Text = "Idle Animation" };
        public CheckBox chkDirFix = new CheckBox { Text = "Direction Fix" };
        public CheckBox chkWalkThrough = new CheckBox { Text = "Walk Through" };
        public CheckBox chkShowName = new CheckBox { Text = "Show Name" };

        // Move route related
        public ListBox lstMoveRoute = new ListBox();
        public ListBox lstvwMoveRoute = new ListBox();
        public CheckBox chkIgnoreMove = new CheckBox { Text = "Ignore" };
        public CheckBox chkRepeatRoute = new CheckBox { Text = "Repeat" };
        public Button btnMoveRoute = new Button { Text = "Move Route" };
        public Button btnMoveRouteOk = new Button { Text = "Route OK" };
        public Button btnMoveRouteCancel = new Button { Text = "Cancel" };

        // Graphics selection
        public Button btnOK = new Button { Text = "OK" };
        public Button btnCancel = new Button { Text = "Cancel" };

        public CheckBox chkGlobal = new CheckBox { Text = "Global" };

        public EditorEvent()
        {
            _instance = this;
            Title = "Event Editor";
            // Make the editor more compact by default
            ClientSize = new Size(1200, 680);
            InitializeComponent();
            Event.EventEditorInit();
        }

        private void InitializeComponent()
        {
            // Ensure Load is subscribed first
            Load += Editor_Events_Load; // hook existing load logic
            // LEFT: RPG Maker-like page details and conditions
            // Give key condition controls reasonable widths so they are visible
            cmbPlayerVar.Width = 220;
            cmbPlayerVarCompare.Width = 100;
            nudPlayerVariable.Width = 100;
            cmbPlayerSwitch.Width = 220;
            cmbPlayerSwitchCompare.Width = 100;
            cmbHasItem.Width = 220;
            nudCondition_HasItem.Width = 100;
            cmbSelfSwitch.Width = 80;
            cmbSelfSwitchCompare.Width = 100;
            var conditions = new GroupBox
            {
                Content = new TableLayout
                {
                    Spacing = new Size(6, 4),
                    Rows =
                    {
                        // Player Variable (two rows so Compare/Value aren't clipped)
                        new TableRow(
                            chkPlayerVar,
                            new Label{ Text = "Variable:"},
                            new TableCell(cmbPlayerVar, true)
                        ),
                        new TableRow(
                            null,
                            new Label{ Text = "Compare"},
                            cmbPlayerVarCompare,
                            new Label{ Text = "Value"},
                            nudPlayerVariable
                        ),

                        // Player Switch (two rows)
                        new TableRow(
                            chkPlayerSwitch,
                            new Label{ Text = "Switch:"},
                            new TableCell(cmbPlayerSwitch, true)
                        ),
                        new TableRow(
                            null,
                            new Label{ Text = "Compare"},
                            cmbPlayerSwitchCompare
                        ),

                        // Has Item (two rows)
                        new TableRow(
                            chkHasItem,
                            new Label{ Text = "Item:"},
                            new TableCell(cmbHasItem, true)
                        ),
                        new TableRow(
                            null,
                            new Label{ Text = "Amount"},
                            nudCondition_HasItem
                        ),

                        // Self Switch (two rows)
                        new TableRow(
                            chkSelfSwitch,
                            new Label{ Text = "Self:"},
                            cmbSelfSwitch
                        ),
                        new TableRow(
                            null,
                            new Label{ Text = "Compare"},
                            cmbSelfSwitchCompare
                        )
                    }
                }
            };

            // Smaller preview to keep the UI compact
            picGraphic.Size = new Size(64, 64);

            var pageSettingsLeft = new TableLayout
            {
                Spacing = new Size(4, 2),
                Rows =
                {
                    new TableRow(new Label{ Text = "Trigger"}, cmbTrigger, new Label{ Text = "Positioning"}, cmbPositioning),
                    new TableRow(new Label{ Text = "Move Type"}, cmbMoveType, new Label{ Text = "Move Speed"}, cmbMoveSpeed),
                    new TableRow(new Label{ Text = "Move Freq"}, cmbMoveFreq, new Label{ Text = "Move Wait"}, cmbMoveWait),
                    // Graphic selector
                    new TableRow(new Label{ Text = "Graphic"}, cmbGraphic, new Label{ Text = "Index"}, nudGraphic)
                }
            };
            // Right-side column: preview, action buttons, and checkboxes stacked to utilize height
            var previewPanel = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 8,
                Items =
                {
                    new Label{ Text = "Preview" },
                    picGraphic,
                    new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnMoveRoute } },
                    new StackLayout{ Orientation = Orientation.Vertical, Spacing = 6, Items =
                        {
                            new Label{ Text = "Options" },
                            new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 10, Items = { chkWalkAnim, chkWalkThrough } },
                            new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 10, Items = { chkDirFix, chkShowName } }
                        }
                    }
                }
            };

            var pageSettings = new GroupBox
            {
                Text = "Page Settings",
                Content = new TableLayout
                {
                    Spacing = new Size(6, 6),
                    Rows =
                    {
                        new TableRow(new TableCell(pageSettingsLeft, true)),
                        new TableRow(new TableCell(previewPanel, true))
                    }
                }
            };

            // Build Set Graphic frame UI (used by both Page Settings and Move Route -> Set Graphic)
            if (fraGraphic.Content == null)
            {
                // full-sheet preview; make it scrollable in case of large assets
                var graphicScroll = new Scrollable
                {
                    Content = picGraphicSel,
                    ExpandContentWidth = false,
                    ExpandContentHeight = false,
                    Size = new Size(360, 300)
                };
                var graphicTip = new Label { Text = "Tip: Use the Graphic and Index in Page Settings, then click a frame on the sheet to select (characters are 4x4 frames)." };

                fraGraphic.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items =
                    {
                        new Label{ Text = "Set Graphic", Font = SystemFonts.Bold(12) },
                        graphicTip,
                        graphicScroll
                    }
                };
            }

            // Button to open switches/variables manager
            var btnOpenLabeling = new Button { Text = "Switches && Variables…" };
            btnOpenLabeling.Click += BtnLabeling_Click;

            // Page controls row
            pagesBar = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Items =
                {
                    new Label{ Text = "Pages" },
                    btnNewPage,
                    btnCopyPage,
                    btnPastePage,
                    btnDeletePage
                }
            };

            var leftPane = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 6,
                Items =
                {
                    new StackLayout { Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "Name"}, txtName, chkGlobal } },
                    conditions,
                    pageSettings,
                    btnOpenLabeling
                }
            };
            // React when user switches tabs (pages)
            tabPages.SelectedIndexChanged += TabPages_Click;
            // Wire condition toggles
            chkPlayerVar.CheckedChanged += ChkPlayerVar_CheckedChanged;
            chkPlayerSwitch.CheckedChanged += ChkPlayerSwitch_CheckedChanged;
            chkHasItem.CheckedChanged += ChkHasItem_CheckedChanged;
            chkSelfSwitch.CheckedChanged += ChkSelfSwitch_CheckedChanged;
            // Wire condition editors
            cmbPlayerVar.SelectedIndexChanged += CmbPlayerVar_SelectedIndexChanged;
            cmbPlayerVarCompare.SelectedIndexChanged += CmbPlayervarCompare_SelectedIndexChanged;
            nudPlayerVariable.ValueChanged += NudPlayerVariable_ValueChanged;
            cmbPlayerSwitch.SelectedIndexChanged += CmbPlayerSwitch_SelectedIndexChanged;
            cmbPlayerSwitchCompare.SelectedIndexChanged += CmbPlayerSwitchCompare_SelectedIndexChanged;
            cmbHasItem.SelectedIndexChanged += CmbHasItem_SelectedIndexChanged;
            cmbSelfSwitch.SelectedIndexChanged += CmbSelfSwitch_SelectedIndexChanged;
            cmbSelfSwitchCompare.SelectedIndexChanged += CmbSelfSwitchCompare_SelectedIndexChanged;
            // Wire page buttons
            btnNewPage.Click += BtnNewPage_Click;
            btnCopyPage.Click += BtnCopyPage_Click;
            btnPastePage.Click += BtnPastePage_Click;
            btnDeletePage.Click += BtnDeletePage_Click;
            // Wire page settings controls
            cmbTrigger.SelectedIndexChanged += CmbTrigger_SelectedIndexChanged;
            cmbPositioning.SelectedIndexChanged += CmbPositioning_SelectedIndexChanged;
            cmbMoveType.SelectedIndexChanged += CmbMoveType_SelectedIndexChanged;
            cmbMoveSpeed.SelectedIndexChanged += CmbMoveSpeed_SelectedIndexChanged;
            cmbMoveFreq.SelectedIndexChanged += CmbMoveFreq_SelectedIndexChanged;
            chkWalkAnim.CheckedChanged += ChkWalkAnim_CheckedChanged;
            chkWalkThrough.CheckedChanged += ChkWalkThrough_CheckedChanged;
            chkDirFix.CheckedChanged += ChkDirFix_CheckedChanged;
            chkShowName.CheckedChanged += ChkShowName_CheckedChanged;
            // Event-level fields
            txtName.TextChanged += TxtName_TextChanged;
            chkGlobal.CheckedChanged += ChkGlobal_CheckedChanged;
            btnMoveRoute.Click += BtnMoveRoute_Click;
            // Make the graphic preview open the Set Graphic selector when clicked
            picGraphic.Cursor = Cursors.Pointer;
            picGraphic.MouseDown += PicGraphic_Click;
            picGraphicSel.MouseDown += PicGraphicSel_MouseDown;
            // Ensure the tab control has some vertical space via layout (StackLayoutItem true above)
            var leftPadded = new Panel { Padding = new Eto.Drawing.Padding(12,8,8,8), Content = leftPane };

            // RIGHT: command palette and command list + editors
            // Build the Variable/Switch management panel UI
            // Show variables/switches panel by default; command frames will appear here when selected
            pnlVariableSwitches.Visible = true;
            fraLabeling.Visible = true;
            FraRenaming.Visible = false;

            // Rename view
            var btnRenameOk = new Button { Text = "OK" };
            btnRenameOk.Click += BtnRename_Ok_Click;
            var btnRenameCancel = new Button { Text = "Cancel" };
            btnRenameCancel.Click += BtnRename_Cancel_Click;

            FraRenaming.Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 6,
                Items =
                {
                    new Label{ Text = "Rename" },
                    lblEditing,
                    txtRename,
                    new StackLayout { Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnRenameOk, btnRenameCancel } }
                }
            };

            // Labeling view
            lstSwitches.Size = new Size(220, 240);
            lstVariables.Size = new Size(220, 240);
            lstSwitches.MouseDoubleClick += LstSwitches_DoubleClick;
            lstVariables.MouseDoubleClick += LstVariables_DoubleClick;

            var btnRenameSwitch = new Button { Text = "Rename Switch…" };
            btnRenameSwitch.Click += BtnRenameSwitch_Click;
            var btnRenameVariable = new Button { Text = "Rename Variable…" };
            btnRenameVariable.Click += BtnRenameVariable_Click;
            var btnLabelOk = new Button { Text = "OK" };
            btnLabelOk.Click += BtnLabel_Ok_Click;
            var btnLabelCancel = new Button { Text = "Cancel" };
            btnLabelCancel.Click += BtnLabel_Cancel_Click;

            fraLabeling.Content = new TableLayout
            {
                Spacing = new Size(8,6),
                Rows =
                {
                    new TableRow(
                        new TableCell(new StackLayout { Orientation = Orientation.Vertical, Spacing = 4, Items = { new Label{ Text = "Switches" }, lstSwitches, btnRenameSwitch } }, true),
                        new TableCell(new StackLayout { Orientation = Orientation.Vertical, Spacing = 4, Items = { new Label{ Text = "Variables" }, lstVariables, btnRenameVariable } }, true)
                    ),
                    new TableRow(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnLabelOk, btnLabelCancel } })
                }
            };

            pnlVariableSwitches.Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 6,
                Items = { fraLabeling, FraRenaming }
            };


            // Build the command palette tree (categories + leaf commands)
            BuildCommandPalette();


            // Auto-size command palette width to fit widest text
            try
            {
                int minWidth = 200; // fallback minimum
                int maxWidth = minWidth;
                var font = SystemFonts.Default();
                using (var bmp = new Bitmap(new Size(1, 1), PixelFormat.Format32bppRgba))
                using (var g = new Graphics(bmp))
                {
                    void MeasureNode(TreeGridItem node)
                    {
                        if (node != null && node.Values != null && node.Values.Count() > 0)
                        {
                            var val = node.Values.ElementAtOrDefault(0);
                            var text = val != null ? val.ToString() : string.Empty;
                            var size = g.MeasureString(font, text);
                            if (size.Width > maxWidth)
                                maxWidth = (int)size.Width + 48; // add padding for icon/expand
                        }
                        if (node != null && node.Children != null)
                        {
                            foreach (var child in node.Children)
                                if (child is TreeGridItem tgiChild)
                                    MeasureNode(tgiChild);
                        }
                    }
                    if (tvCommands.DataStore is TreeGridItemCollection roots)
                    {
                        foreach (var root in roots)
                            if (root is TreeGridItem tgiRoot)
                                MeasureNode(tgiRoot);
                    }
                }
                tvCommands.Width = maxWidth;
            }
            catch { tvCommands.Width = 200; }

            // Remove fixed widths from list so it expands
            try { lstCommands.Width = -1; } catch { }

            // Bottom OK/Cancel bar (aligned right)
            var spacer = new Panel();
            var bottomButtons = new TableLayout
            {
                Spacing = new Size(6,6),
                Rows =
                {
                    new TableRow(new TableCell(spacer, true), new TableCell(btnOK), new TableCell(btnCancel))
                }
            };
            // Wire OK/Cancel handlers
            btnOK.Click += BtnOK_Click;
            btnCancel.Click += BtnCancel_Click;

            // Build command area (palette + list) and host below the variables/switches panel
            var commandButtons = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Items = { btnAddCommand, btnEditCommand, btnDeleteComand, btnClearCommand }
            };

            // Use a TableLayout so the grids expand horizontally and vertically
            var commandArea = new TableLayout
            {
                Spacing = new Size(6,6),
                Rows =
                {
                    new TableRow(new Label{ Text = "Command Palette" }),
                    new TableRow(new TableCell(new Panel { Content = tvCommands, Size = new Size(-1, 300) }, true)),
                    new TableRow(new Label{ Text = "Event Commands" }),
                    new TableRow(new TableCell(lstCommands, true)),
                    new TableRow(commandButtons)
                }
            };
            fraCommands.Content = commandArea;
            fraCommands.Visible = true;

            // Host where we will display active frames (including Variables/Switches) over the command palette
            if (frameHost == null) frameHost = new Panel();
            frameHost.Visible = false;

            // Set consistent width for frame text fields
            try
            {
                const int frameTextWidth = 300;
                // TextAreas: set width while preserving a sensible height
                if (txtShowText != null)
                {
                    var h = txtShowText.Size.Height > 0 ? txtShowText.Size.Height : 160;
                    txtShowText.Size = new Size(frameTextWidth, h);
                }
                if (txtAddText_Text != null)
                {
                    var h2 = txtAddText_Text.Size.Height > 0 ? txtAddText_Text.Size.Height : 120;
                    txtAddText_Text.Size = new Size(frameTextWidth, h2);
                }
                // TextBoxes
                txtChoicePrompt.Width = frameTextWidth;
                txtChoices1.Width = frameTextWidth;
                txtChoices2.Width = frameTextWidth;
                txtChoices3.Width = frameTextWidth;
                txtChoices4.Width = frameTextWidth;
                txtChatbubbleText.Width = frameTextWidth;
                txtGoToLabel.Width = frameTextWidth;
                txtRename.Width = frameTextWidth;
            }
            catch { }

            // Build frame UIs so panels have visible content when shown
            BuildFrameUIs();

            // Build a simple vertical stack with frameHost (top) and command palette (bottom)
            frameHost.Visible = false; // start with palette visible
            fraCommands.Visible = true;
            var rightStack = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 6,
                Items = { new StackLayoutItem(frameHost, true), fraCommands }
            };
            _rightStack = rightStack;


            // Use a table so the overlay fills remaining space, and include OK/Cancel in the scrollable area
            var rightPane = new TableLayout
            {
                Spacing = new Size(6,6),
                Rows =
                {
                    new TableRow(new TableCell(rightStack, true)),
                    new TableRow(bottomButtons)
                }
            };
            // Wire up command list and buttons
            lstCommands.SelectedIndexChanged += LstCommands_SelectedIndexChanged;
            btnAddCommand.Click += BtnAddCommand_Click;
            btnEditCommand.Click += BtnEditCommand_Click;
            btnDeleteComand.Click += BtnDeleteComand_Click;
            btnClearCommand.Click += BtnClearCommand_Click;
            // Respond to selection change and double-click activation
            tvCommands.SelectionChanged += TvCommands_AfterSelect;
            tvCommands.MouseDoubleClick += (s, e) => TvCommands_AfterSelect(s, EventArgs.Empty);
            _rightScroll = new Scrollable { Content = rightPane, ExpandContentWidth = true, ExpandContentHeight = true, Padding = new Eto.Drawing.Padding(8,6,6,6) };
            HideAllFrames();

            // Build the full editor content into editorHost, which will be placed inside the selected tab
            mainSplit = new Splitter
            {
                Orientation = Orientation.Horizontal,
                Panel1 = new Scrollable { Content = leftPadded, ExpandContentWidth = true },
                Panel2 = _rightScroll,
                // Make the left pane wider by default (min 800px, ~45% of window)
                // Make the left pane reasonably wide by default (min 640px)
                Position = Math.Max(640, (int)(ClientSize.Width * 0.42))
            };
                // Prevent either pane from collapsing to 0 so left page details are always visible
            try
            {
                    // Keep a generous minimum so controls don't clip
                    // Keep minimums smaller for a compact layout
                    // Prefer keeping left width fixed while resizing
                    try { mainSplit.FixedPanel = Eto.Forms.SplitterFixedPanel.Panel1; } catch { }
            }
            catch { }
            editorHost.Content = mainSplit;

            // Wrap the full editor with the pages toolbar into a reusable host we can move between tabs
            // Use TableLayout so the Splitter fills all remaining space
            tabContentHost.Content = new TableLayout
            {
                Spacing = new Size(0,0),
                Padding = new Eto.Drawing.Padding(0),
                Rows =
                {
                    new TableRow(pagesBar),
                    new TableRow(new TableCell(editorHost, true))
                }
            };

            // Top-level: only the TabControl so the tab page fills the entire editor
            Content = new TableLayout
            {
                Spacing = new Size(0,0),
                Padding = new Eto.Drawing.Padding(6),
                Rows =
                {
                    new TableRow(new TableCell(tabPages, true))
                }
            };
            // Ensure there's at least one visible page before Load runs, so the form isn't blank
            if (tabPages.Pages.Count == 0)
            {
                tabPages.Pages.Add(new TabPage { Text = "1" });
                tabPages.SelectedIndex = 0;
            }
            // Mount editor content into the selected page now; Load() will rebuild pages later
            AttachEditorHostToSelectedTab();

            // Re-balance the splitter when the window shows/resizes to keep the left pane visible
            Shown += (s, e) =>
            {
                try
                {
                    if (mainSplit != null)
                        mainSplit.Position = Math.Max(640, (int)(ClientSize.Width * 0.42));
                    // Sync initial Move Route button state
                    SyncMoveRouteButton();
                }
                catch { }
            };
            SizeChanged += (s, e) =>
            {
                try
                {
                    if (mainSplit != null)
                        mainSplit.Position = Math.Max(640, Math.Min(ClientSize.Width - 380, mainSplit.Position));
                }
                catch { }
            };

        }

        // No extra helpers needed; visibility is controlled by existing code paths that toggle
        // specific frame panels and fraCommands. Frames are placed before fraCommands to appear on top.

        // Build all command frame UIs (headers, inputs, OK/Cancel)
        private void BuildFrameUIs()
        {
            // Show Text
            if (fraShowText.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnShowTextOk_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnShowTextCancel_Click;
                // Ensure the text area has a sensible default size for visibility
                if (txtShowText.Size.Width <= 0 || txtShowText.Size.Height <= 0)
                    txtShowText.Size = new Size(520, 160);
                fraShowText.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items =
                    {
                        new Label{ Text = "Show Text", Font = SystemFonts.Bold(12) },
                        txtShowText,
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } }
                    }
                };
            }

            // Show Choices
            if (fraShowChoices.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnShowChoicesOk_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnShowChoicesCancel_Click;
                fraShowChoices.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items =
                    {
                        new Label{ Text = "Show Choices", Font = SystemFonts.Bold(12) },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "Prompt" }, txtChoicePrompt }},
                        new Label{ Text = "Choices" },
                        txtChoices1, txtChoices2, txtChoices3, txtChoices4,
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } }
                    }
                };
            }

            // Add Chatbox Text
            if (fraAddText.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnAddTextOk_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnAddTextCancel_Click;
                fraAddText.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items =
                    {
                        new Label{ Text = "Add Chatbox Text", Font = SystemFonts.Bold(12) },
                        txtAddText_Text,
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 8, Items = { optAddText_Map, optAddText_Global, optAddText_Player }},
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } }
                    }
                };
            }

            // Show ChatBubble
            if (fraShowChatBubble.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnShowChatBubbleOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnShowChatBubbleCancel_Click;
                cmbChatBubbleTargetType.SelectedIndexChanged += CmbChatBubbleTargetType_SelectedIndexChanged;
                fraShowChatBubble.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items =
                    {
                        new Label{ Text = "Show ChatBubble", Font = SystemFonts.Bold(12) },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "Text" }, txtChatbubbleText }},
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "Target" }, cmbChatBubbleTargetType, cmbChatBubbleTarget }},
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } }
                    }
                };
            }

            // Player Variable
            if (fraPlayerVariable.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnPlayerVarOk_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnPlayerVarCancel_Click;
                optVariableAction0.CheckedChanged += OptVariableAction0_CheckedChanged;
                optVariableAction1.CheckedChanged += OptVariableAction1_CheckedChanged;
                optVariableAction2.CheckedChanged += OptVariableAction2_CheckedChanged;
                optVariableAction3.CheckedChanged += OptVariableAction3_CheckedChanged;
                fraPlayerVariable.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items =
                    {
                        new Label{ Text = "Set Player Variable", Font = SystemFonts.Bold(12) },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "Variable" }, cmbVariable }},
                        new Label{ Text = "Action" },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 8, Items = { optVariableAction0, optVariableAction1, optVariableAction2, optVariableAction3 }},
                        new Label{ Text = "Data" },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { nudVariableData0, nudVariableData1, nudVariableData2, nudVariableData3, nudVariableData4 }},
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } }
                    }
                };
            }

            // Player Switch
            if (fraPlayerSwitch.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnSetPlayerSwitchOk_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnSetPlayerSwitchCancel_Click;
                fraPlayerSwitch.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items =
                    {
                        new Label{ Text = "Set Player Switch", Font = SystemFonts.Bold(12) },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "Switch" }, cmbSwitch, new Label{ Text = "Set" }, cmbPlayerSwitchSet }},
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } }
                    }
                };
            }

            // Self Switch
            if (fraSetSelfSwitch.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnSelfswitchOk_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnSelfswitchCancel_Click;
                fraSetSelfSwitch.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items =
                    {
                        new Label{ Text = "Set Self Switch", Font = SystemFonts.Bold(12) },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "Switch" }, cmbSetSelfSwitch, new Label{ Text = "To" }, cmbSetSelfSwitchTo }},
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } }
                    }
                };
            }

            // Conditional Branch
            if (fraConditionalBranch.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnConditionalBranchOk_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnConditionalBranchCancel_Click;
                // condition toggles already wired in InitializeComponent
                fraConditionalBranch.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items =
                    {
                        new Label{ Text = "Conditional Branch", Font = SystemFonts.Bold(12) },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 10, Items = { optCondition0, optCondition1, optCondition2, optCondition3, optCondition4, optCondition5, optCondition6, optCondition8, optCondition9 }},
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } }
                    }
                };
            }

            // Label
            if (fraCreateLabel.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnCreateLabelOk_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnCreateLabelCancel_Click;
                fraCreateLabel.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Label", Font = SystemFonts.Bold(12) }, txtLabelName, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Go To Label
            if (fraGoToLabel.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnGoToLabelOk_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnGoToLabelCancel_Click;
                fraGoToLabel.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Go To Label", Font = SystemFonts.Bold(12) }, txtGoToLabel, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Change Items
            if (fraChangeItems.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnChangeItemsOk_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnChangeItemsCancel_Click;
                fraChangeItems.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items =
                    {
                        new Label{ Text = "Change Items", Font = SystemFonts.Bold(12) },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { cmbChangeItemIndex, new Label{ Text = "Amount" }, nudChangeItemsAmount } },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 8, Items = { optChangeItemSet, optChangeItemAdd, optChangeItemRemove } },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } }
                    }
                };
            }

            // Change Level
            if (fraChangeLevel.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnChangeLevelOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnChangeLevelCancel_Click;
                fraChangeLevel.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Change Level", Font = SystemFonts.Bold(12) }, nudChangeLevel, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Change Skills
            if (fraChangeSkills.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnChangeSkillsOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnChangeSkillsCancel_Click;
                fraChangeSkills.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Change Skills", Font = SystemFonts.Bold(12) }, cmbChangeSkills, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { optChangeSkillsAdd, optChangeSkillsRemove } }, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Change Job
            if (fraChangeJob.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnChangeJobOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnChangeJobCancel_Click;
                fraChangeJob.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Change Job", Font = SystemFonts.Bold(12) }, cmbChangeJob, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Change Sprite
            if (fraChangeSprite.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnChangeSpriteOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnChangeSpriteCancel_Click;
                fraChangeSprite.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Change Sprite", Font = SystemFonts.Bold(12) }, nudChangeSprite, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Change Gender
            if (fraChangeGender.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnChangeGenderOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnChangeGenderCancel_Click;
                fraChangeGender.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Change Gender", Font = SystemFonts.Bold(12) }, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 8, Items = { optChangeSexMale, optChangeSexFemale } }, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Change PK
            if (fraChangePK.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnChangePkOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnChangePkCancel_Click;
                fraChangePK.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Change PK", Font = SystemFonts.Bold(12) }, cmbSetPK, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Give Experience
            if (fraGiveExp.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnGiveExpOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnGiveExpCancel_Click;
                fraGiveExp.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Give Experience", Font = SystemFonts.Bold(12) }, nudGiveExp, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Warp Player
            if (fraPlayerWarp.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnPlayerWarpOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnPlayerWarpCancel_Click;
                fraPlayerWarp.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items =
                    {
                        new Label{ Text = "Warp Player", Font = SystemFonts.Bold(12) },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "Map" }, nudWPMap, new Label{ Text = "X" }, nudWPX, new Label{ Text = "Y" }, nudWPY, new Label{ Text = "Dir" }, cmbWarpPlayerDir }},
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } }
                    }
                };
            }

            // Move Route
            if (fraMoveRoute.Content == null)
            {
                // Buttons already exist as fields
                btnMoveRouteOk.Click += BtnMoveRouteOk_Click;
                btnMoveRouteCancel.Click += BtnMoveRouteCancel_Click;
                fraMoveRoute.Content = new TableLayout
                {
                    Spacing = new Size(6,6),
                    Rows =
                    {
                        new TableRow(new Label{ Text = "Move Route", Font = SystemFonts.Bold(12) }),
                        new TableRow(new TableCell(lstvwMoveRoute, true)),
                        new TableRow(new TableCell(lstMoveRoute, true)),
                        new TableRow(new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 10, Items = { chkIgnoreMove, chkRepeatRoute } }),
                        new TableRow(new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnMoveRouteOk, btnMoveRouteCancel } })
                    }
                };
            }

            // Move Route Wait
            if (fraMoveRouteWait.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnMoveWaitOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnMoveWaitCancel_Click;
                fraMoveRouteWait.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Wait for Route Completion", Font = SystemFonts.Bold(12) }, cmbMoveWait, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Spawn NPC
            if (fraSpawnNpc.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnSpawnNpcOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnSpawnNpcCancel_Click;
                fraSpawnNpc.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Force Spawn NPC", Font = SystemFonts.Bold(12) }, cmbSpawnNpc, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Play Animation
            if (fraPlayAnimation.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnPlayAnimationOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnPlayAnimationCancel_Click;
                fraPlayAnimation.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items =
                    {
                        new Label{ Text = "Play Animation", Font = SystemFonts.Bold(12) },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "Target" }, cmbAnimTargetType, cmbPlayAnimEvent } },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "Animation" }, cmbPlayAnim } },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { lblPlayAnimX, nudPlayAnimTileX, lblPlayAnimY, nudPlayAnimTileY } },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } }
                    }
                };
            }

            // Set Fog
            if (fraSetFog.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnSetFogOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnSetFogCancel_Click;
                fraSetFog.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Set Fog", Font = SystemFonts.Bold(12) }, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "Fog" }, nudFogData0, new Label{ Text = "X Offset" }, nudFogData1, new Label{ Text = "Y Offset" }, nudFogData2 } }, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Set Weather
            if (fraSetWeather.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnSetWeatherOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnSetWeatherCancel_Click;
                fraSetWeather.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Set Weather", Font = SystemFonts.Bold(12) }, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { CmbWeather, new Label{ Text = "Intensity" }, nudWeatherIntensity } }, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Map Tint
            if (fraMapTint.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnMapTintOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnMapTintCancel_Click;
                fraMapTint.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Set Map Tint", Font = SystemFonts.Bold(12) }, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "R" }, nudMapTintData0, new Label{ Text = "G" }, nudMapTintData1, new Label{ Text = "B" }, nudMapTintData2, new Label{ Text = "A" }, nudMapTintData3 } }, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Play BGM
            if (fraPlayBGM.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnPlayBgmOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnPlayBgmCancel_Click;
                fraPlayBGM.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Play BGM", Font = SystemFonts.Bold(12) }, cmbPlayBGM, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Play Sound
            if (fraPlaySound.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnPlaySoundOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnPlaySoundCancel_Click;
                fraPlaySound.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Play Sound", Font = SystemFonts.Bold(12) }, cmbPlaySound, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Wait
            if (fraSetWait.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnSetWaitOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnSetWaitCancel_Click;
                fraSetWait.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Wait", Font = SystemFonts.Bold(12) }, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "Seconds" }, nudWaitAmount } }, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Set Access
            if (fraSetAccess.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnSetAccessOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnSetAccessCancel_Click;
                fraSetAccess.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Set Access", Font = SystemFonts.Bold(12) }, cmbSetAccess, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Open Shop
            if (fraOpenShop.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnOpenShopOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnOpenShopCancel_Click;
                fraOpenShop.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items = { new Label{ Text = "Open Shop", Font = SystemFonts.Bold(12) }, cmbOpenShop, new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } } }
                };
            }

            // Show Picture
            if (fraShowPic.Content == null)
            {
                var btnOk = new Button { Text = "OK" }; btnOk.Click += BtnShowPicOK_Click;
                var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += BtnShowPicCancel_Click;
                fraShowPic.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 6,
                    Items =
                    {
                        new Label{ Text = "Show Picture", Font = SystemFonts.Bold(12) },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "Picture" }, nudShowPicture } },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "Location" }, cmbPicLoc } },
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { new Label{ Text = "Offset X" }, nudPicOffsetX, new Label{ Text = "Offset Y" }, nudPicOffsetY } },
                        picShowPic,
                        new StackLayout{ Orientation = Orientation.Horizontal, Spacing = 6, Items = { btnOk, btnCancel } }
                    }
                };
            }
        }

        // Build command palette with categories and leaves
        private void BuildCommandPalette()
        {
            tvCommands.Columns.Clear();
            tvCommands.Columns.Add(new GridColumn
            {
                HeaderText = "Command",
                DataCell = new TextBoxCell(0),
            });

            var root = new TreeGridItemCollection();

            TreeGridItem Cat(string name, params string[] children)
            {
                var item = new TreeGridItem { Values = new object[] { name } };
                foreach (var c in children)
                    item.Children.Add(new TreeGridItem { Values = new object[] { c } });
                return item;
            }

            root.Add(Cat("Messages",
                "Show Text",
                "Show Choices",
                "Add Chatbox Text",
                "Show ChatBubble"));
            root.Add(Cat("Flow",
                "Set Player Variable",
                "Set Player Switch",
                "Set Self Switch",
                "Conditional Branch",
                "Stop Event Processing",
                "Label",
                "GoTo Label"));
            root.Add(Cat("Player",
                "Change Items",
                "Restore HP",
                "Restore MP",
                "Restore SP",
                "Level Up",
                "Change Level",
                "Change Skills",
                "Change Job",
                "Change Sprite",
                "Change Gender",
                "Change PK",
                "Give Experience"));
            root.Add(Cat("Movement",
                "Warp Player",
                "Set Move Route",
                "Wait for Route Completion",
                "Force Spawn Npc",
                "Hold Player",
                "Release Player"));
            root.Add(Cat("Animation",
                "Play Animation"));
            root.Add(Cat("Map",
                "Set Fog",
                "Set Weather",
                "Set Map Tinting"));
            root.Add(Cat("Audio",
                "Play BGM",
                "Stop BGM",
                "Play Sound",
                "Stop Sounds"));
            root.Add(Cat("Utility",
                "Wait...",
                "Set Access",
                "Open Bank",
                "Open Shop",
                "Fade In",
                "Fade Out",
                "Flash White",
                "Show Picture",
                "Hide Picture"));

            tvCommands.DataStore = root;
            // Expand top-level categories
            foreach (var obj in root)
            {
                if (obj is TreeGridItem tgi)
                    tgi.Expanded = true;
            }
        }

        #region Form

        public void ClearConditionFrame()
        {
            int i;

            cmbCondition_PlayerVarIndex.Enabled = false;
            cmbCondition_PlayerVarIndex.Items.Clear();

            for (i = 0; i < Variables.MaxVariables; i++)
                cmbCondition_PlayerVarIndex.Items.Add(i + 1 + ". " + Event.Variables[i]);
            cmbCondition_PlayerVarIndex.SelectedIndex = 0;
            cmbCondition_PlayerVarCompare.SelectedIndex = 0;
            cmbCondition_PlayerVarCompare.Enabled = false;
            nudCondition_PlayerVarCondition.Enabled = false;
            nudCondition_PlayerVarCondition.Value = 0;
            cmbCondition_PlayerSwitch.Enabled = false;
            cmbCondition_PlayerSwitch.Items.Clear();

            for (i = 0; i < Variables.MaxSwitches; i++)
                cmbCondition_PlayerSwitch.Items.Add(i + 1 + ". " + Event.Switches[i]);
            cmbCondition_PlayerSwitch.SelectedIndex = 0;
            cmbCondtion_PlayerSwitchCondition.Enabled = false;
            cmbCondtion_PlayerSwitchCondition.SelectedIndex = 0;
            cmbCondition_HasItem.Enabled = false;
            cmbCondition_HasItem.Items.Clear();

            for (i = 0; i < Core.Globals.Variables.MaxItems; i++)
                cmbCondition_HasItem.Items.Add(i + 1 + ". " + Item.Instance[i].Name);
            cmbCondition_HasItem.SelectedIndex = 0;
            nudCondition_HasItem.Enabled = false;
            nudCondition_HasItem.Value = 1;
            cmbCondition_JobIs.Enabled = false;
            cmbCondition_JobIs.Items.Clear();

            for (i = 0; i < Variables.MaxJobs; i++)
                cmbCondition_JobIs.Items.Add(i + 1 + ". " + Job.Instance[i].Name);
            cmbCondition_JobIs.SelectedIndex = 0;
            cmbCondition_LearntSkill.Enabled = false;
            cmbCondition_LearntSkill.Items.Clear();

            for (i = 0; i < Variables.MaxSkills; i++)
                cmbCondition_LearntSkill.Items.Add(i + 1 + ". " + Strings.Trim(Data.Skill[i].Name));
            cmbCondition_LearntSkill.SelectedIndex = 0;
            cmbCondition_LevelCompare.Enabled = false;
            cmbCondition_LevelCompare.SelectedIndex = 0;
            nudCondition_LevelAmount.Enabled = false;
            nudCondition_LevelAmount.Value = 0;
            if (cmbCondition_SelfSwitch.Items.Count > -1)
            {
                cmbCondition_SelfSwitch.SelectedIndex = 0;
            }

            cmbCondition_SelfSwitch.Enabled = false;

            if (cmbCondition_SelfSwitchCondition.Items.Count > -1)
            {
                cmbCondition_SelfSwitchCondition.SelectedIndex = 0;
            }

            cmbCondition_SelfSwitchCondition.Enabled = false;

            cmbCondition_Gender.Enabled = false;

            cmbCondition_Time.Enabled = false;
        }

        private void Editor_Events_Load(object? sender, EventArgs e)
        {
            try
            {
                int i;

                // Safety: ensure Instance has at least one page so the tab area isn't empty
                if (Event.Instance.PageCount <= 0 || Event.Instance.Pages == null || Event.Instance.Pages.Length == 0)
                {
                    Event.Instance.PageCount = 1;
                    Array.Resize(ref Event.Instance.Pages, 1);
                }

                // Add a bit of inner margin so content isn't flush with window edges
                Padding = new Eto.Drawing.Padding(8);

                cmbSwitch.Items.Clear();
                for (i = 0; i < Variables.MaxSwitches; i++)
                    cmbSwitch.Items.Add(i + 1 + ". " + Event.Switches[i]);
                cmbSwitch.SelectedIndex = 0;
                cmbVariable.Items.Clear();

                for (i = 0; i < Variables.MaxVariables; i++)
                    cmbVariable.Items.Add(i + 1 + ". " + Event.Variables[i]);
                cmbVariable.SelectedIndex = 0;
                cmbChangeItemIndex.Items.Clear();
                for (i = 0; i < Core.Globals.Variables.MaxItems; i++)
                    cmbChangeItemIndex.Items.Add(Item.Instance[i].Name);
                cmbChangeItemIndex.SelectedIndex = 0;
                nudChangeLevel.MinValue = 1;
                nudChangeLevel.MaxValue = GameState.MaxLevel;
                nudChangeLevel.Value = 1;
                cmbChangeSkills.Items.Clear();

                for (i = 0; i < Variables.MaxSkills; i++)
                    cmbChangeSkills.Items.Add(Data.Skill[i].Name);
                cmbChangeSkills.SelectedIndex = 0;
                cmbChangeJob.Items.Clear();

                for (i = 0; i < Variables.MaxJobs; i++)
                    cmbChangeJob.Items.Add(Strings.Trim(Job.Instance[i].Name));
                cmbChangeJob.SelectedIndex = 0;
                nudChangeSprite.MaxValue = GameState.NumCharacters;
                cmbPlayAnim.Items.Clear();

                for (i = 0; i < Variables.MaxAnimations; i++)
                    cmbPlayAnim.Items.Add(i + 1 + ". " + Animation.Instance[i].Name);
                cmbPlayAnim.SelectedIndex = 0;

                cmbPlayBGM.Items.Clear();

                General.CacheMusic();
                var loopTo = Information.UBound(Audio.MusicCache);
                for (i = 0; i < loopTo; i++)
                    cmbPlayBGM.Items.Add(Audio.MusicCache[i]);
                cmbPlayBGM.SelectedIndex = 0;
                cmbPlaySound.Items.Clear();

                General.CacheSound();
                var loopTo1 = Information.UBound(Audio.SoundCache);
                for (i = 0; i < loopTo1; i++)
                    cmbPlaySound.Items.Add(Audio.SoundCache[i]);
                cmbPlaySound.SelectedIndex = 0;
                cmbOpenShop.Items.Clear();

                for (i = 0; i < Variables.MaxVariables; i++)
                    cmbOpenShop.Items.Add(i + 1 + ". " + Data.Shop[i].Name);
                cmbOpenShop.SelectedIndex = 0;
                cmbSpawnNpc.Items.Clear();

                for (i = 0; i < Variables.MaxMapNpcs; i++)
                {
                    if (Data.MyMap.Npc[i] > 0)
                    {
                        cmbSpawnNpc.Items.Add(i + 1 + ". " + Data.Npc[Data.MyMap.Npc[i]].Name);
                    }
                    else
                    {
                        cmbSpawnNpc.Items.Add(i + ". ");
                    }
                }

                cmbSpawnNpc.SelectedIndex = 0;
                nudFogData0.MaxValue = GameState.NumFogs;
                nudWPMap.MaxValue = Variables.MaxVariables;

                cmbEvent.Items.Add("This Event");
                cmbEvent.SelectedIndex = 0;

                // Populate Page Settings option combos
                cmbTrigger.Items.Clear();
                cmbTrigger.Items.Add("Action Button");
                cmbTrigger.Items.Add("Player Touch");
                cmbTrigger.Items.Add("Parallel");

                cmbPositioning.Items.Clear();
                cmbPositioning.Items.Add("Below Player");
                cmbPositioning.Items.Add("Same as Player");
                cmbPositioning.Items.Add("Above Player");

                cmbMoveType.Items.Clear();
                cmbMoveType.Items.Add("Fixed");
                cmbMoveType.Items.Add("Random");
                cmbMoveType.Items.Add("Route");

                cmbMoveSpeed.Items.Clear();
                cmbMoveSpeed.Items.Add("8x Slower");
                cmbMoveSpeed.Items.Add("4x Slower");
                cmbMoveSpeed.Items.Add("2x Slower");
                cmbMoveSpeed.Items.Add("Normal");
                cmbMoveSpeed.Items.Add("2x Faster");
                cmbMoveSpeed.Items.Add("4x Faster");

                cmbMoveFreq.Items.Clear();
                cmbMoveFreq.Items.Add("Lowest");
                cmbMoveFreq.Items.Add("Lower");
                cmbMoveFreq.Items.Add("Normal");
                cmbMoveFreq.Items.Add("Higher");
                cmbMoveFreq.Items.Add("Highest");

                // Move Wait is typically filled contextually; provide a default entry
                cmbMoveWait.Items.Clear();
                cmbMoveWait.Items.Add("This Event");
                cmbMoveWait.SelectedIndex = 0;

                // Graphic type choices
                cmbGraphic.Items.Clear();
                cmbGraphic.Items.Add("None");
                cmbGraphic.Items.Add("Character");
                cmbGraphic.Items.Add("Tileset");
                cmbGraphic.SelectedIndexChanged += CmbGraphic_SelectedIndexChanged;
                nudGraphic.ValueChanged += nudGraphic_ValueChanged;

                // set the tabs
                tabPages.Pages.Clear();

                var loopTo2 = Event.Instance.PageCount;
                for (i = 0; i < loopTo2; i++)
                    tabPages.Pages.Add(new TabPage { Text = Conversion.Str(i + 1) });

                // items
                cmbHasItem.Items.Clear();
                for (i = 0; i < Core.Globals.Variables.MaxItems; i++)
                    cmbHasItem.Items.Add(i + 1 + ": " + Item.Instance[i].Name);

                // variables
                cmbPlayerVar.Items.Clear();
                for (i = 0; i < Variables.MaxVariables; i++)
                    cmbPlayerVar.Items.Add(i + 1 + ". " + Event.Variables[i]);
                // player var compare options
                cmbPlayerVarCompare.Items.Clear();
                cmbPlayerVarCompare.Items.Add("=");
                cmbPlayerVarCompare.Items.Add(">");
                cmbPlayerVarCompare.Items.Add("<");
                cmbPlayerVarCompare.Items.Add("!=");
                cmbPlayerVarCompare.Items.Add(">=");
                cmbPlayerVarCompare.Items.Add("<=");
                // switches
                cmbPlayerSwitch.Items.Clear();
                for (i = 0; i < Variables.MaxSwitches; i++)
                    cmbPlayerSwitch.Items.Add(i + 1 + ". " + Event.Switches[i]);
                // player switch compare options
                cmbPlayerSwitchCompare.Items.Clear();
                cmbPlayerSwitchCompare.Items.Add("On");
                cmbPlayerSwitchCompare.Items.Add("Off");
                // self switch list A-D and compare ON/OFF
                cmbSelfSwitch.Items.Clear();
                cmbSelfSwitch.Items.Add("A");
                cmbSelfSwitch.Items.Add("B");
                cmbSelfSwitch.Items.Add("C");
                cmbSelfSwitch.Items.Add("D");
                cmbSelfSwitch.SelectedIndex = 0;
                cmbSelfSwitchCompare.Items.Clear();
                cmbSelfSwitchCompare.Items.Add("On");
                cmbSelfSwitchCompare.Items.Add("Off");

                // enable delete button
                btnDeletePage.Enabled = Event.Instance.PageCount > 1;
                btnPastePage.Enabled = false;

                nudShowPicture.MaxValue = GameState.NumPictures;
                cmbPicLoc.SelectedIndex = 0;
                fraDialogue.Visible = false;

                if (tabPages.SelectedIndex < 0 && tabPages.Pages.Count > 0)
                {
                    tabPages.SelectedIndex = 0;
                    Event.CurPageNum = 0;
                }
                // Load page 1 to start off with
                Event.CurPageNum = 0;
                if (string.IsNullOrEmpty(Event.Instance.Name))
                    Event.Instance.Name = string.Empty;
                txtName.Text = Event.Instance.Name;

                Event.EventEditorLoadPage(Event.CurPageNum);
                AttachEditorHostToSelectedTab();
                HideAllFrames();
            }
            catch (Exception ex)
            {
                try { System.Console.WriteLine($"[EventEditor] Load error: {ex}"); } catch { }
            }
        }

        private void Editor_Event_Resize(object? sender, EventArgs e) { }
        private void Editor_Event_Activated(object? sender, EventArgs e) { }

        public void DrawGraphic()
        {
            try
            {
                // Clear first
                picGraphic.Image = null;

                // Validate page and selection
                if (Event.Instance.Pages == null || Event.CurPageNum < 0 || Event.CurPageNum >= Event.Instance.Pages.Length)
                    return;

                var gfxType = cmbGraphic.SelectedIndex; // 0=None, 1=Character, 2=Tileset
                var gfxIndex = (int)System.Math.Round(nudGraphic.Value);
                if (gfxType <= 0 || gfxIndex <= 0)
                    return;

                string basePath = gfxType == 1 ? DataPath.Characters : DataPath.Tilesets;
                string path = System.IO.Path.Combine(basePath, gfxIndex.ToString()) + GameState.GfxExt;

                if (!System.IO.File.Exists(path))
                    return;

                var src = new Eto.Drawing.Bitmap(path);

                // Character sheet preview: dynamically compute first idle frame or selected frame using segmentation logic
                if (gfxType == 1 && src.Width > 0 && src.Height > 0)
                {
                    try
                    {
                        // Dynamic direction rows (supports configured -> 8 -> 4 -> 1 fallback)
                        int configuredDirs = SettingsManager.Instance.SpriteDirections <= 0 ? 4 : SettingsManager.Instance.SpriteDirections;
                        configuredDirs = Math.Max(1, configuredDirs);
                        int directionRows;
                        if (src.Height % configuredDirs == 0) directionRows = configuredDirs;
                        else if (configuredDirs != 8 && src.Height % 8 == 0) directionRows = 8;
                        else if (configuredDirs != 4 && src.Height % 4 == 0) directionRows = 4;
                        else directionRows = 1;
                        int idleFrames = Math.Max(1, SettingsManager.Instance.IdleFrames);
                        int runFrames = Math.Max(1, SettingsManager.Instance.RunFrames);
                        int attackFrames = Math.Max(1, SettingsManager.Instance.AttackFrames);
                        int expectedCols = idleFrames + runFrames + attackFrames;
                        int frameHeight = src.Height / directionRows;
                        if (frameHeight <= 0) frameHeight = src.Height; // safety fallback

                        bool widthDivisible = expectedCols > 0 && src.Width % expectedCols == 0;
                        bool canSegment = widthDivisible; // treat divisible width as segmented sheet
                        int frameColumnsForWidth = canSegment ? expectedCols : idleFrames;
                        if (!canSegment && src.Width % idleFrames != 0)
                        {
                            // Fallback: attempt square-ish frames based on row height
                            int approx = frameHeight > 0 ? src.Width / frameHeight : idleFrames;
                            if (approx > 0) frameColumnsForWidth = approx;
                        }
                        int frameWidth = src.Width / Math.Max(1, frameColumnsForWidth);
                        if (frameWidth <= 0) frameWidth = src.Width; // final fallback

                        // Use selected sub-frame if user clicked in selection (Event.GraphicSelX/Y). Clamp inside first direction row (down-facing assumed row 0)
                        int fx = Event.GraphicSelX; if (fx < 0) fx = 0; else if (fx >= frameColumnsForWidth) fx = frameColumnsForWidth - 1;
                        int fy = Event.GraphicSelY; if (fy < 0) fy = 0; else if (fy >= directionRows) fy = 0; // restrict to row 0 for preview simplicity

                        var cropped = new Bitmap(frameWidth, frameHeight, PixelFormat.Format32bppRgba);
                        using (var g = new Graphics(cropped))
                        {
                            var srcRect = new Eto.Drawing.Rectangle(fx * frameWidth, fy * frameHeight, frameWidth, frameHeight);
                            g.DrawImage(src, new Eto.Drawing.Rectangle(0, 0, frameWidth, frameHeight), srcRect);
                        }
                        picGraphic.Image = cropped;
                    }
                    catch
                    {
                        // Fallback to full image if segmentation fails
                        picGraphic.Image = src;
                    }
                }
                else
                {
                    picGraphic.Image = src; // Tileset or unsupported graphic type
                }
            }
            catch
            {
                picGraphic.Image = null;
            }
        }

        // Renders the full spritesheet/tileset into the selection view used by the Set Graphic frame
        private void DrawGraphicSelectionPreview()
        {
            try
            {
                picGraphicSel.Image = null;

                var gfxType = cmbGraphic.SelectedIndex; // 0=None, 1=Character, 2=Tileset
                var gfxIndex = (int)Math.Round(nudGraphic.Value);
                if (gfxType <= 0 || gfxIndex <= 0)
                    return;

                string basePath = gfxType == 1 ? DataPath.Characters : DataPath.Tilesets;
                string path = Path.Combine(basePath, gfxIndex.ToString()) + GameState.GfxExt;
                if (!File.Exists(path))
                    return;

                var src = new Bitmap(path);
                picGraphicSel.Image = src;
                try { picGraphicSel.Size = new Size(src.Width, src.Height); } catch { }
            }
            catch
            {
                picGraphicSel.Image = null;
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (fraGraphic.Visible == false)
            {
                Event.EventEditorOK();
                Event.Instance = default;
                Close();
            }
            else
            {
                if (Event.GraphicSelType == 0)
                {
                    Event.Instance.Pages[Event.CurPageNum].GraphicType = (byte)cmbGraphic.SelectedIndex;
                    Event.Instance.Pages[Event.CurPageNum].Graphic = (int)Math.Round(nudGraphic.Value);
                    Event.Instance.Pages[Event.CurPageNum].GraphicX = Event.GraphicSelX;
                    Event.Instance.Pages[Event.CurPageNum].GraphicY = Event.GraphicSelY;
                    Event.Instance.Pages[Event.CurPageNum].GraphicX2 = Event.GraphicSelX2;
                    Event.Instance.Pages[Event.CurPageNum].GraphicY2 = Event.GraphicSelY2;
                }
                else
                {
                    AddMoveRouteCommand(42);
                    Event.GraphicSelType = 0;
                }
                fraGraphic.Visible = false;
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            if (fraGraphic.Visible == false)
            {
                Event.Instance = default;
                Close();
            }
            else
            {
                Event.Instance.Pages[Event.CurPageNum].GraphicType = tmpGraphicType;
                Event.Instance.Pages[Event.CurPageNum].Graphic = tmpGraphicIndex;
                fraGraphic.Visible = false;
                DrawGraphic();
            }     
        }

        private void TvCommands_AfterSelect(object? sender, EventArgs e)
        {
            // Use selectedText directly to show the appropriate frame; ignore categories
            var x = 0;
            string selectedText = string.Empty;
            var item = tvCommands.SelectedItem as TreeGridItem;
            if (item == null) return;
            if (item.Children != null && item.Children.Count > 0) return; // category, do nothing
            var vals = item.Values;
            if (vals != null && vals.Length > 0) selectedText = vals[0]?.ToString() ?? string.Empty;
            // Always start from a clean state so only the target frame is visible
            HideAllFrames();
            // Keep command palette visible; show frames above via layout ordering
            // Scroll to top so the frame header is in view
            ScrollRightPaneTop();

            switch (selectedText)
            {
                // Messages

                // show text
                case "Show Text":
                    {
                        txtShowText.Text = "";
                        // host inside dialogue wrapper
                        ShowFrame(fraShowText, true);
                        break;
                    }
                // show choices
                case "Show Choices":
                    {
                        txtChoicePrompt.Text = "";
                        txtChoices1.Text = "";
                        txtChoices2.Text = "";
                        txtChoices3.Text = "";
                        txtChoices4.Text = "";

                        ShowFrame(fraShowChoices, true);
                        break;
                    }
                // chatbox text
                case "Add Chatbox Text":
                    {
                        txtAddText_Text.Text = "";
                        optAddText_Player.Checked = true;
                        ShowFrame(fraAddText, true);
                        break;
                    }
                // chat bubble
                case "Show ChatBubble":
                    {
                        txtChatbubbleText.Text = "";
                        cmbChatBubbleTargetType.SelectedIndex = 0;
                        cmbChatBubbleTarget.Visible = false;
                        ShowFrame(fraShowChatBubble, true);
                        break;
                    }
                // event progression
                // player variable
                case "Set Player Variable":
                    {
                        nudVariableData0.Value = 0;
                        nudVariableData1.Value = 0;
                        nudVariableData2.Value = 0;
                        nudVariableData3.Value = 0;
                        nudVariableData4.Value = 0;

                        cmbVariable.SelectedIndex = 0;
                        optVariableAction0.Checked = true;
                        ShowFrame(fraPlayerVariable, true);
                        break;
                    }
                // player switch
                case "Set Player Switch":
                    {
                        cmbPlayerSwitchSet.SelectedIndex = 0;
                        cmbSwitch.SelectedIndex = 0;
                        ShowFrame(fraPlayerSwitch, true);
                        break;
                    }
                // self switch
                case "Set Self Switch":
                    {
                        cmbSetSelfSwitchTo.SelectedIndex = 0;
                        ShowFrame(fraSetSelfSwitch, true);
                        break;
                    }
                // flow control

                // conditional branch
                case "Conditional Branch":
                    {
                        ShowFrame(fraConditionalBranch, true);
                        optCondition0.Checked = true;
                        ClearConditionFrame();
                        cmbCondition_PlayerVarIndex.Enabled = true;
                        cmbCondition_PlayerVarCompare.Enabled = true;
                        nudCondition_PlayerVarCondition.Enabled = true;
                        break;
                    }
                // Exit Event Process
                case "Stop Event Processing":
                    {
                        Event.AddCommand((int)EventCommand.ExitEventProcess);
                        break;
                    }
                // Label
                case "Label":
                    {
                        txtLabelName.Text = "";
                        ShowFrame(fraCreateLabel, true);
                        break;
                    }
                // GoTo Label
                case "GoTo Label":
                    {
                        txtGoToLabel.Text = "";
                        ShowFrame(fraGoToLabel, true);
                        break;
                    }
                // Player Control

                // Change Items
                case "Change Items":
                    {
                        cmbChangeItemIndex.SelectedIndex = 0;
                        optChangeItemSet.Checked = true;
                        nudChangeItemsAmount.Value = 0;
                        ShowFrame(fraChangeItems, true);
                        break;
                    }
                // Restore HP
                case "Restore HP":
                    {
                        Event.AddCommand((int)EventCommand.RestoreHealth);
                        break;
                    }
                // Restore MP
                case "Restore MP":
                    {
                        Event.AddCommand((int)EventCommand.RestoreMana);
                        break;
                    }
                // Restore SP
                case "Restore SP":
                    {
                        Event.AddCommand((int)EventCommand.RestoreStamina);
                        break;
                    }
                // Level Up
                case "Level Up":
                    {
                        Event.AddCommand((int)EventCommand.ChangeLevel);
                        break;
                    }
                // Change Level
                case "Change Level":
                    {
                        nudChangeLevel.Value = 1;
                        ShowFrame(fraChangeLevel, true);
                        break;
                    }
                // Change Skills
                case "Change Skills":
                    {
                        cmbChangeSkills.SelectedIndex = 0;
                        ShowFrame(fraChangeSkills, true);
                        break;
                    }
                // Change Job
                case "Change Job":
                    {
                        if (Variables.MaxJobs > 0)
                        {
                            if (cmbChangeJob.Items.Count == 0)
                            {
                                cmbChangeJob.Items.Clear();

                                for (int i = 0; i < Variables.MaxJobs; i++)
                                    cmbChangeJob.Items.Add(Strings.Trim(Job.Instance[i].Name));
                                cmbChangeJob.SelectedIndex = 0;
                            }
                        }
                        ShowFrame(fraChangeJob, true);
                        break;
                    }
                // Change Sprite
                case "Change Sprite":
                    {
                        nudChangeSprite.Value = 1;
                        ShowFrame(fraChangeSprite, true);
                        break;
                    }
                // Change Gender
                case "Change Gender":
                    {
                        optChangeSexMale.Checked = true;
                        ShowFrame(fraChangeGender, true);
                        break;
                    }
                // Change PK
                case "Change PK":
                    {
                        cmbSetPK.SelectedIndex = 0;
                        ShowFrame(fraChangePK, true);
                        break;
                    }
                // Give Exp
                case "Give Experience":
                    {
                        nudGiveExp.Value = 0;
                        ShowFrame(fraGiveExp, true);
                        break;
                    }
                // Movement

                // Warp Player
                case "Warp Player":
                    {
                        nudWPMap.Value = 0;
                        nudWPX.Value = 0;
                        nudWPY.Value = 0;
                        cmbWarpPlayerDir.SelectedIndex = 0;
                        ShowFrame(fraPlayerWarp, true);
                        break;
                    }
                // Set Move Route
                case "Set Move Route":
                    {
                        fraMoveRoute.Visible = true;
                        lstMoveRoute.Items.Clear();
                        Event.ListOfEvents = new int[Data.MyMap.EventCount];
                        Event.ListOfEvents[0] = Event.EditorId;
                        for (int i = 0, loopTo = Data.MyMap.EventCount; i < loopTo; i++)
                        {
                            if (i != Event.EditorId)
                            {
                                cmbEvent.Items.Add(Data.MyMap.Event[i].Name);
                                x = x + 1;
                                Event.ListOfEvents[x] = i;
                            }
                        }
                        Event.IsMoveRouteCommand = true;
                        chkIgnoreMove.Checked = false;
                        chkRepeatRoute.Checked = false;
                        Event.TempMoveRouteCount = 0;
                        Event.TempMoveRoute = new Type.MoveRoute[1];
                        ShowFrame(fraMoveRoute, false);
                        break;
                    }
                // Wait for Route Completion
                case "Wait for Route Completion":
                    {
                        cmbMoveWait.Items.Clear();
                        Event.ListOfEvents = new int[Data.MyMap.EventCount];
                        Event.ListOfEvents[0] = Event.EditorId;
                        cmbMoveWait.Items.Add("This Event");
                        cmbMoveWait.SelectedIndex = 0;
                        cmbMoveWait.Enabled = true;
                        for (int i = 0, loopTo1 = Data.MyMap.EventCount; i < loopTo1; i++)
                        {
                            if (i != Event.EditorId)
                            {
                                cmbMoveWait.Items.Add(Data.MyMap.Event[i].Name);
                                x = x + 1;
                                Event.ListOfEvents[x] = i;
                            }
                        }
                        ShowFrame(fraMoveRouteWait, true);
                        break;
                    }
                // Force Spawn Npc
                case "Force Spawn Npc":
                    {
                        // lets populate the combobox
                        cmbSpawnNpc.Items.Clear();
                        for (int i = 0; i < Variables.MaxVariables; i++)
                            cmbSpawnNpc.Items.Add(Strings.Trim(Data.Npc[i].Name));
                        cmbSpawnNpc.SelectedIndex = 0;
                        ShowFrame(fraSpawnNpc, true);
                        break;
                    }
                // Hold Player
                case "Hold Player":
                    {
                        Event.AddCommand((int)EventCommand.HoldPlayer);
                        break;
                    }
                // Release Player
                case "Release Player":
                    {
                        Event.AddCommand((int)EventCommand.ReleasePlayer);
                        break;
                    }
                // Animation

                // Play Animation
                case "Play Animation":
                    {
                        cmbPlayAnimEvent.Items.Clear();

                        for (int i = 0, loopTo2 = Data.MyMap.EventCount; i < loopTo2; i++)
                            cmbPlayAnimEvent.Items.Add(i + 1 + ". " + Data.MyMap.Event[i].Name);
                        cmbPlayAnimEvent.SelectedIndex = 0;
                        cmbAnimTargetType.SelectedIndex = 0;
                        cmbPlayAnim.SelectedIndex = 0;
                        nudPlayAnimTileX.Value = 0;
                        nudPlayAnimTileY.Value = 0;
                        nudPlayAnimTileX.MaxValue = Data.MyMap.MaxX;
                        nudPlayAnimTileY.MaxValue = Data.MyMap.MaxY;
                        ShowFrame(fraPlayAnimation, true);
                        lblPlayAnimX.Visible = false;
                        lblPlayAnimY.Visible = false;
                        nudPlayAnimTileX.Visible = false;
                        nudPlayAnimTileY.Visible = false;
                        cmbPlayAnimEvent.Visible = false;
                        break;
                    }
                // Map Functions

                // Set Fog
                case "Set Fog":
                    {
                        nudFogData0.Value = 0;
                        nudFogData1.Value = 0;
                        nudFogData2.Value = 0;
                        ShowFrame(fraSetFog, true);
                        break;
                    }
                // Set Weather
                case "Set Weather":
                    {
                        CmbWeather.SelectedIndex = 0;
                        nudWeatherIntensity.Value = 0;
                        ShowFrame(fraSetWeather, true);
                        break;
                    }
                // Set Map Tinting
                case "Set Map Tinting":
                    {
                        nudMapTintData0.Value = 0;
                        nudMapTintData1.Value = 0;
                        nudMapTintData2.Value = 0;
                        nudMapTintData3.Value = 0;
                        ShowFrame(fraMapTint, true);
                        break;
                    }
                // Music and Sound

                // PlayBGM
                case "Play BGM":
                    {
                        cmbPlayBGM.SelectedIndex = 0;
                        ShowFrame(fraPlayBGM, true);
                        break;
                    }
                // Stop BGM
                case "Stop BGM":
                    {
                        Event.AddCommand((int)EventCommand.FadeOutBgm);
                        break;
                    }
                // Play Sound
                case "Play Sound":
                    {
                        cmbPlaySound.SelectedIndex = 0;
                        ShowFrame(fraPlaySound, true);
                        break;
                    }
                // Stop Sounds
                case "Stop Sounds":
                    {
                        Event.AddCommand((int)EventCommand.StopSound);
                        break;
                    }
                // Etc...

                // Wait...
                case "Wait...":
                    {
                        nudWaitAmount.Value = 1;
                        ShowFrame(fraSetWait, true);
                        break;
                    }
                // Set Access
                case "Set Access":
                    {
                        cmbSetAccess.SelectedIndex = 0;
                        ShowFrame(fraSetAccess, true);
                        break;
                    }
                // Shop, bank etc

                // Open bank
                case "Open Bank":
                    {
                        Event.AddCommand((int)EventCommand.OpenBank);
                        break;
                    }
                // Open shop
                case "Open Shop":
                    {
                        ShowFrame(fraOpenShop, true);
                        cmbOpenShop.SelectedIndex = 0;
                        break;
                    }
                // cutscene options

                // Fade in
                case "Fade In":
                    {
                        Event.AddCommand((int)EventCommand.FadeIn);
                        break;
                    }
                // Fade out
                case "Fade Out":
                    {
                        Event.AddCommand((int)EventCommand.FadeOut);
                        break;
                    }
                // Flash white
                case "Flash White":
                    {
                        Event.AddCommand((int)EventCommand.FlashScreen);
                        break;
                    }
                // Show pic
                case "Show Picture":
                    {
                        nudShowPicture.Value = 0;
                        cmbPicLoc.SelectedIndex = 0;
                        nudPicOffsetX.Value = 0;
                        nudPicOffsetY.Value = 0;
                        ShowFrame(fraShowPic, true);
                        break;
                    }
                // Hide pic
                case "Hide Picture":
                    {
                        Event.AddCommand((int)EventCommand.HidePicture);
                        break;
                    }
            }
        }

        private void BtnCancelCommand_Click(object? sender, EventArgs e)
        {
            // Close any open command frame overlay and keep the palette visible
            HideAllFrames();
        }

        #endregion

        #region Page Buttons

        private void TabPages_Click(object? sender, EventArgs e)
        {
            Event.CurPageNum = tabPages.SelectedIndex;
            Event.EventEditorLoadPage(Event.CurPageNum);
            // Refresh the graphic controls/preview for the newly selected page
            RefreshGraphicControlsFromPage();
            AttachEditorHostToSelectedTab();
            HideAllFrames();
            SyncMoveRouteButton();
        }

        private void BtnNewPage_Click(object? sender, EventArgs e)
        {
            int pageCount;
            int i;

            if (chkGlobal.Checked == true)
            {
                Interaction.MsgBox("You cannot have multiple pages on global events!");
                return;
            }

            pageCount = Event.Instance.PageCount + 1;

            // redim the array
            Array.Resize(ref Event.Instance.Pages, pageCount);

            Event.Instance.PageCount = pageCount;

            // set the tabs
            tabPages.Pages.Clear();

            var loopTo = Event.Instance.PageCount;
            for (i = 0; i < loopTo; i++)
                tabPages.Pages.Add(new TabPage { Text = Conversion.Str(i + 1) });
            btnDeletePage.Enabled = true;
            // Select and load the newly created page
            tabPages.SelectedIndex = Math.Max(0, Event.Instance.PageCount - 1);
            Event.CurPageNum = tabPages.SelectedIndex;
            Event.EventEditorLoadPage(Event.CurPageNum);
            RefreshGraphicControlsFromPage();
            AttachEditorHostToSelectedTab();
            SyncMoveRouteButton();
        }

        private void BtnCopyPage_Click(object? sender, EventArgs e)
        {
            Event.CopyEventPage = Event.Instance.Pages[Event.CurPageNum];
            btnPastePage.Enabled = true;
        }

        private void BtnPastePage_Click(object? sender, EventArgs e)
        {
            Event.Instance.Pages[Event.CurPageNum] = Event.CopyEventPage;
            Event.EventEditorLoadPage(Event.CurPageNum);
            RefreshGraphicControlsFromPage();
            AttachEditorHostToSelectedTab();
            SyncMoveRouteButton();
        }

        private void BtnDeletePage_Click(object? sender, EventArgs e)
        {
            Event.Instance.Pages[Event.CurPageNum] = default;

            // move everything else down a notch
            if (Event.CurPageNum < Event.Instance.PageCount)
            {
                for (int i = Event.CurPageNum, loopTo = Event.Instance.PageCount - 1; i < loopTo; i++)
                    Event.Instance.Pages[i] = Event.Instance.Pages[i + 1];
            }
            Event.Instance.PageCount = Event.Instance.PageCount - 1;
            Event.CurPageNum = Event.Instance.PageCount - 1;
            Event.EventEditorLoadPage(Event.CurPageNum);

            // set the tabs
            tabPages.Pages.Clear();

            for (int i = 0, loopTo1 = Event.Instance.PageCount; i < loopTo1; i++)
                tabPages.Pages.Add(new TabPage { Text = Conversion.Str(i + 1) });

            // set the tab back
            tabPages.SelectedIndex = Math.Min(Event.CurPageNum, Math.Max(0, Event.Instance.PageCount - 1));
            Event.CurPageNum = tabPages.SelectedIndex;
            Event.EventEditorLoadPage(Event.CurPageNum);
            RefreshGraphicControlsFromPage();
            // make sure we disable
            if (Event.Instance.PageCount == 1)
            {
                btnDeletePage.Enabled = false;
            }
            AttachEditorHostToSelectedTab();
            SyncMoveRouteButton();

        }

        private void BtnClearPage_Click(object? sender, EventArgs e)
        {
            Event.Instance.Pages[Event.CurPageNum] = default;
            Event.EventEditorLoadPage(Event.CurPageNum);
        }

        private void TxtName_TextChanged(object? sender, EventArgs e)
        {
            Event.Instance.Name = Strings.Trim(txtName.Text);
        }

        #endregion

        #region Conditions

        private void ChkPlayerVar_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkPlayerVar.Checked == true)
            {
                cmbPlayerVar.Enabled = true;
                nudPlayerVariable.Enabled = true;
                cmbPlayerVarCompare.Enabled = true;
                Event.Instance.Pages[Event.CurPageNum].ChkVariable = 1;
            }
            else
            {
                cmbPlayerVar.Enabled = false;
                nudPlayerVariable.Enabled = false;
                cmbPlayerVarCompare.Enabled = false;
                Event.Instance.Pages[Event.CurPageNum].ChkVariable = 0;
            }
        }

        private void CmbPlayerVar_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbPlayerVar.SelectedIndex == -1)
                return;
            Event.Instance.Pages[Event.CurPageNum].VariableIndex = cmbPlayerVar.SelectedIndex;
        }

        private void CmbPlayervarCompare_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbPlayerVarCompare.SelectedIndex == -1)
                return;
            Event.Instance.Pages[Event.CurPageNum].VariableCompare = cmbPlayerVarCompare.SelectedIndex;
        }

        private void NudPlayerVariable_ValueChanged(object? sender, EventArgs e)
        {
            Event.Instance.Pages[Event.CurPageNum].VariableCondition = (int)Math.Round(nudPlayerVariable.Value);
        }

        private void ChkPlayerSwitch_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkPlayerSwitch.Checked == true)
            {
                cmbPlayerSwitch.Enabled = true;
                cmbPlayerSwitchCompare.Enabled = true;
                Event.Instance.Pages[Event.CurPageNum].ChkSwitch = 1;
            }
            else
            {
                cmbPlayerSwitch.Enabled = false;
                cmbPlayerSwitchCompare.Enabled = false;
                Event.Instance.Pages[Event.CurPageNum].ChkSwitch = 0;
            }
        }

        private void CmbPlayerSwitch_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbPlayerSwitch.SelectedIndex == -1)
                return;
            Event.Instance.Pages[Event.CurPageNum].SwitchIndex = cmbPlayerSwitch.SelectedIndex;
        }

        private void CmbPlayerSwitchCompare_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbPlayerSwitchCompare.SelectedIndex == -1)
                return;
            Event.Instance.Pages[Event.CurPageNum].SwitchCompare = cmbPlayerSwitchCompare.SelectedIndex;
        }

        private void ChkHasItem_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkHasItem.Checked == true)
            {
                Event.Instance.Pages[Event.CurPageNum].ChkHasItem = 1;
                cmbHasItem.Enabled = true;
            }
            else
            {
                Event.Instance.Pages[Event.CurPageNum].ChkHasItem = 0;
                cmbHasItem.Enabled = false;
            }

        }

        private void CmbHasItem_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbHasItem.SelectedIndex == -1)
                return;
            Event.Instance.Pages[Event.CurPageNum].HasItemIndex = cmbHasItem.SelectedIndex;
            Event.Instance.Pages[Event.CurPageNum].HasItemAmount = (int)Math.Round(nudCondition_HasItem.Value);
        }

        private void ChkSelfSwitch_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkSelfSwitch.Checked == true)
            {
                cmbSelfSwitch.Enabled = true;
                cmbSelfSwitchCompare.Enabled = true;
                Event.Instance.Pages[Event.CurPageNum].ChkSelfSwitch = 1;
            }
            else
            {
                cmbSelfSwitch.Enabled = false;
                cmbSelfSwitchCompare.Enabled = false;
                Event.Instance.Pages[Event.CurPageNum].ChkSelfSwitch = 0;
            }
        }

        private void CmbSelfSwitch_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbSelfSwitch.SelectedIndex == -1)
                return;

            if (Event.Instance.Pages == null)
                return;

            Event.Instance.Pages[Event.CurPageNum].SelfSwitchIndex = cmbSelfSwitch.SelectedIndex;
        }

        private void CmbSelfSwitchCompare_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbSelfSwitchCompare.SelectedIndex == -1)
                return;
            Event.Instance.Pages[Event.CurPageNum].SelfSwitchCompare = cmbSelfSwitchCompare.SelectedIndex;
        }

        #endregion

        #region Graphic

    private void PicGraphic_Click(object? sender, MouseEventArgs e)
        {
            tmpGraphicIndex = Event.Instance.Pages[Event.CurPageNum].Graphic;
            tmpGraphicType = Event.Instance.Pages[Event.CurPageNum].GraphicType;
            // Show the Set Graphic frame via the overlay host
            HideAllFrames();
            ShowFrame(fraGraphic, false);
            Event.GraphicSelType = 0;
            // Render selection sheet so user can click a frame
            DrawGraphicSelectionPreview();
        }

        private void CmbGraphic_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbGraphic.SelectedIndex == -1)
                return;
                
            if (_syncingGraphic) return;

            Event.Instance.Pages[Event.CurPageNum].GraphicType = (byte)cmbGraphic.SelectedIndex;
            // set the max on the scrollbar
            switch (cmbGraphic.SelectedIndex)
            {
                case 0: // None
                    {
                        nudGraphic.Enabled = false;
                        break;
                    }
                case 1: // character
                    {
                        nudGraphic.MinValue = 0;
                        nudGraphic.MaxValue = GameState.NumCharacters;
                        nudGraphic.Enabled = true;
                        break;
                    }
                case 2: // Tileset
                    {
                        nudGraphic.MinValue = 0;
                        nudGraphic.MaxValue = GameState.NumTileSets;
                        nudGraphic.Enabled = true;
                        break;
                    }
            }

            if (Event.Instance.Pages[Event.CurPageNum].GraphicType == 1)
            {
                if (nudGraphic.Value <= 0 | nudGraphic.Value > GameState.NumCharacters)
                    return;
            }

            else if (Event.Instance.Pages[Event.CurPageNum].GraphicType == 2)
            {
                if (nudGraphic.Value <= 0 | nudGraphic.Value > GameState.NumTileSets)
                    return;

            }
            DrawGraphic();
        }

        private void PicGraphicSel_MouseDown(object? sender, MouseEventArgs e)
        {
            int X;
            int Y;

            X = (int)e.Location.X;
            Y = (int)e.Location.Y;
            // Enforce minimum index 0 on raw selection
            if (X < 0) X = 0;
            if (Y < 0) Y = 0;

            int selW = (int)Math.Round(Math.Ceiling((decimal)(X)) - Event.GraphicSelX);
            int selH = (int)Math.Round(Math.Ceiling((decimal)(Y)) - Event.GraphicSelY);

            if (cmbGraphic.SelectedIndex == 2)
            {
                // Multi-tile (shift-mod) selection not yet implemented in Eto. Single-tile select:
                Event.GraphicSelX = System.Math.Max(0, (int)System.Math.Round(System.Math.Ceiling((decimal)(X))));
                Event.GraphicSelY = System.Math.Max(0, (int)System.Math.Round(System.Math.Ceiling((decimal)(Y))));
                Event.GraphicSelX2 = 1;
                Event.GraphicSelY2 = 1;
            }
            else if (cmbGraphic.SelectedIndex == 1)
            {
                Event.GraphicSelX = System.Math.Max(0, X);
                Event.GraphicSelY = System.Math.Max(0, Y);
                Event.GraphicSelX2 = 0;
                Event.GraphicSelY2 = 0;

                if (nudGraphic.Value <= 0 | nudGraphic.Value > GameState.NumCharacters)
                    return;

                var gfxInfo = GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Characters, nudGraphic.Value.ToString()));
                int dirs = ComputeDirectionRows(gfxInfo.Height);
                // Compute frame dimensions heuristically using animation settings
                int idleFrames = Math.Max(1, SettingsManager.Instance.IdleFrames);
                int runFrames = Math.Max(1, SettingsManager.Instance.RunFrames);
                int attackFrames = Math.Max(1, SettingsManager.Instance.AttackFrames);
                int expectedCols = idleFrames + runFrames + attackFrames;
                int frameWidth;
                if (expectedCols > 0 && gfxInfo.Width % expectedCols == 0)
                    frameWidth = gfxInfo.Width / expectedCols;
                else if (gfxInfo.Width % idleFrames == 0)
                    frameWidth = gfxInfo.Width / idleFrames;
                else
                {
                    // fallback approximate square frames
                    int approx = (gfxInfo.Height / dirs) > 0 ? gfxInfo.Width / (gfxInfo.Height / dirs) : gfxInfo.Width;
                    if (approx <= 0) approx = 1;
                    frameWidth = gfxInfo.Width / approx;
                }
                int frameHeight = gfxInfo.Height / dirs;
                if (frameWidth <= 0) frameWidth = gfxInfo.Width;
                if (frameHeight <= 0) frameHeight = gfxInfo.Height;

                // Determine column index
                int col = Event.GraphicSelX / Math.Max(1, frameWidth);
                if (col < 0) col = 0;
                int maxCols = Math.Max(1, gfxInfo.Width / Math.Max(1, frameWidth));
                if (col >= maxCols) col = maxCols - 1;
                Event.GraphicSelX = col;

                // Determine row (direction) index
                int row = Event.GraphicSelY / Math.Max(1, frameHeight);
                if (row < 0) row = 0;
                if (row >= dirs) row = dirs - 1;
                Event.GraphicSelY = row;
            }
            DrawGraphic();
            // Also refresh the full-sheet display (not strictly needed for single-frame highlighting)
            // but ensures any size changes are applied
            DrawGraphicSelectionPreview();
        }

        private void nudGraphic_ValueChanged(object? sender, EventArgs e)
        {
            if (!_syncingGraphic)
            {
                // Persist to page only when this is a user-driven change
                Event.Instance.Pages[Event.CurPageNum].Graphic = (int)Math.Round(nudGraphic.Value);
            }
            DrawGraphic();
            DrawGraphicSelectionPreview();
        }

        #endregion

        // Helper to compute direction row count with fallback heuristics
        private static int ComputeDirectionRows(int bmpHeight)
        {
            int configured = SettingsManager.Instance.SpriteDirections <= 0 ? 4 : SettingsManager.Instance.SpriteDirections;
            configured = Math.Max(1, configured);
            if (bmpHeight <= 0) return 1;
            if (bmpHeight % configured == 0) return configured;
            if (configured != 8 && bmpHeight % 8 == 0) return 8;
            if (configured != 4 && bmpHeight % 4 == 0) return 4;
            return 1;
        }

        #region Movement

        private void CmbMoveType_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbMoveType.SelectedIndex == -1)
                return;
            Event.Instance.Pages[Event.CurPageNum].MoveType = (byte)cmbMoveType.SelectedIndex;
            SyncMoveRouteButton();
        }

        // Helper: keep Move Route button in sync with current Move Type
        private void SyncMoveRouteButton()
        {
            try
            {
                // Always allow opening the Move Route editor; if not in Route mode,
                // we'll switch the page to Route when the user clicks the button.
                btnMoveRoute.Enabled = true;
                try
                {
                    btnMoveRoute.ToolTip = (cmbMoveType.SelectedIndex == 2)
                        ? "Edit this page's move route."
                        : "Click to switch Move Type to 'Route' and edit the move route.";
                }
                catch { }
            }
            catch { }
        }

        // Ensure cmbGraphic/nudGraphic reflect the current page and preview is updated
        private void RefreshGraphicControlsFromPage()
        {
            try
            {
                if (Event.Instance.Pages == null || Event.CurPageNum < 0 || Event.CurPageNum >= Event.Instance.Pages.Length)
                    return;
                var page = Event.Instance.Pages[Event.CurPageNum];

                _syncingGraphic = true;
                try
                {
                    // Set type first to update ranges via SelectedIndexChanged handler
                    cmbGraphic.SelectedIndex = page.GraphicType;
                    // Ensure max matches the type before value assignment (defensive)
                    switch (page.GraphicType)
                    {
                        case 1: // Character
                            nudGraphic.MaxValue = GameState.NumCharacters;
                            break;
                        case 2: // Tileset
                            nudGraphic.MaxValue = GameState.NumTileSets;
                            break;
                        default:
                            break;
                    }
                    // Clamp and set value
                    var min = (int)Math.Round(nudGraphic.MinValue);
                    var max = (int)Math.Round(nudGraphic.MaxValue);
                    var val = page.Graphic;
                    if (max > 0)
                        val = Math.Max(min, Math.Min(max, val));
                    nudGraphic.Value = val;
                }
                finally
                {
                    _syncingGraphic = false;
                }

                // Update both previews after sync
                DrawGraphic();
                DrawGraphicSelectionPreview();
            }
            catch { }
        }

        private void BtnMoveRoute_Click(object? sender, EventArgs e)
        {
            // Ensure the page is in Route mode so the route is actually used
            try
            {
                if (cmbMoveType.SelectedIndex != 2)
                {
                    cmbMoveType.SelectedIndex = 2;
                    try { Event.Instance.Pages[Event.CurPageNum].MoveType = (byte)2; } catch { }
                }
            }
            catch { }
            // BringToFront removed for Eto
            lstMoveRoute.Items.Clear();
            Event.IsMoveRouteCommand = false;
            chkIgnoreMove.Checked = Conversions.ToBoolean(Event.Instance.Pages[Event.CurPageNum].IgnoreMoveRoute);
            chkRepeatRoute.Checked = Conversions.ToBoolean(Event.Instance.Pages[Event.CurPageNum].RepeatMoveRoute);
            Event.TempMoveRouteCount = Event.Instance.Pages[Event.CurPageNum].MoveRouteCount;

            // Will it let me do this?
            Event.TempMoveRoute = Event.Instance.Pages[Event.CurPageNum].MoveRoute;
            for (int i = 0, loopTo = Event.TempMoveRouteCount; i < loopTo; i++)
            {
                switch (Event.TempMoveRoute[i].Index)
                {
                    case 1:
                        {
                            lstMoveRoute.Items.Add("Move Up");
                            break;
                        }
                    case 2:
                        {
                            lstMoveRoute.Items.Add("Move Down");
                            break;
                        }
                    case 3:
                        {
                            lstMoveRoute.Items.Add("Move Left");
                            break;
                        }
                    case 4:
                        {
                            lstMoveRoute.Items.Add("Move Right");
                            break;
                        }
                    case 5:
                        {
                            lstMoveRoute.Items.Add("Move Randomly");
                            break;
                        }
                    case 6:
                        {
                            lstMoveRoute.Items.Add("Move Towards Player");
                            break;
                        }
                    case 7:
                        {
                            lstMoveRoute.Items.Add("Move Away From Player");
                            break;
                        }
                    case 8:
                        {
                            lstMoveRoute.Items.Add("Step Forward");
                            break;
                        }
                    case 9:
                        {
                            lstMoveRoute.Items.Add("Step Back");
                            break;
                        }
                    case 10:
                        {
                            lstMoveRoute.Items.Add("Wait 100ms");
                            break;
                        }
                    case 11:
                        {
                            lstMoveRoute.Items.Add("Wait 500ms");
                            break;
                        }
                    case 12:
                        {
                            lstMoveRoute.Items.Add("Wait 1000ms");
                            break;
                        }
                    case 13:
                        {
                            lstMoveRoute.Items.Add("Turn Up");
                            break;
                        }
                    case 14:
                        {
                            lstMoveRoute.Items.Add("Turn Down");
                            break;
                        }
                    case 15:
                        {
                            lstMoveRoute.Items.Add("Turn Left");
                            break;
                        }
                    case 16:
                        {
                            lstMoveRoute.Items.Add("Turn Right");
                            break;
                        }
                    case 17:
                        {
                            lstMoveRoute.Items.Add("Turn 90 Degrees To the Right");
                            break;
                        }
                    case 18:
                        {
                            lstMoveRoute.Items.Add("Turn 90 Degrees To the Left");
                            break;
                        }
                    case 19:
                        {
                            lstMoveRoute.Items.Add("Turn Around 180 Degrees");
                            break;
                        }
                    case 20:
                        {
                            lstMoveRoute.Items.Add("Turn Randomly");
                            break;
                        }
                    case 21:
                        {
                            lstMoveRoute.Items.Add("Turn Towards Player");
                            break;
                        }
                    case 22:
                        {
                            lstMoveRoute.Items.Add("Turn Away from Player");
                            break;
                        }
                    case 23:
                        {
                            lstMoveRoute.Items.Add("Set Speed 8x Slower");
                            break;
                        }
                    case 24:
                        {
                            lstMoveRoute.Items.Add("Set Speed 4x Slower");
                            break;
                        }
                    case 25:
                        {
                            lstMoveRoute.Items.Add("Set Speed 2x Slower");
                            break;
                        }
                    case 26:
                        {
                            lstMoveRoute.Items.Add("Set Speed to Normal");
                            break;
                        }
                    case 27:
                        {
                            lstMoveRoute.Items.Add("Set Speed 2x Faster");
                            break;
                        }
                    case 28:
                        {
                            lstMoveRoute.Items.Add("Set Speed 4x Faster");
                            break;
                        }
                    case 29:
                        {
                            lstMoveRoute.Items.Add("Set Frequency Lowest");
                            break;
                        }
                    case 30:
                        {
                            lstMoveRoute.Items.Add("Set Frequency Lower");
                            break;
                        }
                    case 31:
                        {
                            lstMoveRoute.Items.Add("Set Frequency Normal");
                            break;
                        }
                    case 32:
                        {
                            lstMoveRoute.Items.Add("Set Frequency Higher");
                            break;
                        }
                    case 33:
                        {
                            lstMoveRoute.Items.Add("Set Frequency Highest");
                            break;
                        }
                    case 34:
                        {
                            lstMoveRoute.Items.Add("Turn On Walking Animation");
                            break;
                        }
                    case 35:
                        {
                            lstMoveRoute.Items.Add("Turn Off Walking Animation");
                            break;
                        }
                    case 36:
                        {
                            lstMoveRoute.Items.Add("Turn On Fixed Direction");
                            break;
                        }
                    case 37:
                        {
                            lstMoveRoute.Items.Add("Turn Off Fixed Direction");
                            break;
                        }
                    case 38:
                        {
                            lstMoveRoute.Items.Add("Turn On Walk Through");
                            break;
                        }
                    case 39:
                        {
                            lstMoveRoute.Items.Add("Turn Off Walk Through");
                            break;
                        }
                    case 40:
                        {
                            lstMoveRoute.Items.Add("Set Position Below Player");
                            break;
                        }
                    case 41:
                        {
                            lstMoveRoute.Items.Add("Set Position at Player Level");
                            break;
                        }
                    case 42:
                        {
                            lstMoveRoute.Items.Add("Set Position Above Player");
                            break;
                        }
                    case 43:
                        {
                            lstMoveRoute.Items.Add("Set Graphic");
                            break;
                        }
                }
            }

            fraMoveRoute.Visible = true;

        }

        private void CmbMoveSpeed_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbMoveSpeed.SelectedIndex == -1)
                return;
            Event.Instance.Pages[Event.CurPageNum].MoveSpeed = (byte)cmbMoveSpeed.SelectedIndex;
        }

        private void CmbMoveFreq_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbMoveFreq.SelectedIndex == -1)
                return;
            Event.Instance.Pages[Event.CurPageNum].MoveFreq = (byte)cmbMoveFreq.SelectedIndex;
        }

        #endregion

        #region Positioning

        private void CmbPositioning_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (Event.Instance.Pages == null)
                return;

            if (cmbPositioning.SelectedIndex == -1)
                return;

            Event.Instance.Pages[Event.CurPageNum].Position = (byte)cmbPositioning.SelectedIndex;
        }

        #endregion

        #region Trigger

        private void CmbTrigger_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (Event.Instance.Pages == null)
                return;

            if (cmbTrigger.SelectedIndex == -1)
                return;
            Event.Instance.Pages[Event.CurPageNum].Trigger = (byte)cmbTrigger.SelectedIndex;
        }

        private void ChkGlobal_CheckedChanged(object? sender, EventArgs e)
        {
            if (IsSyncingPageUI) return; // ignore programmatic updates
            if (Event.Instance.PageCount > 0)
            {
                if (MessageBox.Show("If you set the event to global you will lose all pages except for your first one. Do you want to continue?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
            }
            if (chkGlobal.Checked == true)
            {
                Event.Instance.Globals = 1;
            }
            else
            {
                Event.Instance.Globals = 0;
            }

            Event.Instance.PageCount = 1;
            Event.CurPageNum = 0;
            tabPages.Pages.Clear();

            for (int i = 0, loopTo = Event.Instance.PageCount; i < loopTo; i++)
                tabPages.Pages.Add(new TabPage { Text = (i + 1).ToString() });
            // Always select the first page when globalizing and load it
            tabPages.SelectedIndex = 0;
            Event.CurPageNum = 0;
            Event.EventEditorLoadPage(Event.CurPageNum);
            RefreshGraphicControlsFromPage();
            SyncMoveRouteButton();
        }

        #endregion

        #region Options

        private void ChkWalkAnim_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkWalkAnim.Checked == true)
            {
                Event.Instance.Pages[Event.CurPageNum].IdleAnim = 1;
            }
            else
            {
                Event.Instance.Pages[Event.CurPageNum].IdleAnim = 0;
            }

        }

        private void ChkDirFix_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkDirFix.Checked == true)
            {
                Event.Instance.Pages[Event.CurPageNum].DirFix = 1;
            }
            else
            {
                Event.Instance.Pages[Event.CurPageNum].DirFix = 0;
            }

        }

        private void ChkWalkThrough_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkWalkThrough.Checked == true)
            {
                Event.Instance.Pages[Event.CurPageNum].WalkThrough = 1;
            }
            else
            {
                Event.Instance.Pages[Event.CurPageNum].WalkThrough = 0;
            }

        }

        private void ChkShowName_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkShowName.Checked == true)
            {
                Event.Instance.Pages[Event.CurPageNum].ShowName = 1;
            }
            else
            {
                Event.Instance.Pages[Event.CurPageNum].ShowName = 0;
            }

        }

        #endregion

        #region Commands

        private void LstCommands_SelectedIndexChanged(object? sender, EventArgs e)
        {
            Event.CurCommand = lstCommands.SelectedIndex;
        }

        private void BtnAddCommand_Click(object? sender, EventArgs e)
        {
            // Trigger the currently selected palette action; if none selected, focus palette
            Event.IsEdit = false;
            if (tvCommands.SelectedItem == null)
            {
                // If nothing is selected, auto-select the first available command leaf
                try
                {
                    if (tvCommands.DataStore is TreeGridItemCollection root && root.Count > 0)
                    {
                        TreeGridItem? FindFirstLeaf(TreeGridItem node)
                        {
                            if (node.Children == null || node.Children.Count == 0)
                                return node;
                            var child = node.Children[0] as TreeGridItem;
                            return child != null ? FindFirstLeaf(child) : null;
                        }
                        var firstCat = root[0] as TreeGridItem;
                        var leaf = firstCat != null ? FindFirstLeaf(firstCat) : null;
                        if (leaf != null)
                        {
                            tvCommands.SelectedItem = leaf; // SelectionChanged will execute
                            return;
                        }
                    }
                }
                catch { }
                tvCommands.Focus();
                return;
            }

            // If a category is selected, select the first leaf under it
            bool changedSelection = false;
            if (tvCommands.SelectedItem is TreeGridItem sel)
            {
                TreeGridItem? FindFirstLeaf(TreeGridItem node)
                {
                    if (node.Children == null || node.Children.Count == 0)
                        return node;
                    // descend to first child recursively
                    var child = node.Children[0] as TreeGridItem;
                    return child != null ? FindFirstLeaf(child) : null;
                }

                if (sel.Children != null && sel.Children.Count > 0)
                {
                    var leaf = FindFirstLeaf(sel);
                    if (leaf != null && !ReferenceEquals(leaf, sel))
                    {
                        tvCommands.SelectedItem = leaf;
                        changedSelection = true; // SelectionChanged will trigger handler
                    }
                }
            }

            if (!changedSelection)
            {
                // If we didn't change selection (already a leaf), execute now
                TvCommands_AfterSelect(tvCommands, EventArgs.Empty);
            }                 
        }                                                        

        private void BtnEditCommand_Click(object? sender, EventArgs e)
        {
            // Invoke legacy edit logic to populate fields
            Event.EditEventCommand();
            // Bridge legacy visibility flags to the new frameHost flow
            try
            {
                // If any known frame panel is visible from legacy code, re-show through ShowFrame
                Panel? visible = null;
                if (fraShowText.Visible) visible = fraShowText;
                else if (fraShowChoices.Visible) visible = fraShowChoices;
                else if (fraAddText.Visible) visible = fraAddText;
                else if (fraShowChatBubble.Visible) visible = fraShowChatBubble;
                else if (fraPlayerVariable.Visible) visible = fraPlayerVariable;
                else if (fraPlayerSwitch.Visible) visible = fraPlayerSwitch;
                else if (fraSetSelfSwitch.Visible) visible = fraSetSelfSwitch;
                else if (fraConditionalBranch.Visible) visible = fraConditionalBranch;
                else if (fraCreateLabel.Visible) visible = fraCreateLabel;
                else if (fraGoToLabel.Visible) visible = fraGoToLabel;
                else if (fraChangeItems.Visible) visible = fraChangeItems;
                else if (fraChangeLevel.Visible) visible = fraChangeLevel;
                else if (fraChangeSkills.Visible) visible = fraChangeSkills;
                else if (fraChangeJob.Visible) visible = fraChangeJob;
                else if (fraChangeSprite.Visible) visible = fraChangeSprite;
                else if (fraChangeGender.Visible) visible = fraChangeGender;
                else if (fraChangePK.Visible) visible = fraChangePK;
                else if (fraPlayerWarp.Visible) visible = fraPlayerWarp;
                else if (fraMoveRoute.Visible) visible = fraMoveRoute;
                else if (fraMoveRouteWait.Visible) visible = fraMoveRouteWait;
                else if (fraSpawnNpc.Visible) visible = fraSpawnNpc;
                else if (fraPlayAnimation.Visible) visible = fraPlayAnimation;
                else if (fraSetFog.Visible) visible = fraSetFog;
                else if (fraSetWeather.Visible) visible = fraSetWeather;
                else if (fraMapTint.Visible) visible = fraMapTint;
                else if (fraPlayBGM.Visible) visible = fraPlayBGM;
                else if (fraPlaySound.Visible) visible = fraPlaySound;
                else if (fraShowPic.Visible) visible = fraShowPic;

                if (visible != null)
                {
                    // Hide legacy flags to avoid duplicate visibility and show via new host
                    HideAllFrames();
                    ShowFrame(visible, useDialogueWrapper: true);
                }
            }
            catch { }
        }

        private void BtnDeleteComand_Click(object? sender, EventArgs e)
        {
            Event.DeleteEventCommand();
        }

        private void BtnClearCommand_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear all event commands?", "Clear Event Commands?", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Event.ClearEventCommands();
            }
        }

        #endregion

        #region Variables/Switches

        // 'Renaming Variables/Switches
        private void BtnLabeling_Click(object? sender, EventArgs e)
        {
            // Populate lists fresh
            lstSwitches.Items.Clear();
            for (int i = 0; i < Variables.MaxSwitches; i++)
                lstSwitches.Items.Add((i + 1) + ". " + Event.Switches[i]);
            lstVariables.Items.Clear();
            for (int i = 0; i < Variables.MaxVariables; i++)
                lstVariables.Items.Add((i + 1) + ". " + Event.Variables[i]);

            // Reset substate and show via unified overlay flow
            FraRenaming.Visible = false;
            fraLabeling.Visible = true;
            ShowFrame(pnlVariableSwitches, false);
        }

        private void BtnRename_Ok_Click(object? sender, EventArgs e)
        {
            FraRenaming.Visible = false;
            fraLabeling.Visible = true;

            switch (Event.RenameType)
            {
                case 1:
                    {
                        // Variable
                        if (Event.RenameIndex >= 0 & Event.RenameIndex < Variables.MaxVariables)
                        {
                            Event.Variables[Event.RenameIndex] = txtRename.Text;
                            FraRenaming.Visible = false;
                            fraLabeling.Visible = true;
                            Event.RenameType = 0;
                            Event.RenameIndex = 0;
                        }

                        break;
                    }
                case 2:
                    {
                        // Switch
                        if (Event.RenameIndex >= 0 & Event.RenameIndex < Variables.MaxSwitches)
                        {
                            Event.Switches[Event.RenameIndex] = txtRename.Text;
                            FraRenaming.Visible = false;
                            fraLabeling.Visible = true;
                            Event.RenameType = 0;
                            Event.RenameIndex = 0;
                        }

                        break;
                    }
            }
            // Refresh all places where switch/variable names appear
            RefreshSwitchAndVariableUI();
            // Restore command palette after rename
            try { HideAllFrames(); } catch { }
        }

        private void BtnRename_Cancel_Click(object? sender, EventArgs e)
        {
            FraRenaming.Visible = false;
            fraLabeling.Visible = true;

            Event.RenameType = 0;
            Event.RenameIndex = 0;
            lstSwitches.Items.Clear();

            for (int i = 0; i < Variables.MaxSwitches; i++)
                lstSwitches.Items.Add((i + 1).ToString() + ". " + Event.Switches[i]);
            lstSwitches.SelectedIndex = 0;
            lstVariables.Items.Clear();

            for (int i = 0; i < Variables.MaxVariables; i++)
                lstVariables.Items.Add((i + 1).ToString() + ". " + Event.Variables[i]);
            lstVariables.SelectedIndex = 0;
            // Restore command palette on cancel
            try { HideAllFrames(); } catch { }
        }

        private void TxtRename_TextChanged(object? sender, EventArgs e)
        {
            Event.Instance.Name = Strings.Trim(txtName.Text);
        }

        private void LstVariables_DoubleClick(object? sender, MouseEventArgs e)
        {
            if (lstVariables.SelectedIndex > -1 & lstVariables.SelectedIndex < Variables.MaxVariables)
            {
                FraRenaming.Visible = true;
                fraLabeling.Visible = false;
                lblEditing.Text = "Editing Variable: " + (lstVariables.SelectedIndex + 1).ToString();
                txtRename.Text = Event.Variables[lstVariables.SelectedIndex];
                Event.RenameType = 1;
                Event.RenameIndex = lstVariables.SelectedIndex;
            }
        }

        private void LstSwitches_DoubleClick(object? sender, MouseEventArgs e)
        {
            if (lstSwitches.SelectedIndex > -1 & lstSwitches.SelectedIndex < Variables.MaxSwitches)
            {
                FraRenaming.Visible = true;
                fraLabeling.Visible = false;
                lblEditing.Text = "Editing Switch: " + (lstSwitches.SelectedIndex + 1).ToString();
                txtRename.Text = Event.Switches[lstSwitches.SelectedIndex];
                Event.RenameType = 2;
                Event.RenameIndex = lstSwitches.SelectedIndex;
            }
        }

        private void BtnRenameVariable_Click(object? sender, EventArgs e)
        {
            if (lstVariables.SelectedIndex < 0 && lstVariables.Items.Count > 0)
                lstVariables.SelectedIndex = 0;
            if (lstVariables.SelectedIndex > -1 & lstVariables.SelectedIndex < Variables.MaxVariables)
            {
                FraRenaming.Visible = true;
                fraLabeling.Visible = false;
                lblEditing.Text = "Editing Variable: " + (lstVariables.SelectedIndex + 1).ToString();
                txtRename.Text = Event.Variables[lstVariables.SelectedIndex];
                Event.RenameType = 1;
                Event.RenameIndex = lstVariables.SelectedIndex;
            }
        }

        private void BtnRenameSwitch_Click(object? sender, EventArgs e)
        {
            if (lstSwitches.SelectedIndex < 0 && lstSwitches.Items.Count > 0)
                lstSwitches.SelectedIndex = 0;
            if (lstSwitches.SelectedIndex > -1 & lstSwitches.SelectedIndex < Variables.MaxSwitches)
            {
                FraRenaming.Visible = true;
                fraLabeling.Visible = false;
                lblEditing.Text = "Editing Switch: " + (lstSwitches.SelectedIndex + 1).ToString();
                txtRename.Text = Event.Switches[lstSwitches.SelectedIndex];
                Event.RenameType = 2;
                Event.RenameIndex = lstSwitches.SelectedIndex;
            }
        }

        private void BtnLabel_Ok_Click(object? sender, EventArgs e)
        {
            Sender.SendSwitchesAndVariables();

            // Ensure UI reflects latest names after save
            RefreshSwitchAndVariableUI();

            // Close the Switches/Variables panel after saving
            try
            {
                FraRenaming.Visible = false;
                fraLabeling.Visible = true;
                HideAllFrames();
            }
            catch { }
        }

        private void BtnLabel_Cancel_Click(object? sender, EventArgs e)
        {
            Sender.SendRequestSwitchesAndVariables();

            // Revert UI to a clean state and close the panel
            try
            {
                FraRenaming.Visible = false;
                fraLabeling.Visible = true;
                HideAllFrames();
                // Reset any rename context
                Event.RenameType = 0;
                Event.RenameIndex = 0;
            }
            catch { }
        }

        // Refresh UI elements that display switch/variable names
        private void RefreshSwitchAndVariableUI()
        {
            // Lists
            var prevSwitchListIdx = lstSwitches.SelectedIndex;
            var prevVarListIdx = lstVariables.SelectedIndex;

            lstSwitches.Items.Clear();
            for (int i = 0; i < Variables.MaxSwitches; i++)
                lstSwitches.Items.Add((i + 1) + ". " + Strings.Trim(Event.Switches[i]));
            if (lstSwitches.Items.Count > 0)
                lstSwitches.SelectedIndex = (prevSwitchListIdx >= 0 && prevSwitchListIdx < lstSwitches.Items.Count) ? prevSwitchListIdx : 0;

            lstVariables.Items.Clear();
            for (int i = 0; i < Variables.MaxVariables; i++)
                lstVariables.Items.Add((i + 1) + ". " + Strings.Trim(Event.Variables[i]));
            if (lstVariables.Items.Count > 0)
                lstVariables.SelectedIndex = (prevVarListIdx >= 0 && prevVarListIdx < lstVariables.Items.Count) ? prevVarListIdx : 0;

            // Helper local function to refresh a combo with 1-based indexed names
            void RefreshCombo(ComboBox combo, string[] names)
            {
                int prev = combo.SelectedIndex;
                combo.Items.Clear();
                for (int i = 0; i < names.Length; i++)
                    combo.Items.Add((i + 1) + ". " + Strings.Trim(names[i]));
                if (combo.Items.Count > 0)
                    combo.SelectedIndex = (prev >= 0 && prev < combo.Items.Count) ? prev : 0;
            }

            // Variables combos
            RefreshCombo(cmbVariable, Event.Variables);
            RefreshCombo(cmbPlayerVar, Event.Variables);
            // Conditions variable index combo
            RefreshCombo(cmbCondition_PlayerVarIndex, Event.Variables);

            // Switch combos
            RefreshCombo(cmbSwitch, Event.Switches);
            RefreshCombo(cmbPlayerSwitch, Event.Switches);
            // Conditions switch combo
            RefreshCombo(cmbCondition_PlayerSwitch, Event.Switches);
        }

        // MoveRoute Commands
        private void LstvwMoveRoute_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Eto ListBox: single SelectedIndex
            if (lstvwMoveRoute.SelectedIndex < 0)
                return;
            int selectedIndex = lstvwMoveRoute.SelectedIndex;

            switch (selectedIndex + 1)
            {
                // Set Graphic
                case 43:
                    {
                        // Show the Set Graphic frame and mark that selection applies to a route command
                        Event.GraphicSelType = 1;
                        HideAllFrames();
                        ShowFrame(fraGraphic, false);
                        DrawGraphicSelectionPreview();
                        break;
                    }

                default:
                    {
                        AddMoveRouteCommand(selectedIndex);
                        break;
                    }
            }
        }

        private void LstMoveRoute_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Delete)
            {
                // remove move route command lol
                if (lstMoveRoute.SelectedIndex > -1)
                {
                    RemoveMoveRouteCommand(lstMoveRoute.SelectedIndex);
                }
            }
        }

        public void AddMoveRouteCommand(int Index)
        {
            int i;
            int X;

            Index = Index + 1;
            if (lstMoveRoute.SelectedIndex > -1)
            {
                i = lstMoveRoute.SelectedIndex;
                Event.TempMoveRouteCount += 1;
                Array.Resize(ref Event.TempMoveRoute, Event.TempMoveRouteCount);
                var loopTo = i;
                for (X = Event.TempMoveRouteCount; X > loopTo; X -= 1)
                    Event.TempMoveRoute[X + 1] = Event.TempMoveRoute[X];
                Event.TempMoveRoute[i].Index = Index;
                // if set graphic then...
                if (Index == 43)
                {
                    Event.TempMoveRoute[i].Data1 = cmbGraphic.SelectedIndex;
                    Event.TempMoveRoute[i].Data2 = (int)Math.Round(nudGraphic.Value);
                    Event.TempMoveRoute[i].Data3 = Event.GraphicSelX;
                    Event.TempMoveRoute[i].Data4 = Event.GraphicSelX2;
                    Event.TempMoveRoute[i].Data5 = Event.GraphicSelY;
                    Event.TempMoveRoute[i].Data6 = Event.GraphicSelY2;
                }
                PopulateMoveRouteList();
            }
            else
            {
                Event.TempMoveRouteCount += 1;
                Array.Resize(ref Event.TempMoveRoute, Event.TempMoveRouteCount);
                Event.TempMoveRoute[Event.TempMoveRouteCount].Index = Index;
                PopulateMoveRouteList();
                // if set graphic then....
                if (Index == 43)
                {
                    Event.TempMoveRoute[Event.TempMoveRouteCount].Data1 = cmbGraphic.SelectedIndex;
                    Event.TempMoveRoute[Event.TempMoveRouteCount].Data2 = (int)Math.Round(nudGraphic.Value);
                    Event.TempMoveRoute[Event.TempMoveRouteCount].Data3 = Event.GraphicSelX;
                    Event.TempMoveRoute[Event.TempMoveRouteCount].Data4 = Event.GraphicSelX2;
                    Event.TempMoveRoute[Event.TempMoveRouteCount].Data5 = Event.GraphicSelY;
                    Event.TempMoveRoute[Event.TempMoveRouteCount].Data6 = Event.GraphicSelY2;
                }
            }

        }

        public void RemoveMoveRouteCommand(int Index)
        {
            int i;

            Index = Index + 1;
            if (Index > 0 & Index <= Event.TempMoveRouteCount)
            {
                if (Event.TempMoveRoute == null)
                {
                    return;
                }
                var loopTo = Event.TempMoveRouteCount;
                for (i = Index + 1; i < loopTo; i++)
                    Event.TempMoveRoute![i - 1] = Event.TempMoveRoute[i];
                Event.TempMoveRouteCount = Event.TempMoveRouteCount - 1;
                if (Event.TempMoveRouteCount == 0)
                {
                    Event.TempMoveRoute = new Type.MoveRoute[1];
                }
                else
                {
                    Array.Resize(ref Event.TempMoveRoute, Event.TempMoveRouteCount);
                }
                PopulateMoveRouteList();
            }

        }

        public void PopulateMoveRouteList()
        {
            int i;

            lstMoveRoute.Items.Clear();

            var loopTo = Event.TempMoveRouteCount;
            for (i = 0; i < loopTo; i++)
            {
                if (Event.TempMoveRoute == null)
                    return;
                switch (Event.TempMoveRoute![i].Index)
                {
                    case 1:
                        {
                            lstMoveRoute.Items.Add("Move Up");
                            break;
                        }
                    case 2:
                        {
                            lstMoveRoute.Items.Add("Move Down");
                            break;
                        }
                    case 3:
                        {
                            lstMoveRoute.Items.Add("Move Left");
                            break;
                        }
                    case 4:
                        {
                            lstMoveRoute.Items.Add("Move Right");
                            break;
                        }
                    case 5:
                        {
                            lstMoveRoute.Items.Add("Move Randomly");
                            break;
                        }
                    case 6:
                        {
                            lstMoveRoute.Items.Add("Move Towards Player");
                            break;
                        }
                    case 7:
                        {
                            lstMoveRoute.Items.Add("Move Away From Player");
                            break;
                        }
                    case 8:
                        {
                            lstMoveRoute.Items.Add("Step Forward");
                            break;
                        }
                    case 9:
                        {
                            lstMoveRoute.Items.Add("Step Back");
                            break;
                        }
                    case 10:
                        {
                            lstMoveRoute.Items.Add("Wait 100ms");
                            break;
                        }
                    case 11:
                        {
                            lstMoveRoute.Items.Add("Wait 500ms");
                            break;
                        }
                    case 12:
                        {
                            lstMoveRoute.Items.Add("Wait 1000ms");
                            break;
                        }
                    case 13:
                        {
                            lstMoveRoute.Items.Add("Turn Up");
                            break;
                        }
                    case 14:
                        {
                            lstMoveRoute.Items.Add("Turn Down");
                            break;
                        }
                    case 15:
                        {
                            lstMoveRoute.Items.Add("Turn Left");
                            break;
                        }
                    case 16:
                        {
                            lstMoveRoute.Items.Add("Turn Right");
                            break;
                        }
                    case 17:
                        {
                            lstMoveRoute.Items.Add("Turn 90 Degrees To the Right");
                            break;
                        }
                    case 18:
                        {
                            lstMoveRoute.Items.Add("Turn 90 Degrees To the Left");
                            break;
                        }
                    case 19:
                        {
                            lstMoveRoute.Items.Add("Turn Around 180 Degrees");
                            break;
                        }
                    case 20:
                        {
                            lstMoveRoute.Items.Add("Turn Randomly");
                            break;
                        }
                    case 21:
                        {
                            lstMoveRoute.Items.Add("Turn Towards Player");
                            break;
                        }
                    case 22:
                        {
                            lstMoveRoute.Items.Add("Turn Away from Player");
                            break;
                        }
                    case 23:
                        {
                            lstMoveRoute.Items.Add("Set Speed 8x Slower");
                            break;
                        }
                    case 24:
                        {
                            lstMoveRoute.Items.Add("Set Speed 4x Slower");
                            break;
                        }
                    case 25:
                        {
                            lstMoveRoute.Items.Add("Set Speed 2x Slower");
                            break;
                        }
                    case 26:
                        {
                            lstMoveRoute.Items.Add("Set Speed to Normal");
                            break;
                        }
                    case 27:
                        {
                            lstMoveRoute.Items.Add("Set Speed 2x Faster");
                            break;
                        }
                    case 28:
                        {
                            lstMoveRoute.Items.Add("Set Speed 4x Faster");
                            break;
                        }
                    case 29:
                        {
                            lstMoveRoute.Items.Add("Set Frequency Lowest");
                            break;
                        }
                    case 30:
                        {
                            lstMoveRoute.Items.Add("Set Frequency Lower");
                            break;
                        }
                    case 31:
                        {
                            lstMoveRoute.Items.Add("Set Frequency Normal");
                            break;
                        }
                    case 32:
                        {
                            lstMoveRoute.Items.Add("Set Frequency Higher");
                            break;
                        }
                    case 33:
                        {
                            lstMoveRoute.Items.Add("Set Frequency Highest");
                            break;
                        }
                    case 34:
                        {
                            lstMoveRoute.Items.Add("Turn On Walking Animation");
                            break;
                        }
                    case 35:
                        {
                            lstMoveRoute.Items.Add("Turn Off Walking Animation");
                            break;
                        }
                    case 36:
                        {
                            lstMoveRoute.Items.Add("Turn On Fixed Direction");
                            break;
                        }
                    case 37:
                        {
                            lstMoveRoute.Items.Add("Turn Off Fixed Direction");
                            break;
                        }
                    case 38:
                        {
                            lstMoveRoute.Items.Add("Turn On Walk Through");
                            break;
                        }
                    case 39:
                        {
                            lstMoveRoute.Items.Add("Turn Off Walk Through");
                            break;
                        }
                    case 40:
                        {
                            lstMoveRoute.Items.Add("Set Position Below Player");
                            break;
                        }
                    case 41:
                        {
                            lstMoveRoute.Items.Add("Set Position at Player Level");
                            break;
                        }
                    case 42:
                        {
                            lstMoveRoute.Items.Add("Set Position Above Player");
                            break;
                        }
                    case 43:
                        {
                            lstMoveRoute.Items.Add("Set Graphic");
                            break;
                        }
                }
            }

        }

        private void ChkIgnoreMove_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkIgnoreMove.Checked == true)
            {
                Event.Instance.Pages[Event.CurPageNum].IgnoreMoveRoute = 1;
            }
            else
            {
                Event.Instance.Pages[Event.CurPageNum].IgnoreMoveRoute = 0;
            }
        }

        private void ChkRepeatRoute_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkRepeatRoute.Checked == true)
            {
                Event.Instance.Pages[Event.CurPageNum].RepeatMoveRoute = 1;
            }
            else
            {
                Event.Instance.Pages[Event.CurPageNum].RepeatMoveRoute = 0;
            }
        }

        private void BtnMoveRouteOk_Click(object? sender, EventArgs e)
        {
            if (Event.IsMoveRouteCommand == true)
            {
                if (!Event.IsEdit)
                {
                    Event.AddCommand((int)EventCommand.SetMoveRoute);
                }
                else
                {
                    Event.EditCommand();
                }
                Event.TempMoveRouteCount = 0;
                Event.TempMoveRoute = new Type.MoveRoute[1];
                HideAllFrames();
            }
            else
            {
                Event.Instance.Pages[Event.CurPageNum].MoveRouteCount = Event.TempMoveRouteCount;
                Event.Instance.Pages[Event.CurPageNum].MoveRoute = Event.TempMoveRoute!;
                Event.TempMoveRouteCount = 0;
                Event.TempMoveRoute = new Type.MoveRoute[1];
                HideAllFrames();
            }
        }

        private void BtnMoveRouteCancel_Click(object? sender, EventArgs e)
        {
            Event.TempMoveRouteCount = 0;
            Event.TempMoveRoute = new Type.MoveRoute[1];
            HideAllFrames();
        }

        #endregion

        #region CommandFrames

        #region Show Text

        private void BtnShowTextOk_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.ShowText);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnShowTextCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Add Text

        private void BtnAddTextOk_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.AddText);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnAddTextCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Show Choices
        private void BtnShowChoicesOk_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.ShowChoices);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnShowChoicesCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Show Chatbubble

        private void CmbChatBubbleTargetType_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbChatBubbleTargetType.SelectedIndex == (int)TargetType.None)
            {
                cmbChatBubbleTarget.Visible = false;
            }
            else if (cmbChatBubbleTargetType.SelectedIndex == (int)TargetType.Player)
            {
                cmbChatBubbleTarget.Visible = true;
                cmbChatBubbleTarget.Items.Clear();

                for (int i = 0; i < Variables.MaxNpcs; i++)
                {
                    if (Data.MyMap.Npc[i] < 0)
                    {
                        cmbChatBubbleTarget.Items.Add(i + ". ");
                    }
                    else
                    {
                        cmbChatBubbleTarget.Items.Add(i + 1 + ". " + Data.Npc[Data.MyMap.Npc[i]].Name);
                    }
                }
                cmbChatBubbleTarget.SelectedIndex = 0;
            }
            else if (cmbChatBubbleTargetType.SelectedIndex == (int)TargetType.Npc)
            {
                cmbChatBubbleTarget.Visible = true;
                cmbChatBubbleTarget.Items.Clear();

                for (int i = 0, loopTo = Data.MyMap.EventCount; i < loopTo; i++)
                    cmbChatBubbleTarget.Items.Add(i + 1 + ". " + Data.MyMap.Event[i].Name);
                cmbChatBubbleTarget.SelectedIndex = 0;
            }

        }

        private void BtnShowChatBubbleOK_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.ShowChatBubble);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnShowChatBubbleCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Set Player Variable

        private void OptVariableAction0_CheckedChanged(object? sender, EventArgs e)
        {
            if (optVariableAction0.Checked == true)
            {
                nudVariableData0.Enabled = true;
                nudVariableData0.Value = 0;
                nudVariableData1.Enabled = false;
                nudVariableData1.Value = 0;
                nudVariableData2.Enabled = false;
                nudVariableData2.Value = 0;
                nudVariableData3.Value = 0;
                nudVariableData4.Enabled = false;
                nudVariableData4.Value = 0;
            }
        }

        private void OptVariableAction1_CheckedChanged(object? sender, EventArgs e)
        {
            if (optVariableAction1.Checked == true)
            {
                nudVariableData0.Enabled = false;
                nudVariableData0.Value = 0;
                nudVariableData1.Enabled = true;
                nudVariableData1.Value = 0;
                nudVariableData2.Enabled = false;
                nudVariableData2.Value = 0;
                nudVariableData3.Enabled = false;
                nudVariableData3.Value = 0;
                nudVariableData4.Enabled = false;
                nudVariableData4.Value = 0;
            }
        }

        private void OptVariableAction2_CheckedChanged(object? sender, EventArgs e)
        {
            if (optVariableAction2.Checked == true)
            {
                nudVariableData0.Enabled = false;
                nudVariableData0.Value = 0;
                nudVariableData1.Enabled = false;
                nudVariableData1.Value = 0;
                nudVariableData2.Enabled = true;
                nudVariableData2.Value = 0;
                nudVariableData3.Enabled = false;
                nudVariableData3.Value = 0;
                nudVariableData4.Enabled = false;
                nudVariableData4.Value = 0;
            }
        }

        private void OptVariableAction3_CheckedChanged(object? sender, EventArgs e)
        {
            if (optVariableAction3.Checked == true)
            {
                nudVariableData0.Enabled = false;
                nudVariableData0.Value = 0;
                nudVariableData1.Enabled = false;
                nudVariableData1.Value = 0;
                nudVariableData2.Enabled = false;
                nudVariableData2.Value = 0;
                nudVariableData3.Enabled = true;
                nudVariableData3.Value = 0;
                nudVariableData4.Enabled = true;
                nudVariableData4.Value = 0;
            }
        }

        private void BtnPlayerVarOk_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.ModifyVariable);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnPlayerVarCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Set Player Switch

        private void BtnSetPlayerSwitchOk_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.ModifySwitch);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnSetPlayerSwitchCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Set Self Switch

        private void BtnSelfswitchOk_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.ModifySelfSwitch);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnSelfswitchCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Conditional Branch

        private void OptCondition_Index0_CheckedChanged(object? sender, EventArgs e)
        {
            if (!optCondition0.Checked)
                return;

            ClearConditionFrame();

            cmbCondition_PlayerVarIndex.Enabled = true;
            cmbCondition_PlayerVarCompare.Enabled = true;
            nudCondition_PlayerVarCondition.Enabled = true;
        }

        private void OptCondition1_CheckedChanged(object? sender, EventArgs e)
        {
            if (!optCondition1.Checked)
                return;

            ClearConditionFrame();

            cmbCondition_PlayerSwitch.Enabled = true;
            cmbCondtion_PlayerSwitchCondition.Enabled = true;
        }

        private void OptCondition2_CheckedChanged(object? sender, EventArgs e)
        {
            if (!optCondition2.Checked)
                return;

            ClearConditionFrame();

            cmbCondition_HasItem.Enabled = true;
            nudCondition_HasItem.Enabled = true;
        }

        private void OptCondition3_CheckedChanged(object? sender, EventArgs e)
        {
            if (!optCondition3.Checked)
                return;

            ClearConditionFrame();

            cmbCondition_JobIs.Enabled = true;
        }

        private void OptCondition4_CheckedChanged(object? sender, EventArgs e)
        {
            if (!optCondition4.Checked)
                return;

            ClearConditionFrame();
            cmbCondition_LearntSkill.Enabled = true;
        }


        private void OptCondition5_CheckedChanged(object? sender, EventArgs e)
        {
            if (!optCondition5.Checked)
                return;

            ClearConditionFrame();

            cmbCondition_LevelCompare.Enabled = true;
            nudCondition_LevelAmount.Enabled = true;
        }

        private void OptCondition6_CheckedChanged(object? sender, EventArgs e)
        {
            if (!optCondition6.Checked)
                return;

            ClearConditionFrame();

            cmbCondition_SelfSwitch.Enabled = true;
            cmbCondition_SelfSwitchCondition.Enabled = true;
        }

        private void OptCondition8_CheckedChanged(object? sender, EventArgs e)
        {
            if (!optCondition8.Checked)
                return;

            ClearConditionFrame();

            cmbCondition_Gender.Enabled = true;
        }

        private void OptCondition9_CheckedChanged(object? sender, EventArgs e)
        {
            if (!optCondition9.Checked)
                return;

            ClearConditionFrame();

            cmbCondition_Time.Enabled = true;
        }

        private void BtnConditionalBranchOk_Click(object? sender, EventArgs e)
        {
            if (Event.IsEdit == false)
            {
                Event.AddCommand((int)EventCommand.ConditionalBranch);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnConditionalBranchCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Create Label

        private void BtnCreateLabelOk_Click(object? sender, EventArgs e)
        {
            if (Event.IsEdit == false)
            {
                Event.AddCommand((int)EventCommand.Label);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }
        private void BtnCreateLabelCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region GoTo Label

        private void BtnGoToLabelOk_Click(object? sender, EventArgs e)
        {
            if (Event.IsEdit == false)
            {
                Event.AddCommand((int)EventCommand.GoToLabel);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnGoToLabelCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Change Items

         private void BtnChangeItemsOk_Click(object? sender, EventArgs e)
        {
            if (Event.IsEdit == false)
            {
                Event.AddCommand((int)EventCommand.ChangeItems);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnChangeItemsCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Change Level

        private void BtnChangeLevelOK_Click(object? sender, EventArgs e)
        {
            if (Event.IsEdit == false)
            {
                Event.AddCommand((int)EventCommand.ChangeLevel);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnChangeLevelCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Change Skills

        private void BtnChangeSkillsOK_Click(object? sender, EventArgs e)
        {
            if (Event.IsEdit == false)
            {
                Event.AddCommand((int)EventCommand.ChangeSkills);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnChangeSkillsCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Change Job

        private void BtnChangeJobOK_Click(object? sender, EventArgs e)
        {
            if (Event.IsEdit == false)
            {
                Event.AddCommand((int)EventCommand.ChangeJob);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnChangeJobCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Change Sprite

        private void BtnChangeSpriteOK_Click(object? sender, EventArgs e)
        {
            if (Event.IsEdit == false)
            {
                Event.AddCommand((int)EventCommand.ChangeSprite);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnChangeSpriteCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Change Gender

        private void BtnChangeGenderOK_Click(object? sender, EventArgs e)
        {
            if (Event.IsEdit == false)
            {
                Event.AddCommand((int)EventCommand.ChangeSex);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnChangeGenderCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Change PK

        private void BtnChangePkOK_Click(object? sender, EventArgs e)
        {
            if (Event.IsEdit == false)
            {
                Event.AddCommand((int)EventCommand.SetPlayerKillable);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnChangePkCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Give Exp

        private void BtnGiveExpOK_Click(object? sender, EventArgs e)
        {
            if (Event.IsEdit == false)
            {
                Event.AddCommand((int)EventCommand.GiveExperience);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnGiveExpCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Player Warp

        private void BtnPlayerWarpOK_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.WarpPlayer);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnPlayerWarpCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Route Completion

        private void BtnMoveWaitOK_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.WaitMovementCompletion);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnMoveWaitCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Spawn Npc

        private void BtnSpawnNpcOK_Click(object? sender, EventArgs e)
        {
            if (Event.IsEdit == false)
            {
                Event.AddCommand((int)EventCommand.SpawnNpc);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnSpawnNpcCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Play Animation

        private void OptPlayAnimPlayer_CheckedChanged(object? sender, EventArgs e)
        {
            lblPlayAnimX.Visible = false;
            lblPlayAnimY.Visible = false;
            nudPlayAnimTileX.Visible = false;
            nudPlayAnimTileY.Visible = false;
            cmbPlayAnimEvent.Visible = false;
        }

        private void OptPlayAnimEvent_CheckedChanged(object? sender, EventArgs e)
        {
            lblPlayAnimX.Visible = false;
            lblPlayAnimY.Visible = false;
            nudPlayAnimTileX.Visible = false;
            nudPlayAnimTileY.Visible = false;
            cmbPlayAnimEvent.Visible = true;
        }

        private void OptPlayAnimTile_CheckedChanged(object? sender, EventArgs e)
        {
            lblPlayAnimX.Visible = true;
            lblPlayAnimY.Visible = true;
            nudPlayAnimTileX.Visible = true;
            nudPlayAnimTileY.Visible = true;
            cmbPlayAnimEvent.Visible = false;
        }

        private void BtnPlayAnimationOK_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.PlayAnimation);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnPlayAnimationCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Set Fog

        private void BtnSetFogOK_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.SetFog);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnSetFogCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Set Weather

        private void BtnSetWeatherOK_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.SetWeather);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnSetWeatherCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Set Map Tint

        private void BtnMapTintOK_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.SetScreenTint);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnMapTintCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Play BGM

        private void BtnPlayBgmOK_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.PlayBgm);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnPlayBgmCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Play Sound

        private void BtnPlaySoundOK_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.PlaySound);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnPlaySoundCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Wait

        private void BtnSetWaitOK_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.Wait);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnSetWaitCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Set Access

        private void BtnSetAccessOK_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.SetAccessLevel);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnSetAccessCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion

        #region Show Pic

        private void BtnShowPicOK_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.ShowPicture);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnShowPicCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        private void nudShowPicture_Click(object? sender, EventArgs e)
        {
            DrawPicture();
        }

        private void DrawPicture()
        {
            int Sprite;

            Sprite = (int)Math.Round(nudShowPicture.Value);

            if (Sprite < 1 | Sprite > GameState.NumPictures)
            {
                picShowPic.Image = null;
                return;
            }

            if (File.Exists(System.IO.Path.Combine(DataPath.Pictures, Sprite + GameState.GfxExt)))
            {
                var bmpPath = System.IO.Path.Combine(DataPath.Pictures, Sprite + GameState.GfxExt);
                try
                {
                    var bmp = new Eto.Drawing.Bitmap(bmpPath);
                    picShowPic.Image = bmp;
                }
                catch
                {
                    picShowPic.Image = null;
                }
            }
        }

        #endregion

        #region Open Shop

        private void BtnOpenShopOK_Click(object? sender, EventArgs e)
        {
            if (!Event.IsEdit)
            {
                Event.AddCommand((int)EventCommand.OpenShop);
            }
            else
            {
                Event.EditCommand();
            }
            HideAllFrames();
        }

        private void BtnOpenShopCancel_Click(object? sender, EventArgs e)
        {
            HideAllFrames();
        }

        #endregion
        #endregion

        // Ensure the full editor (left+right panes) is mounted into the active tab so tabs span the whole editor
        private void AttachEditorHostToSelectedTab()
        {
            try
            {
                // Detach from previously hosted tab if any
                if (hostedTab != null && ReferenceEquals(hostedTab.Content, tabContentHost))
                {
                    hostedTab.Content = null;
                    hostedTab = null;
                }
                // Attach reusable host to current selected page
                if (tabPages.SelectedPage is TabPage sel)
                {
                    sel.Content = tabContentHost;
                    hostedTab = sel;
                    // ensure layout refresh
                    sel.Invalidate();
                }
            }
            catch { }
        }

        // Place a frame panel into the TOP host area (where Variables/Switches live).
        // If useDialogueWrapper is true, embed the content inside fraDialogue, otherwise show directly.
        private void ShowFrame(Panel content, bool useDialogueWrapper)
        {
            try
            {
                if (useDialogueWrapper)
                {
                    // Ensure wrapper hosts the content
                    fraDialogue.Content = content;
                    fraDialogue.Visible = true;
                    content.Visible = true;
                    frameHost.Content = fraDialogue;
                }
                else
                {
                    content.Visible = true;
                    frameHost.Content = content;
                }
                // Ensure sizing before showing
                SyncOverlayChildSizes();
                frameHost.Visible = true;
                // Swap palette out and show the active frame
                fraCommands.Visible = false;
                frameHost.Visible = true;
                // Nudge layout
                try { _rightStack?.Invalidate(); } catch { }
                try { _rightScroll?.Invalidate(); } catch { }
                try { editorHost?.Invalidate(); } catch { }
                // Defer one more size sync after layout on UI thread
                try { Application.Instance?.Invoke(() => SyncOverlayChildSizes()); } catch { }
                // Force layout refresh
                frameHost.Invalidate();
                ScrollRightPaneTop();
            }
            catch { }
        }

        // Hide all frames and show the variables/switches area at the top, keep commands at the bottom
        private void HideAllFrames()
        {
            // Set visibility off for all known frames so the command area is shown
            fraDialogue.Visible = false;
            fraGraphic.Visible = false;
            fraMoveRoute.Visible = false;
            fraShowText.Visible = false;
            fraShowChoices.Visible = false;
            fraAddText.Visible = false;
            fraShowChatBubble.Visible = false;
            fraPlayerVariable.Visible = false;
            fraPlayerSwitch.Visible = false;
            fraSetSelfSwitch.Visible = false;
            fraConditionalBranch.Visible = false;
            fraCreateLabel.Visible = false;
            fraGoToLabel.Visible = false;
            fraChangeItems.Visible = false;
            fraChangeLevel.Visible = false;
            fraChangeSkills.Visible = false;
            fraChangeJob.Visible = false;
            fraChangeSprite.Visible = false;
            fraChangeGender.Visible = false;
            fraChangePK.Visible = false;
            fraGiveExp.Visible = false;
            fraPlayerWarp.Visible = false;
            fraMoveRouteWait.Visible = false;
            fraSpawnNpc.Visible = false;
            fraPlayAnimation.Visible = false;
            fraSetFog.Visible = false;
            fraSetWeather.Visible = false;
            fraMapTint.Visible = false;
            fraPlayBGM.Visible = false;
            fraPlaySound.Visible = false;
            fraSetWait.Visible = false;
            fraSetAccess.Visible = false;
            fraShowPic.Visible = false;
            fraOpenShop.Visible = false;
            // Clear the frame host so nothing is overlayed in the top area
            try { frameHost.Content = null; } catch { }
            frameHost.Visible = false;
            // Show command palette again
            fraCommands.Visible = true;
            frameHost.Visible = false;
            SyncOverlayChildSizes();
            try { _rightStack?.Invalidate(); } catch { }
            try { _rightScroll?.Invalidate(); } catch { }
            try { editorHost?.Invalidate(); } catch { }
        }
    }
}