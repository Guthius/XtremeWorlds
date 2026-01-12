using Client.Net;
using Core.Globals;

namespace Client.Game.UI.Windows;

public class WinRespawn
{
    public static void OnRespawnClick()
    {
        if (GameState.MyIndex < 0 || GameState.MyIndex >= Player.Instance.Count)
        {
            return;
        }

        var remainingMs = Player.Instance[GameState.MyIndex].DeathTimer - Client.General.GetTickCount();
        if (remainingMs <= 0)
        {
            return;
        }

        Sender.SendRespawnNow();
    }
}
