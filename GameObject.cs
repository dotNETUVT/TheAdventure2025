namespace TheAdventure;

public class GameObject
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 32;
    public int Height { get; set; } = 32;

    public bool IsCollidingWith(GameObject other)
    {
        return X < other.X + other.Width &&
               X + Width > other.X &&
               Y < other.Y + other.Height &&
               Y + Height > other.Y;
    }
}
