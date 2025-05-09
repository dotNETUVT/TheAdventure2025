using Silk.NET.Maths;

namespace TheAdventure;

public static class RectangleExtensions
{
    public static bool Overlaps(this Rectangle<int> rect1, Rectangle<int> rect2)
    {
        return rect1.Origin.X < rect2.Origin.X + rect2.Size.X &&
               rect1.Origin.X + rect1.Size.X > rect2.Origin.X &&
               rect1.Origin.Y < rect2.Origin.Y + rect2.Size.Y &&
               rect1.Origin.Y + rect1.Size.Y > rect2.Origin.Y;
    }
}