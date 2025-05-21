using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;

namespace TheAdventure;

public class Engine
{
    private readonly GameRenderer _renderer;
    private readonly Input        _input;
    private readonly ScriptEngine _scriptEngine;

    private readonly Dictionary<int, GameObject> _gameObjects    = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile>       _tileIdMap      = new();
    private Level        _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    public Engine(GameRenderer renderer, Input input, ScriptEngine scriptEngine)
    {
        _renderer     = renderer;
        _input        = input;
        _scriptEngine = scriptEngine;

        _input.OnMouseClick += (_, p) => AddBomb(p.x, p.y);
    }
    
    private int _texHeartFull;
    private int _texHeartEmpty;
    
    public void SetupWorld()
    {
        _player = new PlayerObject(
            SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);

        _texHeartFull  = _renderer.LoadTexture(Path.Combine("Assets","UI","heart_full.png"),  out _);
        _texHeartEmpty = _renderer.LoadTexture(Path.Combine("Assets","UI","heart_empty.png"), out _);

        var levelJson  = File.ReadAllText(Path.Combine("Assets","terrain.tmj"));
        _currentLevel  = JsonSerializer.Deserialize<Level>(levelJson)
                         ?? throw new Exception("Failed to load level");

        foreach (var ts in _currentLevel.TileSets
                         .Select(t => File.ReadAllText(Path.Combine("Assets", t.Source)))
                         .Select(json => JsonSerializer.Deserialize<TileSet>(json)))
        {
            if (ts == null) throw new Exception("Failed to load tile set");

            foreach (var tile in ts.Tiles)
            {
                tile.TextureId = _renderer.LoadTexture(Path.Combine("Assets", tile.Image), out _);
                _tileIdMap.Add(tile.Id!.Value, tile);
            }
            _loadedTileSets.Add(ts.Name, ts);
        }

        if (_currentLevel.Width       is null ||
            _currentLevel.Height      is null ||
            _currentLevel.TileWidth   is null ||
            _currentLevel.TileHeight  is null)
            throw new Exception("Invalid level dimensions");

        int worldW = _currentLevel.Width.Value  * _currentLevel.TileWidth.Value;
        int worldH = _currentLevel.Height.Value * _currentLevel.TileHeight.Value;
        _renderer.SetWorldBounds(new Rectangle<int>(0,0, worldW, worldH));

        _scriptEngine.LoadAll(Path.Combine("Assets","Scripts"));
    }
    
    public bool ProcessFrame()
    {
        var now       = DateTimeOffset.Now;
        var elapsedMs = (now - _lastUpdate).TotalMilliseconds;
        _lastUpdate   = now;

        if (_player == null) return false;

        double up    = _input.IsUpPressed()    ? 1 : 0;
        double down  = _input.IsDownPressed()  ? 1 : 0;
        double left  = _input.IsLeftPressed()  ? 1 : 0;
        double right = _input.IsRightPressed() ? 1 : 0;

        bool attackKey = _input.IsKeyAPressed() && (up+down+left+right <= 1);
        bool dashKey   = _input.IsDashPressed();
        bool spawnBomb = _input.IsKeyBPressed();

        _player.UpdatePosition(up, down, left, right, dashKey, 48, 48, elapsedMs);
        if (attackKey) _player.Attack();

        foreach (var o in _gameObjects.Values)
            if (o is BombGameObject b) b.Update(elapsedMs);

        _scriptEngine.ExecuteAll(this);

        if (spawnBomb)
            AddBomb(_player.Position.X, _player.Position.Y, translateCoordinates:false);

        if (_player.State.State == PlayerObject.PlayerState.GameOver &&
            _player.SpriteSheet.AnimationFinished)
        {
            Thread.Sleep(500);
            return true;
        }

        return false;
    }


    public void RenderFrame()
    {
        _renderer.SetDrawColor(0,0,0,255);
        _renderer.ClearScreen();

        _renderer.CameraLookAt(_player!.Position.X, _player.Position.Y);

        RenderTerrain();
        RenderAllObjects();
        RenderHud();

        _renderer.PresentFrame();
    }

    private void RenderHud()
    {
        const int gap = 6, w = 24, h = 24;

        for (int i = 0; i < _player!.MaxHealth; i++)
        {
            int tex = i < _player.Health ? _texHeartFull : _texHeartEmpty;
            var dst = new Rectangle<int>(8 + i*(w+gap), 8, w, h);
            (int sw,int sh) = _renderer.GetTextureSize(tex);
            _renderer.RenderTextureScreen(tex, new Rectangle<int>(0,0,sw,sh), dst);
        }
    }

    private void RenderAllObjects()
    {
        var expired = new List<int>();

        foreach (var obj in GetRenderables())
        {
            obj.Render(_renderer);

            if (_player != null && obj is BombGameObject bomb && !bomb.IsExpired)
            {
                var dx = Math.Abs(_player.Position.X - bomb.Position.X);
                var dy = Math.Abs(_player.Position.Y - bomb.Position.Y);

                if (dx < 32 && dy < 32)
                {
                    bool swinging = _player.State.State == PlayerObject.PlayerState.Attack &&
                                    !_player.SpriteSheet.AnimationFinished;

                    if (swinging)
                    {
                        var dir = _player.State.Direction switch
                        {
                            PlayerObject.PlayerStateDirection.Up    => new Vector2D<double>(0,-1),
                            PlayerObject.PlayerStateDirection.Down  => new Vector2D<double>(0, 1),
                            PlayerObject.PlayerStateDirection.Left  => new Vector2D<double>(-1,0),
                            PlayerObject.PlayerStateDirection.Right => new Vector2D<double>(1, 0),
                            _ => default
                        };
                        bomb.Velocity = dir * 300;
                    }
                    else if (!_player.IsInvincible)
                    {
                        _player.TakeDamage(1);
                    }
                }
            }

            if (obj is TemporaryGameObject { IsExpired: true } tmp)
                expired.Add(tmp.Id);
        }

        foreach (int id in expired)
            _gameObjects.Remove(id);

        _player?.Render(_renderer);
    }

    private void RenderTerrain()
    {
        foreach (var layer in _currentLevel.Layers)
        {
            for (int i = 0; i < _currentLevel.Width; i++)
            for (int j = 0; j < _currentLevel.Height; j++)
            {
                int dataIdx = j * layer.Width!.Value + i;
                int tileIdx = layer.Data[dataIdx].GetValueOrDefault() - 1;
                if (tileIdx < 0) continue;

                Tile t  = _tileIdMap[tileIdx];
                int tw  = t.ImageWidth  ?? 0;
                int th  = t.ImageHeight ?? 0;

                var src = new Rectangle<int>(0,0,tw,th);
                var dst = new Rectangle<int>(i*tw, j*th, tw, th);
                _renderer.RenderTexture(t.TextureId, src, dst);
            }
        }
    }
    
    public IEnumerable<RenderableGameObject> GetRenderables() =>
        _gameObjects.Values.OfType<RenderableGameObject>();

    public (int X,int Y) GetPlayerPosition() => _player!.Position;

    public void AddBomb(int x, int y, bool translateCoordinates = true)
    {
        var world = translateCoordinates
            ? _renderer.ToWorldCoordinates(x, y)
            : new Vector2D<int>(x, y);

        var sheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        sheet.ActivateAnimation("Explode");

        var bomb  = new BombGameObject(sheet, 2.1, (world.X, world.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }
}
