using Core.Globals;
using Microsoft.Xna.Framework;
using System.IO;

namespace Client.Game.UI;

public static class WindowRenderer
{
    public static void Render(Window window)
    {
        if (window.Design[0] == Design.ComboMenu)
        {
            // Draw a solid black background area slightly larger than the items
            // to improve readability of combo menu entries.
            DesignRenderer.Render(Design.TextBlack,
                window.X - 2,
                window.Y - 2,
                window.Width + 4,
                window.Height + 4);

            var path = Path.Combine(DataPath.Gui, "1");

            if (window.List.Count == 0)
            {
                return;
            }

            var y = window.Y + 2;
            var x = window.X;
            int visibleRows = Math.Max(1, (window.Height - 2) / 16);
            int maxStart = Math.Max(0, window.List.Count - visibleRows);
            int start = Math.Clamp(window.ScrollOffset, 0, maxStart);

            // Use the same render scale as TextRenderer.RenderText
            const float scale = 12f / 16f;

            for (var row = 0; row < visibleRows; row++)
            {
                int i = start + row;
                if (i >= window.List.Count) break;

                if (i == window.Value || i == window.Group)
                {
                    GameClient.RenderTexture(ref path, x, y - 1, 0, 0, window.Width, 15, 255, 0, 0, 0);
                }

                var line = window.List[i];
                var lineWidth = TextRenderer.GetTextWidth(line, window.Font, scale);
                var left = x + (window.Width - lineWidth) / 2;

                TextRenderer.RenderText(line, left, y, Color.White, Color.Black, window.Font);

                y += 16;
            }

            return;
        }

        switch (window.Design[(int) window.State])
        {
            case Design.WindowBlack:
                RenderWindowBlack(window);
                break;

            case Design.WindowNormal:
                RenderWindowNormal(window);
                break;

            case Design.WindowNoBar:
                RenderWindowNoBar(window);
                break;

            case Design.WindowEmpty:
                RenderWindowEmpty(window);
                break;

            case Design.WindowDescription:
                RenderWindowDescription(window);
                break;

            case Design.WindowWithShadow:
                RenderWindowWithShadow(window);
                break;

            case Design.WindowParty:
                RenderWindowParty(window);
                break;
        }

        window.OnDraw?.Invoke();
    }

    private static void RenderWindowBlack(Window window)
    {
        var path = Path.Combine(DataPath.Gui, "61");

        GameClient.RenderTexture(ref path, window.X, window.Y, 0, 0, window.Width, window.Height, 190, 255, 255);
    }

    private static void RenderWindowNormal(Window window)
    {
        var path = Path.Combine(DataPath.Items, window.Icon.ToString());

        DesignRenderer.Render(Design.Wood, window.X, window.Y, window.Width, window.Height);
        DesignRenderer.Render(Design.Green, window.X, window.Y, window.Width, 23);

        GameClient.RenderTexture(ref path,
            window.X + window.XOffset,
            window.Y - 16 + window.YOffset, 0, 0,
            window.Width, window.Height,
            window.Width, window.Height);

        TextRenderer.RenderText(window.Text, window.X + 32, window.Y + 4, Color.White, Color.Black);
    }

    private static void RenderWindowNoBar(Window window)
    {
        DesignRenderer.Render(Design.Wood, window.X, window.Y, window.Width, window.Height);
    }

    private static void RenderWindowEmpty(Window window)
    {
        var path = Path.Combine(DataPath.Items, window.Icon.ToString());

        if (window.Icon <= 0 || window.Icon > GameState.NumItems)
        {
            return;
        }
        
        DesignRenderer.Render(Design.WoodEmpty, window.X, window.Y, window.Width, window.Height);
        DesignRenderer.Render(Design.Green, window.X, window.Y, window.Width, 23);

        GameClient.RenderTexture(ref path,
            window.X + window.XOffset,
            window.Y - 16 + window.YOffset, 0, 0,
            window.Width, window.Height,
            window.Width, window.Height);

        TextRenderer.RenderText(window.Text, window.X + 32, window.Y + 4, Color.White, Color.Black);
    }

    private static void RenderWindowDescription(Window window)
    {
        DesignRenderer.Render(Design.WindowDescription, window.X, window.Y, window.Width, window.Height);
    }

    private static void RenderWindowWithShadow(Window window)
    {
        DesignRenderer.Render(Design.WindowWithShadow, window.X, window.Y, window.Width, window.Height);
    }

    private static void RenderWindowParty(Window window)
    {
        DesignRenderer.Render(Design.WindowParty, window.X, window.Y, window.Width, window.Height);
    }
}