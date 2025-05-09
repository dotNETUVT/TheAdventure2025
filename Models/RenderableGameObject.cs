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
}

    public class StaticGameObject : RenderableGameObject
{
    private readonly int _textureId;
    private readonly int _width;
    private readonly int _height;

    public StaticGameObject(int textureId, int x, int y, int width, int height)
        : base(null!, (x, y))  // null! is ugly, but we override everything
    {
        _textureId = textureId;
        _width = width;
        _height = height;
    }

    public override void Render(GameRenderer renderer)
    {
        var src = new Rectangle<int>(0, 0, _width, _height);
        var dst = new Rectangle<int>(Position.X, Position.Y, _width, _height);
        renderer.RenderTexture(_textureId, src, dst);
    }
}

