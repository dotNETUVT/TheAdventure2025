using Silk.NET.Maths;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAdventure.Models
{
    class PlayerObject2 : GameObject
    {
        public int X { get; set; } = 100;
        public int Y { get; set; } = 100;

        private Rectangle<int> _source = new(10, 50, 200, 200);
        private Rectangle<int> _target = new(0, 0, 48, 48);
        private Rectangle<int> _worldBounds;

        private readonly int _textureId;

        private const int Speed = 80; // pixels per second
        private enum Direction { Down, Up, Left, Right }
        private Direction _currentDirection = Direction.Down;

        private int _directionSign = 1;

        private int _lives = 3;
        private List<int> _heartTextures = new List<int>(); 

        private bool _gameOver = false;
        private Random _random = new();
        private double _directionChangeCooldown = 0;

        private int _gameOverTextureId = -1;
        private Rectangle<int> _gameOverSourceRect;

        public PlayerObject2(GameRenderer renderer, Rectangle<int> worldBounds)
        {
            _worldBounds = worldBounds;
            _textureId = renderer.LoadTexture(Path.Combine("Assets", "WolfSpriteSheet.png"), out _);
            _gameOverTextureId = renderer.LoadTexture(Path.Combine("Assets", "GirlWon.png"), out var gameOverSize);
            _gameOverSourceRect = new Rectangle<int>(0, 0, 3000, 3000);

            if (_textureId < 0)
            {
                throw new Exception("Failed to load wolf texture");
            }

            _heartTextures.Add(renderer.LoadTexture(Path.Combine("Assets", "Heart.png"), out _));

            UpdateTarget();
        }

        public void Update(int time)
        {
            if (_gameOver) return;
            var deltaSeconds = time / 1000.0;
            var pixelsToMove = Speed * (time / 1000.0);
            _damageCooldown -= deltaSeconds;

            _directionChangeCooldown -= deltaSeconds;
            if (_directionChangeCooldown <= 0)
            {
                _currentDirection = (Direction)_random.Next(0, 4);
                _directionChangeCooldown = 1 + _random.NextDouble() * 2; 
            }

            switch (_currentDirection)
            {
                case Direction.Up: Y -= (int)pixelsToMove; break;
                case Direction.Down: Y += (int)pixelsToMove; break;
                case Direction.Left: X -= (int)pixelsToMove; break;
                case Direction.Right: X += (int)pixelsToMove; break;
            }

            if (X <= 0 || X >= _worldBounds.Size.X - _target.Size.X ||
                Y <= 0 || Y >= _worldBounds.Size.Y - _target.Size.Y)
            {
                _currentDirection = (Direction)_random.Next(0, 4);
            }

            X = Math.Clamp(X, 0, _worldBounds.Size.X - _target.Size.X);
            Y = Math.Clamp(Y, 0, _worldBounds.Size.Y - _target.Size.Y);

            UpdateTarget();
            UpdateSource();
        }

        public void Render(GameRenderer renderer)
        {
            if (_gameOver)
            {
                var destRect = new Rectangle<int>(0, 0, _worldBounds.Size.X, _worldBounds.Size.Y);
                renderer.RenderTexture(_gameOverTextureId, _gameOverSourceRect, destRect);
            }
            else
            {
                var flip = _currentDirection == Direction.Left ? RendererFlip.FlipHorizontal : RendererFlip.None;
                renderer.RenderTexture(_textureId, _source, _target, flip);

                for (int i = 0; i < _lives; i++)
                {
                    var heartX = _worldBounds.Size.X - (i + 1) * 40;
                    var heartY = 10;
                    renderer.RenderTexture(_heartTextures[0], new Rectangle<int>(0, 0, 32, 32), new Rectangle<int>(heartX, heartY, 32, 32));
                }
            }
        }

        private void UpdateTarget()
        {
            _target = new(X, Y, 48, 48);
        }

        private void UpdateSource()
        {
            var sideRect = new Rectangle<int>(10, 800, 200, 200);
            _source = _currentDirection switch
            {
                Direction.Down => new Rectangle<int>(10, 40, 200, 200),
                Direction.Up => new Rectangle<int>(250, 40, 200, 200),
                Direction.Left => sideRect,
                Direction.Right => sideRect,
                _ => _source
            };
        }
        public void TakeDamage()
        {
            if (_lives > 0)
            {
                _lives--;
                _damageCooldown = 1.0;
                if (_lives == 0)
                {
                    _gameOver = true;
                }
            }
        }
        public Rectangle<int> GetBounds()
        {
            //return _target;
            return new Rectangle<int>(X, Y, 48, 48);
        }

        private double _damageCooldown = 0;

        public bool CanTakeDamage()
        {
            return _damageCooldown <= 0;
        }

    }
}
