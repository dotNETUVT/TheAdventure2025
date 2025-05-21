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

    private readonly List<EnemyObject> _enemies = new();
    private readonly Random _random = new();
    private DateTimeOffset _lastEnemySpawn = DateTimeOffset.Now;
    private DateTimeOffset _gameStartTime = DateTimeOffset.Now;
    private double _enemySpawnIntervalSeconds = 5.0;
    private const double _minEnemySpawnInterval = 2.5;
    private int _maxEnemies = 10;
    private const int _absoluteMaxEnemies = 15;

    // Wave system components
    private WaveSystem _waveSystem;
    private WaveUI _waveUI;
    private bool _waveCompletedThisFrame = false;

    private Level _currentLevel = new();
    private PlayerObject? _player;

    // Track player bombs - now can have multiple active bombs depending on buffs
    private List<int> _playerBombIds = new();

    private bool _wasAttackPressed = false;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    // Property to check if the game should exit
    public bool ShouldExit { get; private set; }
    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;
        _gameStartTime = DateTimeOffset.Now; // Initialize game start time

        // Initialize wave system
        _waveSystem = new WaveSystem();
        _waveUI = new WaveUI(_renderer, _waveSystem);

        // Show first wave started message
        _waveUI.ShowWaveStartMessage(1);

        // Subscribe to enemy death events to update wave system
        EnemyObject.OnEnemyDefeated += (enemy) =>
        {
            // Update wave system when enemy is defeated
            _waveSystem.EnemyDefeated();
        };

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void SetupWorld()
    {
        _player = new(SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);

        _enemies.Add(new EnemyObject(300, 300));

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
        bool isAttacking = _input.IsKeyAPressed();
        bool addBomb = _input.IsKeyBPressed();

        _player.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame);

        // Handle attack state changes
        if (isAttacking)
        {
            _player.Attack();
        }
        else if (_wasAttackPressed && !isAttacking && _player.State.State == PlayerObject.PlayerState.Attack)
        {
            // Attack button was released while in attack state, force return to previous state
            if (up != 0 || down != 0 || left != 0 || right != 0)
            {
                _player.SetState(PlayerObject.PlayerState.Move);
            }
            else
            {
                _player.SetState(PlayerObject.PlayerState.Idle);
            }
        }

        _wasAttackPressed = isAttacking;

        // Reset wave completion flag at the beginning of each frame
        _waveCompletedThisFrame = false;

        // Check if wave is completed (all enemies killed)
        if (_waveSystem.WaveCompleted && _enemies.Count == 0)
        {
            // Apply a random buff to the player
            var buffType = _waveSystem.GetRandomBuff();
            _player.ApplyBuff(buffType);

            // Show the buff message
            _waveUI.ShowBuffMessage(buffType);

            // Start the next wave
            _waveSystem.StartNextWave();

            // Show new wave message
            _waveUI.ShowWaveStartMessage(_waveSystem.CurrentWave);

            // Set flag so we don't repeatedly complete waves
            _waveCompletedThisFrame = true;

            // Update difficulty based on current wave
            UpdateDifficultyForWave();
        }

        // Check for enemy spawn if we still need more enemies
        if (_enemies.Count < _maxEnemies &&
            (currentTime - _lastEnemySpawn).TotalSeconds > _enemySpawnIntervalSeconds)
        {
            SpawnNewEnemy();
            _lastEnemySpawn = currentTime;
        }

        // Update enemies and check for hits
        UpdateEnemies(msSinceLastFrame);

        _scriptEngine.ExecuteAll(this);

        if (addBomb)
        {
            AddBomb(_player.Position.X, _player.Position.Y, false, true);
        }
    }

    private void SpawnNewEnemy()
    {
        // Spawn enemy at a random position around the player
        int minSpawnDistance = 250;
        int maxSpawnDistance = 400;
        int spawnDistance = _random.Next(minSpawnDistance, maxSpawnDistance);

        double angle = _random.NextDouble() * Math.PI * 2;

        int enemyX = _player!.Position.X + (int)(Math.Cos(angle) * spawnDistance);
        int enemyY = _player!.Position.Y + (int)(Math.Sin(angle) * spawnDistance);

        _enemies.Add(new EnemyObject(enemyX, enemyY));
    }

    private void UpdateEnemies(double msSinceLastFrame)
    {
        if (_player == null || _player.State.State == PlayerObject.PlayerState.GameOver)
        {
            return;
        }

        foreach (var enemy in _enemies)
        {
            enemy.UpdatePosition(_player, msSinceLastFrame);

            // Check if player hit enemy with sword
            enemy.CheckHit(_player);

            // Check if enemy collides with player
            if (enemy.CheckPlayerCollision(_player))
            {
                _player.GameOver();
                return;
            }
        }

        _enemies.RemoveAll(e => e.IsDeathAnimationFinished);
    }
    private void UpdateDifficulty()
    {
        // Calculate time elapsed since game started
        double minutesElapsed = (DateTimeOffset.Now - _gameStartTime).TotalMinutes;

        // Gradually decrease spawn interval down to minimum
        _enemySpawnIntervalSeconds = Math.Max(_minEnemySpawnInterval, 5.0 - (minutesElapsed * 0.25));

        // Gradually increase max enemies up to absolute maximum
        _maxEnemies = Math.Min(_absoluteMaxEnemies, 10 + (int)(minutesElapsed / 2));
    }

    private void UpdateDifficultyForWave()
    {
        // Use wave system to determine difficulty
        var waveDifficulty = _waveSystem.GetWaveDifficulty();

        // Apply wave-based difficulty settings
        _enemySpawnIntervalSeconds = waveDifficulty.SpawnIntervalSeconds;
        _maxEnemies = waveDifficulty.MaxEnemies;
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        var playerPosition = _player!.Position;
        _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);

        RenderTerrain();
        RenderAllObjects();

        // Render wave UI last so it appears on top
        _waveUI.Render();

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

            // If this was the player's bomb, clear the tracked ID
            if (_playerBombIds.Contains(id))
            {
                _playerBombIds.Remove(id);
            }

            if (_player == null)
            {
                continue;
            }

            var tempGameObject = (TemporaryGameObject)gameObject!;

            // Check for bomb hits on enemies
            // Use default bomb radius for now - in real implementation we would get from metadata
            float radiusMultiplier = 1.0f;

            foreach (var enemy in _enemies)
            {
                enemy.CheckBombHit(tempGameObject.Position.X, tempGameObject.Position.Y, radiusMultiplier);
            }

            // Check if bomb hits the player
            var deltaX = Math.Abs(_player.Position.X - tempGameObject.Position.X);
            var deltaY = Math.Abs(_player.Position.Y - tempGameObject.Position.Y);
            if (deltaX < 32 && deltaY < 32)
            {
                _player.GameOver();
            }
        }

        // Render all enemies
        foreach (var enemy in _enemies)
        {
            enemy.Render(_renderer);
        }

        _player?.Render(_renderer);

        // Check if the player is in GameOver state and if the animation is finished
        if (_player != null && _player.State.State == PlayerObject.PlayerState.GameOver && _player.SpriteSheet.AnimationFinished)
        {
            ShouldExit = true;
        }
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

    public (int X, int Y) GetPlayerPosition()
    {
        return _player!.Position;
    }

    public void AddBomb(int X, int Y, bool translateCoordinates = true, bool isPlayerBomb = false)
    {
        // If this is a player bomb and there's already an active player bomb, don't create a new one
        if (isPlayerBomb && _playerBombIds.Count > 0)
        {
            return;
        }

        var worldCoords = translateCoordinates ? _renderer.ToWorldCoordinates(X, Y) : new Vector2D<int>(X, Y);

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);

        // If this is a player bomb, save its ID
        if (isPlayerBomb)
        {
            _playerBombIds.Add(bomb.Id);
        }
    }
}