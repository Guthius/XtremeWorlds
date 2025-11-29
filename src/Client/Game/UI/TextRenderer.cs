using Core.Configurations;
using Core.Globals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using static Core.Globals.Command;

namespace Client.Game.UI;

public static class TextRenderer
{
    public static readonly Dictionary<Font, SpriteFont> Fonts = new();

    // Bitmap font support structures
    private class BitmapFont
    {
        public Texture2D Atlas;
        public Dictionary<char, Rectangle> Glyphs = new();
        public int LineHeight; // raw cell height
        public int CharHeight; // CellHeight - 4 like VB
        public int Spacing; // glyph gap
        public int BaseOffset; // base char offset from header
        public int XOffset; // per-font draw offset (baseline tweak)
        public int YOffset; // per-font draw offset (baseline tweak)
        public char? ColourChar; // special in-text color control marker, if used
        public Dictionary<char,int> Adv = new();
    }
    private static readonly Dictionary<Font, BitmapFont> _bitmapFonts = new();

    public static Color GetColorForAmount(int amount)
    {
        return amount switch
        {
            < 1000000 => Color.White,
            < 10000000 => Color.Yellow,
            _ => Color.LightGreen
        };
    }

    public static string CensorText(string input)
    {
        return new string('*', input.Length);
    }

    public static string SanitizeText(string text, SpriteFont font)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        var sanitizedText = new StringBuilder();

        foreach (var ch in text)
        {
            if (font.Characters.Contains(ch))
            {
                sanitizedText.Append(ch);
            }
        }

