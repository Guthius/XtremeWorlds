using Core.Globals;

namespace Client.Game.UI;

public sealed class Window : Component
{
    public int InitialX { get; set; }
    public int InitialY { get; set; }
    public int MovedX { get; set; }
    public int MovedY { get; set; }
    public bool CanDrag { get; set; } = true;
    public Font Font { get; set; }
    public string Text { get; set; } = string.Empty;
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public int Icon { get; set; }
    public long Value { get; set; }
    public int Group { get; set; }
    public byte ZChange { get; set; }
    public int ZOrder { get; set; }
    public Action? OnDraw { get; set; }
    public bool ClickThrough { get; set; }
    
    public Control? ParentControl { get; set; }

    public ControlState State { get; set; }
    public List<string> List { get; set; } = []; // Drop down items?
    // For scrollable popups (e.g., combo menus): starting item index
    public int ScrollOffset { get; set; }

    // Arrays for states
    public List<Design> Design { get; set; } = [];
    public List<int>? Image { get; set; }
    public List<Action?> CallBack { get; set; } = [];

    // Controls in this window
    public List<Control> Controls { get; } = [];
    public Control? LastControl { get; set; }
    public Control? ActiveControl { get; set; }

    public Control GetChild(string controlName)
    {
        // 1) Exact match (existing behavior)
        foreach (var control in Controls)
        {
            if (string.Equals(control.Name, controlName, StringComparison.CurrentCultureIgnoreCase))
            {
                return control;
            }
        }

        // 2) Fallback: match by last segment to support group-scoped names like "group/child",
        //    "group:child", "group.child" or "group\\child".
        static string LastSegment(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            // Consider common separators for grouping
            int i1 = name.LastIndexOf('/');
            int i2 = name.LastIndexOf('\\');
            int i3 = name.LastIndexOf(':');
            int i4 = name.LastIndexOf('.');
            int idx = Math.Max(Math.Max(i1, i2), Math.Max(i3, i4));
            return idx >= 0 && idx + 1 < name.Length ? name[(idx + 1)..] : name;
        }

        var wanted = LastSegment(controlName);
        foreach (var control in Controls)
        {
            var tail = LastSegment(control.Name);
            if (string.Equals(tail, wanted, StringComparison.CurrentCultureIgnoreCase))
            {
                return control;
            }
        }

        throw new InvalidOperationException("Control not found: " + controlName);
    }
    
    public bool Contains(int x, int y)
    {
        return x >= X && x <= X + Width && y >= Y && y <= Y + Height;
    }
}