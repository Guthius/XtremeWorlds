using Core.Globals;
using Client.Game.UI.Controls;
using Microsoft.Xna.Framework;
using System;
using System.IO;

namespace Client.Game.UI.Windows;

public class WinDescription
{
    public static void OnDraw()
    {
        if (GameState.DescItem == -1 || GameState.DescType == 0)
        {
            return;
        }

        var winDescription = WindowManager.GetWindowByName("winDescription");
        if (winDescription is null)
        {
            return;
        }

        var x = winDescription.X;
        var y = winDescription.Y;

        void DrawBar(string controlName)
        {
            var ctrl = winDescription.GetChild(controlName);
            if (ctrl is not PictureBox bar || !bar.Visible)
                return;

            var argPath1 = Path.Combine(DataPath.Gui, "45");
            var width = Math.Clamp(bar.Value, 0, bar.Width);

            // Bar texture layout: two vertical slices, each 66x12.
            // Top slice (y=0) is the background, bottom slice (y=12) is the filled portion.
            const int barSliceHeight = 12;

            // Background
            GameClient.RenderTexture(ref argPath1,
                x + bar.X,
                y + bar.Y,
                0, 0,
                bar.Width, barSliceHeight,
                bar.Width, barSliceHeight);

            GameClient.RenderTexture(ref argPath1,
                x + bar.X,
                y + bar.Y, 0, 12,
                width, barSliceHeight,
                width, barSliceHeight);
        }

        switch (GameState.DescType)
        {
            case 1: // Item
            {
                if (GameState.DescItem < 0 || GameState.DescItem >= Item.Instance.Count)
                    return;

                Item.OnStream(GameState.DescItem);

                var iconPath = Path.Combine(DataPath.Items, Item.Instance[GameState.DescItem].Icon.ToString());

                GameClient.RenderTexture(ref iconPath, x + 20, y + 34, 0, 0, 64, 64, 32, 32);

                // Durability bar (if enabled by ShowItemDesc)
                DrawBar("picDurability");
                break;
            }

            case 2: // Skill
            {
                // Rank bar (if enabled by ShowSkillDesc)
                DrawBar("picBar");

                // Draw selected skill's icon (not item icon)
                if (GameState.DescItem < 0 || GameState.DescItem >= Skill.Instance.Count)
                    return;

                Skill.OnStream(GameState.DescItem);
                
                int icon = Skill.Instance[GameState.DescItem].Icon;
                if (icon < 1 || icon > GameState.NumSkills)
                    return;

                var path = Path.Combine(DataPath.Skills, icon.ToString());
                GameClient.RenderTexture(ref path, x + 20, y + 34, 0, 0, 64, 64, 32, 32);
                break;
            }
        }

        if (GameState.Description is null)
        {
            return;
        }

        // Prefer aligning description lines to the right panel rather than a hardcoded offset.
        // Use lblJob/lblLevel as the primary anchor (even when hidden), falling back to picSep.
        var textCenterX = x + 140;
        if (WindowManager.TryGetControl("winDescription", "lblJob", out var lblCtrl)
            || WindowManager.TryGetControl("winDescription", "lblLevel", out lblCtrl))
        {
            textCenterX = x + lblCtrl.X + (lblCtrl.Width / 2) - 6;
        }
        else if (WindowManager.TryGetControl("winDescription", "picSep", out var sepCtrl))
        {
            var rightPanelLeft = sepCtrl.X;
            var rightPanelWidth = winDescription.Width - rightPanelLeft;
            textCenterX = x + rightPanelLeft + (rightPanelWidth / 2) - 6;
        }

        // The description buffer typically starts with a couple reserved/blank entries.
        // Start slightly higher so the first real line aligns with the icon.
        var offset = 6;
        for (var i = 0; i < GameState.Description.Length; i++)
        {
            TextRenderer.Render(GameState.Description[i].Caption,
                textCenterX - TextRenderer.GetTextWidth(GameState.Description[i].Caption) / 2,
                y + offset,
                GameClient.ToXnaColor(GameState.Description[i].Color),
                Color.Black, winDescription.Font);

            offset += 14;
        }
    }
}