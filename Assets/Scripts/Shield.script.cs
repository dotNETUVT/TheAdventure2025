using TheAdventure.Scripting;
using System;
using TheAdventure;
using TheAdventure.Models;

public class Shield : IScript
{
    DateTimeOffset _nextShieldTimestamp;
    float _shieldSpawnDuration = 5f; // seconds
    ShieldInstance? _activeShield = null;

    public void Initialize()
    {
        _nextShieldTimestamp = DateTimeOffset.UtcNow.AddSeconds(20);
        _activeShield = null;
    }

    public void Execute(Engine engine)
    {
        var now = DateTimeOffset.UtcNow;
        if (_activeShield != null && (now - _activeShield.SpawnTime).TotalSeconds > _shieldSpawnDuration)
        {
            // Remove shield if not picked up in time
            engine.RemoveShield(_activeShield.Id);
            _activeShield = null;
        }

        if (_nextShieldTimestamp < now && _activeShield == null)
        {
            var playerPos = engine.GetPlayerPosition();
            var shieldPosX = playerPos.X + Random.Shared.Next(-70, 70);
            var shieldPosY = playerPos.Y + Random.Shared.Next(-70, 70);
            int shieldId = engine.AddShield(shieldPosX, shieldPosY);
            _activeShield = new ShieldInstance { Id = shieldId, SpawnTime = now };
            _nextShieldTimestamp = now.AddSeconds(20);
        }
    }

    class ShieldInstance
    {
        public int Id;
        public DateTimeOffset SpawnTime;
    }
}
