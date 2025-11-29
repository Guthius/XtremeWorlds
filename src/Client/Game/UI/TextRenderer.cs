using Core.Configurations;
using Core.Globals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Text;
using static Core.Globals.Command;

namespace Client.Game.UI;

public static class TextRenderer
{
    public static readonly Dictionary<Font, SpriteFont> Fonts = new();
    public static readonly Dictionary<Core.Globals.BitmapFont, BitmapFont> BitmapFonts = new();

    public class BitmapFont
    {
        public Texture2D Atlas;
        public Dictionary<char, Rectangle> Glyphs = new();
        public int LineHeight; // Raw cell height
        public int CharHeight; // Effective glyph draw height (== LineHeight for crispness)
        public int Spacing; // Horizontal gap between glyphs
        public int BaseOffset; // Starting character code in legacy sheet
        public int XOffset; // Baseline tweak X
        public int YOffset; // Baseline tweak Y
        public char? ColourChar;
        public Dictionary<char, int> Adv = new();
    }

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
            // Guard: width <= 0 -> monospace fallback
            advances[ch] = raw > 0 ? raw : cellW;
        }

        RegisterBitmapFont(font, atlas, glyphRects, cellH, spacing: 0, baseOffset: baseChar, advances: advances);

        if (BitmapFonts.TryGetValue(font, out var bf))
        {
            // Keep full pixel height for crisp rendering, avoid subtracting arbitrary padding
            bf.CharHeight = bf.LineHeight;
            bf.Spacing = 0;
            bf.XOffset = 0;
            bf.YOffset = 0;
            bf.ColourChar = '�'; // keep legacy colour code marker if needed
        }
        return true;
    }

    public static bool HasBitmapFont(Core.Globals.BitmapFont font) => BitmapFonts.ContainsKey(font);

    public static void TryLoadLegacyFont(GraphicsDevice gd, string fontName)
    {
        try
        {
            var datPath = Path.Combine(DataPath.Fonts, fontName + ".dat");
            var pngPath = Path.Combine(DataPath.Fonts, fontName + ".png");

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
        int idx = 0;
        int len = text.Length;

        foreach (var ch in text)
        {
            if (colorSkip > 0)
            {
                colorSkip--;
                continue;
            }
            if (ch == '\r') continue;

            if (ch == '\n')
            {
                maxLine = Math.Max(maxLine, current);
                current = 0;
                continue;
            }

            if (bf.ColourChar.HasValue && ch == bf.ColourChar.Value)
            {
                colorSkip = 2; // skip next two colour argument chars
                continue;
            }

            if (!bf.Glyphs.TryGetValue(ch, out var rect))
                rect = bf.Glyphs.TryGetValue(' ', out var sp) ? sp : new Rectangle(0, 0, bf.LineHeight / 2, bf.LineHeight);

            int adv = bf.Adv.TryGetValue(ch, out var a) ? a : rect.Width;
            if (adv <= 0) adv = rect.Width;

            current += adv;
            // add spacing between glyphs (not after last)
            if (idx < len - 1) current += bf.Spacing;
            idx++;
        }

        int w = Math.Max(maxLine, current);
        return (int)(w * textSize);
    }

    public static int GetTextWidth(string text, Font font = Font.Georgia, float textSize = 1.0f)
    {
        if (SettingsManager.Instance.BitmapFont)
        {
            if (Enum.TryParse<Core.Globals.BitmapFont>(font.ToString(), out var bfEnum) && HasBitmapFont(bfEnum))
                return GetTextWidth(text ?? string.Empty, bfEnum, textSize);
        }

        if (!Fonts.TryGetValue(font, out var spriteFont))
        {
            if (!Fonts.TryGetValue(Font.Georgia, out spriteFont))
            {
                if (Fonts.Count > 0) spriteFont = Fonts.Values.First();
                else return (int)Math.Round((text?.Length ?? 0) * 8f * textSize);
            }
        }
        var sanitizedText = SanitizeText(text ?? string.Empty, spriteFont);
        var dims = spriteFont.MeasureString(sanitizedText);
        return (int)Math.Round(dims.X * textSize);
    }

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
            size += 10L; // legacy approximation

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

    // Explicit bitmap render overload
    public static void RenderText(string text, int x, int y, Color frontColor, Color backColor, Core.Globals.BitmapFont font, float textSize = 1.0f)
    {
        if (string.IsNullOrEmpty(text) || GameClient.SpriteBatch == null) return;
        if (!TryGetBitmapFont(font, out var bf)) return;

        // IMPORTANT: For crisp bitmap fonts ensure SpriteBatch.Begin(...) uses SamplerState.PointClamp externally.
        int originX = x - bf.XOffset;
        int originY = y - bf.YOffset;
        int lineX = originX;
        int lineY = originY;
        int shadowX = lineX + 1;
        int shadowY = lineY + 1;
        int colorSkip = 0;
        int idx = 0;
        int len = text.Length;

        int lineAdvance = (int)Math.Round(bf.CharHeight * textSize);
        int gapAdvance = (int)Math.Round(3 * textSize); // line gap

        foreach (var ch in text)
        {
            if (colorSkip > 0)
            {
                colorSkip--;
                continue;
            }
            if (ch == '\r') continue;

            if (ch == '\n')
            {
                lineY += lineAdvance + gapAdvance;
                shadowY += lineAdvance + gapAdvance;
                lineX = originX;
                shadowX = originX + 1;
                idx = 0;
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

            int drawW = (int)Math.Round(adv * textSize);
            int drawH = (int)Math.Round(bf.CharHeight * textSize);

            // Draw shadow
            GameClient.SpriteBatch.Draw(bf.Atlas, new Rectangle(shadowX, shadowY, drawW, drawH), rect, backColor);
            // Draw glyph
            GameClient.SpriteBatch.Draw(bf.Atlas, new Rectangle(lineX, lineY, drawW, drawH), rect, frontColor);

            shadowX += drawW + (idx < len - 1 ? bf.Spacing : 0);
            lineX += drawW + (idx < len - 1 ? bf.Spacing : 0);
            idx++;
        }
    }

    // SpriteFont render with auto-bitmap override
    public static void RenderText(string text, int x, int y, Color frontColor, Color backColor, Font font = Font.Georgia, float textSize = 1.0f)
    {
        if (SettingsManager.Instance.BitmapFont)
        {
            if (Enum.TryParse<Core.Globals.BitmapFont>(font.ToString(), out var bfEnum) && HasBitmapFont(bfEnum))
            {
                RenderText(text, x, y, frontColor, backColor, bfEnum, textSize);
                return;
            }
        }

        if (!Fonts.TryGetValue(font, out var spriteFont))
        {
            if (!Fonts.TryGetValue(Font.Georgia, out spriteFont))
            {
                if (Fonts.Count > 0) spriteFont = Fonts.Values.First();
                else return;
            }
        }

        var sanitizedText = SanitizeText(text, spriteFont);
        GameClient.SpriteBatch.DrawString(spriteFont, sanitizedText, new Vector2(x + 1, y + 1), backColor, 0.0f, Vector2.Zero, (12f / 16f) * textSize, SpriteEffects.None, 0.0f);
        GameClient.SpriteBatch.DrawString(spriteFont, sanitizedText, new Vector2(x, y), frontColor, 0.0f, Vector2.Zero, (12f / 16f) * textSize, SpriteEffects.None, 0.0f);
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
                ref var withBlock = ref Data.MyMap.Tile[x, y];
                var tX = (int)Math.Round(GameLogic.ConvertMapX(x * GameState.SizeX) - 4 + GameState.SizeX * 0.5d);
                var tY = (int)Math.Round(GameLogic.ConvertMapY(y * GameState.SizeY) - 7 + GameState.SizeY * 0.5d);
                tA = GameState.EditorAttribute == 1 ? (int)withBlock.Type : (int)withBlock.Type2;
                switch (tA)
                {
                    case (int)TileType.Blocked: RenderText("B", tX, tY, Color.Red, Color.Black); break;
                    case (int)TileType.Warp: RenderText("W", tX, tY, Color.Blue, Color.Black); break;
                    case (int)TileType.Item: RenderText("I", tX, tY, Color.White, Color.Black); break;
                    case (int)TileType.NpcAvoid: RenderText("N", tX, tY, Color.White, Color.Black); break;
                    case (int)TileType.Resource: RenderText("R", tX, tY, Color.Green, Color.Black); break;
                    case (int)TileType.NpcSpawn: RenderText("S", tX, tY, Color.Yellow, Color.Black); break;
                    case (int)TileType.Shop: RenderText("S", tX, tY, Color.Blue, Color.Black); break;
                    case (int)TileType.Bank: RenderText("B", tX, tY, Color.Blue, Color.Black); break;
                    case (int)TileType.Heal: RenderText("H", tX, tY, Color.Green, Color.Black); break;
                    case (int)TileType.Trap: RenderText("T", tX, tY, Color.Red, Color.Black); break;
                    case (int)TileType.Animation: RenderText("A", tX, tY, Color.Red, Color.Black); break;
                    case (int)TileType.NoCrossing: RenderText("X", tX, tY, Color.Red, Color.Black); break;
                }
            }
        }
    }

    public static void DrawNpcName(int mapNpcNum)
    {
        int textY;
        var color = default(Color);
        var backColor = default(Color);

        double npcNum = Data.MyMapNpc[mapNpcNum].Num;

        if (npcNum < 0 | npcNum > Variables.MaxNpcs) return;
        if (EditorType.Map == GameState.MyEditorType) return;

        switch (Data.Npc[(int)npcNum].Behavior)
        {
            case 0: color = Color.Red; backColor = Color.Black; break;
            case 1: color = Color.Green; backColor = Color.Black; break;
            case 2: color = Color.Yellow; backColor = Color.Black; break;
        }

        var remaining = Data.MyMapNpc[mapNpcNum].DeathTimer - General.GetTickCount() / 1000;
        if (remaining < 0) remaining = 0;

        var name = remaining > 0 ? $"{remaining}..." : Data.Npc[(int)npcNum].Name;

        int baseWorldX = Data.MyMapNpc[mapNpcNum].X;
        int baseWorldY = Data.MyMapNpc[mapNpcNum].Y;
        int centerX = GameLogic.ConvertMapX(baseWorldX) + GameState.SizeX / 2 - 4;
        var textX = centerX - (int)(GetTextWidth(name) / 6d);

        int spriteNum = Data.Npc[(int)npcNum].Sprite;
        if (spriteNum <= 0 || spriteNum > GameState.NumCharacters)
        {
            textY = GameLogic.ConvertMapY(baseWorldY) - 16;
            RenderText(name, textX, textY, color, backColor);
            return;
        }

        var gfxInfo = GameClient.GetGfxInfo(Path.Combine(DataPath.Characters, spriteNum.ToString()));
        if (gfxInfo == null || gfxInfo.Height <= 0)
        {
            textY = GameLogic.ConvertMapY(baseWorldY) - 16;
            RenderText(name, textX, textY, color, backColor);
            return;
        }

        int configuredDirs = SettingsManager.Instance.SpriteDirections;
        if (configuredDirs <= 0) configuredDirs = 4;
        configuredDirs = Math.Max(1, configuredDirs);
        int directionRows = 1;
        if (gfxInfo.Height % configuredDirs == 0) directionRows = configuredDirs;
        else if (configuredDirs != 8 && gfxInfo.Height % 8 == 0) directionRows = 8;
        else if (configuredDirs != 4 && gfxInfo.Height % 4 == 0) directionRows = 4;

        int frameHeight = gfxInfo.Height / directionRows;
        if (frameHeight <= 0) frameHeight = 32;

        int spriteTopWorldY = baseWorldY;
        if (frameHeight > 32) spriteTopWorldY = baseWorldY - (frameHeight - 32);

        int spriteTopScreenY = GameLogic.ConvertMapY(spriteTopWorldY);
        int textPixelHeight = (int)Math.Ceiling(Fonts[Font.Georgia].LineSpacing * 12f / 16f);
        int margin = 8;
        textY = spriteTopScreenY - textPixelHeight + margin;
        RenderText(name, textX, textY, color, backColor);
    }

    public static void DrawEventName(int index)
    {
        if (Data.MapEvents == null) return;
        if (index < 0 || index >= Data.MapEvents.Length) return;

        var textY = 0;
        var color = Color.Green;
        var backcolor = Color.Black;
        var name = Data.MapEvents[index].Name;

        var textX = GameLogic.ConvertMapX(Data.MapEvents[index].X) + GameState.SizeX / 2 - 6;
        textX -= GetTextWidth(name) / 6;

        if (Data.MapEvents[index].GraphicType == 1)
        {
            int spriteNum = Data.MapEvents[index].Graphic;
            if (spriteNum <= 0 || spriteNum > GameState.NumCharacters)
            {
                textY = GameLogic.ConvertMapY(Data.MapEvents[index].Y) - 16;
            }
            else
            {
                var gfxInfo = GameClient.GetGfxInfo(Path.Combine(DataPath.Characters, spriteNum.ToString()));
                if (gfxInfo == null || gfxInfo.Height <= 0)
                {
                    textY = GameLogic.ConvertMapY(Data.MapEvents[index].Y) - 16;
                }
                else
                {
                    int configuredDirs = SettingsManager.Instance.SpriteDirections;
                    if (configuredDirs <= 0) configuredDirs = 4;
                    configuredDirs = Math.Max(1, configuredDirs);
                    int directionRows = 1;
                    if (gfxInfo.Height % configuredDirs == 0) directionRows = configuredDirs;
                    else if (configuredDirs != 8 && gfxInfo.Height % 8 == 0) directionRows = 8;
                    else if (configuredDirs != 4 && gfxInfo.Height % 4 == 0) directionRows = 4;

                    int frameHeight = gfxInfo.Height / directionRows;
                    if (frameHeight <= 0) frameHeight = 32;

                    int baseWorldY = Data.MapEvents[index].Y;
                    int spriteTopWorldY = baseWorldY;
                    if (frameHeight > 32) spriteTopWorldY = baseWorldY - (frameHeight - 32);

                    int spriteTopScreenY = GameLogic.ConvertMapY(spriteTopWorldY);
                    int textPixelHeight = (int)Math.Ceiling(Fonts[Font.Georgia].LineSpacing * 12f / 16f);
                    int margin = 8;
                    textY = spriteTopScreenY - textPixelHeight + margin;
                }
            }
        }
        else if (Data.MapEvents[index].GraphicType == 2)
        {
            if (Data.MapEvents[index].GraphicY2 > 0)
            {
                textX = textX + Data.MapEvents[index].GraphicY2 * GameState.SizeY / 2 - 6;
                textY = GameLogic.ConvertMapY(Data.MapEvents[index].Y) - Data.MapEvents[index].GraphicY2 * GameState.SizeY + 16;
            }
            else
            {
                textY = GameLogic.ConvertMapY(Data.MapEvents[index].Y) - 32 + 16;
            }
        }
        else
        {
            textY = GameLogic.ConvertMapY(Data.MapEvents[index].Y) - 16;
        }

        RenderText(name, textX, textY, color, backcolor);
    }

    public static void DrawActionMsg(int index)
    {
        var x = 0;
        var y = 0;
        var time = 0;

        switch (Data.ActionMsg[index].Type)
        {
            case (int)ActionMessageType.Static:
            {
                time = 1500;
                if (Data.ActionMsg[index].Y > 0)
                {
                    x = Data.ActionMsg[index].X + Conversion.Int(GameState.SizeX / 2) - Strings.Len(Data.ActionMsg[index].Message) / 2 * 8;
                    y = Data.ActionMsg[index].Y - Conversion.Int(GameState.SizeY / 2) - 2;
                }
                else
                {
                    x = Data.ActionMsg[index].X + Conversion.Int(GameState.SizeX / 2) - Strings.Len(Data.ActionMsg[index].Message) / 2 * 8;
                    y = Data.ActionMsg[index].Y - Conversion.Int(GameState.SizeY / 2) + 18;
                }
                break;
            }
            case (int)ActionMessageType.Scroll:
            {
                time = 1500;
                if (Data.ActionMsg[index].Y > 0)
                {
                    x = Data.ActionMsg[index].X + Conversion.Int(GameState.SizeX / 2) - Strings.Len(Data.ActionMsg[index].Message) / 2 * 8;
                    y = (int)Math.Round(Data.ActionMsg[index].Y - Conversion.Int(GameState.SizeY / 2) - 2 - Data.ActionMsg[index].Scroll * 0.6d);
                    Data.ActionMsg[index].Scroll++;
                }
                else
                {
                    x = Data.ActionMsg[index].X + Conversion.Int(GameState.SizeX / 2) - Strings.Len(Data.ActionMsg[index].Message) / 2 * 8;
                    y = (int)Math.Round(Data.ActionMsg[index].Y - Conversion.Int(GameState.SizeY / 2) + 18 + Data.ActionMsg[index].Scroll * 0.6d);
                    Data.ActionMsg[index].Scroll++;
                }
                break;
            }
            case (int)ActionMessageType.Screen:
            {
                time = 3000;
                for (int i = byte.MaxValue; i >= 0; i--)
                {
                    if (Data.ActionMsg[i].Type == (int)ActionMessageType.Screen && i != index)
                    {
                        GameLogic.ClearActionMsg((byte)index);
                        index = i;
                    }
                }
                x = GameState.ResolutionWidth / 2 - Strings.Len(Data.ActionMsg[index].Message) / 2 * 8;
                y = 425;
                break;
            }
        }

        x = GameLogic.ConvertMapX(x);
        y = GameLogic.ConvertMapY(y);

        if (General.GetTickCount() < Data.ActionMsg[index].Created + time)
        {
            RenderText(Data.ActionMsg[index].Message, x, y, GameClient.QbColorToXnaColor(Data.ActionMsg[index].Color), Color.Black);
        }
        else
        {
            GameLogic.ClearActionMsg((byte)index);
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
                        RenderText(wrappedLines[j], (int)xO, (int)(yO + yOffset + 10 * j), color2, color2);
                    }
                    rLines += wrappedLines.Length;
                    for (var x = 0; x < wrappedLines.Length; x++)
                        if (GetTextWidth(wrappedLines[x]) > topWidth) topWidth = GetTextWidth(wrappedLines[x]);
                }
                else
                {
                    yOffset -= 12L;
                    RenderText(Data.Chat[(int)i].Text, (int)xO, (int)(yO + yOffset), color2, color2);
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
        RenderText(Data.MyMap.Name, (int)Math.Round(GameState.ResolutionWidth / 2d - GetTextWidth(Data.MyMap.Name)), 10, GameState.DrawMapNameColor, Color.Black);
    }

    public static void DrawPlayerName(int index)
    {
        int textY;
        var color = default(Color);
        var backColor = default(Color);

        if (!GetPlayerPk(index))
        {
            switch (GetPlayerAccess(index))
            {
                case (int)AccessLevel.Player: color = Color.White; backColor = Color.Black; break;
                case (int)AccessLevel.Moderator: color = Color.Cyan; backColor = Color.White; break;
                case (int)AccessLevel.Mapper: color = Color.Green; backColor = Color.Black; break;
                case (int)AccessLevel.Developer: color = Color.Blue; backColor = Color.Black; break;
                case (int)AccessLevel.Owner: color = Color.Yellow; backColor = Color.Black; break;
            }
        }
        else
        {
            color = Color.Red;
        }

        var remaining = (Data.Player[index].DeathTimer - General.GetTickCount()) / 1000;
        if (remaining < 0) remaining = 0;
        var name = remaining > 0 ? $"{remaining}..." : Data.Player[index].Name;

        int baseWorldX = GetPlayerRawX(index);
        int baseWorldY = GetPlayerRawY(index);
        int centerX = GameLogic.ConvertMapX(baseWorldX) + GameState.SizeX / 2 - 6;
        var textX = (int)Math.Round(centerX - GetTextWidth(name) / 6d);

        int spriteNum = GetPlayerSprite(index);
        if (spriteNum <= 0 || spriteNum > GameState.NumCharacters)
        {
            textY = GameLogic.ConvertMapY(baseWorldY) - 16;
            RenderText(name, textX, textY, color, backColor);
            return;
        }

        var gfxInfo = GameClient.GetGfxInfo(Path.Combine(DataPath.Characters, spriteNum.ToString()));
        if (gfxInfo == null || gfxInfo.Height <= 0)
        {
            textY = GameLogic.ConvertMapY(baseWorldY) - 16;
            RenderText(name, textX, textY, color, backColor);
            return;
        }

        int configuredDirs = SettingsManager.Instance.SpriteDirections;
        if (configuredDirs <= 0) configuredDirs = 4;
        configuredDirs = Math.Max(1, configuredDirs);
        int directionRows = 1;
        if (gfxInfo.Height % configuredDirs == 0) directionRows = configuredDirs;
        else if (configuredDirs != 8 && gfxInfo.Height % 8 == 0) directionRows = 8;
        else if (configuredDirs != 4 && gfxInfo.Height % 4 == 0) directionRows = 4;

        int frameHeight = gfxInfo.Height / directionRows;
        if (frameHeight <= 0) frameHeight = 32;

        int spriteTopWorldY = baseWorldY;
        if (frameHeight > 32) spriteTopWorldY = baseWorldY - (frameHeight - 32);

        int spriteTopScreenY = GameLogic.ConvertMapY(spriteTopWorldY);
        int textPixelHeight = (int)Math.Ceiling(Fonts[Font.Georgia].LineSpacing * 12f / 16f);
        int margin = 8;
        textY = spriteTopScreenY - textPixelHeight + margin;
        RenderText(name, textX, textY, color, backColor);
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
        if (SettingsManager.Instance.BitmapFont)
        {
            if (Enum.TryParse<Core.Globals.BitmapFont>(font.ToString(), out var bfEnum) && HasBitmapFont(bfEnum))
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
                    return (int)Math.Round(16f * lines * textSize);
                }
            }
        }
        var dimensions = spriteFont.MeasureString(text ?? string.Empty);
        return (int)Math.Round(dimensions.Y * textSize);
    }
}