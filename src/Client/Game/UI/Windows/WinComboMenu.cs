using Client.Game.UI.Controls;

namespace Client.Game.UI.Windows;

public class WinComboMenu
{
    private static TextBox? _previousTextBox;
    private static Window? _previousTextBoxWindow;

    /// <summary>
    /// Returns true if the combo menu is currently open for the given window/control.
    /// </summary>
    public static bool IsOpen(Window window, int controlIndex)
    {
        var winComboMenu = WindowManager.GetWindowByName("winComboMenu");
        if (winComboMenu is null || !winComboMenu.Visible)
            return false;
        // Check if the menu is for the current ComboBox
        if (window.Controls[controlIndex] is ComboBox comboBox && winComboMenu.ParentControl == comboBox)
            return true;
        return false;
    }

    private static void CapturePreviousTextBoxFocus()
    {
        // Focus in this UI is represented by ActiveWindow.ActiveControl and is only
        // meaningful for TextBox controls.
        if (WindowManager.ActiveWindow?.ActiveControl is TextBox tb)
        {
            _previousTextBoxWindow = WindowManager.ActiveWindow;
            _previousTextBox = tb;
        }
    }

    private static void RestorePreviousTextBoxFocus()
    {
        if (_previousTextBoxWindow is null || _previousTextBox is null)
        {
            return;
        }

        try
        {
            if (_previousTextBoxWindow.Visible &&
                _previousTextBox.Visible &&
                _previousTextBox.Enabled &&
                _previousTextBoxWindow.Controls.Contains(_previousTextBox))
            {
                WindowManager.ActiveWindow = _previousTextBoxWindow;
                _previousTextBoxWindow.ActiveControl = _previousTextBox;

                // Optional: fire any focus-enter callback if wired.
                if (_previousTextBox.CallBack.Count > (int)ControlState.FocusEnter)
                {
                    _previousTextBox.CallBack[(int)ControlState.FocusEnter]?.Invoke();
                }
            }
        }
        finally
        {
            _previousTextBoxWindow = null;
            _previousTextBox = null;
        }
    }
    
    public static void Close()
    {
        WindowManager.HideWindow("winComboMenuBG");
        WindowManager.HideWindow("winComboMenu");

        // When the combo menu closes, return focus to the previously focused TextBox.
        RestorePreviousTextBoxFocus();
    }

    public static void Show(Window window, int controlIndex)
    {
        CapturePreviousTextBoxFocus();

        if (window.Controls[controlIndex] is not ComboBox comboBox)
        {
            return;
        }

        var winComboMenu = WindowManager.GetWindowByName("winComboMenu");
        if (winComboMenu is null)
        {
            return;
        }

        winComboMenu.ParentControl = comboBox;
        winComboMenu.X = window.X + comboBox.X + 2;

        // Desired dropdown position (below control)
        var y = window.Y + comboBox.Y + comboBox.Height;
        winComboMenu.Y = y;

        // Populate list and selection
        winComboMenu.List = comboBox.Items;
        winComboMenu.Value = comboBox.Value;
        winComboMenu.Group = 0;

        // Compute height with clamp to available screen space; enable scrolling via ScrollOffset
        int desiredHeight = 2 + comboBox.Items.Count * 16;
        int availableBelow = GameState.ResolutionHeight - y - 10; // 10px margin from bottom
        int height = desiredHeight;
        if (availableBelow < desiredHeight)
        {
            // If not enough space below, try placing above; else clamp
            int availableAbove = y - 10; // margin from top
            if (availableAbove > availableBelow && availableAbove > 2)
            {
                // Place above control
                height = Math.Min(desiredHeight, availableAbove);
                winComboMenu.Y = y - height - comboBox.Height;
            }
            else
            {
                height = Math.Max(34, availableBelow); // at least 2 visible rows
            }
        }
        winComboMenu.Height = height;
        winComboMenu.Width = comboBox.Width - 4;
        // Center selection within visible area
        int visibleRows = Math.Max(1, (winComboMenu.Height - 2) / 16);
        int maxStart = Math.Max(0, winComboMenu.List.Count - visibleRows);
        winComboMenu.ScrollOffset = Math.Clamp(comboBox.Value - visibleRows / 2, 0, maxStart);
        winComboMenu.Visible = true;

        WindowManager.ShowWindow("winComboMenuBG", true, false);
        WindowManager.ShowWindow("winComboMenu", true, false);
    }
}