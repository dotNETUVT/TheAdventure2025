using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;

namespace TheAdventure;

public class Engine
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private bool _wasSpacePressed = false;
    private bool _wasEPressed = false;

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private List<ChestObject> _chests = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;
    private PlayerObject? _player2;
    private bool _wasEnterPressed = false;
    private bool _wasRightShiftPressed = false;
    private bool _wasLeftShiftPressed = false;


    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        //_input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void SetupWorld()
    {
        _player = new(_renderer, isPlayerOne: true);
        _player2 = new(_renderer);
        _player2.X = _player.X - 50; // slight offset so they're not overlapping
        _player2.Y = _player.Y;


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

        InitializeChests();
    }

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        // Player 1 (Arrow keys)
        double up1 = _input.IsUpPressed() ? 1.0 : 0.0;
        double down1 = _input.IsDownPressed() ? 1.0 : 0.0;
        double left1 = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right1 = _input.IsRightPressed() ? 1.0 : 0.0;
        _player?.UpdatePosition(up1, down1, left1, right1, (int)msSinceLastFrame);

        // Player 2 (WASD)
        double up2 = _input.IsWPressed() ? 1.0 : 0.0;
        double down2 = _input.IsSPressed() ? 1.0 : 0.0;
        double left2 = _input.IsAPressed() ? 1.0 : 0.0;
        double right2 = _input.IsDPressed() ? 1.0 : 0.0;
        _player2?.UpdatePosition(up2, down2, left2, right2, (int)msSinceLastFrame);

        // --- Place Bombs ---
        // Player 1: Enter to place
        bool isEnterPressed = _input.IsEnterPressed();
        if (isEnterPressed && !_wasEnterPressed && _player != null)
        {
            if (_player.TryUseBomb())
                AddBomb(_player.X, _player.Y - 5);
        }
        _wasEnterPressed = isEnterPressed;

        // Player 2: Space to place bomb
        bool isSpacePressed = _input.IsSpacePressed();
        if (isSpacePressed && !_wasSpacePressed && _player2 != null)
        {
            if (_player2.TryUseBomb())
                AddBomb(_player2.X, _player2.Y - 5);
        }
        _wasSpacePressed = isSpacePressed;

        // --- Open chests ---
        // Player 1: Right Shift to open chests
        bool isRightShiftPressed = _input.IsRightShiftPressed();
        if (isRightShiftPressed && !_wasRightShiftPressed && _player != null && _player.BombCount < 5)
        {
            foreach (var chest in _chests)
            {
                if (chest.CanInteract((_player.X, _player.Y)))
                {
                    chest.Open(bombs => _player.AddBomb(bombs));
                    break;
                }
            }
        }
        _wasRightShiftPressed = isRightShiftPressed;

        // Player 2: Left Shift to open chests
        bool isLeftShiftPressed = _input.IsLeftShiftPressed();
        if (isLeftShiftPressed && !_wasLeftShiftPressed && _player2 != null && _player2.BombCount < 5)
        {
            foreach (var chest in _chests)
            {
                if (chest.CanInteract((_player2.X, _player2.Y)))
                {
                    chest.Open(bombs => _player2.AddBomb(bombs));
                    break;
                }
            }
        }
        _wasLeftShiftPressed = isLeftShiftPressed;


        // Calculate max distance based on camera view (window size)
        int maxX = (int)(_renderer.ScreenWidth * 0.95); // Padding to avoid edge clipping
        int maxY = (int)(_renderer.ScreenHeight * 0.95);

        ClampPlayersTogether(maxX, maxY);

        _player?.ClampToWorld(_renderer.WorldBounds);
        _player2?.ClampToWorld(_renderer.WorldBounds);
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        _renderer.CameraLookAt(_player!.X, _player!.Y, _player2!.X, _player2!.Y);

        RenderTerrain();
        RenderAllObjects();

        // Right HUD for Player 1
        if (_player != null) 
            RenderHUD(_player, alignRight: true);

        // Left HUD for Player 2
        if (_player2 != null)
            RenderHUD(_player2, alignRight: false);

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
        _player2?.Render(_renderer);
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

    public void AddBomb(int x, int y)
    {
        var spriteSheet = new SpriteSheet(
            _renderer,
            Path.Combine("Assets", "BombExploding.png"),
            rowCount: 1,
            columnCount: 13,
            frameWidth: 32,
            frameHeight: 64,
            frameCenter: (16, 48)
        );

        spriteSheet.Animations["Explode"] = new SpriteSheet.Animation
        {
            StartFrame = (0, 0),
            EndFrame = (0, 12),
            DurationMs = 2000,
            Loop = false
        };

        spriteSheet.ActivateAnimation("Explode");

        var bomb = new TemporaryGameObject(spriteSheet, ttl: 2.1, position: (x, y));
        _gameObjects.Add(bomb.Id, bomb);
    }

    private void RenderHUD(PlayerObject player, bool alignRight = false)
    {
        int maxBombs = 5;
        int gap = 2;
        int barWidth = maxBombs * 30 - gap;
        int barHeight = 20;
        int margin = 10;
        int totalGapWidth = (maxBombs - 1) * gap;
        int segmentWidth = (barWidth - totalGapWidth) / maxBombs;

        int screenWidth = _renderer.ScreenWidth;
        int xStart = alignRight
            ? screenWidth - margin - barWidth
            : margin;

        // Draw background bar in the correct position
        var backgroundRect = new Rectangle<int>(xStart, margin, barWidth, barHeight);
        _renderer.FillScreenRectangle(backgroundRect, 128, 128, 128, 255);

        // Draw each bomb segment with spacing
        for (int i = 0; i < player.BombCount; i++)
        {
            int segmentX = alignRight
                ? xStart + i * (segmentWidth + gap)
                : xStart + i * (segmentWidth + gap);

            var segmentRect = new Rectangle<int>(segmentX, margin, segmentWidth, barHeight);
            _renderer.FillScreenRectangle(segmentRect, 255, 0, 0, 255);
        }

        // Draw border
        _renderer.DrawScreenRectangle(backgroundRect, 255, 255, 255, 255);
    }

    private void InitializeChests()
    {
        var random = new Random();
        var placed = new List<(int X, int Y)>();

        int levelWidth = (_currentLevel.Width ?? 20) * (_currentLevel.TileWidth ?? 32);
        int levelHeight = (_currentLevel.Height ?? 20) * (_currentLevel.TileHeight ?? 32);

        // 1) Determine chest count based on area
        int area = levelWidth * levelHeight;
        int chestCount = Math.Max(1, area / 100_000);

        // 2) Enforce spacing = 1/3 of smaller map dimension
        int minSpacing = Math.Min(levelWidth, levelHeight) / 3;
        int minSqDist = minSpacing * minSpacing;

        while (placed.Count < chestCount)
        {
            var chestSpriteSheet = new SpriteSheet(
                _renderer,
                Path.Combine("Assets", "chest.png"),
                rowCount: 2,
                columnCount: 3,
                frameWidth: 56,
                frameHeight: 60,
                frameCenter: (28, 30)
            );

            chestSpriteSheet.Animations["Closed"] = new SpriteSheet.Animation
            {
                StartFrame = (0, 0),
                EndFrame = (0, 0),
                DurationMs = 1000,
                Loop = true
            };

            chestSpriteSheet.Animations["Opening"] = new SpriteSheet.Animation
            {
                StartFrame = (0, 0),
                EndFrame = (1, 2),
                DurationMs = 650,
                Loop = false
            };
            // leave a 100px margin all around
            int x = random.Next(100, levelWidth - 100);
            int y = random.Next(100, levelHeight - 100);

            // reject if it's too close to any existing chest
            if (placed.Any(p =>
            {
                int dx = p.X - x, dy = p.Y - y;
                return dx * dx + dy * dy < minSqDist;
            }))
                continue;

            placed.Add((x, y));

            var chest = new ChestObject(chestSpriteSheet, (x, y));
            chest.SpriteSheet.ActivateAnimation("Closed");

            _chests.Add(chest);
            _gameObjects.Add(chest.Id, chest);
        }
    }

    private void ClampPlayersTogether(int maxX, int maxY)
    {
        // Calculate horizontal and vertical separation
        int dx = _player2!.X - _player!.X;
        int dy = _player2.Y - _player.Y;

        // Clamp X-axis
        if (Math.Abs(dx) > maxX)
        {
            int excess = (Math.Abs(dx) - maxX) * Math.Sign(dx);
            int adjustment = excess / 2;
            _player.X += adjustment;
            _player2.X -= adjustment;
        }

        // Clamp Y-axis
        if (Math.Abs(dy) > maxY)
        {
            int excess = (Math.Abs(dy) - maxY) * Math.Sign(dy);
            int adjustment = excess / 2;
            _player.Y += adjustment;
            _player2.Y -= adjustment;
        }
    }
}