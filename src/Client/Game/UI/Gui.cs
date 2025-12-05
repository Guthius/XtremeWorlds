using System.Collections.Concurrent;
using System.Diagnostics;
using Client.Game.UI.Controls;
using Client.Game.UI.Windows;
using Core.Configurations;
using Core.Globals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using static Core.Globals.Command;
using Type = Core.Globals.Type;
using System.IO;

namespace Client.Game.UI;

public class WindowManager
{
    // GUI
    public static ConcurrentDictionary<long, Window> Windows { get; private set; } = new();
    public static Window? ActiveWindow { get; set; }

    // GUi parts
    public static Type.ControlPart DragBox;

    // Used for automatically the zOrder
    public static int ZOrderWin;
    public static int ZOrderCon;

    // Declare a timer to control when dragging can begin
    private static readonly Stopwatch DragTimer = new();
    private const double DragInterval = 100d; // Set the interval in milliseconds to start dragging
    private static bool _canDrag; // Flag to control when dragging is allowed
    private static bool _isDragging;
    private static bool _isSelected;

    public static bool IsMouseOverAnyWindow => _mouseOverAnyWindow;

    private static bool _mouseOverAnyWindow;

    // Lock dragging if initial press was over a control or a different window
    private static bool _dragLockedByPress;
    public static bool IsWindowActive => _isSelected;

    public static void UpdateZOrder(long windowIndex, bool forced = false)
    {
        var window = Windows[windowIndex];

        if (!forced)
        {
            if (window.ZChange == 0)
            {
                return;
            }
        }

        if (window.ZOrder == Windows.Count - 1)
        {
            return;
        }

        var oldZOrder = window.ZOrder;

        for (var i = 1; i <= Windows.Count; i++)
        {
            if (Windows[i].ZOrder > oldZOrder)
            {
                Windows[i].ZOrder--;
            }
        }

        window.ZOrder = Windows.Count - 1;
    }

    // Safe helpers to avoid null/KeyNotFound when UI isn't fully initialized
    public static bool TryGetWindow(string windowName, out Window? window)
    {
        window = GetWindowByName(windowName);
        return window is not null;
    }

    public static bool TryGetControl(string windowName, string controlName, out Control? control)
    {
        control = null;
        var window = GetWindowByName(windowName);
        if (window is null)
        {
            return false;
        }

        try
        {
            control = window.GetChild(controlName);
            return control is not null;
        }
        catch
        {
            return false;
        }
    }

    public static int CreateWindow(string name, string caption, Font font, int zOrder, int left, int top, int width,
        int height, int icon, bool visible = true, int xOffset = 0, int yOffset = 0, Design designNorm = Design.None,
        Design designHover = Design.None, Design designMousedown = Design.None, int imageNorm = 0, int imageHover = 0,
        int imageMousedown = 0, Action? callbackNorm = null, Action? callbackHover = null,
        Action? callbackMousemove = null, Action? callbackMousedown = null, Action? callbackDblclick = null,
        Action? onDraw = null, bool canDrag = true, byte zChange = 1, bool clickThrough = false)
    {
        var stateCount = Enum.GetValues<ControlState>().Length;
        var design = new List<Design>(Enumerable.Repeat((Design)0, stateCount));
        var image = new List<int>(Enumerable.Repeat(0, stateCount));
        var callback = new List<Action?>(Enumerable.Repeat((Action)null, stateCount));

        // Assign specific values for each state
        design[(int)ControlState.Normal] = designNorm;
        design[(int)ControlState.Hover] = designHover;
        design[(int)ControlState.MouseDown] = designMousedown;

        image[(int)ControlState.Normal] = imageNorm;
        image[(int)ControlState.Hover] = imageHover;
        image[(int)ControlState.MouseDown] = imageMousedown;

        callback[(int)ControlState.Normal] = callbackNorm;
        callback[(int)ControlState.Hover] = callbackHover;
        callback[(int)ControlState.MouseDown] = callbackMousedown;
        callback[(int)ControlState.MouseMove] = callbackMousemove;
        callback[(int)ControlState.DoubleClick] = callbackDblclick;

        // Create a new instance of Window and populate it
        var window = new Window
        {
            Name = name,
            X = left,
            Y = top,
            InitialX = left,
            InitialY = top,
            Width = width,
            Height = height,
            Visible = visible,
            CanDrag = canDrag,
            Font = font,
            Text = caption,
            XOffset = xOffset,
            YOffset = yOffset,
            Icon = icon,
            ZChange = zChange,
            ZOrder = zOrder,
            OnDraw = onDraw,
            ClickThrough = clickThrough,
            Design = design,
            Image = image,
            CallBack = callback
        };

        Windows.TryAdd(Windows.Count + 1, window);

        if (visible)
        {
            ActiveWindow = window;
        }

        return Windows.Count;
    }

    public static void CreateTextbox(int windowIndex, string name, int left, int top, int width, int height,
        string text = "", Font font = Font.Georgia, Alignment align = Alignment.Left, bool visible = true,
        int alpha = 255, bool isActive = true, int xOffset = 0, int yOffset = 0, int? imageNorm = null,
        int? imageHover = null, int? imageMousedown = null, Design designNorm = Design.None,
        Design designHover = Design.None, Design designMousedown = Design.None, bool censor = false, int icon = 0,
        Action? callbackNorm = null, Action? callbackHover = null, Action? callbackMousedown = null,
        Action? callbackMousemove = null, Action? callbackDblclick = null, Action? callbackEnter = null)
    {
        var stateCount = Enum.GetValues<ControlState>().Length;

        var callbacks = new List<Action?>(Enumerable.Repeat((Action)null, stateCount).ToList());

        callbacks[(int)ControlState.Normal] = callbackNorm;
        callbacks[(int)ControlState.Hover] = callbackHover;
        callbacks[(int)ControlState.MouseDown] = callbackMousedown;
        callbacks[(int)ControlState.MouseMove] = callbackMousemove;
        callbacks[(int)ControlState.DoubleClick] = callbackDblclick;
        callbacks[(int)ControlState.FocusEnter] = callbackEnter;

        if (!Windows.TryGetValue(windowIndex, out var window))
        {
            throw new UIException($"{windowIndex} is not a valid window index.");
        }

        var textBox = new TextBox
        {
            Name = name,
            X = left,
            Y = top,
            Width = width,
            Height = height,
            Visible = visible,
            Text = text,
            Align = align,
            Font = font,
            Color = Color.White,
            Alpha = alpha,
            XOffset = xOffset,
            YOffset = yOffset,
            ZOrder = window.Controls.Count,
            Censor = censor,
            Icon = icon,
            Design = designNorm,
            DesignHover = designHover,
            DesignMouseDown = designMousedown,
            Image = imageNorm,
            ImageHover = imageHover,
            ImageMouseDown = imageMousedown,
            CallBack = callbacks
        };

        window.Controls.Add(textBox);

        if (isActive)
        {
            window.ActiveControl = textBox;
        }

        ZOrderCon++;
    }

