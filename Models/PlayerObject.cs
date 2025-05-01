using Silk.NET.Maths;
using Silk.NET.SDL;

namespace TheAdventure.Models;

public class PlayerObject : GameObject
{
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;
    
    public int MaxHealth { get; private set; } = 100;
    public int Health { get; private set; }
    public bool IsDead => Health <= 0;
    
    public int AttackDamage { get; private set; } = 25;
    public int AttackRange { get; private set; } = 45;
    public bool IsInvulnerable { get; private set; } = false;
    private DateTimeOffset _lastDamageTime = DateTimeOffset.MinValue;
    private readonly TimeSpan _invulnerabilityDuration = TimeSpan.FromMilliseconds(500);

    private readonly SpriteSheet _spriteSheet;
    private string _currentAnimation = "IdleDown";
    private bool _isMoving = false;
    private bool _isAttacking = false;
    private DateTimeOffset _lastAttackTime = DateTimeOffset.MinValue;
    private readonly TimeSpan _attackCooldown = TimeSpan.FromMilliseconds(500);
    private readonly int _attackDurationMs = 300;

    private const int Speed = 128;

    public PlayerObject(GameRenderer renderer)
    {
        Health = MaxHealth;
        
        _spriteSheet = new SpriteSheet(renderer, Path.Combine("Assets", "player.png"), 10, 6, 48, 48, (24, 42))
        {
            Animations = new Dictionary<string, SpriteSheet.Animation>()
            {
                { "IdleDown", new SpriteSheet.Animation{ StartFrame = (0, 0), EndFrame = (0, 5), DurationMs = 1000, Loop = true } },
                { "IdleLeft", new SpriteSheet.Animation{ StartFrame = (1, 0), EndFrame = (1, 5), DurationMs = 1000, Loop = true, Flip = RendererFlip.Horizontal } },
                { "IdleRight", new SpriteSheet.Animation{ StartFrame = (1, 0), EndFrame = (1, 5), DurationMs = 1000, Loop = true } },
                { "IdleUp", new SpriteSheet.Animation{ StartFrame = (2, 0), EndFrame = (2, 5), DurationMs = 1000, Loop = true } },

                { "WalkDown", new SpriteSheet.Animation{ StartFrame = (3, 0), EndFrame = (3, 5), DurationMs = 400, Loop = true } },
                { "WalkLeft", new SpriteSheet.Animation{ StartFrame = (4, 0), EndFrame = (4, 5), DurationMs = 400, Loop = true, Flip = RendererFlip.Horizontal } },
                { "WalkRight", new SpriteSheet.Animation{ StartFrame = (4, 0), EndFrame = (4, 5), DurationMs = 400, Loop = true } },
                { "WalkUp", new SpriteSheet.Animation{ StartFrame = (5, 0), EndFrame = (5, 3), DurationMs = 400, Loop = true } },

                { "AttackDown", new SpriteSheet.Animation{ StartFrame = (6, 0), EndFrame = (6, 3), DurationMs = _attackDurationMs, Loop = false } },
                { "AttackLeft", new SpriteSheet.Animation{ StartFrame = (7, 0), EndFrame = (7, 3), DurationMs = _attackDurationMs, Loop = false, Flip = RendererFlip.Horizontal } },
                { "AttackRight", new SpriteSheet.Animation{ StartFrame = (7, 0), EndFrame = (7, 3), DurationMs = _attackDurationMs, Loop = false } },
                { "AttackUp", new SpriteSheet.Animation{ StartFrame = (8, 0), EndFrame = (8, 3), DurationMs = _attackDurationMs, Loop = false } },
            }
        };
        _spriteSheet.ActivateAnimation(_currentAnimation);
    }

