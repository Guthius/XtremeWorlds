using Client.Net;
using Core.Globals;

namespace Client.Game.UI.Windows;

public class WinRespawn
{
    public static void OnClick()
    {
        if (GameState.MyIndex < 0 || GameState.MyIndex >= Player.Instance.Count)
        {
            return;
        }

        var remaining = Player.Instance[GameState.MyIndex].DeathTimer - Client.General.GetTickCount();
        if (remaining <= 0)
        {
            return;
        }

        Sender.RespawnNow();
    }
}
