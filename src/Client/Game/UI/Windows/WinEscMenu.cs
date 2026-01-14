using Client.Net;

namespace Client.Game.UI.Windows;

public class WinEscMenu
{
    public static void OnClose()
    {
        WindowManager.HideWindow("winEscMenu");
    }

    public static void OnOptionsClick()
    {
        WindowManager.HideWindow("winEscMenu");
        WindowManager.ShowWindow("winOptions", true);
        GameLogic.SetOptionsScreen();
    }

    public static void OnMainMenuClick()
    {
        // We're going back to a menu screen; ensure flags are consistent
        GameState.InGame = false;
        GameState.InMenu = true;
        WindowManager.HideWindows();

        WindowManager.ShowWindow("winLogin");
        Sender.Logout();
    }

    public static void OnExitClick()
    {
        WindowManager.HideWindow("winEscMenu");

        General.DestroyGame();
    }
}