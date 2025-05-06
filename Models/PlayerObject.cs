using Silk.NET.Maths;

namespace TheAdventure.Models;
using System; // For Math.Abs
using Silk.NET.Maths; // For Vector2D<float>

public class PlayerObject : RenderableGameObject
{
    private const int _speed = 128; // pixels per second
    private string _currentAnimation = "IdleDown";

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        SpriteSheet.ActivateAnimation(_currentAnimation);
    }

    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        if (up + down + left + right == 0)
        {
            return;
        }
        
        var pixelsToMove = _speed * (time / 1000.0);
        
        // Use floating-point calculations
        var newX = Position.X + (float)(right * pixelsToMove);
        newX -= (float)(left * pixelsToMove);
        
        var newY = Position.Y + (float)(down * pixelsToMove);
        newY -= (float)(up * pixelsToMove);
        
        var newAnimation = _currentAnimation;

        // Compare floats directly
        if (newY < Position.Y && _currentAnimation != "MoveUp")
        {
            newAnimation = "MoveUp";
        }
        
        if (newY > Position.Y && newAnimation != "MoveDown")
        {
            newAnimation = "MoveDown";
        }
        
        if (newX < Position.X && newAnimation != "MoveLeft")
        {
            newAnimation = "MoveLeft";
        }
        
        if (newX > Position.X && newAnimation != "MoveRight")
        {
            newAnimation = "MoveRight";
        }
        
        // Use tolerance for floating-point comparison
        if (Math.Abs(newX - Position.X) < 0.01 && 
            Math.Abs(newY - Position.Y) < 0.01 && 
            newAnimation != "IdleDown")
        {
            newAnimation = "IdleDown";
        }

        if (newAnimation != _currentAnimation)
        {
            _currentAnimation = newAnimation;
            SpriteSheet.ActivateAnimation(_currentAnimation);
        }
        
        // Update position with Vector2D<float>
        Position = new Vector2D<float>(newX, newY);
    }
}