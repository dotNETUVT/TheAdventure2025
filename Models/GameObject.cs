namespace TheAdventure.Models;

public class GameObject
{
    public int Id { get; private set; }

    private static int _nextId = -1; // Start at -1 so first Increment is 0

    public GameObject()
    {
        Id = Interlocked.Increment(ref _nextId);
    }
}