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

        var usernameCtrl = window.GetChild("txtUsername");
        var passwordCtrl = window.GetChild("txtPassword");
        if (usernameCtrl == null || passwordCtrl == null)
        {
            return;
        }
        var username = usernameCtrl.Text;
        var password = passwordCtrl.Text;

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
        if (winLogin == null) { return; }
        var checkBoxSaveUsername = winLogin.GetChild("chkSaveUsername");
        if (checkBoxSaveUsername == null) { return; }
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