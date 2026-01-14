using Client.Net;
using Core.Globals;

namespace Client.Game.UI.Windows;

public static class WinEventChat
{
    private static readonly string[] EventChoiceButtons =
    [
        "btnEventChoice1",
        "btnEventChoice2",
        "btnEventChoice3",
        "btnEventChoice4",
    ];

    public static bool IsVisible
    {
        get
        {
            return WindowManager.TryGetWindow("winEventChat", out var window) && window is not null && window.Visible;
        }
    }

    private static void EnsureWired()
    {
        if (!WindowManager.TryGetWindow("winEventChat", out var window) || window is null)
        {
            return;
        }

        window.CanDrag = false;

        if (WindowManager.TryGetControl("winEventChat", "btnEventChoice1", out var b1))
        {
            b1.CallBack[(int)ControlState.MouseDown] = OnChoice1;
        }

        if (WindowManager.TryGetControl("winEventChat", "btnEventChoice2", out var b2))
        {
            b2.CallBack[(int)ControlState.MouseDown] = OnChoice2;
        }

        if (WindowManager.TryGetControl("winEventChat", "btnEventChoice3", out var b3))
        {
            b3.CallBack[(int)ControlState.MouseDown] = OnChoice3;
        }

        if (WindowManager.TryGetControl("winEventChat", "btnEventChoice4", out var b4))
        {
            b4.CallBack[(int)ControlState.MouseDown] = OnChoice4;
        }
    }

    public static void Show()
    {
        WindowManager.HideWindow("winChat");
        WindowManager.HideWindow("winChatSmall");

        // Show the RPG-style message window.
        WindowManager.ShowWindow("winEventChat", forced: true, resetPosition: false);
        EnsureWired();

        if (WindowManager.TryGetWindow("winEventChat", out var eventWin) && eventWin is not null)
        {
            eventWin.X = (GameState.ResolutionWidth - eventWin.Width) / 2;
            eventWin.Y = GameState.ResolutionHeight - eventWin.Height - 20;
            eventWin.InitialX = eventWin.X;
            eventWin.InitialY = eventWin.Y;
        }

        if (WindowManager.TryGetControl("winEventChat", "lblEventText", out var lbl))
        {
            lbl.Text = Client.Event.EventText ?? string.Empty;
        }

        RefreshChoices();
    }

    public static void OnCancel()
    {
        if (!IsVisible)
        {
            return;
        }

        // Send a negative reply to indicate "cancel/abort".
        SendReply(-1);
    }

    public static void OnEventEnded()
    {
        WindowManager.HideWindow("winEventChat");
        WindowManager.ShowWindow("winChatSmall", resetPosition: false);
    }

    private static void RefreshChoices()
    {
        const string windowName = "winEventChat";

        if (WindowManager.TryGetControl(windowName, "picChoices", out var picChoices))
        {
            picChoices.Visible = Client.Event.EventChatType != 0;
        }

        // ShowText => a single "Continue" action (reply=0).
        if (Client.Event.EventChatType == 0)
        {
            if (WindowManager.TryGetControl(windowName, EventChoiceButtons[0], out var btn0))
            {
                btn0.Text = "Continue";
                btn0.Visible = true;
                btn0.Enabled = true;
            }

            for (var i = 1; i < EventChoiceButtons.Length; i++)
            {
                if (WindowManager.TryGetControl(windowName, EventChoiceButtons[i], out var btn))
                {
                    btn.Visible = false;
                    btn.Enabled = false;
                    btn.Text = string.Empty;
                }
            }

            return;
        }

        // ShowChoices => up to 4 options.
        for (var i = 0; i < EventChoiceButtons.Length; i++)
        {
            var visible = i < Client.Event.EventChoiceVisible.Length && Client.Event.EventChoiceVisible[i];
            var text = i < Client.Event.EventChoices.Length ? Client.Event.EventChoices[i] ?? string.Empty : string.Empty;
            var shouldShow = visible && !string.IsNullOrWhiteSpace(text);

            if (!WindowManager.TryGetControl(windowName, EventChoiceButtons[i], out var btn))
            {
                continue;
            }

            btn.Text = text;
            btn.Visible = shouldShow;
            btn.Enabled = shouldShow;
        }
    }

    private static void SendReply(int reply)
    {
        Sender.EventChatReply(Client.Event.EventReplyId, Client.Event.EventReplyPage, reply);

        // Hide the modal event prompt immediately; server may send another prompt.
        WindowManager.HideWindow("winEventChat");
        WindowManager.ShowWindow("winChatSmall", resetPosition: false);
        Client.Event.ClearEventChat();
    }

    public static void OnChoice1() => OnChoice(1);
    public static void OnChoice2() => OnChoice(2);
    public static void OnChoice3() => OnChoice(3);
    public static void OnChoice4() => OnChoice(4);

    private static void OnChoice(int choice)
    {
        // If this is a ShowText prompt, any button click should be reply=0.
        if (Client.Event.EventChatType == 0)
        {
            SendReply(0);
            return;
        }

        SendReply(choice);
    }
}
