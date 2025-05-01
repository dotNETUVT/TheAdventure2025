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

    private bool _debugSkipToBoss = false;
    
    private BossOrcObject? _boss = null;
    private bool _bossSpawned = false;
    private readonly int _bossWave = 5;
    private bool _showBossMessage = false;
    private DateTimeOffset _bossSpawnTime = DateTimeOffset.MinValue;
    private readonly TimeSpan _bossMessageDuration = TimeSpan.FromSeconds(5);
    
    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;
    
    private int _totalEnemiesKilled = 0;
    
    private int _currentWave = 1;
    private int _orcsPerWave = 5;
    private bool _waveCompleted = false;
    private DateTimeOffset _nextWaveTime = DateTimeOffset.MinValue;
    private TimeSpan _waveCooldown = TimeSpan.FromSeconds(3);
    
    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void SetupWorld(bool debugSkipToBoss = false)
    {
        _debugSkipToBoss = debugSkipToBoss;
        
        _gameObjects.Clear();
        _orcs.Clear();
        _orcsToRemove.Clear();
        _heartPickups.Clear();
        _heartsToRemove.Clear();
        _bombs.Clear();
        
        _currentWave = _debugSkipToBoss ? _bossWave : 1;
        _orcsPerWave = 5;
        _waveCompleted = false;
        _nextWaveTime = DateTimeOffset.MinValue;
        _boss = null;
        _bossSpawned = false;
        
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

        SpawnOrcs(_orcsPerWave);
    }
    
    private void SpawnOrcs(int count)
    {
        Random random = new Random();
        
        if (_currentWave == _bossWave && !_bossSpawned && !_waveCompleted)
        {
            int distance = 500;
            double angle = random.NextDouble() * Math.PI * 2;
            
            int x = _player!.X + (int)(Math.Cos(angle) * distance);
            int y = _player!.Y + (int)(Math.Sin(angle) * distance);
            
            x = Math.Clamp(x, 100, (_currentLevel.Width ?? 60) * (_currentLevel.TileWidth ?? 16) - 100);
            y = Math.Clamp(y, 100, (_currentLevel.Height ?? 40) * (_currentLevel.TileHeight ?? 16) - 100);
            
            _boss = new BossOrcObject(_renderer, (x, y));
            _boss.PlayerTarget = _player;
            _gameObjects.Add(_boss.Id, _boss);
            _orcs.Add(_boss);
            _bossSpawned = true;
            
            _bossSpawnTime = DateTimeOffset.Now;
            _showBossMessage = true;
            
            _player?.SetEnemies(_orcs);
            
            return;
        }
        
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
        
        _player?.SetEnemies(_orcs);
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
        
        if (_input.IsDebugBossKeyPressed() && !_bossSpawned && !_debugSkipToBoss)
        {
            foreach (var orc in _orcs.ToList())
            {
                _gameObjects.Remove(orc.Id);
            }
            _orcs.Clear();
            
            _currentWave = _bossWave;
            _waveCompleted = false;
            SpawnOrcs(1);
            return;
        }

        if (_player != null && _currentWave > _player.HighestWaveReached)
        {
            _player.HighestWaveReached = _currentWave;
        }

        if (_bossSpawned && _boss != null && _boss.IsDead && _player != null && !_player.HasWon && !_player.IsDead)
        {
            _player.SetVictorious();
            return;
        }

        if (_player?.IsDead == true || _player?.HasWon == true)
        {
            return;
        }

        if (!_waveCompleted && _orcs.Count == 0)
        {
            _waveCompleted = true;
            _nextWaveTime = DateTimeOffset.Now + _waveCooldown;
        }

        if (_waveCompleted && DateTimeOffset.Now >= _nextWaveTime)
        {
            _currentWave++;
            _orcsPerWave = 5 + (_currentWave - 1) * 2;
            SpawnOrcs(_orcsPerWave);
            _waveCompleted = false;
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
        
        if (_orcsToRemove.Count > 0 && _player != null)
        {
            _player.SetEnemies(_orcs);
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
        
        _player?.RenderGameOverScreen(_renderer);
        _player?.RenderVictoryScreen(_renderer);
        
        if (_player?.IsDead != true && _player?.HasWon != true)
        {
            string waveText = "Wave: " + _currentWave;
            _renderer.RenderUIText(waveText, 20, 20, 255, 255, 255);
            
            if (_waveCompleted)
            {
                TimeSpan timeLeft = _nextWaveTime - DateTimeOffset.Now;
                if (timeLeft.TotalSeconds > 0)
                {
                    string nextWaveText = $"Next wave in {timeLeft.TotalSeconds:0} seconds";
                    _renderer.RenderUIText(nextWaveText, 20, 60, 255, 220, 100);
                }
            }
            else
            {
                string enemiesText = $"Enemies: {_orcs.Count}";
                _renderer.RenderUIText(enemiesText, 20, 60, 220, 100, 100);
            }

            if (_showBossMessage && (DateTimeOffset.Now - _bossSpawnTime) < _bossMessageDuration)
            {
                var windowSize = _renderer.GetWindowSize();
                int windowWidth = windowSize.Width;
                
                string bossMessage = "BOSS FIGHT!";
                int textWidth = bossMessage.Length * 15;
                _renderer.RenderUIText(bossMessage, windowWidth / 2 - textWidth / 2, 100, 255, 0, 0);
                
                string bossSubtitle = "Defeat the Orc Chieftain";
                textWidth = bossSubtitle.Length * 10;
                _renderer.RenderUIText(bossSubtitle, windowWidth / 2 - textWidth / 2, 140, 255, 200, 0);
            }
            else if ((DateTimeOffset.Now - _bossSpawnTime) >= _bossMessageDuration)
            {
                _showBossMessage = false;
            }

            foreach (var orc in _orcs)
            {
                if (!orc.IsDead)
                {
                    if (orc is BossOrcObject)
                    {
                        _renderer.RenderDirectionalArrow(orc.Position, 255, 0, 255);
                    }
                    else
                    {
                        _renderer.RenderDirectionalArrow(orc.Position, 255, 0, 0);
                    }
                }
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

        BombObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y), 30, 120f);
        _gameObjects.Add(bomb.Id, bomb);
        _bombs.Add(bomb);
    }
}