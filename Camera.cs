using Silk.NET.Maths;

namespace TheAdventure;

public class Camera
{
    private int _x;
    private int _y;
    private Rectangle<int> _worldBounds = new();

    public int X => _x;
    public int Y => _y;

    public readonly int Width;
    public readonly int Height;
    
    public Camera(int width, int height)
    {
        Width = width;
        Height = height;
    }
    
    public void SetWorldBounds(Rectangle<int> bounds)
    {
        var marginLeft = Width / 2;
        var marginTop = Height / 2;
        
        if (marginLeft * 2 > bounds.Size.X)
        {
            marginLeft = 48;
        }
        
        if (marginTop * 2 > bounds.Size.Y)
        {
            marginTop = 48;
        }
        
        _worldBounds = new Rectangle<int>(marginLeft, marginTop, bounds.Size.X - marginLeft * 2,
            bounds.Size.Y - marginTop * 2);
        _x = marginLeft;
        _y = marginTop;
    }
    
    public void LookAt(int x, int y)
    {
        if (_worldBounds.Contains(new Vector2D<int>(_x, y)))
        {
            _y = y;
        }
        if (_worldBounds.Contains(new Vector2D<int>(x, _y)))
        {
            _x = x;
        }
    }

    public void LookAtPlayers(int x1, int y1, int x2, int y2)
    {
        int midX = (x1 + x2) / 2;
        int midY = (y1 + y2) / 2;

        int left = _worldBounds.Origin.X;
        int right = _worldBounds.Origin.X + _worldBounds.Size.X;
        int top = _worldBounds.Origin.Y;
        int bottom = _worldBounds.Origin.Y + _worldBounds.Size.Y;

        _x = Math.Clamp(midX, left, right);
        _y = Math.Clamp(midY, top, bottom);
    }

    public Rectangle<int> ToScreenCoordinates(Rectangle<int> rect)
    {
        return rect.GetTranslated(new Vector2D<int>(Width / 2 - X, Height / 2 - Y));
    }

    public Vector2D<int> ToWorldCoordinates(Vector2D<int> point)
    {
        return point - new Vector2D<int>(Width / 2 - X, Height / 2 - Y);
    }
}