using System.Reflection;
using System.Text.Json;
using Silk.NET.Maths;
using Silk.NET.SDL;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace TheAdventure;

public class Engine : IDisposable
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private readonly ScriptEngine _scriptEngine = new();
    private readonly AudioManagerNAudio _audioManager;

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    private const int BOMB_DAMAGE = 25;
    private const int BOMB_BLAST_RADIUS = 48;

    // Fields for UI Hearts
    private int _heartTextureId = -1; // Initialize to an invalid ID
    private TextureData _heartTextureData; // To store dimensions of heart.png

    // Fields for Health Pickups (if you've added them)
    private SpriteSheet? _healthPickupSpriteSheet;
    private const int HEALTH_PICKUP_AMOUNT = 25;


    private const int POINTS_PER_HEART = 25;
    private const int UI_HEART_DISPLAY_WIDTH = 24;
    private const int UI_HEART_DISPLAY_HEIGHT = 24;

    private const int PLAYER_START_X = 100;
    private const int PLAYER_START_Y = 100;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;
        _audioManager = new AudioManagerNAudio();

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
        _input.OnMouseWheel += HandleMouseWheel;
    }

    private void HandleMouseWheel(int scrollY)
    {
        _renderer.AdjustCameraZoom(scrollY);
    }

    public void SetupWorld()
    {
        // 1. Initialize AudioManager (NAudio doesn't need an explicit init call after construction for basic use)
        //    Load audio files
        string audioDir = Path.Combine("Assets"); // As per your last update
        if (!Directory.Exists(audioDir))
        {
            // Attempt to create if it doesn't exist, though assets should be pre-existing
            try { Directory.CreateDirectory(audioDir); }
            catch (Exception ex) { Console.WriteLine($"Could not create audio directory '{audioDir}': {ex.Message}"); }
        }

        // Use your specified paths
        _audioManager.LoadMusic(Path.Combine(audioDir, "background_music.mp3"), "background");
        _audioManager.LoadSoundEffect(Path.Combine(audioDir, "bomb_explosion.mp3"), "bomb_explosion");

        // 2. Load Player
        // Ensure Player.json and player.png are in the "Assets" folder
        _player = new PlayerObject(SpriteSheet.Load(_renderer, "Player.json", "Assets"), PLAYER_START_X, PLAYER_START_Y);

        // 3. Load Level Data (terrain.tmj and its tilesets)
        var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        var level = JsonSerializer.Deserialize<Level>(levelContent);
        if (level == null) throw new Exception("Failed to load level");

        if (level.TileSets != null) // Ensure TileSets list is not null
        {
            foreach (var tileSetRef in level.TileSets)
            {
                if (string.IsNullOrEmpty(tileSetRef.Source))
                {
                    Console.WriteLine("Warning: Tileset reference has no source.");
                    continue;
                }
                var tileSetFilePath = Path.Combine("Assets", tileSetRef.Source);
                if (!File.Exists(tileSetFilePath))
                {
                    Console.WriteLine($"Warning: Tileset source file not found: {tileSetFilePath}");
                    continue;
                }
                var tileSetContent = File.ReadAllText(tileSetFilePath);
                var tileSet = JsonSerializer.Deserialize<TileSet>(tileSetContent);
                if (tileSet == null)
                {
                    Console.WriteLine($"Warning: Failed to deserialize tileset: {tileSetRef.Source}");
                    continue;
                }
                if (tileSet.Tiles != null)
                {
                    foreach (var tile in tileSet.Tiles)
                    {
                        if (string.IsNullOrEmpty(tile.Image)) continue;
                        var tileImagePath = Path.Combine("Assets", tile.Image);
                        if (!File.Exists(tileImagePath))
                        {
                            Console.WriteLine($"Warning: Tile image file not found: {tileImagePath} for tileset {tileSet.Name}");
                            continue;
                        }
                        // Store texture data for each tile image if needed, or just get ID
                        tile.TextureId = _renderer.LoadTexture(tileImagePath, out _);
                        if (tile.Id.HasValue) _tileIdMap.Add(tile.Id.Value, tile);
                    }
                }
                if (!string.IsNullOrEmpty(tileSet.Name)) _loadedTileSets.Add(tileSet.Name, tileSet);
            }
        }

        if (level.Width == null || level.Height == null) throw new Exception("Level data is missing Width or Height.");
        if (level.TileWidth == null || level.TileHeight == null) throw new Exception("Level data is missing TileWidth or TileHeight.");

        _renderer.SetWorldBounds(new Rectangle<int>(0, 0, level.Width.Value * level.TileWidth.Value,
            level.Height.Value * level.TileHeight.Value));

        _renderer.ResetCamera(PLAYER_START_X, PLAYER_START_Y);
        _currentLevel = level;

        // 4. Load Scripts
        string scriptPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Scripts");
        if (!Directory.Exists(scriptPath))
        {
            scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts");
            if (!Directory.Exists(scriptPath))  // Fallback for typical project structures if BaseDirectory is bin/Debug
            {
                var projectRoot = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.FullName;
                if (projectRoot != null) scriptPath = Path.Combine(projectRoot, "Assets", "Scripts");
            }
        }
        if (Directory.Exists(Path.GetFullPath(scriptPath)))
        {
            _scriptEngine.LoadAll(Path.GetFullPath(scriptPath));
        }
        else
        {
            Console.WriteLine($"Warning: Script directory not found at any attempted path. Last tried: {Path.GetFullPath(scriptPath)}");
        }

        // 5. Load UI Heart Texture - THIS IS THE CRUCIAL PART FOR THE UI HEARTS
        // Ensure "heart.png" is a SEPARATE image file, specifically for the UI heart.
        // It should NOT be part of a larger spritesheet that _heartTextureId might accidentally point to.
        try
        {
            string heartImagePath = Path.Combine("Assets", "heart.png");
            if (!File.Exists(heartImagePath))
            {
                throw new FileNotFoundException($"heart.png not found at {heartImagePath}");
            }
            // This specific call loads "heart.png" and its data into _heartTextureId and _heartTextureData
            _heartTextureId = _renderer.LoadTexture(heartImagePath, out _heartTextureData);
            Console.WriteLine($"SetupWorld: Loaded heart.png for UI. TextureID: {_heartTextureId}, Original WxH: {_heartTextureData.Width}x{_heartTextureData.Height}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CRITICAL ERROR loading heart.png for UI: {ex.Message}. Health UI will not be displayed.");
            _heartTextureId = -1; // Mark as invalid if loading failed
        }

        // 6. Load Health PICKUP SpriteSheet (if you have this feature)
        // This uses "health_pickup.json" and its associated image. This is SEPARATE from the UI heart.
        try
        {
            string healthPickupJsonPath = Path.Combine("Assets", "health_pickup.json");
            if (File.Exists(healthPickupJsonPath))
            {
                _healthPickupSpriteSheet = SpriteSheet.Load(_renderer, "health_pickup.json", "Assets");
                Console.WriteLine("SetupWorld: Successfully loaded health_pickup.json spritesheet.");
            }
            else
            {
                Console.WriteLine("SetupWorld: health_pickup.json not found, health pickups may not have visuals if spawned.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SetupWorld: Failed to load health_pickup.json or its image: {ex.Message}");
            _healthPickupSpriteSheet = null;
        }

        // 7. Initialize other game elements (like spawning initial pickups) and start music
        InitializeGameElements();

        _audioManager.PlayMusic(true);
        _audioManager.SetMusicVolume(0.5f); // NAudio volume is 0.0f to 1.0f
    }

    // ... (InitializeGameElements, RestartGame, ProcessFrame, UpdateGameObjects, RenderFrame, RenderUI, RenderAllGameObjects, RenderTerrain, GetRenderables, GetPlayerPosition, AddBomb, Dispose methods should be the same as the last fully provided Engine.cs) ...
    // Make sure RenderUI uses _heartTextureId and _heartTextureData as intended.
    private void InitializeGameElements()
    {
        _gameObjects.Clear();
        if (_scriptEngine != null) _scriptEngine.ReinitializeAllScripts(this);

        // Spawn some health pickups for testing
        if (_healthPickupSpriteSheet != null)
        {
            SpawnHealthPickup(200, 200, HEALTH_PICKUP_AMOUNT);
            SpawnHealthPickup(300, 150, HEALTH_PICKUP_AMOUNT);
        }
    }

    private void SpawnHealthPickup(int x, int y, int healAmount)
    {
        if (_healthPickupSpriteSheet == null)
        {
            // Console.WriteLine("Cannot spawn health pickup, SpriteSheet not loaded."); // Already logged in SetupWorld
            return;
        }
        var pickup = new HealthPickup(_healthPickupSpriteSheet, (x, y), healAmount);
        _gameObjects.Add(pickup.Id, pickup);
    }

    private void RestartGame()
    {
        Console.WriteLine("Restarting game...");
        _player?.Reset();
        _renderer.ResetCamera(PLAYER_START_X, PLAYER_START_Y);
        InitializeGameElements();
        _audioManager.PlayMusic(true);
    }

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;
        if (_player == null) return;
        if (_player.IsDead())
        {
            if (_input.IsKeyRPressed())
            {
                RestartGame();
            }
        }
        else
        {
            double up = _input.IsUpPressed() ? 1.0 : 0.0;
            double down = _input.IsDownPressed() ? 1.0 : 0.0;
            double left = _input.IsLeftPressed() ? 1.0 : 0.0;
            double right = _input.IsRightPressed() ? 1.0 : 0.0;
            bool isAttacking = _input.IsKeyAPressed() && (up + down + left + right <= 0.1);
            bool addBomb = _input.IsKeyBPressed();
            int playerFrameWidth = _player.SpriteSheet?.FrameWidth ?? 48;
            int playerFrameHeight = _player.SpriteSheet?.FrameHeight ?? 48;
            _player.UpdatePosition(up, down, left, right, playerFrameWidth, playerFrameHeight, msSinceLastFrame);
            if (isAttacking)
            {
                _player.Attack();
            }
            if (addBomb)
            {
                AddBomb(_player.Position.X, _player.Position.Y, false);
            }
        }
        _scriptEngine.ExecuteAll(this);
        UpdateGameObjects();
    }

    private void UpdateGameObjects()
    {
        var toRemove = new List<int>();
        foreach (var gameObjectEntry in _gameObjects.ToList())
        {
            var gameObject = gameObjectEntry.Value;
            if (gameObject is TemporaryGameObject tempGameObject)
            {
                if (tempGameObject.IsExpired)
                {
                    toRemove.Add(tempGameObject.Id);
                    _audioManager.PlaySoundEffect("bomb_explosion");
                    if (_player != null && !_player.IsDead())
                    {
                        var playerPos = _player.Position;
                        var bombPos = tempGameObject.Position;
                        var deltaX = playerPos.X - bombPos.X;
                        var deltaY = playerPos.Y - bombPos.Y;
                        var distanceSq = (deltaX * deltaX) + (deltaY * deltaY);
                        if (distanceSq < BOMB_BLAST_RADIUS * BOMB_BLAST_RADIUS)
                        {
                            _player.TakeDamage(BOMB_DAMAGE);
                        }
                    }
                }
            }
            else if (gameObject is HealthPickup pickup)
            {
                if (_player != null && !_player.IsDead() && pickup.SpriteSheet != null) // Ensure pickup has spritesheet
                {
                    var playerRect = new Rectangle<int>(
                        _player.Position.X - (_player.SpriteSheet?.FrameCenter.OffsetX ?? 0),
                        _player.Position.Y - (_player.SpriteSheet?.FrameCenter.OffsetY ?? 0),
                        _player.SpriteSheet?.FrameWidth ?? 0,
                        _player.SpriteSheet?.FrameHeight ?? 0
                    );
                    var pickupRect = new Rectangle<int>(
                        pickup.Position.X - pickup.SpriteSheet.FrameCenter.OffsetX,
                        pickup.Position.Y - pickup.SpriteSheet.FrameCenter.OffsetY,
                        pickup.SpriteSheet.FrameWidth,
                        pickup.SpriteSheet.FrameHeight
                    );
                    if (playerRect.Size.X > 0 && playerRect.Size.Y > 0 && pickupRect.Size.X > 0 && pickupRect.Size.Y > 0 && // Ensure valid rects
                        playerRect.Origin.X < pickupRect.Origin.X + pickupRect.Size.X &&
                        playerRect.Origin.X + playerRect.Size.X > pickupRect.Origin.X &&
                        playerRect.Origin.Y < pickupRect.Origin.Y + pickupRect.Size.Y &&
                        playerRect.Origin.Y + playerRect.Size.Y > pickupRect.Origin.Y)
                    {
                        _player.Heal(pickup.HealAmount);
                        toRemove.Add(pickup.Id);
                        Console.WriteLine("Player collected health pickup.");
                    }
                }
            }
        }
        foreach (var id in toRemove)
        {
            _gameObjects.Remove(id);
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
        RenderAllGameObjects();
        RenderUI();
        _renderer.PresentFrame();
    }

    private void RenderUI()
    {
        if (_player != null && _heartTextureId != -1 && _heartTextureData.Width > 0 && _heartTextureData.Height > 0)
        {
            int healthToDisplay = _player.CurrentHealth;
            if (healthToDisplay <= 0 && _player.IsDead()) return;
            int numHearts = healthToDisplay / POINTS_PER_HEART;
            if (healthToDisplay > 0 && numHearts == 0)
            {
                numHearts = 1;
            }
            if (numHearts <= 0) return;
            int padding = 2;
            int startX = 10;
            int startY = 10;
            var srcRect = new Rectangle<int>(0, 0, _heartTextureData.Width, _heartTextureData.Height);
            for (int i = 0; i < numHearts; ++i)
            {
                var destScreenRect = new Rectangle<int>(
                    startX + i * (UI_HEART_DISPLAY_WIDTH + padding),
                    startY,
                    UI_HEART_DISPLAY_WIDTH,
                    UI_HEART_DISPLAY_HEIGHT
                );
                _renderer.RenderUITexture(_heartTextureId, srcRect, destScreenRect, RendererFlip.None, 0.0, new Point(0, 0));
            }
        }
    }
    public void RenderAllGameObjects()
    {
        foreach (var gameObject in _gameObjects.Values.ToList())
        {
            if (gameObject is RenderableGameObject renderable)
            {
                renderable.Render(_renderer);
            }
        }
        _player?.Render(_renderer);
    }
    public void RenderTerrain()
    {
        if (_currentLevel?.Layers == null) return;
        foreach (var currentLayer in _currentLevel.Layers)
        {
            if (currentLayer?.Data == null || !currentLayer.Width.HasValue || !currentLayer.Height.HasValue ||
                !_currentLevel.TileWidth.HasValue || !_currentLevel.TileHeight.HasValue) continue;
            for (int i = 0; i < currentLayer.Width.Value; ++i)
            {
                for (int j = 0; j < currentLayer.Height.Value; ++j)
                {
                    int dataIndex = j * currentLayer.Width.Value + i;
                    if (dataIndex >= currentLayer.Data.Count || dataIndex < 0) continue;
                    var tileGid = currentLayer.Data[dataIndex];
                    if (tileGid == null || tileGid.Value == 0) continue;
                    int firstGid = _currentLevel.TileSets.FirstOrDefault()?.FirstGID ?? 1;
                    var localTileId = tileGid.Value - firstGid;
                    if (localTileId < 0) continue;
                    if (!_tileIdMap.TryGetValue(localTileId, out var currentTile)) continue;
                    var tileWidthOnSprite = currentTile.ImageWidth ?? _currentLevel.TileWidth.Value;
                    var tileHeightOnSprite = currentTile.ImageHeight ?? _currentLevel.TileHeight.Value;
                    var sourceRect = new Rectangle<int>(0, 0, tileWidthOnSprite, tileHeightOnSprite);
                    var destRect = new Rectangle<int>(i * _currentLevel.TileWidth.Value, j * _currentLevel.TileHeight.Value,
                                                    _currentLevel.TileWidth.Value, _currentLevel.TileHeight.Value);
                    _renderer.RenderTexture(currentTile.TextureId, sourceRect, destRect);
                }
            }
        }
    }
    public IEnumerable<RenderableGameObject> GetRenderables() // Not currently used, RenderAllGameObjects iterates _gameObjects directly
    {
        foreach (var gameObject in _gameObjects.Values.ToList())
        {
            if (gameObject is RenderableGameObject renderableGameObject)
            {
                yield return renderableGameObject;
            }
        }
    }
    public (int X, int Y) GetPlayerPosition()
    {
        if (_player == null) return (PLAYER_START_X, PLAYER_START_Y);
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
    public void Dispose()
    {
        _audioManager?.Dispose();
    }
}