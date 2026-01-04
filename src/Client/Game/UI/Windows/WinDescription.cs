using Core.Globals;
using Microsoft.Xna.Framework;
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

        switch (GameState.DescType)
        {
            case 1: // Inventory Item
            {
                var iconPath = Path.Combine(DataPath.Items, Item.Instance[GameState.DescItem].Icon.ToString());

                GameClient.RenderTexture(ref iconPath, x + 20, y + 34, 0, 0, 64, 64, 32, 32);

                break;
            }

            case 2: // Skill Icon
            {
                var picBar = winDescription.GetChild("picBar");
                if (picBar.Visible)
                {
                    var argPath1 = Path.Combine(DataPath.Gui, "45");

                    GameClient.RenderTexture(ref argPath1,
                        x + picBar.X,
                        y + picBar.Y, 0, 12,
                        picBar.Value, 12,
                        picBar.Value, 12);
                }

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

        var offset = 18;
        for (var i = 0; i < GameState.Description.Length; i++)
        {
            TextRenderer.Render(GameState.Description[i].Caption,
                x + 140 - TextRenderer.GetTextWidth(GameState.Description[i].Caption) / 2,
                y + offset,
                GameClient.ToXnaColor(GameState.Description[i].Color),
                Color.Black, winDescription.Font);

            offset += 12;
        }
    }
}