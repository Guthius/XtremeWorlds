using Core.Globals;
using Microsoft.Xna.Framework;

namespace Client.Game.UI.Controls;

public sealed class Button : Control
{
    private bool _isMouseOver;
    private bool _isMousePressed;

    public int Icon { get; set; }
    public Design? DesignHover { get; set; }
    public Design? DesignMouseDown { get; set; }
    public int? ImageHover { get; set; }
    public int? ImageMouseDown { get; set; }

    public override void Render(int x, int y)
    {
        var design = GetDesign();
        if (design != Design.None)
        {
            DesignRenderer.Render(design, X + x, Y + y, Width, Height);
        }

        var image = GetImage();
        if (image is not null)
        {
            var path = Path.Combine(DataPath.Gui, image.Value.ToString());

            GameClient.RenderTexture(ref path,
                X + x,
                Y + y, 0, 0,
                Width, Height,
                Width, Height);
        }

        if (Icon > 0)
        {
            var gfxInfo = GameClient.GetGfxInfo(Path.Combine(DataPath.Items, Icon.ToString()));
            if (gfxInfo is not null)
            {
                var path = Path.Combine(DataPath.Items, Icon.ToString());

                GameClient.RenderTexture(ref path,
                    X + x + XOffset,
                    Y + y + YOffset, 0, 0,
                    gfxInfo.Width, gfxInfo.Height,
                    gfxInfo.Width, gfxInfo.Height);
            }
        }

        var size = TextRenderer.Fonts[Font].MeasureString(Text);

        var paddingX = size.X / 6.0d;
        var paddingY = size.Y / 6.0d;

        var textX = X + x + XOffset + (Width - size.X) / 2 + paddingX - 4;
        var textY = Y + y + YOffset + (Height - size.Y) / 2 + paddingY;

        TextRenderer.RenderText(Text,
            (int) Math.Round(textX),
            (int) Math.Round(textY),
            Color, Color.Black,
            Font);

        OnDraw();
    }

    private Design GetDesign()
    {
        Design? design = null;

        if (_isMousePressed)
        {
            design = DesignMouseDown;
        }
        else if (_isMouseOver)
        {
            design = DesignHover;
        }

        return design ?? Design;
    }

    private int? GetImage()
    {
        int? image = null;

        if (_isMousePressed)
        {
            image = ImageMouseDown;
        }
        else if (_isMouseOver)
        {
            image = ImageHover;
        }

        return image ?? Image;
    }

    protected override void OnMouseEnter()
    {
        _isMouseOver = true;

        base.OnMouseEnter();
    }

    protected override void OnMouseLeave()
    {
        _isMouseOver = false;

        base.OnMouseLeave();
    }

    protected override void OnMousePressed(int x, int y)
    {
        Gui.CaptureMouse(this);

        _isMousePressed = true;

        base.OnMousePressed(x, y);
    }

    protected override void OnMouseReleased(int x, int y)
    {
        Gui.ReleaseMouse();

        _isMousePressed = false;

        if (Contains(x, y))
        {
            OnClick();
        }

        base.OnMouseReleased(x, y);
    }
}