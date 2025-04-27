using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;

namespace TheAdventure;

public class Engine
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    private Random _random = new Random();
    private double _enemySpawnTimer = 0;


    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void SetupWorld()
    {
        _player = new(_renderer);

        var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        var level = JsonSerializer.Deserialize<Level>(levelContent);
        if (level == null)
        {
            throw new Exception("Failed to load level");
        }

        foreach (var tileSetRef in level.TileSets)
        {
            var tileSetContent = File.ReadAllText(Path.Combine("Assets", tileSetRef.Source));
            var tileSet = JsonSerializer.Deserialize<TileSet>(tileSetContent);
            if (tileSet == null)
            {
                throw new Exception("Failed to load tile set");
            }

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

        _renderer.SetWorldBounds(new Rectangle<int>(0, 0, level.Width.Value * level.TileWidth.Value,
            level.Height.Value * level.TileHeight.Value));

        _currentLevel = level;
    }

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;

        _player?.UpdatePosition(up, down, left, right, (int)msSinceLastFrame);

        if (_player == null) return;

        foreach (var obj in _gameObjects.Values)
        {
            if (obj is EnemyObject enemy)
            {
                enemy.Update(_player, (int)msSinceLastFrame);
            }
        }

        _enemySpawnTimer += msSinceLastFrame;
        if (_enemySpawnTimer > 3000)
        {
            SpawnEnemy();
            _enemySpawnTimer = 0;
        }
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        _renderer.CameraLookAt(_player!.X, _player!.Y);

        RenderTerrain();
        RenderAllObjects();

        _renderer.PresentFrame();
    }

    public void RenderAllObjects()
    {
        var toRemove = new List<int>();
        foreach (var gameObject in GetRenderables())
        {
            gameObject.Render(_renderer);
            if (gameObject is TemporaryGameObject { IsExpired: true } tempGameObject)
            {
                toRemove.Add(tempGameObject.Id);
            }
        }

        foreach (var id in toRemove)
        {
            _gameObjects.Remove(id);
        }

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

    private void AddBomb(int screenX, int screenY)
    {
        var worldCoords = _renderer.ToWorldCoordinates(screenX, screenY);

        SpriteSheet spriteSheet = new(_renderer, Path.Combine("Assets", "BombExploding.png"), 1, 13, 32, 64, (16, 48));
        spriteSheet.Animations["Explode"] = new SpriteSheet.Animation
        {
            StartFrame = (0, 0),
            EndFrame = (0, 12),
            DurationMs = 2000,
            Loop = false
        };
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }

    private void SpawnEnemy()
    {
        if (_player == null) return;

        var bounds = _renderer.GetWorldBounds();

        int worldLeft = bounds.Origin.X;
        int worldTop = bounds.Origin.Y;
        int worldRight = worldLeft + bounds.Size.X;
        int worldBottom = worldTop + bounds.Size.Y;

        int spawnX = _random.Next(worldLeft, worldRight);
        int spawnY = _random.Next(worldTop, worldBottom);

        var dx = spawnX - _player.X;
        var dy = spawnY - _player.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance < 200)
        {
            spawnX += _random.Next(200, 400) * (_random.Next(0, 2) == 0 ? -1 : 1);
            spawnY += _random.Next(200, 400) * (_random.Next(0, 2) == 0 ? -1 : 1);
        }

        var enemy = new EnemyObject(_renderer, spawnX, spawnY);

        Console.WriteLine($"Enemy Created at Position: X={spawnX}, Y={spawnY}");

        _gameObjects.Add(enemy.Id, enemy);
    }



}