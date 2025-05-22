using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Extensions;
using TheAdventure.Scripting;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace TheAdventure;

public class Engine
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private readonly ScriptEngine _scriptEngine;

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    private const int MaxBombs = 5;
    private int _score = 0;

    private bool _isGameOver = false;
    private bool _isGameWon = false;
    private const int BombDamage = 25;
    private const int WinningScore = 5000;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;
        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);

        _scriptEngine = new ScriptEngine();
        string scriptFolderPath = Path.Combine("Assets", "Scripts");
        if (Directory.Exists(scriptFolderPath))
        {
            _scriptEngine.LoadAll(scriptFolderPath);
        }
        else
        {
            Console.WriteLine($"Warning: Script folder not found at {Path.GetFullPath(scriptFolderPath)}");
        }
    }

    public int GetScore() => _score;

    public void SetupWorld()
    {
        _player = new(_renderer);
        _isGameOver = false;
        _isGameWon = false;
        _score = 0;

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
                if (tile.Id.HasValue)
                {
                    tile.TextureId = _renderer.LoadTexture(Path.Combine("Assets", tile.Image), out _);
                    _tileIdMap.Add(tile.Id.Value, tile);
                }
            }
            _loadedTileSets.Add(tileSet.Name, tileSet);
        }

        if (!level.Width.HasValue || !level.Height.HasValue) throw new Exception("Invalid level dimensions");
        if (!level.TileWidth.HasValue || !level.TileHeight.HasValue) throw new Exception("Invalid tile dimensions");

        _renderer.SetWorldBounds(new Rectangle<int>(0, 0, level.Width.Value * level.TileWidth.Value, level.Height.Value * level.TileHeight.Value));
        _currentLevel = level;

        _renderer.PlayBackgroundMusic();
    }

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        if (_isGameOver || _isGameWon)
        {
            _renderer.StopBackgroundMusic();
            return;
        }

        _scriptEngine.ExecuteAll(this);

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;

        _player?.UpdatePosition(up, down, left, right, (int)msSinceLastFrame, GetActiveBombs().Where(b => !b.IsExpired));

        if (_player != null && _player.IsAlive)
        {
            _score += (int)(msSinceLastFrame / 7.0);

            if (_score >= WinningScore)
            {
                _score = WinningScore;
                _isGameWon = true;
                _renderer.StopBackgroundMusic();
            }
        }
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        if (_player != null && !_isGameOver && !_isGameWon)
        {
            _renderer.CameraLookAt(_player.X, _player.Y);
        }

        RenderTerrain();
        RenderAllObjects();

        _renderer.RenderScoreOnScreen(_score, 10, 10);

        if (_player != null)
        {
            if (_isGameWon)
            {
                _renderer.RenderYouWinScreen();
            }
            else if (_isGameOver)
            {
                _renderer.RenderYouLostScreen();
            }
            else
            {
                int screenWidth = _renderer.GetWindowWidth();
                int healthDisplayWidth = _renderer.CalculateHealthDisplayWidth(_player.CurrentHealth);
                int healthPaddingRight = 10;
                int healthDisplayX = screenWidth - healthDisplayWidth - healthPaddingRight;
                int healthDisplayY = 10;

                _renderer.RenderHealthOnScreen(_player.CurrentHealth, healthDisplayX, healthDisplayY);
            }
        }

        _renderer.PresentFrame();
    }

    public void RenderAllObjects()
    {
        var toRemove = new List<int>();

        foreach (var gameObject in GetRenderables())
        {
            if (!_isGameOver && !_isGameWon || gameObject is TemporaryGameObject)
            {
                gameObject.Render(_renderer);
            }

            if (gameObject is TemporaryGameObject tempBomb && tempBomb.IsExpired)
            {
                toRemove.Add(tempBomb.Id);

                if (!_isGameOver && !_isGameWon && _player != null && _player.IsAlive)
                {
                    _renderer.PlayExplosionSound();

                    var bombSpriteSheet = tempBomb.SpriteSheet;
                    var explosionRect = new Rectangle<int>(
                        tempBomb.Position.X - bombSpriteSheet.FrameCenter.OffsetX,
                        tempBomb.Position.Y - bombSpriteSheet.FrameCenter.OffsetY,
                        bombSpriteSheet.FrameWidth,
                        bombSpriteSheet.FrameHeight
                    );

                    if (explosionRect.IntersectsWith(_player.GetBoundingBox()))
                    {
                        _player.TakeDamage(BombDamage);
                        if (!_player.IsAlive)
                        {
                            _isGameOver = true;
                            _renderer.StopBackgroundMusic();
                        }
                    }
                }
            }
        }

        foreach (var id in toRemove)
        {
            _gameObjects.Remove(id);
        }

        if (_player != null)
        {
            if (!_isGameOver && !_isGameWon)
            {
                _player.Render(_renderer);
            }
        }
    }

    public void RenderTerrain()
    {
        if (_isGameOver || _isGameWon) return;

        foreach (var currentLayer in _currentLevel.Layers)
        {
            for (int i = 0; i < (_currentLevel.Width ?? 0); ++i)
            {
                for (int j = 0; j < (_currentLevel.Height ?? 0); ++j)
                {
                    if (currentLayer.Width == null || currentLayer.Data == null || currentLayer.Data.Count == 0) continue;
                    int dataIndex = j * currentLayer.Width.Value + i;
                    if (dataIndex < 0 || dataIndex >= currentLayer.Data.Count) continue;
                    var tileGid = currentLayer.Data[dataIndex];
                    if (!tileGid.HasValue || tileGid.Value == 0) continue;
                    var currentTileId = tileGid.Value - 1;
                    if (_tileIdMap.TryGetValue(currentTileId, out var currentTile))
                    {
                        var tileWidth = currentTile.ImageWidth ?? 0;
                        var tileHeight = currentTile.ImageHeight ?? 0;
                        if (tileWidth == 0 || tileHeight == 0) continue;
                        var sourceRect = new Rectangle<int>(0, 0, tileWidth, tileHeight);
                        var destRect = new Rectangle<int>(i * (_currentLevel.TileWidth ?? tileWidth), j * (_currentLevel.TileHeight ?? tileHeight), tileWidth, tileHeight);
                        _renderer.RenderTexture(currentTile.TextureId, sourceRect, destRect);
                    }
                }
            }
        }
    }

    public IEnumerable<RenderableGameObject> GetRenderables() => _gameObjects.Values.OfType<RenderableGameObject>();
    public IEnumerable<TemporaryGameObject> GetActiveBombs() => _gameObjects.Values.OfType<TemporaryGameObject>();

    public void AddBomb(int screenX, int screenY)
    {
        if (_isGameOver || _isGameWon || GetActiveBombs().Count(b => !b.IsExpired) >= MaxBombs)
            return;

        var worldCoords = _renderer.ToWorldCoordinates(screenX, screenY);
        var spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        var bomb = new TemporaryGameObject(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }

    public (int X, int Y) GetPlayerPosition() => _player != null ? (_player.X, _player.Y) : (0, 0);

    public void CleanUp()
    {
        _renderer.Dispose();
    }
}