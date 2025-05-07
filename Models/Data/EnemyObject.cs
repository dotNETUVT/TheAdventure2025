using Silk.NET.Maths;
using Silk.NET.SDL;

namespace TheAdventure.Models;

public class EnemyObject : RenderableGameObject
{
    private double _speed = 50.0;
    private Vector2D<double> _direction = new(1, 0);
    private Vector2D<double> _exactPosition;
    public bool IsAlive { get; private set; } = true;
    private RenderableGameObject _player;
    private double _deathTimer;
    private const double DeathAnimationDuration = 400;
    private const int CollisionSize = 32; 
    
    public bool ShouldBeRemoved => !IsAlive && _deathTimer >= DeathAnimationDuration;

    public EnemyObject(SpriteSheet spriteSheet, double x, double y, RenderableGameObject player) 
        : base(spriteSheet, ((int)x, (int)y), 0.0, new Point())
    {
        _exactPosition = new Vector2D<double>(x, y);
        _player = player;
        spriteSheet.ActivateAnimation("Walk");
    }

    public void Update(double deltaTime)
    {
        if (!IsAlive)
        {
            _deathTimer += deltaTime;
            return;
        }

        var playerPos = new Vector2D<double>(_player.Position.X, _player.Position.Y);
        var toPlayer = new Vector2D<double>(
            playerPos.X - _exactPosition.X,
            playerPos.Y - _exactPosition.Y
        );

        var length = Math.Sqrt(toPlayer.X * toPlayer.X + toPlayer.Y * toPlayer.Y);
        if (length > 0)
        {
            _direction = new Vector2D<double>(toPlayer.X / length, toPlayer.Y / length);
        }

        Angle = Math.Atan2(_direction.Y, _direction.X) * (180.0 / Math.PI);

        _exactPosition = new Vector2D<double>(
            _exactPosition.X + _direction.X * _speed * (deltaTime / 1000),
            _exactPosition.Y + _direction.Y * _speed * (deltaTime / 1000)
        );
        
        Position = ((int)_exactPosition.X, (int)_exactPosition.Y);
    }

    public void Die()
    {
        if (!IsAlive) return;
        
        IsAlive = false;
        _deathTimer = 0;
        SpriteSheet.ActivateAnimation("Die");
    }

    public Rectangle<double> GetBounds()
    {
        double halfSize = CollisionSize / 2.0;
        return new Rectangle<double>(
            _exactPosition.X - halfSize,
            _exactPosition.Y - halfSize,
            CollisionSize,
            CollisionSize
        );
    }
}