using Core.Configurations;
using Core.Globals;

namespace Client.Game.UI.Windows;

public static class WinOptions
{
    // Apply a 0-based resolution index immediately (update settings and graphics)
    public static void ApplyResolutionSelection(int selIndex)
    {
        if (selIndex < 0 || selIndex >= 13) // we currently populate 13 entries in cmbRes
            return;

        byte newRes = (byte)(selIndex + 1); // stored setting is 1-based
        if (SettingsManager.Instance.Resolution == newRes)
            return;

        SettingsManager.Instance.Resolution = newRes;
        try { SettingsManager.Save(); } catch { }

        try
        {
            if (GameClient.Graphics != null)
            {
                if (GameClient.Graphics.IsFullScreen)
                {
                    var size = General.GetResolutionSize(newRes);
                    GameClient.Graphics.PreferredBackBufferWidth = size.Width;
                    GameClient.Graphics.PreferredBackBufferHeight = size.Height;
                    GameClient.Graphics.ApplyChanges();
                }
                // In windowed mode we render to a RenderTarget sized by Settings.Resolution; no backbuffer change needed
            }
        }
        catch { /* ignore apply errors */ }
    }

    public static void OnConfirm()
    {
        var restartRequired = false;

        var winOptions = WindowManager.GetWindowByName("winOptions");
        if (winOptions is null)
        {
            return;
        }

        var checkBoxMusic = winOptions.GetChild("chkMusic");
        var checkBoxSound = winOptions.GetChild("chkSound");
        var checkBoxAutoTile = winOptions.GetChild("chkAutotile");
        var checkBoxFullscreen = winOptions.GetChild("chkFullscreen");
        var checkBoxVsync = winOptions.GetChild("chkVsync");
        var comboBoxResolution = winOptions.GetChild("cmbRes");

        // Music
        var enabled = checkBoxMusic.Value != 0;
        if (SettingsManager.Instance.Music != enabled)
        {
            SettingsManager.Instance.Music = enabled;

            if (!enabled)
            {
                TextRenderer.AddText("Music turned off.", (int) ColorName.BrightGreen);

                Sound.StopMusic();
            }
            else
            {
                TextRenderer.AddText("Music tured on.", (int) ColorName.BrightGreen);

                var music = GameState.InGame ? Data.MyMap.Music : SettingsManager.Instance.Music.ToString();
                if (music != "None.")
                {
                    Sound.PlayMusic(music);
                }
                else
                {
                    Sound.StopMusic();
                }
            }
        }

        // Sound
        enabled = checkBoxSound.Value != 0;
        if (SettingsManager.Instance.Sound != enabled)
        {
            SettingsManager.Instance.Sound = enabled;

            TextRenderer.AddText(!enabled ? "Sound turned off." : "Sound tured on.", (int) ColorName.BrightGreen);
        }


        // autotiles
        enabled = checkBoxAutoTile.Value != 0;
        if (SettingsManager.Instance.Autotile != enabled)
        {
            SettingsManager.Instance.Autotile = enabled;
            if (!enabled)
            {
                if (GameState.InGame)
                {
                    TextRenderer.AddText("Autotiles turned off.", (int) ColorName.BrightGreen);
                    Autotile.InitAutotiles();
                }
            }
            else if (GameState.InGame)
            {
                TextRenderer.AddText("Autotiles turned on.", (int) ColorName.BrightGreen);
                Autotile.InitAutotiles();
            }
        }


        // Fullscreen
        enabled = checkBoxFullscreen.Value != 0;
        if (SettingsManager.Instance.Fullscreen != enabled)
        {
            SettingsManager.Instance.Fullscreen = enabled;

            restartRequired = true;
        }

        // VSync
        enabled = checkBoxVsync.Value != 0;
        if (SettingsManager.Instance.Vsync != enabled)
        {
            SettingsManager.Instance.Vsync = enabled;
            restartRequired = true;
        }

        // Resolution
        // Resolution (combobox is 0-based; stored value is 1-based). Apply immediately.
        ApplyResolutionSelection(comboBoxResolution.Value);

        SettingsManager.Save();

        if (GameState.InGame && restartRequired)
        {
            TextRenderer.AddText("Some changes will take effect next time you load the game.", (int) ColorName.BrightGreen);
        }

        OnClose();
    }

    public static void OnClose()
    {
        WindowManager.HideWindow("winOptions");
        WindowManager.ShowWindow("winEscMenu");
    }
}