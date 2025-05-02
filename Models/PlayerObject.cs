using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int _normalSpeed = 128; // pixels per second
    private const int _superSpeed = 256; // double speed
    private string _currentAnimation = "IdleDown";
    public bool IsDead { get; private set; } = false;
    public bool IsImmortal { get; private set; } = false;
    public bool IsSuperSpeed { get; private set; } = false;

    // Calculate current speed based on super speed state
    private int CurrentSpeed => IsSuperSpeed ? _superSpeed : _normalSpeed;

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        SpriteSheet.ActivateAnimation(_currentAnimation);
    }

    public void Die()
    {
        if (IsDead || IsImmortal) return; 

        IsDead = true;
        IsImmortal = false;
        IsSuperSpeed = false;
        _currentAnimation = "Die"; 
        SpriteSheet.ActivateAnimation(_currentAnimation); 
    }
    
    public void Reset(int x, int y)
    {
        IsDead = false;
        IsImmortal = false;
        IsSuperSpeed = false;
        Position = (x, y);
        _currentAnimation = "IdleDown";
        SpriteSheet.ActivateAnimation(_currentAnimation);
    }

    public void ToggleImmortality()
    {
        IsImmortal = !IsImmortal;
    }
    
    public void ToggleSuperSpeed()
    {
        IsSuperSpeed = !IsSuperSpeed;
    }

    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        if (IsDead) 
        {
            return;
        }
        
        if (up + down + left + right == 0) 
        {
            if (_currentAnimation != "IdleDown")
            {
                 _currentAnimation = "IdleDown";
                 SpriteSheet.ActivateAnimation(_currentAnimation);
            }
            return;
        }
        
        // Calculate movement - always allow movement regardless of immortality status
        var pixelsToMove = CurrentSpeed * (time / 1000.0);
        
        var x = Position.X + (int)(right * pixelsToMove);
        x -= (int)(left * pixelsToMove);
        
        var y = Position.Y + (int)(down * pixelsToMove);
        y -= (int)(up * pixelsToMove);
        
        // Update animation based on movement direction
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
        
        // Always update position - no collision checking with bombs
        Position = (x, y);
    }
}