using Core.Configurations;
using Core.Globals;
using System.IO;

namespace Client.Game.UI.Windows;

public class WinNewChar
{
    public static void OnDrawSprite()
    {
        var winNewChar = WindowManager.GetWindowByName("winNewChar");
        if (winNewChar is null)
        {
            return;
        }

        var spriteIndex = GameState.NewCnarGender == Sex.Male ? Job.Instance[GameState.NewCharJob].MaleSprite : Job.Instance[GameState.NewCharJob].FemaleSprite;

        if (spriteIndex < 1 || spriteIndex > GameState.NumCharacters)
            spriteIndex = 1;

        var spritePath = Path.Combine(DataPath.Characters, spriteIndex.ToString());
        var sprite = GameClient.GetGfxInfo(Path.Combine(DataPath.Characters, spriteIndex.ToString()));
        if (sprite is null)
        {
            return;
        }

        var frameCount = SettingsManager.Instance.RunFrames + SettingsManager.Instance.IdleFrames + SettingsManager.Instance.AttackFrames;
        var w = sprite.Width / frameCount;
        var dirs = Math.Max(1, SettingsManager.Instance.SpriteDirections);
        if (sprite.Height % dirs != 0) dirs = 4; // fallback
        var h = sprite.Height / (dirs == 0 ? 1 : dirs);

        GameClient.RenderTexture(ref spritePath,
            winNewChar.X + (w / 2) + 134,
            winNewChar.Y + 80, 0, 0,
            w, h, w, h);
    }

    public static void OnLeftClick()
    {
        var spriteIndex = GameState.NewCnarGender == Sex.Male ? Job.Instance[GameState.NewCharJob].MaleSprite : Job.Instance[GameState.NewCharJob].FemaleSprite;
        if (GameState.NewCharactersprite < 0)
        {
            GameState.NewCharactersprite = spriteIndex;
        }
        else
        {
            GameState.NewCharactersprite -= 1;
        }
    }

    public static void OnRightClick()
    {
        var spriteIndex = GameState.NewCnarGender == Sex.Male
            ? Job.Instance[GameState.NewCharJob].MaleSprite
            : Job.Instance[GameState.NewCharJob].FemaleSprite;

        if (GameState.NewCharactersprite >= spriteIndex)
        {
            GameState.NewCharactersprite = 1;
        }
        else
        {
            GameState.NewCharactersprite += 1;
        }
    }

    public static void OnMaleChecked()
    {
        GameState.NewCharactersprite = 1;
        GameState.NewCnarGender = Sex.Male;

        var winNewChar = WindowManager.GetWindowByName("winNewChar");
        if (winNewChar is null)
        {
            return;
        }

        if (winNewChar.GetChild("chkMale").Value != 0)
        {
            return;
        }

        winNewChar.GetChild("chkFemale").Value = 0;
        winNewChar.GetChild("chkMale").Value = 1;
    }

    public static void OnFemaleChecked()
    {
        GameState.NewCharactersprite = 1;
        GameState.NewCnarGender = Sex.Female;

        var winNewChar = WindowManager.GetWindowByName("winNewChar");
        if (winNewChar is null)
        {
            return;
        }

        if (winNewChar.GetChild("chkFemale").Value != 0)
        {
            return;
        }

        winNewChar.GetChild("chkFemale").Value = 1;
        winNewChar.GetChild("chkMale").Value = 0;
    }

    public static void OnCancel()
    {
        var winNewChar = WindowManager.GetWindowByName("winNewChar");
        if (winNewChar is null)
        {
            return;
        }

        winNewChar.GetChild("txtName").Text = "";
        winNewChar.GetChild("chkMale").Value = 0;
        winNewChar.GetChild("chkFemale").Value = 0;

        GameState.NewCharactersprite = 1;
        GameState.NewCnarGender = Sex.Male;

        WindowManager.HideWindows();
        WindowManager.ShowWindow("winJobs");
    }

    public static void OnAccept()
    {
        var winNewChar = WindowManager.GetWindowByName("winNewChar");
        if (winNewChar is null)
        {
            return;
        }

        var name = winNewChar.GetChild("txtName").Text;

        WindowManager.HideWindows();

        GameLogic.AddChar(name, (int) GameState.NewCnarGender, GameState.NewCharJob, GameState.NewCharactersprite);
    }
}