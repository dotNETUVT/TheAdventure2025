using Silk.NET.SDL;

namespace TheAdventure.Models;

public class TemporaryGameObject : RenderableGameObject
{
    public double Ttl { get; init; }
    public bool IsExpired => (DateTimeOffset.Now - _spawnTime).TotalSeconds >= Ttl;
    
    private DateTimeOffset _spawnTime;
    
    public TemporaryGameObject(SpriteSheet spriteSheet, double ttl, (int X, int Y) position, double angle = 0.0, Point rotationCenter = new())
        : base(spriteSheet, position, angle, rotationCenter)
    {
        Ttl = ttl;
        _spawnTime = DateTimeOffset.Now;
    }

    public override void Render(GameRenderer renderer, bool paused = false)
    {
        base.Render(renderer, paused);
        var elapsed  = (DateTimeOffset.Now - _spawnTime).TotalSeconds;
        var tperc = 1.0 - Math.Clamp(elapsed / Ttl, 0.0, 1.0);
        var alpha = (byte)(tperc*tperc * 255);

        renderer.SetTextureAlpha(SpriteSheet.TextureId, alpha);
        base.Render(renderer, paused);
        renderer.SetTextureAlpha(SpriteSheet.TextureId, 255);
    }
}