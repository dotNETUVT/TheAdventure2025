using Silk.NET.Maths;

namespace TheAdventure.Models
{
    public class TrainingDummyObject : RenderableGameObject
    {
        private int _health = 3;

        public TrainingDummyObject(SpriteSheet spriteSheet, int x, int y)
            : base(spriteSheet, (x, y))
        {
        }

        public void TakeDamage()
        {
            _health--;
            if (_health <= 0)
            {
                IsExpired = true;
            }
        }
    }
}
