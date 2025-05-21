using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int _speed = 128; // pixels per second

    private const double _regenInterval = 10_000;
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
    public int Health => _health;

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        SetState(PlayerState.Idle, PlayerStateDirection.Down);
        _health = MaxHealth;
        _regenTimer = 0;
    }

    #region State helpers
    public void SetState(PlayerState state) => SetState(state, State.Direction);

    public void SetState(PlayerState state, PlayerStateDirection direction)
    {
        if (State.State == PlayerState.GameOver)
            return;

        if (State.State == state && State.Direction == direction)
            return;

        if (state == PlayerState.None && direction == PlayerStateDirection.None)
        {
            SpriteSheet.ActivateAnimation(null);
        }
        else if (state == PlayerState.GameOver)
        {
            SpriteSheet.ActivateAnimation(nameof(PlayerState.GameOver));
        }
        else
        {
            var animationName = $"{state}{direction}";
            SpriteSheet.ActivateAnimation(animationName);
        }

        State = (state, direction);
    }

    public void GameOver() => SetState(PlayerState.GameOver, PlayerStateDirection.None);

    public void Attack()
    {
        if (State.State == PlayerState.GameOver)
            return;

        SetState(PlayerState.Attack, State.Direction);
    }
    #endregion

    #region Movement / Update
    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        if (State.State == PlayerState.GameOver)
            return;

        RegenerateHealth(time);

        var pixelsToMove = _speed * (time / 1000.0);

        var x = Position.X + (int)(right * pixelsToMove);
        x -= (int)(left * pixelsToMove);

        var y = Position.Y + (int)(down * pixelsToMove);
        y -= (int)(up * pixelsToMove);

        var newState = State.State;
        var newDirection = State.Direction;

        if (x == Position.X && y == Position.Y)
        {
            // Player hasn't moved
            if (State.State == PlayerState.Attack)
            {
                if (SpriteSheet.AnimationFinished)
                    newState = PlayerState.Idle;
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
                newDirection = PlayerStateDirection.Up;
            else if (y > Position.Y && newDirection != PlayerStateDirection.Down)
                newDirection = PlayerStateDirection.Down;

            if (x < Position.X && newDirection != PlayerStateDirection.Left)
                newDirection = PlayerStateDirection.Left;
            else if (x > Position.X && newDirection != PlayerStateDirection.Right)
                newDirection = PlayerStateDirection.Right;
        }

        if (newState != State.State || newDirection != State.Direction)
            SetState(newState, newDirection);

        Position = (x, y);
    }
    #endregion

    #region Health / Damage / Regeneration
    public void TakeDamage(int amount)
    {
        if (State.State == PlayerState.GameOver)
            return;

        _health = Math.Max(0, _health - amount);
        _regenTimer = 0;

        if (_health == 0)
            GameOver();
    }

    private void RegenerateHealth(double elapsedMs)
    {
        if (_health >= MaxHealth)
        {
            _regenTimer = 0;
            return;
        }

        _regenTimer += elapsedMs;

        if (_regenTimer >= _regenInterval)
        {
            var heartsToAdd = (int)(_regenTimer / _regenInterval);
            _health = Math.Min(MaxHealth, _health + heartsToAdd);
            _regenTimer -= heartsToAdd * _regenInterval;
        }
    }
    #endregion
}