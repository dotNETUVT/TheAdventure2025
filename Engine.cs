using System.Reflection;
using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;
using ImGuiNET;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using Silk.NET.Input;

namespace TheAdventure;

public class Engine
{
    public GameRenderer Renderer => _renderer;
    public PlayerObject? Player => _player;
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private readonly ScriptEngine _scriptEngine = new();

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;
    private DateTime _lastConsoleLog = DateTime.Now;

  

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
        

    }

    public void SetupWorld()
    {
        _player = new(SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);


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

        if (_player == null)
        {
            return;
        }

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;
        bool isAttacking = _input.IsKeyAPressed() && (up + down + left + right <= 1);
        bool addBomb = _input.IsKeyBPressed();

        _player.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame);
        if (isAttacking)
        {
            _player.Attack();
        }
        
        _scriptEngine.ExecuteAll(this);

        if (addBomb)
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

        var scoreTex = CreateUITextTexture($"Score: {_player.Score}", System.Drawing.Color.White);
        var hpTex = CreateUITextTexture($"HP: {_player.Health}", System.Drawing.Color.Red);
        DrawUIText(scoreTex, 10, 40);
        DrawUIText(hpTex, 10, 10);
        var shieldStatus = _player.IsShielded ? "Shield Activated" : "Shield Deactivated";
        var shieldColor = _player.IsShielded ? System.Drawing.Color.Cyan : System.Drawing.Color.Black;
        var shieldTex = CreateUITextTexture(shieldStatus, shieldColor);
        DrawUIText(shieldTex, 10, 70);
        _renderer.PresentFrame();
    }

    public void RenderAllObjects()
    {
        var toRemove = new List<int>();
        // --- Check for potion collision before rendering ---
        if (_player != null)
        {
            foreach (var gameObject in GetRenderables())
            {
                if (gameObject is TemporaryGameObject tempGameObject &&
                    tempGameObject.SpriteSheet != null)
                {
                    // MagicPotion pickup
                    if (tempGameObject.SpriteSheet.FileName == "MagicPotion.png")
                    {
                        var deltaX = Math.Abs(_player.Position.X - tempGameObject.Position.X);
                        var deltaY = Math.Abs(_player.Position.Y - tempGameObject.Position.Y);
                        if (deltaX < 32 && deltaY < 32)
                        {
                            _player.Heal(40);
                            Console.WriteLine("Ai luat viata!");
                            toRemove.Add(tempGameObject.Id);
                            continue;
                        }
                    }
                    // Shield pickup
                    else if (tempGameObject.SpriteSheet.FileName == "Shield.png")
                    {
                        var deltaX = Math.Abs(_player.Position.X - tempGameObject.Position.X);
                        var deltaY = Math.Abs(_player.Position.Y - tempGameObject.Position.Y);
                        if (deltaX < 32 && deltaY < 32)
                        {
                            _player.IsShielded = true;
                            Console.WriteLine("Ai luat shield!");
                            toRemove.Add(tempGameObject.Id);
                            continue;
                        }
                    }
                }
            }
        }
        // --- Normal rendering and bomb expiration logic ---
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
            // Only bombs deal damage on expiration
            if (tempGameObject.SpriteSheet != null && tempGameObject.SpriteSheet.FileName == "BombExploding.png")
            {
                var deltaX = Math.Abs(_player.Position.X - tempGameObject.Position.X);
                var deltaY = Math.Abs(_player.Position.Y - tempGameObject.Position.Y);
                if (deltaX < 32 && deltaY < 32)
                {
                    if (_player.IsShielded)
                    {
                        Console.WriteLine("Ai fost aparat de shield!");
                        _player.IsShielded = false;
                    }
                    else
                    {
                        Console.WriteLine("Te-a lovit bomba!");
                        _player.TakeDamage(40);
                    }
                }
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

    public void AddPotion(int X, int Y, bool translateCoordinates = true)
    {
        var worldCoords = translateCoordinates ? _renderer.ToWorldCoordinates(X, Y) : new Vector2D<int>(X, Y);

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "MagicPotion.json", "Assets");
        spriteSheet.ActivateAnimation("Idle");

        TemporaryGameObject potion = new(spriteSheet, 3.0, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(potion.Id, potion);
    }

    public int AddShield(int X, int Y, bool translateCoordinates = true)
    {
        var worldCoords = translateCoordinates ? _renderer.ToWorldCoordinates(X, Y) : new Vector2D<int>(X, Y);
        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "Shield.json", "Assets");
        spriteSheet.ActivateAnimation("Idle");
        TemporaryGameObject shield = new(spriteSheet, 5.0, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(shield.Id, shield);
        return shield.Id;
    }

    public void RemoveShield(int id)
    {
        _gameObjects.Remove(id, out _);
    }

    public void AddGameObject(GameObject obj)
    {
        if (!_gameObjects.ContainsKey(obj.Id))
            _gameObjects.Add(obj.Id, obj);
    }

    public void RemoveGameObject(int id)
    {
        _gameObjects.Remove(id, out _);
    }
    
    private (int textureId, int width, int height) CreateUITextTexture(string text, System.Drawing.Color color)
{
    using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(200, 50);
    image.Mutate(ctx =>
    {
        ctx.Clear(new SixLabors.ImageSharp.PixelFormats.Rgba32(0, 0, 0, 0));
        var fontCollection = new SixLabors.Fonts.FontCollection();
        var fontFamily = fontCollection.Add("Assets/Fonts/arial.ttf");
        var font = fontFamily.CreateFont(24, SixLabors.Fonts.FontStyle.Bold);
        ctx.DrawText(text, font, new SixLabors.ImageSharp.PixelFormats.Rgba32(color.R, color.G, color.B, color.A), new SixLabors.ImageSharp.PointF(0, 0));
    });

    using (var ms = new System.IO.MemoryStream())
    {
        image.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
        ms.Position = 0;
        var textureId = _renderer.LoadTexture(ms, out var texData);
        return (textureId, texData.Width, texData.Height);
    }
}

// Desenează textul pe UI (HUD), resetând camera ca să fie mereu în colțul ecranului
private void DrawUIText((int textureId, int width, int height) tex, int x, int y)
{
    var originalCameraPos = _renderer.ToWorldCoordinates(0, 0);
    _renderer.CameraLookAt(0, 0);

    _renderer.RenderTexture(
        tex.textureId,
        new Silk.NET.Maths.Rectangle<int>(0, 0, tex.width, tex.height),
        new Silk.NET.Maths.Rectangle<int>(x, y, tex.width, tex.height)
    );

    _renderer.CameraLookAt(originalCameraPos.X, originalCameraPos.Y);
}
}