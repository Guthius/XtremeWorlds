namespace Client.Game.UI.Controls;

public sealed class TextBox : Control
{
    public bool Censor { get; set; }
    
    public override void Render(int x, int y)
    {
        var design = Design;
        if (design != Design.None)
        {
            DesignRenderer.Render(design, X + x, Y + y, Width, Height, Alpha);
        }
        
        string input = null;

        if (Gui.ActiveWindow?.ActiveControl == this)
        {
            input = GameState.ChatShowLine;
        }

        var text = ((Censor ? TextRenderer.CensorText(Text) : Text) + input).Replace("\0", string.Empty);
        var textSize = TextRenderer.Fonts[Font].MeasureString(text);

        TextRenderer.RenderText(
            text,
            X + x + XOffset,
            Y + y + YOffset + (int) (Height - textSize.Y) / 2,
            Color,
            Microsoft.Xna.Framework.Color.Black,
            Font);
        
        OnDraw();
    }
}