using Core.Globals;
using Microsoft.Xna.Framework;

namespace Client.Game.UI;

public abstract class Control : Component
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
    
    public Font Font { get; set; } = Font.Georgia;
    public Color Color { get; set; } = Color.White;
    public int Alpha { get; set; } = 255;
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public bool Enabled { get; set; } = true;
    public string Tooltip { get; set; } = string.Empty;
    public Design Design { get; set; } = Design.None;

    public int? Image { get; set; }
    
    public event Action? MouseEnter;
    public event Action? MouseLeave;
    
    public abstract void Render(int x, int y);
    
    public void HandleMouseEnter()
    {
        OnMouseEnter();
    }

    protected virtual void OnMouseEnter()
    {
        MouseEnter?.Invoke();
    }
    
    public void HandleMouseLeave()
    {
       OnMouseLeave();
    }

    protected virtual void OnMouseLeave()
    {
        MouseLeave?.Invoke();
    }
}