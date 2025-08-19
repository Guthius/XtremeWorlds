namespace Client.Game.UI;

public abstract class Component
{
    public Component? Parent { get; set; }
    public string Name { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Visible { get; set; }

    public event Action? Draw;
    public event Action? MouseDown;
    public event Action? MouseUp;
    public event Action? Click;

    public bool Contains(int x, int y)
    {
        return x >= X && x <= X + Width && y >= Y && y <= Y + Height;
    }

    private (int X, int Y) GetScreenPosition()
    {
        if (Parent is null)
        {
            return (X, Y);
        }

        var (x, y) = Parent.GetScreenPosition();

        return (x + X, y + Y);
    }

    /// <summary>
    /// Converts the specified screen (global) coordinates to local coordinates.
    /// </summary>
    /// <param name="x">The screen X coordinate</param>
    /// <param name="y">The screen Y coordinate</param>
    /// <returns>The local coordinates</returns>
    public (int X, int Y) PointToLocal(int x, int y)
    {
        if (Parent is null)
        {
            return (x - X, y - Y);
        }

        var (parentX, parentY) = Parent.GetScreenPosition();

        return (x - parentX, y - parentY);
    }

    /// <summary>
    /// Converts the specified local coordinates to screen coordinates.
    /// </summary>
    /// <param name="x">The local X coordinate</param>
    /// <param name="y">The local Y coordinate</param>
    /// <returns>The screen coordinates</returns>
    protected (int X, int Y) PointToScreen(int x, int y)
    {
        if (Parent is null)
        {
            return (X + x, Y + y);
        }

        var (parentX, parentY) = Parent.GetScreenPosition();

        return (parentX + x, parentY + y);
    }

    protected virtual void OnDraw()
    {
        Draw?.Invoke();
    }

    public void HandleMouseMove(int x, int y)
    {
        OnMouseMove(x, y);
    }

    protected virtual void OnMouseMove(int x, int y)
    {
    }

    public void HandleMousePressed(int x, int y)
    {
        OnMousePressed(x, y);
    }

    protected virtual void OnMousePressed(int x, int y)
    {
        MouseDown?.Invoke();
    }

    public void HandleMouseReleased(int x, int y)
    {
        OnMouseReleased(x, y);
    }

    protected virtual void OnMouseReleased(int x, int y)
    {
        MouseUp?.Invoke();
    }

    protected virtual void OnClick()
    {
        Click?.Invoke();
    }
}