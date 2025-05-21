using TheAdventure.Models;
using Silk.NET.Maths;
using Silk.NET.SDL; 

namespace TheAdventure.Models.Data
{
    public class CoinObject : TemporaryGameObject
    {
        public CoinObject(SpriteSheet spriteSheet, (int X, int Y) position)
            : base(spriteSheet, double.MaxValue, position)
        {
        }

        public override void Render(GameRenderer renderer)
        {
            float scale = 0.3f; 
            int width = (int)(SpriteSheet.FrameWidth * scale);
            int height = (int)(SpriteSheet.FrameHeight * scale);

            var dest = (Position.X, Position.Y);

            var rotationCenter = new Point { X = width / 2, Y = height / 2 };

            SpriteSheet.Render(
                renderer,
                dest,
                angle: 0.0,
                rotationCenter: rotationCenter
            );
        }
    }
}
