using Silk.NET.SDL;
using Silk.NET.Maths;
using System.IO;
using System.Collections.Generic;
using System;

namespace TheAdventure.Models;

public class OrcObject : RenderableGameObject
{
    public int MaxHealth { get; private set; } = 50;
    public int Health { get; private set; }
    public bool IsDead => Health <= 0;
    public DateTimeOffset? DeathTime { get; private set; } = null;
    
    public int AttackDamage { get; private set; } = 10;
    public int AttackRange { get; private set; } = 25;
    private DateTimeOffset _lastAttackTime = DateTimeOffset.MinValue;
    private readonly TimeSpan _attackCooldown = TimeSpan.FromMilliseconds(2000);
    private bool _isAttacking = false;
    private readonly int _attackDurationMs = 500;
    
    public bool IsAggressive { get; set; } = true;
    public float ChaseRange { get; set; } = 200f;
    public float AttackRangeTrigger { get; set; } = 45f;
    public PlayerObject? PlayerTarget { get; set; }
    
    private readonly Dictionary<string, SpriteSheet> _animationSheets;
    private string _currentAnimation = "IdleDown";
    private bool _isMoving = false;
    private string _currentDirection = "Down";

    private const int OrcFrameWidth = 64;
    private const int OrcFrameHeight = 64;
    private const int OrcColumnCount = 6;
    private const int OrcRowCount = 4;
    private static readonly (int OffsetX, int OffsetY) OrcFrameCenter = (32, 58);
    
    private Random _random = new Random();
    private const int MoveSpeed = 40;
    private const int ChaseSpeed = 100;
    private Vector2D<float> _movementVelocity = new(0, 0);
    private const float MovementSmoothing = 0.85f;

    public OrcObject(GameRenderer renderer, (int X, int Y) position)
        : base(LoadInitialSheet(renderer), position)
    {
        Health = MaxHealth;
        
        _animationSheets = new Dictionary<string, SpriteSheet>();
        _animationSheets["Idle"] = base.SpriteSheet;

        var walkSheet = new SpriteSheet(renderer, Path.Combine("Assets", "orc", "Orc1_walk", "orc1_walk_full.png"),
                                       OrcRowCount, OrcColumnCount, OrcFrameWidth, OrcFrameHeight, OrcFrameCenter);
        walkSheet.Animations["WalkDown"] = new SpriteSheet.Animation { StartFrame = (0, 0), EndFrame = (0, 5), DurationMs = 600, Loop = true };
        walkSheet.Animations["WalkUp"] = new SpriteSheet.Animation { StartFrame = (1, 0), EndFrame = (1, 5), DurationMs = 600, Loop = true };
        walkSheet.Animations["WalkLeft"] = new SpriteSheet.Animation { StartFrame = (2, 0), EndFrame = (2, 5), DurationMs = 600, Loop = true };
        walkSheet.Animations["WalkRight"] = new SpriteSheet.Animation { StartFrame = (3, 0), EndFrame = (3, 5), DurationMs = 600, Loop = true };
        _animationSheets["Walk"] = walkSheet;

        var runSheet = new SpriteSheet(renderer, Path.Combine("Assets", "orc", "Orc1_run", "orc1_run_full.png"),
                                      OrcRowCount, OrcColumnCount, OrcFrameWidth, OrcFrameHeight, OrcFrameCenter);
        runSheet.Animations["RunDown"] = new SpriteSheet.Animation { StartFrame = (0, 0), EndFrame = (0, 5), DurationMs = 400, Loop = true };
        runSheet.Animations["RunUp"] = new SpriteSheet.Animation { StartFrame = (1, 0), EndFrame = (1, 5), DurationMs = 400, Loop = true };
        runSheet.Animations["RunLeft"] = new SpriteSheet.Animation { StartFrame = (2, 0), EndFrame = (2, 5), DurationMs = 400, Loop = true };
        runSheet.Animations["RunRight"] = new SpriteSheet.Animation { StartFrame = (3, 0), EndFrame = (3, 5), DurationMs = 400, Loop = true };
        _animationSheets["Run"] = runSheet;

        var attackSheet = new SpriteSheet(renderer, Path.Combine("Assets", "orc", "Orc1_attack", "orc1_attack_full.png"),
                                         OrcRowCount, OrcColumnCount, OrcFrameWidth, OrcFrameHeight, OrcFrameCenter);
        attackSheet.Animations["AttackDown"] = new SpriteSheet.Animation { StartFrame = (0, 0), EndFrame = (0, 5), DurationMs = 500, Loop = false };
        attackSheet.Animations["AttackUp"] = new SpriteSheet.Animation { StartFrame = (1, 0), EndFrame = (1, 5), DurationMs = 500, Loop = false };
        attackSheet.Animations["AttackLeft"] = new SpriteSheet.Animation { StartFrame = (2, 0), EndFrame = (2, 5), DurationMs = 500, Loop = false };
        attackSheet.Animations["AttackRight"] = new SpriteSheet.Animation { StartFrame = (3, 0), EndFrame = (3, 5), DurationMs = 500, Loop = false };
        _animationSheets["Attack"] = attackSheet;

        var deathSheet = new SpriteSheet(renderer, Path.Combine("Assets", "orc", "Orc1_death", "orc1_death_full.png"),
                                        OrcRowCount, OrcColumnCount, OrcFrameWidth, OrcFrameHeight, OrcFrameCenter);
        deathSheet.Animations["Death"] = new SpriteSheet.Animation { StartFrame = (0, 0), EndFrame = (0, 5), DurationMs = 800, Loop = false };
        _animationSheets["Death"] = deathSheet;

        ActivateAnimation("IdleDown");
    }

