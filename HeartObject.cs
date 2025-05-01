using TheAdventure.Models;

namespace TheAdventure;

public class HeartObject : RenderableGameObject
{
    public HeartObject(SpriteSheet spriteSheet, (int X, int Y) position)
        : base(spriteSheet, position) { }
}


