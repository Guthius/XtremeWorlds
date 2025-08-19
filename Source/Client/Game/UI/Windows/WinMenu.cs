namespace Client.Game.UI.Windows;

public static class WinMenu
{
    private static void ToggleWindow(string windowName)
    {
        var window = Gui.GetWindowByName(windowName);
        if (window is null)
        {
            return;
        }

        if (window.Visible)
        {
            Gui.HideWindow(windowName);
        }
        else
        {
            Gui.ShowWindow(windowName, resetPosition: false);
        }
    }

    public static void OnCharacterClick()
    {
        ToggleWindow("winCharacter");
    }

    public static void OnInventoryClick()
    {
        ToggleWindow("winInventory");
    }

    public static void OnSkillsClick()
    {
        ToggleWindow("winSkills");
    }

    public static void OnMapClick()
    {
        // TODO: Implement map window
    }

    public static void OnGuildClick()
    {
        // TODO: Implement guild window
    }

    public static void OnQuestClick()
    {
        // TODO: Implement quest window
    }
}