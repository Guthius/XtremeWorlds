using Microsoft.Xna.Framework;
using System;

namespace Client.Game.UI.Controls;

public sealed class GroupBox : Control
{
    private const int CaptionOffsetX = 10;
    private const int CaptionOffsetY = 6;
    private const int BorderPadding = 10;

    public int BackgroundAlpha { get; set; } = 255;

    // Child control range in Window.Controls assigned by loader
    public int FirstChildIndex { get; set; } = -1;
    public int LastChildIndex { get; set; } = -1;

    public override void Render(int x, int y)
    {
        int absX = X + x;
        int absY = Y + y;

        // Default background
        DesignRenderer.Render(Design.Parchment, absX, absY, Width, Height);

        // Caption
        if (!string.IsNullOrWhiteSpace(Text))
        {
            TextRenderer.Render(Text, absX + CaptionOffsetX, absY + CaptionOffsetY, Color.White, Color.Black, Font);
        }
    }

    /// <summary>
    /// Computes a size that fits all following controls in the parent window starting at startIndex.
    /// </summary>
    public static (int width, int height) ComputeFit(Window parent, int startIndex)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        for (int i = startIndex; i < parent.Controls.Count; i++)
        {
            var c = parent.Controls[i];
            minX = Math.Min(minX, c.X);
            minY = Math.Min(minY, c.Y);
            maxX = Math.Max(maxX, c.X + c.Width);
            maxY = Math.Max(maxY, c.Y + c.Height);
        }
        if (minX == int.MaxValue)
        {
            return (Width: 1, Height: 1);
        }
        int w = (maxX - minX) + BorderPadding * 2;
        int h = (maxY - minY) + BorderPadding * 2;
        return (w, h);
    }
}
