using TheAdventure.Models;
using Silk.NET.Maths;
using TheAdventure;

public class OreObject : RenderableGameObject
{
    private readonly SpriteSheet _spriteSheet;
    private readonly Vector2D<int> _position;
    private bool _isDestroyed;
    private const int COLLISION_SIZE = 16;
    private const int BOMB_BREAK_RADIUS = 48;



    public OreObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        _spriteSheet = spriteSheet;
        _position = new Vector2D<int>(x, y);
        _isDestroyed = false;
        
    }

    public bool IsNearBomb((int X, int Y) position)
    {
        if (_isDestroyed) return false;
        
        // Calculate distance between ore and bomb
        int deltaX = Position.X - position.X;
        int deltaY = Position.Y - position.Y;
        double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        
        return distance <= BOMB_BREAK_RADIUS;

    }
    
    public bool IsColliding((int X, int Y) playerPosition)
    {
        if (_isDestroyed) return false;
        
        var oreBox = new
        {
            Left = Position.X,
            Right = Position.X + COLLISION_SIZE,
            Top = Position.Y,
            Bottom = Position.Y + COLLISION_SIZE
        };
        
        var playerBox = new
        {
            Left = playerPosition.X,
            Right = playerPosition.X + COLLISION_SIZE,
            Top = playerPosition.Y,
            Bottom = playerPosition.Y + COLLISION_SIZE
        };

        return !(oreBox.Left >= playerBox.Right ||
                 oreBox.Right <= playerBox.Left ||
                 oreBox.Top >= playerBox.Bottom ||
                 oreBox.Bottom <= playerBox.Top);
    }
    public void Break()
    {
        _isDestroyed = true;
        
    }

    public bool IsDestroyed => _isDestroyed;

    public override void Render(GameRenderer renderer)
    {
        if (!_isDestroyed)
        {
            _spriteSheet.Render(renderer, (_position.X, _position.Y));
        }
    }
}