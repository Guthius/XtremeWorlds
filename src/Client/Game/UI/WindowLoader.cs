using System.IO;
using System.Xml;
using Core.Globals;

namespace Client.Game.UI;

public static class WindowLoader
{
    private const Font DefaultWindowFont = Font.Georgia;
    private const Font DefaultControlFont = Font.Arial;

    public static Window FromLayout(string layoutName)
    {
        // Resolve layout path relative to the packaged Content root
        var path = Path.Combine(DataPath.Skins, "Layouts", layoutName + ".xml");
        if (!File.Exists(path))
        {
            throw new UIException(
                $"Unable to load window layout '{layoutName}'. " +
                $"Layout file '{path}' does not exist.");
        }

        using var stream = File.OpenRead(path);

        using var xmlReader = XmlReader.Create(stream, new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true
        });

        xmlReader.MoveToContent();
        if (xmlReader.NodeType != XmlNodeType.Element ||
            xmlReader.Name != "Window")
        {
            throw new UIException("Window layout file is missing root 'Window' element.");
        }

        var windowIndex = ReadWindow(xmlReader);

        return WindowManager.Windows[windowIndex];
    }

    private static int ReadWindow(XmlReader xmlReader)
    {
        var name = xmlReader.GetAttribute("Name");
        var caption = xmlReader.GetAttribute("Caption");
        var fontName = xmlReader.GetAttribute("Font");
        var font = GetFontByName(fontName, DefaultWindowFont);
        var size = xmlReader.GetAttribute("Size");
        var sizeVec = GetVector(size);
        var position = xmlReader.GetAttribute("Position");
        var positionVec = GetVector(position);
        var icon = xmlReader.GetAttribute("Icon");
        var iconOffset = xmlReader.GetAttribute("IconOffset");
        var iconOffsetVec = GetVector(iconOffset);
        var designName = xmlReader.GetAttribute("Design");
        var design = GetDesignByName(designName, Design.None);
        var designHoverName = xmlReader.GetAttribute("DesignHover");
        var designHover = GetDesignByName(designHoverName, design);
        var designMousedownName = xmlReader.GetAttribute("DesignMouseDown");
        var designMousedown = GetDesignByName(designMousedownName, design);
        var startPosition = xmlReader.GetAttribute("StartPosition");
        var visible = GetBoolean(xmlReader.GetAttribute("Visible"), false);
        var windowIndex = WindowManager.CreateWindow(
            name: name ?? string.Empty,
            caption: caption ?? string.Empty,
            font: font,
            zOrder: WindowManager.ZOrderWin,
            left: positionVec.X,
            top: positionVec.Y,
            width: sizeVec.X,
            height: sizeVec.Y,
            icon: GetIcon(icon),
            visible: visible,
            xOffset: iconOffsetVec.X,
            yOffset: iconOffsetVec.Y,
            designNorm: design,
            designHover: designHover,
            designMousedown: designMousedown);

        if (!string.IsNullOrEmpty(startPosition))
        {
            if (startPosition.Equals("Center", StringComparison.OrdinalIgnoreCase) ||
                startPosition.Equals("CenterScreen", StringComparison.OrdinalIgnoreCase))
            {
                WindowManager.CentralizeWindow(windowIndex);
            }
        }

        WindowManager.ZOrderCon = 0;

        while (xmlReader.Read())
        {
            if (xmlReader.NodeType == XmlNodeType.Element)
            {
                ReadControl(xmlReader, windowIndex);
            }
            else if (xmlReader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }
        }

        return windowIndex;
    }

    private static void ReadControl(XmlReader xmlReader, int windowIndex)
    {
        switch (xmlReader.Name)
        {
            case "ScrollBar":
                ReadScrollBar(xmlReader, windowIndex);
                break;

            case "Button":
                ReadButton(xmlReader, windowIndex);
                break;

            case "CheckBox":
                ReadCheckBox(xmlReader, windowIndex);
                break;

            case "Label":
                ReadLabel(xmlReader, windowIndex);
                break;

            case "PictureBox":
                ReadPictureBox(xmlReader, windowIndex);
                break;

            case "TextBox":
                ReadTextBox(xmlReader, windowIndex);
                break;

            case "ComboBox":
                ReadComboBox(xmlReader, windowIndex);
                break;

            case "ListBox":
                ReadListBox(xmlReader, windowIndex);
                break;

            case "GroupBox":
                ReadGroupBox(xmlReader, windowIndex);
                return;
        }

        if (!xmlReader.IsEmptyElement)
        {
            xmlReader.Skip();
        }

    }

    private static void ReadScrollBar(XmlReader xmlReader, int windowIndex)
    {
        var name = xmlReader.GetAttribute("Name");
        var position = xmlReader.GetAttribute("Position");
        var positionVec = GetVector(position);
        var size = xmlReader.GetAttribute("Size");
        var sizeVec = GetVector(size);
        var min = GetInt32(xmlReader.GetAttribute("Min"), 0);
        var max = GetInt32(xmlReader.GetAttribute("Max"), 99);
        var value = GetInt32(xmlReader.GetAttribute("Value"), 0);
        var orientation = xmlReader.GetAttribute("Orientation");
        var vertical = true;
        if (!string.IsNullOrEmpty(orientation))
        {
            vertical = !orientation.Equals("Horizontal", StringComparison.OrdinalIgnoreCase);
        }
        var thumbSize = GetInt32(xmlReader.GetAttribute("ThumbSize"), 16);

        WindowManager.CreateScrollBar(
            windowIndex: windowIndex,
            name: name ?? string.Empty,
            left: positionVec.X,
            top: positionVec.Y,
            width: sizeVec.X,
            height: sizeVec.Y,
            min: min,
            max: max,
            value: value,
            vertical: vertical,
            thumbSize: thumbSize
        );
    }
    private static void ReadGroupBox(XmlReader xmlReader, int windowIndex)
    {
        var name = xmlReader.GetAttribute("Name") ?? string.Empty;
        var caption = xmlReader.GetAttribute("Caption") ?? string.Empty;
        var posAttr = xmlReader.GetAttribute("Position");
        var sizeAttr = xmlReader.GetAttribute("Size");
        var position = GetVector(posAttr);
        var size = GetVector(sizeAttr);

        // Create the GroupBox first
        WindowManager.CreateGroupBox(windowIndex, name, position.X, position.Y, size.X, size.Y, caption);

        bool autoPos = string.IsNullOrEmpty(posAttr);
        bool autoSize = string.IsNullOrEmpty(sizeAttr) || size.X <= 0 || size.Y <= 0;

        var window = WindowManager.Windows[windowIndex];
        int groupIndex = window.Controls.Count - 1;
        int childStart = window.Controls.Count;

        // Read child controls inside the group
        if (!xmlReader.IsEmptyElement)
        {
            var depth = xmlReader.Depth;
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    ReadControl(xmlReader, windowIndex);
                }
                else if (xmlReader.NodeType == XmlNodeType.EndElement &&
                         xmlReader.Depth == depth &&
                         xmlReader.Name == "GroupBox")
                {
                    break;
                }
            }
        }

        // Autosize to fit enclosed controls if requested (uses child local coordinates before offsetting)
        if (autoPos || autoSize)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            for (int i = childStart; i < window.Controls.Count; i++)
            {
                var c = window.Controls[i];
                minX = Math.Min(minX, c.X);
                minY = Math.Min(minY, c.Y);
                maxX = Math.Max(maxX, c.X + c.Width);
                maxY = Math.Max(maxY, c.Y + c.Height);
            }

            if (minX != int.MaxValue)
            {
                const int padding = 8;
                var group = (Client.Game.UI.Controls.GroupBox)window.Controls[groupIndex];

                if (autoPos)
                {
                    // stack vertically below previous group boxes
                    int lastBottom = padding;
                    for (int i = 0; i < groupIndex; i++)
                    {
                        if (window.Controls[i] is Client.Game.UI.Controls.GroupBox gbPrev && gbPrev.Visible)
                        {
                            lastBottom = Math.Max(lastBottom, gbPrev.Y + gbPrev.Height + padding);
                        }
                    }
                    group.X = padding;
                    group.Y = lastBottom;
                }

                if (autoSize)
                {
                    group.Width = Math.Max(1, (maxX - minX) + padding * 2);
                    group.Height = Math.Max(1, (maxY - minY) + padding * 2);
                }
            }
        }

        // Assign child range to group
        if (window.Controls[groupIndex] is Client.Game.UI.Controls.GroupBox gb)
        {
            gb.FirstChildIndex = childStart;
            gb.LastChildIndex = window.Controls.Count - 1;

            // Constrain group box inside window margins
            const int margin = 6;
            if (gb.X < margin) gb.X = margin;
            if (gb.X + gb.Width > window.Width - margin)
                gb.Width = Math.Max(1, window.Width - margin - gb.X);
            if (gb.Y + gb.Height > window.Height - margin)
                gb.Height = Math.Max(1, window.Height - margin - gb.Y);

            // Offset child controls so their X/Y become relative to the group's origin.
            for (int i = gb.FirstChildIndex; i <= gb.LastChildIndex; i++)
            {
                var child = window.Controls[i];
                if (ReferenceEquals(child, gb)) continue; // skip the group itself
                child.X += gb.X;
                child.Y += gb.Y;
            }
        }
    }

    private static void ReadListBox(XmlReader xmlReader, int windowIndex)
    {
        var name = xmlReader.GetAttribute("Name");
        var position = xmlReader.GetAttribute("Position");
        var positionVec = GetVector(position);
        var size = xmlReader.GetAttribute("Size");
        var sizeVec = GetVector(size);

        WindowManager.CreateListBox(
            windowIndex,
            name ?? string.Empty,
            positionVec.X,
            positionVec.Y,
            sizeVec.X,
            sizeVec.Y);
    }

    private static void ReadComboBox(XmlReader xmlReader, int windowIndex)
    {
        var name = xmlReader.GetAttribute("Name");
        var position = xmlReader.GetAttribute("Position");
        var positionVec = GetVector(position);
        var size = xmlReader.GetAttribute("Size");
        var sizeVec = GetVector(size);
        var designName = xmlReader.GetAttribute("Design");
        var design = GetDesignByName(designName, Design.ComboBox);

        WindowManager.CreateComboBox(
            windowIndex: windowIndex,
            name: name ?? string.Empty,
            left: positionVec.X,
            top: positionVec.Y,
            width: sizeVec.X,
            height: sizeVec.Y,
            design: design);
    }

    private static void ReadLabel(XmlReader xmlReader, int windowIndex)
    {
        var name = xmlReader.GetAttribute("Name");
        var text = xmlReader.GetAttribute("Text");
        var position = xmlReader.GetAttribute("Position");
        var positionVec = GetVector(position);
        var size = xmlReader.GetAttribute("Size");
        var sizeVec = GetVector(size);
        var fontName = xmlReader.GetAttribute("Font");
        var font = GetFontByName(fontName, DefaultControlFont);
        var alignmentName = xmlReader.GetAttribute("Align");
        var alignment = GetAlignmentByName(alignmentName, Alignment.Left);

        WindowManager.CreateLabel(
            windowIndex: windowIndex,
            name: name ?? string.Empty,
            text: text ?? string.Empty,
            left: positionVec.X,
            top: positionVec.Y,
            width: sizeVec.X,
            height: sizeVec.Y,
            font: font,
            align: alignment);
    }

    private static void ReadPictureBox(XmlReader xmlReader, int windowIndex)
    {
        var name = xmlReader.GetAttribute("Name");
        var position = xmlReader.GetAttribute("Position");
        var positionVec = GetVector(position);
        var size = xmlReader.GetAttribute("Size");
        var sizeVec = GetVector(size);
        var image = GetInt32(xmlReader.GetAttribute("Image"));
        var imageHover = GetInt32(xmlReader.GetAttribute("ImageHover"), image);
        var imageMousedown = GetInt32(xmlReader.GetAttribute("ImageMouseDown"), image);
        var designName = xmlReader.GetAttribute("Design");
        var design = GetDesignByName(designName, Design.None);
        var designHoverName = xmlReader.GetAttribute("DesignHover");
        var designHover = GetDesignByName(designHoverName, design);
        var designMousedownName = xmlReader.GetAttribute("DesignMouseDown");
        var designMousedown = GetDesignByName(designMousedownName, design);

        WindowManager.CreatePictureBox(
            windowIndex,
            name ?? string.Empty,
            positionVec.X,
            positionVec.Y,
            sizeVec.X,
            sizeVec.Y,
            imageNorm: image,
            imageHover: imageHover,
            imageMousedown: imageMousedown,
            designNorm: design,
            designHover: designHover,
            designMousedown: designMousedown);
    }

    private static void ReadButton(XmlReader xmlReader, int windowIndex)
    {
        var name = xmlReader.GetAttribute("Name");
        var text = xmlReader.GetAttribute("Text");
        var position = xmlReader.GetAttribute("Position");
        var positionVec = GetVector(position);
        var size = xmlReader.GetAttribute("Size");
        var sizeVec = GetVector(size);
        var fontName = xmlReader.GetAttribute("Font");
        var font = GetFontByName(fontName, DefaultControlFont);
        var image = GetInt32(xmlReader.GetAttribute("Image"));
        var iconNormal = GetInt32(xmlReader.GetAttribute("Icon"));
        var imageHover = GetInt32(xmlReader.GetAttribute("ImageHover"), image);
        var imageMousedown = GetInt32(xmlReader.GetAttribute("ImageMouseDown"), image);
        var designName = xmlReader.GetAttribute("Design");
        var design = GetDesignByName(designName, Design.None);
        var designHoverName = xmlReader.GetAttribute("DesignHover");
        var designHover = GetDesignByName(designHoverName, design);
        var designMousedownName = xmlReader.GetAttribute("DesignMouseDown");
        var designMousedown = GetDesignByName(designMousedownName, design);

        var x = positionVec.X;
        if (x < 0)
        {
            x = WindowManager.Windows[windowIndex].Width + x;
        }

        var y = positionVec.Y;
        if (y < 0)
        {
            y = WindowManager.Windows[windowIndex].Height + y;
        }

        WindowManager.CreateButton(
            windowIndex: windowIndex,
            name: name ?? string.Empty,
            text: text ?? string.Empty,
            left: x, top: y,
            width: sizeVec.X,
            height: sizeVec.Y,
            font: font,
            imageNorm: image,
            imageHover: imageHover,
            imageMousedown: imageMousedown,
            designNorm: design,
            designHover: designHover,
            designMousedown: designMousedown,
            icon: iconNormal
        );
    }

    private static void ReadTextBox(XmlReader xmlReader, int windowIndex)
    {
        var name = xmlReader.GetAttribute("Name");
        var text = xmlReader.GetAttribute("Text");
        var position = xmlReader.GetAttribute("Position");
        var positionVec = GetVector(position);
        var size = xmlReader.GetAttribute("Size");
        var sizeVec = GetVector(size);
        var fontName = xmlReader.GetAttribute("Font");
        var font = GetFontByName(fontName, DefaultControlFont);
        var designName = xmlReader.GetAttribute("Design");
        var design = GetDesignByName(designName, Design.TextWhite);
        var designHoverName = xmlReader.GetAttribute("DesignHover");
        var designHover = GetDesignByName(designHoverName, design);
        var designMousedownName = xmlReader.GetAttribute("DesignMouseDown");
        var designMousedown = GetDesignByName(designMousedownName, design);
        var censor = GetBoolean(xmlReader.GetAttribute("Censor"));

        WindowManager.CreateTextbox(
            windowIndex: windowIndex,
            name: name ?? string.Empty,
            left: positionVec.X,
            top: positionVec.Y,
            width: sizeVec.X,
            height: sizeVec.Y,
            text: text ?? string.Empty,
            font: font,
            xOffset: 5,
            yOffset: 3,
            designNorm: design,
            designHover: designHover,
            designMousedown: designMousedown, censor: censor);
    }

    private static void ReadCheckBox(XmlReader xmlReader, int windowIndex)
    {
        var name = xmlReader.GetAttribute("Name");
        var text = xmlReader.GetAttribute("Text");
        var position = xmlReader.GetAttribute("Position");
        var positionVec = GetVector(position);
        var size = xmlReader.GetAttribute("Size");
        var sizeVec = GetVector(size);
        var fontName = xmlReader.GetAttribute("Font");
        var font = GetFontByName(fontName, DefaultControlFont);
        var designName = xmlReader.GetAttribute("Design");
        var design = GetDesignByName(designName, Design.None);

        WindowManager.CreateCheckBox(
            windowIndex: windowIndex,
            name: name ?? string.Empty,
            text: text ?? string.Empty,
            left: positionVec.X,
            top: positionVec.Y,
            width: sizeVec.X,
            font: font,
            theDesign: design);
    }

    private static Font GetFontByName(string? fontName, Font defaultValue)
    {
        if (string.IsNullOrEmpty(fontName))
        {
            return defaultValue;
        }

        if (Enum.TryParse<Font>(fontName, true, out var result))
        {
            return result;
        }

        return defaultValue;
    }

    private static Alignment GetAlignmentByName(string? alignmentName, Alignment defaultValue)
    {
        if (string.IsNullOrEmpty(alignmentName))
        {
            return defaultValue;
        }

        if (Enum.TryParse<Alignment>(alignmentName, true, out var result))
        {
            return result;
        }

        return defaultValue;
    }

    private static (int X, int Y) GetVector(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return (0, 0);
        }

        var comma = value.IndexOf(',');
        if (comma == -1)
        {
            return (0, 0);
        }

        if (!int.TryParse(value.AsSpan(0, comma), out var x)) x = 0;
        if (!int.TryParse(value.AsSpan(comma + 1), out var y)) y = 0;

        return (x, y);
    }

    private static int GetIcon(string? icon)
    {
        if (string.IsNullOrEmpty(icon))
        {
            return 0;
        }

        if (int.TryParse(icon, out var result))
        {
            return result;
        }

        return 0;
    }

    private static Design GetDesignByName(string? designName, Design defaultValue)
    {
        if (string.IsNullOrEmpty(designName))
        {
            return defaultValue;
        }

        if (Enum.TryParse<Design>(designName, true, out var result))
        {
            return result;
        }

        return defaultValue;
    }

    private static bool GetBoolean(string? value, bool defaultValue = false)
    {
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        return defaultValue;
    }

    private static int GetInt32(string? value, int defaultValue = 0)
    {
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        if (int.TryParse(value, out var result))
        {
            return result;
        }

        return defaultValue;
    }
}