using System.Reflection;
using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;

namespace TheAdventure;

public class Engine : IDisposable
{



    private enum GameState
    {
        TitleScreen,
        Playing,
        Paused,
        GameOver
    }

    private GameState _state = GameState.TitleScreen;

    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private readonly ScriptEngine _scriptEngine = new();

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    private int _gameOverTextureId;
    private TextureData _gameOverTextureData;

    private int _titleTextureId;
    private TextureData _titleTextureData;

    private SoundManager _soundManager;
    private bool _gameOverSoundPlayed = false;

    private int _pauseTextureId;
    private TextureData _pauseTextureData;

   


    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) =>
    {
        if (_player == null) return;
        if (_player.State.State == PlayerObject.PlayerState.GameOver) return;

        AddBomb(coords.x, coords.y);
    };



        _soundManager = new SoundManager();

    }

    public void SetupWorld()
    {
        _titleTextureId = _renderer.LoadTexture("Assets/TitleScreen.png", out _titleTextureData);
        _pauseTextureId = _renderer.LoadTexture("Assets/PAUSED.png", out _pauseTextureData);


        _player = new(SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);
        _gameOverTextureId = _renderer.LoadTexture("Assets/GameOver.png", out _gameOverTextureData);


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
    /* ─── time bookkeeping ─────────────────────────────────────────── */
    var now           = DateTimeOffset.Now;
    double msSinceLast = (now - _lastUpdate).TotalMilliseconds;
    _lastUpdate        = now;

    if (_player == null) return;

    /* ─── TITLE SCREEN ─────────────────────────────────────────────── */
    if (_state == GameState.TitleScreen)
    {
        if (_input.IsKeyEnterPressed())
            _state = GameState.Playing;

        return;                             // wait on title screen
    }

    /* ─── GAME OVER : stop all gameplay / bombs / scripts ─────────── */
    if (_player.State.State == PlayerObject.PlayerState.GameOver)
        return;                             // nothing runs after death

    /* ─── ACTIVE GAME LOGIC (runs only while alive) ───────────────── */
    double up    = _input.IsUpPressed()    ? 1.0 : 0.0;
    double down  = _input.IsDownPressed()  ? 1.0 : 0.0;
    double left  = _input.IsLeftPressed()  ? 1.0 : 0.0;
    double right = _input.IsRightPressed() ? 1.0 : 0.0;

    bool attack  = _input.IsKeyAPressed()  && (up + down + left + right <= 1);
    bool addBomb = _input.IsKeyBPressed();

    _player.UpdatePosition(up, down, left, right, 48, 48, msSinceLast);

    if (attack)
        _player.Attack();

    /* scripts run only while alive */
    _scriptEngine.ExecuteAll(this);

    /* manual “B” bomb (alive only) */
    if (addBomb)
        AddBomb(_player.Position.X, _player.Position.Y, false);
}






    public void RenderFrame()
{
    _renderer.SetDrawColor(0, 0, 0, 255);
    _renderer.ClearScreen();

    // ─── TITLE SCREEN ───────────────────────────────
    if (_state == GameState.TitleScreen)
    {
        _renderer.DrawTitleScreen(_titleTextureId, _titleTextureData);
        _renderer.PresentFrame();
        return;
    }

    var playerPosition = _player!.Position;
    _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);

    RenderTerrain();
    RenderAllObjects();

    /* ----------  PAUSED overlay  ---------- */
if (_state == GameState.Paused)
{
    // 1) screen size
    int w = _renderer.GetWindowWidth();
    int h = _renderer.GetWindowHeight();

    // 2) scale the image to 50 % × 30 % of the screen
    int imgW = (int)(w * 0.5);
    int imgH = (int)(h * 0.3);

    // 3) destination (centered)
    var dst = new Rectangle<int>(
        (w - imgW) / 2,
        (h - imgH) / 2,
        imgW,
        imgH);

    // 4) source (full texture)
    var src = new Rectangle<int>(
        0, 0,
        _pauseTextureData.Width,
        _pauseTextureData.Height);

    // 5) draw & present
    _renderer.RenderTexture(_pauseTextureId, src, dst);
    _renderer.PresentFrame();
    return;                     // ⚠ stop the rest of the frame
}




    // ─── GAME OVER SCREEN ───────────────────────────────
        if (_player != null && _player.State.State == PlayerObject.PlayerState.GameOver)
        {
            if (!_gameOverSoundPlayed)
            {
                _soundManager.PlayGameOverSound();
                _gameOverSoundPlayed = true;
            }

            int screenW = _renderer.GetWindowWidth();
            int screenH = _renderer.GetWindowHeight();

            int imgW = (int)(screenW * 0.5);
            int imgH = (int)(screenH * 0.3);

            var dstRect = new Rectangle<int>(
                (screenW - imgW) / 2,
                (screenH - imgH) / 2,
                imgW,
                imgH
            );

            var srcRect = new Rectangle<int>(0, 0, _gameOverTextureData.Width, _gameOverTextureData.Height);

            _renderer.RenderTexture(_gameOverTextureId, srcRect, dstRect);
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
            if (deltaX < 32 && deltaY < 32)
            {
                _player.GameOver();
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
    
    public void Dispose()
{
    _soundManager?.Dispose();
}

}