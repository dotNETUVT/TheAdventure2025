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

    private const int BOMB_DAMAGE = 25;
    private const int BOMB_BLAST_RADIUS = 48;

    private int _heartTextureId = -1;
    private TextureData _heartTextureData;
    private const int POINTS_PER_HEART = 25;
    private const int UI_HEART_DISPLAY_WIDTH = 24;
    private const int UI_HEART_DISPLAY_HEIGHT = 24;

    private const int PLAYER_START_X = 100;
    private const int PLAYER_START_Y = 100;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
        _input.OnMouseWheel += HandleMouseWheel;
    }

    private void HandleMouseWheel(int scrollY)
    {
        _renderer.AdjustCameraZoom(scrollY);
    }

    public void SetupWorld()
    {
        _player = new PlayerObject(SpriteSheet.Load(_renderer, "Player.json", "Assets"), PLAYER_START_X, PLAYER_START_Y);

        var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        var level = JsonSerializer.Deserialize<Level>(levelContent);
        if (level == null) throw new Exception("Failed to load level");

        foreach (var tileSetRef in level.TileSets)
        {
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
            { // Add null check for tileSet.Tiles
                foreach (var tile in tileSet.Tiles)
                {
                    if (tile.Image == null) continue;
                    var tileImagePath = Path.Combine("Assets", tile.Image);
                    if (!File.Exists(tileImagePath))
                    {
                        Console.WriteLine($"Warning: Tile image file not found: {tileImagePath}");
                        continue;
                    }
                    tile.TextureId = _renderer.LoadTexture(tileImagePath, out _);
                    if (tile.Id.HasValue) _tileIdMap.Add(tile.Id.Value, tile);
                }
            }
            if (!string.IsNullOrEmpty(tileSet.Name)) _loadedTileSets.Add(tileSet.Name, tileSet);
        }

        if (level.Width == null || level.Height == null) throw new Exception("Invalid level dimensions");
        if (level.TileWidth == null || level.TileHeight == null) throw new Exception("Invalid tile dimensions");

        _renderer.SetWorldBounds(new Rectangle<int>(0, 0, level.Width.Value * level.TileWidth.Value,
            level.Height.Value * level.TileHeight.Value));

        _renderer.ResetCamera(PLAYER_START_X, PLAYER_START_Y);

        _currentLevel = level;
        // Ensure the script path is correct, often relative to the execution directory
        string scriptPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Scripts");
        if (!Directory.Exists(scriptPath))
        { // Fallback for development if BaseDirectory is not solution root
            scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Assets", "Scripts"); // Adjust as per your dev structure
            if (!Directory.Exists(scriptPath)) scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts"); // Another common place
        }
        _scriptEngine.LoadAll(Path.GetFullPath(scriptPath));


        try
        {
            _heartTextureId = _renderer.LoadTexture(Path.Combine("Assets", "heart.png"), out _heartTextureData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load heart.png: {ex.Message}. Health UI will not be displayed.");
            _heartTextureId = -1;
        }
        InitializeGameElements();
    }

    private void InitializeGameElements()
    {
        _gameObjects.Clear();
        if (_scriptEngine != null) _scriptEngine.ReinitializeAllScripts(this);
    }

    private void RestartGame()
    {
        Console.WriteLine("Restarting game...");
        _player?.Reset();
        _renderer.ResetCamera(PLAYER_START_X, PLAYER_START_Y);
        InitializeGameElements();
    }

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;
        if (_player == null) return;
        if (_player.IsDead())
        {
            // Only check for restart key if enough time has passed since Game Over
            // to prevent instant restart if R was held down.
            // This requires tracking game over time or key release.
            // For simplicity now, just check IsKeyRPressed.
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

            // Ensure player spritesheet is available for frame width/height
            int playerFrameWidth = _player.SpriteSheet?.FrameWidth ?? 48; // Default if null
            int playerFrameHeight = _player.SpriteSheet?.FrameHeight ?? 48; // Default if null
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
        // Use ToList() to create a copy if _gameObjects can be modified by script execution or other logic during this loop
        foreach (var gameObject in _gameObjects.Values.ToList())
        {
            if (gameObject is TemporaryGameObject tempGameObject)
            {
                if (tempGameObject.IsExpired)
                {
                    toRemove.Add(tempGameObject.Id);
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
        if (_player != null && _heartTextureId != -1)
        {
            int healthToDisplay = _player.CurrentHealth;
            if (healthToDisplay <= 0 && _player.IsDead()) return;

            int numHearts = healthToDisplay / POINTS_PER_HEART;
            if (healthToDisplay > 0 && numHearts == 0 && healthToDisplay < POINTS_PER_HEART)
            {
                numHearts = 1;
            }

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
        foreach (var gameObject in GetRenderables())
        {
            gameObject.Render(_renderer);
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

                    // This GID mapping assumes a single tileset starting at GID 1 in Tiled.
                    // For multiple tilesets, you need to find the correct tileset based on GID range
                    // and then calculate the local tile ID within that tileset.
                    // Example simple mapping for one tileset:
                    int firstGid = _currentLevel.TileSets.FirstOrDefault()?.FirstGID ?? 1; // Get first GID or default to 1
                    var localTileId = tileGid.Value - firstGid;
                    if (localTileId < 0) continue; // GID is from a different (or no) tileset

                    if (!_tileIdMap.TryGetValue(localTileId, out var currentTile)) continue;

                    var tileWidthOnSprite = currentTile.ImageWidth ?? _currentLevel.TileWidth.Value;
                    var tileHeightOnSprite = currentTile.ImageHeight ?? _currentLevel.TileHeight.Value;

                    var sourceRect = new Rectangle<int>(0, 0, tileWidthOnSprite, tileHeightOnSprite);
                    var destRect = new Rectangle<int>(i * _currentLevel.TileWidth.Value, j * _currentLevel.TileHeight.Value,
                                                    _currentLevel.TileWidth.Value, _currentLevel.TileHeight.Value); // Render at grid size
                    _renderer.RenderTexture(currentTile.TextureId, sourceRect, destRect);
                }
            }
        }
    }

    public IEnumerable<RenderableGameObject> GetRenderables()
    {
        // ToList() if _gameObjects might be modified elsewhere during iteration
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
}