        return sanitizedText.ToString();
    }

    public static void RegisterBitmapFont(Font font, Texture2D atlas, Dictionary<char, Rectangle> glyphs, int lineHeight, int spacing = 1, int baseOffset = 32, Dictionary<char,int>? advances = null)
    {
        var bf = new BitmapFont{Atlas=atlas, Glyphs=glyphs, LineHeight=lineHeight, Spacing=spacing, BaseOffset=baseOffset};
        if (advances!=null) bf.Adv = advances;
        _bitmapFonts[font] = bf;
    }

    private static bool TryGetBitmapFont(Font font, out BitmapFont bf)
    {
        return _bitmapFonts.TryGetValue(font, out bf);
    }

    // Load legacy .dat header (VFH) + atlas; ignores cached vertex data portion beyond widths.
    public static bool LoadLegacyBitmapFont(Font font, string datPath, string pngPath, GraphicsDevice gd)
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
        // Remaining bytes (vertex data) are ignored.
        int rowPitch = cellW == 0 ? 1 : bmpW / cellW;
        float colFactor = cellW / (float)bmpW;
        float rowFactor = cellH / (float)bmpH;
        var glyphRects = new Dictionary<char, Rectangle>();
        var advances = new Dictionary<char,int>();
        for (int code=0; code < 256; code++)
        {
            int row = (code - baseChar) / rowPitch;
            int col = (code - baseChar) - (row * rowPitch);
            if (row < 0) row = 0; if (col < 0) col = 0;
            int x = col * cellW; int y = row * cellH;
            if (x + cellW > bmpW || y + cellH > bmpH) continue; // skip out of range
            var ch = (char)code;
            glyphRects[ch] = new Rectangle(x,y,cellW,cellH);
            advances[ch] = widths[code] == 0 ? cellW : widths[code];
        }
        RegisterBitmapFont(font, atlas, glyphRects, cellH, 1, baseChar, advances);
        return true;
    }

    // Add helper method to check if a font is bitmap-enabled
    public static bool HasBitmapFont(Font font)
    {
        return _bitmapFonts.ContainsKey(font);
    }

    // Public loader invoked during content load for legacy VB headers
    public static void TryLoadLegacyFont(GraphicsDevice gd, string fontName)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var fontsDir = Path.Combine(baseDir, "Content", "Fonts");
            var datPath = Path.Combine(fontsDir, fontName + ".dat");
            var pngPath = Path.Combine(fontsDir, fontName + ".png");
            if (LoadLegacyBitmapFont(Font.Georgia, datPath, pngPath, gd)) return;
        }
        catch { }
    }

    // Adjust GetTextWidth to use scale parameter for bitmap fonts when needed
    public static int GetTextWidth(string text, Font font = Font.Georgia, float textSize = 1.0f)
    {
        if (TryGetBitmapFont(font, out var bf))
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int maxLine = 0;
            int current = 0;
            foreach (var ch in text)
            {
                if (ch == '\n')
                {
                    if (current > maxLine) maxLine = current;
                    current = 0;
                    continue;
                }

                Rectangle rect;
                int adv;
                if (!bf.Glyphs.TryGetValue(ch, out rect))
                {
                    rect = bf.Glyphs.TryGetValue(' ', out var sp) ? sp : new Rectangle(0, 0, bf.LineHeight / 2, bf.LineHeight);
                }
                adv = bf.Adv.TryGetValue(ch, out var a) ? a : rect.Width;

                // Add advance; only add spacing if a next printable follows
                current += adv;
                current += bf.Spacing;
            }

            // Remove spacing after last glyph on each measured line by subtracting spacing if any glyph was added
            if (current >= bf.Spacing) current -= bf.Spacing;
            int w = Math.Max(maxLine, current);
            return (int)Math.Round(w * textSize);
        }

        // SpriteFont fallback unchanged
        if (!Fonts.TryGetValue(font, out var spriteFont))
        {
            if (!Fonts.TryGetValue(Font.Georgia, out spriteFont))
            {
                if (Fonts.Count > 0)
                {
                    spriteFont = Fonts.Values.First();
                }
                else
                {
                    // No fonts loaded at all; return an estimated width to avoid crashing.
                    return (int)Math.Round((text?.Length ?? 0) * 8f * textSize);
                }
            }
        }

        var sanitizedText = SanitizeText(text ?? string.Empty, spriteFont);
        var textDimensions = spriteFont.MeasureString(sanitizedText);
        return (int)Math.Round(textDimensions.X * textSize);
    }

    // Get the height of the text with optional scaling

    public static void AddText(string text, int color, long alpha = 255L, byte channel = 0)
    {
        // wordwrap
        string[] wrappedLines = System.Array.Empty<string>();
        WordWrap(text, Font.Georgia, WindowManager.Windows[WindowManager.GetWindowIndex("winChat")].Width, ref wrappedLines);

        GameState.ChatHighIndex += wrappedLines.Length;

        if (GameState.ChatHighIndex > Variables.ChatLines)
            GameState.ChatHighIndex = Variables.ChatLines;

        // Move the rest of the chat lines up
        for (var i = (int) GameState.ChatHighIndex - wrappedLines.Length; i > 0; i--)
        {
            Data.Chat[i] = Data.Chat[i - 1];
        }

        for (int i = wrappedLines.Length - 1, chatIndex = 0; i >= 0; i--, chatIndex++)
        {
            // Add the wrapped line to the chat
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

        // Too small of text
        if (Strings.Len(text) < 2)
        {
            theArray = new string[2];
            theArray[1] = text;
            return;
        }

        // default values
        var b = 1L;
        var lastSpace = 1L;
        var size = 0L;
        long tmpNum = Strings.Len(text);

        var loopTo = tmpNum;
        for (var i = 1L; i <= loopTo; i++)
        {
            // if it's a space, store it
            switch (Strings.Mid(text, (int) i, 1) ?? "")
            {
                case " ":
                {
                    lastSpace = i;
                    break;
                }
            }

            // Add up the size
            size = size + 10L;

            // Check for too large of a size
            if (size > maxLineLen)
            {
                // Check if the last space was too far back
                if (i - lastSpace > 10L)
                {
                    // Too far away to the last space, so break at the last character
                    lineCount = lineCount + 1L;
                    Array.Resize(ref theArray, (int) (lineCount));
                    theArray[(int) lineCount - 1] = Strings.Mid(text, (int) b, (int) (i - 1L - b));
                    b = i - 1L;
                    size = 0L;
                }
                else
                {
                    // Break at the last space to preserve the word
                    lineCount = lineCount + 1L;
                    Array.Resize(ref theArray, (int) (lineCount));

                    // Ensure b is within valid range
                    if (b < 0L)
                        b = 0L;

                    if (b > text.Length)
                        b = text.Length;

                    // Ensure the length parameter is not negative
                    var substringLength = (int) (lastSpace - b);
                    if (substringLength < 0)
                        substringLength = 0;

                    // Extract the substring and assign it to the array
                    theArray[(int) lineCount - 1] = Strings.Mid(text, (int) b, substringLength);

                    b = lastSpace + 1L;
                    // Count all the words we ignored (the ones that weren't printed, but are before "i")
                    size = GetTextWidth(Strings.Mid(text, (int) lastSpace, (int) (i - lastSpace)), font);
                }
            }

            // Remainder
            if (i == Strings.Len(text))
            {
                if (b != i)
                {
                    lineCount = lineCount + 1L;
                    Array.Resize(ref theArray, (int) (lineCount));
                    theArray[(int) lineCount - 1] = Strings.Mid(text, (int) b, (int) i);
                }
            }
        }
    }

    public static void RenderText(string text, int x, int y, Color frontColor, Color backColor, Font font = Font.Georgia, float textSize = 1.0f)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (GameClient.SpriteBatch == null) return;
        if (TryGetBitmapFont(font, out var bf))
        {
            int originX = x - bf.XOffset;
            int originY = y - bf.YOffset;

            int lineX = originX;
            int lineY = originY;
            int shadowX = lineX + 1;
            int shadowY = lineY + 1;

            int colorSkip = 0;

            foreach (var ch in text)
            {
                if (colorSkip > 0)
                {
                    colorSkip--;
                    continue;
                }

                if (ch == '\r') continue; // VB splits by vbCrLf; ignore CR
                if (ch == '\n')
                {
                    // Move to next line
                    int lineAdvance = (int)Math.Round(bf.LineHeight * textSize) + (int)Math.Round(bf.Spacing * textSize);
                    lineY += lineAdvance;
                    shadowY += lineAdvance;
                    lineX = x;
                    shadowX = x + 1;
                    continue;
                }

                if (!bf.Glyphs.TryGetValue(ch, out var rect))
                {
                    rect = bf.Glyphs.TryGetValue(' ', out var sp) ? sp : new Rectangle(0, 0, bf.LineHeight / 2, bf.LineHeight);
                }
                int adv = bf.Adv.TryGetValue(ch, out var a) ? a : rect.Width;

                int dw = (int)Math.Round(adv * textSize);
                int dh = (int)Math.Round((bf.CharHeight > 0 ? bf.CharHeight : rect.Height) * textSize);

                GameClient.SpriteBatch.Draw(bf.Atlas, new Rectangle(shadowX, shadowY, dw, dh), rect, backColor);
                GameClient.SpriteBatch.Draw(bf.Atlas, new Rectangle(lineX, lineY, dw, dh), rect, frontColor);

                int gap = (int)Math.Round(bf.Spacing * textSize);
                shadowX += dw + gap;
                lineX += dw + gap;
            }
            return;
        }

        // SpriteFont fallback – apply textSize consistently
        if (!Fonts.TryGetValue(font, out var spriteFont))
        {
            if (!Fonts.TryGetValue(Font.Georgia, out spriteFont))
            {
                if (Fonts.Count > 0)
                {
                    spriteFont = Fonts.Values.First();
                }
                else
                {
                    return; // No fonts available; nothing to render, but don't crash.
                }
            }
        }

        var sanitizedText = SanitizeText(text, spriteFont);
        GameClient.SpriteBatch.DrawString(spriteFont, sanitizedText, new Vector2(x + 1, y + 1), backColor, 0.0f, Vector2.Zero, 12f / 16.0f, SpriteEffects.None, 0.0f);
        GameClient.SpriteBatch.DrawString(spriteFont, sanitizedText, new Vector2(x, y), frontColor, 0.0f, Vector2.Zero, 12f / 16.0f, SpriteEffects.None, 0.0f);
    }

    public static void DrawMapAttributes()
    {
        int tA;

        var loopTo = (int) GameState.TileView.Right;
        for (var x = (int) GameState.TileView.Left; x < loopTo; x++)
        {
            var loopTo1 = (int) GameState.TileView.Bottom;
            for (var y = (int) GameState.TileView.Top; y < loopTo1; y++)
            {
                if (GameLogic.IsValidMapPoint(x, y))
                {
                    {
                        ref var withBlock = ref Data.MyMap.Tile[x, y];
                        var tX = (int) Math.Round(GameLogic.ConvertMapX(x * GameState.SizeX) - 4 + GameState.SizeX * 0.5d);
                        var tY = (int) Math.Round(GameLogic.ConvertMapY(y * GameState.SizeY) - 7 + GameState.SizeY * 0.5d);

                        if (GameState.EditorAttribute == 1)
                        {
                            tA = (int) withBlock.Type;
                        }
                        else
                        {
                            tA = (int) withBlock.Type2;
                        }

                        switch (tA)
                        {
                            case (int) TileType.Blocked:
                            {
                                RenderText("B", tX, tY, Color.Red, Color.Black);
                                break;
                            }
                            case (int) TileType.Warp:
                            {
                                RenderText("W", tX, tY, Color.Blue, Color.Black);
                                break;
                            }
                            case (int) TileType.Item:
                            {
                                RenderText("I", tX, tY, Color.White, Color.Black);
                                break;
                            }
                            case (int) TileType.NpcAvoid:
                            {
                                RenderText("N", tX, tY, Color.White, Color.Black);
                                break;
                            }
                            case (int) TileType.Resource:
                            {
                                RenderText("R", tX, tY, Color.Green, Color.Black);
                                break;
                            }
                            case (int) TileType.NpcSpawn:
                            {
                                RenderText("S", tX, tY, Color.Yellow, Color.Black);
                                break;
                            }
                            case (int) TileType.Shop:
                            {
                                RenderText("S", tX, tY, Color.Blue, Color.Black);
                                break;
                            }
                            case (int) TileType.Bank:
                            {
                                RenderText("B", tX, tY, Color.Blue, Color.Black);
                                break;
                            }
                            case (int) TileType.Heal:
                            {
                                RenderText("H", tX, tY, Color.Green, Color.Black);
                                break;
                            }
                            case (int) TileType.Trap:
                            {
                                RenderText("T", tX, tY, Color.Red, Color.Black);
                                break;
                            }
                            case (int) TileType.Animation:
                            {
                                RenderText("A", tX, tY, Color.Red, Color.Black);
                                break;
                            }
                            case (int) TileType.NoCrossing:
                            {
                                RenderText("X", tX, tY, Color.Red, Color.Black);
                                break;
                            }
                        }
                    }
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

        if (npcNum < 0 | npcNum > Variables.MaxNpcs)
            return;
            

        if (EditorType.Map == GameState.MyEditorType)
            return;

        switch (Data.Npc[(int)npcNum].Behavior)
        {
            case 0: // attack on sight
                {
                    color = Color.Red;
                    backColor = Color.Black;
                    break;
                }
            case 1: // attack when attacked + guard
                {
                    color = Color.Green;
                    backColor = Color.Black;
                    break;
                }
            case 2: // friendly + shopkeeper + quest
                {
                    color = Color.Yellow;
                    backColor = Color.Black;
                    break;
                }
        }

        var remaining = Data.MyMapNpc[mapNpcNum].DeathTimer - General.GetTickCount() / 1000;
        if (remaining < 0) remaining = 0;

        var name = "";

        if (remaining > 0)
        {
            name = $"{remaining}...";
        }
        else
        {
            name = Data.Npc[(int)npcNum].Name;
        }

        int baseWorldX = Data.MyMapNpc[mapNpcNum].X;
        int baseWorldY = Data.MyMapNpc[mapNpcNum].Y;
        int centerX = GameLogic.ConvertMapX(baseWorldX) + GameState.SizeX / 2 - 4;
        var textX = centerX - (int)(GetTextWidth(name) / 6d);

        // Determine name Y based on tall sprite logic (same as DrawPlayerName)
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
        if (frameHeight > 32)
        {
            spriteTopWorldY = baseWorldY - (frameHeight - 32);
        }

        int spriteTopScreenY = GameLogic.ConvertMapY(spriteTopWorldY);
        int textPixelHeight = (int)Math.Ceiling(Fonts[Font.Georgia].LineSpacing * 12f / 16f);
        int margin = 8;

        textY = spriteTopScreenY - textPixelHeight + margin;

        // Draw name
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

        // calc pos
        var textX = GameLogic.ConvertMapX(Data.MapEvents[index].X) + GameState.SizeX / 2 - 6;
        textX -= GetTextWidth(name) / 6;

        // Choose Y using same tall-sprite logic as DrawPlayerName when the event uses a character sprite
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
                    if (frameHeight > 32)
                    {
                        spriteTopWorldY = baseWorldY - (frameHeight - 32);
                    }

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
            // GraphicType 0 or unknown – fallback legacy behavior
            textY = GameLogic.ConvertMapY(Data.MapEvents[index].Y) - 16;
        }

        // Draw name
        RenderText(name, textX, textY, color, backcolor);
    }

    public static void DrawActionMsg(int index)
    {
        var x = 0;
        var y = 0;
        var time = 0;

        // how long we want each message to appear
        switch (Data.ActionMsg[index].Type)
        {
            case (int) ActionMessageType.Static:
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

            case (int) ActionMessageType.Scroll:
            {
                time = 1500;

                if (Data.ActionMsg[index].Y > 0)
                {
                    x = Data.ActionMsg[index].X + Conversion.Int(GameState.SizeX / 2) - Strings.Len(Data.ActionMsg[index].Message) / 2 * 8;
                    y = (int) Math.Round(Data.ActionMsg[index].Y - Conversion.Int(GameState.SizeY / 2) - 2 - Data.ActionMsg[index].Scroll * 0.6d);
                    Data.ActionMsg[index].Scroll = Data.ActionMsg[index].Scroll + 1;
                }
                else
                {
                    x = Data.ActionMsg[index].X + Conversion.Int(GameState.SizeX / 2) - Strings.Len(Data.ActionMsg[index].Message) / 2 * 8;
                    y = (int) Math.Round(Data.ActionMsg[index].Y - Conversion.Int(GameState.SizeY / 2) + 18 + Data.ActionMsg[index].Scroll * 0.6d);
                    Data.ActionMsg[index].Scroll = Data.ActionMsg[index].Scroll + 1;
                }

                break;
            }

            case (int) ActionMessageType.Screen:
            {
                time = 3000;

                // This will kill any action screen messages that there in the system
                for (int i = byte.MaxValue; i >= 0; i -= 1)
                {
                    if (Data.ActionMsg[i].Type == (int) ActionMessageType.Screen)
                    {
                        if (i != index)
                        {
                            GameLogic.ClearActionMsg((byte) index);
                            index = i;
                        }
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
            GameLogic.ClearActionMsg((byte) index);
        }
    }

    public static void DrawChat()
    {
    var yOffset = 0L;
        var topWidth = 0;

        // set the position
        var xO = 19L;
        xO += WindowManager.Windows[WindowManager.GetWindowIndex("winChat")].X;
        long yO = GameState.ResolutionHeight - 45;
        var width = (int) WindowManager.Windows[WindowManager.GetWindowIndex("winChat")].Width;

        // loop through chat
        var rLines = 1;
        var i = GameState.ChatScroll;

        while (rLines < 8)
        {
            if (i >= Variables.ChatLines)
                break;

            // exit out early if we come to a blank string
            if (Strings.Len(Data.Chat[(int) i].Text) == 0)
                break;

            // get visible state
            var isVisible = true;
            if (GameState.InSmallChat)
            {
                if (!Data.Chat[(int) i].Visible)
                    isVisible = false;
            }

            if (SettingsManager.Instance.ChannelState[Data.Chat[i].Channel] == 0)
                isVisible = false;

            // make sure it's visible
            if (isVisible)
            {
                // render line
                var color = Data.Chat[(int) i].Color;
                var color2 = GameClient.QbColorToXnaColor(color);

                // check if we need to word wrap
                if (GetTextWidth(Data.Chat[i].Text) > width)
                {
                    // word wrap
                    string[] wrappedLines = new string[0];
                    WordWrap(Data.Chat[(int) i].Text, Font.Georgia, width, ref wrappedLines);

                    // continue on
                    yOffset = yOffset - 10 * wrappedLines.Length;
                    for (var j = 0; j < wrappedLines.Length; j++)
                    {
                        RenderText(wrappedLines[j], (int) xO, (int) (yO + yOffset + 10 * j), color2, color2);
                    }

                    rLines += wrappedLines.Length;

                    // set the top width
                    var loopTo = wrappedLines.Length;
                    for (var x = 0; x < loopTo; x++)
                    {
                        if (GetTextWidth(wrappedLines[x]) > topWidth)
                            topWidth = GetTextWidth(wrappedLines[x]);
                    }
                }
                else
                {
                    // normal
                    yOffset = yOffset - 12L; // Adjusted spacing from 14 to 12

                    RenderText(Data.Chat[(int) i].Text, (int) xO, (int) (yO + yOffset), color2, color2);
                    rLines = rLines + 1;

                    // set the top width
                    if (GetTextWidth(Data.Chat[(int) i].Text) > topWidth)
                        topWidth = GetTextWidth(Data.Chat[(int) i].Text);
                }
            }

            // increment chat pointer
            i = i + 1L;
        }

        // get the height of the small chat box
        GameLogic.SetChatHeight(rLines * 12); // Adjusted spacing from 14 to 12
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

        // Check access level
        if (!GetPlayerPk(index))
        {
            switch (GetPlayerAccess(index))
            {
                case (int) AccessLevel.Player:
                    color = Color.White;
                    backColor = Color.Black;
                    break;

                case (int) AccessLevel.Moderator:
                    color = Color.Cyan;
                    backColor = Color.White;
                    break;

                case (int) AccessLevel.Mapper:
                    color = Color.Green;
                    backColor = Color.Black;
                    break;

                case (int) AccessLevel.Developer:
                    color = Color.Blue;
                    backColor = Color.Black;
                    break;

                case (int) AccessLevel.Owner:
                    color = Color.Yellow;
                    backColor = Color.Black;
                    break;
            }
        }
        else
        {
            color = Color.Red;
        }

        var remaining = (Data.Player[index].DeathTimer - General.GetTickCount()) / 1000;
        if (remaining < 0) remaining = 0;

        var name = "";

        if (remaining > 0)
        {
            name = $"{remaining}...";
        }
        else
        {
            name = Data.Player[index].Name;
        }

        int baseWorldX = GetPlayerRawX(index);
        int baseWorldY = GetPlayerRawY(index);
        int centerX = GameLogic.ConvertMapX(baseWorldX) + GameState.SizeX / 2 - 6;
        var textX = (int)Math.Round(centerX - GetTextWidth(name) / 6d);

        int spriteNum = GetPlayerSprite(index);
        if (spriteNum <= 0 || spriteNum > GameState.NumCharacters)
        {
            // Fallback legacy behavior if sprite invalid
            textY = GameLogic.ConvertMapY(baseWorldY) - 16;
            RenderText(name, textX, textY, color, backColor);
            return;
        }

        // Acquire gfx info and dynamically determine direction rows + frame height
        var gfxInfo = GameClient.GetGfxInfo(Path.Combine(DataPath.Characters, spriteNum.ToString()));
        if (gfxInfo == null || gfxInfo.Height <= 0)
        {
            textY = GameLogic.ConvertMapY(baseWorldY) - 16;
            RenderText(name, textX, textY, color, backColor);
            return;
        }

        // Use the shared helper from Program (static) via full qualification to avoid duplicate logic if namespace differs
        // Compute direction rows (mirror logic from GameClient.ComputeDirectionRows)
        int configuredDirs = SettingsManager.Instance.SpriteDirections;
        if (configuredDirs <= 0) configuredDirs = 4;
        configuredDirs = Math.Max(1, configuredDirs);
        int directionRows = 1;
        if (gfxInfo.Height % configuredDirs == 0) directionRows = configuredDirs;
        else if (configuredDirs != 8 && gfxInfo.Height % 8 == 0) directionRows = 8;
        else if (configuredDirs != 4 && gfxInfo.Height % 4 == 0) directionRows = 4;
        // else remain 1

        int frameHeight = gfxInfo.Height / directionRows;
        if (frameHeight <= 0) frameHeight = 32; // safety fallback

        // Determine the world Y where sprite base (feet) is drawn accounting for tall sprite upward shift in DrawPlayer
        int worldBaseY = baseWorldY;
        int spriteTopWorldY = worldBaseY; // will subtract any tall-sprite offset below
        if (frameHeight > 32)
        {
            spriteTopWorldY = baseWorldY - (frameHeight - 32);
        }

        // Convert top of sprite to screen coordinates
        int spriteTopScreenY = GameLogic.ConvertMapY(spriteTopWorldY);

        int textPixelHeight = (int)Math.Ceiling(Fonts[Font.Georgia].LineSpacing * 12f / 16f);
        int margin = 8;
        textY = spriteTopScreenY - textPixelHeight + margin;
        RenderText(name, textX, textY, color, backColor);
    }

    public static int GetTextHeight(string text, Font font = Font.Georgia, float textSize = 1.0f)
    {
        if (TryGetBitmapFont(font, out var bf))
        {
            int lines = 1;
            if (!string.IsNullOrEmpty(text))
            {
                foreach (var ch in text) if (ch == '\n') lines++;
            }
            int h = (int)Math.Round((bf.CharHeight > 0 ? bf.CharHeight : bf.LineHeight) * textSize);
            int gap = (int)Math.Round(3 * textSize);
            return lines * h + (lines - 1) * gap;
        }

        // SpriteFont fallback unchanged
        if (!Fonts.TryGetValue(font, out var spriteFont))
        {
            if (!Fonts.TryGetValue(Font.Georgia, out spriteFont))
            {
                if (Fonts.Count > 0)
                {
                    spriteFont = Fonts.Values.First();
                }
                else
                {
                    // Fallback estimate: ~16px per line (Georgia baseline), scaled
                    int lines = 1;
                    if (!string.IsNullOrEmpty(text))
                    {
                        foreach (var ch in text) if (ch == '\n') lines++;
                    }
                    return (int)Math.Round(16f * lines * textSize);
                }
            }
        }

        var dimensions = spriteFont.MeasureString(text ?? string.Empty);
        return (int)Math.Round(dimensions.Y * textSize);
    }
}