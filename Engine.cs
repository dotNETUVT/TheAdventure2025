using System.Reflection;
using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;
using TheAdventure.Audio;


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

    private bool _wasAPressedLastFrame = false;
    private bool _isAttacking = false;
    private DateTimeOffset _attackStartTime = DateTimeOffset.MinValue;
    private const double _attackDurationMs = 500; // durata atacului in ms
    private bool _sprintEnabled = false;
    private readonly Audio.AudioPlayer _swordSound = new("Assets/sword_slash.wav");

    private bool _isPaused = false;
    private bool _wasPPressedLastFrame = false;

    private bool _awaitingExitConfirmation = false;

    private bool IsGameFrozen() => _isPaused || _awaitingExitConfirmation;


    private bool _isGameOver = false;

    

    private readonly MusicPlayer _music = new();

    private int _heartTextureId;
    private const int _maxLives = 3;
    private int _currentLives = _maxLives;



    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void SetupWorld()
    {
        _player = new(SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);
        _gameObjects.Clear();
        _loadedTileSets.Clear();
        _tileIdMap.Clear();
        _music.PlayLoop("Assets/background_music.mp3");
        _heartTextureId = _renderer.LoadTexture(Path.Combine("Assets", "heart.png"), out _);
        _currentLives = _maxLives;

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
        // === ESC pressed → exit confirmation
        if (_awaitingExitConfirmation)
        {
            foreach (var obj in _gameObjects.Values)
                if (obj is TemporaryGameObject temp) temp.Pause();

            if (_input.IsKeyYPressed())
            {
                _music.Stop();
                Environment.Exit(0);
            }
            else if (_input.IsKeyNPressed())
            {
                _awaitingExitConfirmation = false;
                foreach (var obj in _gameObjects.Values)
                    if (obj is TemporaryGameObject temp) temp.Resume();
            }
            return;
        }

        // === R pressed during Game Over → reset game
        if (_isGameOver)
        {
            if (_input.IsKeyRPressed())
            {
                _gameObjects.Clear();
                _isPaused = false;
                _awaitingExitConfirmation = false;
                _isAttacking = false;
                _isGameOver = false;
                _player = null;
                _music.Stop();
                SetupWorld();
            }
            return;
        }

        // === ESC pressed → initiate exit
        if (_input.IsEscapePressed())
        {
            _awaitingExitConfirmation = true;
            return;
        }

        // === toggle pause with P
        bool pPressedNow = _input.IsKeyPPressed();
        bool pJustPressed = pPressedNow && !_wasPPressedLastFrame;
        _wasPPressedLastFrame = pPressedNow;

        if (pJustPressed)
        {
            _isPaused = !_isPaused;
            foreach (var obj in _gameObjects.Values)
            {
                if (obj is TemporaryGameObject temp)
                {
                    if (_isPaused) temp.Pause();
                    else temp.Resume();
                }
            }
        }

        // === if paused, halt logic
        if (_isPaused)
            return;

        // === Main game logic
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        if (_player == null) return;

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;

        bool wPressed = _input.IsWPressed();
        bool aPressedNow = _input.IsKeyAPressed();
        bool aJustPressed = aPressedNow && !_wasAPressedLastFrame;
        _wasAPressedLastFrame = aPressedNow;

        _sprintEnabled = wPressed;
        double speedMultiplier = _sprintEnabled ? 1.5 : 1.0;

        if (aJustPressed && !_isAttacking)
        {
            _player.Attack();
            _swordSound.Play();
            _attackStartTime = currentTime;
            _isAttacking = true;
        }

        if (_isAttacking && (currentTime - _attackStartTime).TotalMilliseconds >= _attackDurationMs)
        {
            _isAttacking = false;
        }

        if (!_isAttacking)
        {
            var timeWithSprint = msSinceLastFrame * speedMultiplier;
            int width = (_currentLevel.Width ?? 100) * (_currentLevel.TileWidth ?? 32);
            int height = (_currentLevel.Height ?? 100) * (_currentLevel.TileHeight ?? 32);

            _player.UpdatePosition(up, down, left, right, width, height, timeWithSprint);
        }

        if (_input.IsKeyBPressed())
        {
            AddBomb(_player.Position.X, _player.Position.Y, false);
        }

        // === Remove expired bombs and check collision
        var toRemove = _gameObjects
            .Where(pair => pair.Value is TemporaryGameObject t && t.IsExpired)
            .Select(pair => pair.Key)
            .ToList();

        foreach (var id in toRemove)
        {
            if (_gameObjects.TryGetValue(id, out var gameObject) &&
                gameObject is TemporaryGameObject tempObject)
            {
                var dx = Math.Abs(_player.Position.X - tempObject.Position.X);
                var dy = Math.Abs(_player.Position.Y - tempObject.Position.Y);
                if (dx < 32 && dy < 32)
                {
                    _currentLives--;

                    if (_currentLives <= 0)
                    {
                        _player.GameOver();
                        _isGameOver = true;
                    }
                }


            }
            _gameObjects.Remove(id);
        }

        // === Run scripts
        _scriptEngine.ExecuteAll(this);
    }




    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        var playerPosition = _player!.Position;
        _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);

        RenderTerrain();
        RenderAllObjects();

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
        if (IsGameFrozen())
        {
            _renderer.DrawText("GAME PAUSED", 250, 200);
        }

        if (_awaitingExitConfirmation)
        {
            _renderer.DrawText("Ești sigur că vrei să ieși? [Y / N]", 180, 250);
        }

        if (_isGameOver)
        {
            _renderer.DrawText("GAME OVER — Press R to restart", 180, 250);
        }

       // _renderer.DrawText($"Lives: {_player.Lives}", 20, 20);

        
        for (int i = 0; i < _currentLives; i++)
        {
            var dst = new Rectangle<int>(20 + i * 40, 20, 32, 32); // decalaj orizontal
            var src = new Rectangle<int>(0, 0, 32, 32); // întreaga inimioară
            _renderer.RenderTexture(_heartTextureId, src, dst);
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

    public void AddBomb(int X, int Y, bool translateCoordinates = true)
    {
        var worldCoords = translateCoordinates ? _renderer.ToWorldCoordinates(X, Y) : new Vector2D<int>(X, Y);

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }


}