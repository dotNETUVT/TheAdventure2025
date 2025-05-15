using TheAdventure.Models;
using System;

namespace TheAdventure.Models
{
    public class RobotObject : RenderableGameObject
    {
        private const int _speed = 80;   
        public RobotObject(SpriteSheet sheet, (int X, int Y) pos)
            : base(sheet, pos)
        {
            sheet.ActivateAnimation("MoveDown");
        }

        public void UpdateTowards((int X, int Y) target, double elapsedMs)
        {
            var dx = target.X - Position.X;
            var dy = target.Y - Position.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 1) return;      

            var pixels = _speed * (elapsedMs / 1000.0);
            var step = Math.Min(1, pixels / dist);

            Position = (
                Position.X + (int)(dx * step),
                Position.Y + (int)(dy * step)
            );

            if (Math.Abs(dx) > Math.Abs(dy))
                SpriteSheet.ActivateAnimation(dx > 0 ? "MoveRight" : "MoveLeft");
            else
                SpriteSheet.ActivateAnimation(dy > 0 ? "MoveDown" : "MoveUp");
        }
    }
}
