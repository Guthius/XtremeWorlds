using System;
using Microsoft.Xna.Framework;
using System.IO;

namespace Client.Game.UI.Controls;

public sealed class ComboBox : Control
{
    private const int ArrowSprite = 66;

    public List<string> Items { get; } = [];

    // Add a public property for Value (selection index)
    public new int Value { get; set; }

    public override void Render(int x, int y)
    {
        switch (Design)
        {
            case Design.ComboBoxNormal:
                DesignRenderer.Render(Design.TextBlack, X + x, Y + y, Width, Height);

                // Always display the selected item if Value is in range
                if (Items.Count > 0 && Value >= 0 && Value < Items.Count)
                {
                    var text = Items[Value];
                    var tw = TextRenderer.GetTextWidth(text, Font);

                    // Reserve space for the dropdown arrow when centering text
                    var arrowW = 5;
                    var paddingL = 3;
                    var paddingR = arrowW + 6; // arrow width + margin
                    var innerWidth = Math.Max(0, Width - paddingL - paddingR);
                    var left = X + x + paddingL + Math.Max(0, (innerWidth - tw) / 2);
                    var top = Y + y; // vertical baseline consistent with other controls
                    TextRenderer.RenderText(text, left, top, Color, Color.Black);
                }

                var path = Path.Combine(Texture[0], ArrowSprite.ToString());

                // Draw arrow inside the control bounds near the right edge
                var arrowW2 = 5;
                var arrowH = 4;
                var arrowX = X + x + Width - arrowW2 - 3;
                var arrowY = Y + y + (Height - arrowH) / 2;
                GameClient.RenderTexture(ref path, arrowX, arrowY, 0, 0, arrowW2, arrowH, arrowW2, arrowH);
                break;
        }
    }
}