using Client.Net;
using Core.Globals;
using static Core.Globals.Command;

namespace Client.Game.UI.Windows;

public class WinPlayerMenu
{
    public static void OnClose()
    {
        WindowManager.HideWindow("winRightClickBG");
        WindowManager.HideWindow("winPlayerMenu");
    }
    
    public static void OnPartyInvite()
    {
        OnClose();
        
        Sender.SendPartyRequest(GetPlayerName((int) GameState.PlayerMenuIndex));
    }

    public static void OnTradeRequest()
    {
        OnClose();
        
        Sender.SendTradeRequest(GetPlayerName((int) GameState.PlayerMenuIndex));
    }

    public static void OnGuildInvite()
    {
        OnClose();
        
        TextRenderer.AddText("System not yet in place.", (int) ColorName.BrightRed);
    }

    public static void OnPrivateMessage()
    {
        OnClose();
        
        TextRenderer.AddText("System not yet in place.", (int) ColorName.BrightRed);
    }
}