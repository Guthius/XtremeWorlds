using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Client.Game.UI;

public sealed class UIComponent(Microsoft.Xna.Framework.Game game) : GameComponent(game)
{
    private MouseState _currentMouseState;
    private MouseState _previousMouseState;

    public override void Update(GameTime gameTime)
    {
        CheckForMouseEvents();
    }

    private void CheckForMouseEvents()
    {
        _previousMouseState = _currentMouseState;
        _currentMouseState = Mouse.GetState();

        var mouseX = _currentMouseState.X;
        var mouseY = _currentMouseState.Y;
        
        if (_currentMouseState.LeftButton != _previousMouseState.LeftButton)
        {
            switch (_currentMouseState.LeftButton)
            {
                case ButtonState.Pressed:
                    OnLeftMousePressed(mouseX, mouseY);
                    break;
                
                case ButtonState.Released:
                    OnLeftMouseReleased(mouseX, mouseY);
                    break;
            }
        }

        if (mouseX != _previousMouseState.X || mouseY != _previousMouseState.Y)
        {
            OnMouseMoved(mouseX, mouseY);
        }
    }

    private static void OnMouseMoved(int currentX, int currentY)
    {
        Gui.OnMouseMoved(currentX, currentY);
    }

    private static void OnLeftMousePressed(int x, int y)
    {
        Gui.OnMousePressed(x, y);
    }

    private static void OnLeftMouseReleased(int x, int y)
    {
        Gui.OnMouseReleased(x, y);
    }
}