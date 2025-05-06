using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;

namespace TheAdventure;

public class Engine
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();
    private readonly Dictionary<(int X, int Y), bool> _bombPositions = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;
    private DateTimeOffset _lastBombPlacement = DateTimeOffset.MinValue;
    private const double _bombPlacementCooldown = 1.5; // seconds
    private bool _isPaused = false;
    private bool _escapeWasPressed = false;
    private PauseMenu? _pauseMenu;
    private DateTimeOffset _pauseStartTime;
    private double _pauseTimeTotal = 0;



    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;
        _pauseMenu = new PauseMenu(renderer);

        _input.OnMouseClick += (_, coords) => TryAddBomb(coords.x, coords.y);
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

        var worldBounds = new Rectangle<int>(0, 0, level.Width.Value * level.TileWidth.Value,
            level.Height.Value * level.TileHeight.Value);

        _renderer.SetWorldBounds(worldBounds);

        // Set player world bounds
        _player.SetWorldBounds(worldBounds);

        _currentLevel = level;
    }

    public bool ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        // Check if pause state has changed
        bool wasPaused = _isPaused;

        // Handle pause toggle with Escape key
        if (_input.IsEscapePressed() && !_escapeWasPressed)
        {
            _isPaused = !_isPaused;

            // Record time when pausing starts/ends
            if (_isPaused)
            {
                _pauseStartTime = DateTimeOffset.Now;
            }
            else
            {
                // Add time spent paused to total
                _pauseTimeTotal += (DateTimeOffset.Now - _pauseStartTime).TotalSeconds;
            }

            // Immediately update all temporary objects when pause state changes
            foreach (var gameObject in _gameObjects.Values)
            {
                if (gameObject is TemporaryGameObject tempObject)
                {
                    tempObject.SetPaused(_isPaused);
                }
            }

            // Pause/unpause the player animations too
            _player?.SetPaused(_isPaused);
        }
        _escapeWasPressed = _input.IsEscapePressed();

        // If paused, process pause menu instead of game
        if (_isPaused)
        {
            bool shouldQuit = _pauseMenu!.ProcessInput(_input);

            // Check if we should resume
            if (_pauseMenu.ShouldResume())
            {
                _isPaused = false;

                // Add time spent paused to total
                _pauseTimeTotal += (DateTimeOffset.Now - _pauseStartTime).TotalSeconds;

                // Update all temporary objects when resuming
                foreach (var gameObject in _gameObjects.Values)
                {
                    if (gameObject is TemporaryGameObject tempObject)
                    {
                        tempObject.SetPaused(false);
                    }
                }

                // Unpause the player animations too
                _player?.SetPaused(false);
            }

            return shouldQuit; // Return if we should quit
        }

        // If pause state changed (but wasn't caught above), update all objects
        if (wasPaused && !_isPaused)
        {
            // Add time spent paused to total if not already counted
            _pauseTimeTotal += (DateTimeOffset.Now - _pauseStartTime).TotalSeconds;

            // Pause/unpause all temporary objects
            foreach (var gameObject in _gameObjects.Values)
            {
                if (gameObject is TemporaryGameObject tempObject)
                {
                    tempObject.SetPaused(false);
                }
            }

            // Unpause the player animations too
            _player?.SetPaused(false);
        }

        // Normal game processing when not paused
        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;

        _player?.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame);

        // Handle space bar for attack
        if (_input.IsSpacePressed())
        {
            _player?.Attack();
        }

        // Handle L key for lay down
        if (_input.IsLPressed())
        {
            _player?.LayDown();
        }

        return false; // Continue game
    }
    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        var playerPosition = _player!.Position;
        _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);

        RenderTerrain();
        RenderAllObjects();

        // Render pause menu on top if paused
        if (_isPaused && _pauseMenu != null)
        {
            _pauseMenu.Render();
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

                // Remove from bomb positions tracking
                foreach (var position in _bombPositions.Keys.ToList())
                {
                    if (Math.Abs(tempGameObject.Position.X - position.X) < 20 &&
                        Math.Abs(tempGameObject.Position.Y - position.Y) < 20)
                    {
                        _bombPositions.Remove(position);
                    }
                }
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

    private void TryAddBomb(int screenX, int screenY)
    {
        // Don't add bombs when paused
        if (_isPaused)
        {
            return;
        }

        var currentTime = DateTimeOffset.Now;

        // Check if cooldown has passed (accounting for pause time correctly)
        if (_lastBombPlacement != DateTimeOffset.MinValue)
        {
            double elapsedSinceLastBomb = (currentTime - _lastBombPlacement).TotalSeconds;

            // If we haven't been paused enough time to satisfy cooldown, return
            if (elapsedSinceLastBomb < _bombPlacementCooldown)
            {
                return;
            }
        }

        var worldCoords = _renderer.ToWorldCoordinates(screenX, screenY);

        // Get the world bounds from the camera
        var cameraWidth = (_currentLevel.Width ?? 0) * (_currentLevel.TileWidth ?? 0);
        var cameraHeight = (_currentLevel.Height ?? 0) * (_currentLevel.TileHeight ?? 0);

        // Ensure placement is within world boundaries
        var bombX = Math.Clamp(worldCoords.X, 0, cameraWidth);
        var bombY = Math.Clamp(worldCoords.Y, 0, cameraHeight);

        // Check if there's already a bomb at this position (with a smaller radius check)
        foreach (var position in _bombPositions.Keys)
        {
            if (Math.Abs(position.X - bombX) < 15 && Math.Abs(position.Y - bombY) < 15)
            {
                return; // Bomb already exists here
            }
        }

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (bombX, bombY));
        _gameObjects.Add(bomb.Id, bomb);

        // Track bomb position
        _bombPositions[(bombX, bombY)] = true;
        _pauseTimeTotal = 0;

        // Update cooldown with actual time
        _lastBombPlacement = currentTime;
    }
}