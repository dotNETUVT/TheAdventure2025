using Silk.NET.Maths;
using Silk.NET.SDL;
using System;

namespace TheAdventure.Models
{
    public class PlayerObjectEnemy : GameObject
    {
        public int X { get; set; } = 200;
        public int Y { get; set; } = 200;

        private Rectangle<int> _source = new(0, 0, 48, 48);
        private Rectangle<int> _target = new(0, 0, 48, 48);

        private readonly int _textureId;

        private const int Speed = 70;

        public int Health { get; set; } = 2;

        private Direction _direction = Direction.Down;
        private int _frame = 0;
        private double _frameTimer = 0;
        private const int FrameWidth = 58;
        private const int FrameHeight = 68;
        private const int FrameCount = 4;
        private const double FrameDuration = 0.2;

        private bool _isDead = false;
        private int _deathFrame = 0;
        private double _deathAnimationTimer = 0;
        private const int DeathFrameCount = 3;
        private const double DeathFrameDuration = 0.3;

        private bool _isAttacking = false;
        private int _attackFrame = 0;
        private double _attackTimer = 0;
        private const int AttackFrameCount = 3;
        private const double AttackFrameDuration = 0.15;


        public bool IsDead => _isDead;
        public bool IsDeathAnimationFinished => _deathFrame == DeathFrameCount - 1 && _deathAnimationTimer >= DeathFrameDuration;

        private double _movementTimer = 0;
        private const double MovementDuration = 1.5;
        private readonly Random _random = new();

        private bool _flipHorizontal = false;

        public Rectangle<int> WorldBounds { get; set; }



        public enum Direction
        {
            Down = 0,
            Left = 3,
            Right = 1,
            Up = 2
        }



        public PlayerObjectEnemy(GameRenderer renderer)
        {
            _textureId = renderer.LoadTexture(Path.Combine("Assets", "enemy.png"), out _);
            if (_textureId < 0)
            {
                throw new Exception("Failed to load enemy texture");
            }

            UpdateTarget();
        }

        public void UpdatePosition(int time)
        {
            if (_isDead)
            {
                _deathAnimationTimer += time / 2000.0;
                if (_deathAnimationTimer >= DeathFrameDuration && _deathFrame < DeathFrameCount - 1)
                {
                    _deathFrame++;
                    _deathAnimationTimer = 0;
                }

                UpdateDeathSource();
                return;
            }

            _movementTimer -= time / 1000.0;
            if (_movementTimer <= 0)
            {
                ChooseRandomDirection();
                _movementTimer = MovementDuration;
            }

            MoveInCurrentDirection(time);
        }

        private void ChooseRandomDirection()
        {
            int dir = _random.Next(0, 4);
            _direction = (Direction)dir;
        }


        private void MoveInCurrentDirection(int time)
        {
            var pixelsToMove = Speed * (time / 1000.0);
            bool moved = false;

            int newX = X;
            int newY = Y;

            switch (_direction)
            {
                case Direction.Up:
                    newY -= (int)pixelsToMove;
                    break;
                case Direction.Down:
                    newY += (int)pixelsToMove;
                    break;
                case Direction.Left:
                    newX -= (int)pixelsToMove;
                    break;
                case Direction.Right:
                    newX += (int)pixelsToMove;
                    break;
            }

            int scalePercent = 65;
            int width = FrameWidth * scalePercent / 100;
            int height = FrameHeight * scalePercent / 100;

            if (newX >= WorldBounds.Origin.X &&
                newX + width <= WorldBounds.Origin.X + WorldBounds.Size.X  &&
                newY >= WorldBounds.Origin.Y &&
                newY + height <= WorldBounds.Origin.Y + WorldBounds.Size.Y)
            {
                X = newX;
                Y = newY;
                moved = true;
            }

            UpdateTarget();

            if (moved)
            {
                _frameTimer += time / 1000.0;
                if (_frameTimer >= FrameDuration)
                {
                    _frame = (_frame + 1) % FrameCount;
                    _frameTimer = 0;
                }
            }
            else
            {
                _frame = 0;
            }

            UpdateSource();
        }


        public void Render(GameRenderer renderer)
        {

            renderer.RenderTexture(
            _textureId,
            _source,
            _target,
            _flipHorizontal ? RendererFlip.Horizontal : RendererFlip.None
);

        }

        private void UpdateTarget()
        {
            int scalePercent = 65;
            int width = FrameWidth * scalePercent / 100;
            int height = FrameHeight * scalePercent / 100;
            _target = new(X , Y , width, height);
        }


        private void UpdateSource()
        {
            if (_isAttacking)
            {
                UpdateAttackSource();
                return;
            }

            int frameX = 0;
            int frameY = 0;

            switch (_direction)
            {
                case Direction.Down:
                    frameX = 2;
                    frameY = 0;
                    _flipHorizontal = false;
                    break;
                case Direction.Up:
                    frameX = 0;
                    frameY = 0;
                    _flipHorizontal = false;
                    break;
                case Direction.Right:
                    frameX = 1;
                    frameY = 2;
                    _flipHorizontal = false;
                    break;
                case Direction.Left:
                    frameX = 1;
                    frameY = 2;
                    _flipHorizontal = true;
                    break;
            }

            _source = new Rectangle<int>(
                frameX * FrameWidth,
                frameY * FrameHeight,
                FrameWidth,
                FrameHeight
            );
        }



        public void FollowPlayer(PlayerObject player, int time)
        {
            if (_isDead)
            {
                return;
            }

            int dx = player.X - X;
            int dy = player.Y - Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance < 15)
            {
                _isAttacking = true;
                _attackTimer += time / 1000.0;

                if (_attackTimer >= AttackFrameDuration)
                {
                    _attackFrame = (_attackFrame + 1) % AttackFrameCount;
                    _attackTimer = 0;
                }

                UpdateAttackSource();
                return;
            }
            else
            {

                _isAttacking = false;
                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    if (dx > 0)
                        _direction = Direction.Right;
                    else
                        _direction = Direction.Left;
                }
                else
                {
                    if (dy > 0)
                        _direction = Direction.Down;
                    else
                        _direction = Direction.Up;
                }

                double distToMove = Speed * (time / 1000.0);

                double vectorLength = Math.Sqrt(dx * dx + dy * dy);
                if (vectorLength > 0)
                {
                    double normX = dx / vectorLength;
                    double normY = dy / vectorLength;

                    X += (int)(normX * distToMove);
                    Y += (int)(normY * distToMove);
                }

                UpdateTarget();
                UpdateSource();
            }
        }

        private void UpdateAttackSource()
        {
            int attackRow = 3; 
            int attackStartColumn = 1; 

            _source = new Rectangle<int>(
                (attackStartColumn + _attackFrame) * FrameWidth,
                attackRow * FrameHeight,
                FrameWidth,
                FrameHeight
            );
        }

        private static readonly (int X, int Y)[] DeathFrames = new[]
        {
            (0, 9),
            (1, 9),
            (2, 9)
        };

        private void UpdateDeathSource()
        {
            var (frameX, frameY) = DeathFrames[_deathFrame];
            _source = new Rectangle<int>(
                frameX * FrameWidth,
                frameY * FrameHeight,
                FrameWidth,
                FrameHeight
            );
        }
    }
}
