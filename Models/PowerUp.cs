using Silk.NET.Maths;
using TheAdventure.Models;

namespace TheAdventure.Models
{
    public abstract class PowerUp : RenderableGameObject
    {
        public bool IsCollected { get; protected set; } = false;

        protected PowerUp(SpriteSheet spriteSheet, (int X, int Y) position)
            : base(spriteSheet, position)
        {
        }

        public abstract void ApplyEffect(PlayerObject player);

        public virtual void Collect()
        {
            IsCollected = true;
        }
    }
}