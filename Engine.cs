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
    private DateTimeOffset _lastFireballTime = DateTimeOffset.MinValue;
    private readonly TimeSpan _fireballCooldown = TimeSpan.FromSeconds(0.25);

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;
        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void SetupWorld()
    {
        _player = new(_renderer);

        var levelJson = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        _currentLevel = JsonSerializer.Deserialize<Level>(levelJson) ?? throw new Exception("Failed to load level");

        foreach (var tilesetRef in _currentLevel.TileSets)
        {
            var tilesetJson = File.ReadAllText(Path.Combine("Assets", tilesetRef.Source));
            var tileSet = JsonSerializer.Deserialize<TileSet>(tilesetJson) ?? throw new Exception("Failed to load tileset");

            foreach (var tile in tileSet.Tiles)
            {
                tile.TextureId = _renderer.LoadTexture(Path.Combine("Assets", tile.Image), out _);
                _tileIdMap[tile.Id!.Value] = tile;
            }

            _loadedTileSets[tileSet.Name] = tileSet;
        }

        if (_currentLevel.Width is null || _currentLevel.Height is null ||
            _currentLevel.TileWidth is null || _currentLevel.TileHeight is null)
            throw new Exception("Invalid level or tile dimensions");

        _renderer.SetWorldBounds(new Rectangle<int>(
            0, 0,
            _currentLevel.Width.Value * _currentLevel.TileWidth.Value,
            _currentLevel.Height.Value * _currentLevel.TileHeight.Value
        ));

        SpawnEnemy(300, 200);
        SpawnEnemy(400, 200);
    }

    public void ProcessFrame()
    {
        var now = DateTimeOffset.Now;
        var deltaMs = (now - _lastUpdate).TotalMilliseconds;
        _lastUpdate = now;

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;
        double space = _input.IsSpacePressed() ? 1.0 : 0.0;

        if (!_player!.IsDead)
            _player.UpdatePosition(up, down, left, right, (int)deltaMs);

        if (space == 1.0)
            AddFireball();

        foreach (var obj in _gameObjects.Values)
        {
            switch (obj)
            {
                case Fireball fireball:
                    fireball.Update((int)deltaMs);
                    break;
                case Enemy enemy:
                    enemy.Update((int)deltaMs, (_player.X, _player.Y));
                    break;
            }
        }

        CheckPlayerEnemyCollisions();
        HandleCollisions();
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();
        _renderer.CameraLookAt(_player!.X, _player.Y);

        RenderTerrain();
        RenderAllObjects();
        _renderer.PresentFrame();
    }

    public void RenderTerrain()
    {
        foreach (var layer in _currentLevel.Layers)
        {
            int width = layer.Width ?? 0;
            int height = layer.Height ?? 0;

            for (int i = 0; i < _currentLevel.Width; ++i)
            {
                for (int j = 0; j < _currentLevel.Height; ++j)
                {
                    int index = j * width + i;
                    int tileId = (layer.Data[index] ?? 1) - 1;

                    if (!_tileIdMap.TryGetValue(tileId, out var tile))
                        continue;

                    int tileWidth = tile.ImageWidth ?? 0;
                    int tileHeight = tile.ImageHeight ?? 0;

                    var src = new Rectangle<int>(0, 0, tileWidth, tileHeight);
                    var dst = new Rectangle<int>(i * tileWidth, j * tileHeight, tileWidth, tileHeight);
                    _renderer.RenderTexture(tile.TextureId, src, dst);
                }
            }
        }
    }

    private void RenderAllObjects()
    {
        var toRemove = new List<int>();

        foreach (var obj in GetRenderables())
        {
            obj.Render(_renderer);

            if (obj is Fireball f && f.IsExpired) toRemove.Add(f.Id);
            else if (obj is TemporaryGameObject t && t.IsExpired) toRemove.Add(t.Id);
        }

        foreach (var id in toRemove)
            _gameObjects.Remove(id);

        _player?.Render(_renderer);
    }

    public IEnumerable<RenderableGameObject> GetRenderables()
    {
        return _gameObjects.Values.OfType<RenderableGameObject>();
    }

    private void AddFireball()
    {
        if (DateTimeOffset.Now - _lastFireballTime < _fireballCooldown) return;

        _lastFireballTime = DateTimeOffset.Now;

        var dir = _player!.CurrentDirection switch
        {
            Direction.Up => new Vector2D<double>(0, -1),
            Direction.Down => new Vector2D<double>(0, 1),
            Direction.Left => new Vector2D<double>(-1, 0),
            Direction.Right => new Vector2D<double>(1, 0),
            _ => new Vector2D<double>(1, 0)
        };

        var sheet = new SpriteSheet(_renderer, Path.Combine("Assets", "Fireball.png"), 1, 5, 64, 32, (32, 16));
        sheet.Animations["Fireball"] = new SpriteSheet.Animation
        {
            StartFrame = new SpriteSheet.Position { Row = 0, Col = 0 },
            EndFrame = new SpriteSheet.Position { Row = 0, Col = 4 },
            DurationMs = 500,
            Loop = true
        };  
        sheet.ActivateAnimation("Fireball");

        var pos = GetFireballStartPosition(_player);
        var fireball = new Fireball(sheet, 2.0, pos, dir, 300.0);
        _gameObjects.Add(fireball.Id, fireball);
    }

    private (int X, int Y) GetFireballStartPosition(PlayerObject player)
    {
        return player.CurrentDirection switch
        {
            Direction.Up => (player.X + 20, player.Y + 20),
            Direction.Down => (player.X + 45, player.Y - 20),
            Direction.Left => (player.X, player.Y + 20),
            Direction.Right => (player.X + 20, player.Y - 5),
            _ => (player.X, player.Y)
        };
    }

    private void AddBomb(int screenX, int screenY)
    {
        var worldCoords = _renderer.ToWorldCoordinates(screenX, screenY);

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");

        if (spriteSheet.Animations.TryGetValue("Explode", out var anim))
        {
            anim.DurationMs = 700;
            spriteSheet.Animations["Explode"] = anim;
        }

        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 0.7, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }


    private void SpawnEnemy(int x, int y)
    {
        var spriteSheet = new SpriteSheet(
            _renderer,
            Path.Combine("Assets", "zombie.png"),
            1, 7,
            16, 16,
            (8, 8)
        );

        var enemy = new Enemy(spriteSheet, (x, y));
        _gameObjects.Add(enemy.Id, enemy);
    }

    private void CheckPlayerEnemyCollisions()
    {
        foreach (var obj in _gameObjects.Values)
        {
            if (obj is Enemy enemy)
            {
                var dx = enemy.Position.X - _player!.X;
                var dy = enemy.Position.Y - _player.Y;

                if (dx * dx + dy * dy < 10 && !_player.IsDead)
                    _player.Die();
            }
        }
    }

    private void HandleCollisions()
    {
        var toRemoveFireballs = new List<int>();
        var toRemoveEnemies = new List<int>();

        var activeBombs = _gameObjects.Values
            .OfType<TemporaryGameObject>()
            .Where(b => b.IsExpired) 
            .ToList();


        foreach (var obj in _gameObjects.Values)
        {
            if (obj is Fireball fireball)
            {
                foreach (var enemyObj in _gameObjects.Values)
                {
                    if (enemyObj is Enemy e &&
                        AreRectsColliding(fireball.Position, 32, 32, e.Position, 48, 48))
                    {
                        toRemoveFireballs.Add(fireball.Id);
                        toRemoveEnemies.Add(e.Id);
                    }
                }
            }

            if (obj is Enemy enemy)
            {
                foreach (var bomb in activeBombs)
                {
                    if (AreRectsColliding(bomb.Position, 32, 32, enemy.Position, 48, 48)) 
                    {
                        toRemoveEnemies.Add(enemy.Id);
                    }
                }
            }
        }

        foreach (var id in toRemoveFireballs.Concat(toRemoveEnemies).Distinct())
        {
            _gameObjects.Remove(id);
        }
    }



    private bool AreRectsColliding((int X, int Y) posA, int widthA, int heightA,
                                   (int X, int Y) posB, int widthB, int heightB)
    {
        return
            posA.X < posB.X + widthB &&
            posA.X + widthA > posB.X &&
            posA.Y < posB.Y + heightB &&
            posA.Y + heightA > posB.Y;
    }
}
