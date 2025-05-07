using Silk.NET.Input;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure;

public class Fireball : RenderableGameObject
{
    private readonly double _ttl;
    private readonly DateTimeOffset _created = DateTimeOffset.Now;
    private readonly Vector2D<double> _velocity;
    private readonly double _angle;

    public bool IsExpired => (DateTimeOffset.Now - _created).TotalSeconds > _ttl;

    public Fireball(SpriteSheet spriteSheet, double ttl, (int x, int y) start, Vector2D<double> direction, double speed)
        : base(spriteSheet, start)
    {
        var length = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        if (length > 0) direction /= length;
        _velocity = direction * speed;
        _ttl = ttl;

        _angle = Math.Atan2(direction.Y, direction.X) * (180.0 / Math.PI);
    }

    public void Update(int msSinceLastUpdate)
    {
        double seconds = msSinceLastUpdate / 1000.0;
        Position = (
            Position.X + (int)(_velocity.X * seconds),
            Position.Y + (int)(_velocity.Y * seconds)
        );
    }

    public override void Render(GameRenderer renderer)
    {
        SpriteSheet.Render(renderer, Position, _angle);
    }
}
