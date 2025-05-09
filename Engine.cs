using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;

namespace TheAdventure;

public class Engine
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    
    private bool _isGameOver;

    
    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;
    
    private readonly List<EnemyObject> _enemies = new();
    private readonly Random _random = new();

    private readonly List<CollectibleObject> _collectibles = new();

    
    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;
    private DateTimeOffset _lastEnemySpawn = DateTimeOffset.Now;
    private const double EnemySpawnInterval = 2000; 

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
    var level = JsonSerializer.Deserialize<Level>(levelContent);
        if (level == null)
        {
            throw new Exception("Failed to load level");
        }

        _tileIdMap.Clear();

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
            
                if (!_tileIdMap.ContainsKey(tile.Id!.Value))
                {
                    _tileIdMap.Add(tile.Id!.Value, tile);
                }
            }

            if (_loadedTileSets.ContainsKey(tileSet.Name))
            {
                _loadedTileSets.Remove(tileSet.Name);
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

   
public void RenderFrame()
{
    _renderer.Clear();
    _renderer.SetDrawColor(0, 0, 0, 255);
    _renderer.ClearScreen();

    if (_isGameOver)
    {
        _renderer.RenderGameOver();
    }
    else
    {
        var playerPosition = _player!.Position;
        _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);

        RenderTerrain();
        RenderAllObjects();
    }
    
    _renderer.PresentFrame();
}

    public void RenderTerrain()
    {
        foreach (var currentLayer in _currentLevel.Layers)
        {
            for (int i = 0;i < _currentLevel.Width; ++i)
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

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }
    
private void SpawnEnemy()
{
    if (_player == null)
        return;
        
    var spawnDistance = 300; 
    var angle = _random.NextDouble() * Math.PI * 2;
    
    var spawnX = _player.Position.X + Math.Cos(angle) * spawnDistance;
    var spawnY = _player.Position.Y + Math.Sin(angle) * spawnDistance;

    var enemySprite = SpriteSheet.Load(_renderer, "Enemy.json", "Assets");
    var enemy = new EnemyObject(enemySprite, spawnX, spawnY, _player);
    _enemies.Add(enemy);
}

   

private void UpdateEnemies(double deltaTime)
{
    var currentTime = DateTimeOffset.Now;
    if (_enemies.Count == 0 && (currentTime - _lastEnemySpawn).TotalMilliseconds >= EnemySpawnInterval)
    {
        SpawnEnemy();
        _lastEnemySpawn = currentTime;
    }

    var explodingBombs = _gameObjects.Values
        .OfType<TemporaryGameObject>()
        .Where(b => b.IsExploding)
        .Select(b => new Rectangle<double>(b.Position.X - 50, b.Position.Y - 50, 100, 100))
        .ToList();
    

    foreach (var enemy in _enemies.ToList())
    {
        enemy.Update(deltaTime);
        
        foreach (var bombBound in explodingBombs)
        {
            if (RectanglesIntersect(enemy.GetBounds(), bombBound))
            {
                var (enemyX, enemyY) = enemy.Position;
                
                enemy.Die();
                
                SpawnCollectible((int)enemyX, (int)enemyY);
                break;
            }
        }

        if (enemy.ShouldBeRemoved)
        {
            _enemies.Remove(enemy);
        }
    }
}

private void SpawnCollectible(int x, int y)
{
    var powerupSheet = SpriteSheet.Load(_renderer, "Powerup.json", "Assets");
    var collectible = new CollectibleObject(powerupSheet, x, y);
    _collectibles.Add(collectible);
    Console.WriteLine($"Collectibles count: {_collectibles.Count}");
}

    private bool RectanglesIntersect(Rectangle<double> a, Rectangle<double> b)
    {
        return a.Origin.X < (b.Origin.X + b.Size.X) &&
               (a.Origin.X + a.Size.X) > b.Origin.X &&
               a.Origin.Y < (b.Origin.Y + b.Size.Y) &&
               (a.Origin.Y + a.Size.Y) > b.Origin.Y;
    }

public void ProcessFrame()
{   
    if (_isGameOver)
    {
        if (_input.IsKeyPressed(KeyCode.R))
        {
            RestartGame();
        }
        return;
    }

    var currentTime = DateTimeOffset.Now;
    var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
    _lastUpdate = currentTime;

    double up = _input.IsUpPressed() ? 1.0 : 0.0;
    double down = _input.IsDownPressed() ? 1.0 : 0.0;
    double left = _input.IsLeftPressed() ? 1.0 : 0.0;
    double right = _input.IsRightPressed() ? 1.0 : 0.0;

    _player?.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame);
    UpdateEnemies(msSinceLastFrame);
    
    if (_player != null)
    {
        var explodingBombs = _gameObjects.Values
            .OfType<TemporaryGameObject>()
            .Where(b => b.IsExploding)
            .ToList();
            
        foreach (var bomb in explodingBombs)
        {
            var explosionBounds = new Rectangle<double>(
                bomb.Position.X - 50, 
                bomb.Position.Y - 50, 
                100, 
                100
            );
            
            var playerBounds = new Rectangle<double>(
                _player.Position.X - 16, 
                _player.Position.Y - 16, 
                32, 
                32
            );
            
            if (RectanglesIntersect(playerBounds, explosionBounds))
            {
                _player.Die();
                _isGameOver = true;
                return;
            }
        }
    }
    
    foreach (var collectible in _collectibles.ToList())
    {
        collectible.Update(msSinceLastFrame);
        
        if (_player != null && RectanglesIntersect(
            new Rectangle<double>(_player.Position.X - 16, _player.Position.Y - 16, 32, 32),
            collectible.GetBounds()))
        {
            _player.AddPower(collectible.PowerValue);
            _collectibles.Remove(collectible);
        }
    }
    
    foreach (var enemy in _enemies)
    {
        if (enemy.IsAlive && CheckCollision(_player, enemy))
        {
            _player.Die();
            _isGameOver = true;
            return;
        }
    }
}

private bool CheckCollision(RenderableGameObject obj1, EnemyObject enemy)
{
    var bounds1 = new Rectangle<double>(obj1.Position.X - 16, obj1.Position.Y - 16, 32, 32);
    var bounds2 = enemy.GetBounds();
    
    return bounds1.Origin.X < (bounds2.Origin.X + bounds2.Size.X) &&
           (bounds1.Origin.X + bounds1.Size.X) > bounds2.Origin.X &&
           bounds1.Origin.Y < (bounds2.Origin.Y + bounds2.Size.Y) &&
           (bounds1.Origin.Y + bounds1.Size.Y) > bounds2.Origin.Y;
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

    foreach (var collectible in _collectibles)
    {
        collectible.Render(_renderer);
    }

    foreach (var enemy in _enemies)
    {
        enemy.Render(_renderer);
    }

    _player?.Render(_renderer);
}

private void RestartGame()
{
    _isGameOver = false;
    _enemies.Clear();
    _gameObjects.Clear();
    _collectibles.Clear();
    
    _loadedTileSets.Clear();
    _tileIdMap.Clear();
    
    SetupWorld();
    _lastUpdate = DateTimeOffset.Now;
    _lastEnemySpawn = DateTimeOffset.Now;
}
    
}