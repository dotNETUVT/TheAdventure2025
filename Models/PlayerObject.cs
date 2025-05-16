using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int _speed = 128; // pixels per second
    private const int _maxHealth = 100;
    private const float _gravity = 980.0f; // pixels per second squared
    private const float _jumpForce = -450.0f; // negative because Y grows downward
    private const float _maxFallSpeed = 500.0f;

    public int Health { get; private set; }
    public bool IsInvulnerable { get; private set; }
    private DateTimeOffset _lastHitTime;
    private const double _invulnerabilityDuration = 1.5; // seconds

    private float _verticalVelocity = 0;
    private bool _isGrounded = true;
    private int _jumpsRemaining = 2;

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
        Health = _maxHealth;
        _lastHitTime = DateTimeOffset.MinValue;
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

    public void TakeDamage(int amount)
    {
        if (State.State == PlayerState.GameOver || IsInvulnerable)
        {
            return;
        }

        Health = Math.Max(0, Health - amount);
        IsInvulnerable = true;
        _lastHitTime = DateTimeOffset.Now;

        if (Health <= 0)
        {
            GameOver();
        }
    }

    public void UpdateInvulnerability()
    {
        if (IsInvulnerable && (DateTimeOffset.Now - _lastHitTime).TotalSeconds >= _invulnerabilityDuration)
        {
            IsInvulnerable = false;
        }
    }

    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double deltaTimeMs)
    {
        UpdateInvulnerability();

        if (State.State == PlayerState.GameOver)
        {
            return;
        }

        var deltaTime = deltaTimeMs / 1000.0; // Convert to seconds
        var pixelsToMove = _speed * deltaTime;

        // Handle horizontal movement
        var x = Position.X + (int)(right * pixelsToMove);
        x -= (int)(left * pixelsToMove);

        // Handle vertical movement
        var y = Position.Y;
        if (!_isGrounded) 
        {
            // Apply gravity when in air
            _verticalVelocity += _gravity * (float)deltaTime;
            _verticalVelocity = Math.Min(_verticalVelocity, _maxFallSpeed);
            y += (int)(_verticalVelocity * deltaTime);
        }
        else
        {
            // Normal vertical movement when on ground
            y += (int)(down * pixelsToMove);
            y -= (int)(up * pixelsToMove);
        }

        // Ground collision check
        if (y > 300)
        {
            y = 300;
            _verticalVelocity = 0;
            _isGrounded = true;
            _jumpsRemaining = 2;
        }

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

        if (newState != State.State || newDirection != State.Direction)
        {
            SetState(newState, newDirection);
        }

        Position = (x, y);
    }

    public void Jump()
    {
        if (_jumpsRemaining > 0)
        {
            _verticalVelocity = _jumpForce;
            _isGrounded = false;
            _jumpsRemaining--;
        }
    }
}