using Core.Configurations;
using Core.Globals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Text;
using static Core.Globals.Commands;

namespace Client.Game.UI;

public static class TextRenderer
{
    public static readonly Dictionary<Font, SpriteFont> Fonts = new();
    public static readonly Dictionary<Core.Globals.BitmapFont, BitmapFont> BitmapFonts = new();

    // Style toggles (mage style disabled for cleaner look)
    public static bool UseMageStyle = false;          // Disabled
    public static float MageGlowStrength = 0.08f;     // Unused now
    public static float MageScaleBoost = 1.02f;       // Unused now

    public class BitmapFont
    {
        public Texture2D Atlas = null!;
        public Dictionary<char, Rectangle> Glyphs = new();
        public int LineHeight;
        public int CharHeight;
        public int Spacing;
        public int BaseOffset;
        public int XOffset;
        public int YOffset;
        public char? ColourChar;
        public Dictionary<char, int> Adv = new();
    }

    public const float BaseScale = 12f / 16f;
    public const float AccentScale = 12f / 16f;     
    private static readonly float effectiveScale = AccentScale;

    public static Color GetColorForAmount(int amount) =>
        amount switch
        {
            < 1_000_000 => Color.White,
            < 10_000_000 => Color.Yellow,
            _ => Color.LightGreen
        };

    public static string CensorText(string input) => new('*', input.Length);

