using System.Text.Json;
using Silk.NET.Maths;
using Silk.NET.SDL;
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

    private Rectangle<int> _worldBounds;
    private Level _currentLevel = new();
    private PlayerObject? _player;
    private PlayerObject2? _player2;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;
    private bool _isRPressed = false;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }
     
    public void SetupWorld()
    {
        var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        var level = JsonSerializer.Deserialize<Level>(levelContent);

        var worldBounds = new Rectangle<int>(0, 0, level.Width.Value * level.TileWidth.Value, level.Height.Value * level.TileHeight.Value);
        var centerX = 0;
        var centerY = 0;

        _player = new PlayerObject(_renderer, worldBounds);
        _player.X = centerX + 480;
        _player.Y = centerY + 200;

        _player2 = new PlayerObject2(_renderer, worldBounds);
        _player2.X = centerX + 50;
        _player2.Y = centerY + 200;
        _gameObjects.Add(_player2.Id, _player2);

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

        _worldBounds = new Rectangle<int>(0, 0,
        level.Width.Value * level.TileWidth.Value,
        level.Height.Value * level.TileHeight.Value);

        centerX = _worldBounds.Size.X / 2;
        centerY = _worldBounds.Size.Y / 2;

        var houseSprite = new SpriteSheet(_renderer, Path.Combine("Assets", "House.png"), 1, 1, 170, 150, (0, 0));
        var house = new RenderableGameObject(houseSprite, (centerX - 90, centerY - 190)); 

        _gameObjects.Add(house.Id, house);
    }


    private bool _isSpacePressed = false;
    private bool _isFPressed = false;

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;
        int ms = (int)msSinceLastFrame;

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;

        _player?.UpdatePosition(up, down, left, right, ms);

        double upW = _input.IsWPressed() ? 1 : 0;
        double downS = _input.IsSPressed() ? 1 : 0;
        double leftA = _input.IsAPressed() ? 1 : 0;
        double rightD = _input.IsDPressed() ? 1 : 0;

        _player2?.UpdatePosition(upW, downS, leftA, rightD, ms);

        var toRemove = new List<int>();

        foreach (var obj in _gameObjects.Values)
        {
            if (obj is Blueberry blueberry && _player2 != null)
            {
                var blueberryRect = blueberry.GetBounds();
                var player2Rect = _player2.GetBounds();

                var blueberryMin = blueberryRect.Origin;
                var blueberryMax = blueberryRect.Origin + blueberryRect.Size;

                var player2Min = player2Rect.Origin;
                var player2Max = player2Rect.Origin + player2Rect.Size;

                if (blueberryMin.X < player2Max.X &&
                    blueberryMax.X > player2Min.X &&
                    blueberryMin.Y < player2Max.Y &&
                    blueberryMax.Y > player2Min.Y)
                {
                    if (_player2.CanTakeDamage())
                    {
                        _player2.TakeDamage();
                        toRemove.Add(blueberry.Id);
                    }
                }
            }

            if (obj is Stick stick && _player != null)
            {
                var stickRect = stick.GetBounds();
                var playerRect = _player.GetBounds();

                var stickMin = stickRect.Origin;
                var stickMax = stickRect.Origin + stickRect.Size;

                var playerMin = playerRect.Origin;
                var playerMax = playerRect.Origin + playerRect.Size;

                if (stickMin.X < playerMax.X &&
                    stickMax.X > playerMin.X &&
                    stickMin.Y < playerMax.Y &&
                    stickMax.Y > playerMin.Y)
                {
                    if (_player.CanTakeDamage())
                    {
                        _player.TakeDamage();
                        toRemove.Add(stick.Id); 
                    }
                }
            }

            if (obj is TemporaryGameObject tempObj)
            {
                tempObj.Update(ms);
            }

        }

        foreach (var id in toRemove)
        {
            _gameObjects.Remove(id);
        }


        if (_input.IsSpacePressed() && !_isSpacePressed) 
        {
            ThrowBlueberry();
            _isSpacePressed = true;
        }
        else if (!_input.IsSpacePressed()) 
        {
            _isSpacePressed = false; 
        }

        if (_input.IsFPressed() && !_isFPressed)
        {
            ThrowStick();
            _isFPressed = true;
        }
        else if (!_input.IsFPressed())
        {
            _isFPressed = false;
        }

        if (_input.IsRPressed() && !_isRPressed)
        {
            ResetGame();
            _isRPressed = true;
        }
        else if (!_input.IsRPressed())
        {
            _isRPressed = false;
        }



        foreach (var obj in _gameObjects.Values)
        {
            if (obj is TemporaryGameObject tempObj)
            {
                tempObj.Update(ms);
            }
        }

    }

    private static bool RectanglesOverlap(Rectangle<int> a, Rectangle<int> b)
    {
        return a.Origin.X < b.Origin.X + b.Size.X &&
               a.Origin.X + a.Size.X > b.Origin.X &&
               a.Origin.Y < b.Origin.Y + b.Size.Y &&
               a.Origin.Y + a.Size.Y > b.Origin.Y;
    }

    private void ResetGame()
    {
        _gameObjects.Clear();
        _loadedTileSets.Clear();
        _tileIdMap.Clear();
        _player = null;
        _player2 = null;

        SetupWorld();
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

        if (_player.IsGameOver())
        {
            _player2.Render(_renderer);
            _player.Render(_renderer);
        }
        else if (_player2.IsGameOver())
        {
            _player.Render(_renderer);
            _player2.Render(_renderer);
        }
        else
        {
            _player.Render(_renderer);
            _player2.Render(_renderer);
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

    private void ThrowBlueberry()
    {
        if (_player == null) return;

        var direction = _player.GetDirectionVector();
        var startX = _player.X + 24; 
        var startY = _player.Y + 24;

        var blueberry = new Blueberry(_renderer, (startX, startY), direction);
        _gameObjects.Add(blueberry.Id, blueberry);
    }

    private void ThrowStick()
    {
        if (_player2 == null) return;

        var direction = _player2.GetDirectionVector();
        var startX = _player2.X + 24;
        var startY = _player2.Y + 24;

        var stick = new Stick(_renderer, (startX, startY), direction);
        _gameObjects.Add(stick.Id, stick);
    }

}