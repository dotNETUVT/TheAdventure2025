using Silk.NET.Maths;

namespace TheAdventure.Models;

public class DamagingFlower : RenderableGameObject
{
    public DamagingFlower(SpriteSheet spriteSheet, (int X, int Y) position)
        : base(spriteSheet, position)
    {
    }

    public override void Render(GameRenderer renderer)
    {
        var srcRect = new Rectangle<int>(3 * 32, 5 * 32, 32, 32);
        var dstRect = new Rectangle<int>(
            Position.X - SpriteSheet.FrameCenter.OffsetX,
            Position.Y - SpriteSheet.FrameCenter.OffsetY,
            SpriteSheet.FrameWidth,
            SpriteSheet.FrameHeight
        );

        renderer.RenderTexture(SpriteSheet.TextureId, srcRect, dstRect);
    }
}