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

    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    private int _heartFullId;
    private int _heartHalfId;
    private int _heartEmptyId;
    private GameState _state = GameState.Playing;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;
        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void SetupWorld()
    {
        _player = new(_renderer);

        var flowerSpriteSheet = new SpriteSheet(
            _renderer,
            Path.Combine("Assets", "flowers.png"),
            6, 12, 32, 32, (16, 16));

        var flower1 = new HealingFlower(flowerSpriteSheet, (300, 200));
        var flower2 = new DamagingFlower(flowerSpriteSheet, (400, 200));

        _gameObjects.Add(flower1.Id, flower1);
        _gameObjects.Add(flower2.Id, flower2);

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
                _tileIdMap.Add(tile.Id!.Value, tile);
            }
        }

        if (level.Width == null || level.Height == null) throw new Exception("Invalid level dimensions");
        if (level.TileWidth == null || level.TileHeight == null) throw new Exception("Invalid tile dimensions");

        _renderer.SetWorldBounds(new Rectangle<int>(0, 0, level.Width.Value * level.TileWidth.Value,
            level.Height.Value * level.TileHeight.Value));

        _currentLevel = level;

        _heartFullId = _renderer.LoadTexture(Path.Combine("Assets", "heart_full.png"), out _);
        _heartHalfId = _renderer.LoadTexture(Path.Combine("Assets", "heart_half.png"), out _);
        _heartEmptyId = _renderer.LoadTexture(Path.Combine("Assets", "heart_empty.png"), out _);
        

    }

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        if (_state != GameState.Playing || _player is null) return;

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;

        _player.UpdatePosition(up, down, left, right, (int)msSinceLastFrame);

        var toRemove = new List<int>();
        var toSpawn = new List<bool>();  

        foreach (var obj in GetRenderables())
        {
            if (obj is HealingFlower healing && Intersects(_player, healing))
            {
                _player.Heal(1);
                toRemove.Add(healing.Id);
                toSpawn.Add(true);
            }
            else if (obj is DamagingFlower damaging && Intersects(_player, damaging))
            {
                _player.TakeDamage(1);
                toRemove.Add(damaging.Id);
                toSpawn.Add(false);
            }
        }

        foreach (var id in toRemove)
        {
            _gameObjects.Remove(id);
        }
        
        foreach (var isHealing in toSpawn)
        {
            var rand = new Random();

            int x = rand.Next(100, _currentLevel.Width!.Value * _currentLevel.TileWidth!.Value - 100);
            int y = rand.Next(100, _currentLevel.Height!.Value * _currentLevel.TileHeight!.Value - 100);

            var spriteSheet = new SpriteSheet(
                _renderer,
                Path.Combine("Assets", "flowers.png"),
                6, 12, 32, 32, (16, 16));

            GameObject flower = isHealing
                ? new HealingFlower(spriteSheet, (x, y))
                : new DamagingFlower(spriteSheet, (x, y));

            _gameObjects.Add(flower.Id, flower);
        }

        if (!_player.IsAlive)
        {
            _state = GameState.GameOver;
        }
    }


    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        if (_state == GameState.GameOver)
        {
            _renderer.DrawTextCentered("GAME OVER", highlight: true);
            _renderer.PresentFrame();
            return;
        }

        _renderer.CameraLookAt(_player!.X, _player!.Y);

        RenderTerrain();
        RenderAllObjects();

        if (_player is not null)
        {
       
            int heartWidth = 36;
            int heartHeight = 32;
            int spacing = 2;

        
            int xStart = 10;
            int yStart = 10;

            int health = _player.Health;

            for (int i = 0; i < 3; i++)
            {
                int drawX = xStart + i * (heartWidth + spacing);
                int heartState = Math.Min(2, health); 

                int textureId = heartState switch
                {
                    2 => _heartFullId,
                    1 => _heartHalfId,
                    _ => _heartEmptyId
                };

                var dst = new Rectangle<int>(drawX, yStart, heartWidth, heartHeight);
                var src = new Rectangle<int>(0, 0, heartWidth, heartHeight);
                _renderer.RenderTextureNoCamera(textureId, src, dst);

                health -= heartState;
            }
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
                    if (dataIndex == null) continue;

                    var currentTileId = currentLayer.Data[dataIndex.Value] - 1;
                    if (currentTileId == null) continue;

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

    private bool Intersects(PlayerObject player, RenderableGameObject obj)
    {
        int px = player.X;
        int py = player.Y;
        int pw = 48;
        int ph = 48;

        int ox = obj.Position.X - 16;
        int oy = obj.Position.Y - 16;
        int ow = 32;
        int oh = 32;

        return px < ox + ow &&
               px + pw > ox &&
               py < oy + oh &&
               py + ph > oy;
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

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }
}
