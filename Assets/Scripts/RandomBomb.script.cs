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

    public void Execute(IGameState state)
    {
        PlayingState _state = state as PlayingState;
        if (_state == null)
        {
            throw new InvalidOperationException("RandomBomb script can only be executed in a PlayingState.");
        }
        Execute((PlayingState)state);
    }

    public void Execute(PlayingState state)
    {
        if (_nextBombTimestamp < GameTime.Instance.Now)
        {
            _nextBombDelay = Random.Shared.Next(4, 8);
            _nextBombTimestamp = GameTime.Instance.Now.AddSeconds(_nextBombDelay);
            var playerPos = state.GetPlayerPosition();
            var bombPosX = playerPos.X + Random.Shared.Next(-50, 50);
            var bombPosY = playerPos.Y + Random.Shared.Next(-50, 50);
            state.AddBomb(bombPosX, bombPosY, false);
        }
    }
}