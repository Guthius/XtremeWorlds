using Core.Globals;

namespace Client.Game.UI.Controls;

public sealed class PictureBox : Control
{
    public string? ImagePath { get; set; }
    
    public override void Render(int x, int y)
    {
        var design = Design;
        if (design != Design.None)
        {
            DesignRenderer.Render(design, X + x, Y + y, Width, Height, Alpha);
        }

        var image = Image;
        if (image is null)
        {
            OnDraw();
            
            return;
        }

        var path = Path.Combine(ImagePath ?? DataPath.Gui, image.Value.ToString());

        GameClient.RenderTexture(ref path, X + x, Y + y, 0, 0, Width, Height, Width, Height, (byte) Alpha);
        
        OnDraw();
    }
}