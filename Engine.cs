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

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();
    private DateTime _lastBombTime = DateTime.MinValue;
    private int? _cooldownTextureId = null;
    private DateTime? _cooldownMessageStart = null;

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    private const int BombCooldownMs = 600; // 0.6 sec
    private RenderableGameObject? _speedPowerUp;
    private DateTime _lastPowerUpSpawn = DateTime.MinValue;
    private static readonly Random _rng = new();

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;
        _cooldownTextureId = _renderer.LoadTexture(Path.Combine("Assets", "bomb_cooldown.png"), out _);
        _input.OnMouseClick += (_, coords) =>
        {
            if ((DateTime.Now - _lastBombTime).TotalMilliseconds >= BombCooldownMs)
            {
                AddBomb(coords.x, coords.y);
                _lastBombTime = DateTime.Now;
            }
            else
            {
                // write smth on screen to let the user know the bomb s on cooldown
                _cooldownMessageStart = DateTime.Now;
            }
        };
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

        int? waterGid = null;
        foreach (var tileSetRef in level.TileSets)
        {
            var tileSetContent = File.ReadAllText(Path.Combine("Assets", tileSetRef.Source));
            var tileSet = JsonSerializer.Deserialize<TileSet>(tileSetContent);
            if (tileSet == null)
                throw new Exception("Failed to load tile set");

            int firstGid = tileSetRef.FirstGID!.Value;

            foreach (var tile in tileSet.Tiles)
            {
                tile.TextureId = _renderer.LoadTexture(Path.Combine("Assets", tile.Image), out _);

                if (!string.IsNullOrEmpty(tile.Image) && tile.Image.Contains("water"))
                {
                    tile.IsWalkable = false;
                    waterGid = firstGid + tile.Id!.Value;
                }

                _tileIdMap[firstGid + tile.Id!.Value] = tile;
            }

            _loadedTileSets[tileSet.Name] = tileSet;

            if (waterGid.HasValue)
                break;
        }

        if (level.Width == null || level.Height == null)
        {
            throw new Exception("Invalid level dimensions");
        }

        if (level.TileWidth == null || level.TileHeight == null)
        {
            throw new Exception("Invalid tile dimensions");
        }

        int waterBlockCount = 12; // mai puține patch-uri, dar mai mari
        Random rng = new Random();


        int? waterTileId = 4;
        foreach (var tileSet in _loadedTileSets.Values)
        {
            foreach (var tile in tileSet.Tiles)
            {
                if (!string.IsNullOrEmpty(tile.Image) && tile.Image.Contains("water"))
                {
                    waterTileId = tile.Id;
                    break;
                }
            }
            if (waterTileId.HasValue)
                break;
        }

        if (!waterTileId.HasValue)
        {
            throw new Exception("Water tile not found in tilesets Assets folder");
        }

        foreach (var layer in level.Layers)
        {
            if (layer.Id == 4) continue;

            if (layer.Width is null || layer.Height is null)
                continue;

            for (int i = 0; i < waterBlockCount; i++)
            {
                int waterBlockWidth = rng.Next(2, 5); 
                int waterBlockHeight = rng.Next(2, 6);

                int startX = rng.Next(0, layer.Width.Value - waterBlockWidth);
                int startY = rng.Next(0, layer.Height.Value - waterBlockHeight);

                for (int dy = 0; dy < waterBlockHeight; dy++)
                {
                    for (int dx = 0; dx < waterBlockWidth; dx++)
                    {
                        if (rng.NextDouble() < 0.15 && (dx == 0 || dy == 0 || dx == waterBlockWidth - 1 || dy == waterBlockHeight - 1))
                            continue;

                        int x = startX + dx;
                        int y = startY + dy;
                        int tileIndex = y * layer.Width.Value + x;

                        if (tileIndex < 0 || tileIndex >= layer.Data.Count)
                            continue;

                        int? currentTile = layer.Data[tileIndex];

                        // check if the current tile is a grass block
                        if (currentTile.HasValue && _tileIdMap.TryGetValue(currentTile.Value, out var tile) && tile.Image.Contains("grass"))
                        {
                            layer.Data[tileIndex] = waterGid.Value;
                        }
                    }
                }
            }
        }

        _renderer.SetWorldBounds(new Rectangle<int>(0, 0, level.Width.Value * level.TileWidth.Value,
            level.Height.Value * level.TileHeight.Value));

        _currentLevel = level;

        _scriptEngine.LoadAll(Path.Combine("Assets", "Scripts"));
    }
    private bool CanWalkTo(int tileX, int tileY)
    {
        //Console.WriteLine($"x=: {tileX}, y={tileY}");
        var groundLayer = _currentLevel.Layers.FirstOrDefault(l => l.Type == "tilelayer" && l.Data != null);
        if (groundLayer == null || groundLayer.Width is null)
        {
            return true;
        }

        int index = tileY * groundLayer.Width.Value + tileX;
        if (index < 0 || index >= groundLayer.Data.Count)
            return true;

        int tileId = groundLayer.Data[index] ?? 0;

        if (_tileIdMap.TryGetValue(tileId, out var tile))
        {
            //Console.WriteLine($"Tile ID: {tileId}, Walkable: {tile.IsWalkable}");
            return tile.IsWalkable;
        }

        return true;
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
        bool addBomb = _input.IsKeyBPressed() && (DateTime.Now - _lastBombTime).TotalMilliseconds >= BombCooldownMs;

        if(addBomb)
            _lastBombTime = DateTime.Now;
        if(_input.IsKeyBPressed() && addBomb == false)
            _cooldownMessageStart = DateTime.Now;

        _player.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame, CanWalkTo);
        if (isAttacking)
        {
            _player.Attack();
        }
        
        _scriptEngine.ExecuteAll(this);
       if ((DateTime.Now - _lastPowerUpSpawn).TotalSeconds >= 10 && _speedPowerUp == null)
        {
            _lastPowerUpSpawn = DateTime.Now;

            var spriteSheet = SpriteSheet.Load(_renderer, "speed.json", "Assets");
            spriteSheet.ActivateAnimation("Default");

            int spawnX = _rng.Next(0, 500 - 48);
            int spawnY = _rng.Next(0, 500 - 48);

            _speedPowerUp = new TemporaryGameObject(spriteSheet, 9999, (spawnX, spawnY));
        }

        if (addBomb)
        {
            AddBomb(_player.Position.X, _player.Position.Y, false);
        }
    


        // Coliziune cu power-up
        if (_speedPowerUp is not null && _player is not null)
        {
            var dx = _player.Position.X - _speedPowerUp.Position.X;
            var dy = _player.Position.Y - _speedPowerUp.Position.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance < 40)
            {
                _player.ApplySpeedBoost(2.0, 3.0); // dublează viteza 3 secunde
                _speedPowerUp = null;
            }
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

        if (_cooldownTextureId.HasValue && _cooldownMessageStart.HasValue)
        {
            if ((DateTime.Now - _cooldownMessageStart.Value).TotalMilliseconds < 600)
            {
                var dst = new Silk.NET.Maths.Rectangle<int>(20, 20, 200, 40);
                var src = new Silk.NET.Maths.Rectangle<int>(0, 0, 200, 40);
                _renderer.RenderScreenSpaceTexture(_cooldownTextureId.Value, src, dst);
            }
            else
            {
                _cooldownMessageStart = null; //stergem mesajuk
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
        if (_speedPowerUp is not null)
        {
            _speedPowerUp.Render(_renderer);
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

                    if (!currentTileId.HasValue || !_tileIdMap.ContainsKey(currentTileId.Value))
                        continue;

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
}