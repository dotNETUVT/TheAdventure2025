using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using System.Collections.Generic;

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
    private List<OrcObject> _orcs = new();
    private List<OrcObject> _orcsToRemove = new();
    private List<HeartPickup> _heartPickups = new();
    private List<HeartPickup> _heartsToRemove = new();
    private List<BombObject> _bombs = new();

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void SetupWorld()
    {
        _gameObjects.Clear();
        _orcs.Clear();
        _orcsToRemove.Clear();
        _heartPickups.Clear();
        _heartsToRemove.Clear();
        _bombs.Clear();
        
        _player = new(_renderer);

        var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        var level = JsonSerializer.Deserialize<Level>(levelContent);
        if (level == null)
        {
            throw new Exception("Failed to load level");
        }

        if (_loadedTileSets.Count == 0)
        {
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

        SpawnOrcs(5);
    }
    
    private void SpawnOrcs(int count)
    {
        Random random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            int distance = random.Next(150, 400);
            double angle = random.NextDouble() * Math.PI * 2;
            
            int x = _player!.X + (int)(Math.Cos(angle) * distance);
            int y = _player!.Y + (int)(Math.Sin(angle) * distance);
            
            x = Math.Clamp(x, 100, (_currentLevel.Width ?? 60) * (_currentLevel.TileWidth ?? 16) - 100);
            y = Math.Clamp(y, 100, (_currentLevel.Height ?? 40) * (_currentLevel.TileHeight ?? 16) - 100);
            
            var orc = new OrcObject(_renderer, (x, y));
            orc.PlayerTarget = _player;
            _gameObjects.Add(orc.Id, orc);
            _orcs.Add(orc);
        }
    }

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        var deltaSeconds = msSinceLastFrame / 1000.0;
        _lastUpdate = currentTime;

        if (_input.IsRestartPressed())
        {
            SetupWorld();
            return;
        }

        if (_player?.IsDead == true)
        {
            return;
        }

        _player?.Update(_input, (int)msSinceLastFrame);

        _orcsToRemove.Clear();
        _heartsToRemove.Clear();

        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is BombObject bomb && bomb.ShouldDamage())
            {
                if (_player != null && !_player.IsDead && 
                    bomb.IsInExplosionRange(new Vector2D<int>(_player.X, _player.Y)))
                {
                    _player.TakeDamage(bomb.Damage);
                }
                
                foreach (var orc in _orcs)
                {
                    if (!orc.IsDead && bomb.IsInExplosionRange(new Vector2D<int>(orc.Position.X, orc.Position.Y)))
                    {
                        orc.TakeDamage(bomb.Damage);
                    }
                }
            }
        }

        foreach (var orc in _orcs)
        {
            orc.Update(deltaSeconds);
            
            if (_player != null && _player.IsCurrentlyAttacking() && !orc.IsDead)
            {
                if (_player.IsPositionInAttackArc(new Vector2D<int>(orc.Position.X, orc.Position.Y)))
                {
                    int distanceToDamageBonus(float distance)
                    {
                        float range = _player.AttackRange;
                        if (distance < range * 0.5f)
                            return (_player.AttackDamage * 3) / 2;
                        else
                            return _player.AttackDamage;
                    }
                    
                    int dx = orc.Position.X - _player.X;
                    int dy = orc.Position.Y - _player.Y;
                    float distance = MathF.Sqrt(dx * dx + dy * dy);
                    
                    int damage = distanceToDamageBonus(distance);
                    orc.TakeDamage(damage);
                }
            }
            
            if (orc.IsDead)
            {
                if (orc.DeathTime.HasValue && (DateTimeOffset.Now - orc.DeathTime.Value) > TimeSpan.FromSeconds(2))
                {
                    _orcsToRemove.Add(orc);
                }
            }
        }
        
        foreach (var heart in _heartPickups)
        {
            heart.Update(deltaSeconds);
            
            if (heart.IsPickedUp)
            {
                _heartsToRemove.Add(heart);
            }
        }
        
        Random random = new Random();
        foreach (var deadOrc in _orcsToRemove)
        {
            if (random.NextDouble() <= 0.6)
            {
                var heartPickup = new HeartPickup(_renderer, deadOrc.Position);
                heartPickup.PlayerTarget = _player;
                _heartPickups.Add(heartPickup);
                _gameObjects.Add(heartPickup.Id, heartPickup);
            }
            
            _orcs.Remove(deadOrc);
            _gameObjects.Remove(deadOrc.Id);
            
            if (_orcs.Count < 10 && random.NextDouble() < 0.2)
            {
                SpawnOrcs(1);
            }
        }
        
        foreach (var heart in _heartsToRemove)
        {
            _heartPickups.Remove(heart);
            _gameObjects.Remove(heart.Id);
        }

        var expiredIds = new List<int>();
        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is TemporaryGameObject tempGo && tempGo.IsExpired)
            {
                expiredIds.Add(tempGo.Id);
            }
        }

        foreach (var id in expiredIds)
        {
            _gameObjects.Remove(id);
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
        if (_player != null)
        {
        }
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

        BombObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y), 30, 120f);
        _gameObjects.Add(bomb.Id, bomb);
        _bombs.Add(bomb);
    }
}