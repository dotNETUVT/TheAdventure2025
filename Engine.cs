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

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    private bool _gameOver = false;
    private DateTimeOffset _gameOverTime;

    public static Engine? Instance { get; private set; }
    public Input Input => _input;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;
        Instance = this;

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
    }


    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        var playerPosition = _player!.Position;
        _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);

        RenderTerrain();
        RenderAllObjects();

        RenderSprintMeter();

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


    private void UpdateGameLogic()
    {
        var explodingBombs = new List<TemporaryGameObject>();

        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is TemporaryGameObject tempObject)
            {
                tempObject.Update();

                if (tempObject.IsExploding)
                {
                    explodingBombs.Add(tempObject);
                }
            }
        }

        if (_player != null && !_player.IsDead)
        {
            foreach (var bomb in explodingBombs)
            {
                double distance = CalculateDistance(_player.Position, bomb.Position);
                if (distance <= bomb.ExplosionRadius)
                {
                    _player.Die();
                    break;
                }
            }
        }

        if (_player != null && _player.IsDead && _player.IsDeathAnimationComplete())
        {
            if (!_gameOver)
            {
                _gameOver = true;
                _gameOverTime = DateTimeOffset.Now;
            }

            if ((DateTimeOffset.Now - _gameOverTime).TotalSeconds > 3)
            {
                RestartGame();
            }
        }
    }

    private double CalculateDistance((int X, int Y) point1, (int X, int Y) point2)
    {
        int dx = point2.X - point1.X;
        int dy = point2.Y - point1.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;

        _player?.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame);

        UpdateGameLogic();

        RenderSprintMeter();
    }

    private void AddBomb(int screenX, int screenY)
    {
        var worldCoords = _renderer.ToWorldCoordinates(screenX, screenY);

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y),
                                       explosionRadius: 100);
        _gameObjects.Add(bomb.Id, bomb);
    }

    private void RestartGame()
    {
        _gameOver = false;
        _gameObjects.Clear();

        _player = new(SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);

        var playerPosition = _player.Position;
        _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);

    }

    private void RenderSprintMeter()
    {
        if (_player == null) return;

        var windowSize = _renderer.GetWindowSize();

        int meterWidth = 200;
        int meterHeight = 20;
        int meterX = windowSize.Width - meterWidth - 20; // 20px from right edge
        int meterY = 20; // 20px from top

        var meterRect = new Rectangle<int>(meterX, meterY, meterWidth, meterHeight);

        _renderer.SetDrawColor(100, 100, 100, 200);
        _renderer.DrawRect(meterRect);

        float fillPercentage = _player.GetSprintPercentage();
        int fillWidth = (int)(meterWidth * fillPercentage);

        var fillRect = new Rectangle<int>(meterX, meterY, fillWidth, meterHeight);
        _renderer.SetDrawColor(0, 255, 0, 200);
        _renderer.FillRect(fillRect);

        _renderer.SetDrawColor(255, 255, 255, 255);
        _renderer.DrawRect(meterRect);
    }
}