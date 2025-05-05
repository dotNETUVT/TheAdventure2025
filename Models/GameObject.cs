namespace TheAdventure.Models;

public class GameObject
{
    public int Id { get; private set; }
    
    private static int _nextId = -1;
    
    public GameObject()
    {
        Id = Interlocked.Increment(ref _nextId);
    }
    public virtual void Update(int elapsedMs) { }
}

