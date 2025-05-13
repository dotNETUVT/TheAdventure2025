using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAdventure.Models
{
    class Blueberry : TemporaryGameObject
    {
        private const int Speed = 300;
        private int _directionX;
        private int _directionY;

        public Blueberry(GameRenderer renderer, (int x, int y) startPos, (int dx, int dy) direction)
            : base(new SpriteSheet(renderer, Path.Combine("Assets", "Blueberry.png"), 1, 1, 10, 10, (5, 5)), 5.0, startPos)
        {
            _directionX = direction.dx;
            _directionY = direction.dy;
        }

        public override void Update(int ms)
        {
            double move = Speed * (ms / 1000.0);
            Position = (Position.X + (int)(move * _directionX), Position.Y + (int)(move * _directionY));

        }
        public Rectangle<int> GetBounds()
        {
            return new Rectangle<int>(Position.X, Position.Y, 10, 10);
        }

    }
}
