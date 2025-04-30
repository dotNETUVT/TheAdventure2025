using Silk.NET.Maths;
using Silk.NET.SDL;

namespace TheAdventure.Models;

public class BombObject : TemporaryGameObject
{
    public int Damage { get; init; } = 30;
    public float ExplosionRadius { get; init; } = 120f;
    
    private bool _hasDamaged = false;
    private float _explosionTiming = 0.8f;
    
    public BombObject(SpriteSheet spriteSheet, double ttl, (int X, int Y) position, 
                      int damage = 30, float radius = 120f,
                      double angle = 0.0, Point rotationCenter = new())
        : base(spriteSheet, ttl, position, angle, rotationCenter)
    {
        Damage = damage;
        ExplosionRadius = radius;
    }
    
    public bool ShouldDamage()
    {
        if (_hasDamaged) return false;
        
        double elapsedTime = (DateTimeOffset.Now - _spawnTime).TotalSeconds;
        bool shouldDamage = elapsedTime >= (Ttl * _explosionTiming) && !_hasDamaged;
        
        if (shouldDamage)
        {
            _hasDamaged = true;
        }
        
        return shouldDamage;
    }
    
    public bool IsInExplosionRange(Vector2D<int> position)
    {
        int dx = position.X - Position.X;
        int dy = position.Y - Position.Y;
        float distance = MathF.Sqrt(dx * dx + dy * dy);
        
        return distance <= ExplosionRadius;
    }
}