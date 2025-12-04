using Client;
using Client.Game.UI;
using Core.Configurations;
using Core.Globals;
using Core.Interfaces;
using Microsoft.Xna.Framework;
using static Core.Globals.Command;

public class ChatBubble : IData
{
    public static void OnClear(int index)
    {
        ref var instance = ref Data.ChatBubble[index];
        instance.Target = -1;
        instance.TargetType = 0;
        instance.Msg = "";
        instance.Color = 0;
        instance.Timer = 0;
        instance.Active = false;
    }

    public static void OnDraw(int index)
    {
        var theArray = System.Array.Empty<string>();
        int x;
        int y;
        long i;
        var maxWidth = default(long);
        long x2;
        long y2;
        int color;
        long tmpNum;

        ref var instance = ref Data.ChatBubble[(int) index];

        // exit out early
        if (instance.TargetType == 0)
            return;

        color = instance.Color;

        // calculate position
        switch (instance.TargetType)
        {
            case (byte) TargetType.Player:
            {
                // it's a player
                if (GetPlayerMap(instance.Target) != GetPlayerMap(GameState.MyIndex))
                    return;

                // Base anchor previously used for bubble (top of classic 32px frame)
                x = GameLogic.ConvertMapX(Data.Player[instance.Target].X) + 16;
                y = GameLogic.ConvertMapY(Data.Player[instance.Target].Y) - 8;

                // Adjust upward so bubble sits above nameplate.
                // Recreate nameplate top Y (TextRenderer logic simplified):
                int spriteNumLocal = GetPlayerSprite((int)instance.Target);
                if (spriteNumLocal > 0 && spriteNumLocal <= GameState.NumCharacters)
                {
                    var gi = GameClient.GetGfxInfo(System.IO.Path.Combine(Core.Globals.DataPath.Characters, spriteNumLocal.ToString()));
                    if (gi != null && gi.Height > 0)
                    {
                        int configuredDirs = SettingsManager.Instance.SpriteDirections <= 0 ? 4 : SettingsManager.Instance.SpriteDirections;
                        configuredDirs = Math.Max(1, configuredDirs);
                        int dirs;
                        if (gi.Height % configuredDirs == 0) dirs = configuredDirs;
                        else if (configuredDirs != 8 && gi.Height % 8 == 0) dirs = 8;
                        else if (configuredDirs != 4 && gi.Height % 4 == 0) dirs = 4;
                        else dirs = 1;
                        int frameHeight = gi.Height / dirs;
                        if (frameHeight <= 0) frameHeight = 32;
                        int worldBaseY = Data.Player[instance.Target].Y;
                        if (frameHeight > 32)
                        {
                            // replicate upward shift used when drawing tall sprites
                            int shift = frameHeight - 32;
                            y -= shift; // move anchor up to sprite top
                        }
                        // Nameplate sits (margin + textHeight) above spriteTop
                        int textHeight = (int)Math.Ceiling(TextRenderer.Fonts[Font.Georgia].LineSpacing * 12f / 16f);
                        int nameGap = 4; // from TextRenderer
                        int bubbleExtra = 4; // extra visual gap above name
                        y -= (textHeight);
                        y += nameGap + bubbleExtra; // move anchor up above nameplate
                    }
                }
                break;
            }
            case (byte) TargetType.Event:
            {
                // Event X/Y are stored as tile coordinates
                x = GameLogic.ConvertMapX(Data.MyMap.Event[instance.Target].X * Constants.TileSize) + 16;
                y = GameLogic.ConvertMapY(Data.MyMap.Event[instance.Target].Y * Constants.TileSize) - 16;
                break;
            }

            case (byte) TargetType.Npc:
            {
                x = GameLogic.ConvertMapX(Data.MyMapNpc[instance.Target].X) + 16;
                y = GameLogic.ConvertMapY(Data.MyMapNpc[instance.Target].Y) - 32;
                break;
            }

            default:
            {
                x = 0;
                y = 0;
                return;
            }
        }

        instance.Msg = instance.Msg.Replace("\0", string.Empty);

        // word wrap
        TextRenderer.WordWrap(instance.Msg, Font.Georgia, GameState.ChatBubbleWidth, ref theArray);

        // find max width
        tmpNum = Information.UBound(theArray);

        var loopTo = tmpNum;
        for (i = 0L; i <= loopTo; i++)
        {
            if (TextRenderer.GetTextWidth(theArray[(int) i], Font.Georgia) > maxWidth)
                maxWidth = TextRenderer.GetTextWidth(theArray[(int) i], Font.Georgia);
        }

        // calculate the new position 
        x2 = x - maxWidth / 2L;
        y2 = y - (Information.UBound(theArray) + 1) * 12;

        // render bubble - top left
        string argPath = System.IO.Path.Combine(Core.Globals.DataPath.Gui, 33.ToString());
        GameClient.RenderTexture(ref argPath, (int) (x2 - 9L), (int) (y2 - 5L), 0, 0, 9, 5, 9, 5);

        // top right
        string argPath1 = System.IO.Path.Combine(Core.Globals.DataPath.Gui, 33.ToString());
        GameClient.RenderTexture(ref argPath1, (int) (x2 + maxWidth), (int) (y2 - 5L), 119, 0, 9, 5, 9, 5);

        // top
        string argPath2 = System.IO.Path.Combine(Core.Globals.DataPath.Gui, 33.ToString());
        GameClient.RenderTexture(ref argPath2, (int) x2, (int) (y2 - 5L), 9, 0, (int) maxWidth, 5, 5, 5);

        // bottom left
        string argPath3 = System.IO.Path.Combine(Core.Globals.DataPath.Gui, 33.ToString());
        GameClient.RenderTexture(ref argPath3, (int) (x2 - 9L), (int) y, 0, 19, 9, 6, 9, 6);

        // bottom right
        string argPath4 = System.IO.Path.Combine(Core.Globals.DataPath.Gui, 33.ToString());
        GameClient.RenderTexture(ref argPath4, (int) (x2 + maxWidth), (int) y, 119, 19, 9, 6, 9, 6);

        // bottom - left half
        string argPath5 = System.IO.Path.Combine(Core.Globals.DataPath.Gui, 33.ToString());
        GameClient.RenderTexture(ref argPath5, (int) x2, (int) y, 9, 19, (int) (maxWidth / 2L - 5L), 6, 6, 6);

        // bottom - right half
        string argPath6 = System.IO.Path.Combine(Core.Globals.DataPath.Gui, 33.ToString());
        GameClient.RenderTexture(ref argPath6, (int) (x2 + maxWidth / 2L + 6L), (int) y, 9, 19, (int) (maxWidth / 2L - 5L), 6,
            9,
            6);

        // left
        string argPath7 = System.IO.Path.Combine(Core.Globals.DataPath.Gui, 33.ToString());
        GameClient.RenderTexture(ref argPath7, (int) (x2 - 9L), (int) y2, 0, 6, 9, (Information.UBound(theArray) + 1) * 12, 9, 6);

        // right
        string argPath8 = System.IO.Path.Combine(Core.Globals.DataPath.Gui, 33.ToString());
        GameClient.RenderTexture(ref argPath8, (int) (x2 + maxWidth), (int) y2, 119, 6, 9, (Information.UBound(theArray) + 1) * 12,
            9,
            6);

        // center
        string argPath9 = System.IO.Path.Combine(Core.Globals.DataPath.Gui, 33.ToString());
        GameClient.RenderTexture(ref argPath9, (int) x2, (int) y2, 9, 5, (int) maxWidth, (Information.UBound(theArray) + 1) * 12, 9,
            5);

        // little pointy bit
        string argPath10 = System.IO.Path.Combine(Core.Globals.DataPath.Gui, 33.ToString());
        GameClient.RenderTexture(ref argPath10, (int) (x - 5L), (int) y, 58, 19, 11, 11, 11, 11);

        // render each line centralized
        tmpNum = Information.UBound(theArray);

        var loopTo1 = tmpNum;
        for (i = 0; i <= loopTo1; i++)
        {
            if (theArray[(int) i] == null)
                continue;

            // Measure button text size and apply padding
            var textSize = TextRenderer.Fonts[Font.Georgia].MeasureString(theArray[(int) i]);
            float actualWidth = textSize.X;
            float actualHeight = textSize.Y;

            // Calculate horizontal and vertical centers with padding
            double padding = (double) actualWidth / 6.0d;

            TextRenderer.OnDraw(theArray[(int) i],
                (int) Math.Round(x - theArray[(int) i].Length / 2d - TextRenderer.GetTextWidth(theArray[(int) i]) / 2d +
                                    padding), (int) y2, GameClient.QbColorToXnaColor(instance.Color),
                Color.Black);
            y2 = y2 + 12L;
        }

        // check if it's timed out - close it if so
        if (instance.Timer + 5000 < General.GetTickCount())
        {
            instance.Active = false;
        }
    }

    public static void OnReset()
    {
        for (int i = 0; i < Data.ChatBubble.Length; i++)
            OnClear(i);
    }

    public static void OnLoad(int index)
    {
        throw new NotImplementedException();
    }

    public static void OnStream(int index)
    {
        throw new NotImplementedException();
    }

    public static void OnSave(int index)
    {
        throw new NotImplementedException();
    }
}