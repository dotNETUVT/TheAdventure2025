using System.Reflection;
using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;


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

    private int _lives = 3;
    private bool _isGameOver = false;
    private bool _awaitingRetry = false;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) =>
        {
            // added for stop adding bombs if user = dead
            if (!_awaitingRetry && !_isGameOver)
            {
                AddBomb(coords.x, coords.y);
            }
        };

    }

    private bool _scriptsLoaded = false;
    public void SetupWorld()
    {
        // clearing and setup a new world
        _tileIdMap.Clear();
        _loadedTileSets.Clear();
        _currentLevel = new();

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

        if (!_scriptsLoaded)
        {
            _scriptEngine.LoadAll(Path.Combine("Assets", "Scripts"));
            _scriptsLoaded = true;
        }
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
        bool addBomb = _input.IsKeyBPressed();
        // retry = button R
        bool retry = _input.IsKeyRPressed();
        // restartGame = button ENTER
        bool restartGame = _input.IsKeyEnterPressed();

        _player.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame);
        if (isAttacking)
        {
            _player.Attack();
        }

        // added for stop adding bombs if user = dead
        if (!_awaitingRetry && !_isGameOver)
        {
            _scriptEngine.ExecuteAll(this);
        }

        // added for stop adding bombs if user = dead
        if (addBomb && !_awaitingRetry && !_isGameOver)
        {
            AddBomb(_player.Position.X, _player.Position.Y, false);
        }
        // retry until gameOver
        if (_awaitingRetry && retry && !_isGameOver)
        {
            RestartLevel(keepLives: true);
            _awaitingRetry = false;
        }
        // gameOver 3/3 lifes taken -> restartGame
        if (_isGameOver && restartGame)
        {
            _lives = 3;
            _isGameOver = false;
            _awaitingRetry = false;
            RestartLevel(keepLives: false);
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

        if (!_isGameOver)
        {
            var livesTex = CreateUITextTexture($"Lives: {_lives}/3", new Rgba32(255, 255, 255));
            DrawUIText(livesTex, 20, 20);
        }

        if (_isGameOver)
        {
            var overTex = CreateUITextTexture("GAME OVER - Press ENTER to restart", new Rgba32(255, 0, 0));
            int x = (800 - overTex.width) / 2;   
            int y = (600 - overTex.height) / 2;  
            DrawUIText(overTex, x, y);
        }
        else if (_awaitingRetry)
        {
            var retryTex = CreateUITextTexture("You died! Press R to retry.", new Rgba32(255, 255, 255));
            DrawUIText(retryTex, 20, 60);
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
            if (deltaX < 32 && deltaY < 32 && !_awaitingRetry && !_isGameOver)
            {
                _lives--;
                _awaitingRetry = true;
                _player.GameOver();

                if (_lives == 0)
                {
                    _isGameOver = true;
                }
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

    public void AddBomb(int X, int Y, bool translateCoordinates = true)
    {
        var worldCoords = translateCoordinates ? _renderer.ToWorldCoordinates(X, Y) : new Vector2D<int>(X, Y);

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }
    // restartLevel
    public void RestartLevel(bool keepLives)
    {
        _gameObjects.Clear();
        SetupWorld();
    }

    private (int textureId, int width, int height) CreateUITextTexture(string text, Rgba32 color)
    {
        using var image = new Image<Rgba32>(600, 60);
        image.Mutate(ctx =>
        {
            ctx.Clear(new Rgba32(0, 0, 0, 0));
            ctx.DrawText(text, SystemFonts.CreateFont("Arial", 22), color, new PointF(0, 0));
        });

        var tmpPath = Path.GetTempFileName();
        image.SaveAsPng(tmpPath);

        var textureId = _renderer.LoadTexture(tmpPath, out var texData);
        File.Delete(tmpPath);

        return (textureId, texData.Width, texData.Height);
    }

    private void DrawUIText((int textureId, int width, int height) tex, int x, int y)
    {
        _renderer.RenderUITexture(
            tex.textureId,
            new Rectangle<int>(0, 0, tex.width, tex.height),
            new Rectangle<int>(x, y, tex.width, tex.height)
        );
    }

}