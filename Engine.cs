using System.Reflection;
using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;

namespace TheAdventure;

public class Engine
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private readonly ScriptEngine _scriptEngine = new();

    private readonly Dictionary<int, GameObject> _gameObjects = [];
    private readonly Dictionary<string, TileSet> _loadedTileSets = [];
    private readonly Dictionary<int, Tile> _tileIdMap = [];

    private Level _currentLevel = new();
    private PlayerObject? _player;
    private Rectangle<int> _worldBounds;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void SetupWorld()
    {
        _player = new(SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);

        var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        var level = JsonSerializer.Deserialize<Level>(levelContent) ?? throw new Exception("Failed to load level");
        foreach (var tileSetRef in level.TileSets)
        {
            var tileSetContent = File.ReadAllText(Path.Combine("Assets", tileSetRef.Source));
            var tileSet = JsonSerializer.Deserialize<TileSet>(tileSetContent) ?? throw new Exception("Failed to load tile set");
            foreach (var tile in tileSet.Tiles)
            {
                tile.TextureId = _renderer.LoadTexture(Path.Combine("Assets", tile.Image), out _);
                _tileIdMap.Add(tile.Id!.Value, tile);
            }

            _loadedTileSets.Add(tileSet.Name, tileSet);
        }

        if (level.Width == null || level.Height == null)
        {
            throw new Exception("Invalid level dimensions");
        }

        if (level.TileWidth == null || level.TileHeight == null)
        {
            throw new Exception("Invalid tile dimensions");
        }

        _worldBounds = new Rectangle<int>(0, 0, level.Width.Value * level.TileWidth.Value,
            level.Height.Value * level.TileHeight.Value);
        _renderer.SetWorldBounds(_worldBounds);

        _currentLevel = level;

        _scriptEngine.LoadAll(Path.Combine("Assets", "Scripts"));
    }

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        if (_player == null)
        {
            return;
        }

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;
        bool isAttacking = _input.IsKeyAPressed() && (up + down + left + right <= 1);
        bool addBomb = _input.IsKeyBPressed();

        _player.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame);
        if (isAttacking)
        {
            _player.Attack();
        }

        UpdateSlimes(msSinceLastFrame);

        _scriptEngine.ExecuteAll(this);

        if (addBomb)
        {
            AddBomb(_player.Position.X, _player.Position.Y, false);
        }
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        var playerPosition = _player!.Position;
        _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);

        RenderTerrain();
        RenderAllObjects();

        _renderer.PresentFrame();
    }

    public void RenderAllObjects()
    {
        var toRemove = new List<int>();
        var explodingBombs = new List<(int X, int Y)>();

        foreach (var gameObject in GetRenderables())
        {
            gameObject.Render(_renderer);

            if (gameObject is TemporaryGameObject { IsExpired: true } tempGameObject)
            {
                toRemove.Add(tempGameObject.Id);
                explodingBombs.Add(tempGameObject.Position);
            }
        }

        foreach (var bombPos in explodingBombs)
        {
            CheckBombSlimeCollisions(bombPos);
        }

        foreach (var id in toRemove)
        {
            _gameObjects.Remove(id, out var gameObject);

            if (_player == null)
            {
                continue;
            }

            var tempGameObject = (TemporaryGameObject)gameObject!;
            var deltaX = Math.Abs(_player.Position.X - tempGameObject.Position.X);
            var deltaY = Math.Abs(_player.Position.Y - tempGameObject.Position.Y);
            if (deltaX < 32 && deltaY < 32)
            {
                _player.GameOver();
            }
        }

        CheckSlimePlayerCollisions();

        _player?.Render(_renderer);
    }

    public void RenderTerrain()
    {
        foreach (var currentLayer in _currentLevel.Layers)
        {
            for (int i = 0; i < _currentLevel.Width; ++i)
            {
                for (int j = 0; j < _currentLevel.Height; ++j)
                {
                    int? dataIndex = j * currentLayer.Width + i;
                    if (dataIndex == null)
                    {
                        continue;
                    }

                    var currentTileId = currentLayer.Data[dataIndex.Value] - 1;
                    if (currentTileId == null)
                    {
                        continue;
                    }

                    var currentTile = _tileIdMap[currentTileId.Value];

                    var tileWidth = currentTile.ImageWidth ?? 0;
                    var tileHeight = currentTile.ImageHeight ?? 0;

                    var sourceRect = new Rectangle<int>(0, 0, tileWidth, tileHeight);
                    var destRect = new Rectangle<int>(i * tileWidth, j * tileHeight, tileWidth, tileHeight);
                    _renderer.RenderTexture(currentTile.TextureId, sourceRect, destRect);
                }
            }
        }
    }

    public IEnumerable<RenderableGameObject> GetRenderables()
    {
        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is RenderableGameObject renderableGameObject)
            {
                yield return renderableGameObject;
            }
        }
    }

    public (int X, int Y) GetPlayerPosition()
    {
        return _player!.Position;
    }

    public void AddBomb(int X, int Y, bool translateCoordinates = true)
    {
        var worldCoords = translateCoordinates ? _renderer.ToWorldCoordinates(X, Y) : new Vector2D<int>(X, Y);

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }

    public void SpawnRandomSlime()
    {
        if (_player == null) return;

        var playerPos = _player.Position;
        var spawnPos = ChooseSlimeSpawnLocation(playerPos);
        var slimeType = ChooseSlimeType();

        AddSlime(spawnPos.X, spawnPos.Y, slimeType);
    }

    private (int X, int Y) ChooseSlimeSpawnLocation((int X, int Y) playerPos)
    {
        var random = new Random();
        var spawnDistance = 100;

        var spawnOptions = new[]
        {
            (X: random.Next(_worldBounds.Origin.X, _worldBounds.Origin.X + _worldBounds.Size.X),
             Y: _worldBounds.Origin.Y - spawnDistance),
            (X: _worldBounds.Origin.X + _worldBounds.Size.X + spawnDistance,
             Y: random.Next(_worldBounds.Origin.Y, _worldBounds.Origin.Y + _worldBounds.Size.Y)),
            (X: random.Next(_worldBounds.Origin.X, _worldBounds.Origin.X + _worldBounds.Size.X),
             Y: _worldBounds.Origin.Y + _worldBounds.Size.Y + spawnDistance),
            (X: _worldBounds.Origin.X - spawnDistance,
             Y: random.Next(_worldBounds.Origin.Y, _worldBounds.Origin.Y + _worldBounds.Size.Y))
        };

        var distances = spawnOptions.Select(option =>
        {
            var dx = option.X - playerPos.X;
            var dy = option.Y - playerPos.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }).ToArray();

        var totalDistance = distances.Sum();
        if (totalDistance == 0) return spawnOptions[random.Next(spawnOptions.Length)];

        var randomValue = random.NextDouble() * totalDistance;
        var cumulativeDistance = 0.0;

        for (int i = 0; i < spawnOptions.Length; i++)
        {
            cumulativeDistance += distances[i];
            if (randomValue <= cumulativeDistance)
            {
                return spawnOptions[i];
            }
        }

        return spawnOptions[random.Next(spawnOptions.Length)];
    }

    private SlimeEnemy.SlimeType ChooseSlimeType()
    {
        var random = new Random();
        var roll = random.NextDouble();

        if (roll < 0.6)
            return SlimeEnemy.SlimeType.Normal;
        else if (roll < 0.8)
            return SlimeEnemy.SlimeType.Fast;
        else if (roll < 0.95)
            return SlimeEnemy.SlimeType.Heavy;
        else
            return SlimeEnemy.SlimeType.Erratic;
    }

    public void AddSlime(int x, int y, SlimeEnemy.SlimeType type = SlimeEnemy.SlimeType.Normal)
    {
        try
        {
            var spriteSheet = SpriteSheet.Load(_renderer, "Slime.json", "Assets");
            var slime = new SlimeEnemy(spriteSheet, (x, y), type);
            _gameObjects.Add(slime.Id, slime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not create slime: {ex.Message}");
        }
    }

    public int GetSlimeCount()
    {
        return _gameObjects.Values.OfType<SlimeEnemy>().Count(s => s.IsAlive);
    }

    public Rectangle<int> GetWorldBounds()
    {
        return _worldBounds;
    }

    private void UpdateSlimes(double deltaTimeMs)
    {
        if (_player == null) return;

        var playerPosition = _player.Position;
        var slimesToRemove = new List<int>();

        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is SlimeEnemy slime)
            {
                if (slime.IsAlive)
                {
                    slime.Update(deltaTimeMs, playerPosition, _worldBounds);
                }
                else
                {
                    if (slime.SpriteSheet.AnimationFinished)
                    {
                        slimesToRemove.Add(slime.Id);
                    }
                }
            }
        }

        foreach (var id in slimesToRemove)
        {
            _gameObjects.Remove(id);
        }
    }

    private void CheckSlimePlayerCollisions()
    {
        if (_player == null) return;

        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is SlimeEnemy slime && slime.IsAlive)
            {
                if (slime.CheckCollisionWithPlayer(_player.Position, 32))
                {
                    _player.GameOver();
                    return;
                }
            }
        }
    }

    private void CheckBombSlimeCollisions((int X, int Y) bombPosition)
    {
        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is SlimeEnemy slime)
            {
                slime.CheckCollisionWithBomb(bombPosition, 32);
            }
        }
    }
}