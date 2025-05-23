using TheAdventure.Scripting;
using System;
using TheAdventure;
using TheAdventure.GameState;

public class RandomBomb : IScript
{
    DateTimeOffset _nextBombTimestamp;
    private double _nextBombDelay;

    public void Initialize()
    {
        _nextBombDelay = Random.Shared.Next(4, 8);
        _nextBombTimestamp = GameTime.Instance.Now.AddSeconds(_nextBombDelay);
    }

    public void Execute(Engine engine)
    {
        if (_nextBombTimestamp < GameTime.Instance.Now)
        {
            _nextBombDelay = Random.Shared.Next(4, 8);
            _nextBombTimestamp = GameTime.Instance.Now.AddSeconds(_nextBombDelay);
            var playerPos = engine.GetPlayerPosition();
            var bombPosX = playerPos.X + Random.Shared.Next(-50, 50);
            var bombPosY = playerPos.Y + Random.Shared.Next(-50, 50);
            engine.AddBomb(bombPosX, bombPosY, false);
        }
    }
}