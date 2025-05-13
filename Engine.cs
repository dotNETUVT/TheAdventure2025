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

    private bool _bombExploding = false;
    private DateTimeOffset _bombEndTime;

    private SpriteSheet? _heartsSpriteSheet;

    private List<RenderableGameObject> _heartIcons = new();

    private bool _shouldKillPlayerAfterExplosion = false;

    private int _appleTextureId;
    private List<RenderableGameObject> _apples = new();

    private SpriteSheet? _appleSpriteSheet;



    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void SetupWorld()
    {
        _player = new(_renderer);

        _heartsSpriteSheet = new SpriteSheet(_renderer, Path.Combine("Assets", "hearts.png"), 1, 3, 32, 32, (48, 16));
        RenderableGameObject hearts = new(_heartsSpriteSheet, (60, 30));


        _gameObjects.Add(hearts.Id, hearts);


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

        _appleSpriteSheet = new SpriteSheet(_renderer, Path.Combine("Assets", "apple.png"), 1, 1, 32, 38, (16, 16));

        SpawnApple();
    }

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;

        _player?.UpdatePosition(up, down, left, right, (int)msSinceLastFrame);


        if (_player != null)
        {
            var collected = new List<RenderableGameObject>();
            foreach (var apple in _apples)
            {
                var dx = _player.X - apple.Position.X;
                var dy = _player.Y - apple.Position.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < 40 && _player.Health == 1)
                {
                    _player.Health++;

                    if (_player.Health == 2)
                    {
                        _heartsSpriteSheet.Animations["Hearts"] = new SpriteSheet.Animation
                        {
                            StartFrame = (0, 0),
                            EndFrame = (0, 0),
                            DurationMs = 2000,
                            Loop = false
                        };
                    }
                    Console.WriteLine($"Viata jucatorului a crescut la {_player.Health}.");


                    _heartsSpriteSheet.ActivateAnimation("Hearts");

                    collected.Add(apple);
                }
            }

            foreach (var apple in collected)
            {
                _apples.Remove(apple);
                _gameObjects.Remove(apple.Id);

                SpawnApple();
            }
        }


        if (_bombExploding && DateTimeOffset.Now > _bombEndTime)
        {
            _bombExploding = false;

            if (_shouldKillPlayerAfterExplosion && _player != null)
            {
                _player.Die();
                _shouldKillPlayerAfterExplosion = false;
            }

        }
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        _renderer.CameraLookAt(_player!.X, _player!.Y);


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
            _gameObjects.Remove(id);
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

    public void TriggerBomb()
    {
        if (_player == null) return;

        var screenX = (int)(_player.X); 
        var screenY = (int)(_player.Y);

        AddBomb(screenX, screenY);  
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

    private void AddBomb(int screenX, int screenY)
    {
        var worldCoords = _renderer.ToWorldCoordinates(screenX, screenY);

        SpriteSheet spriteSheet = new(_renderer, Path.Combine("Assets", "BombExploding.png"), 1, 13, 32, 64, (16, 48));
        spriteSheet.Animations["Explode"] = new SpriteSheet.Animation
        {
            StartFrame = (0, 0),
            EndFrame = (0, 12),
            DurationMs = 2000,
            Loop = false
        };
        spriteSheet.ActivateAnimation("Explode");

        if (_player != null)
        {
            int dx = _player.X - worldCoords.X;
            int dy = _player.Y - worldCoords.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            const double bombDamageRadius = 50.0; 

            if (distance <= bombDamageRadius)
            {
                _player.Health--;
                Console.WriteLine($"Jucatorul a primit daune! Viata actuala: {_player.Health}");

                if (_player.Health <= 0)
                {
                    _shouldKillPlayerAfterExplosion = true;
                }


                if (_player.Health == 1)
                {
                    _heartsSpriteSheet.Animations["Hearts"] = new SpriteSheet.Animation
                    {
                        StartFrame = (0, 0),
                        EndFrame = (0, 1),
                        DurationMs = 2000,
                        Loop = false
                    };
                }
                else if (_player.Health == 0)
                {
                    _heartsSpriteSheet.Animations["Hearts"] = new SpriteSheet.Animation
                    {
                        StartFrame = (0, 1),
                        EndFrame = (0, 2),
                        DurationMs = 2000,
                        Loop = false
                    };
                }

                _heartsSpriteSheet.ActivateAnimation("Hearts");
            }
        }


        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);

        _bombExploding = true;
        _bombEndTime = DateTimeOffset.Now.AddSeconds(2.1);
    }


    private void SpawnApple()
    {
        if (_player == null || _appleSpriteSheet == null) return;

        var random = new Random();

        int camCenterX = _player.X;
        int camCenterY = _player.Y;

        int viewportWidth = _renderer.ViewportWidth;
        int viewportHeight = _renderer.ViewportHeight;

        int halfW = viewportWidth / 2;
        int halfH = viewportHeight / 2;

        int minX = camCenterX - halfW;
        int maxX = camCenterX + halfW;

        int minY = camCenterY - halfH;
        int maxY = camCenterY + halfH;

        minX = Math.Max(minX, 0);
        minY = Math.Max(minY, 0);
        maxX = Math.Min(maxX, _currentLevel.Width!.Value * _currentLevel.TileWidth!.Value);
        maxY = Math.Min(maxY, _currentLevel.Height!.Value * _currentLevel.TileHeight!.Value);

        int x = random.Next(minX, maxX);
        int y = random.Next(minY, maxY);

        var apple = new RenderableGameObject(_appleSpriteSheet, (x, y));
        _gameObjects.Add(apple.Id, apple);
        _apples.Add(apple);
    }
}