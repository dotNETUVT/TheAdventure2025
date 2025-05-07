using Silk.NET.SDL;

namespace TheAdventure.Models;

public class RenderableGameObject : GameObject
{
    public SpriteSheet SpriteSheet { get; set; }
    public (int X, int Y) Position { get; set; }
    public double Angle { get; set; }
    public Point RotationCenter { get; set; }

    // Allow specific object classes to define their own scale
    public virtual float Scale => 1.0f;

    public RenderableGameObject(SpriteSheet spriteSheet, (int X, int Y) position, double angle = 0.0,
        Point rotationCenter = new())
        : base(position)
    {
        SpriteSheet = spriteSheet;
        Position = position;
        Angle = angle;
        RotationCenter = rotationCenter;
    }

    public virtual void Render(GameRenderer renderer)
    {
        int scaledWidth = (int)(SpriteSheet.FrameWidth * Scale);
        int scaledHeight = (int)(SpriteSheet.FrameHeight * Scale);

        SpriteSheet.Render(renderer, Position, Angle, RotationCenter, scaledWidth, scaledHeight);
    }
}
