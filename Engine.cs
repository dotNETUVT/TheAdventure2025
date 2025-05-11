using System.Text.Json;
using Silk.NET.Maths;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;
using Color = System.Drawing.Color;


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
    
    private int _score = 0;
    private HighScore _highScoreData;
    private const int ScorePerBombSurvival = 10;
    private const int ScorePerChest = 50;
    
    private readonly HashSet<int> _openedChests = new();
    
    private bool _paused = false;
    private bool _prevEscapePressed = false;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void SetupWorld()
    {
        _tileIdMap.Clear();
        _loadedTileSets.Clear();
        LoadHighScore();
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

        _scriptEngine.LoadAll(Path.Combine("Assets", "Scripts"));
    }

    public void ProcessFrame()
    {
        bool esc = _input.IsKeyEscapePressed();
        if (esc && !_prevEscapePressed)
            _paused = !_paused;
        _prevEscapePressed = esc;

        if (_paused)
            return;
        
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        if (_player == null)
        {
            return;
        }
        
        if (_player?.IsGameOver ?? false)
        {
            var gameOver = new GameOverScreen(_renderer, _input, _score, _highScoreData.Score);
            if (gameOver.Run())
                ResetGame();
            else
                Environment.Exit(0);
        }
        
        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;
        bool isAttacking = _input.IsKeyAPressed() && (up + down + left + right <= 1);
        bool addBomb = _input.IsKeyBPressed();

        _player.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame);
        if (isAttacking)
        {
            _player.Attack();
        }
        
        _scriptEngine.ExecuteAll(this);

        if (addBomb)
        {
            AddBomb(_player.Position.X, _player.Position.Y, false);
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

        var scoreTex = CreateUITextTexture($"Score: {_score}", Color.White);
        var highScoreTex = CreateUITextTexture($"High Score: {_highScoreData.Score}", Color.Yellow);

        DrawUIText(scoreTex, 10, 10);
        DrawUIText(highScoreTex, 10, 40);
        
        if (_paused)
        {
            var pauseTex = CreateUITextTexture("PAUSED", Color.White);
            var resumeTex = CreateUITextTexture("Press ESC to resume", Color.White);

            DrawUIText(pauseTex, 640/2 - pauseTex.width/2, 400/2 - pauseTex.height);
            DrawUIText(resumeTex, 640/2 - resumeTex.width/2, 400/2 + 10);
        }
        
        _renderer.PresentFrame();
    }

    public void RenderAllObjects()
    {
        var toRemove = new List<int>();
        foreach (var gameObject in GetRenderables())
        {
            gameObject.Render(_renderer);
            if (gameObject is TemporaryGameObject tempGameObject)
            {
                bool isBomb = tempGameObject.SpriteSheet.FileName.Contains("Bomb");
                bool isChest = tempGameObject.SpriteSheet.FileName.Contains("TreasureChest");

                var deltaX = Math.Abs(_player!.Position.X - tempGameObject.Position.X);
                var deltaY = Math.Abs(_player.Position.Y - tempGameObject.Position.Y);

                if (isBomb && tempGameObject.IsExpired)
                {
                    if (deltaX < 32 && deltaY < 32)
                        _player.GameOver();
                    else
                        _score += ScorePerBombSurvival;

                    toRemove.Add(tempGameObject.Id);
                }
                else if (isChest)
                {
                    bool inAttackRange = deltaX < 48 && deltaY < 48;
                    bool idleAnimation = tempGameObject.SpriteSheet.ActiveAnimation == tempGameObject.SpriteSheet.Animations["Idle"];

                    if (!_openedChests.Contains(tempGameObject.Id)
                        && inAttackRange
                        && _player.State.State == PlayerObject.PlayerState.Attack
                        && idleAnimation)
                    {
                        tempGameObject.ActivateAnimation("Open");
                        _score += ScorePerChest;
                        _openedChests.Add(tempGameObject.Id);
                    }

                    bool expiredUntouched = tempGameObject.IsExpired && !_openedChests.Contains(tempGameObject.Id);
                    bool openedDone = _openedChests.Contains(tempGameObject.Id) && tempGameObject.SpriteSheet.AnimationFinished;
                    if (expiredUntouched || openedDone)
                        toRemove.Add(tempGameObject.Id);
                }
            }
        }

        foreach (var id in toRemove)
        {
            _gameObjects.Remove(id);
            _openedChests.Remove(id);
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
    
    public void AddTreasureChest(int X, int Y, bool translateCoordinates = true)
    {
        var worldCoords = translateCoordinates ? _renderer.ToWorldCoordinates(X, Y) : new Vector2D<int>(X, Y);

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "TreasureChest.json", "Assets");
        spriteSheet.ActivateAnimation("Idle");

        TemporaryGameObject chest = new(spriteSheet, 10, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(chest.Id, chest);
    }
    
    private void LoadHighScore()
    {
        var path = "highscore.json";
        _highScoreData = File.Exists(path)
            ? JsonSerializer.Deserialize<HighScore>(File.ReadAllText(path))!
            : new HighScore();
    }

    private void SaveHighScore()
    {
        if (_score > _highScoreData.Score)
        {
            _highScoreData.Score = _score;
            File.WriteAllText("highscore.json", JsonSerializer.Serialize(_highScoreData));
        }
    }

    private void ResetGame()
    {
        SaveHighScore();
        _score = 0;
        _gameObjects.Clear();
        SetupWorld();
    }
    
    private (int textureId, int width, int height) CreateUITextTexture(string text, Color color)
    {
        using var image = new Image<Rgba32>(300, 50);
        image.Mutate(ctx =>
        {
            ctx.Clear(new Rgba32(0, 0, 0, 0));
            ctx.DrawText(text, SystemFonts.CreateFont("Arial", 24), new Rgba32(color.R, color.G, color.B, color.A), new PointF(0, 0));
        });

        var tmpPath = Path.GetTempFileName();
        image.SaveAsPng(tmpPath);

        var textureId = _renderer.LoadTexture(tmpPath, out var texData);
        File.Delete(tmpPath);

        return (textureId, texData.Width, texData.Height);
    }

    private void DrawUIText((int textureId, int width, int height) tex, int x, int y)
    {
        var originalCameraPos = _renderer.ToWorldCoordinates(0, 0);
        _renderer.CameraLookAt(0, 0);

        _renderer.RenderUITexture(
            tex.textureId,
            new Rectangle<int>(0, 0, tex.width, tex.height),
            new Rectangle<int>(x, y, tex.width, tex.height));

        _renderer.CameraLookAt(originalCameraPos.X, originalCameraPos.Y);
    }
}