    private static SpriteSheet LoadInitialSheet(GameRenderer renderer)
    {
        var idleSheet = new SpriteSheet(renderer, Path.Combine("Assets", "orc", "Orc1_idle", "orc1_idle_full.png"),
                                       OrcRowCount, OrcColumnCount, OrcFrameWidth, OrcFrameHeight, OrcFrameCenter);
        idleSheet.Animations["IdleDown"] = new SpriteSheet.Animation { StartFrame = (0, 0), EndFrame = (0, 5), DurationMs = 1000, Loop = true };
        idleSheet.Animations["IdleUp"] = new SpriteSheet.Animation { StartFrame = (1, 0), EndFrame = (1, 5), DurationMs = 1000, Loop = true };
        idleSheet.Animations["IdleLeft"] = new SpriteSheet.Animation { StartFrame = (2, 0), EndFrame = (2, 5), DurationMs = 1000, Loop = true };
        idleSheet.Animations["IdleRight"] = new SpriteSheet.Animation { StartFrame = (3, 0), EndFrame = (3, 5), DurationMs = 1000, Loop = true };
        return idleSheet;
    }

    public void Update(double deltaTimeSeconds)
    {
        if (IsDead)
        {
            if (_currentAnimation != "Death")
            {
                ActivateAnimation("Death");
            }
            return;
        }
        
        bool attackAnimationFinished = !_isAttacking || 
                                      (DateTimeOffset.Now - _lastAttackTime) >= TimeSpan.FromMilliseconds(_attackDurationMs);
        
        if (_isAttacking && !attackAnimationFinished)
        {
            return;
        }
        else if (_isAttacking && attackAnimationFinished)
        {
            _isAttacking = false;
            ActivateAnimation("Idle" + _currentDirection);
        }

        if (PlayerTarget != null && IsAggressive && !PlayerTarget.IsDead)
        {
            float distance = CalculateDistanceToPlayer();
            
            if (distance <= AttackRangeTrigger)
            {
                TryAttackPlayer(deltaTimeSeconds);
                return;
            }
            else if (distance <= ChaseRange)
            {
                ChasePlayer(deltaTimeSeconds);
                return;
            }
        }
        
        RandomMovement(deltaTimeSeconds);
    }
    
    private float CalculateDistanceToPlayer()
    {
        if (PlayerTarget == null) return float.MaxValue;
        
        int dx = Position.X - PlayerTarget.X;
        int dy = Position.Y - PlayerTarget.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
    
    private void TryAttackPlayer(double deltaTimeSeconds)
    {
        if (PlayerTarget == null || PlayerTarget.IsDead) return;
        
        UpdateFacingDirection();
        
        bool canAttack = (DateTimeOffset.Now - _lastAttackTime) >= _attackCooldown;
        
        if (canAttack)
        {
            _isAttacking = true;
            _lastAttackTime = DateTimeOffset.Now;
            
            string attackAnimation = "Attack" + _currentDirection;
            ActivateAnimation(attackAnimation);
            
            PlayerTarget.TakeDamage(AttackDamage);
        }
        else
        {
            ActivateAnimation("Idle" + _currentDirection);
        }
    }
    
    private void ChasePlayer(double deltaTimeSeconds)
    {
        if (PlayerTarget == null) return;
        
        float dx = PlayerTarget.X - Position.X;
        float dy = PlayerTarget.Y - Position.Y;
        
        float length = MathF.Sqrt(dx * dx + dy * dy);
        float ndx = dx / length;
        float ndy = dy / length;
        
        float targetVelocityX = ndx * ChaseSpeed;
        float targetVelocityY = ndy * ChaseSpeed;
        
        _movementVelocity.X = _movementVelocity.X * MovementSmoothing + targetVelocityX * (1 - MovementSmoothing);
        _movementVelocity.Y = _movementVelocity.Y * MovementSmoothing + targetVelocityY * (1 - MovementSmoothing);
        
        int moveX = (int)(_movementVelocity.X * deltaTimeSeconds);
        int moveY = (int)(_movementVelocity.Y * deltaTimeSeconds);
        
        Position = (Position.X + moveX, Position.Y + moveY);
        
        UpdateFacingDirectionFromMovement(moveX, moveY);
        
        ActivateAnimation("Run" + _currentDirection);
    }
    
    private void UpdateFacingDirection()
    {
        if (PlayerTarget == null) return;
        
        int dx = PlayerTarget.X - Position.X;
        int dy = PlayerTarget.Y - Position.Y;
        
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            _currentDirection = dx > 0 ? "Right" : "Left";
        }
        else
        {
            _currentDirection = dy > 0 ? "Down" : "Up";
        }
    }
    
