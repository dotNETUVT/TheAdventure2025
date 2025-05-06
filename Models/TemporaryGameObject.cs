using Silk.NET.SDL;

namespace TheAdventure.Models;

public class TemporaryGameObject : RenderableGameObject
{
    public double Ttl { get; init; }
    private double _elapsedTime = 0;
    private bool _isPaused = false;
    private DateTimeOffset _lastUpdateTime;
    private DateTimeOffset _pauseStartTime;

    public bool IsExpired => _elapsedTime >= Ttl;

    private DateTimeOffset _spawnTime;

    public TemporaryGameObject(SpriteSheet spriteSheet, double ttl, (int X, int Y) position, double angle = 0.0, Point rotationCenter = default)
        : base(spriteSheet, position, angle, rotationCenter)
    {
        Ttl = ttl;
        _spawnTime = DateTimeOffset.Now;
        _lastUpdateTime = _spawnTime;
    }


    public void SetPaused(bool paused)
    {
        // Only make changes if pause state is changing
        if (paused == _isPaused) return;

        if (paused)
        {
            // When pausing, update elapsed time and record pause start time
            var now = DateTimeOffset.Now;
            _elapsedTime += (now - _lastUpdateTime).TotalSeconds;
            _pauseStartTime = now;
        }
        else
        {
            // When unpausing, set the last update time to now
            _lastUpdateTime = DateTimeOffset.Now;
        }

        _isPaused = paused;
        SpriteSheet.SetPaused(paused);
    }

    public override void Render(GameRenderer renderer)
    {
        // Update elapsed time if not paused
        if (!_isPaused)
        {
            var now = DateTimeOffset.Now;
            _elapsedTime += (now - _lastUpdateTime).TotalSeconds;
            _lastUpdateTime = now;
        }

        // Only render if not expired
        if (!IsExpired)
        {
            base.Render(renderer);
        }
    }
}