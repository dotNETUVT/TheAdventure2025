using System.Numerics;

namespace TheAdventure.Generation;

public class TerrainGenerator
{
    private readonly Random _random;
    private readonly int _seed;
    private readonly float[,] _heightMap;
    
    public TerrainGenerator(int seed)
    {
        _seed = seed;
        _random = new Random(seed);
        _heightMap = new float[256, 256]; 
        
        GenerateHeightMap();
    }

    private void GenerateHeightMap()
    {
        for (int x = 0; x < _heightMap.GetLength(0); x++)
        {
            for (int y = 0; y < _heightMap.GetLength(1); y++)
            {
                _heightMap[x, y] = GenerateHeight(x, y);
            }
        }
    }

    private float GenerateHeight(int x, int y)
    {
        float scale = 50f; 
        return PerlinNoise(x / scale, y / scale);
    }

    private float PerlinNoise(float x, float y)
    {
        int xi = (int)Math.Floor(x) & 255;
        int yi = (int)Math.Floor(y) & 255;
        float xf = x - (float)Math.Floor(x);
        float yf = y - (float)Math.Floor(y);
        
        Vector2 topRight = RandomGradient(xi + 1, yi + 1);
        Vector2 topLeft = RandomGradient(xi, yi + 1);
        Vector2 bottomRight = RandomGradient(xi + 1, yi);
        Vector2 bottomLeft = RandomGradient(xi, yi);
        
        float tx = Fade(xf);
        float ty = Fade(yf);
        
        return Lerp(
            Lerp(Dot(bottomLeft, xf, yf), Dot(bottomRight, xf - 1, yf), tx),
            Lerp(Dot(topLeft, xf, yf - 1), Dot(topRight, xf - 1, yf - 1), tx),
            ty);
    }

    private Vector2 RandomGradient(int x, int y)
    {
        int hash = (x * 73856093) ^ (y * 19349663) ^ _seed;
        var rng = new Random(hash);
        float angle = (float)(rng.NextDouble() * 2 * Math.PI);
        return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
    }

    private float Dot(Vector2 gradient, float x, float y)
    {
        return gradient.X * x + gradient.Y * y;
    }

    private float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private float Lerp(float a, float b, float t)
    {
        return a + t * (b - a);
    }
    
    
    private static readonly int[] WaterTiles = { 0, 0, 0, 0, 0, 0, 0, 1, 2 };
    private static readonly int[] SandTiles = { 3, 3, 3, 3, 3, 3, 3, 4, 5 };
    private static readonly int[] GrassTiles = { 6, 7, 8, 9 };

    public (int TileId, float Height) GetTileAt(int x, int y)
    {
        float height = _heightMap[x, y];

        if (height < 0.03f)
        {
            return (GetWeightedRandomTile(new[]
            {
                (0, 11f), 
                (1, 1f), 
                (2, 1f)  
            }), height);
        }

        if (height < 0.12f)
        {
            return (GetWeightedRandomTile(new[]
            {
                (3, 11f),
                (4, 1f),
                (5, 1f)
            }), height);
        }
        
        return (GetWeightedRandomTile(new[]
        {
            (6, 3f),
            (7, 2f),
            (8, 1f),
            (9, 1f)
        }), height);
    }

    
    private int GetWeightedRandomTile((int TileId, float Weight)[] tiles)
    {
        float totalWeight = tiles.Sum(t => t.Weight);
        float choice = (float)_random.NextDouble() * totalWeight;
        float cumulative = 0f;

        foreach (var tile in tiles)
        {
            cumulative += tile.Weight;
            if (choice < cumulative)
                return tile.TileId;
        }

        return tiles.Last().TileId; 
    }
    public bool IsLand(int x, int y)
    {
        if (x < 0 || x >= _heightMap.GetLength(0) || y < 0 || y >= _heightMap.GetLength(1))
            return false;
        
        float height = _heightMap[x, y];
        return height >= 0.03f; 
    }

    public (int X, int Y) FindLandLocation()
    {
        int centerX = _heightMap.GetLength(0) / 2;
        int centerY = _heightMap.GetLength(1) / 2;
        
        for (int radius = 0; radius < 256; radius++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (IsLand(x, y))
                    {
                        return (x * 16, y * 16); 
                    }
                }
            }
        }
        return (centerX * 16, centerY * 16);
    }

}