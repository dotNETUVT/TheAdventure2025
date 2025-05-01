using Silk.NET.Maths;
using Silk.NET.SDL;
using System;
using System.IO;

namespace TheAdventure.Models;

public class HeartPickup : RenderableGameObject
{
    public int HealAmount { get; private set; } = 20;
    public bool IsActive { get; private set; } = true;
    public bool IsPickedUp { get; private set; } = false;
    public bool IsFloatingTowardsPlayer { get; private set; } = false;
    
    public float FloatSpeed { get; private set; } = 150f;
    public float AttractionRange { get; private set; } = 80f;
    public PlayerObject? PlayerTarget { get; set; }
    
    private readonly float _bobHeight = 10f;
    private readonly float _bobSpeed = 2f;
    private readonly float _pulseSpeed = 3f;
    private readonly float _pulseAmount = 0.2f;
    
    private readonly DateTimeOffset _spawnTime;
    private Vector2D<float> _basePosition;
    private readonly Random _random = new Random();
    private readonly TimeSpan _pickupDelay = TimeSpan.FromSeconds(2);
    
    public HeartPickup(GameRenderer renderer, (int X, int Y) position)
        : base(CreateHeartSpriteSheet(renderer), position)
    {
        _spawnTime = DateTimeOffset.Now;
        _basePosition = new Vector2D<float>(position.X, position.Y);
        
        Position = (
            position.X + _random.Next(-10, 10),
            position.Y + _random.Next(-5, 5)
        );
    }
    
    private static SpriteSheet CreateHeartSpriteSheet(GameRenderer renderer)
    {
        string heartImagePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "heart.png");
        
        var heartSheet = new SpriteSheet(
            renderer, 
            heartImagePath,
            1, 1,
            16, 16,
            (8, 8)
        );
        
        heartSheet.Animations["Idle"] = new SpriteSheet.Animation
        {
            StartFrame = (0, 0),
            EndFrame = (0, 0),
            DurationMs = 1000,
            Loop = true
        };
        
        heartSheet.ActivateAnimation("Idle");
        return heartSheet;
    }
    
    public void Update(double deltaTimeSeconds)
    {
        if (!IsActive || IsPickedUp)
            return;
            
        float timeSinceSpawn = (float)(DateTimeOffset.Now - _spawnTime).TotalSeconds;
        float verticalOffset = MathF.Sin(timeSinceSpawn * _bobSpeed) * _bobHeight;
        
        if (PlayerTarget != null && !IsFloatingTowardsPlayer)
        {
            float distanceToPlayer = CalculateDistanceToPlayer();
            if (distanceToPlayer <= AttractionRange)
            {
                IsFloatingTowardsPlayer = true;
            }
        }
        
        if (IsFloatingTowardsPlayer && PlayerTarget != null)
        {
            MoveTowardsPlayer(deltaTimeSeconds);
            
            if (CalculateDistanceToPlayer() < 20)
            {
                Pickup();
            }
        }
        else
        {
            Position = ((int)_basePosition.X, (int)(_basePosition.Y + verticalOffset));
        }
    }
    
    private float CalculateDistanceToPlayer()
    {
        if (PlayerTarget == null)
            return float.MaxValue;
            
        float dx = Position.X - PlayerTarget.X;
        float dy = Position.Y - PlayerTarget.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
    
    private void MoveTowardsPlayer(double deltaTimeSeconds)
    {
        if (PlayerTarget == null)
            return;
            
        float dx = PlayerTarget.X - Position.X;
        float dy = PlayerTarget.Y - Position.Y;
        
        float distance = MathF.Sqrt(dx * dx + dy * dy);
        if (distance < 0.1f) return;
        
        float ndx = dx / distance;
        float ndy = dy / distance;
        
        float moveSpeed = FloatSpeed * (float)deltaTimeSeconds;
        _basePosition.X += ndx * moveSpeed;
        _basePosition.Y += ndy * moveSpeed;
        
        float timeSinceSpawn = (float)(DateTimeOffset.Now - _spawnTime).TotalSeconds;
        float verticalOffset = MathF.Sin(timeSinceSpawn * _bobSpeed) * _bobHeight;
        
        Position = ((int)_basePosition.X, (int)(_basePosition.Y + verticalOffset));
    }
    
    public bool Pickup()
    {
        if (IsPickedUp || !IsActive)
            return false;
            
        IsPickedUp = true;
        IsActive = false;
        
        if (PlayerTarget != null)
        {
            PlayerTarget.RestoreHealth(HealAmount);
            return true;
        }
        
        return false;
    }
    
    public override void Render(GameRenderer renderer)
    {
        if (!IsActive || IsPickedUp)
            return;
            
        float timeFactor = (float)Math.Sin((DateTimeOffset.Now - _spawnTime).TotalSeconds * _pulseSpeed) * _pulseAmount + 1.0f;
        
        SpriteSheet.Render(
            renderer, 
            Position, 
            0.0
        );
        
    }
}