    public static void CreatePictureBox(int windowIndex, string name, int left, int top, int width, int height,
        bool visible = true, int alpha = 255, int? imageNorm = null, int? imageHover = null, int? imageMousedown = null,
        Design designNorm = Design.None, Design? designHover = null, Design? designMousedown = null,
        string texturePath = "", Action? callbackNorm = null, Action? callbackHover = null,
        Action? callbackMousedown = null, Action? callbackMousemove = null, Action? callbackDblclick = null,
        Action? onDraw = null)
    {
        if (!Windows.TryGetValue(windowIndex, out var window))
        {
            throw new UIException($"{windowIndex} is not a valid window index.");
        }

        var stateCount = Enum.GetValues<ControlState>().Length;
        var texture = new List<string>(Enumerable.Repeat(string.Empty, stateCount));
        var callback = new List<Action?>(Enumerable.Repeat((Action)null, stateCount));

        if (string.IsNullOrEmpty(texturePath))
        {
            texturePath = DataPath.Gui;
        }

        texture[(int)ControlState.Normal] = texturePath;
        texture[(int)ControlState.Hover] = texturePath;
        texture[(int)ControlState.MouseDown] = texturePath;

        callback[(int)ControlState.Normal] = callbackNorm;
        callback[(int)ControlState.Hover] = callbackHover;
        callback[(int)ControlState.MouseDown] = callbackMousedown;
        callback[(int)ControlState.MouseMove] = callbackMousemove;
        callback[(int)ControlState.DoubleClick] = callbackDblclick;

        if (imageNorm == 0) imageNorm = null;
        if (imageHover == 0) imageHover = null;
        if (imageMousedown == 0) imageMousedown = null;

        var pictureBox = new PictureBox
        {
            Name = name,
            X = left,
            Y = top,
            Width = width,
            Height = height,
            Visible = visible,
            Color = Color.White,
            Alpha = alpha,
            ZOrder = ZOrderCon,
            OnDraw = onDraw,
            Design = designNorm,
            DesignHover = designHover ?? designNorm,
            DesignMouseDown = designMousedown ?? designNorm,
            Image = imageNorm,
            ImageHover = imageHover,
            ImageMouseDown = imageMousedown,
            Texture = texture,
            CallBack = callback
        };

        window.Controls.Add(pictureBox);

        ZOrderCon++;
    }

    public static void CreateButton(int windowIndex, string name, int left, int top, int width, int height,
        string text = "", Font font = Font.Georgia, int icon = 0, int? imageNorm = null, int? imageHover = null,
        int? imageMousedown = null, bool visible = true, Design designNorm = Design.None, Design? designHover = null,
        Design? designMousedown = null, Action? callbackNorm = null, Action? callbackHover = null,
        Action? callbackMousedown = null, Action? callbackMousemove = null, Action? callbackDblclick = null,
        int xOffset = 0, int yOffset = 0, string tooltip = "")
    {
        if (!Windows.TryGetValue(windowIndex, out var window))
        {
            throw new UIException($"{windowIndex} is not a valid window index.");
        }

        var stateCount = Enum.GetValues<ControlState>().Length;
        var texture = new List<string>(Enumerable.Repeat(DataPath.Designs, stateCount).ToList());
        var callback = new List<Action?>(Enumerable.Repeat((Action)null, stateCount).ToList());

        texture[(int)ControlState.Normal] = DataPath.Gui;
        texture[(int)ControlState.Hover] = DataPath.Gui;
        texture[(int)ControlState.MouseDown] = DataPath.Gui;

        callback[(int)ControlState.Normal] = callbackNorm;
        callback[(int)ControlState.Hover] = callbackHover;
        callback[(int)ControlState.MouseDown] = callbackMousedown;
        callback[(int)ControlState.MouseMove] = callbackMousemove;
        callback[(int)ControlState.DoubleClick] = callbackDblclick;

        if (imageNorm == 0) imageNorm = null;
        if (imageHover == 0) imageHover = null;
        if (imageMousedown == 0) imageMousedown = null;

        var button = new Button
        {
            Name = name,
            X = left,
            Y = top,
            Width = width,
            Height = height,
            Visible = visible,
            Text = text,
            Font = font,
            XOffset = xOffset,
            YOffset = yOffset,
            ZOrder = ZOrderCon,
            Tooltip = tooltip,
            Icon = icon,
            Design = designNorm,
            DesignHover = designHover ?? designNorm,
            DesignMouseDown = designMousedown ?? designNorm,
            Image = imageNorm,
            ImageHover = imageHover,
            ImageMouseDown = imageMousedown,
            Texture = texture,
            CallBack = callback
        };

        window.Controls.Add(button);

        ZOrderCon++;
    }

    public static void CreateLabel(int windowIndex, string name, int left, int top, int width, int height, string text,
        Font font, Alignment align = Alignment.Left, bool visible = true, bool clickThrough = false,
        bool censor = false, Action? callbackNorm = null, Action? callbackHover = null,
        Action? callbackMousedown = null, Action? callbackMousemove = null, Action? callbackDblclick = null,
        bool enabled = false)
    {
        if (!Windows.TryGetValue(windowIndex, out var window))
        {
            throw new UIException($"{windowIndex} is not a valid window index.");
        }

        var controlStateCount = Enum.GetValues<ControlState>().Length;
        var callbackLabel = new List<Action?>(Enumerable.Repeat((Action)null, controlStateCount).ToList());

        callbackLabel[(int)ControlState.Normal] = callbackNorm;
        callbackLabel[(int)ControlState.Hover] = callbackHover;
        callbackLabel[(int)ControlState.MouseDown] = callbackMousedown;
        callbackLabel[(int)ControlState.MouseMove] = callbackMousemove;
        callbackLabel[(int)ControlState.DoubleClick] = callbackDblclick;

        var label = new Label
        {
            Name = name,
            X = left,
            Y = top,
            Width = width,
            Height = height,
            Visible = visible,
            Text = text,
            Align = align,
            Font = font,
            ZOrder = ZOrderCon,
            Enabled = enabled,
            CallBack = callbackLabel
        };

        window.Controls.Add(label);

        ZOrderCon++;
    }

