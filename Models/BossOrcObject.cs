using Silk.NET.Maths;
using Silk.NET.SDL;
using System.IO;
using System.Collections.Generic;
using System;

namespace TheAdventure.Models;

public class BossOrcObject : OrcObject
{
    public bool IsBoss { get; } = true;
    private readonly float _bombThrowCooldown = 3.0f;
    private DateTimeOffset _lastBombThrow = DateTimeOffset.MinValue;
    
    private readonly int _bombDamage = 20;
    private readonly float _bombRadius = 150f;
    
    private bool _isThrowing = false;
    private DateTimeOffset _throwStartTime = DateTimeOffset.MinValue;
    private readonly int _throwDurationMs = 800;
    
    private List<BouncingBomb> _activeBombs = new();
    
    private readonly SpriteSheet _bombSheet;
    
    private int _frameCounter = 0;
    
    public BossOrcObject(GameRenderer renderer, (int X, int Y) position)
        : base(renderer, position)
    {
        MaxHealth = 250;
        Health = MaxHealth;
        AttackDamage = 15;
        AttackRange = 35;
        ChaseRange = 5000f;
        
        LoadOrc2Sprites(renderer);
        
        _bombSheet = new SpriteSheet(renderer, Path.Combine("Assets", "BombExploding.png"), 1, 13, 32, 64, (16, 48));
        _bombSheet.Animations["Explode"] = new SpriteSheet.Animation
        {
            StartFrame = (0, 0),
            EndFrame = (0, 12),
            DurationMs = 2000,
            Loop = false
        };
    }
    
    private void LoadOrc2Sprites(GameRenderer renderer)
    {
        const int OrcFrameWidth = 64;
        const int OrcFrameHeight = 64;
        const int OrcColumnCount = 6;
        const int OrcRowCount = 4;
        var OrcFrameCenter = (32, 58);
        
        var idleSheet = new SpriteSheet(renderer, Path.Combine("Assets", "orc", "Orc2_idle", "orc2_idle_full.png"),
                                       OrcRowCount, OrcColumnCount, OrcFrameWidth, OrcFrameHeight, OrcFrameCenter);
        idleSheet.Animations["IdleDown"] = new SpriteSheet.Animation { StartFrame = (0, 0), EndFrame = (0, 3), DurationMs = 1000, Loop = true };
        idleSheet.Animations["IdleUp"] = new SpriteSheet.Animation { StartFrame = (1, 0), EndFrame = (1, 3), DurationMs = 1000, Loop = true };
        idleSheet.Animations["IdleLeft"] = new SpriteSheet.Animation { StartFrame = (2, 0), EndFrame = (2, 3), DurationMs = 1000, Loop = true };
        idleSheet.Animations["IdleRight"] = new SpriteSheet.Animation { StartFrame = (3, 0), EndFrame = (3, 3), DurationMs = 1000, Loop = true };
        base.SpriteSheet = idleSheet;
        
        var walkSheet = new SpriteSheet(renderer, Path.Combine("Assets", "orc", "Orc2_walk", "orc2_walk_full.png"),
                                       OrcRowCount, OrcColumnCount, OrcFrameWidth, OrcFrameHeight, OrcFrameCenter);
        walkSheet.Animations["WalkDown"] = new SpriteSheet.Animation { StartFrame = (0, 0), EndFrame = (0, 5), DurationMs = 600, Loop = true };
        walkSheet.Animations["WalkUp"] = new SpriteSheet.Animation { StartFrame = (1, 0), EndFrame = (1, 5), DurationMs = 600, Loop = true };
        walkSheet.Animations["WalkLeft"] = new SpriteSheet.Animation { StartFrame = (2, 0), EndFrame = (2, 5), DurationMs = 600, Loop = true };
        walkSheet.Animations["WalkRight"] = new SpriteSheet.Animation { StartFrame = (3, 0), EndFrame = (3, 5), DurationMs = 600, Loop = true };
        
        var runSheet = new SpriteSheet(renderer, Path.Combine("Assets", "orc", "Orc2_run", "orc2_run_full.png"),
                                      OrcRowCount, OrcColumnCount, OrcFrameWidth, OrcFrameHeight, OrcFrameCenter);
        runSheet.Animations["RunDown"] = new SpriteSheet.Animation { StartFrame = (0, 0), EndFrame = (0, 5), DurationMs = 400, Loop = true };
        runSheet.Animations["RunUp"] = new SpriteSheet.Animation { StartFrame = (1, 0), EndFrame = (1, 5), DurationMs = 400, Loop = true };
        runSheet.Animations["RunLeft"] = new SpriteSheet.Animation { StartFrame = (2, 0), EndFrame = (2, 5), DurationMs = 400, Loop = true };
        runSheet.Animations["RunRight"] = new SpriteSheet.Animation { StartFrame = (3, 0), EndFrame = (3, 5), DurationMs = 400, Loop = true };
        
        var attackSheet = new SpriteSheet(renderer, Path.Combine("Assets", "orc", "Orc2_attack", "orc2_attack_full.png"),
                                         OrcRowCount, OrcColumnCount, OrcFrameWidth, OrcFrameHeight, OrcFrameCenter);
        attackSheet.Animations["AttackDown"] = new SpriteSheet.Animation { StartFrame = (0, 0), EndFrame = (0, 5), DurationMs = 500, Loop = false };
        attackSheet.Animations["AttackUp"] = new SpriteSheet.Animation { StartFrame = (1, 0), EndFrame = (1, 5), DurationMs = 500, Loop = false };
        attackSheet.Animations["AttackLeft"] = new SpriteSheet.Animation { StartFrame = (2, 0), EndFrame = (2, 5), DurationMs = 500, Loop = false };
        attackSheet.Animations["AttackRight"] = new SpriteSheet.Animation { StartFrame = (3, 0), EndFrame = (3, 5), DurationMs = 500, Loop = false };
        
        var deathSheet = new SpriteSheet(renderer, Path.Combine("Assets", "orc", "Orc2_death", "orc2_death_full.png"),
                                        OrcRowCount, OrcColumnCount, OrcFrameWidth, OrcFrameHeight, OrcFrameCenter);
        deathSheet.Animations["Death"] = new SpriteSheet.Animation { StartFrame = (0, 0), EndFrame = (0, 5), DurationMs = 800, Loop = false };
        
        var hurtSheet = new SpriteSheet(renderer, Path.Combine("Assets", "orc", "Orc2_hurt", "orc2_hurt_full.png"),
                                       OrcRowCount, OrcColumnCount, OrcFrameWidth, OrcFrameHeight, OrcFrameCenter);
        hurtSheet.Animations["HurtDown"] = new SpriteSheet.Animation { StartFrame = (0, 0), EndFrame = (0, 3), DurationMs = 300, Loop = false };
        hurtSheet.Animations["HurtUp"] = new SpriteSheet.Animation { StartFrame = (1, 0), EndFrame = (1, 3), DurationMs = 300, Loop = false };
        hurtSheet.Animations["HurtLeft"] = new SpriteSheet.Animation { StartFrame = (2, 0), EndFrame = (2, 3), DurationMs = 300, Loop = false };
        hurtSheet.Animations["HurtRight"] = new SpriteSheet.Animation { StartFrame = (3, 0), EndFrame = (3, 3), DurationMs = 300, Loop = false };
        
        typeof(OrcObject)
            .GetField("_animationSheets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(this, new Dictionary<string, SpriteSheet> {
                { "Idle", idleSheet },
                { "Walk", walkSheet },
                { "Run", runSheet },
                { "Attack", attackSheet },
                { "Death", deathSheet },
                { "Hurt", hurtSheet }
            });
        
