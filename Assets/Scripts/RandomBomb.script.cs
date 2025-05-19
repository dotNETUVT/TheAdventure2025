using TheAdventure.Scripting;
using System;
using TheAdventure;

//modificari pentru dificultate dinamica:
/*nivel dificultate:
	Nivelul de dificultate creste la fiecare 30 secunde
	In functie de nivelul de dificultate:
		Apar mai multe bombe, pana la 10
		Apar random, dar mai aproape de player
		Apar la un interval mai scurt*/
public class RandomBomb : IScript
{
    private DateTimeOffset _nextBombTimestamp;
    private DateTimeOffset _startTime;
    private double _difficultyMultiplier = 1.0;

    public void Initialize()
    {
        _startTime = DateTimeOffset.UtcNow;
        _nextBombTimestamp = DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(2, 5));
    }

    public void Execute(Engine engine)
    {
        if (_nextBombTimestamp < DateTimeOffset.UtcNow)
        {
            var secondsElapsed = (DateTimeOffset.UtcNow - _startTime).TotalSeconds;

            _difficultyMultiplier = Math.Min(15.0, 1 + secondsElapsed / 30.0);

            int bombsToSpawn = Math.Min((int)_difficultyMultiplier, 10);

            int maxDistance = Math.Max(50, 150 - (int)(_difficultyMultiplier * 10));

            var playerPos = engine.GetPlayerPosition();

            for (int i = 0; i < bombsToSpawn; i++)
            {
                int bombPosX = playerPos.X + Random.Shared.Next(-maxDistance, maxDistance);
                int bombPosY = playerPos.Y + Random.Shared.Next(-maxDistance, maxDistance);

                engine.AddBomb(bombPosX, bombPosY, false);
            }

            double delay = Math.Max(4, 5.0 - (_difficultyMultiplier * 0.3));
            _nextBombTimestamp = DateTimeOffset.UtcNow.AddSeconds(delay);
        }
    }
}
