namespace TheAdventure.Models;

public class WaveSystem
{
    public int CurrentWave { get; private set; } = 1;
    public int EnemiesRemainingInWave { get; private set; }
    public bool WaveCompleted => EnemiesRemainingInWave <= 0;

    private readonly Random _random = new();

    // Probability weights for different buff types
    private static readonly Dictionary<PlayerBuffType, int> BuffWeights = new()
    {
        { PlayerBuffType.SpeedBoost, 30 },        // Common
        { PlayerBuffType.DamageBoost, 25 },       // Common
        { PlayerBuffType.HealthRestore, 20 },     // Uncommon
        { PlayerBuffType.ExtraBomb, 10 },         // Rare
        { PlayerBuffType.BombRadius, 15 }         // Uncommon
    };

    private static readonly int TotalWeight = BuffWeights.Values.Sum();

    // Wave configuration
    private const int BaseEnemiesPerWave = 5;
    private const int EnemyIncreasePerWave = 2;

    public WaveSystem()
    {
        EnemiesRemainingInWave = CalculateEnemiesForWave(CurrentWave);
    }

    public void EnemyDefeated()
    {
        if (EnemiesRemainingInWave > 0)
        {
            EnemiesRemainingInWave--;
        }
    }

    public void StartNextWave()
    {
        CurrentWave++;
        EnemiesRemainingInWave = CalculateEnemiesForWave(CurrentWave);
    }

    public PlayerBuffType GetRandomBuff()
    {
        int randomValue = _random.Next(TotalWeight);
        int weightSum = 0;

        foreach (var buffWeight in BuffWeights)
        {
            weightSum += buffWeight.Value;
            if (randomValue < weightSum)
            {
                return buffWeight.Key;
            }
        }

        // Fallback to speed boost if something goes wrong
        return PlayerBuffType.SpeedBoost;
    }

    private int CalculateEnemiesForWave(int wave)
    {
        return BaseEnemiesPerWave + ((wave - 1) * EnemyIncreasePerWave);
    }

    public (int EnemiesPerWave, int MaxEnemies, double SpawnIntervalSeconds) GetWaveDifficulty()
    {
        // Calculate difficulty settings based on wave
        double spawnInterval = Math.Max(2.0, 5.0 - (CurrentWave * 0.25));
        int maxEnemies = Math.Min(15, 10 + CurrentWave);

        return (EnemiesRemainingInWave, maxEnemies, spawnInterval);
    }
}
