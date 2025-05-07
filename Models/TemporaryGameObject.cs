using TheAdventure.Models;

public class TemporaryGameObject : RenderableGameObject
{
    public bool IsExpired => (DateTimeOffset.Now - _startTime).TotalSeconds >= _durationSeconds;
    private readonly DateTimeOffset _startTime = DateTimeOffset.Now;
    private readonly double _durationSeconds;

    public TemporaryGameObject(SpriteSheet spriteSheet, double durationSeconds, (int X, int Y) position)
        : base(spriteSheet, position)
    {
        _durationSeconds = durationSeconds;
    }
}
