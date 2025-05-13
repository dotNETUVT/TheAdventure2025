using Silk.NET.SDL;
using System;

namespace TheAdventure.Models
{
    public class EnemyObject : RenderableGameObject
    {
        private const double _speed = 64.0;
        public bool IsDead { get; private set; }

        private double _x;
        private double _y;

        private SpriteSheet _spriteSheet; 

        public EnemyObject(SpriteSheet sheet, (int X, int Y) start)
            : base(sheet, start)
        {
            _x = start.X;
            _y = start.Y;

            _spriteSheet = sheet;
            _spriteSheet.ActivateAnimation("Move");
        }

        public void Update((int X, int Y) playerPos, double deltaMs)
        {
            if (IsDead) return;

            var dx = playerPos.X - _x;
            var dy = playerPos.Y - _y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 1) return;

            var nx = dx / dist;
            var ny = dy / dist;
            var move = _speed * (deltaMs / 1000.0);

            _x += nx * move;
            _y += ny * move;
        }

        public override void Render(GameRenderer renderer)
        {
            _spriteSheet.RenderScaled(renderer, ((int)_x, (int)_y), 2.0); 
        }

        public (int X, int Y) GetPosition()
        {
            return ((int)_x, (int)_y);
        }

        public void Kill()
        {
            IsDead = true;
        }
    }


}
