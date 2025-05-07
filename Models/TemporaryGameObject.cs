using Silk.NET.SDL;

namespace TheAdventure.Models;

public class TemporaryGameObject : RenderableGameObject
{
    private readonly double _lifetime;
    private double _elapsed;
    public bool IsExpired => _elapsed >= _lifetime;
    
    public bool IsExploding => _elapsed >= (_lifetime * 0.6) && _elapsed <= (_lifetime * 0.7);

    public TemporaryGameObject(SpriteSheet spriteSheet, double lifetime, (int X, int Y) position) 
        : base(spriteSheet, position)
    {
        _lifetime = lifetime;
        _elapsed = 0;
    }

    public override void Render(GameRenderer renderer)
    {
        base.Render(renderer);
        _elapsed += 0.016;
    }
}