    private void UpdateFacingDirectionFromMovement(int moveX, int moveY)
    {
        if (Math.Abs(moveX) > Math.Abs(moveY))
        {
            _currentDirection = moveX > 0 ? "Right" : "Left";
        }
        else if (Math.Abs(moveY) > 0)
        {
            _currentDirection = moveY > 0 ? "Down" : "Up";
        }
    }
    
    private void RandomMovement(double deltaTimeSeconds)
    {
        string desiredAnimationBase = "Idle";

        if (!_isMoving && _random.NextDouble() < 0.01)
        {
             _isMoving = true;
             desiredAnimationBase = "Walk";
             int directionChoice = _random.Next(4);
             if (directionChoice == 0) _currentDirection = "Down";
             else if (directionChoice == 1) _currentDirection = "Up";
             else if (directionChoice == 2) _currentDirection = "Left";
             else _currentDirection = "Right";

        } 
        else if (_isMoving && _random.NextDouble() < 0.05)
        {
             _isMoving = false;
             desiredAnimationBase = "Idle";
        }
        else if (_isMoving)
        {
            desiredAnimationBase = "Walk";
        }


        if (_isMoving) {
             int deltaX = 0;
             int deltaY = 0;

             switch (_currentDirection)
             {
                 case "Up":
                     deltaY = -(int)(MoveSpeed * deltaTimeSeconds);
                     break;
                 case "Down":
                     deltaY = (int)(MoveSpeed * deltaTimeSeconds);
                     break;
                 case "Left":
                     deltaX = -(int)(MoveSpeed * deltaTimeSeconds);
                     break;
                 case "Right":
                     deltaX = (int)(MoveSpeed * deltaTimeSeconds);
                     break;
             }
             Position = (Position.X + deltaX, Position.Y + deltaY);
        }

        string desiredAnimation = desiredAnimationBase + _currentDirection;

        ActivateAnimation(desiredAnimation);
    }
    
    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        
        Health = Math.Max(0, Health - damage);
        
        if (IsDead)
        {
            DeathTime = DateTimeOffset.Now;
            ActivateAnimation("Death");
        }
    }

    public void ActivateAnimation(string name)
    {
        string baseName = name;
        string direction = "";

        if (name.EndsWith("Down")) { baseName = name.Replace("Down", ""); direction = "Down"; }
        else if (name.EndsWith("Up")) { baseName = name.Replace("Up", ""); direction = "Up"; }
        else if (name.EndsWith("Left")) { baseName = name.Replace("Left", ""); direction = "Left"; }
        else if (name.EndsWith("Right")) { baseName = name.Replace("Right", ""); direction = "Right"; }
        else if (!_animationSheets.ContainsKey(name) && _animationSheets.ContainsKey(baseName))
        {
        }
        else if (!_animationSheets.ContainsKey(baseName))
        {
             Console.WriteLine($"Warning: Base animation type '{baseName}' derived from '{name}' not found for OrcObject.");
             baseName = "Idle";
             direction = _currentDirection;
             name = baseName + direction;
        }

        bool forceRestart = _currentAnimation != name;

        if (_animationSheets.TryGetValue(baseName, out var newSheet))
        {
            if (newSheet.Animations.ContainsKey(name))
            {
                bool sheetChanged = false;
                if (this.SpriteSheet != newSheet)
                {
                    this.SpriteSheet = newSheet;
                    sheetChanged = true;
                }
                
                this.SpriteSheet.ActivateAnimation(name);
                
                if (sheetChanged || forceRestart)
                {
                    this.SpriteSheet.ForceRestartAnimation();
                }
                
                _currentAnimation = name;
                if (!string.IsNullOrEmpty(direction))
                {
                    _currentDirection = direction;
                }
            }
            else
            {
                if (newSheet.Animations.ContainsKey(baseName)) {
                    bool sheetChanged = false;
                    if (this.SpriteSheet != newSheet) {
                        this.SpriteSheet = newSheet;
                        sheetChanged = true;
                    }
                    this.SpriteSheet.ActivateAnimation(baseName);
                    
                    if (sheetChanged || forceRestart)
                    {
                        this.SpriteSheet.ForceRestartAnimation();
                    }
                    
                    _currentAnimation = baseName;
                } else {
                    Console.WriteLine($"Warning: Animation '{name}' (or base '{baseName}') not found in sheet '{baseName}' for OrcObject.");
                    ActivateAnimation("Idle" + _currentDirection);
                }
            }
        }
        else
        {
             Console.WriteLine($"Warning: Animation sheet for '{baseName}' not found for OrcObject.");
             ActivateAnimation("IdleDown");
        }
    }
    
    public override void Render(GameRenderer renderer)
    {
        base.Render(renderer);
        
        if (!IsDead)
        {
            int healthBarWidth = 40;
            int healthBarHeight = 5;
            int healthBarX = Position.X - healthBarWidth / 2;
            int healthBarY = Position.Y - 50;
            
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
}
