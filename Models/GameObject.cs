using Silk.NET.Maths;

namespace TheAdventure.Models;

public class GameObject
{
    public int Id { get; private set; }
    public string Type { get; set; } = "Unknown";
    public Vector2D<float> Position {get; set;}
    
    private static int _nextId = -1;
    
    public GameObject()
    {
        Id = Interlocked.Increment(ref _nextId);
    }
}