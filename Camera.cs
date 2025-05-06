using Silk.NET.Maths;

namespace TheAdventure;

public class Camera
{
    private int _x;
    private int _y;
    private Rectangle<int> _worldBounds = new();
    public int ViewportWidth { get; private set; }
    public int ViewportHeight { get; private set; }

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

    public Rectangle<int> ToScreenCoordinates(Rectangle<int> rect)
    {
        return rect.GetTranslated(new Vector2D<int>(Width / 2 - X, Height / 2 - Y));
    }

    public Vector2D<int> ToWorldCoordinates(Vector2D<int> point)
    {
        return point - new Vector2D<int>(Width / 2 - X, Height / 2 - Y);
    }
}