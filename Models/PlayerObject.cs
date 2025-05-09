using Silk.NET.Maths;
using Silk.NET.SDL;
using System;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int _speed = 128;
    private const int TOP_OFFSET = 24;
    private const int SIDE_OFFSET = 12;
    private Rectangle<int> _worldBounds;

    public enum PlayerStateDirection
    {
        None = 0,
        Down,
        Up,
        Left,
        Right,
    }

    public enum PlayerState
    {
        None = 0,
        Idle,
        Move,
        Attack,
        GameOver
    }

    public (PlayerState State, PlayerStateDirection Direction) State { get; private set; }

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        SetState(PlayerState.Idle, PlayerStateDirection.Down);
        _worldBounds = new Rectangle<int>(0, 0, 1000, 1000);
    }

    public void SetWorldBounds(Rectangle<int> bounds)
    {
        _worldBounds = new Rectangle<int>(
            bounds.Origin.X + SIDE_OFFSET,
            bounds.Origin.Y + TOP_OFFSET,
            bounds.Size.X - (SIDE_OFFSET * 2),
            bounds.Size.Y - TOP_OFFSET
        );
        Console.WriteLine($"Player bounds set: X={_worldBounds.Origin.X}, Y={_worldBounds.Origin.Y}, " +
                          $"Width={_worldBounds.Size.X}, Height={_worldBounds.Size.Y}");
    }

    public void SetState(PlayerState state)
    {
        SetState(state, State.Direction);
    }

    public void SetState(PlayerState state, PlayerStateDirection direction)
    {
        if (State.State == PlayerState.GameOver)
        {
            return;
        }

        if (State.State == state && State.Direction == direction)
        {
            return;
        }

        if (state == PlayerState.None && direction == PlayerStateDirection.None)
        {
            SpriteSheet.ActivateAnimation(null);
        }
        else if (state == PlayerState.GameOver)
        {
            SpriteSheet.ActivateAnimation(Enum.GetName(state));
        }
        else
        {
            var animationName = Enum.GetName(state) + Enum.GetName(direction);
            SpriteSheet.ActivateAnimation(animationName);
        }

        State = (state, direction);
    }

    public void GameOver()
    {
        SetState(PlayerState.GameOver, PlayerStateDirection.None);
    }

    public void Attack()
    {
        if (State.State == PlayerState.GameOver)
        {
            return;
        }

        var direction = State.Direction;
        SetState(PlayerState.Attack, direction);
    }

    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        if (State.State == PlayerState.GameOver)
        {
            return;
        }

        var pixelsToMove = _speed * (time / 1000.0);

        var x = Position.X + (int)(right * pixelsToMove);
        x -= (int)(left * pixelsToMove);

        var y = Position.Y + (int)(down * pixelsToMove);
        y -= (int)(up * pixelsToMove);

        x = Math.Clamp(x, _worldBounds.Origin.X, _worldBounds.Origin.X + _worldBounds.Size.X);
        y = Math.Clamp(y, _worldBounds.Origin.Y, _worldBounds.Origin.Y + _worldBounds.Size.Y);

        var newState = State.State;
        var newDirection = State.Direction;

        if (x == Position.X && y == Position.Y)
        {
            if (State.State == PlayerState.Attack)
            {
                if (SpriteSheet.AnimationFinished)
                {
                    newState = PlayerState.Idle;
                }
            }
            else
            {
                newState = PlayerState.Idle;
            }
        }
        else
        {
            newState = PlayerState.Move;

            if (y < Position.Y && newDirection != PlayerStateDirection.Up)
            {
                newDirection = PlayerStateDirection.Up;
            }
            else if (y > Position.Y && newDirection != PlayerStateDirection.Down)
            {
                newDirection = PlayerStateDirection.Down;
            }
            else if (x < Position.X && newDirection != PlayerStateDirection.Left)
            {
                newDirection = PlayerStateDirection.Left;
            }
            else if (x > Position.X && newDirection != PlayerStateDirection.Right)
            {
                newDirection = PlayerStateDirection.Right;
            }
        }

        if (newState != State.State || newDirection != State.Direction)
        {
            SetState(newState, newDirection);
        }

        Position = (x, y);
    }
}