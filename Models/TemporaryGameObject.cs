using Silk.NET.SDL;

namespace TheAdventure.Models;

public class TemporaryGameObject : RenderableGameObject
{
    public double Ttl { get; init; }
    public bool IsExpired => (DateTimeOffset.Now - _spawnTime).TotalSeconds >= Ttl;
    
    private DateTimeOffset _spawnTime;
    
    public TemporaryGameObject(SpriteSheet spriteSheet, double ttl, (int X, int Y) position, string type, double angle = 0.0, Point rotationCenter = new())
        : base(spriteSheet, position, type, angle, rotationCenter)
    {
        Ttl = ttl;
        _spawnTime = DateTimeOffset.Now;
    }
}