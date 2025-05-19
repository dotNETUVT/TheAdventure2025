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

    private int _shakeDuration = 0;
    private int _shakeStrength = 0;
    private readonly Random _random = new();

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
            marginLeft = 48;
        if (marginTop * 2 > bounds.Size.Y)
            marginTop = 48;

        _worldBounds = new Rectangle<int>(marginLeft, marginTop, bounds.Size.X - marginLeft * 2, bounds.Size.Y - marginTop * 2);
        _x = marginLeft;
        _y = marginTop;
    }

    public void Shake(int duration, int strength)
    {
        _shakeDuration = duration;
        _shakeStrength = strength;
    }

    public void LookAt(int x, int y)
    {
        if (_worldBounds.Contains(new Vector2D<int>(_x, y)))
            _y = y;
        if (_worldBounds.Contains(new Vector2D<int>(x, _y)))
            _x = x;

        if (_shakeDuration > 0)
        {
            int offsetX = _random.Next(-_shakeStrength, _shakeStrength + 1);
            int offsetY = _random.Next(-_shakeStrength, _shakeStrength + 1);
            _x += offsetX;
            _y += offsetY;
            _shakeDuration--;
        }
    }

    public Rectangle<int> ToScreenCoordinates(Rectangle<int> rect)
    {
        return rect.GetTranslated(new Vector2D<int>(Width / 2 - X, Height / 2 - Y));
    }

    public Vector2D<int> ToWorldCoordinates(Vector2D<int> point)
    {
        return point - new Vector2D<int>(Width / 2 - _x, Height / 2 - _y);
    }
}
