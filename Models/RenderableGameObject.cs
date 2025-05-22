using Silk.NET.SDL;
using Silk.NET.Maths;

namespace TheAdventure.Models;

public class RenderableGameObject : GameObject
{
    public SpriteSheet SpriteSheet { get; set; }
    public (int X, int Y) Position { get; set; }
    public double Angle { get; set; }
    public Point RotationCenter { get; set; }

    public RenderableGameObject(SpriteSheet spriteSheet, (int X, int Y) position, double angle = 0.0,
        Point rotationCenter = new())
        : base()
    {
        SpriteSheet = spriteSheet;
        Position = position;
        Angle = angle;
        RotationCenter = rotationCenter;
    }

    public virtual void Render(GameRenderer renderer)
    {
        SpriteSheet.Render(renderer, Position, Angle, RotationCenter);
    }

    public virtual Rectangle<int> GetBoundingBox()
    {
        int topLeftX = Position.X - SpriteSheet.FrameCenter.OffsetX;
        int topLeftY = Position.Y - SpriteSheet.FrameCenter.OffsetY;
        return new Rectangle<int>(topLeftX, topLeftY, SpriteSheet.FrameWidth, SpriteSheet.FrameHeight);
    }
}