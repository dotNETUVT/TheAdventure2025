using System.Reflection;
using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;
using System.Collections.Generic; 
using System.Linq; 

namespace TheAdventure;

public class Engine
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private readonly ScriptEngine _scriptEngine = new();

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

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

        _scriptEngine.LoadAll(Path.Combine("Assets", "Scripts"));
    }

    public void SpawnSpeedBoostPowerUp(int x, int y)
    {
        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "SpeedBoost.json", "Assets");
   
        spriteSheet.ActivateAnimation("Idle"); 
        
        SpeedBoostPowerUp powerUp = new(spriteSheet, (x, y));
        _gameObjects.Add(powerUp.Id, powerUp);
    }

    private void ProcessPowerUpCollisions()
    {
        if (_player == null || _player.State.State == PlayerObject.PlayerState.GameOver) return;

        var playerBounds = _player.GetBoundingBox();
        List<int> collectedPowerUpIds = new List<int>();

        foreach (var kvp in _gameObjects)
        {
            if (kvp.Value is PowerUp powerUp && !powerUp.IsCollected)
            {
                var powerUpBounds = powerUp.GetBoundingBox();
                
                bool collision = playerBounds.Origin.X < powerUpBounds.Origin.X + powerUpBounds.Size.X &&
                                 playerBounds.Origin.X + playerBounds.Size.X > powerUpBounds.Origin.X &&
                                 playerBounds.Origin.Y < powerUpBounds.Origin.Y + powerUpBounds.Size.Y &&
                                 playerBounds.Origin.Y + playerBounds.Size.Y > powerUpBounds.Origin.Y;

                if (collision)
                {
                    powerUp.ApplyEffect(_player);
                    powerUp.Collect(); 
                    collectedPowerUpIds.Add(powerUp.Id);
                }
            }
        }
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
        
        ProcessPowerUpCollisions(); 

        _scriptEngine.ExecuteAll(this); // Scripts will run here, potentially spawning power-ups

        if (addBomb)
        {
            AddBomb(_player.Position.X, _player.Position.Y, false);
        }
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        if(_player == null) return; 

        var playerPosition = _player.Position;
        _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);

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
            else if (gameObject is PowerUp { IsCollected: true } powerUp)
            {
                toRemove.Add(powerUp.Id); 
            }
        }

        foreach (var id in toRemove)
        {
            if (_gameObjects.Remove(id, out var gameObject))
            {
                if (gameObject is TemporaryGameObject tempGameObject && _player != null)
                {
                    var deltaX = Math.Abs(_player.Position.X - tempGameObject.Position.X);
                    var deltaY = Math.Abs(_player.Position.Y - tempGameObject.Position.Y);
                    if (deltaX < 32 && deltaY < 32) 
                    {
                        _player.GameOver();
                    }
                }
            }
        }

        _player?.Render(_renderer);
    }

    public void RenderTerrain()
    {
        if(_currentLevel == null || _currentLevel.Layers == null) return;

        foreach (var currentLayer in _currentLevel.Layers)
        {
            if(currentLayer == null || currentLayer.Data == null || _currentLevel.Width == null || _currentLevel.Height == null || currentLayer.Width == null) continue;

            for (int i = 0; i < _currentLevel.Width; ++i)
            {
                for (int j = 0; j < _currentLevel.Height; ++j)
                {
                    int dataIndex = j * currentLayer.Width.Value + i; 
                    if (dataIndex < 0 || dataIndex >= currentLayer.Data.Count) 
                    {
                        continue;
                    }
                    
                    var tileGid = currentLayer.Data[dataIndex];
                    if (!tileGid.HasValue || tileGid.Value == 0) 
                    {
                        continue;
                    }

                    var currentTileId = tileGid.Value - 1; 
                    
                    if (!_tileIdMap.TryGetValue(currentTileId, out var currentTile))
                    {
                        continue;
                    }

                    var tileWidth = currentTile.ImageWidth ?? _currentLevel.TileWidth ?? 0;
                    var tileHeight = currentTile.ImageHeight ?? _currentLevel.TileHeight ?? 0;
                    if(tileWidth == 0 || tileHeight == 0) continue;


                    var sourceRect = new Rectangle<int>(0, 0, tileWidth, tileHeight);
                    var destRect = new Rectangle<int>(i * (_currentLevel.TileWidth ?? 0), j * (_currentLevel.TileHeight ?? 0), tileWidth, tileHeight);
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
        if (_player == null) return (0,0); 
        return _player.Position;
    }

    public void AddBomb(int X, int Y, bool translateCoordinates = true)
    {
        var worldCoords = translateCoordinates ? _renderer.ToWorldCoordinates(X, Y) : new Vector2D<int>(X, Y);

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }
}