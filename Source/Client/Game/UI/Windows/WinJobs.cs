using Core.Configurations;
using Core.Globals;
using Microsoft.Xna.Framework;
using System.IO;

namespace Client.Game.UI.Windows;

public static class WinJobs
{
    public static void OnDrawSprite()
    {
        var winJobs = WindowManager.GetWindowByName("winJobs");
        if (winJobs is null)
        {
            return;
        }

        int spriteIndex;

        if (Data.Job[GameState.NewCharJob].Name == "")
        {
            spriteIndex = GameState.NewCharJob switch
            {
                0 => 1, // Warrior
                1 => 2, // Wizard
                2 => 3, // Whisperer
                _ => 0
            };
        }
        else
        {
            spriteIndex = winJobs.GetChild("chkMale").Value == 1 
                ? Data.Job[GameState.NewCharJob].MaleSprite 
                : Data.Job[GameState.NewCharJob].FemaleSprite;
        }


        var spritePath = Path.Combine(DataPath.Characters, spriteIndex.ToString());
        var spriteTexture = GameClient.GetGfxInfo(spritePath);
        if (spriteTexture is null)
        {
            return;
        }

        // Determine dynamic direction rows (supports configured -> 8 -> 4 -> 1 fallback)
        int configuredDirs = SettingsManager.Instance.SpriteDirections <= 0 ? 4 : SettingsManager.Instance.SpriteDirections;
        configuredDirs = Math.Max(1, configuredDirs);
        int directionRows;
        if (spriteTexture.Height % configuredDirs == 0) directionRows = configuredDirs;
        else if (configuredDirs != 8 && spriteTexture.Height % 8 == 0) directionRows = 8;
        else if (configuredDirs != 4 && spriteTexture.Height % 4 == 0) directionRows = 4;
        else directionRows = 1;

        int frameHeight = Math.Max(1, spriteTexture.Height / directionRows);

        // Segment logic: treat width as Idle+Run+Attack segments if divisible by the sum of frames.
        int idleFrames = Math.Max(1, SettingsManager.Instance.IdleFrames);
        int runFrames = Math.Max(1, SettingsManager.Instance.RunFrames);
        int attackFrames = Math.Max(1, SettingsManager.Instance.AttackFrames);
        int expectedCols = idleFrames + runFrames + attackFrames;
        bool segmented = expectedCols > 0 && spriteTexture.Width % expectedCols == 0;
        int frameWidth;
        if (segmented)
        {
            frameWidth = spriteTexture.Width / expectedCols; // show first idle frame (col 0)
        }
        else
        {
            // Legacy heuristic: assume square frames using frameHeight if that divides evenly
            if (frameHeight > 0 && spriteTexture.Width % frameHeight == 0)
            {
                int approxCols = spriteTexture.Width / frameHeight;
                if (approxCols > 0) frameWidth = spriteTexture.Width / approxCols; else frameWidth = spriteTexture.Width;
            }
            else
            {
                // Fallback: single frame across whole width
                frameWidth = spriteTexture.Width;
            }
        }

    // Center sprite in gap between window left and description background (picBackground at 127,55 size 210x124)
    int windowLeft = winJobs.X; // window origin
    int gapLeft = windowLeft + 6; // parchment left
    int gapRight = winJobs.X + 127; // start of description background
    int gapWidth = gapRight - gapLeft; // width of free area
    int parchmentTop = winJobs.Y + 26;
    int parchmentBottom = parchmentTop + 197; // parchment height
    // Horizontal center within gap
    int destX = gapLeft + (gapWidth - frameWidth) / 2;
    // Vertical placement: raise sprite by using a smaller baseline padding
    int baselineY = parchmentBottom - 50; // baseline 50px above bottom of parchment
    int destY = baselineY - frameHeight;
    GameClient.RenderTexture(ref spritePath, destX, destY, 0, 0, frameWidth, frameHeight, frameWidth, frameHeight);
    }

    public static void OnDrawDescription()
    {
        const int lineHeight = 12;

        var winJobs = WindowManager.GetWindowByName("winJobs");
        if (winJobs is null)
        {
            return;
        }

        var lines = default(string[]);
        var text = "";

        // Get job description or use default
        if (Data.Job[GameState.NewCharJob].Desc == "")
        {
            switch (GameState.NewCharJob)
            {
                case 0: // Warrior
                    {
                        text = "The way of a warrior has never been an easy one. ...";
                        break;
                    }
                case 1: // Wizard
                    {
                        text = "Wizards are often mistrusted characters who ... enjoy setting things on fire.";
                        break;
                    }
                case 2: // Whisperer
                    {
                        text = "The art of healing comes with pressure and guilt, ...";
                        break;
                    }
            }
        }
        else
        {
            text = Data.Job[GameState.NewCharJob].Desc;
        }

        TextRenderer.WordWrap(text, winJobs.Font, 330, ref lines);

        var y = winJobs.Y + 60;

        foreach (var line in lines)
        {
            if (line == "") continue;
            
            var x = winJobs.X + 118 + 200 / 2 - TextRenderer.GetTextWidth(line, winJobs.Font) / 2;

            var textClean = new string(line.Where(c => TextRenderer.Fonts[winJobs.Font].Characters.Contains(c)).ToArray());
            var textSize = TextRenderer.Fonts[winJobs.Font].MeasureString(textClean);

            var padding = (int) (textSize.X / 6);

            TextRenderer.RenderText(line, x + padding, y, Color.White, Color.Black);

            y += lineHeight;
        }
    }

    public static void OnLeftClick()
    {
        var winJobs = WindowManager.GetWindowByName("winJobs");
        if (winJobs is null)
        {
            return;
        }

        GameState.NewCharJob -= 1;
        if (GameState.NewCharJob < 0)
        {
            GameState.NewCharJob = 0;
        }

        winJobs.GetChild("lblJobName").Text = Data.Job[GameState.NewCharJob].Name;
    }

    public static void OnRightClick()
    {
        var winJobs = WindowManager.GetWindowByName("winJobs");
        if (winJobs is null)
        {
            return;
        }

        if (GameState.NewCharJob >= Constant.MaxJobs - 1 || string.IsNullOrEmpty(Data.Job[GameState.NewCharJob].Desc) & GameState.NewCharJob >= Constant.MaxJobs)
        {
            return;
        }

        GameState.NewCharJob += 1;

        winJobs.GetChild("lblJobName").Text = Data.Job[GameState.NewCharJob].Name;
    }

    public static void OnAccept()
    {
        WindowManager.HideWindow("winJobs");
        WindowManager.ShowWindow("winNewChar");

        var winNewChar = WindowManager.GetWindowByName("winNewChar");
        if (winNewChar is null)
        {
            return;
        }

        winNewChar.GetChild("txtName").Text = "";
        winNewChar.GetChild("chkMale").Value = 1;
        winNewChar.GetChild("chkFemale").Value = 0;
    }

    public static void OnClose()
    {
        WindowManager.HideWindows();

        WindowManager.ShowWindow("winChars");
    }
}