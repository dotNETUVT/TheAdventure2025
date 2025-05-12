using TheAdventure.Scripting;
using TheAdventure;
using TheAdventure.Generation;
using System;
using System.Collections.Generic;

public class OreGenerator : IScript
{
    private DateTimeOffset _nextOreTimestamp;
    private const float ORE_CHANCE = 0.9f;
    private const int MIN_SPAWN_DELAY = 2;
    private const int MAX_SPAWN_DELAY = 3;
    private const int MAX_ORES = 1000;
    private HashSet<(int X, int Y)> _orePositions = new();
    private readonly TerrainGenerator _terrainGenerator;
    
    private const int MAP_WIDTH = 256;
    private const int MAP_HEIGHT = 256;
    private const int TILE_SIZE = 16;

    public OreGenerator()
    {
        _terrainGenerator = new TerrainGenerator(Random.Shared.Next());
    }

    public void Initialize()
    {
        _nextOreTimestamp = DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(MIN_SPAWN_DELAY, MAX_SPAWN_DELAY));
        _orePositions.Clear();
    }

    private (int x, int y) FindRandomLandPosition()
    {
        int maxAttempts = 50;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int x = Random.Shared.Next(MAP_WIDTH);
            int y = Random.Shared.Next(MAP_HEIGHT);

            if (_terrainGenerator.IsLand(x, y))
            {
                return (x * TILE_SIZE, y * TILE_SIZE);
            }
        }
        return _terrainGenerator.FindLandLocation();
    }

    public void Execute(Engine engine)
    {
        if (_nextOreTimestamp > DateTimeOffset.UtcNow)
            return;

        _nextOreTimestamp = DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(MIN_SPAWN_DELAY, MAX_SPAWN_DELAY));

        if (_orePositions.Count >= MAX_ORES)
            return;

        var (oreX, oreY) = FindRandomLandPosition();
        var newOrePos = (oreX, oreY);

        if (!_orePositions.Contains(newOrePos) && Random.Shared.NextDouble() < ORE_CHANCE)
        {
            engine.AddOre(oreX, oreY, false);
            _orePositions.Add(newOrePos);
            Console.WriteLine($"Ore added at {oreX}, {oreY}. Total ores: {_orePositions.Count}");
        }
    }

    public void OnOreBreak((int X, int Y) position)
    {
        _orePositions.Remove(position);
    }
}