using Silk.NET.Maths;
using Silk.NET.SDL;
using System;
using System.IO;

namespace TheAdventure.Models;

public class PlayerObject : GameObject
{
    public int X { get; private set; } = 100;
    public int Y { get; private set; } = 100;
    public bool IsDead { get; private set; } = false;

    private const int Speed = 128;
    private Direction _currentDirection = Direction.Down;
    private readonly SpriteSheet _spriteSheet;

    public Direction CurrentDirection => _currentDirection;

    public PlayerObject(GameRenderer renderer) : base((100, 100))
    {
        _spriteSheet = new SpriteSheet(renderer, Path.Combine("Assets", "player.png"), 6, 12, 48, 48, (24, 42));

        _spriteSheet.Animations["WalkDown"] = new SpriteSheet.Animation { StartFrame = new SpriteSheet.Position { Row = 0, Col = 0 }, EndFrame = new SpriteSheet.Position { Row = 0, Col = 3 }, DurationMs = 400, Loop = true };
        _spriteSheet.Animations["WalkRight"] = new SpriteSheet.Animation { StartFrame = new SpriteSheet.Position { Row = 1, Col = 0 }, EndFrame = new SpriteSheet.Position { Row = 1, Col = 3 }, DurationMs = 400, Loop = true };
        _spriteSheet.Animations["WalkLeft"] = new SpriteSheet.Animation { StartFrame = new SpriteSheet.Position { Row = 1, Col = 0 }, EndFrame = new SpriteSheet.Position { Row = 1, Col = 3 }, DurationMs = 400, Loop = true, Flip = RendererFlip.Horizontal };
        _spriteSheet.Animations["WalkUp"] = new SpriteSheet.Animation { StartFrame = new SpriteSheet.Position { Row = 2, Col = 0 }, EndFrame = new SpriteSheet.Position { Row = 2, Col = 3 }, DurationMs = 400, Loop = true };

        _spriteSheet.Animations["IdleDown"] = new SpriteSheet.Animation { StartFrame = new SpriteSheet.Position { Row = 2, Col = 0 }, EndFrame = new SpriteSheet.Position { Row = 2, Col = 0 }, DurationMs = 1000, Loop = true };
        _spriteSheet.Animations["IdleRight"] = new SpriteSheet.Animation { StartFrame = new SpriteSheet.Position { Row = 1, Col = 0 }, EndFrame = new SpriteSheet.Position { Row = 1, Col = 0 }, DurationMs = 1000, Loop = true };
        _spriteSheet.Animations["IdleLeft"] = new SpriteSheet.Animation { StartFrame = new SpriteSheet.Position { Row = 1, Col = 0 }, EndFrame = new SpriteSheet.Position { Row = 1, Col = 0 }, DurationMs = 1000, Loop = true, Flip = RendererFlip.Horizontal };
        _spriteSheet.Animations["IdleUp"] = new SpriteSheet.Animation { StartFrame = new SpriteSheet.Position { Row = 0, Col = 0 }, EndFrame = new SpriteSheet.Position { Row = 0, Col = 0 }, DurationMs = 1000, Loop = true };

        _spriteSheet.Animations["Death"] = new SpriteSheet.Animation
        {
            StartFrame = new SpriteSheet.Position { Row = 9, Col = 0 },
            EndFrame = new SpriteSheet.Position { Row = 9, Col = 3 },
            DurationMs = 2000,
            Loop = false
        };

        _spriteSheet.ActivateAnimation("IdleDown");
    }

    public void UpdatePosition(double up, double down, double left, double right, int time)
    {
        if (IsDead) return;

        var moved = false;
        var pixels = Speed * (time / 1000.0);

        if (up > 0) { Y -= (int)pixels; _currentDirection = Direction.Up; moved = true; }
        else if (down > 0) { Y += (int)pixels; _currentDirection = Direction.Down; moved = true; }

        if (left > 0) { X -= (int)pixels; _currentDirection = Direction.Left; moved = true; }
        else if (right > 0) { X += (int)pixels; _currentDirection = Direction.Right; moved = true; }

        var anim = moved ? $"Walk{_currentDirection}" : $"Idle{_currentDirection}";
        _spriteSheet.ActivateAnimation(anim);
    }

    public void Die()
    {
        IsDead = true;
        _spriteSheet.ActivateAnimation("Death");
    }

    public void Render(GameRenderer renderer)
    {
        _spriteSheet.Render(renderer, (X, Y));
        if (!IsDead)
        {
            renderer.SetDrawColor(255, 0, 0, 255);
        }
    }
}
