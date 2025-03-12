using Silk.NET.Maths;

namespace TheAdventure;

public class GameLogic
{
    private readonly List<GameObject> _gameObjects = new();

    private int _frameCount;

    public void InitializeGame(GameRenderer gameRenderer)
    {
        var textureId = gameRenderer.LoadTexture("image.png", out var textureInfo);
        var sampleRenderableObject = new RenderableGameObject(textureId,
            new Rectangle<int>(0, 0, textureInfo.Width, textureInfo.Height),
            new Rectangle<int>(0, 0, textureInfo.Width, textureInfo.Height), textureInfo);
        _gameObjects.Add(sampleRenderableObject);
    }

    public void ProcessFrame()
    {
        var renderableObject = (RenderableGameObject)_gameObjects.First();
        var i = _frameCount % 10;
        var j = _frameCount / 10;
        var cellWidth = renderableObject.TextureInformation.Width / 10;
        var cellHeight = renderableObject.TextureInformation.Height / 10;
        var x = i * cellWidth;
        var y = j * cellHeight;
        Rectangle<int> srcDest = new Rectangle<int>(x, y, cellWidth, cellHeight);
        renderableObject.TextureSource = srcDest;
        renderableObject.TextureDestination = new Rectangle<int>(0, 0, cellWidth, cellHeight);
        ++_frameCount;
        if (_frameCount == 100)
        {
            _frameCount = 0;
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