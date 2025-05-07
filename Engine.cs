using System.Text.Json;
using Silk.NET.Maths;
using Silk.NET.SDL;
using TheAdventure.Models;
using TheAdventure.Models.Data;

namespace TheAdventure;

public class Engine
{
    private bool _shouldResetGame = false; // When hp goes to 0
    private DateTimeOffset _deathTime;
    private bool _isInDeathSequence = false;

    private readonly GameRenderer _renderer;
    private readonly Input _input;

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    private HealthBarRenderer? _healthBarRenderer;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void SetupWorld()
    {
        _gameObjects.Clear();

        _player = new PlayerObject(_renderer);
        _healthBarRenderer = new HealthBarRenderer(
            _renderer,
            _player,
            width: 50,
            height: 5,
            offsetY: -30,
            offsetX: 47
        );

        _player.OnPlayerDeath += (sender, args) => _shouldResetGame = true;


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

        _renderer.SetWorldBounds(new Rectangle<int>(
            0, 0,
            level.Width.Value * level.TileWidth.Value,
            level.Height.Value * level.TileHeight.Value
        ));

        _currentLevel = level;

        _renderer.LoadDeathScreenTexture();
    }
    private void AddHealthPack(int x, int y)
    {
        var spriteSheet = new SpriteSheet(
            _renderer,
            Path.Combine("Assets", "health.png"),
            1, 1, // rows, columns
            32, 32, // frame size
            (16, 16) // frame center (half of width and height)
        );

        HealthPackObject healthPack = new(spriteSheet, (x, y));
        _gameObjects.Add(healthPack.Id, healthPack);
    }
    private void AddSpeedPack(int x, int y)
    {
        var spriteSheet = new SpriteSheet(
            _renderer,
            Path.Combine("Assets", "speed.png"),
            1, 1, // rows, columns
            32, 32, // frame size
            (16, 16) // frame center (half of width and height)
        );

        SpeedPackObject speedPack = new(spriteSheet, (x, y));
        _gameObjects.Add(speedPack.Id, speedPack);
    }

    private void ResetGame()
    {
        _renderer.ClearAllTextures();


        _gameObjects.Clear();
        _loadedTileSets.Clear();
        _tileIdMap.Clear();

        SetupWorld();
        _shouldResetGame = false;


    }
    public void ProcessFrame()
    {
        if (_shouldResetGame && !_isInDeathSequence)
        {
            StartDeathSequence();
        }

        if (_isInDeathSequence)
        {
            var deathDuration = (DateTimeOffset.Now - _deathTime).TotalSeconds;
            if (deathDuration >= 2.0)
            {
                ResetGame();
                _isInDeathSequence = false;
            }
            return;
        }

        if (_shouldResetGame)
        {
            ResetGame();
            return;
        }

        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;

        _player?.UpdatePosition(up, down, left, right, (int)msSinceLastFrame);

        foreach (var obj in _gameObjects.Values)
        {
            if (obj is TemporaryGameObject bomb && !bomb.HasDealtDamage)
            {
                double timeSinceSpawn = (DateTimeOffset.Now - bomb.SpawnTime).TotalSeconds;
                double timeUntilExpire = bomb.Ttl - timeSinceSpawn;

                if (timeUntilExpire <= 1)
                {
                    double dx = bomb.X - _player!.X;
                    double dy = bomb.Y - _player.Y;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    const double explosionRadius = 75.0;
                    if (distance <= explosionRadius)
                    {
                        _player.TakeDamage(25);
                        Console.WriteLine($"Player hit by bomb! Health: {_player.CurrentHealth}/{_player.MaxHealth}");
                    }

                    bomb.HasDealtDamage = true;
                }
            }
        }

        Random rand = new Random();
        double spawnChance = 0.001; 
        if (rand.NextDouble() < spawnChance)
        {
            int randomX = rand.Next(0, 500);
            int randomY = rand.Next(0, 500);
           
            AddHealthPack(randomX, randomY);
        }
        Random randSpeed = new Random();
        double spawnChanceSpeed = 0.003;
        if (randSpeed.NextDouble() < spawnChanceSpeed)
        {
            int randomX = rand.Next(0, 500);
            int randomY = rand.Next(0, 500);

            AddSpeedPack(randomX, randomY);
        }


        var toRemove = new List<int>();
        foreach (var obj in _gameObjects.Values)
        {
            if (obj is HealthPackObject healthPack && !healthPack.IsCollected)
            {
                if (healthPack.CheckCollision((_player!.X, _player.Y)))
                {
                    healthPack.Collect(_player);
                    toRemove.Add(healthPack.Id);
                    Console.WriteLine($"Collected health pack at ({healthPack.X}, {healthPack.Y})");
                }
            }
        }
        foreach (var obj in _gameObjects.Values)
        {
            if (obj is SpeedPackObject speedPack && !speedPack.IsCollected)
            {
                if (speedPack.CheckCollision((_player!.X, _player.Y)))
                {
                    speedPack.Collect(_player);
                    toRemove.Add(speedPack.Id);
                    Console.WriteLine($"Collected speed pack at ({speedPack.X}, {speedPack.Y})");
                }
            }
        }

        foreach (var id in toRemove)
        {
            _gameObjects.Remove(id);
        }
    }

    public void RenderFrame()
    {
        if (_isInDeathSequence)
        {
            _renderer.RenderDeathScreen();
            return;
        }

        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        _renderer.CameraLookAt(_player!.X, _player!.Y);

        RenderTerrain();
        RenderAllObjects();
        _healthBarRenderer?.Render();

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



    private void StartDeathSequence()
    {
        _isInDeathSequence = true;
        _deathTime = DateTimeOffset.Now;
        Console.WriteLine("Player died - starting death sequence");
    }
}