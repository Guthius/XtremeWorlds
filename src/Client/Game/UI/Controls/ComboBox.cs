using System;
using Microsoft.Xna.Framework;
using System.IO;

namespace Client.Game.UI.Controls;

public sealed class ComboBox : Control
{
    private const int ArrowSprite = 66;

    public List<string> Items { get; } = [];

    public new int Value { get; set; }

    public override void Render(int x, int y)
    {
        // Gradient background (new) instead of transparent/text black panel
        DesignRenderer.Render(Design.ComboBox, X + x, Y + y, Width, Height);

        // Center the selected item's text based on its width at render scale
        const float scale = 12f / 16f;

        if (Items.Count > 0 && Value >= 0 && Value < Items.Count)
        {
            var text = Items[Value];
            var textWidth = TextRenderer.GetTextWidth(text, Font, scale);

            // Reserve space for the dropdown arrow when centering text
            const int arrowW = 5;
            const int paddingL = 3;
            int paddingR = arrowW + 6; // arrow width + margin
            int innerWidth = Math.Max(0, Width - paddingL - paddingR);

            int left = X + x + paddingL + Math.Max(0, (innerWidth - textWidth) / 2);
            int top = Y + y + 2; // vertical baseline consistent with other controls

            TextRenderer.OnRender(text, left, top, Color, Color.Black, Font);
        }

        var path = Path.Combine(Texture[0], ArrowSprite.ToString());

        // Draw arrow inside the control bounds near the right edge
        const int arrowW2 = 5;
        const int arrowH = 4;
        int arrowX = X + x + Width - arrowW2 - 3;
        int arrowY = Y + y + (Height - arrowH) / 2;
        GameClient.RenderTexture(ref path, arrowX, arrowY, 0, 0, arrowW2, arrowH, arrowW2, arrowH);
    }
}