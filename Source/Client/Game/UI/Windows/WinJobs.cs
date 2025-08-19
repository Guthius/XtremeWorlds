using Core.Globals;
using Microsoft.Xna.Framework;

namespace Client.Game.UI.Windows;

public static class WinJobs
{
    private const int JobWarrior = 0;
    private const int JobWizard = 1;
    private const int JobWhisperer = 2;
    
    public static void OnDrawSprite()
    {
        var winJobs = Gui.GetWindowByName("winJobs");
        if (winJobs is null)
        {
            return;
        }

        int spriteIndex;

        if (Data.Job[GameState.NewCharJob].Name == "")
        {
            spriteIndex = GameState.NewCharJob switch
            {
                JobWarrior => 1,
                JobWizard => 2,
                JobWhisperer => 3,
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

        var w = spriteTexture.Width / 4;
        var h = spriteTexture.Height / 4;

        GameClient.RenderTexture(ref spritePath,
            winJobs.X + 50,
            winJobs.Y + 90,
            0, 0, w, h, w, h);
    }

    public static void OnDrawDescription()
    {
        const int lineHeight = 14;

        var winJobs = Gui.GetWindowByName("winJobs");
        if (winJobs is null)
        {
            return;
        }
        
        var text = "";
        if (Data.Job[GameState.NewCharJob].Desc == "")
        {
            text = GameState.NewCharJob switch
            {
                JobWarrior => "The way of a warrior has never been an easy one. ...",
                JobWizard => "Wizards are often mistrusted characters who ... enjoy setting things on fire.",
                JobWhisperer => "The art of healing comes with pressure and guilt, ...",
                _ => text
            };
        }
        else
        {
            text = Data.Job[GameState.NewCharJob].Desc;
        }
        
        var lines = Array.Empty<string>();
        
        TextRenderer.WordWrap(text, winJobs.Font, 330, ref lines);

        var y = winJobs.Y + 60;

        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            
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
        var winJobs = Gui.GetWindowByName("winJobs");
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
        var winJobs = Gui.GetWindowByName("winJobs");
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
        Gui.HideWindow("winJobs");
        Gui.ShowWindow("winNewChar");

        var winNewChar = Gui.GetWindowByName("winNewChar");
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
        Gui.HideWindows();

        Gui.ShowWindow("winChars");
    }
}