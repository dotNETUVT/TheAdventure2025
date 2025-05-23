using Silk.NET.Maths;
using System;
using System.Text.Json;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;

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
    private DateTimeOffset _startTime;
    private bool _dead;

    private int _worldWidth;
    private int _worldHeight;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) =>
        {
            if (_dead)
            {
                if (_renderer.IsPlayButtonClicked(coords.x, coords.y))
                    Restart();
                return;
            }

            AddBomb(coords.x, coords.y);
        };
    }

    public void SetupWorld()
    {
        _player = new(SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);
        _gameObjects.Clear();
        _tileIdMap.Clear();
        _loadedTileSets.Clear();

        var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        _currentLevel = JsonSerializer.Deserialize<Level>(levelContent) ?? throw new Exception("Failed to load level");

        foreach (var tileSetRef in _currentLevel.TileSets)
        {
            var tileSetContent = File.ReadAllText(Path.Combine("Assets", tileSetRef.Source));
            var tileSet = JsonSerializer.Deserialize<TileSet>(tileSetContent) ?? throw new Exception("Failed to load tileset");
            foreach (var tile in tileSet.Tiles)
            {
                tile.TextureId = _renderer.LoadTexture(Path.Combine("Assets", tile.Image), out _);
                _tileIdMap[tile.Id!.Value] = tile;
            }
            _loadedTileSets[tileSet.Name] = tileSet;
        }

        _worldWidth = _currentLevel.Width!.Value * _currentLevel.TileWidth!.Value;
        _worldHeight = _currentLevel.Height!.Value * _currentLevel.TileHeight!.Value;

        _renderer.SetWorldBounds(new Rectangle<int>(0, 0, _worldWidth, _worldHeight));

        _scriptEngine.LoadAll(Path.Combine("Assets", "Scripts"));

        _dead = false;
        _renderer.ResetTimer();
        _startTime = _lastUpdate = DateTimeOffset.Now;
    }

    public void ProcessFrame()
    {
        if (_dead || _player == null) return;

        var now = DateTimeOffset.Now;
        var dt = (now - _lastUpdate).TotalMilliseconds;
        _lastUpdate = now;

        double up = _input.IsUpPressed() ? 1 : 0;
        double down = _input.IsDownPressed() ? 1 : 0;
        double left = _input.IsLeftPressed() ? 1 : 0;
        double right = _input.IsRightPressed() ? 1 : 0;
        bool atk = _input.IsKeyAPressed() && (up + down + left + right <= 1);
        bool bomb = _input.IsKeyBPressed();

        _player.UpdatePosition(up, down, left, right, 48, 48, dt);

        var ppos = _player.Position;
        ppos.X = Math.Clamp(ppos.X, 0, _worldWidth - 48);
        ppos.Y = Math.Clamp(ppos.Y, 0, _worldHeight - 48);
        _player.Position = ppos;

        if (atk) _player.Attack();

        _scriptEngine.ExecuteAll(this);

        if (bomb) AddBomb(_player.Position.X, _player.Position.Y, false);
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        if (_player != null)
        {
            var p = _player.Position;
            _renderer.CameraLookAt(p.X, p.Y);
        }

        RenderTerrain();
        RenderAllObjects();

        _renderer.PresentFrame();
    }

    private void RenderAllObjects()
    {
        var remove = new List<int>();
        foreach (var obj in GetRenderables())
        {
            obj.Render(_renderer);
            if (obj is TemporaryGameObject { IsExpired: true } t) remove.Add(t.Id);
        }

        foreach (var id in remove)
        {
            _gameObjects.Remove(id, out var go);
            if (_player == null || go == null) continue;
            var t = (TemporaryGameObject)go;
            if (Math.Abs(_player.Position.X - t.Position.X) < 32 &&
                Math.Abs(_player.Position.Y - t.Position.Y) < 32)
            {
                _player.GameOver();
                _renderer.StopTimer();
                _dead = true;
            }
        }

        _player?.Render(_renderer);
    }

    private void RenderTerrain()
    {
        foreach (var layer in _currentLevel.Layers)
        {
            for (int i = 0; i < _currentLevel.Width; i++)
                for (int j = 0; j < _currentLevel.Height; j++)
                {
                    int idx = (int)(j * layer.Width + i);
                    int gid = (int)(layer.Data[idx] - 1);
                    if (gid < 0) continue;

                    var tile = _tileIdMap[gid];
                    int tw = tile.ImageWidth!.Value;
                    int th = tile.ImageHeight!.Value;

                    var src = new Rectangle<int>(0, 0, tw, th);
                    var dst = new Rectangle<int>(i * tw, j * th, tw, th);
                    _renderer.RenderTexture(tile.TextureId, src, dst);
                }
        }
    }

    private IEnumerable<RenderableGameObject> GetRenderables()
    {
        foreach (var g in _gameObjects.Values)
            if (g is RenderableGameObject r) yield return r;
    }

    private void Restart() => SetupWorld();

    public (int X, int Y) GetPlayerPosition()
    {
        return (_player?.Position.X ?? 0, _player?.Position.Y ?? 0);
    }

    public void AddBomb(int X, int Y, bool translateCoordinates = true)
    {
        var world = translateCoordinates ? _renderer.ToWorldCoordinates(X, Y) : new Vector2D<int>(X, Y);
        world = new Vector2D<int>(
            Math.Clamp(world.X, 0, _worldWidth - 32),
            Math.Clamp(world.Y, 0, _worldHeight - 32));

        var sheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        sheet.ActivateAnimation("Explode");
        var bomb = new TemporaryGameObject(sheet, 2.1, (world.X, world.Y));
        _gameObjects[bomb.Id] = bomb;
    }
}