using Silk.NET.Maths;
using TheAdventure.Models;
using System;
using Silk.NET.SDL;
using TheAdventure;

public class Enemy : RenderableGameObject
{
    private static readonly Random _random = new();
    private (int X, int Y) _spawnPoint;
    private Vector2D<double> _direction = new(0, 0);
    private (int X, int Y) _target;
    private double _speed = 100.0;
    private double _detectionRadius = 120.0;
    private DateTimeOffset _lastTargetSet = DateTimeOffset.Now;
    private bool _isChasing = false;
    private Direction _currentDirection = Direction.Left;
    private SpriteSheet _spriteSheet;

    // Pathing state
    private (int X, int Y) _lastPosition;
    private double _distanceTraveled = 0.0;
    private const double MinDistance = 50.0; 
    private bool _isMovingHorizontally = true; 
    public override float Scale => 2.5f;

    public Enemy(SpriteSheet spriteSheet, (int x, int y) position)
        : base(spriteSheet, position)
    {
        _spawnPoint = position;
        _target = position;
        _lastPosition = position;
        _spriteSheet = spriteSheet;

        _spriteSheet.Animations["WalkLeft"] = new SpriteSheet.Animation
        {
            StartFrame = new SpriteSheet.Position { Row = 0, Col = 1 },
            EndFrame = new SpriteSheet.Position { Row = 0, Col = 7 },
            DurationMs = 600,
            Loop = true
        };
        _spriteSheet.Animations["WalkRight"] = new SpriteSheet.Animation
        {
            Flip = RendererFlip.Horizontal
        };

        _spriteSheet.ActivateAnimation("WalkLeft");
        PickNewWanderTarget();
    }

    public void Update(int msSinceLastUpdate, (int X, int Y) playerPos)
    {
        double seconds = msSinceLastUpdate / 1000.0;

        var toPlayer = new Vector2D<double>(playerPos.X - Position.X, playerPos.Y - Position.Y);
        var distanceToPlayer = toPlayer.Length;

        if (distanceToPlayer < _detectionRadius)
        {
            _isChasing = true;
            _target = playerPos;
        }
        else if (_isChasing)
        {
            _isChasing = false;
            PickNewWanderTarget();
        }

        var toTarget = new Vector2D<double>(_target.X - Position.X, _target.Y - Position.Y);
        if (toTarget.Length < 2)
        {
            if (!_isChasing)
            {
                PickNewWanderTarget();
            }
            else
            {
                _direction = new Vector2D<double>(0, 0);
            }
            _distanceTraveled = 0;
        }

        double deltaX = Position.X - _lastPosition.X;
        double deltaY = Position.Y - _lastPosition.Y;
        _distanceTraveled += Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        _lastPosition = Position;

        if (_distanceTraveled >= MinDistance || toTarget.Length < MinDistance)
        {
            UpdatePathing(toTarget);
            _distanceTraveled = 0;
        }

        var newX = Position.X + (int)(_direction.X * _speed * seconds);
        var newY = Position.Y + (int)(_direction.Y * _speed * seconds);

        bool isMoving = Math.Abs(_direction.X) > 0.01 || Math.Abs(_direction.Y) > 0.01;
        var anim = isMoving ? $"Walk{_currentDirection}" : $"Idle{_currentDirection}";
        _spriteSheet.ActivateAnimation(anim);

        var currentAnim = _spriteSheet.Animations[anim];
        Console.WriteLine($"Enemy Animation: {anim}, Frame: ({currentAnim.StartFrame.Row}, {currentAnim.StartFrame.Col})");

        Position = (newX, newY);
    }

    private void UpdatePathing(Vector2D<double> toTarget)
    {
        double absX = Math.Abs(toTarget.X);
        double absY = Math.Abs(toTarget.Y);

        if (absX > absY)
        {
            _isMovingHorizontally = true;
            _direction = new Vector2D<double>(toTarget.X > 0 ? 1 : -1, 0);
            _currentDirection = toTarget.X > 0 ? Direction.Right : Direction.Left;
        }
        else
        {
            _isMovingHorizontally = false;
            _direction = new Vector2D<double>(0, toTarget.Y > 0 ? 1 : -1);
            _currentDirection = toTarget.X >= 0 ? Direction.Right : Direction.Left; // Still face left/right based on X
        }
    }

    private void UpdateDirection()
    {
        _currentDirection = _direction.X >= 0 ? Direction.Right : Direction.Left;
    }

    private void PickNewWanderTarget()
    {
        int dirX = _random.Next(-1, 2);
        int dirY = _random.Next(-1, 2);

        while (dirX == 0 && dirY == 0)
        {
            dirX = _random.Next(-1, 2);
            dirY = _random.Next(-1, 2);
        }

        _target = (
            _spawnPoint.X + dirX * 100,
            _spawnPoint.Y + dirY * 100
        );

        var toTarget = new Vector2D<double>(_target.X - Position.X, _target.Y - Position.Y);
        UpdatePathing(toTarget);
        _distanceTraveled = 0;
        _lastTargetSet = DateTimeOffset.Now;
    }

    public virtual void Render(GameRenderer renderer)
    {
        int scaledWidth = (int)(SpriteSheet.FrameWidth * Scale);
        int scaledHeight = (int)(SpriteSheet.FrameHeight * Scale);

        SpriteSheet.Render(
            renderer,
            Position,
            Angle,
            RotationCenter,
            scaledWidth,
            scaledHeight
        );
    }

}