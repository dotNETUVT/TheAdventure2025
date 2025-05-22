using TheAdventure.Models;

namespace TheAdventure.Models;

public class FlowerObject : RenderableGameObject
{
    public FlowerObject(SpriteSheet spriteSheet, (int X, int Y) position)
        : base(spriteSheet, position)
    {
    }

    public override void Render(GameRenderer renderer)
    {
        base.Render(renderer);
    }
}
