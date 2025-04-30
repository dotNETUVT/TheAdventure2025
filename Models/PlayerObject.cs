using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int _speed = 128; // pixels per second
    private string _currentAnimation = "IdleDown";

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        SpriteSheet.ActivateAnimation(_currentAnimation);
    }

    public void UpdatePosition(double up, double down, double left, double right, bool isSpacePressed, int width, int height, double time)
    {
        if (isSpacePressed)
        {
            string attackAnimation = _currentAnimation switch
            {
                "MoveUp" or "IdleUp" => "AtackUp",
                "MoveDown" or "IdleDown" => "AtackDown",
                "MoveLeft" or "IdleLeft" => "AtackLeft",
                "MoveRight" or "IdleRight" => "AtackRight",
                _ => _currentAnimation
            };

            if (_currentAnimation != attackAnimation)
            {
                _currentAnimation = attackAnimation;
                SpriteSheet.ActivateAnimation(_currentAnimation);
            }

            return;
        }

        if (_currentAnimation.StartsWith("Atack") && SpriteSheet.ActiveAnimation != null && !SpriteSheet.ActiveAnimation.Loop)
        {
            if ((DateTimeOffset.Now - SpriteSheet.AnimationStart).TotalMilliseconds >= SpriteSheet.ActiveAnimation.DurationMs)
            {
                _currentAnimation = _currentAnimation switch
                {
                    "AtackUp" => "IdleUp",
                    "AtackDown" => "IdleDown",
                    "AtackLeft" => "IdleLeft",
                    "AtackRight" => "IdleRight",
                    _ => _currentAnimation
                };
                SpriteSheet.ActivateAnimation(_currentAnimation);
            }
        }

        if (up + down + left + right == 0)
        {
            if (_currentAnimation == "MoveUp")
            {
                _currentAnimation = "IdleUp";
                SpriteSheet.ActivateAnimation(_currentAnimation);
            }
            else if (_currentAnimation == "MoveDown")
            {
                _currentAnimation = "IdleDown";
                SpriteSheet.ActivateAnimation(_currentAnimation);
            }
            else if (_currentAnimation == "MoveLeft")
            {
                _currentAnimation = "IdleLeft";
                SpriteSheet.ActivateAnimation(_currentAnimation);
            }
            else if (_currentAnimation == "MoveRight")
            {
                _currentAnimation = "IdleRight";
                SpriteSheet.ActivateAnimation(_currentAnimation);
            }

            return;
        }

        var pixelsToMove = _speed * (time / 1000.0);

        var x = Position.X + (int)(right * pixelsToMove);
        x -= (int)(left * pixelsToMove);

        var y = Position.Y + (int)(down * pixelsToMove);
        y -= (int)(up * pixelsToMove);

        var newAnimation = _currentAnimation;

        if (y < Position.Y && _currentAnimation != "MoveUp")
        {
            newAnimation = "MoveUp";
        }

        if (y > Position.Y && newAnimation != "MoveDown")
        {
            newAnimation = "MoveDown";
        }

        if (x < Position.X && newAnimation != "MoveLeft")
        {
            newAnimation = "MoveLeft";
        }

        if (x > Position.X && newAnimation != "MoveRight")
        {
            newAnimation = "MoveRight";
        }

        if (newAnimation != _currentAnimation)
        {
            _currentAnimation = newAnimation;
            SpriteSheet.ActivateAnimation(_currentAnimation);
        }

        Position = (x, y);
    }
}