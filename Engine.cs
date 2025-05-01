using System.Text.Json;
using Silk.NET.Maths;
using SixLabors.ImageSharp;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using SixLabors.Fonts;

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

    private int _lives = 3;
    private List<HeartObject> _collectedHearts = new();
    private bool _isGameOver = false;

    private int _heartTextureId;
    private TextureData _heartTextureData;

    private int _gameOverTextureId;
    private TextureData _gameOverTextureData;

    private int _winTextureId; 
    private TextureData _winTextureData; 

    private List<CoinObject> _coins = new();
    private int _collectedCoins = 0;
    private int _coinTextureId;
    private TextureData _coinTextureData;
    private DateTimeOffset _lastCoinUpdate = DateTimeOffset.Now;
    private bool _isGameWin = false;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void SetupWorld()
    {
        _tileIdMap.Clear();
        _loadedTileSets.Clear();

        _player = new(SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);

        _heartTextureId = _renderer.LoadTexture(Path.Combine("Assets", "heart.png"), out _heartTextureData);

        _gameOverTextureId = _renderer.LoadTexture(Path.Combine("Assets", "GameOverText.png"), out _gameOverTextureData);

        _winTextureId = _renderer.LoadTexture(Path.Combine("Assets", "WinScreen.png"), out _winTextureData);

        _coinTextureId = _renderer.LoadTexture(Path.Combine("Assets", "coin.png"), out _coinTextureData);

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

        AddHeartsToField(5);
        AddCoinsToField(10);
    }


    private void AddHeartsToField(int count)
    {
        SpriteSheet heartSprite = SpriteSheet.Load(_renderer, "Heart.json", "Assets");
        Random random = new();

        for (int i = 0; i < count; i++)
        {
            int x = random.Next(0, _currentLevel.Width.Value * _currentLevel.TileWidth.Value);
            int y = random.Next(0, _currentLevel.Height.Value * _currentLevel.TileHeight.Value);

            HeartObject heart = new(heartSprite, (x, y));
            _gameObjects.Add(heart.Id, heart);
        }
    }

    private void AddCoinsToField(int count)
    {
        SpriteSheet coinSprite = SpriteSheet.Load(_renderer, "Coin.json", "Assets");
        Random random = new();

        for (int i = 0; i < count; i++)
        {
            int x = random.Next(0, _currentLevel.Width.Value * _currentLevel.TileWidth.Value);
            int y = random.Next(0, _currentLevel.Height.Value * _currentLevel.TileHeight.Value);

            CoinObject coin = new(coinSprite, x, y);
            _coins.Add(coin);
        }
    }

    private void UpdateCoinPositions()
    {
        var currentTime = DateTimeOffset.Now;
        if ((currentTime - _lastCoinUpdate).TotalSeconds >= 15)
        {
            foreach (var coin in _coins)
            {
                coin.RandomizePosition(_currentLevel.Width.Value * _currentLevel.TileWidth.Value,
                                       _currentLevel.Height.Value * _currentLevel.TileHeight.Value);
            }
            _lastCoinUpdate = currentTime;
        }
    }

    private void CheckCoinCollection()
    {
        var toRemove = new List<CoinObject>();

        foreach (var coin in _coins)
        {
            var dx = coin.Position.X - _player!.Position.X;
            var dy = coin.Position.Y - _player.Position.Y;

            if (Math.Abs(dx) < 20 && Math.Abs(dy) < 20)
            {
                _collectedCoins++;
                toRemove.Add(coin);
            }
        }

        foreach (var coin in toRemove)
        {
            _coins.Remove(coin);
        }

        if (_collectedCoins >= 3)
        {
            _isGameOver = false; 
            _isGameWin = true;   
        }
    }

    private void RenderCoins()
    {
        foreach (var coin in _coins)
        {
            coin.Render(_renderer);
        }
    }

    private void RenderCollectedCoins()
    {
        int count = Math.Min(_collectedCoins, 3);
        var srcRect = new Rectangle<int>(0, 0, _coinTextureData.Width, _coinTextureData.Height);

        for (int i = 0; i < count; i++)
        {
            var dstRect = new Rectangle<int>(20 + i * 40, 20, 32, 32);
            _renderer.RenderTextureScreen(_coinTextureId, srcRect, dstRect);
        }
    }

    public void ProcessFrame()
    {
        if (_isGameWin)
        {
            if (_input.IsRPressed()) 
            {
                RestartGame();
            }
            return;
        }

        if (_isGameOver) return;

        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;

        _player?.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame);

        CheckHeartCollection();
        CheckBombExplosions();
        CheckCoinCollection();
        UpdateCoinPositions();
    }

    private void CheckHeartCollection()
    {
        var toRemove = new List<int>();

        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is HeartObject heart)
            {
                var dx = heart.Position.X - _player!.Position.X;
                var dy = heart.Position.Y - _player.Position.Y;

                if (Math.Abs(dx) < 20 && Math.Abs(dy) < 20)
                {
                    _lives++;
                    toRemove.Add(heart.Id);
                }
            }
        }

        foreach (var id in toRemove)
        {
            _gameObjects.Remove(id);
        }
    }

    private void CheckBombExplosions()
    {
        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is TemporaryGameObject bomb && bomb.IsExpired)
            {
                var dx = bomb.Position.X - _player!.Position.X;
                var dy = bomb.Position.Y - _player.Position.Y;

                if (Math.Abs(dx) < 20 && Math.Abs(dy) < 20)
                {
                    _lives--;
                    if (_lives <= 0)
                    {
                        _player.SpriteSheet.ActivateAnimation("Dying");
                        Task.Delay(1000).ContinueWith(_ => _isGameOver = true);
                    }
                }
            }
        }
    }

    public void RenderFrame()
    {
        if (_isGameWin)
        {
            RenderWinScreen();
            return;
        }

        if (_isGameOver)
        {
            RenderGameOverScreen();
            return;
        }

        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        var playerPosition = _player!.Position;
        _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);

        RenderTerrain();
        RenderAllObjects();
        RenderCoins();
        RenderCollectedCoins();
        RenderLives();

        _renderer.PresentFrame();
    }

    private void RenderLives()
    {
        var windowSize = _renderer.WindowSize;
        int windowWidth = windowSize.Width;
        int y = 20;

        for (int i = 0; i < _lives; i++)
        {
            int x = windowWidth - (i + 1) * 40;
            var srcRect = new Rectangle<int>(0, 0, _heartTextureData.Width, _heartTextureData.Height);
            var dstRect = new Rectangle<int>(x, y, _heartTextureData.Width, _heartTextureData.Height);
            _renderer.RenderTextureScreen(_heartTextureId, srcRect, dstRect);
        }
    }

    private void RenderGameOverScreen()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        var windowSize = _renderer.WindowSize;
        int windowWidth = windowSize.Width;
        int windowHeight = windowSize.Height;

        int scaledWidth = windowWidth;
        int scaledHeight = (int)(scaledWidth / 1.5);
        int yPos = 20;

        var srcRect = new Rectangle<int>(0, 0, _gameOverTextureData.Width, _gameOverTextureData.Height);
        var dstRect = new Rectangle<int>(0, yPos, scaledWidth, scaledHeight);

        _renderer.RenderTextureScreen(_gameOverTextureId, srcRect, dstRect);
        _renderer.PresentFrame();
    }

    private void RenderWinScreen()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        var windowSize = _renderer.WindowSize;
        int windowWidth = windowSize.Width;
        int windowHeight = windowSize.Height;

        int scaledWidth = windowWidth;
        int scaledHeight = (int)(scaledWidth / 1.5);
        int yPos = (windowHeight - scaledHeight) / 2;

        var srcRect = new Rectangle<int>(0, 0, _winTextureData.Width, _winTextureData.Height);
        var dstRect = new Rectangle<int>(0, yPos, scaledWidth, scaledHeight);

        _renderer.RenderTextureScreen(_winTextureId, srcRect, dstRect);
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

        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is TemporaryGameObject bomb && !bomb.IsExpired)
            {
                var dx = bomb.Position.X - worldCoords.X;
                var dy = bomb.Position.Y - worldCoords.Y;
                var distanceSquared = dx * dx + dy * dy;

                if (distanceSquared <= 400)
                {
                    return;
                }

                if ((dx == 0 && Math.Abs(dy) <= 20) || (dy == 0 && Math.Abs(dx) <= 20))
                {
                    return;
                }
            }
        }

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject newBomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(newBomb.Id, newBomb);
    }

    private void RestartGame()
    {
        _lives = 3;
        _collectedHearts.Clear();
        _gameObjects.Clear();
        _coins.Clear();
        _collectedCoins = 0; 
        _isGameOver = false;
        _isGameWin = false;

        SetupWorld();
    }
}
