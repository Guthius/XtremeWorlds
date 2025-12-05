using Client.Net;
using Core.Globals;

namespace Client.Game.UI.Windows;

public class WinRegister
{
    public static void OnRegister()
    {
        var winRegister = WindowManager.GetWindowByName("winRegister");
        if (winRegister is null)
        {
            return;
        }

        var username = winRegister.GetChild("txtUsername").Text;
        var password1 = winRegister.GetChild("txtPassword").Text;
        var password2 = winRegister.GetChild("txtRetypePassword").Text;

        if (password1 != password2)
        {
            GameLogic.Dialogue(
                "Register",
                "Passwords don't match.",
                "Please try again.",
                DialogueType.Alert);

            ClearPasswords();

            return;
        }

        if (!Network.IsConnected)
        {
            GameLogic.Dialogue(
                "Invalid Connection",
                "Cannot connect to game server.",
                "Please try again.",
                DialogueType.Alert);

            return;
        }

        Sender.SendRegister(username, password1);
    }

    public static void OnClose()
    {
        WindowManager.HideWindows();

        WindowManager.ShowWindow("winLogin");
    }

    public static void ClearPasswords()
    {
        var winRegister = WindowManager.GetWindowByName("winRegister");
        if (winRegister != null)
        {
            var pwd1 = winRegister.GetChild("txtPassword");
            if (pwd1 != null) pwd1.Text = "";
            var pwd2 = winRegister.GetChild("txtRetypePassword");
            if (pwd2 != null) pwd2.Text = "";
        }

        var winLogin = WindowManager.GetWindowByName("winLogin");
        if (winLogin != null)
        {
            var pwd = winLogin.GetChild("txtPassword");
            if (pwd != null) pwd.Text = "";
        }
        
    }
}