namespace Client.Game.UI.Windows;

public class WinMenu
{
    public static void OnCharacterClick()
    {
        var windowIndex = WindowManager.GetWindow("winCharacter");

        if (WindowManager.Windows[windowIndex].Visible)
        {
            WindowManager.HideWindow(windowIndex);
        }
        else
        {
            WindowManager.ShowWindow(windowIndex, resetPosition: false);
        }
    }

    public static void OnInventoryClick()
    {
        var windowIndex = WindowManager.GetWindow("winInventory");

        if (WindowManager.Windows[windowIndex].Visible)
        {
            WindowManager.HideWindow(windowIndex);
        }
        else
        {
            WindowManager.ShowWindow(windowIndex, resetPosition: false);
        }
    }

    public static void OnSkillsClick()
    {
        var windowIndex = WindowManager.GetWindow("winSkills");

        if (WindowManager.Windows[windowIndex].Visible)
        {
            WindowManager.HideWindow(windowIndex);
        }
        else
        {
            WindowManager.ShowWindow(windowIndex, resetPosition: false);
        }
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