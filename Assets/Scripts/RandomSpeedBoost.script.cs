using TheAdventure.Scripting;
using System;
using TheAdventure;

public class RandomSpeedBoost : IScript
{
    DateTimeOffset _nextSpeedBoostTimestamp;

    public void Initialize()
    {
        _nextSpeedBoostTimestamp = DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(5, 10));
    }

    public void Execute(Engine engine)
    {
        if (_nextSpeedBoostTimestamp < DateTimeOffset.UtcNow)
        {
            _nextSpeedBoostTimestamp = DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(5, 10));
            var playerPos = engine.GetPlayerPosition();
            var spawnPosX = playerPos.X + Random.Shared.Next(-150, 150);
            var spawnPosY = playerPos.Y + Random.Shared.Next(-150, 150);
            engine.SpawnSpeedBoostPowerUp(spawnPosX, spawnPosY);
        }
    }
}


