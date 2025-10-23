using Client.Net;
using Core.Configurations;
using Core.Globals;
using System.IO;

namespace Client.Game.UI.Windows;

public static class WinChars
{
    public static void OnSelectCharacter1Click()
    {
        Sender.SendUseChar(1);
    }

    public static void OnSelectCharacter2Click()
    {
        Sender.SendUseChar(2);
    }

    public static void OnSelectCharacter3Click()
    {
        Sender.SendUseChar(3);
    }

    private static void TryDeleteCharacter(int slot)
    {
        GameLogic.Dialogue(
            "Delete Character",
            "Deleting this character is permanent.",
            "Delete this character?",
            DialogueType.DeleteCharacter,
            DialogueStyle.YesNo,
            slot);
    }

    public static void OnDeleteCharacter1Click()
    {
        TryDeleteCharacter(1);
    }

    public static void OnDeleteCharacter2Click()
    {
        TryDeleteCharacter(2);
    }

    public static void OnDeleteCharacter3Click()
    {
        TryDeleteCharacter(3);
    }

    private static void TryCreateCharacter(int slot)
    {
        GameState.CharNum = (byte) slot;
        GameLogic.ShowJobs();
    }

    public static void OnCreateCharacter1Click()
    {
        TryCreateCharacter(1);
    }

    public static void OnCreateCharacter2Click()
    {
        TryCreateCharacter(2);
    }

    public static void OnCreateCharacter3Click()
    {
        TryCreateCharacter(3);
    }

    public static void OnClose()
    {
        WindowManager.HideWindows();
        WindowManager.ShowWindow("winLogin");
    }
    
    public static void OnDraw()
    {
        var winChars = WindowManager.GetWindowByName("winChars");
        if (winChars is null)
        {
            return;
        }

        for (var i = 0; i < Variables.MaxChars; i++)
        {
            if (string.IsNullOrEmpty(GameState.CharName[i]))
            {
                continue;
            }

            if (GameState.CharSprite[i] <= 0)
            {
                continue;
            }

            var spritePath = Path.Combine(DataPath.Characters, GameState.CharSprite[i].ToString());
            var sprite = GameClient.GetGfxInfo(spritePath);
            if (sprite is null)
            {
                continue;
            }

            var x = winChars.X + 24 + (i * 110);
            var y = winChars.Y + 90;

            var frameCount = SettingsManager.Instance.RunFrames + SettingsManager.Instance.IdleFrames + SettingsManager.Instance.AttackFrames;
            var w = sprite.Width / frameCount;
            var dirs = Math.Max(1, SettingsManager.Instance.SpriteDirections);
            if (sprite.Height % dirs != 0) dirs = 4; // fallback legacy
            var h = sprite.Height / (dirs == 0 ? 1 : dirs);

            if (GameState.CharSprite[i] <= GameState.NumCharacters)
            {
                GameClient.RenderTexture(ref spritePath, x, y, 0, 0, w, h, w, h);
            }

            // Draw equipment overlays (paperdolls) after base sprite
            Equipment[] eqOrder = new[] { Equipment.Armor, Equipment.Helmet, Equipment.Shield, Equipment.Weapon };
            foreach (var eq in eqOrder)
            {
                var itemIndex = GameState.CharEq[i, (byte)eq];
                if (itemIndex <= 0)
                {
                    continue;
                }

                var dollPath = Path.Combine(DataPath.Paperdolls, itemIndex.ToString());
                var doll = GameClient.GetGfxInfo(dollPath);
                if (doll is null)
                {
                    continue;
                }
                
                var sourceW = doll.Width / frameCount;
                var dirs2 = Math.Max(1, SettingsManager.Instance.SpriteDirections);
                if (doll.Height % dirs2 != 0) dirs2 = 4;
                var sourceH = doll.Height / (dirs2 == 0 ? 1 : dirs2);

                GameClient.RenderTexture(ref dollPath, x, y, 0, 0, w, h, sourceW, sourceH);
            }
        }
    
    }
}