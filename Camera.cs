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
        _worldBounds = bounds;
        _x = bounds.Center.X;
        _y = bounds.Center.Y;
    }
    
    public void LookAt(int x, int y)
    {
        int halfW = Width / 2;
        int halfH = Height / 2;

        int left   = _worldBounds.Origin.X;
        int right  = _worldBounds.Origin.X + _worldBounds.Size.X;
        int top    = _worldBounds.Origin.Y;
        int bottom = _worldBounds.Origin.Y + _worldBounds.Size.Y;

        int worldWidth = right - left;
        int worldHeight = bottom - top;

        // If window is wider/taller than the world, center the camera
        if (Width >= worldWidth)
            _x = left + worldWidth / 2;
        else
            _x = Math.Clamp(x, left + halfW, right - halfW);

        if (Height >= worldHeight)
            _y = top + worldHeight / 2;
        else
            _y = Math.Clamp(y, top + halfH, bottom - halfH);
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