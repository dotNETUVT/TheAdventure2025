using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    public const int _speed = 128; 
    private int _oreCount;


    public enum PlayerStateDirection
    {
        None = 0,
        Down,
        Up,
        Left,
        Right,
        SwimmingUp,
        SwimmingDown,
        SwimmingLeft,
        SwimmingRight,
    }

    public enum PlayerState
    {
        None = 0,
        Idle,
        Move,
        Attack,
        GameOver,
        Swimming
    }

    public (PlayerState State, PlayerStateDirection Direction) State { get; private set; }

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        _oreCount = 0;
        SetState(PlayerState.Idle, PlayerStateDirection.Down);
    }
    
    public void CollectOre()
    {
        _oreCount++;
    }
    
    public int OreCount => _oreCount;



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
            string animationName;
            if (state == PlayerState.Swimming)
            {
                switch (direction)
                {
                    case PlayerStateDirection.SwimmingUp:
                        animationName = "SwimmingUp";
                        break;
                    case PlayerStateDirection.SwimmingDown:
                        animationName = "SwimmingDown";
                        break;
                    case PlayerStateDirection.SwimmingLeft:
                        animationName = "SwimmingLeft";
                        break;
                    case PlayerStateDirection.SwimmingRight:
                        animationName = "SwimmingRight";
                        break;
                    default:
                        animationName = null;
                        break;
                }
            }
            else
            {
                animationName = Enum.GetName(state) + Enum.GetName(direction);
            }

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

    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time,
        bool isSwimming)
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

        var newState = State.State;
        var newDirection = State.Direction;
        
        if (isSwimming)
        {
            newState = PlayerState.Swimming;

            if (y < Position.Y && newDirection != PlayerStateDirection.SwimmingUp)
            {
                newDirection = PlayerStateDirection.SwimmingUp;
            }

            if (y > Position.Y && newDirection != PlayerStateDirection.SwimmingDown)
            {
                newDirection = PlayerStateDirection.SwimmingDown;
            }

            if (x < Position.X && newDirection != PlayerStateDirection.SwimmingLeft)
            {
                newDirection = PlayerStateDirection.SwimmingLeft;
            }

            if (x > Position.X && newDirection != PlayerStateDirection.SwimmingRight)
            {
                newDirection = PlayerStateDirection.SwimmingRight;
            }
        }
        else
        {
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

                if (y > Position.Y && newDirection != PlayerStateDirection.Down)
                {
                    newDirection = PlayerStateDirection.Down;
                }

                if (x < Position.X && newDirection != PlayerStateDirection.Left)
                {
                    newDirection = PlayerStateDirection.Left;
                }

                if (x > Position.X && newDirection != PlayerStateDirection.Right)
                {
                    newDirection = PlayerStateDirection.Right;
                }
            }
        }

        if (newState != State.State || newDirection != State.Direction)
        {
            SetState(newState, newDirection);
        }

        Position = (x, y);
    }
}