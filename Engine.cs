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
    private DateTimeOffset _lastEnemySpawnTime = DateTimeOffset.Now;
    private readonly Random _random = new();
    private readonly List<GameObject> _spawnQueue = new();
    private record PendingEnemySpawn(int X, int Y, string Type);
    private readonly List<PendingEnemySpawn> _enemySpawnQueue = new();
    private int? _deathOverlayTextureId = null;
    private TextureData _deathOverlayTextureData;
    private DateTimeOffset? _playerDeathTime = null;


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
        
        //this here is just for some static spawns to debug stuff
        
        //var bat = new EnemyObject(_renderer, _player, 300, 300); 
        //_gameObjects.Add(bat.Id, bat); 
        
        //var slime = new SlimeObject(_renderer, _player, 400, 400);
        //_gameObjects.Add(slime.Id, slime);

        
        var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        var level = JsonSerializer.Deserialize<Level>(levelContent);
        _deathOverlayTextureId = _renderer.LoadTexture(Path.Combine("Assets", "game_over.jpg"), out _deathOverlayTextureData);

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

        _renderer.SetWorldBounds(new Rectangle<int>(0, 0, 960, 640));

        _currentLevel = level;
    }
    
    private void SpawnRandomEnemy()
    {
        if (_player == null) return;

        int x = _random.Next(32, 960 - 32);
        int y = _random.Next(32, 640 - 32);

        var portal = new SpawnObject(_renderer, (x, y));
        _gameObjects.Add(portal.Id, portal);

        Task.Delay(600).ContinueWith(_ =>
        {
            var type = _random.Next(2) == 0 ? "Bat" : "Slime";
            lock (_enemySpawnQueue)
            {
                _enemySpawnQueue.Add(new PendingEnemySpawn(x, y, type));
            }
        });
    }
    
    public void ProcessFrame()
    {
        lock (_enemySpawnQueue)
        {
            foreach (var spawn in _enemySpawnQueue)
            {
                RenderableGameObject enemy = spawn.Type == "Bat"
                    ? new EnemyObject(_renderer, _player!, spawn.X, spawn.Y)
                    : new SlimeObject(_renderer, _player!, spawn.X, spawn.Y);

                _gameObjects.Add(enemy.Id, enemy);
            }

            _enemySpawnQueue.Clear();
        }
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;

        bool isAttack = _input.IsAttackPressed();
        bool isSprinting = _input.IsSprintPressed();

        _player?.UpdatePosition(up, down, left, right, (int)msSinceLastFrame, isAttack, isSprinting);
        
        if (_player != null && _player.IsDead() && _playerDeathTime == null)
        {
            _playerDeathTime = DateTimeOffset.Now;
        }

        foreach (var gameObject in _gameObjects.Values)
        {
            gameObject.Update((int)msSinceLastFrame);
        }
        
        if ((DateTimeOffset.Now - _lastEnemySpawnTime).TotalSeconds >= 5)
        {
            SpawnRandomEnemy();
            _lastEnemySpawnTime = DateTimeOffset.Now;
        }
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(34, 139, 34, 255);
        _renderer.ClearScreen();

        _renderer.CameraLookAt(_player!.X, _player!.Y);

        RenderTerrain();
        RenderAllObjects();

        if (_player!.IsDead() && _deathOverlayTextureId.HasValue && _playerDeathTime != null)
        {
            var elapsedSinceDeath = (DateTimeOffset.Now - _playerDeathTime.Value).TotalMilliseconds;
            if (elapsedSinceDeath >= 2000) 
            {
                var (winW, winH) = _renderer.WindowSize;
                var texW = _deathOverlayTextureData.Width;
                var texH = _deathOverlayTextureData.Height;

                var dstRect = new Rectangle<int>((winW - texW) / 2, (winH - texH) / 2, texW, texH);
                var srcRect = new Rectangle<int>(0, 0, texW, texH);

                _renderer.RenderRawTexture(_deathOverlayTextureId.Value, srcRect, dstRect);
            }
        }

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

        var enemies = _gameObjects.Values.OfType<EnemyObject>().ToList();
        BombObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y), _player!, enemies);
        _gameObjects.Add(bomb.Id, bomb);
    }
}