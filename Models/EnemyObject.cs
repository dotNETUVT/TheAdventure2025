using Silk.NET.Maths;
using Silk.NET.SDL;
using System;
using System.IO;

namespace TheAdventure.Models;

public class EnemyObject : RenderableGameObject
{
    public int X { get; private set; }
    public int Y { get; private set; }

    private Rectangle<int> _source = new(0, 0, 48, 48);
    private Rectangle<int> _target = new(0, 0, 48, 48);

    private const int Speed = 64;

    private string _currentDirection = "idle";
    private string _lastDirection = "idle";
    
    public DateTimeOffset DeathTime { get; set; } = DateTimeOffset.MinValue;

    public bool IsDead { get; set; } = false;

    public EnemyObject(GameRenderer renderer, int startX, int startY)
        : base(
            spriteSheet: new SpriteSheet(renderer, Path.Combine("Assets", "enemy.png"), 10, 6, 48, 48, (24, 24)),
            position: (startX, startY),
            angle: 0
        )
    {
        X = startX;
        Y = startY;
        renderer.LoadTexture(Path.Combine("Assets", "enemy.png"), out _);
        UpdateTarget();
    }

    public override void Render(GameRenderer renderer)
    {
        base.Render(renderer);
    }

    public void Update(PlayerObject player, int time)
    {
        if (IsDead)
            return;

        var pixelsToMove = Speed * (time / 1000.0);

        double directionX = player.X + 48 - X;
        double directionY = player.Y - 20 - Y;

        double distance = Math.Sqrt(directionX * directionX + directionY * directionY);
        if (distance > 0)
        {
            directionX /= distance;
            directionY /= distance;
        }

        X += (int)Math.Round(directionX * pixelsToMove);
        Y += (int)Math.Round(directionY * pixelsToMove);

        // Update current direction based on movement
        UpdateDirection(directionX, directionY, pixelsToMove > 0);

        Position = (X, Y);

        UpdateTarget();
    }

    private void UpdateDirection(double directionX, double directionY, bool moving)
    {
        if (!moving)
        {
            _currentDirection = "idle";
            return;
        }
        if (Math.Abs(directionX) > Math.Abs(directionY))
        {
            if (directionX > 0)
                _currentDirection = "right";
            else
                _currentDirection = "left";
        }
        else
        {
            if (directionY > 0)
                _currentDirection = "down";
            else
                _currentDirection = "up";
        }

        if(_currentDirection != _lastDirection)
        {
            _lastDirection = _currentDirection;

            UpdateSpriteBasedOnDirection();
        }
    }

    private void UpdateSpriteBasedOnDirection()
    {
        switch (_currentDirection)
        {
            case "right":
                SpriteSheet.ActivateAnimation("WalkRight");
                break;
            case "left":
                SpriteSheet.ActivateAnimation("WalkLeft");
                break;
            case "up":
                SpriteSheet.ActivateAnimation("WalkUp");
                break;
            case "down":
                SpriteSheet.ActivateAnimation("WalkDown");
                break;
            case "idle":
                SpriteSheet.ActivateAnimation("IdleDown");
                break;
        }
    }

    private void UpdateTarget()
    {
        _target = new(X + 24, Y - 42, 48, 48);
    }
}
