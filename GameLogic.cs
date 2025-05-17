using Silk.NET.Maths;

namespace TheAdventure;

public class GameLogic
{
    private readonly List<GameObject> _gameObjects = new();
    private RenderableGameObject _player = null!;
    private readonly List<RenderableGameObject> _coins = new();
    private int _frameCount;
    private int _score = 0;

    public void InitializeGame(GameRenderer gameRenderer)
    {
        var textureId = gameRenderer.LoadTexture("image.png", out var textureInfo);

        _player = new RenderableGameObject(textureId,
            new Rectangle<int>(0, 0, textureInfo.Width / 10, textureInfo.Height / 10),
            new Rectangle<int>(0, 0, textureInfo.Width / 10, textureInfo.Height / 10),
            textureInfo)
        {
            X = 0,
            Y = 0,
            Width = textureInfo.Width / 10,
            Height = textureInfo.Height / 10
        };
        _gameObjects.Add(_player);

        AddCoin(150, 0, textureId, textureInfo);
        AddCoin(250, 0, textureId, textureInfo);
        AddCoin(350, 0, textureId, textureInfo);
    }

    private void AddCoin(int x, int y, int textureId, GameRenderer.TextureInfo textureInfo)
    {
        var coinSize = textureInfo.Width / 10;
        var coin = new RenderableGameObject(textureId,
            new Rectangle<int>(coinSize, 0, coinSize, coinSize),
            new Rectangle<int>(x, y, coinSize, coinSize),
            textureInfo)
        {
            X = x,
            Y = y,
            Width = coinSize,
            Height = coinSize
        };

        _coins.Add(coin);
        _gameObjects.Add(coin);
    }

    public void ProcessFrame()
    {
        _player.X += 1;
        _player.TextureDestination = new Rectangle<int>(_player.X, _player.Y, _player.Width, _player.Height);

        var i = _frameCount % 10;
        var j = _frameCount / 10;
        var cellWidth = _player.TextureInformation.Width / 10;
        var cellHeight = _player.TextureInformation.Height / 10;
        var x = i * cellWidth;
        var y = j * cellHeight;
        _player.TextureSource = new Rectangle<int>(x, y, cellWidth, cellHeight);

        ++_frameCount;
        if (_frameCount == 100) _frameCount = 0;

        for (int iCoin = _coins.Count - 1; iCoin >= 0; iCoin--)
        {
            var coin = _coins[iCoin];
            if (_player.IsCollidingWith(coin))
            {
                _coins.RemoveAt(iCoin);
                _gameObjects.Remove(coin);
                _score++;
                Console.WriteLine($"Coin colectat! Scor: {_score}");
            }
        }
    }

    public IEnumerable<RenderableGameObject> GetRenderables()
    {
        foreach (var gameObject in _gameObjects)
        {
            if (gameObject is RenderableGameObject renderableGameObject)
            {
                yield return renderableGameObject;
            }
        }
    }
}
