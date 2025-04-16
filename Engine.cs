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

    // --- new fields -----------------------------------------------
    private readonly List<EnemyObject> _enemies = new();
    private readonly List<Bomb> _bombs = new();
    private bool _gameOver;
    // --- end new fields -------------------------------------------

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

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

        // --- spawn a single patrolling enemy -------------------------------
        SpriteSheet enemySheet = new(_renderer, Path.Combine("Assets", "player.png"),
                                 1, 1, 48, 48, (24, 24)); // reuse player sprite to save disk space

        var waypoints = new List<(int,int)>
        {
            (300, 100), (500, 100), (500, 200), (300, 200)
        };

        var enemy = new EnemyObject(enemySheet, waypoints, waypoints[0]);
        _enemies.Add(enemy);
        _gameObjects.Add(enemy.Id, enemy);   // so it renders
    }

    public void ProcessFrame()
    {
        if (_gameOver) return;

        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;

        _player?.UpdatePosition(up, down, left, right, (int)msSinceLastFrame);

        HandleBombs();

        foreach (var enemy in _enemies.ToArray())
        {
            bool caught = enemy.Update((int)msSinceLastFrame, _player!.X, _player.Y);
            if (caught)
            {
                Console.WriteLine("💀  Player was caught! Game over.");
                _gameOver = true;
            }
        }
    }

    private void HandleBombs()
    {
        const int  radius          = 80;          // pixels
        const double earlyBurstSec = 1.0;         // explodes 1 second *before* TTL ends

        foreach (var bomb in _bombs.ToArray())
        {
            // time elapsed
            double elapsed = (DateTimeOffset.Now - bomb.SpawnTime).TotalSeconds;
            double explodeAt = bomb.Ttl - earlyBurstSec;

            // detonate once when elapsed passes explodeAt
            if (!bomb.Detonated && elapsed >= explodeAt)
            {
                bomb.Detonated = true;

                foreach (var enemy in _enemies.ToArray())
                {
                    double dx = enemy.Position.X - bomb.Position.X;
                    double dy = enemy.Position.Y - bomb.Position.Y;
                    if (Math.Sqrt(dx * dx + dy * dy) <= radius)
                    {
                        enemy.Kill();
                        _enemies.Remove(enemy);
                        Console.WriteLine("🔥  Enemy destroyed by bomb!");
                    }
                }
            }

            // clean‑up sprite object after full TTL
            if (bomb.IsExpired)
            {
                _gameObjects.Remove(bomb.Id);
                _bombs.Remove(bomb);
            }
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

        Bomb bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y)); // <- Bomb, not TemporaryGameObject
        _gameObjects.Add(bomb.Id, bomb);
        _bombs.Add(bomb);
    }
}