using Core.Globals;

namespace Client.Game.UI;

public sealed class Window : Component
{
    private Control? _mouseOverControl;
    private bool _dragging;
    private int _dragOffsetX;
    private int _dragOffsetY;

    public int InitialX { get; set; }
    public int InitialY { get; set; }
    public int MovedX { get; set; }
    public int MovedY { get; set; }
    
    public bool CanDrag { get; set; } = true;
    public bool CanFocus { get; set; } = true;
    
    public Font Font { get; set; }
    public string Text { get; set; } = string.Empty;
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public int Icon { get; set; }
    public long Value { get; set; }
    public int Group { get; set; }
    public bool Clickthrough { get; set; }
    public Control? ParentControl { get; set; }
    public ControlState State { get; set; }
    public List<string> List { get; set; } = []; // Drop down items?

    // Arrays for states
    public List<Design> Design { get; set; } = [];
    public List<int>? Image { get; set; }
    public List<Action?> CallBack { get; set; } = [];

    // Controls in this window
    public List<Control> Controls { get; } = [];
    public Control? LastControl { get; set; }
    public Control? ActiveControl { get; set; }

    public void Render()
    {
        OnDraw();
    }

    public T GetChild<T>(string controlName) where T : Control
    {
        foreach (var control in Controls)
        {
            if (!string.Equals(control.Name, controlName, StringComparison.CurrentCultureIgnoreCase))
            {
                continue;
            }

            if (control is not T result)
            {
                throw new UIException("Control " + controlName + " is not of type " + typeof(T).Name);
            }

            return result;
        }

        throw new UIException("Control not found: " + controlName);
    }

    public Control GetChild(string controlName)
    {
        foreach (var control in Controls)
        {
            if (string.Equals(control.Name, controlName, StringComparison.CurrentCultureIgnoreCase))
            {
                return control;
            }
        }

        throw new UIException("Control not found: " + controlName);
    }

    protected override void OnMouseMove(int x, int y)
    {
        if (_dragging)
        {
            (x, y) = PointToScreen(x, y);

            X = x - _dragOffsetX;
            Y = y - _dragOffsetY;
            return;
        }

        if (_mouseOverControl is not null)
        {
            _mouseOverControl.HandleMouseLeave();
            _mouseOverControl = null;
        }

        for (var index = Controls.Count - 1; index >= 0; index--)
        {
            var control = Controls[index];
            if (!control.Visible)
            {
                continue;
            }

            if (!control.Contains(x, y))
            {
                continue;
            }

            if (_mouseOverControl != control)
            {
                _mouseOverControl?.HandleMouseLeave();
            }

            _mouseOverControl = control;
            _mouseOverControl.HandleMouseEnter();
            _mouseOverControl.HandleMouseMove(x, y);
            return;
        }
    }

    protected override void OnMousePressed(int x, int y)
    {
        Gui.MoveToFront(this);

        if (_mouseOverControl is not null &&
            _mouseOverControl.Contains(x, y))
        {
            _mouseOverControl.HandleMousePressed(x, y);
            return;
        }

        if (Design[0] != UI.Design.WindowNormal || !CanDrag || y > 23)
        {
            return;
        }

        _dragging = true;
        _dragOffsetX = x;
        _dragOffsetY = y;

        Gui.CaptureMouse(this);
    }

    protected override void OnMouseReleased(int x, int y)
    {
        if (_dragging)
        {
            _dragging = false;

            Gui.ReleaseMouse();
            return;
        }

        if (_mouseOverControl is not null &&
            _mouseOverControl.Contains(x, y))
        {
            _mouseOverControl.HandleMouseReleased(x, y);
        }
    }
}