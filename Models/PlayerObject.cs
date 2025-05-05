using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int _speed = 128; // pixels per second
    private string _currentAnimation = "IdleDown";
    private string _facing = "Down";

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        SpriteSheet.ActivateAnimation(_currentAnimation);
    }
    public void Attack()
    {
        string attackAnim = "Attack" + _facing;

        if (attackAnim == _currentAnimation) return;

        _currentAnimation = attackAnim;
        SpriteSheet.ActivateAnimation(_currentAnimation);
    }

    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        if (up + down + left + right == 0)
        {
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

        if (x == Position.X && y == Position.Y && newAnimation != "IdleDown")
        {
            newAnimation = "IdleDown";
        }

        if (newAnimation != _currentAnimation)
        {
            if (newAnimation.StartsWith("Move"))
            {
                //only update if it's a movement animation
                //othwerwise the latest animation will always be idle and the attack animation will be AttackDown regardless of direction
                _facing = newAnimation.Substring(4);
            }

            _currentAnimation = newAnimation;
            SpriteSheet.ActivateAnimation(_currentAnimation);
        }

        Position = (x, y);
    }
}
