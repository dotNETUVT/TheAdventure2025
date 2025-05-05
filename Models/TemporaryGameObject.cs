using Silk.NET.SDL;
using System;
using System.Numerics;

namespace TheAdventure.Models
{
    public class TemporaryGameObject : RenderableGameObject
    {
        public double Ttl { get; private set; }
        public bool IsExpired => (DateTimeOffset.Now - _spawnTime).TotalSeconds >= Ttl;
        public bool IsFlying => _isFlying;

        private DateTimeOffset _spawnTime;
        private bool _isFlying = false;

        private Vector2 _startPos;
        private Vector2 _direction;     // unit vector from player→bomb
        private Vector2 _perp;          // perpendicular for curve
        private float _speed;           // px/sec
        private double _elapsed;
        private double _flightTime;     // total seconds to fly

        // distance from which bombs are affected by player attack
        private const float ThrowDistanceThreshold = 50f;

        public TemporaryGameObject(SpriteSheet spriteSheet, double ttl, (int X, int Y) position,
                                   double angle = 0.0, Point rotationCenter = new())
            : base(spriteSheet, position, angle, rotationCenter)
        {
            Ttl = ttl;
            _spawnTime = DateTimeOffset.Now;
        }

        public void Launch(Vector2 playerPos, Vector2 playerFacing)
        {
            var bombPos = new Vector2(Position.X, Position.Y);
            var toBomb = bombPos - playerPos;

            // check distance
            if (toBomb.LengthSquared() > ThrowDistanceThreshold * ThrowDistanceThreshold)
                return;

            // check angle (dot product test for 180°)
            if (toBomb != Vector2.Zero)
            {
                Vector2 dirToBomb = Vector2.Normalize(toBomb);
                Vector2 facingNorm = playerFacing == Vector2.Zero ? new Vector2(0, -1) : Vector2.Normalize(playerFacing);

                float dot = Vector2.Dot(facingNorm, dirToBomb);
                if (dot < 0f)
                    return; // bomb is behind the player
            }

            // start flight
            _isFlying = true;
            _elapsed = 0.0;
            _flightTime = 0.5;
            _speed = 170f;
            _startPos = bombPos;

            _direction = Vector2.Normalize(toBomb);
            _perp = new Vector2(-_direction.Y, _direction.X);
        }


        public void Update(double deltaTime)
        {
            if (!_isFlying)
                return;

            _elapsed += deltaTime;
            if (_elapsed >= _flightTime)
            {
                _isFlying = false;
                return;
            }

            float t = (float)(_elapsed / _flightTime); // progress 0→1
            Vector2 linear = _direction * _speed * (float)deltaTime;

            float curveStrength = 30f; // max offset on curve
            float curveAmount = curveStrength * (4 * t * (1 - t)); // parabola

            Vector2 pos = new Vector2(Position.X, Position.Y)
                          + linear
                          + _perp * curveAmount * (float)deltaTime;

            Position = ((int)pos.X, (int)pos.Y);
        }
    }
}
