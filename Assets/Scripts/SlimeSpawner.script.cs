using TheAdventure.Scripting;
using System;
using TheAdventure;

public class SlimeSpawner : IScript
{
    private DateTimeOffset _nextSpawnTime;
    private DateTimeOffset _gameStartTime;

    private readonly double _baseSpawnIntervalSeconds = 1.5;
    private readonly int _maxBaseSlimes = 8;

    public void Initialize()
    {
        _gameStartTime = DateTimeOffset.UtcNow;
        ScheduleNextSpawn();
    }

    public void Execute(Engine engine)
    {
        if (DateTimeOffset.UtcNow >= _nextSpawnTime)
        {
            var currentSlimeCount = engine.GetSlimeCount();
            var maxSlimes = GetMaxSlimesForCurrentTime();

            if (currentSlimeCount < maxSlimes)
            {
                engine.SpawnRandomSlime();
            }

            ScheduleNextSpawn();
        }
    }

    private int GetMaxSlimesForCurrentTime()
    {
        var gameTimeMinutes = (DateTimeOffset.UtcNow - _gameStartTime).TotalMinutes;
        var difficultyBonus = (int)(gameTimeMinutes / 2.0);
        return _maxBaseSlimes + difficultyBonus;
    }

    private void ScheduleNextSpawn()
    {
        var gameTimeMinutes = (DateTimeOffset.UtcNow - _gameStartTime).TotalMinutes;
        var speedMultiplier = 1.0 + (gameTimeMinutes * 0.2);

        var adjustedInterval = _baseSpawnIntervalSeconds / speedMultiplier;
        var randomVariation = (Random.Shared.NextDouble() - 0.5) * 1.0;
        var finalInterval = Math.Max(1.0, adjustedInterval + randomVariation);

        _nextSpawnTime = DateTimeOffset.UtcNow.AddSeconds(finalInterval);
    }
}