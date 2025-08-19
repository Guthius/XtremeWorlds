using Core.Globals;

namespace Client.Game.UI.Windows;

public static class WinDialogue
{
    public static void OnOkay()
    {
        GameLogic.DialogueHandler(1);
    }

    public static void OnYes()
    {
        GameLogic.DialogueHandler(2);
    }

    public static void OnNo()
    {
        GameLogic.DialogueHandler(3);
    }

    public static void OnClose()
    {
        switch (GameState.DiaStyle)
        {
            case DialogueStyle.Okay:
                GameLogic.DialogueHandler(1);
                break;

            case DialogueStyle.YesNo:
                GameLogic.DialogueHandler(3);
                break;
        }
    }
}