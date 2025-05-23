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
    private ScriptEngine _scriptEngine = new();

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    // Pentru afișarea vieților doar când se schimbă
    private int _lastLivesShown = -1;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;
    }

    public void SetupWorld()
    {
        _player = new PlayerObject(SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);
        _player.Lives = 3;
        _player.ResetGameOver();

        var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        var level = JsonSerializer.Deserialize<Level>(levelContent);
        if (level == null) throw new Exception("Failed to load level");

        foreach (var tileSetRef in level.TileSets)
        {
            var tileSetContent = File.ReadAllText(Path.Combine("Assets", tileSetRef.Source));
            var tileSet = JsonSerializer.Deserialize<TileSet>(tileSetContent);
            if (tileSet == null) throw new Exception("Failed to load tile set");

            foreach (var tile in tileSet.Tiles)
            {
                tile.TextureId = _renderer.LoadTexture(Path.Combine("Assets", tile.Image), out _);
                _tileIdMap[tile.Id!.Value] = tile;
            }

            _loadedTileSets[tileSet.Name] = tileSet;
        }

        if (level.Width == null || level.Height == null || level.TileWidth == null || level.TileHeight == null)
        {
            throw new Exception("Invalid level dimensions");
        }

        _renderer.SetWorldBounds(new Rectangle<int>(
            0, 0,
            level.Width.Value * level.TileWidth.Value,
            level.Height.Value * level.TileHeight.Value
        ));

        _currentLevel = level;

        try
        {
            _scriptEngine.LoadAll(Path.Combine("Assets", "Scripts"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Script load failed: {ex.Message}");
        }
    }

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        if (_player != null && _player.IsGameOver && _input.IsKeyRPressed())
        {
            RestartGame();
            return;
        }

        if (_player == null || _player.IsGameOver) return;

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;
        bool isAttacking = _input.IsKeyAPressed() && (up + down + left + right <= 1);
        bool addBomb = _input.IsKeyBPressed();

        _player.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame);

        if (isAttacking)
            _player.Attack();

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

        // Afișăm viețile doar când se schimbă
        if (_player != null && _player.Lives != _lastLivesShown)
        {
            Console.WriteLine($"Lives: {_player.Lives}");
            _lastLivesShown = _player.Lives;
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
                toRemove.Add(tempGameObject.Id);
        }

        foreach (var id in toRemove)
        {
            _gameObjects.Remove(id, out var gameObject);

            if (_player == null) continue;

            var tempGameObject = (TemporaryGameObject)gameObject!;
            var deltaX = Math.Abs(_player.Position.X - tempGameObject.Position.X);
            var deltaY = Math.Abs(_player.Position.Y - tempGameObject.Position.Y);

            if (deltaX < 32 && deltaY < 32)
            {
                _player.DecreaseLife();

                if (_player.Lives <= 0)
                {
                    _player.GameOver();
                }
            }
            else if (!_player.IsGameOver)
            {
                ScoreSystem.AddPoints(10);
            }
        }

        _player?.Render(_renderer);
    }

    public void RenderTerrain()
    {
        foreach (var layer in _currentLevel.Layers)
        {
            if (layer.Width == null || layer.Height == null || layer.Data == null)
                continue;

            for (int i = 0; i < layer.Width; ++i)
            {
                for (int j = 0; j < layer.Height; ++j)
                {
                    int dataIndex = j * layer.Width.Value + i;
                    int? tileIdNullable = layer.Data[dataIndex];

                    if (!tileIdNullable.HasValue)
                        continue;

                    int tileId = tileIdNullable.Value - 1;

                    if (!_tileIdMap.TryGetValue(tileId, out var tile))
                        continue;

                    int tileWidth = tile.ImageWidth ?? 0;
                    int tileHeight = tile.ImageHeight ?? 0;

                    var srcRect = new Rectangle<int>(0, 0, tileWidth, tileHeight);
                    var dstRect = new Rectangle<int>(i * tileWidth, j * tileHeight, tileWidth, tileHeight);

                    _renderer.RenderTexture(tile.TextureId, srcRect, dstRect);
                }
            }
        }
    }

    public IEnumerable<RenderableGameObject> GetRenderables()
    {
        foreach (var obj in _gameObjects.Values)
        {
            if (obj is RenderableGameObject renderable)
                yield return renderable;
        }
    }

    public (int X, int Y) GetPlayerPosition() => _player!.Position;

    public void AddBomb(int x, int y, bool translateCoordinates = true)
    {
        var coords = translateCoordinates ? _renderer.ToWorldCoordinates(x, y) : new Vector2D<int>(x, y);

        

        SpriteSheet sheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        sheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(sheet, 2.1, (coords.X, coords.Y));
        _gameObjects[bomb.Id] = bomb;
    }

    private void RestartGame()
    {
        Console.WriteLine("Restarting game...");
        _gameObjects.Clear();
        _loadedTileSets.Clear();
        _tileIdMap.Clear();
        _scriptEngine = new ScriptEngine(); // eliberează DLL-urile blocate
        ScoreSystem.Reset();
        SetupWorld();
        _lastLivesShown = -1; // să afișeze viețile după restart
    }
}
