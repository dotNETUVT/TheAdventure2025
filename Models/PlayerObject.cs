using Silk.NET.SDL;
using Silk.NET.Maths;
namespace TheAdventure.Models;
public class PlayerObject : GameObject
{
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;

    private const int Speed = 128;
    
    public (int X, int Y) GetPosition() => (X, Y);
    
    private readonly SpriteSheet _spriteSheet;
    
    private bool _isAttacking = false;
    private DateTimeOffset _attackStartTime;
    private const int AttackDurationMs = 500;
    public bool IsAttacking() => _isAttacking;
    
    private bool _isDead = false;
    private DateTimeOffset _deathStartTime;
    private const int DeathDurationMs = 800;

    public bool IsDead() => _isDead;
    
    private string _lastDirection = "Down";

    public PlayerObject(GameRenderer renderer)
    {
        _spriteSheet = new SpriteSheet(renderer, Path.Combine("Assets", "player.png"),
            rowCount: 8, columnCount: 6, frameWidth: 48, frameHeight: 48, frameCenter: (24, 24));
        
        _spriteSheet.Animations["WalkDown"] = new SpriteSheet.Animation { StartFrame = (3, 0), EndFrame = (3, 5), DurationMs = 500, Loop = true };
        _spriteSheet.Animations["WalkLeft"] = new SpriteSheet.Animation { StartFrame = (4, 0), EndFrame = (4, 5), DurationMs = 500, Loop = true, Flip = RendererFlip.Horizontal };
        _spriteSheet.Animations["WalkRight"] = new SpriteSheet.Animation { StartFrame = (4, 0), EndFrame = (4, 5), DurationMs = 500, Loop = true };
        _spriteSheet.Animations["WalkUp"] = new SpriteSheet.Animation { StartFrame = (5, 0), EndFrame = (5, 5), DurationMs = 500, Loop = true };
        
        _spriteSheet.Animations["IdleDown"] = new SpriteSheet.Animation { StartFrame = (3, 0), EndFrame = (3, 0), DurationMs = 1000, Loop = true };
        _spriteSheet.Animations["IdleLeft"] = new SpriteSheet.Animation { StartFrame = (4, 0), EndFrame = (4, 0), DurationMs = 1000, Loop = true, Flip = RendererFlip.Horizontal };
        _spriteSheet.Animations["IdleRight"] = new SpriteSheet.Animation { StartFrame = (4, 0), EndFrame = (4, 0), DurationMs = 1000, Loop = true };
        _spriteSheet.Animations["IdleUp"] = new SpriteSheet.Animation { StartFrame = (5, 0), EndFrame = (5, 0), DurationMs = 1000, Loop = true };
        
        _spriteSheet.Animations["AttackDown"] = new SpriteSheet.Animation { StartFrame = (6, 0), EndFrame = (6, 3), DurationMs = AttackDurationMs, Loop = false };
        _spriteSheet.Animations["AttackLeft"] = new SpriteSheet.Animation { StartFrame = (7, 0), EndFrame = (7, 3), DurationMs = AttackDurationMs, Loop = false, Flip = RendererFlip.Horizontal };
        _spriteSheet.Animations["AttackRight"] = new SpriteSheet.Animation { StartFrame = (7, 0), EndFrame = (7, 3), DurationMs = AttackDurationMs, Loop = false };
        _spriteSheet.Animations["AttackUp"] = new SpriteSheet.Animation { StartFrame = (8, 0), EndFrame = (8, 3), DurationMs = AttackDurationMs, Loop = false };
        
        _spriteSheet.Animations["Die"] = new SpriteSheet.Animation { StartFrame = (9, 0), EndFrame = (9, 2), DurationMs = DeathDurationMs, Loop = false };
        
        _spriteSheet.ActivateAnimation("IdleDown");
    }
    
    public void TriggerDeath()
    {
        if (_isDead) return;

        _isDead = true;
        _deathStartTime = DateTimeOffset.Now;
        _spriteSheet.ActivateAnimation("Die");
    }

    public void UpdatePosition(double up, double down, double left, double right, int time, bool isAttack, bool isSprinting)
    {
        double speedMultiplier = isSprinting ? 2.0 : 1.0;
        var pixelsToMove = Speed * speedMultiplier * (time / 1000.0);
        string? newAnimation = null;
        
        if (_isDead)
        {
            if ((DateTimeOffset.Now - _deathStartTime).TotalMilliseconds >= DeathDurationMs)
            {
                
            }
            return;
        }
        
        if (isAttack && !_isAttacking)
        {
            _isAttacking = true;
            _attackStartTime = DateTimeOffset.Now;
            newAnimation = $"Attack{_lastDirection}";
        }
        else if (_isAttacking)
        {
            if ((DateTimeOffset.Now - _attackStartTime).TotalMilliseconds >= AttackDurationMs)
            {
                _isAttacking = false;
                newAnimation = $"Idle{_lastDirection}";
            }
            else
            {
                newAnimation = $"Attack{_lastDirection}";
            }
        }
        else
        {
            if (up > 0) { Y -= (int)pixelsToMove; newAnimation = "WalkUp"; _lastDirection = "Up"; }
            else if (down > 0) { Y += (int)pixelsToMove; newAnimation = "WalkDown"; _lastDirection = "Down"; }
            else if (left > 0) { X -= (int)pixelsToMove; newAnimation = "WalkLeft"; _lastDirection = "Left"; }
            else if (right > 0) { X += (int)pixelsToMove; newAnimation = "WalkRight"; _lastDirection = "Right"; }
            else { newAnimation = $"Idle{_lastDirection}"; }
        }
        
        X = Math.Clamp(X, 0, 960);
        Y = Math.Clamp(Y, 0, 640);

        if (_spriteSheet.ActiveAnimation != _spriteSheet.Animations[newAnimation])
        {
            _spriteSheet.ActivateAnimation(newAnimation);
        }
    }
    
    public Rectangle<int> GetSwordHitBox()
    {
        int width = 32;
        int height = 32;

        return _lastDirection switch
        {
            "Up" => new Rectangle<int>(X - 16, Y - 48, width, height),
            "Down" => new Rectangle<int>(X - 16, Y, width, height),
            "Left" => new Rectangle<int>(X - 48, Y - 16, height, width),
            "Right" => new Rectangle<int>(X, Y - 16, height, width),
            _ => new Rectangle<int>(X, Y, width, height)
        };
    }
    
    public void Render(GameRenderer renderer)
    {
        _spriteSheet.Render(renderer, (X, Y));
    }
}
