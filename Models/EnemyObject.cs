using Silk.NET.Maths;

namespace TheAdventure.Models
{
    public class EnemyObject : RenderableGameObject
    {
        private const int PatrolDistance = 100;
        private const int DetectionRange = 150;
        private const int AttackRadius   = 2;
        private const int KillRadius     = 4;

        private readonly int _speed;
        private readonly Vector2D<int> _origin;
        private bool _patrolForward = true;

        public bool IsChasing { get; private set; }

        public EnemyObject(SpriteSheet sheet, (int X, int Y) pos, int speed = 64)
            : base(sheet, pos)
        {
            _speed = speed;
            _origin = new Vector2D<int>(pos.X, pos.Y);

            SpriteSheet.ActivateAnimation("Idle");
        }

        public void Update((int X, int Y) playerPos, double deltaMs)
        {
            var p   = new Vector2D<int>(Position.X, Position.Y);
            var dx  = playerPos.X - p.X;
            var dy  = playerPos.Y - p.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist <= DetectionRange)
            {
                if (SpriteSheet.ActiveAnimation?.Loop == false ||
                    SpriteSheet.ActiveAnimation is not null &&
                    SpriteSheet.ActiveAnimation != SpriteSheet.Animations["Walk"])
                {
                    SpriteSheet.ActivateAnimation("Walk");
                }

                IsChasing = true;
                if (Math.Abs(dx) > Math.Abs(dy))
                    p.X += Math.Sign(dx) * (int)(_speed * (deltaMs / 1000));
                else
                    p.Y += Math.Sign(dy) * (int)(_speed * (deltaMs / 1000));
            }
            else
            {
                // ← switch back to Idle if needed
                if (SpriteSheet.ActiveAnimation?.Loop == false ||
                    SpriteSheet.ActiveAnimation is not null &&
                    SpriteSheet.ActiveAnimation != SpriteSheet.Animations["Idle"])
                {
                    SpriteSheet.ActivateAnimation("Idle");
                }

                IsChasing = false;
                var step = (int)(_speed * (deltaMs / 1000));
                p.X += _patrolForward ? step : -step;
                if (p.X >  _origin.X + PatrolDistance) _patrolForward = false;
                if (p.X <  _origin.X - PatrolDistance) _patrolForward = true;
            }

            Position = (p.X, p.Y);
        }

        public bool CanAttackPlayer((int X, int Y) playerPos)
        {
            var dx = Position.X - playerPos.X;
            var dy = Position.Y - playerPos.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= AttackRadius;
        }

        public bool CanBeKilledByPlayer((int X,int Y) playerPos, double killRadius)
        {
            var dx = Position.X - playerPos.X;
            var dy = Position.Y - playerPos.Y;
            return Math.Sqrt(dx*dx + dy*dy) <= killRadius;
        }
    }
}
