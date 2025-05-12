using System.Reflection;
using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Generation;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;

namespace TheAdventure;

public class Engine
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private readonly ScriptEngine _scriptEngine = new();
    private readonly TerrainGenerator _terrainGenerator;

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
        _terrainGenerator = new TerrainGenerator(Random.Shared.Next());

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void SetupWorld()
    {
        _player = new(SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);

        var level = new Level
        {
            Width = 256,
            Height = 256,
            TileWidth = 16,
            TileHeight = 16,
            Layers = new List<Layer>
            {
                new Layer
                {
                    Width = 256,
                    Height = 256,
                    Data = GenerateTerrainData().Select(x => (int?)x).ToList()
                }
            },
            TileSets = new List<TileSetReference>
            {
                new TileSetReference { Source = "grass.tsj"}
            }
        };

        if (level == null)
        {
            throw new Exception("Failed to load level");
        }
        
        var (spawnX, spawnY) = _terrainGenerator.FindLandLocation();
        _player = new(SpriteSheet.Load(_renderer, "Player.json", "Assets"), spawnX, spawnY);

        
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

    private List<int?> GenerateTerrainData()
    {
        var data = new List<int?>(256 * 256);
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                var (tileId, height) = _terrainGenerator.GetTileAt(x, y);
                data.Add(tileId);
            }
        }
        return data;
    }
    
    public void ProcessFrame()
    {
        var playerPos = GetPlayerPosition();
        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is ItemObject item && !item.IsCollected)
            {
                if (item.CanBeCollected(playerPos))
                {
                    item.Collect();
                    _player?.CollectOre();
                }
            }
        }

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
        bool isSwimming = IsWaterTileAt(_player.Position.X, _player.Position.Y);
        bool addBomb = _input.IsKeyBPressed();

        // Calculate new position before updating
        var currentPos = _player.Position;
        var pixelsToMove = PlayerObject._speed * (msSinceLastFrame / 1000.0);
        var newX = currentPos.X + (int)((right - left) * pixelsToMove);
        var newY = currentPos.Y + (int)((down - up) * pixelsToMove);
        
        if (CanPlayerMove((newX, newY)))
        {
            _player.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame, isSwimming);
        }
        else
        {
            _player.UpdatePosition(0, 0, 0, 0, 48, 48, msSinceLastFrame, isSwimming);
        }

        if (isAttacking)
        {
            _player.Attack();
        }
        
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
        
        if (_player != null)
        {
            _renderer.RenderText($"Ores: {_player.OreCount}", 10, 10);
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

                    var currentTileId = currentLayer.Data[dataIndex.Value];
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

        // Schedule ore breaking when bomb expires
        Task.Delay(TimeSpan.FromSeconds(2.1)).ContinueWith(_ =>
        {
            CheckAndBreakOresNearBomb((worldCoords.X, worldCoords.Y));
        });
    }

    private void CheckAndBreakOresNearBomb((int X, int Y) bombPosition)
    {
        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is OreObject ore && !ore.IsDestroyed)
            {
                if (ore.IsNearBomb(bombPosition))
                {
                    ore.Break();
                    AddItem(ore.Position.X, ore.Position.Y);
                }
            }
        }
    }

    public void AddOre(int x, int y, bool translateCoordinates = true)
    {
        var worldCoords = translateCoordinates ? _renderer.ToWorldCoordinates(x, y) : new Vector2D<int>(x, y);
        
        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "Ore.json", "Assets");
        
        OreObject ore = new(spriteSheet, worldCoords.X,worldCoords.Y);
        _gameObjects.Add(ore.Id, ore);
        Console.WriteLine("Ore added");
    }
    
    public void AddItem(int x, int y)
    {
        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "OreItem.json", "Assets");
        ItemObject item = new(spriteSheet, (x, y));
        _gameObjects.Add(item.Id, item);
    }


    private bool IsWaterTileAt(int x, int y)
    {
        int tileX = x / _currentLevel.TileWidth!.Value;
        int tileY = y / _currentLevel.TileHeight!.Value;

        if (tileX < 0 || tileY < 0 || tileX >= _currentLevel.Width || tileY >= _currentLevel.Height)
            return false;

        var index = tileY * _currentLevel.Width.Value + tileX;
        var tileId = _currentLevel.Layers[0].Data[index];

        if (tileId == null || !_tileIdMap.TryGetValue(tileId.Value, out var tile))
            return false;

        return tile.Id == 0 || tile.Id == 1 || tile.Id == 2;
    }
    
    public Dictionary<int, Tile> getTileMap()
    {
        return this._tileIdMap;
    }
    
    public bool CanPlayerMove((int X, int Y) newPosition)
    {
        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is OreObject ore && !ore.IsDestroyed)
            {
                if (ore.IsColliding(newPosition))
                {
                    return false;
                }
            }
        }
        return true;
    }
    public void MovePlayer(double up, double down, double left, double right, double speedX, double speedY, double deltaTime, bool isSwimming)
    {
        if (_player == null) return;

        var currentPos = _player.Position;
        int moveX = (int)(((right - left) * speedX * deltaTime) / 1000.0);
        int moveY = (int)(((down - up) * speedY * deltaTime) / 1000.0);
        
        if (isSwimming)
        {
            moveX /= 2;
            moveY /= 2;
        }

        var newPos = (currentPos.X + moveX, currentPos.Y + moveY);
    
        if (CanPlayerMove(newPos))
        {
            _player.Position = newPos;
        }
    }
}