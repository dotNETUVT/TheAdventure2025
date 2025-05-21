using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int WalkSpeed = 128;
    private const int DashDistance = 96;
    private const double DashDuration = 150;
    private const double DashCooldown = 500;
    private const double RegenInterval = 10_000;

    private double _dashTimeRemaining;
    private double _dashCooldownRemaining;
    private bool   _isDashing;
    private Vector2D<int> _dashDir;

    private double _regenTimer;

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

    public int MaxHealth { get; } = 3;
    private int _health;
    public int  Health    => _health;

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        SetState(PlayerState.Idle, PlayerStateDirection.Down);
        _health = MaxHealth;
    }
    
    public void SetState(PlayerState state) => SetState(state, State.Direction);

    public void SetState(PlayerState state, PlayerStateDirection direction)
    {
        if (State.State == PlayerState.GameOver) return;
        if (State.State == state && State.Direction == direction) return;

        string? anim = state switch
        {
            PlayerState.None when direction == PlayerStateDirection.None => null,
            PlayerState.GameOver => nameof(PlayerState.GameOver),
            _ => $"{state}{direction}"
        };
        SpriteSheet.ActivateAnimation(anim);
        State = (state, direction);
    }

    public void GameOver() => SetState(PlayerState.GameOver, PlayerStateDirection.None);

    public void Attack()
    {
        if (State.State == PlayerState.GameOver) return;
        SetState(PlayerState.Attack, State.Direction);
    }
    private static Vector2D<int> DirToVec(PlayerStateDirection dir) => dir switch
    {
        PlayerStateDirection.Up    => new Vector2D<int>(0, -1),
        PlayerStateDirection.Down  => new Vector2D<int>(0,  1),
        PlayerStateDirection.Left  => new Vector2D<int>(-1, 0),
        PlayerStateDirection.Right => new Vector2D<int>( 1, 0),
        _                          => new Vector2D<int>(0,  0),
    };
    
    public void UpdatePosition(double up, double down, double left, double right, bool dashPressed,
                               int width, int height, double elapsedMs)
    {
        if (State.State == PlayerState.GameOver) return;

        if (_health < MaxHealth)
        {
            _regenTimer += elapsedMs;
            if (_regenTimer >= RegenInterval)
            {
                _health = Math.Min(MaxHealth, _health + 1);
                _regenTimer -= RegenInterval;
            }
        }

        if (_dashCooldownRemaining > 0)
            _dashCooldownRemaining = Math.Max(0, _dashCooldownRemaining - elapsedMs);

        if (_isDashing)
        {
            double dashSpeed = DashDistance / DashDuration; // px per ms
            Position = (Position.X + (int)(_dashDir.X * dashSpeed * elapsedMs),
                        Position.Y + (int)(_dashDir.Y * dashSpeed * elapsedMs));

            _dashTimeRemaining -= elapsedMs;
            if (_dashTimeRemaining <= 0)
            {
                _isDashing = false;
                _dashCooldownRemaining = DashCooldown;
                SetState(PlayerState.Idle, State.Direction);
            }
            return;
        }

        if (dashPressed && _dashCooldownRemaining <= 0)
        {
            PlayerStateDirection dashDir =
                up    > 0 ? PlayerStateDirection.Up    :
                down  > 0 ? PlayerStateDirection.Down  :
                left  > 0 ? PlayerStateDirection.Left  :
                right > 0 ? PlayerStateDirection.Right :
                State.Direction;

            _dashDir = DirToVec(dashDir);
            if (_dashDir != default)
            {
                _isDashing          = true;
                _dashTimeRemaining  = DashDuration;
                SetState(PlayerState.Move, dashDir);
                return;
            }
        }

        double movePx = WalkSpeed * (elapsedMs / 1000.0);
        double dx = (right - left) * movePx;
        double dy = (down - up)  * movePx;

        int newX = Position.X + (int)dx;
        int newY = Position.Y + (int)dy;

        var nextState = State.State;
        var nextDir   = State.Direction;

        if (Math.Abs(dx) < 0.01 && Math.Abs(dy) < 0.01)
        {
            if (State.State == PlayerState.Attack && SpriteSheet.AnimationFinished)
                nextState = PlayerState.Idle;
            else if (State.State != PlayerState.Attack)
                nextState = PlayerState.Idle;
        }
        else
        {
            nextState = PlayerState.Move;
            if (Math.Abs(dy) > Math.Abs(dx))
                nextDir = dy < 0 ? PlayerStateDirection.Up : PlayerStateDirection.Down;
            else
                nextDir = dx < 0 ? PlayerStateDirection.Left : PlayerStateDirection.Right;
        }

        if (nextState != State.State || nextDir != State.Direction)
            SetState(nextState, nextDir);

        Position = (newX, newY);
    }


    public void TakeDamage(int amount)
    {
        if (State.State == PlayerState.GameOver) return;

        _health = Math.Max(0, _health - amount);
        _regenTimer = 0;

        if (_health == 0) GameOver();
    }
}