    public static void CreateCheckBox(int windowIndex, string name, int left, int top, int width, int height = 15,
        int value = 0, string text = "", Font font = Font.Georgia, bool visible = true, Design theDesign = Design.None,
        int group = 0, Action? callbackNorm = null, Action? callbackHover = null, Action? callbackMousedown = null,
        Action? callbackMousemove = null, Action? callbackDblclick = null)
    {
        if (!Windows.TryGetValue(windowIndex, out var window))
        {
            throw new UIException($"{windowIndex} is not a valid window index.");
        }

        var stateCount = Enum.GetValues<ControlState>().Length;
        var texture = new List<string>(Enumerable.Repeat(DataPath.Designs, stateCount).ToList());
        var callback = new List<Action?>(Enumerable.Repeat((Action)null, stateCount).ToList());

        texture[0] = DataPath.Gui;

        callback[(int)ControlState.Normal] = callbackNorm;
        callback[(int)ControlState.Hover] = callbackHover;
        callback[(int)ControlState.MouseDown] = callbackMousedown;
        callback[(int)ControlState.MouseMove] = callbackMousemove;
        callback[(int)ControlState.DoubleClick] = callbackDblclick;

        var checkBox = new CheckBox
        {
            Name = name,
            X = left,
            Y = top,
            Width = width,
            Height = height,
            Visible = visible,
            Value = value,
            Text = text,
            Font = font,
            ZOrder = ZOrderCon,
            Group = group,
            Design = theDesign,
            Texture = texture,
            CallBack = callback
        };

        window.Controls.Add(checkBox);

        ZOrderCon++;
    }

    public static void CreateComboBox(int windowIndex, string name, int left, int top, int width, int height,
        Design design)
    {
        var controlStateCount = Enum.GetValues<ControlState>().Length;
        var texture = new List<string>(Enumerable.Repeat(DataPath.Gui, controlStateCount).ToList());
        var callback = new List<Action?>(Enumerable.Repeat((Action)null, controlStateCount).ToList());

        texture[0] = DataPath.Gui;

        if (!Windows.TryGetValue(windowIndex, out var window))
        {
            return;
        }

        var comboBox = new ComboBox
        {
            Name = name,
            X = left,
            Y = top,
            Width = width,
            Height = height,
            ZOrder = ZOrderCon,
            Design = design,
            Texture = texture,
            CallBack = callback,
            Visible = true
        };

        window.Controls.Add(comboBox);

        ZOrderCon++;
    }

    public static void CreateListBox(int windowIndex, string name, int left, int top, int width, int height)
    {
        if (!Windows.TryGetValue(windowIndex, out var window))
        {
            throw new UIException($"{windowIndex} is not a valid window index.");
        }

        var stateCount = Enum.GetValues<ControlState>().Length;
        var texture = new List<string>(Enumerable.Repeat(DataPath.Gui, stateCount).ToList());
        var callback = new List<Action?>(Enumerable.Repeat((Action)null, stateCount).ToList());

        texture[0] = DataPath.Gui;

        var listBox = new Controls.ListBox
        {
            Name = name,
            X = left,
            Y = top,
            Width = width,
            Height = height,
            Visible = true,
            ZOrder = ZOrderCon,
            Texture = texture,
            CallBack = callback,
            Design = Design.TextBlack
        };

        window.Controls.Add(listBox);

        ZOrderCon++;
    }

    public static void CreateScrollBar(int windowIndex, string name, int left, int top, int width, int height,
        int min = 0, int max = 100, int value = 0, bool vertical = true, int thumbSize = 16)
    {
        if (!Windows.TryGetValue(windowIndex, out var window))
        {
            throw new UIException($"{windowIndex} is not a valid window index.");
        }

        var stateCount = Enum.GetValues<ControlState>().Length;
        var texture = new List<string>(Enumerable.Repeat(DataPath.Designs, stateCount).ToList());
        var callback = new List<Action?>(Enumerable.Repeat((Action)null, stateCount).ToList());

        texture[0] = DataPath.Gui;

        var scroll = new Controls.ScrollBar
        {
            Name = name,
            X = left,
            Y = top,
            Width = width,
            Height = height,
            Visible = true,
            Value = value,
            ZOrder = ZOrderCon,
            Texture = texture,
            CallBack = callback,
            Min = min,
            Max = max,
            Vertical = vertical,
            ThumbSize = Math.Max(8, thumbSize)
        };

        window.Controls.Add(scroll);

        ZOrderCon++;
    }

    public static int GetWindowIndex(string windowName)
    {
        foreach (var kvp in Windows)
        {
            if (string.Equals(kvp.Value.Name, windowName, StringComparison.CurrentCultureIgnoreCase))
            {
                return (int)kvp.Key;
            }
        }

        return 0;
    }

    public static Window? GetWindowByName(string windowName)
    {
        var windowIndex = GetWindowIndex(windowName);
        if (windowIndex == 0)
        {
            return null;
        }

        return Windows[windowIndex];
    }

    public static int GetControlIndex(string window, string controlName)
    {
        var index = GetWindowIndex(window);

        for (var i = 0; i <= Windows[index].Controls.Count - 1; i++)
        {
            if (string.Equals(Windows[index].Controls[i].Name, controlName, StringComparison.CurrentCultureIgnoreCase))
            {
                return i;
            }
        }

        return 0;
    }

    public static bool SetActiveControl(int windowIndex, int controlIndex)
    {
        var window = Windows[windowIndex];

        return SetActiveControl(window, controlIndex);
    }

    public static bool SetActiveControl(Window window, string controlName)
    {
        var controlIndex = GetControlIndex(window.Name, controlName);

        if (controlIndex < 0 || controlIndex >= window.Controls.Count)
        {
            return false;
        }

        switch (window.Controls[controlIndex])
        {
            case TextBox:
                window.LastControl = window.ActiveControl;
                window.ActiveControl = window.Controls[controlIndex];
                return true;
        }

        return false;
    }

    public static bool SetActiveControl(Window window, int controlIndex)
    {
        if (controlIndex < 0 || controlIndex >= window.Controls.Count)
        {
            return false;
        }

        switch (window.Controls[controlIndex])
        {
            case TextBox:
                window.LastControl = window.ActiveControl;
                window.ActiveControl = window.Controls[controlIndex];
                return true;
        }

        return false;
    }

    public static void CentralizeWindow(int windowIndex)
    {
        var window = Windows[windowIndex];

        window.X = (int)Math.Round(GameState.ResolutionWidth / 2d - window.Width / 2d);
        window.Y = (int)Math.Round(GameState.ResolutionHeight / 2d - window.Height / 2d);
        window.InitialX = window.X;
        window.InitialY = window.Y;
    }

    public static void HideWindows()
    {
        for (var i = 1; i <= Windows.Count - 1; i++)
        {
            HideWindow(i);
        }
    }

    public static void ShowWindow(string windowName, bool forced = false, bool resetPosition = true)
    {
        var index = GetWindowIndex(windowName);
        if (index == 0)
        {
            try
            {
                // Try to lazily load the layout if it's not already loaded via the skin script
                WindowLoader.FromLayout(windowName);
                index = GetWindowIndex(windowName);
            }
            catch
            {
                // ignore; will no-op below
            }
        }

        ShowWindow(index, forced, resetPosition);
    }

