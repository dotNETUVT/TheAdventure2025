using Silk.NET.Maths;

namespace TheAdventure.Extensions;

public static class RectangleExtensions
{
  public static bool IntersectsWith(this Rectangle<int> rectA, Rectangle<int> rectB)
  {
    if (rectA.Origin.X + rectA.Size.X <= rectB.Origin.X || rectB.Origin.X + rectB.Size.X <= rectA.Origin.X)
    {
      return false;
    }

    if (rectA.Origin.Y + rectA.Size.Y <= rectB.Origin.Y || rectB.Origin.Y + rectB.Size.Y <= rectA.Origin.Y)
    {
      return false;
    }

    return true;
  }
}