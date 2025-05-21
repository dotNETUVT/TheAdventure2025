using Silk.NET.Maths;

namespace TheAdventure.Models;


public class BombGameObject : TemporaryGameObject
{
    public Vector2D<double> Velocity { get; set; }  // px ⁄ s

    public BombGameObject(SpriteSheet sheet, double ttl, (int X,int Y) pos)
        : base(sheet, ttl, pos) { }

    public void Update(double elapsedMs)
    {
        if (Velocity == default) return;
        Position = (
            Position.X + (int)(Velocity.X * elapsedMs / 1000.0),
            Position.Y + (int)(Velocity.Y * elapsedMs / 1000.0)
        );
    }
}