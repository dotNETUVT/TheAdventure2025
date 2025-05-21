using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int    WalkSpeed      = 128;
    private const int    DashDistance   = 96;
    private const double DashDuration   = 150;
    private const double DashCooldown   = 500;

    private const double RegenInterval  = 10_000;
    private const double InvincibleMs   = 1_000;

    private double _dashTimeRemaining;
    private double _dashCooldownRemaining;
    private bool   _isDashing;
    private Vector2D<int> _dashDir;

    private double _regenTimer;
    private double _invincibleTimer;         
    public  bool   IsInvincible => _invincibleTimer > 0;

    public enum PlayerStateDirection { None=0, Down, Up, Left, Right }
    public enum PlayerState          { None=0, Idle, Move, Attack, GameOver }

    public (PlayerState State, PlayerStateDirection Direction) State { get; private set; }

    public int MaxHealth { get; } = 3;
    private int _health;
    public  int Health    => _health;

    public PlayerObject(SpriteSheet sheet, int x, int y) : base(sheet,(x,y))
    {
        SetState(PlayerState.Idle, PlayerStateDirection.Down);
        _health = MaxHealth;
    }

    public void SetState(PlayerState s) => SetState(s, State.Direction);
    public void SetState(PlayerState s, PlayerStateDirection d)
    {
        if (State.State == PlayerState.GameOver) return;
        if (State.State == s && State.Direction == d) return;

        string? anim = s switch
        {
            PlayerState.None when d==PlayerStateDirection.None => null,
            PlayerState.GameOver => nameof(PlayerState.GameOver),
            _ => $"{s}{d}"
        };
        SpriteSheet.ActivateAnimation(anim);
        State = (s,d);
    }

    public void GameOver() => SetState(PlayerState.GameOver, PlayerStateDirection.None);

    public void Attack()
    {
        if (State.State == PlayerState.GameOver) return;
        SetState(PlayerState.Attack, State.Direction);
    }

    private static Vector2D<int> DirToVec(PlayerStateDirection dir) => dir switch
    {
        PlayerStateDirection.Up    => new(0,-1),
        PlayerStateDirection.Down  => new(0, 1),
        PlayerStateDirection.Left  => new(-1,0),
        PlayerStateDirection.Right => new(1, 0),
        _                          => new(0, 0)
    };

    public void UpdatePosition(double up,double down,double left,double right,bool dash,int w,int h,double ms)
    {
        if (State.State == PlayerState.GameOver) return;

        if (_invincibleTimer > 0)
            _invincibleTimer = Math.Max(0, _invincibleTimer - ms);

        if (_health < MaxHealth)
        {
            _regenTimer += ms;
            if (_regenTimer >= RegenInterval)
            {
                _health = Math.Min(MaxHealth, _health + 1);
                _regenTimer -= RegenInterval;
            }
        }

        if (_dashCooldownRemaining > 0)
            _dashCooldownRemaining = Math.Max(0, _dashCooldownRemaining - ms);

        if (_isDashing)
        {
            double speed = DashDistance / DashDuration;
            Position = (Position.X + (int)(_dashDir.X * speed * ms),
                        Position.Y + (int)(_dashDir.Y * speed * ms));

            _dashTimeRemaining -= ms;
            if (_dashTimeRemaining <= 0)
            {
                _isDashing = false;
                _dashCooldownRemaining = DashCooldown;
                SetState(PlayerState.Idle, State.Direction);
            }
            return;
        }

        if (dash && _dashCooldownRemaining <= 0)
        {
            var dashDir =
                up>0 ? PlayerStateDirection.Up :
                down>0? PlayerStateDirection.Down :
                left>0? PlayerStateDirection.Left :
                right>0?PlayerStateDirection.Right : State.Direction;

            _dashDir = DirToVec(dashDir);
            if (_dashDir != default)
            {
                _isDashing = true;
                _dashTimeRemaining = DashDuration;
                SetState(PlayerState.Move, dashDir);
                return;
            }
        }

        double movePx = WalkSpeed * (ms / 1000.0);
        double dx = (right-left) * movePx;
        double dy = (down-up)   * movePx;

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

        Position = (newX,newY);
    }

    public void TakeDamage(int amount)
    {
        if (State.State == PlayerState.GameOver || IsInvincible) return;

        _health = Math.Max(0, _health - amount);
        _regenTimer = 0;
        _invincibleTimer = InvincibleMs;

        if (_health == 0) GameOver();
    }
}
