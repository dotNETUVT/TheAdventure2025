using Silk.NET.SDL;

namespace TheAdventure.Models;

public class TemporaryGameObject : RenderableGameObject
{
    public double Ttl { get; init; }
    public bool IsExpired => (DateTimeOffset.Now - _spawnTime).TotalSeconds >= Ttl;
    
    private DateTimeOffset _spawnTime;
    public bool IsExploding { get; private set; } = false;
    public double ExplosionRadius { get; set; } = 10;


    public double TimeRemaining => Ttl - (DateTimeOffset.Now - _spawnTime).TotalSeconds;

    public void Update()
    {

        if (!IsExploding && TimeRemaining < 0.5)
        {
            IsExploding = true;
        }
    }


    public TemporaryGameObject(SpriteSheet spriteSheet, double ttl, (int X, int Y) position, double angle = 0.0,
                               Point rotationCenter = new(), double explosionRadius = 100)
        : base(spriteSheet, position, angle, rotationCenter)
    {
        Ttl = ttl;
        _spawnTime = DateTimeOffset.Now;
        ExplosionRadius = explosionRadius;
    }
}