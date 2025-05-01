using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;

namespace TheAdventure;

public class Engine
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private readonly GameUI _gameUI; 

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();
    private readonly Dictionary<int, Vector2D<int>> _activeBombs = new();
    private readonly Dictionary<int, DateTimeOffset> _explodingBombs = new(); 

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    private const int MinimumBombDistance = 32;
    private const int ExplosionRadius = 25; 
    private const double ExplosionTimeThreshold = 2.0; 
    private const double GameOverScreenDelay = 1.0; 

    private DateTimeOffset _gameOverDelayStartTime = DateTimeOffset.MinValue;

    private int? _bombThatHitPlayerId = null;

    public Engine(GameRenderer renderer, Input input, GameUI gameUI) 
    {
        _renderer = renderer;
        _input = input;
        _gameUI = gameUI; 

        _input.OnMouseClick += OnMouseClick;
    }

    private void OnMouseClick(object? sender, (int x, int y) coords)
    {
        if (_gameUI.IsGameOverVisible)
        {
            return; 
        }
        
        if (_player?.IsDead == false)
        {
            AddBomb(coords.x, coords.y);
        }
    }
    
    public void RestartGame()
    {
        _gameObjects.Clear();
        _activeBombs.Clear();
        _explodingBombs.Clear();
        _bombThatHitPlayerId = null; 
        const int initialX = 100;
        const int initialY = 100;
        _player?.Reset(initialX, initialY);
        _renderer.CameraLookAt(initialX, initialY); 
        _gameUI.HideGameOver();
        _gameOverDelayStartTime = DateTimeOffset.MinValue; 
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

        if (_gameUI.IsGameOverVisible)
        {
            if (_input.IsRestartPressed())
            {
                RestartGame();
                return; 
            }
        }
        else if (_player != null && !_player.IsDead && _bombThatHitPlayerId == null) 
        {
            double up = _input.IsUpPressed() ? 1.0 : 0.0;
            double down = _input.IsDownPressed() ? 1.0 : 0.0;
            double left = _input.IsLeftPressed() ? 1.0 : 0.0;
            double right = _input.IsRightPressed() ? 1.0 : 0.0;

            _player.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame);
            
            CheckBombCollisions();
        }
        
        UpdateExplodingBombs(currentTime);

        if (_player != null && _player.IsDead && _gameOverDelayStartTime != DateTimeOffset.MinValue && !_gameUI.IsGameOverVisible)
        {
            if ((currentTime - _gameOverDelayStartTime).TotalSeconds >= GameOverScreenDelay)
            {
                _gameUI.ShowGameOver();
                _gameOverDelayStartTime = DateTimeOffset.MinValue; 
            }
        }
    }
    
    private void UpdateExplodingBombs(DateTimeOffset currentTime) 
    {
        var bombsToRemove = new List<int>();

        foreach (var kvp in _gameObjects)
        {
            if (kvp.Value is TemporaryGameObject tempBomb && _activeBombs.ContainsKey(tempBomb.Id))
            {
                 double timeLeft = tempBomb.Ttl - (currentTime - tempBomb.SpawnTime).TotalSeconds;
                 if (timeLeft <= ExplosionTimeThreshold && !_explodingBombs.ContainsKey(tempBomb.Id))
                 {
                     _explodingBombs[tempBomb.Id] = currentTime;
                 }
            }
        }
        
        foreach (var kvp in _gameObjects)
        {
             if (kvp.Value is TemporaryGameObject tempBomb && tempBomb.IsExpired) 
             {
                 bombsToRemove.Add(tempBomb.Id);
                 _activeBombs.Remove(tempBomb.Id);
                 _explodingBombs.Remove(tempBomb.Id); 

                 if (_player != null && !_player.IsDead && _bombThatHitPlayerId == tempBomb.Id)
                 {
                     _player.Die(); 
                     _gameOverDelayStartTime = currentTime; 
                     _bombThatHitPlayerId = null; 
                 }
             }
        }
        
        foreach (var id in bombsToRemove)
        {
            _gameObjects.Remove(id);
        }
    }

    private void CheckBombCollisions()
    {
        if (_player == null || _player.IsDead || _bombThatHitPlayerId != null) return; 

        var playerPos = new Vector2D<int>(_player.Position.X, _player.Position.Y);
        
        foreach (var bombId in _explodingBombs.Keys) 
        {
            if (_activeBombs.TryGetValue(bombId, out var bombPos))
            {
                var distance = Vector2D.Distance(playerPos, bombPos);
                if (distance < ExplosionRadius) 
                {
                    _bombThatHitPlayerId = bombId; 
                    break; 
                }
            }
        }
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        if (_player != null) 
        {
            var playerPosition = _player.Position;
            _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);
        }

        RenderTerrain();
        RenderAllObjects(); 
        
        _gameUI.Render();

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
                _activeBombs.Remove(tempGameObject.Id);
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

    private bool IsPositionNearActiveBomb(Vector2D<int> position)
    {
        foreach (var bombPos in _activeBombs.Values)
        {
            var distance = Vector2D.Distance(position, bombPos);
            if (distance < MinimumBombDistance)
            {
                return true;
            }
        }
        return false;
    }

    private void AddBomb(int screenX, int screenY)
    {
        if (_player?.IsDead == true) return; 
        
        var worldCoords = _renderer.ToWorldCoordinates(screenX, screenY);
        
        if (IsPositionNearActiveBomb(new Vector2D<int>(worldCoords.X, worldCoords.Y)))
        {
            return;
        }

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y)); 
        _gameObjects.Add(bomb.Id, bomb);
        
        _activeBombs.Add(bomb.Id, new Vector2D<int>(worldCoords.X, worldCoords.Y));
    }
}