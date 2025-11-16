using Core.Globals;

namespace Client.Game.UI.Controls;

public sealed class ScrollBar : Control
{
    public int Min { get; set; }
    public int Max { get; set; } = 100;
    public bool Vertical { get; set; } = true;
    public int ThumbSize { get; set; } = 16;

    public override void Render(int x, int y)
    {
        // Track
        DesignRenderer.Render(Design.TextBlack, X + x, Y + y, Width, Height, Alpha);

        var range = Math.Max(1, Max - Min);
        var clamped = Math.Clamp(Value, Min, Max);
        var t = (float)(clamped - Min) / range;

        if (Vertical)
        {
            var usable = Math.Max(0, Height - ThumbSize);
            var thumbY = Y + y + (int)(usable * t);
            DesignRenderer.Render(Design.Green, X + x, thumbY, Width, ThumbSize, Alpha);
        }
        else
        {
            var usable = Math.Max(0, Width - ThumbSize);
            var thumbX = X + x + (int)(usable * t);
            DesignRenderer.Render(Design.Green, thumbX, Y + y, ThumbSize, Height, Alpha);
        }
    }
}
