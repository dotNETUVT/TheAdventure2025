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
    
    private double _zoom = 1.0;

    public double Zoom => _zoom;

    public void ToggleZoom()
    {
        _zoom = (_zoom == 1.0) ? 0.5 : 1.0;
    }

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
        var translated = rect.GetTranslated(new Vector2D<int>(Width / 2 - X, Height / 2 - Y));
        var zoomed = new Rectangle<int>(
            (int)(translated.Origin.X * _zoom),
            (int)(translated.Origin.Y * _zoom),
            (int)(translated.Size.X * _zoom),
            (int)(translated.Size.Y * _zoom)
        );
        return zoomed;
    }

    public Vector2D<int> ToWorldCoordinates(Vector2D<int> point)
    {
        return point - new Vector2D<int>(Width / 2 - X, Height / 2 - Y);
    }
}