using System;
using TheAdventure.Scripting;
using TheAdventure;

public class RandomChest : IScript
{
    DateTimeOffset _nextChestTimestamp;

    public void Initialize()
    {
        _nextChestTimestamp = DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(4, 10));
    }

    public void Execute(Engine engine)
    {
        if (_nextChestTimestamp < DateTimeOffset.UtcNow)
        {
            _nextChestTimestamp = DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(4, 10));
            var playerPos = engine.GetPlayerPosition();
            var chestPosX = playerPos.X + Random.Shared.Next(-100, 100);
            var chestPosY = playerPos.Y + Random.Shared.Next(-100, 100);
            engine.AddTreasureChest(chestPosX, chestPosY, false);
        }
    }
}