using TheAdventure.Models;

namespace TheAdventure
{

    public class GemObject : RenderableGameObject
    {
        public int Value { get; }

        public GemObject(SpriteSheet sheet, (int X, int Y) position, int value = 1)
            : base(sheet, position)
        {
            Value = value;
        }
    }
}