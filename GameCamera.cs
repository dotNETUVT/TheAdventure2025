using Silk.NET.Maths;

namespace TheAdventure;

public class GameCamera(int width, int height)
{
    private Rectangle<int> _worldBounds = new();

    private int X { get; set; }

    private int Y { get; set; }

    public void SetWorldBounds(Rectangle<int> bounds)
    {
        var halfWidth = width / 2;
        var halfHeight = height / 2;


        var minX = halfWidth;
        var minY = halfHeight;
        var maxX = bounds.Size.X - halfWidth;
        var maxY = bounds.Size.Y - halfHeight;

        if (minX > maxX)
        {
            minX = maxX = bounds.Size.X / 2;
        }

        if (minY > maxY)
        {
            minY = maxY = bounds.Size.Y / 2;
        }

        _worldBounds = new Rectangle<int>(minX, minY, maxX - minX, maxY - minY);
        X = minX;
        Y = minY;
    }

    public void LookAt(int x, int y)
    {
        X = Math.Clamp(x, _worldBounds.Origin.X, _worldBounds.Origin.X + _worldBounds.Size.X);
        Y = Math.Clamp(y, _worldBounds.Origin.Y, _worldBounds.Origin.Y + _worldBounds.Size.Y);
    }

    public Rectangle<int> ToScreenCoordinates(Rectangle<int> rect)
    {
        return rect.GetTranslated(new Vector2D<int>(width / 2 - X, height / 2 - Y));
    }

    public Vector2D<int> ToWorldCoordinates(Vector2D<int> point)
    {
        return point - new Vector2D<int>(width / 2 - X, height / 2 - Y);
    }
}