    public static void ShowWindow(int windowIndex, bool forced = false, bool resetPosition = true)
    {
        if (windowIndex == 0)
        {
            return;
        }

        Windows[windowIndex].Visible = true;

        if (forced)
        {
            UpdateZOrder(windowIndex, forced);
        }
        else if (Windows[windowIndex].ZChange != 0)
        {
            UpdateZOrder(windowIndex);
        }

        ActiveWindow = Windows[windowIndex];
        if (!resetPosition)
        {
            return;
        }

        // If the window was initialized before resolution was known, its initial position
        // may be off-screen (e.g., negative). Recenter it using current resolution.
        var needsRecentering =
            ActiveWindow.InitialX < 0 || ActiveWindow.InitialY < 0 ||
            ActiveWindow.InitialX + ActiveWindow.Width > GameState.ResolutionWidth ||
            ActiveWindow.InitialY + ActiveWindow.Height > GameState.ResolutionHeight;

        if (needsRecentering)
        {
            CentralizeWindow(windowIndex);
            ActiveWindow.InitialX = ActiveWindow.X;
            ActiveWindow.InitialY = ActiveWindow.Y;
        }
        else
        {
            ActiveWindow.X = ActiveWindow.InitialX;
            ActiveWindow.Y = ActiveWindow.InitialY;
        }
    }

    public static void HideWindow(string windowName)
    {
        HideWindow(GetWindowIndex(windowName));
    }

    public static void HideWindow(long windowIndex)
    {
        Windows[windowIndex].Visible = false;

        for (var i = Windows.Count - 1; i >= 1; i += -1)
        {
            if (Windows[i].Visible && Windows[i].ZChange != 1)
            {
                continue;
            }

            ActiveWindow = Windows[i];
            break;
        }
    }

