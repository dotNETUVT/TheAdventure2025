using TheAdventure.Scripting;
using System;
using TheAdventure;

public class RandomBomb : IScript
{
    DateTimeOffset _nextBombTimestamp;

    public void Initialize()
    {
        _nextBombTimestamp = DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(2, 5));
    }

    public void Execute(Engine engine)
    {
        if (_nextBombTimestamp < DateTimeOffset.UtcNow)
        {
            double extra = engine.SurvivedTime / 5.0;
            double baseInterval = Random.Shared.Next(2, 5);
            double interval = baseInterval + extra;
            
            _nextBombTimestamp = DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(2, 5));
            var playerPos = engine.GetPlayerPosition();
            var bombPosX = playerPos.X + Random.Shared.Next(-50, 50);
            var bombPosY = playerPos.Y + Random.Shared.Next(-50, 50);
            engine.AddBomb(bombPosX, bombPosY, false);
        }
    }
}