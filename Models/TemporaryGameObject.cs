using Silk.NET.SDL;

namespace TheAdventure.Models;

public class TemporaryGameObject : RenderableGameObject
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public double Ttl { get; init; }
    public bool IsExpired => (DateTimeOffset.Now - _spawnTime).TotalSeconds >= Ttl;
    
    private DateTimeOffset _spawnTime;
    
    public TemporaryGameObject(SpriteSheet spriteSheet, double ttl, (int X, int Y) position, double angle = 0.0, Point rotationCenter = new())
        : base(spriteSheet, position, angle, rotationCenter)
    {
        X = position.X;
        Y = position.Y;
        Ttl = ttl;
        _spawnTime = DateTimeOffset.Now;
    }
}