    // Rendering & Initialisation
    public static void Init()
    {
        // Erase windows
        Windows = new ConcurrentDictionary<long, Window>();

        // Starter values
        ZOrderWin = 0;
        ZOrderCon = 0;

        // Dynamic UI initialization via Script.Instance (robust: keep going on errors)
        var ui = UIScript.Instance;
        if (ui is not null)
        {
            void Safe(string name, Action call)
            {
                try
                {
                    call();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"UI script error in {name}: {ex.Message}");
                }
            }

            Safe("UpdateWindow_Menu", () => ui.UpdateWindow_Menu());
            Safe("UpdateWindow_Register", () => ui.UpdateWindow_Register());
            Safe("UpdateWindow_Login", () => ui.UpdateWindow_Login());
            Safe("UpdateWindow_NewChar", () => ui.UpdateWindow_NewChar());
            Safe("UpdateWindow_Jobs", () => ui.UpdateWindow_Jobs());
            Safe("UpdateWindow_Chars", () => ui.UpdateWindow_Chars());
            Safe("UpdateWindow_ChatSmall", () => ui.UpdateWindow_ChatSmall());
            Safe("UpdateWindow_Chat", () => ui.UpdateWindow_Chat());
            Safe("UpdateWindow_Menu", () => ui.UpdateWindow_Menu());
            Safe("UpdateWindow_Description", () => ui.UpdateWindow_Description());
            Safe("UpdateWindow_Inventory", () => ui.UpdateWindow_Inventory());
            Safe("UpdateWindow_Skills", () => ui.UpdateWindow_Skills());
            Safe("UpdateWindow_Character", () => ui.UpdateWindow_Character());
            Safe("UpdateWindow_Hotbar", () => ui.UpdateWindow_Hotbar());
            Safe("UpdateWindow_Bank", () => ui.UpdateWindow_Bank());
            Safe("UpdateWindow_Shop", () => ui.UpdateWindow_Shop());
            Safe("UpdateWindow_EscMenu", () => ui.UpdateWindow_EscMenu());
            Safe("UpdateWindow_Bars", () => ui.UpdateWindow_Bars());
            Safe("UpdateWindow_Dialogue", () => ui.UpdateWindow_Dialogue());
            Safe("UpdateWindow_DragBox", () => ui.UpdateWindow_DragBox());
            Safe("UpdateWindow_Options", () => ui.UpdateWindow_Options());
            Safe("UpdateWindow_Trade", () => ui.UpdateWindow_Trade());
            Safe("UpdateWindow_Party", () => ui.UpdateWindow_Party());
            Safe("UpdateWindow_PlayerMenu", () => ui.UpdateWindow_PlayerMenu());
            Safe("UpdateWindow_RightClick", () => ui.UpdateWindow_RightClick());
            Safe("UpdateWindow_Combobox", () => ui.UpdateWindow_Combobox());
            Safe("UpdateWindow_Admin", () => ui.UpdateWindow_Admin());
            Safe("UpdateWindow_MapEditor", () => ui.UpdateWindow_MapEditor());
            Safe("UpdateWindow_NpcEditor", () => ui.UpdateWindow_NpcEditor());
            Safe("UpdateWindow_ItemEditor", () => ui.UpdateWindow_ItemEditor());
            Safe("UpdateWindow_ShopEditor", () => ui.UpdateWindow_ShopEditor());
            Safe("UpdateWindow_JobEditor", () => ui.UpdateWindow_JobEditor());
            Safe("UpdateWindow_ScriptEditor", () => ui.UpdateWindow_ScriptEditor());
            Safe("UpdateWindow_ResourceEditor", () => ui.UpdateWindow_ResourceEditor());
            Safe("UpdateWindow_MoralEditor", () => ui.UpdateWindow_MoralEditor());
            Safe("UpdateWindow_ProjectileEditor", () => ui.UpdateWindow_ProjectileEditor());
            Safe("UpdateWindow_AnimationEditor", () => ui.UpdateWindow_AnimationEditor());
        }
        else
        {
            Console.WriteLine("UI script not loaded; windows will be created on demand from layouts.");
        }
    }

    public static bool OnUpdate(ControlState entState)
    {
        Window? curWindow = null;
        var curControl = -1;

        // Check for MouseDown to start the drag timer
        if (GameClient.IsMouseButtonDown(MouseButton.Left) &&
            GameClient.PreviousMouseState.LeftButton == ButtonState.Released)
        {
            DragTimer.Restart(); // Start the timer on initial mouse down
            _canDrag = false; // Reset drag flag to ensure it doesn't drag immediately
        }

        // Check for MouseUp to reset dragging
        if (GameClient.IsMouseButtonUp(MouseButton.Left))
        {
            _isDragging = false;

            DragTimer.Reset(); // Stop the timer on mouse up
        }

        // Enable dragging if the mouse has been held down for the specified interval
        _canDrag = DragTimer.ElapsedMilliseconds >= DragInterval;

        lock (GameClient.InputLock)
        {
            // On fresh MouseDown, determine if we should lock dragging for this press
            if (GameClient.IsMouseButtonDown(MouseButton.Left) &&
                GameClient.PreviousMouseState.LeftButton == ButtonState.Released)
            {
                var prevActive = ActiveWindow;
                Window? clickedWindow = null;
                // Find top-most visible window under cursor
                foreach (var w in Windows.Values)
                {
                    if (!w.Visible) continue;
                    if (GameState.CurMouseX >= w.X && GameState.CurMouseX <= w.X + w.Width &&
                        GameState.CurMouseY >= w.Y && GameState.CurMouseY <= w.Y + w.Height)
                    {
                        if (clickedWindow is null || w.ZOrder > clickedWindow.ZOrder)
                        {
                            clickedWindow = w;
                        }
                    }
                }

                bool pressedOverControl = false;
                if (clickedWindow is not null)
                {
                    foreach (var c in clickedWindow.Controls)
                    {
                        if (!c.Visible) continue;
                        if (GameState.CurMouseX >= clickedWindow.X + c.X &&
                            GameState.CurMouseX <= clickedWindow.X + c.X + c.Width &&
                            GameState.CurMouseY >= clickedWindow.Y + c.Y &&
                            GameState.CurMouseY <= clickedWindow.Y + c.Y + c.Height)
                        {
                            pressedOverControl = true;
                            break;
                        }
                    }
                }

                // Lock dragging if we pressed over a control or we clicked a different window
                _dragLockedByPress = pressedOverControl || (clickedWindow is not null && clickedWindow != prevActive);
            }

            foreach (var window in Windows.Values)
            {
                if (!window.Visible)
                {
                    continue;
                }

                if (window.State != ControlState.MouseDown)
                {
                    window.State = ControlState.Normal;
                }

                if (GameState.CurMouseX >= window.X &&
                    GameState.CurMouseX <= window.Width + window.X &&
                    GameState.CurMouseY >= window.Y &&
                    GameState.CurMouseY <= window.Height + window.Y)
                {
                    // Handle combo menu logic
                    if (window.Design[0] == Design.ComboMenu)
                    {
                        switch (entState)
                        {
                            case ControlState.MouseMove or ControlState.Hover:
                                ComboMenu_MouseMove(window);
                                break;

                            case ControlState.MouseDown:
                                ComboMenu_MouseDown(window);
                                break;

                            case ControlState.MouseScroll:
                                // Scroll dropdown with mouse wheel
                                int delta = GameClient.CurrentMouseState.ScrollWheelValue -
                                            GameClient.PreviousMouseState.ScrollWheelValue;
                                if (delta != 0)
                                {
                                    int visibleRows = Math.Max(1, (window.Height - 2) / 16);
                                    int maxStart = Math.Max(0, window.List.Count - visibleRows);
                                    int step = delta > 0 ? -1 : 1; // wheel up -> scroll up
                                    window.ScrollOffset = Math.Clamp(window.ScrollOffset + step, 0, maxStart);
                                    // Keep hover aligned with new offset
                                    ComboMenu_MouseMove(window);
                                }

                                break;
                        }
                    }

                    // Track the top-most window
                    if (curWindow is null || window.ZOrder > curWindow.ZOrder)
                    {
                        curWindow = window;

                        _isDragging = true;
                    }

                    if (ActiveWindow is not null)
                    {
                        if (!ActiveWindow.Visible || !ActiveWindow.CanDrag)
                        {
                            ActiveWindow = curWindow;
                        }
                    }
                    else
                    {
                        ActiveWindow = curWindow;
                    }
                }

                if (entState == ControlState.MouseMove && GameClient.IsMouseButtonDown(MouseButton.Left))
                {
                    bool overAnyControl = false;
                    if (ActiveWindow is not null)
                    {
                        foreach (var c in ActiveWindow.Controls)
                        {
                            if (c.Visible && GameState.CurMouseX >= c.X + ActiveWindow.X &&
                                GameState.CurMouseX <= c.X + c.Width + ActiveWindow.X &&
                                GameState.CurMouseY >= c.Y + ActiveWindow.Y &&
                                GameState.CurMouseY <= c.Y + c.Height + ActiveWindow.Y)
                            {
                                overAnyControl = true;
                                break;
                            }
                        }
                    }

                    if (ActiveWindow is not null && _isDragging && !overAnyControl && !_dragLockedByPress)
                    {
                        if (_canDrag && ActiveWindow is { CanDrag: true, Visible: true })
                        {
                            ActiveWindow.X = GameLogic.Clamp(
                                ActiveWindow.X +
                                (GameState.CurMouseX - ActiveWindow.X - ActiveWindow.MovedX), 0,
                                GameState.ResolutionWidth - ActiveWindow.Width);
                            ActiveWindow.Y = GameLogic.Clamp(
                                ActiveWindow.Y +
                                (GameState.CurMouseY - ActiveWindow.Y - ActiveWindow.MovedY), 0,
                                GameState.ResolutionHeight - ActiveWindow.Height);
                            break;
                        }
                    }
                }
            }

            if (curWindow is not null)
            {
                _isSelected = true;

                // Handle the active window's callback
                var callBack = curWindow.CallBack[(int)entState];

                // Execute the callback if it exists
                callBack?.Invoke();

                // Handle controls in the active window
                for (var i = 0; i < curWindow.Controls.Count; i++)
                {
                    var control = curWindow.Controls[i];

                    if (control is { Enabled: true, Visible: true })
                    {
                        if ((GameState.CurMouseX >= control.X + curWindow.X &&
                             GameState.CurMouseX <= control.X + control.Width + curWindow.X &&
                             GameState.CurMouseY >= control.Y + curWindow.Y &&
                             GameState.CurMouseY <= control.Y + control.Height + curWindow.Y))
                        {
                            if (curControl == -1 || (curControl >= 0 && curControl < curWindow.Controls.Count && control.ZOrder > curWindow.Controls[curControl].ZOrder))
                            {
                                curControl = i;
                            }
                        }
                    }
                }

                if (curControl >= 0 && curControl < curWindow.Controls.Count)
                {
                    // Reset all control states
                    for (var j = 0; j < curWindow.Controls.Count; j++)
                    {
                        if (curControl != j)
                        {
                            curWindow.Controls[j].State = ControlState.Normal;
                        }
                    }

                    var instance2 = curWindow.Controls[curControl];

                    instance2.State = entState switch
                    {
                        ControlState.MouseMove => ControlState.Hover,
                        ControlState.MouseDown => ControlState.MouseDown,
                        _ => instance2.State
                    };

                    // Handle specific control types
                    switch (instance2)
                    {
                        case CheckBox checkBox:
                        {
                            if (checkBox.Group > 0 && instance2.Value == 0)
                            {
                                foreach (var control in curWindow.Controls.OfType<CheckBox>())
                                {
                                    if (control != checkBox && control.Group == checkBox.Group)
                                    {
                                        control.Value = 0;
                                    }
                                }

                                instance2.Value = 0;
                            }

                            break;
                        }
                        case ComboBox comboBox:
                        {
                            int itemHeight = 10;
                            int menuPadding = 5;
                            bool menuIsOpen =
                                WinComboMenu.IsOpen(curWindow,
                                    curControl); // You may need to implement this check if not present
                            if (entState == ControlState.MouseDown && GameClient.IsMouseButtonDown(MouseButton.Left))
                            {
                                if (menuIsOpen)
                                {
                                    int menuX = curWindow.X + comboBox.X - menuPadding;
                                    int menuY = curWindow.Y + comboBox.Y + comboBox.Height;
                                    int menuWidth = comboBox.Width + menuPadding * 2;
                                    int menuHeight = comboBox.Items.Count * itemHeight + menuPadding * 2;
                                    bool inMenu = GameState.CurMouseX >= menuX &&
                                                  GameState.CurMouseX <= menuX + menuWidth &&
                                                  GameState.CurMouseY >= menuY &&
                                                  GameState.CurMouseY <= menuY + menuHeight;
                                    int relY = GameState.CurMouseY -
                                               (curWindow.Y + comboBox.Y + comboBox.Height + menuPadding);
                                    int idx = relY / itemHeight;
                                    if (inMenu && idx >= 0 && idx < comboBox.Items.Count)
                                    {
                                        comboBox.Value = idx;
                                        // If this is the options resolution combobox, apply immediately
                                        if (string.Equals(curWindow.Name, "winOptions",
                                                StringComparison.CurrentCultureIgnoreCase) &&
                                            string.Equals(comboBox.Name, "cmbRes",
                                                StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            try
                                            {
                                                WinOptions.ApplyResolutionSelection(idx);
                                            }
                                            catch
                                            {
                                            }
                                        }

                                        WinComboMenu.Close(); // Hide menu after selection
                                    }
                                    else if (!inMenu)
                                    {
                                        // Clicked outside menu, close it
                                        WinComboMenu.Close();
                                    }
                                }
                                else
                                {
                                    // Menu not open yet, open it
                                    WinComboMenu.Show(curWindow, curControl);
                                }
                            }

                            break;
                        }
                        case Controls.ScrollBar scrollBar:
                        {
                            // Allow clicking/draging the scrollbar track to set value
                            bool interacting = entState == ControlState.MouseDown ||
                                               (entState == ControlState.MouseMove &&
                                                GameClient.IsMouseButtonDown(MouseButton.Left));
                            if (interacting)
                            {
                                int mouseX = GameState.CurMouseX - (curWindow.X + scrollBar.X);
                                int mouseY = GameState.CurMouseY - (curWindow.Y + scrollBar.Y);
                                int min = scrollBar.Min;
                                int max = scrollBar.Max;
                                int range = Math.Max(1, max - min);
                                int newVal;
                                if (scrollBar.Vertical)
                                {
                                    int usable = Math.Max(1, scrollBar.Height - scrollBar.ThumbSize);
                                    float t = Math.Clamp(usable == 0 ? 0f : (float)mouseY / usable, 0f, 1f);
                                    newVal = min + (int)Math.Round(t * range);
                                }
                                else
                                {
                                    int usable = Math.Max(1, scrollBar.Width - scrollBar.ThumbSize);
                                    float t = Math.Clamp(usable == 0 ? 0f : (float)mouseX / usable, 0f, 1f);
                                    newVal = min + (int)Math.Round(t * range);
                                }

                                newVal = Math.Clamp(newVal, min, max);
                                if (newVal != scrollBar.Value)
                                {
                                    scrollBar.Value = newVal;
                                    // Notify change via MouseMove callback by convention
                                    scrollBar.CallBack[(int)ControlState.MouseMove]?.Invoke();
                                }
                            }

                            // Mouse wheel scrolling on hovered scrollbar
                            if (entState == ControlState.MouseScroll)
                            {
                                int delta = GameClient.CurrentMouseState.ScrollWheelValue -
                                            GameClient.PreviousMouseState.ScrollWheelValue;
                                if (delta != 0)
                                {
                                    // Default step: 1 unit. For tileset scrollbars, step by tile size * 3.
                                    int step;
                                    bool isTilesetV = string.Equals(scrollBar.Name, "sldTilesetV",
                                        StringComparison.Ordinal);
                                    bool isTilesetH = string.Equals(scrollBar.Name, "sldTilesetH",
                                        StringComparison.Ordinal);
                                    if (isTilesetV || isTilesetH)
                                    {
                                        int per = (isTilesetV ? Constants.TileSize : Constants.TileSize) * 3;
                                        step = delta > 0 ? -per : per; // up scrolls up/left
                                    }
                                    else
                                    {
                                        step = delta > 0 ? -1 : 1; // wheel up -> decrement
                                    }

                                    int newVal = Math.Clamp(scrollBar.Value + step, scrollBar.Min, scrollBar.Max);
                                    if (newVal != scrollBar.Value)
                                    {
                                        scrollBar.Value = newVal;
                                        scrollBar.CallBack[(int)ControlState.MouseMove]?.Invoke();
                                    }
                                }
                            }

                            break;
                        }
                    }

                    if (GameClient.IsMouseButtonDown(MouseButton.Left))
                    {
                        SetActiveControl(curWindow, curControl);
                    }

                    callBack = instance2.CallBack[(int)entState];

                    // Execute the callback if it exists
                    callBack?.Invoke();
                }
            }

            if (curWindow is null)
            {
                ResetInterface();
                _isSelected = false;
            }

            if (entState == ControlState.MouseUp)
            {
                ResetMouseDown();
                // On mouse release, keep lock state until the next MouseDown recomputes it
            }

            // Auto-close combo menu when cursor leaves its area
            if (entState is ControlState.MouseMove or ControlState.Hover)
            {
                if (TryGetWindow("winComboMenu", out var menuWin) && menuWin is not null && menuWin.Visible)
                {
                    bool overMenu = menuWin.Contains(GameState.CurMouseX, GameState.CurMouseY);
                    bool overParentCombo = false;

                    var parentCtrl = menuWin.ParentControl;
                    if (!overMenu && parentCtrl is not null)
                    {
                        // Find the window that owns the parent control
                        foreach (var w in Windows.Values)
                        {
                            if (!w.Visible) continue;
                            if (!w.Controls.Contains(parentCtrl)) continue;

                            int px = w.X + parentCtrl.X;
                            int py = w.Y + parentCtrl.Y;
                            int pw = parentCtrl.Width;
                            int ph = parentCtrl.Height;
                            overParentCombo = GameState.CurMouseX >= px && GameState.CurMouseX <= px + pw &&
                                              GameState.CurMouseY >= py && GameState.CurMouseY <= py + ph;
                            break;
                        }
                    }

                    if (!overMenu && !overParentCombo)
                    {
                        WinComboMenu.Close();
                    }
                }
            }

            // Update cached flag for whether mouse is over any visible window
            _mouseOverAnyWindow = ComputeIsMouseOverAnyWindow();

            return true;
        }
    }

    /// <summary>
    /// Returns true if the current mouse position is over any visible window rectangle.
    /// This can be used by game systems (e.g. camera zoom) to avoid reacting to input while hovering UI.
    /// </summary>
    private static bool ComputeIsMouseOverAnyWindow()
    {
        foreach (var window in Windows.Values)
        {
            if (!window.Visible)
            {
                continue;
            }

            if (GameState.CurMouseX >= window.X &&
                GameState.CurMouseX <= window.X + window.Width &&
                GameState.CurMouseY >= window.Y &&
                GameState.CurMouseY <= window.Y + window.Height)
            {
                return true;
            }
        }

        return false;
    }

    public static void ResetInterface()
    {
        foreach (var window in Windows.Values)
        {
            if (window.State != ControlState.MouseDown)
            {
                window.State = ControlState.Normal;
            }

            if (window.Controls.Count == 0)
            {
                continue;
            }

            foreach (var control in window.Controls)
            {
                if (control.State != ControlState.MouseDown)
                {
                    control.State = ControlState.Normal;
                }
            }
        }
    }

    public static void ResetMouseDown()
    {
        lock (GameClient.InputLock)
        {
            foreach (var window in Windows.Values)
            {
                if (window.State == ControlState.MouseDown)
                {
                    window.State = ControlState.Normal;
                    window.CallBack[(int)ControlState.Normal]?.Invoke();
                }

                if (window.Controls.Count == 0)
                {
                    continue;
                }

                foreach (var control in window.Controls)
                {
                    if (control.State != ControlState.MouseDown)
                    {
                        continue;
                    }

                    control.State = ControlState.Normal;
                    control.CallBack[(int)control.State]?.Invoke();
                }
            }
        }
    }

    public static void Render()
    {
        if (Windows.IsEmpty)
        {
            return;
        }

        foreach (var window in Windows.Values.OrderBy(x => x.ZOrder).Where(x => x.Visible))
        {
            WindowRenderer.Render(window);

            // Render controls in stable passes to ensure layering:
            // 0 - parchment backgrounds, 1 - group boxes, 2 - all other controls
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; i < window.Controls.Count; i++)
                {
                    var control = window.Controls[i];
                    if (!control.Visible) continue;

                    int category = 2;
                    if (control is PictureBox pic && pic.Design == Design.Parchment)
                    {
                        category = 0;
                    }
                    else if (control is GroupBox)
                    {
                        category = 1;
                    }

                    if (category != pass) continue;

                    control.Render(window.X, window.Y);
                    control.OnDraw?.Invoke();
                }
            }
        }
    }

    public static void ComboMenu_MouseMove(Window window)
    {
        // Account for the 2px interior padding used by WindowRenderer when drawing items
        int relY = GameState.CurMouseY - (window.Y + 2);
        if (relY < 0)
        {
            window.Group = -1;
            return;
        }

        int visibleRows = Math.Max(1, (window.Height - 2) / 16);
        int start = Math.Clamp(window.ScrollOffset, 0, Math.Max(0, window.List.Count - visibleRows));
        int idx = start + relY / 16; // each item row is 16px tall
        if (idx >= 0 && idx < window.List.Count)
        {
            window.Group = idx; // hovered index
        }
        else
        {
            window.Group = -1; // no hover
        }
    }

    public static void ComboMenu_MouseDown(Window window)
    {
        if (window.List.Count == 0)
        {
            return;
        }

        int relY = GameState.CurMouseY - (window.Y + 2);
        int visibleRows = Math.Max(1, (window.Height - 2) / 16);
        int start = Math.Clamp(window.ScrollOffset, 0, Math.Max(0, window.List.Count - visibleRows));
        int idx = start + relY / 16;
        if (idx >= 0 && idx < window.List.Count)
        {
            if (window.ParentControl is not null)
            {
                if (window.ParentControl is Client.Game.UI.Controls.ComboBox comboBox)
                {
                    comboBox.Value = idx;
                    comboBox.CallBack[(int)ControlState.MouseMove]?.Invoke();
                }
                else
                {
                    window.ParentControl.Value = idx;
                }
            }
        }

        WinComboMenu.Close();
    }

    public static void ResizeGui()
    {
        // If UI hasn't been initialized yet, bail out safely
        if (Windows.IsEmpty)
        {
            return;
        }

        // Helper to safely apply changes when a window exists
        static void TryApply(string name, Action<Window> apply)
        {
            var idx = GetWindowIndex(name);
            if (idx == 0)
            {
                return;
            }

            if (Windows.TryGetValue(idx, out var w))
            {
                apply(w);
            }
        }

        // move Hotbar
        TryApply("winHotbar", w => w.X = GameState.ResolutionWidth - 432);

        // move chat
        TryApply("winChat", w => w.Y = GameState.ResolutionHeight - 178);
        TryApply("winChatSmall", w => w.Y = GameState.ResolutionHeight - 162);

        // move menu
        TryApply("winMenu", w =>
        {
            w.X = GameState.ResolutionWidth - 238;
            w.Y = GameState.ResolutionHeight - 42;
        });

        // re-size right-click background
        TryApply("winRightClickBG", w =>
        {
            w.Width = GameState.ResolutionWidth;
            w.Height = GameState.ResolutionHeight;
        });

        // re-size combo background
        TryApply("winComboMenuBG", w =>
        {
            w.Width = GameState.ResolutionWidth;
            w.Height = GameState.ResolutionHeight;
        });
    }

    public static void DrawMenuBackground()
    {
        var path = Path.Combine(DataPath.Pictures, "1");

        GameClient.RenderTexture(
            path: ref path,
            dX: 0, dY: 0, sX: 0, sY: 0,
            dW: 1920, dH: 1080,
            sW: 1920, sH: 1080);
    }

    public static void DrawYourTrade()
    {
        var color = 0;
        if (!TryGetWindow("winTrade", out var winTrade) ||
            !TryGetControl("winTrade", "picYour", out var picYour))
        {
            return;
        }

        var xo = winTrade!.X + picYour!.X;
        var yo = winTrade!.Y + picYour!.Y;

        // your items
        for (var i = 0; i < Variables.MaxInv; i++)
        {
            if (Data.TradeYourOffer[i].Num >= 0)
            {
                long itemNum = GetPlayerInv(GameState.MyIndex, Data.TradeYourOffer[i].Num);
                if (itemNum >= 0 & itemNum < Variables.MaxItems)
                {
                    Item.OnStream((int)itemNum);
                    long itemPic = Item.Instance[(int)itemNum].Icon;

                    if (itemPic > 0 & itemPic <= GameState.NumItems)
                    {
                        var top = yo + GameState.TradeTop +
                                    (GameState.TradeOffsetY + 32L) * (i / GameState.TradeColumns);
                        var left = xo + GameState.TradeLeft +
                                    (GameState.TradeOffsetX + 32L) * (i % GameState.TradeColumns);

                        // draw icon
                        var argPath = Path.Combine(DataPath.Items, itemPic.ToString());
                        GameClient.RenderTexture(ref argPath, (int)left, (int)top, 0, 0, 32, 32, 32, 32);

                        // If item is a stack - draw the amount you have
                        if (Data.TradeYourOffer[i].Value > 1)
                        {
                            var y = top + 20L;
                            var x = left + 1L;
                            var amountValue = Data.TradeYourOffer[i].Value;

                            // Color thresholds: <1M white, 1M-10M yellow, >10M bright green
                            if (amountValue < 1_000_000L)
                            {
                                color = (int)ColorName.White;
                            }
                            else if (amountValue > 1_000_000L && amountValue < 10_000_000L)
                            {
                                color = (int)ColorName.Yellow;
                            }
                            else if (amountValue > 10_000_000L)
                            {
                                color = (int)ColorName.BrightGreen;
                            }

                            TextRenderer.OnDraw(
                                GameLogic.ConvertCurrency((int)amountValue),
                                (int)x,
                                (int)y,
                                GameClient.QbColorToXnaColor(color),
                                GameClient.QbColorToXnaColor(color), winTrade.Font);
                        }
                    }
                }
            }
        }
    }

    public static void DrawTheirTrade()
    {
        var color = 0;
        if (!TryGetWindow("winTrade", out var winTrade) ||
            !TryGetControl("winTrade", "picTheir", out var picTheir))
        {
            return;
        }

        var xo = winTrade!.X + picTheir!.X;
        var yo = winTrade!.Y + picTheir!.Y;

        // their items
        for (var i = 0; i < Variables.MaxInv; i++)
        {
            long itemNum = Data.TradeTheirOffer[i].Num;
            if (itemNum >= 0 & itemNum < Variables.MaxItems)
            {
                Item.OnStream((int)itemNum);
                long itemPic = Item.Instance[(int)itemNum].Icon;

                if (itemPic > 0 & itemPic <= GameState.NumItems)
                {
                    var top = yo + GameState.TradeTop +
                                (GameState.TradeOffsetY + 32L) * (i / GameState.TradeColumns);
                    var left = xo + GameState.TradeLeft +
                                (GameState.TradeOffsetX + 32L) * (i % GameState.TradeColumns);

                    // draw icon
                    var argPath = Path.Combine(DataPath.Items, itemPic.ToString());
                    GameClient.RenderTexture(ref argPath, (int)left, (int)top, 0, 0, 32, 32, 32, 32);

                    // If item is a stack - draw the amount you have
                    if (Data.TradeTheirOffer[i].Value > 1)
                    {
                        var y = top + 20L;
                        var x = left + 1L;
                        var amountValue = Data.TradeTheirOffer[i].Value;

                        // Color thresholds: <1M white, 1M-10M yellow, >10M bright green
                        if (amountValue < 1_000_000L)
                        {
                            color = (int)ColorName.White;
                        }
                        else if (amountValue > 1_000_000L && amountValue < 10_000_000L)
                        {
                            color = (int)ColorName.Yellow;
                        }
                        else if (amountValue > 10_000_000L)
                        {
                            color = (int)ColorName.BrightGreen;
                        }

                        TextRenderer.OnDraw(
                            GameLogic.ConvertCurrency((int)amountValue),
                            (int)x,
                            (int)y,
                            GameClient.QbColorToXnaColor(color),
                            GameClient.QbColorToXnaColor(color), winTrade.Font);
                    }
                }
            }
        }
    }

    public static void UpdateActiveControl(Control modifiedControl)
    {
        if (ActiveWindow?.ActiveControl is not null)
        {
            var index = ActiveWindow.Controls.IndexOf(ActiveWindow.ActiveControl);

            // Update the control within the active window's Controls array
            ActiveWindow.Controls[index] = modifiedControl;

            // Notify listeners that text changed: reuse KeyUp slot for TextBox
            var ctrl = ActiveWindow.Controls[index];
            if (ctrl is TextBox)
            {
                ctrl.CallBack[(int)ControlState.KeyUp]?.Invoke();
            }
        }
    }

    public static Control? GetActiveControl()
    {
        return ActiveWindow?.ActiveControl;
    }

    /// <summary>
    /// Moves focus to the next enabled, visible, and focusable control in the active window.
    /// </summary>
    public static void FocusNextControl()
    {
        if (ActiveWindow?.Controls is not { Count: > 0 })
        {
            return;
        }

        var controls = ActiveWindow.Controls;
        var currentIndex = ActiveWindow.ActiveControl is null ? -1 : controls.IndexOf(ActiveWindow.ActiveControl);
        var nextIndex = (currentIndex + 1) % controls.Count;

        while (nextIndex != currentIndex)
        {
            var control = controls[nextIndex];
            if (control is { Enabled: true, Visible: true } and TextBox)
            {
                ActiveWindow.ActiveControl = control;
                return;
            }

            nextIndex = (nextIndex + 1) % controls.Count;
        }
    }

    // Overload that supports custom insertion index and returns created GroupBox
    public static GroupBox CreateGroupBox(int windowIndex, string name, int left, int top, int width, int height, string caption, Design design, int insertIndex)
    {
        if (!Windows.TryGetValue(windowIndex, out var window))
        {
            throw new UIException($"{windowIndex} is not a valid window index.");
        }

        var group = new GroupBox
        {
            Name = name,
            X = left,
            Y = top,
            Width = width,
            Height = height,
            Visible = true,
            Text = caption,
            ZOrder = ZOrderCon,
            Design = design,
            Enabled = false // visual container; ignore hit-testing
        };

        if (insertIndex >= 0 && insertIndex <= window.Controls.Count)
        {
            window.Controls.Insert(insertIndex, group);
        }
        else
        {
            window.Controls.Add(group);
        }

        ZOrderCon++;
        return group;
    }

    // Backward-compatible overload
    public static void CreateGroupBox(int windowIndex, string name, int left, int top, int width, int height, string caption = "", Design design = Design.None)
    {
        CreateGroupBox(windowIndex, name, left, top, width, height, caption, design, -1);
    }

    // Toggle visibility for a GroupBox and its associated child controls recorded by the loader
    public static void SetGroupVisible(Window window, GroupBox group, bool visible)
    {
        if (window == null || group == null)
        {
            return;
        }

        group.Visible = visible;

        // If loader tracked child range, toggle children in that range
        var start = Math.Max(0, group.FirstChildIndex);
        var end = Math.Min(window.Controls.Count - 1, group.LastChildIndex);
        if (start <= end)
        {
            for (int i = start; i <= end; i++)
            {
                window.Controls[i].Visible = visible;
            }
        }
    }
}