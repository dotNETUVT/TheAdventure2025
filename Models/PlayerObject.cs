using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int _speed = 128; 
    private string _currentAnimation = "IdleDown";
    public bool IsAlive { get; private set; } = true;

    public int PowerLevel { get; private set; } = 0;

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        SpriteSheet.ActivateAnimation(_currentAnimation);
    }

    public void Die()
    {
        IsAlive = false;
        _currentAnimation = "Death";
        SpriteSheet.ActivateAnimation(_currentAnimation);
    }

    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        if (!IsAlive || up + down + left + right == 0)
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
            _currentAnimation = newAnimation;
            SpriteSheet.ActivateAnimation(_currentAnimation);
        }
        
        Position = (x, y);
    }

    public void AddPower(int amount)
    {
        PowerLevel += amount;
    }
}