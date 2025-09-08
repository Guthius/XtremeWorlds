using Client.Net;
using Core.Configurations;
using Core.Globals;

namespace Client.Game.UI.Windows;

public static class WinLogin
{
    public static void OnExit()
    {
    // Request the game to exit; Exiting handler will call DestroyGame and end Eto
    try { General.Client.Exit(); } catch { General.DestroyGame(); }
    }
    
    public static void OnLogin()
    {
        var window = WindowManager.GetWindowByName("winLogin");
        if (window is null)
        {
            return;
        }

        var username = window.GetChild("txtUsername").Text;
        var password = window.GetChild("txtPassword").Text;

        if (Network.IsConnected)
        {
            Sender.SendLogin(username, password);
        }
        else
        {
            GameLogic.Dialogue("Invalid Connection", "Cannot connect to game server.", "Please try again.", DialogueType.Alert);
        }
    }

    public static void OnRegister()
    {
        if (!Network.IsConnected)
        {
            GameLogic.Dialogue(
                "Invalid Connection",
                "Cannot connect to game server.",
                "Please try again.",
                DialogueType.Alert);

            return;
        }

        WindowManager.HideWindows();

        WinRegister.ClearPasswords();

        WindowManager.ShowWindow("winRegister");
    }

    public static void OnSaveUserClicked()
    {
        var winLogin = WindowManager.GetWindowByName("winLogin");
        
        var checkBoxSaveUsername = winLogin.GetChild("chkSaveUsername");
        if (checkBoxSaveUsername.Value == 0)
        {
            SettingsManager.Instance.SaveUsername = false;
            SettingsManager.Instance.Username = "";
        }
        else
        {
            SettingsManager.Instance.SaveUsername = true;
        }

        SettingsManager.Save();
    }
}