        ActivateAnimation("IdleDown");
    }
    
    public override void Update(double deltaTimeSeconds)
    {
        _frameCounter++;
        
        if (IsDead)
        {
            base.Update(deltaTimeSeconds);
            return;
        }
        
        UpdateActiveBombs(deltaTimeSeconds);
        
        if (_isThrowing)
        {
            if ((DateTimeOffset.Now - _throwStartTime).TotalMilliseconds >= _throwDurationMs)
            {
                _isThrowing = false;
                string direction = GetBaseDirection();
                ActivateAnimation("Idle" + direction);
            }
            else
            {
                return;
            }
        }
        
        bool canThrowBomb = (DateTimeOffset.Now - _lastBombThrow).TotalSeconds >= _bombThrowCooldown;
        
        if (PlayerTarget != null && !PlayerTarget.IsDead && canThrowBomb && 
            CalculateDistanceToPlayer() < ChaseRange && 
            CalculateDistanceToPlayer() > AttackRangeTrigger)
        {
            if (new Random().NextDouble() < 0.4)
            {
                ThrowBomb();
                return;
            }
        }
        
        base.Update(deltaTimeSeconds);
    }
    
    private void ThrowBomb()
    {
        if (PlayerTarget == null) return;
        
        _isThrowing = true;
        _throwStartTime = DateTimeOffset.Now;
        _lastBombThrow = DateTimeOffset.Now;
        
        string direction = GetBaseDirection();
        ActivateAnimation("Attack" + direction);
        
        float distance = CalculateDistanceToPlayer();
        float throwTime = 1.5f;
        
        BouncingBomb bomb = new BouncingBomb(
            _bombSheet,
            Position,
            new Vector2D<float>(PlayerTarget.X, PlayerTarget.Y),
            throwTime,
            _bombDamage,
            _bombRadius
        );
        
        _activeBombs.Add(bomb);
    }
    
    private void UpdateActiveBombs(double deltaTimeSeconds)
    {
        List<BouncingBomb> bombsToRemove = new();
        
        foreach (var bomb in _activeBombs)
        {
            bomb.Update(deltaTimeSeconds);
            
            if (bomb.IsExpired)
            {
                bombsToRemove.Add(bomb);
                
                if (PlayerTarget != null && !PlayerTarget.IsDead && 
                    bomb.IsInExplosionRange(new Vector2D<int>(PlayerTarget.X, PlayerTarget.Y)))
                {
                    PlayerTarget.TakeDamage(_bombDamage);
                }
            }
        }
        
        foreach (var bomb in bombsToRemove)
        {
            _activeBombs.Remove(bomb);
        }
    }
    
    public override void Render(GameRenderer renderer)
    {
        base.Render(renderer);
        
        foreach (var bomb in _activeBombs)
        {
            bomb.Render(renderer);
        }
    }
    
    private string GetBaseDirection()
    {
        if (PlayerTarget == null) return "Down";
        
        int dx = PlayerTarget.X - Position.X;
        int dy = PlayerTarget.Y - Position.Y;
        
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            return dx > 0 ? "Right" : "Left";
        }
        else
        {
            return dy > 0 ? "Down" : "Up";
        }
    }
    
    private class BouncingBomb
    {
        private SpriteSheet _spriteSheet;
        private (int X, int Y) _startPosition;
        private Vector2D<float> _targetPosition;
        private float _totalTime;
        private DateTimeOffset _startTime;
        private (float X, float Y) _currentPosition;
        private readonly int _damage;
        private readonly float _radius;
        private bool _hasExploded = false;
        private bool _hasBounced = false;
        private bool _explosionAnimationStarted = false;
        
        private float _bounceHeight = 10f;
        private float _bounceTime = 0.2f;
        private DateTimeOffset _bounceStartTime;
        
        public bool IsExpired => (DateTimeOffset.Now - _startTime).TotalSeconds > _totalTime + 2.0;
        
        public BouncingBomb(SpriteSheet spriteSheet, (int X, int Y) startPos, 
                           Vector2D<float> targetPos, float time, 
                           int damage, float radius)
        {
            _spriteSheet = spriteSheet;
            _startPosition = startPos;
            _targetPosition = targetPos;
            _totalTime = time;
            _startTime = DateTimeOffset.Now;
            _currentPosition = startPos;
            _damage = damage;
            _radius = radius;
            
            _spriteSheet.ActivateAnimation("Explode");
            _spriteSheet.PauseAnimation();
        }
        
        public void Update(double deltaTimeSeconds)
        {
            float elapsedTime = (float)(DateTimeOffset.Now - _startTime).TotalSeconds;
            
            if (elapsedTime < _totalTime)
            {
                float progress = elapsedTime / _totalTime;
                
                float x = _startPosition.X + (_targetPosition.X - _startPosition.X) * progress;
                float y = _startPosition.Y + (_targetPosition.Y - _startPosition.Y) * progress;
                
                float arcHeight = 120f;
                float arcFactor = progress * (1 - progress) * 4;
                float verticalOffset = -arcHeight * arcFactor;
                
                _currentPosition = (x, y + verticalOffset);
            }
            else if (!_hasExploded)
            {
                if (!_hasBounced)
                {
                    _hasBounced = true;
                    _bounceStartTime = DateTimeOffset.Now;
                }
                
                float bounceElapsed = (float)(DateTimeOffset.Now - _bounceStartTime).TotalSeconds;
                if (bounceElapsed < _bounceTime)
                {
                    float bounceProgress = bounceElapsed / _bounceTime;
                    float bounceOffset = _bounceHeight * (1 - bounceProgress);
                    _currentPosition = (_targetPosition.X, _targetPosition.Y - bounceOffset);
                }
                else
                {
                    _hasExploded = true;
                    _spriteSheet.ForceRestartAnimation();
                    _spriteSheet.ResumeAnimation();
                    _explosionAnimationStarted = true;
                }
            }
        }
        
        public void Render(GameRenderer renderer)
        {
            if (_hasExploded)
            {
                _spriteSheet.Render(renderer, ((int)_currentPosition.X, (int)_currentPosition.Y));
            }
            else
            {
                _spriteSheet.Render(renderer, ((int)_currentPosition.X, (int)_currentPosition.Y));
            }
        }
        
        public bool IsInExplosionRange(Vector2D<int> position)
        {
            if (!_hasExploded) return false;
            
            float dx = position.X - _currentPosition.X;
            float dy = position.Y - _currentPosition.Y;
            float distance = MathF.Sqrt(dx * dx + dy * dy);
            
            return distance <= _radius;
        }
    }
}