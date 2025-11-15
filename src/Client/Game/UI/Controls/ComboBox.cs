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
                    var left = X + x + Math.Max(0, (Width - tw) / 2);
                    var top = Y + y; // keep baseline; dropdown items are centered horizontally
                    TextRenderer.RenderText(text, left, top, Color, Color.Black);
                }

                var path = Path.Combine(Texture[0], ArrowSprite.ToString());

                // Draw arrow inside the control bounds near the right edge
                var arrowW = 5;
                var arrowH = 4;
                var arrowX = X + x + Width - arrowW - 3;
                var arrowY = Y + y + (Height - arrowH) / 2;
                GameClient.RenderTexture(ref path, arrowX, arrowY, 0, 0, arrowW, arrowH, arrowW, arrowH);
                break;
        }
    }
}