using Silk.NET.Maths;
using Silk.NET.SDL;

namespace TheAdventure.Models;

public class PlayerObject : GameObject
{
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;

    private Rectangle<int> _source = new(0, 0, 120, 120);
    private Rectangle<int> _target = new(0, 0, 48, 48);
    private Rectangle<int> _worldBounds;


    private readonly int _textureId;

    private const int Speed = 140; // pixels per second
    private enum Direction { Down, Left, Right, Up }
    private Direction _currentDirection = Direction.Down;

    public PlayerObject(GameRenderer renderer, Rectangle<int> worldBounds)
    {
        _worldBounds = worldBounds;
        _textureId = renderer.LoadTexture(Path.Combine("Assets", "RedRidingHoodSpriteSheet.png"), out _);
        if (_textureId < 0)
        {
            throw new Exception("Failed to load player texture");
        }

        UpdateTarget();
    }

    public void UpdatePosition(double up, double down, double left, double right, int time)
    {
        var pixelsToMove = Speed * (time / 1000.0);

        if (up > 0)
            _currentDirection = Direction.Up;
        else if (down > 0)
            _currentDirection = Direction.Down;
        else if (left > 0)
            _currentDirection = Direction.Left;
        else if (right > 0)
            _currentDirection = Direction.Right;

        Y -= (int)(pixelsToMove * up);
        Y += (int)(pixelsToMove * down);
        X -= (int)(pixelsToMove * left);
        X += (int)(pixelsToMove * right);

        X = Math.Clamp(X, 0, _worldBounds.Size.X - _target.Size.X);
        Y = Math.Clamp(Y, 0, _worldBounds.Size.Y - _target.Size.Y);

        UpdateTarget();
        UpdateSource(); 
    }


    public void Render(GameRenderer renderer)
    {
        var flip = _currentDirection == Direction.Left ? RendererFlip.FlipHorizontal : RendererFlip.None;
        renderer.RenderTexture(_textureId, _source, _target, flip);
    }

    private void UpdateTarget()
    {
        _target = new(X, Y, 48, 48);
    }

    private void UpdateSource()
    {
        var baseRect = new Rectangle<int>(400, 0, 200, 200);
        _source = _currentDirection switch
        {
            Direction.Down => new Rectangle<int>(0, 0, 200, 200),
            Direction.Right => baseRect,
            Direction.Left => baseRect,
            Direction.Up => new Rectangle<int>(600, 0, 200, 200),
            _ => _source
        };
    }
    public (int dx, int dy) GetDirectionVector()
    {
        return _currentDirection switch
        {
            Direction.Up => (0, -1),
            Direction.Down => (0, 1),
            Direction.Left => (-1, 0),
            Direction.Right => (1, 0),
            _ => (0, 0)
        };
    }

}