    public void Update(Input input, int time)
    {
        if (IsDead)
        {
            return;
        }
            
        if (IsInvulnerable && (DateTimeOffset.Now - _lastDamageTime) >= _invulnerabilityDuration)
        {
            IsInvulnerable = false;
        }
            
        _isMoving = false;
        string desiredAnimation = _currentAnimation;

        bool canAttack = (DateTimeOffset.Now - _lastAttackTime) >= _attackCooldown;
        bool attackAnimationFinished = !_isAttacking || (DateTimeOffset.Now - _lastAttackTime) >= TimeSpan.FromMilliseconds(_attackDurationMs);

        if (_isAttacking && !attackAnimationFinished)
        {
            return;
        }
        else if (_isAttacking && attackAnimationFinished)
        {
            _isAttacking = false;
             if (_currentAnimation.Contains("Down")) desiredAnimation = "IdleDown";
             else if (_currentAnimation.Contains("Left")) desiredAnimation = "IdleLeft";
             else if (_currentAnimation.Contains("Right")) desiredAnimation = "IdleRight";
             else if (_currentAnimation.Contains("Up")) desiredAnimation = "IdleUp";
             else desiredAnimation = "IdleDown";
        }


        if (input.IsAttackPressed() && canAttack && !_isAttacking)
        {
            _isAttacking = true;
            _lastAttackTime = DateTimeOffset.Now;

            if (_currentAnimation.Contains("Down")) desiredAnimation = "AttackDown";
            else if (_currentAnimation.Contains("Left")) desiredAnimation = "AttackLeft";
            else if (_currentAnimation.Contains("Right")) desiredAnimation = "AttackRight";
            else if (_currentAnimation.Contains("Up")) desiredAnimation = "AttackUp";
            else desiredAnimation = "AttackDown";

             _spriteSheet.ActivateAnimation(desiredAnimation);
             _currentAnimation = desiredAnimation;
             return;
        }

        if (!_isAttacking)
        {
            var pixelsToMove = Speed * (time / 1000.0);
            double moveX = 0;
            double moveY = 0;

            if (input.IsUpPressed())
            {
                moveY -= pixelsToMove;
                desiredAnimation = "WalkUp";
                _isMoving = true;
            }
            if (input.IsDownPressed())
            {
                moveY += pixelsToMove;
                desiredAnimation = "WalkDown";
                _isMoving = true;
            }
            if (input.IsLeftPressed())
            {
                moveX -= pixelsToMove;
                desiredAnimation = "WalkLeft";
                _isMoving = true;
            }
            if (input.IsRightPressed())
            {
                moveX += pixelsToMove;
                desiredAnimation = "WalkRight";
                _isMoving = true;
            }

            X += (int)moveX;
            Y += (int)moveY;

            if (!_isMoving)
            {
                 if (desiredAnimation.Contains("WalkDown")) desiredAnimation = "IdleDown";
                 else if (desiredAnimation.Contains("WalkLeft")) desiredAnimation = "IdleLeft";
                 else if (desiredAnimation.Contains("WalkRight")) desiredAnimation = "IdleRight";
                 else if (desiredAnimation.Contains("WalkUp")) desiredAnimation = "IdleUp";
            }

            if (_currentAnimation != desiredAnimation)
            {
                _spriteSheet.ActivateAnimation(desiredAnimation);
                _currentAnimation = desiredAnimation;
            }
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (IsDead || IsInvulnerable) return;
        
        Health = Math.Max(0, Health - damage);
        IsInvulnerable = true;
        _lastDamageTime = DateTimeOffset.Now;
        
        if (IsDead)
        {
        }
    }
    
    public string GetFacingDirection()
    {
        if (_currentAnimation.Contains("Down")) return "Down";
        if (_currentAnimation.Contains("Left")) return "Left";
        if (_currentAnimation.Contains("Right")) return "Right";
        if (_currentAnimation.Contains("Up")) return "Up";
        return "Down";
    }
    
    public bool IsCurrentlyAttacking()
    {
        return _isAttacking && 
               (DateTimeOffset.Now - _lastAttackTime) < TimeSpan.FromMilliseconds(_attackDurationMs * 0.6);
    }
    
    public bool IsPositionInAttackArc(Vector2D<int> position)
    {
        if (!IsCurrentlyAttacking())
            return false;
            
        string facingDirection = GetFacingDirection();
        
        int dx = position.X - X;
        int dy = position.Y - Y;
        float distance = MathF.Sqrt(dx * dx + dy * dy);
        
        float attackRangeModifier = 1.0f;
        if (distance <= AttackRange * 0.5f)
            attackRangeModifier = 1.5f;
        else if (distance > AttackRange * attackRangeModifier)
            return false;
        
        float angle = MathF.Atan2(dy, dx) * (180f / MathF.PI);
        if (angle < 0) angle += 360f;
        
        float arcCenter;
        switch (facingDirection)
        {
            case "Down":
                arcCenter = 90f;
                break;
            case "Up":
                arcCenter = 270f;
                break;
            case "Left":
                arcCenter = 180f;
                break;
            case "Right":
                arcCenter = 0f;
                break;
            default:
                return false;
        }
        
        float arcWidth = 70f;
        
        float angleDiff = MathF.Abs((((angle - arcCenter) + 180f) % 360f) - 180f);
        return angleDiff <= arcWidth;
    }
    
    public Rectangle<int> GetAttackHitbox()
    {
        int hitboxWidth = 30;
        int hitboxHeight = 30;
        int offsetX = 0;
        int offsetY = 0;
        
        switch (GetFacingDirection())
        {
            case "Down":
                offsetY = 20;
                break;
            case "Up":
                offsetY = -30;
                break;
            case "Left":
                offsetX = -30;
                break;
            case "Right":
                offsetX = 30;
                break;
        }
        
        return new Rectangle<int>(X + offsetX - hitboxWidth/2, Y + offsetY - hitboxHeight/2, hitboxWidth, hitboxHeight);
    }
    
    public void RestoreHealth(int amount)
    {
        if (IsDead) return;
        
        Health = Math.Min(MaxHealth, Health + amount);
    }

    public void Render(GameRenderer renderer)
    {
        if (IsInvulnerable)
        {
            if ((DateTimeOffset.Now.Ticks / TimeSpan.TicksPerMillisecond) % 100 < 50)
            {
                _spriteSheet.Render(renderer, (X, Y));
            }
        }
        else
        {
            _spriteSheet.Render(renderer, (X, Y));
        }
        
        int healthBarWidth = 40;
        int healthBarHeight = 5;
        int healthBarX = X - healthBarWidth / 2;
        int healthBarY = Y - 50;
        
        renderer.RenderRectangle(
            new Rectangle<int>(healthBarX, healthBarY, healthBarWidth, healthBarHeight),
            255, 0, 0, 255
        );
        
        int currentHealthWidth = (int)(healthBarWidth * (Health / (float)MaxHealth));
        renderer.RenderRectangle(
            new Rectangle<int>(healthBarX, healthBarY, currentHealthWidth, healthBarHeight),
            0, 255, 0, 255
        );
    }
}