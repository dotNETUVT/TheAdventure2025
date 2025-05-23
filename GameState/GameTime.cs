namespace TheAdventure.GameState;

public class GameTime
{
    private static GameTime? _instance;
    public static GameTime Instance => _instance ??= new GameTime();

    private DateTimeOffset _gameStartTime;
    private DateTimeOffset _lastPauseTime;
    private TimeSpan _totalPausedTime = TimeSpan.Zero;
    private bool _isPaused = false;

    private GameTime()
    {
        _gameStartTime = DateTimeOffset.Now;
    }

    public void Pause()
    {
        if (!_isPaused)
        {
            _lastPauseTime = DateTimeOffset.Now;
            _isPaused = true;
        }
    }

    public void Resume()
    {
        if (_isPaused)
        {
            _totalPausedTime += DateTimeOffset.Now - _lastPauseTime;
            _isPaused = false;
        }
    }

    public DateTimeOffset Now
    {
        get
        {
            if (_isPaused)
            {
                return _lastPauseTime - _totalPausedTime;
            }
            return DateTimeOffset.Now - _totalPausedTime;
        }
    }

    public double GetElapsedTime()
    {
        return (Now - _gameStartTime).TotalSeconds;
    }

    public bool IsPaused => _isPaused;
} 