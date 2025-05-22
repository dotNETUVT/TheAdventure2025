using TheAdventure.Scripting;
using System;
using TheAdventure;
using TheAdventure.Models;

public class RandomPotion : IScript
{
    DateTimeOffset _nextPotionTimestamp;

    public void Initialize()
    {
        _nextPotionTimestamp = DateTimeOffset.UtcNow.AddSeconds(5);
    }

    public void Execute(Engine engine)
    {
        var now = DateTimeOffset.UtcNow;
        if (_nextPotionTimestamp < now)
        {
            var playerPos = engine.GetPlayerPosition();
            var potionPosX = playerPos.X + Random.Shared.Next(-50, 50);
            var potionPosY = playerPos.Y + Random.Shared.Next(-50, 50);
            engine.AddPotion(potionPosX, potionPosY, false);
            _nextPotionTimestamp = now.AddSeconds(5);
        }
    }
}
