using Silk.NET.SDL;

namespace TheAdventure.Models;
using Silk.NET.Maths;

public class RenderableGameObject : GameObject
{
    public SpriteSheet SpriteSheet { get; set; }
    public double Angle { get; set; }
    public Point RotationCenter { get; set; }

    public RenderableGameObject(SpriteSheet spriteSheet, (float X, float Y) position, double angle = 0.0, Point rotationCenter = new())
        : base()
    {
        SpriteSheet = spriteSheet;
        Position = new Vector2D<float>(position.X, position.Y); // Set base Position
        Angle = angle;
        RotationCenter = rotationCenter;
    }

    // Update Render method to use base Position
    public virtual void Render(GameRenderer renderer)
    {
        var renderPosition = ((int)Position.X, (int)Position.Y);
        SpriteSheet.Render(renderer, renderPosition, Angle, RotationCenter);
    }
}