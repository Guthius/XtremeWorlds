using Microsoft.Xna.Framework;

namespace Client.Game.UI.Controls;

public sealed class ListBox : Control
{
    private int _scrollOffset = 0;
    private int _selectedIndex = -1;

    public List<string> Items { get; } = [];
    public int SelectedIndex 
    { 
        get => _selectedIndex; 
        set => _selectedIndex = Math.Clamp(value, -1, Items.Count - 1); 
    }
    public string? SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;
    public int ScrollOffset 
    { 
        get => _scrollOffset; 
        set => _scrollOffset = Math.Max(0, Math.Min(value, Math.Max(0, Items.Count - GetVisibleCount()))); 
    }
    public int ItemHeight { get; set; } = 18;
    public Color SelectionColor { get; set; } = Color.Red;
    public Color BackgroundColor { get; set; } = Color.Black;
    public Color TextColor { get; set; } = Color.White;
    public bool ShowSelectionOutline { get; set; } = true;
    public int Padding { get; set; } = 6;

    public override void Render(int x, int y)
    {
        int renderX = X + x;
        int renderY = Y + y;

        // Render background
        DesignRenderer.Render(Design.TextBlack, renderX, renderY, Width, Height, Alpha);

        int contentX = renderX + Padding;
        int contentY = renderY + Padding;
        int contentWidth = Width - (Padding * 2);
        int contentHeight = Height - (Padding * 2);

        if (contentWidth <= 0 || contentHeight <= 0) return;

        int visibleCount = GetVisibleCount();
        int maxScroll = Math.Max(0, Items.Count - visibleCount);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);

        // Render items
        for (int i = 0; i < visibleCount && (i + _scrollOffset) < Items.Count; i++)
        {
            int itemIndex = i + _scrollOffset;
            int itemY = contentY + i * ItemHeight;
            
            if (itemY + ItemHeight > renderY + Height) break; // Don't render beyond bounds

            string itemText = Items[itemIndex] ?? string.Empty;
            
            // Render selection background/outline
            if (itemIndex == _selectedIndex && ShowSelectionOutline)
            {
                GameClient.DrawOutlineRectangle(contentX - 2, itemY - 1, contentWidth, ItemHeight, SelectionColor, 1f);
            }

            // Render item text
            if (!string.IsNullOrEmpty(itemText))
            {
                TextRenderer.OnDraw(itemText, contentX, itemY, TextColor, BackgroundColor, Font);
            }
        }
    }

    public int GetVisibleCount()
    {
        int contentHeight = Height - (Padding * 2);
        return Math.Max(1, contentHeight / ItemHeight);
    }

    public int GetItemIndexAtPosition(int relativeY)
    {
        int contentY = Padding;
        if (relativeY < contentY) return -1;

        int itemIndex = (relativeY - contentY) / ItemHeight;
        int absoluteIndex = itemIndex + _scrollOffset;

        return absoluteIndex < Items.Count ? absoluteIndex : -1;
    }

    public void ScrollBy(int delta)
    {
        ScrollOffset += delta;
    }

    public void EnsureVisible(int index)
    {
        if (index < 0 || index >= Items.Count) return;

        int visibleCount = GetVisibleCount();
        
        if (index < _scrollOffset)
        {
            ScrollOffset = index;
        }
        else if (index >= _scrollOffset + visibleCount)
        {
            ScrollOffset = index - visibleCount + 1;
        }
    }

    public void Clear()
    {
        Items.Clear();
        _selectedIndex = -1;
        _scrollOffset = 0;
    }

    public void AddItem(string item)
    {
        Items.Add(item ?? string.Empty);
    }

    public void RemoveAt(int index)
    {
        if (index >= 0 && index < Items.Count)
        {
            Items.RemoveAt(index);
            
            if (_selectedIndex == index)
            {
                _selectedIndex = -1;
            }
            else if (_selectedIndex > index)
            {
                _selectedIndex--;
            }
            
            // Adjust scroll offset if needed
            int maxScroll = Math.Max(0, Items.Count - GetVisibleCount());
            _scrollOffset = Math.Min(_scrollOffset, maxScroll);
        }
    }
}