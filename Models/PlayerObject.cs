using Silk.NET.Maths;
using System.Numerics;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int _speed = 128; // pixels per second
    private string _currentAnimation = "IdleDown";
    private string _facing = "Down";

    private double _attackTimer = 0;
    private const double AttackDuration = 0.4; // in seconds

    public string Facing => _facing;

    public bool IsAttacking => _attackTimer > 0;

    public Vector2 FacingDirection
    {
        get
        {
            return _facing switch
            {
                "Up" => new Vector2(0, -1),
                "Down" => new Vector2(0, 1),
                "Left" => new Vector2(-1, 0),
                "Right" => new Vector2(1, 0),
                _ => new Vector2(0, 1)
            };
        }
    }

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        SpriteSheet.ActivateAnimation(_currentAnimation);
    }

    public void Attack()
    {
        string attackAnim = "Attack" + _facing;
        _attackTimer = AttackDuration;

        if (_currentAnimation != attackAnim)
        {
            _currentAnimation = attackAnim;
            SpriteSheet.ActivateAnimation(_currentAnimation);
        }
    }

    public void Update(double deltaTime)
    {
        // Decrease the attack timer
        if (_attackTimer > 0)
        {
            _attackTimer -= deltaTime;
            if (_attackTimer <= 0)
            {
                // Reset to idle animation once attack ends
                string idleAnim = "Idle" + _facing;
                _currentAnimation = idleAnim;
                SpriteSheet.ActivateAnimation(_currentAnimation);
            }
        }
    }

    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        if (IsAttacking)
        {
            // Skip movement while attacking to prevent animation interruption
            return;
        }

        if (up + down + left + right == 0)
        {
            return;
        }

        var pixelsToMove = _speed * (time / 1000.0);

        var x = Position.X + (int)(right * pixelsToMove);
        x -= (int)(left * pixelsToMove);

        var y = Position.Y + (int)(down * pixelsToMove);
        y -= (int)(up * pixelsToMove);

        string newAnimation = _currentAnimation;

        if (y < Position.Y)
        {
            newAnimation = "MoveUp";
        }
        else if (y > Position.Y)
        {
            newAnimation = "MoveDown";
        }
        else if (x < Position.X)
        {
            newAnimation = "MoveLeft";
        }
        else if (x > Position.X)
        {
            newAnimation = "MoveRight";
        }
        else
        {
            newAnimation = "Idle" + _facing;
        }

        if (newAnimation != _currentAnimation)
        {
            if (newAnimation.StartsWith("Move"))
            {
                _facing = newAnimation.Substring(4);
            }

            _currentAnimation = newAnimation;
            SpriteSheet.ActivateAnimation(_currentAnimation);
        }

        Position = (x, y);
    }
}
