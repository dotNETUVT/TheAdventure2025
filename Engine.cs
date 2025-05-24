using System.Reflection;
using System.Text.Json;
using Silk.NET.Maths;
using Silk.NET.SDL;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;
using System.Diagnostics;


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
    private PlayerObject? _playerCat;

    private const string _soundPath = "Assets/boop.wav";
    private Process _audioProcess;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }
    public GameRenderer GetRenderer() => _renderer;

    public void SetupWorld()
    {
        var playerSprite = SpriteSheet.Load(_renderer, "Player.json", "Assets");
        var playerCatSprite = SpriteSheet.Load(_renderer, "Cat.json", "Assets");

        _player = new(playerSprite, 100, 100, KeyBindings.ArrowKeys);
        _playerCat = new(playerCatSprite, 200, 200, KeyBindings.WASDKeys);

        _audioProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "afplay",
                Arguments = $"\"{_soundPath}\"",
                RedirectStandardOutput = false,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };



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
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        if (_player == null || _playerCat == null)
        {
            return;
        }

        double p1Up = _input.IsUpPressed() ? 1.0 : 0.0;
        double p1Down = _input.IsDownPressed() ? 1.0 : 0.0;
        double p1Left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double p1Right = _input.IsRightPressed() ? 1.0 : 0.0;
        bool p1IsAttacking = _input.IsKeySpacePressed() && (p1Up + p1Down + p1Left + p1Right <= 1);
        bool p1AddBomb = _input.IsKeyBPressed();

        double p2Up = _input.IsKeyWPressed() ? 1.0 : 0.0;
        double p2Down = _input.IsKeySPressed() ? 1.0 : 0.0;
        double p2Left = _input.IsKeyAPressed() ? 1.0 : 0.0;
        double p2Right = _input.IsKeyDPressed() ? 1.0 : 0.0;
        // bool p2IsAttacking = _input.IsKeyAPressed() && (up + down + left + right <= 1);
        // bool p2AddBomb = _input.IsKeyBPressed();


        _player.UpdatePosition(p1Up, p1Down, p1Left, p1Right, 48, 48, msSinceLastFrame);
        _playerCat.UpdatePosition(p2Up, p2Down, p2Left, p2Right, 48, 48, msSinceLastFrame);
        if (p1IsAttacking)
        {
            _player.Attack();
        }

        _scriptEngine.ExecuteAll(this);

        if (p1AddBomb)
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

            if (gameObject == null) continue;

            var tempGameObject = (TemporaryGameObject)gameObject!;


            if (tempGameObject.Type.Equals("bomb") && CheckPlayerCollision(_player!, tempGameObject))
                _player.GameOver();


            if (tempGameObject.Type.Equals("treat"))
            {
                TreatObject treat = (TreatObject)tempGameObject;
                if (treat.CheckCollision((_playerCat.Position.X, _playerCat.Position.Y)))
                {
                    _audioProcess.Start();

                    _gameObjects.Remove(treat.Id);
                    continue;

                }
            }
        }

        _player?.Render(_renderer);
        _playerCat?.Render(_renderer);

    }
    private bool CheckPlayerCollision(PlayerObject player, TemporaryGameObject obj)
    {
        var deltaX = Math.Abs(player.Position.X - obj.Position.X);
        var deltaY = Math.Abs(player.Position.Y - obj.Position.Y);
        if (deltaX < 32 && deltaY < 32)
        {
            return true;
        }
        return false;
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

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y), "bomb");
        _gameObjects.Add(bomb.Id, bomb);
    }
    public void AddTreat(int X, int Y, bool translateCoordinates = true)
    {
        var worldCoords = translateCoordinates ? _renderer.ToWorldCoordinates(X, Y) : new Vector2D<int>(X, Y);

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "Treat.json", "Assets");
        spriteSheet.ActivateAnimation("Idle");

        TreatObject treat = new(spriteSheet, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(treat.Id, treat);
    }

}