    public static string SanitizeText(string text, SpriteFont font)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
            if (font.Characters.Contains(ch)) sb.Append(ch);
        return sb.ToString();
    }

    public static void RegisterBitmapFont(Core.Globals.BitmapFont font, Texture2D atlas, Dictionary<char, Rectangle> glyphs, int lineHeight, int spacing = 0, int baseOffset = 32, Dictionary<char, int>? advances = null)
    {
        var bf = new BitmapFont
        {
            Atlas = atlas,
            Glyphs = glyphs,
            LineHeight = lineHeight,
            CharHeight = lineHeight,
            Spacing = spacing,
            BaseOffset = baseOffset
        };
        if (advances != null) bf.Adv = advances;
        BitmapFonts[font] = bf;
    }

    private static bool TryGetBitmapFont(Core.Globals.BitmapFont font, out BitmapFont bf) => BitmapFonts.TryGetValue(font, out bf);

    public static bool LoadLegacyBitmapFont(Core.Globals.BitmapFont font, string datPath, string pngPath, GraphicsDevice gd)
    {
        if (!File.Exists(datPath) || !File.Exists(pngPath)) return false;
        using var fsImg = File.OpenRead(pngPath);
        var atlas = Texture2D.FromStream(gd, fsImg);
        using var fs = File.OpenRead(datPath);
        using var br = new BinaryReader(fs);

        int bmpW = br.ReadInt32();
        int bmpH = br.ReadInt32();
        int cellW = br.ReadInt32();
        int cellH = br.ReadInt32();
        byte baseChar = br.ReadByte();
        var widths = br.ReadBytes(256);

        if (cellW <= 0 || cellH <= 0) return false;

        int rowPitch = bmpW / cellW;
        if (rowPitch <= 0) rowPitch = 1;

        var glyphRects = new Dictionary<char, Rectangle>();
        var advances = new Dictionary<char, int>();

        for (int code = 0; code < 256; code++)
        {
            int row = (code - baseChar) / rowPitch;
            int col = (code - baseChar) - (row * rowPitch);
            if (row < 0) row = 0;
            if (col < 0) col = 0;
            int x = col * cellW;
            int y = row * cellH;
            if (x + cellW > bmpW || y + cellH > bmpH) continue;

            var ch = (char)code;
            glyphRects[ch] = new Rectangle(x, y, cellW, cellH);

            int raw = widths[code];
            advances[ch] = raw > 0 ? raw : cellW;
        }

        RegisterBitmapFont(font, atlas, glyphRects, cellH, spacing: 0, baseOffset: baseChar, advances: advances);

        if (BitmapFonts.TryGetValue(font, out var bf))
        {
            bf.CharHeight = bf.LineHeight;
            bf.Spacing = 0;
            bf.XOffset = 0;
            bf.YOffset = 0;
            bf.ColourChar = '�';
        }
        return true;
    }

    public static bool HasBitmapFont(Core.Globals.BitmapFont font) => BitmapFonts.ContainsKey(font);

    public static void TryLoadLegacyFont(GraphicsDevice gd, string fontName)
    {
        try
        {
            static string? ResolveFontFile(string baseDir, string nameNoExt, string ext)
            {
                var a = Path.Combine(baseDir, nameNoExt + ext);
                if (File.Exists(a)) return a;

                var lower = Path.Combine(baseDir, nameNoExt.ToLowerInvariant() + ext);
                if (File.Exists(lower)) return lower;

                var upper = Path.Combine(baseDir, nameNoExt.ToUpperInvariant() + ext);
                if (File.Exists(upper)) return upper;

                return null;
            }

            var datPath = ResolveFontFile(DataPath.Fonts, fontName, ".dat");
            var pngPath = ResolveFontFile(DataPath.Fonts, fontName, ".png");
            if (datPath == null || pngPath == null) return;

            if (!Enum.TryParse<Core.Globals.BitmapFont>(fontName, out Core.Globals.BitmapFont fontEnum))
                fontEnum = Core.Globals.BitmapFont.Default;

            _ = LoadLegacyBitmapFont(fontEnum, datPath, pngPath, gd);
        }
        catch { }
    }

    public static int GetTextWidth(string text, Core.Globals.BitmapFont font, float textSize = 1.0f)
    {
        if (!TryGetBitmapFont(font, out var bf) || string.IsNullOrEmpty(text)) return 0;

        int maxLine = 0;
        int current = 0;
        int colorSkip = 0;
        int id = 0;
        int len = text.Length;

        foreach (var ch in text)
        {
            if (colorSkip > 0) { colorSkip--; continue; }
            if (ch == '\r') continue;
            if (ch == '\n')
            {
                maxLine = Math.Max(maxLine, current);
                current = 0;
                continue;
            }
            if (bf.ColourChar.HasValue && ch == bf.ColourChar.Value)
            {
                colorSkip = 2;
                continue;
            }
            if (!bf.Glyphs.TryGetValue(ch, out var rect))
                rect = bf.Glyphs.TryGetValue(' ', out var sp) ? sp : new Rectangle(0, 0, bf.LineHeight / 2, bf.LineHeight);

            int adv = bf.Adv.TryGetValue(ch, out var a) ? a : rect.Width;
            if (adv <= 0) adv = rect.Width;

            current += adv;
            if (id < len - 1) current += bf.Spacing;
            id++;
        }

        int w = Math.Max(maxLine, current);
        return (int)(w * textSize);
    }

    public static int GetTextWidth(string text, Font font = Font.Georgia, float textSize = 1.0f)
    {
        if (SettingsManager.Instance.BitmapFont)
        {
            if (Enum.TryParse<Core.Globals.BitmapFont>(font.ToString(), out var bfEnum))
            {
                if (HasBitmapFont(bfEnum))
                    return GetTextWidth(text ?? string.Empty, bfEnum, textSize);
            }
        }

        if (!Fonts.TryGetValue(font, out var spriteFont))
        {
            if (Fonts.Count > 0) spriteFont = Fonts.Values.First();
            else return (int)Math.Round((text?.Length ?? 0) * 8f * textSize);
            
        }

        var sanitized = SanitizeText(text ?? string.Empty, spriteFont);
        var dims = spriteFont.MeasureString(sanitized);
        return (int)Math.Round(dims.X * (effectiveScale / BaseScale) * textSize);
    }

    public static int GetTextHeight(string text, Core.Globals.BitmapFont font, float textSize = 1.0f)
    {
        if (!TryGetBitmapFont(font, out var bf)) return 0;
        int lines = 1;
        if (!string.IsNullOrEmpty(text))
            foreach (var ch in text) if (ch == '\n') lines++;
        int h = (int)Math.Round(bf.CharHeight * textSize);
        int gap = (int)Math.Round(3 * textSize);
        return lines * h + (lines - 1) * gap;
    }

    public static int GetTextHeight(string text, Font font = Font.Georgia, float textSize = 1.0f)
    {
        if (SettingsManager.Instance.BitmapFont &&
            Enum.TryParse<Core.Globals.BitmapFont>(font.ToString(), out var bfEnum))
        {
            if (HasBitmapFont(bfEnum))
                return GetTextHeight(text ?? string.Empty, bfEnum, textSize);
        }

        if (!Fonts.TryGetValue(font, out var spriteFont))
        {
            if (!Fonts.TryGetValue(Font.Georgia, out spriteFont))
            {
                if (Fonts.Count > 0) spriteFont = Fonts.Values.First();
                else
                {
                    int lines = 1;
                    if (!string.IsNullOrEmpty(text))
                        foreach (var ch in text) if (ch == '\n') lines++;
                    return (int)Math.Round(16f * lines * (effectiveScale / BaseScale) * textSize);
                }
            }
            
        }

        var dimensions = spriteFont.MeasureString(text ?? string.Empty);
        return (int)Math.Round(dimensions.Y * (effectiveScale / BaseScale) * textSize);
    }

    // Bitmap render with subtle dual shadow (no glow)
    public static void OnDraw(string text, int x, int y, Color frontColor, Color backColor, Core.Globals.BitmapFont font, float textSize = 1.0f)
    {
        if (string.IsNullOrEmpty(text) || GameClient.SpriteBatch == null) return;
        if (!TryGetBitmapFont(font, out var bf)) return;

        int originX = x - bf.XOffset;
        int originY = y - bf.YOffset;
        int lineX = originX;
        int lineY = originY;
        int colorSkip = 0;
        int id = 0;
        int len = text.Length;

        int lineAdvance = (int)Math.Round(bf.CharHeight * textSize);
        int gapAdvance = (int)Math.Round(3 * textSize);
        int shadowOffset = Math.Max(1, (int)Math.Round(textSize));

        foreach (var ch in text)
        {
            if (colorSkip > 0) { colorSkip--; continue; }
            if (ch == '\r') continue;

            if (ch == '\n')
            {
                lineY += lineAdvance + gapAdvance;
                lineX = originX;
                id = 0;
                continue;
            }

            if (bf.ColourChar.HasValue && ch == bf.ColourChar.Value)
            {
                colorSkip = 2;
                continue;
            }

            if (!bf.Glyphs.TryGetValue(ch, out var rect))
                rect = bf.Glyphs.TryGetValue(' ', out var sp) ? sp : new Rectangle(0, 0, bf.LineHeight / 2, bf.LineHeight);

            int adv = bf.Adv.TryGetValue(ch, out var a) ? a : rect.Width;
            if (adv <= 0) adv = rect.Width;

            // Draw at the glyph cell size, but advance by the font's per-character advance.
            // Using advance as draw width squishes glyphs and breaks spacing.
            int drawW = (int)Math.Round(rect.Width * textSize);
            int drawH = (int)Math.Round(rect.Height * textSize);
            int stepX = (int)Math.Round(adv * textSize);

            // Shadow 1 (primary)
            GameClient.SpriteBatch.Draw(bf.Atlas, new Rectangle(lineX + shadowOffset, lineY + shadowOffset, drawW, drawH), rect, backColor * 0.9f);
            // Shadow 2 (soft offset)
            GameClient.SpriteBatch.Draw(bf.Atlas, new Rectangle(lineX, lineY + shadowOffset, drawW, drawH), rect, backColor * 0.4f);
            // Glyph
            GameClient.SpriteBatch.Draw(bf.Atlas, new Rectangle(lineX, lineY, drawW, drawH), rect, frontColor);

            lineX += stepX + (id < len - 1 ? (int)Math.Round(bf.Spacing * textSize) : 0);
            id++;
        }
    }

    public static void OnDraw(string text, int x, int y, Color frontColor, Color backColor, Font font = Font.Georgia, float textSize = 1.0f)
    {
        if (SettingsManager.Instance.BitmapFont)
        {
            if (Enum.TryParse<Core.Globals.BitmapFont>(font.ToString(), out var bfEnum) && HasBitmapFont(bfEnum))
            {
                OnDraw(text, x, y, frontColor, backColor, bfEnum, textSize);
                return;
            }
        }

        if (!Fonts.TryGetValue(font, out var spriteFont))
        {
            if (!Fonts.TryGetValue(Font.Georgia, out spriteFont)) return;       
        }

        var sanitizedText = SanitizeText(text ?? string.Empty, spriteFont);
        float scale = effectiveScale * textSize;
        Vector2 pos = new(x, y);

        // Shadows
        GameClient.SpriteBatch.DrawString(spriteFont, sanitizedText, pos + new Vector2(1, 1), backColor * 0.9f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        GameClient.SpriteBatch.DrawString(spriteFont, sanitizedText, pos + new Vector2(0, 1), backColor * 0.4f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        // Main text
        GameClient.SpriteBatch.DrawString(spriteFont, sanitizedText, pos, frontColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    // Remaining utility & draw methods unchanged below ...

    public static void AddText(string text, int color, long alpha = 255L, byte channel = 0)
    {
        string[] wrappedLines = System.Array.Empty<string>();
        WordWrap(text, Font.Georgia, WindowManager.Windows[WindowManager.GetWindowIndex("winChat")].Width, ref wrappedLines);
        GameState.ChatHighIndex += wrappedLines.Length;
        if (GameState.ChatHighIndex > Variables.ChatLines) GameState.ChatHighIndex = Variables.ChatLines;

        for (var i = (int)GameState.ChatHighIndex - wrappedLines.Length; i > 0; i--)
            Data.Chat[i] = Data.Chat[i - 1];

        for (int i = wrappedLines.Length - 1, chatIndex = 0; i >= 0; i--, chatIndex++)
        {
            Data.Chat[chatIndex].Text = wrappedLines[i];
            Data.Chat[chatIndex].Color = color;
            Data.Chat[chatIndex].Visible = true;
            Data.Chat[chatIndex].Timer = General.GetTickCount();
            Data.Chat[chatIndex].Channel = channel;
        }
    }

    public static void WordWrap(string text, Font font, long maxLineLen, ref string[] theArray)
    {
        var lineCount = 0L;
        if (Strings.Len(text) < 2)
        {
            theArray = new string[2];
            theArray[1] = text;
            return;
        }

        var b = 1L;
        var lastSpace = 1L;
        var size = 0L;
        long tmpNum = Strings.Len(text);

        for (var i = 1L; i <= tmpNum; i++)
        {
            if (Strings.Mid(text, (int)i, 1) == " ") lastSpace = i;
            size += 10L;

            if (size > maxLineLen)
            {
                if (i - lastSpace > 10L)
                {
                    lineCount++;
                    Array.Resize(ref theArray, (int)lineCount);
                    theArray[(int)lineCount - 1] = Strings.Mid(text, (int)b, (int)(i - 1L - b));
                    b = i - 1L;
                    size = 0L;
                }
                else
                {
                    lineCount++;
                    Array.Resize(ref theArray, (int)lineCount);
                    if (b < 0L) b = 0L;
                    if (b > text.Length) b = text.Length;
                    var substringLength = (int)(lastSpace - b);
                    if (substringLength < 0) substringLength = 0;
                    theArray[(int)lineCount - 1] = Strings.Mid(text, (int)b, substringLength);
                    b = lastSpace + 1L;
                    size = GetTextWidth(Strings.Mid(text, (int)lastSpace, (int)(i - lastSpace)), font);
                }
            }

            if (i == Strings.Len(text) && b != i)
            {
                lineCount++;
                Array.Resize(ref theArray, (int)lineCount);
                theArray[(int)lineCount - 1] = Strings.Mid(text, (int)b, (int)i);
            }
        }
    }

    public static void DrawMapAttributes()
    {
        int tA;
        var loopTo = (int)GameState.TileView.Right;
        for (var x = (int)GameState.TileView.Left; x < loopTo; x++)
        {
            var loopTo1 = (int)GameState.TileView.Bottom;
            for (var y = (int)GameState.TileView.Top; y < loopTo1; y++)
            {
                if (!GameLogic.IsValidMapPoint(x, y)) continue;
                ref var instance = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y];
                var tX = (int)Math.Round(GameLogic.ConvertMapX(x * Constants.TileSize) - 4 + Constants.TileSize * 0.5d);
                var tY = (int)Math.Round(GameLogic.ConvertMapY(y * Constants.TileSize) - 7 + Constants.TileSize * 0.5d);
                tA = GameState.EditorAttribute == 1 ? (int)instance.Type : (int)instance.Type2;
                switch (tA)
                {
                    case (int)TileType.Blocked: OnDraw("B", tX, tY, Color.Red, Color.Black); break;
                    case (int)TileType.Warp: OnDraw("W", tX, tY, Color.Blue, Color.Black); break;
                    case (int)TileType.Item: OnDraw("I", tX, tY, Color.White, Color.Black); break;
                    case (int)TileType.NpcAvoid: OnDraw("N", tX, tY, Color.White, Color.Black); break;
                    case (int)TileType.Resource: OnDraw("R", tX, tY, Color.Green, Color.Black); break;
                    case (int)TileType.NpcSpawn: OnDraw("S", tX, tY, Color.Yellow, Color.Black); break;
                    case (int)TileType.Shop: OnDraw("S", tX, tY, Color.Blue, Color.Black); break;
                    case (int)TileType.Bank: OnDraw("B", tX, tY, Color.Blue, Color.Black); break;
                    case (int)TileType.Heal: OnDraw("H", tX, tY, Color.Green, Color.Black); break;
                    case (int)TileType.Trap: OnDraw("T", tX, tY, Color.Red, Color.Black); break;
                    case (int)TileType.Animation: OnDraw("A", tX, tY, Color.Red, Color.Black); break;
                    case (int)TileType.NoCrossing: OnDraw("X", tX, tY, Color.Red, Color.Black); break;
                }
            }
        }
    }

    public static void DrawActionMessage(int index)
    {
        var x = 0;
        var y = 0;
        var time = 0;

        switch (Data.ActionMessage[index].Type)
        {
            case (int)ActionMessageType.Static:
                time = 1500;
                if (Data.ActionMessage[index].Y > 0)
                {
                    x = Data.ActionMessage[index].X + Conversion.Int(Constants.TileSize / 2) - Strings.Len(Data.ActionMessage[index].Message) / 2 * 8;
                    y = Data.ActionMessage[index].Y - Conversion.Int(Constants.TileSize / 2) - 2;
                }
                else
                {
                    x = Data.ActionMessage[index].X + Conversion.Int(Constants.TileSize / 2) - Strings.Len(Data.ActionMessage[index].Message) / 2 * 8;
                    y = Data.ActionMessage[index].Y - Conversion.Int(Constants.TileSize / 2) + 18;
                }
                break;

            case (int)ActionMessageType.Scroll:
                time = 1500;
                if (Data.ActionMessage[index].Y > 0)
                {
                    x = Data.ActionMessage[index].X + Conversion.Int(Constants.TileSize / 2) - Strings.Len(Data.ActionMessage[index].Message) / 2 * 8;
                    y = (int)Math.Round(Data.ActionMessage[index].Y - Conversion.Int(Constants.TileSize / 2) - 2 - Data.ActionMessage[index].Scroll * 0.6d);
                    Data.ActionMessage[index].Scroll++;
                }
                else
                {
                    x = Data.ActionMessage[index].X + Conversion.Int(Constants.TileSize / 2) - Strings.Len(Data.ActionMessage[index].Message) / 2 * 8;
                    y = (int)Math.Round(Data.ActionMessage[index].Y - Conversion.Int(Constants.TileSize / 2) + 18 + Data.ActionMessage[index].Scroll * 0.6d);
                    Data.ActionMessage[index].Scroll++;
                }
                break;

            case (int)ActionMessageType.Screen:
                time = 3000;
                for (int i = byte.MaxValue; i >= 0; i--)
                {
                    if (Data.ActionMessage[i].Type == (int)ActionMessageType.Screen && i != index)
                    {
                        GameLogic.ClearActionMessage((byte)index);
                        index = i;
                    }
                }
                x = GameState.ResolutionWidth / 2 - Strings.Len(Data.ActionMessage[index].Message) / 2 * 8;
                y = 425;
                break;
        }

        x = GameLogic.ConvertMapX(x);
        y = GameLogic.ConvertMapY(y);

        if (General.GetTickCount() < Data.ActionMessage[index].Created + time)
        {
            OnDraw(Data.ActionMessage[index].Message, x, y, GameClient.QbColorToXnaColor(Data.ActionMessage[index].Color), Color.Black);
        }
        else
        {
            GameLogic.ClearActionMessage((byte)index);
        }
    }

    public static void DrawChat()
    {
        var yOffset = 0L;
        var topWidth = 0;

        var xO = 19L;
        xO += WindowManager.Windows[WindowManager.GetWindowIndex("winChat")].X;
        long yO = GameState.ResolutionHeight - 45;
        var width = (int)WindowManager.Windows[WindowManager.GetWindowIndex("winChat")].Width;

        var rLines = 1;
        var i = GameState.ChatScroll;

        while (rLines < 8)
        {
            if (i >= Variables.ChatLines) break;
            if (Strings.Len(Data.Chat[(int)i].Text) == 0) break;

            var isVisible = true;
            if (GameState.InSmallChat && !Data.Chat[(int)i].Visible) isVisible = false;
            if (SettingsManager.Instance.ChannelState[Data.Chat[i].Channel] == 0) isVisible = false;

            if (isVisible)
            {
                var color = Data.Chat[(int)i].Color;
                var color2 = GameClient.QbColorToXnaColor(color);

                if (GetTextWidth(Data.Chat[i].Text) > width)
                {
                    string[] wrappedLines = new string[0];
                    WordWrap(Data.Chat[(int)i].Text, Font.Georgia, width, ref wrappedLines);
                    yOffset -= 10 * wrappedLines.Length;
                    for (var j = 0; j < wrappedLines.Length; j++)
                    {
                        OnDraw(wrappedLines[j], (int)xO, (int)(yO + yOffset + 10 * j), color2, color2);
                    }
                    rLines += wrappedLines.Length;
                    for (var x = 0; x < wrappedLines.Length; x++)
                        if (GetTextWidth(wrappedLines[x]) > topWidth) topWidth = GetTextWidth(wrappedLines[x]);
                }
                else
                {
                    yOffset -= 12L;
                    OnDraw(Data.Chat[(int)i].Text, (int)xO, (int)(yO + yOffset), color2, color2);
                    rLines++;
                    if (GetTextWidth(Data.Chat[(int)i].Text) > topWidth)
                        topWidth = GetTextWidth(Data.Chat[(int)i].Text);
                }
            }

            i++;
        }

        GameLogic.SetChatHeight(rLines * 12);
        GameLogic.SetChatWidth(topWidth);
    }

    public static void DrawMapName()
    {
        OnDraw(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Name, (int)Math.Round(GameState.ResolutionWidth / 2d - GetTextWidth(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Name)), 10, GameState.DrawMapNameColor, Color.Black);
    }
}