using Silk.NET.Maths;

namespace TheAdventure;

public class GameLogic
{
    private readonly List<GameObject> _gameObjects = new();

    public void InitializeGame(GameRenderer gameRenderer)
    {
        var textureId = gameRenderer.LoadTexture("image.png", out var textureInfo);
        var sampleRenderableObject = new RenderableGameObject(textureId,
            new Rectangle<int>(0, 0, textureInfo.Width, textureInfo.Height),
            new Rectangle<int>(new Vector2D<int>(0, 0), new Vector2D<int>(100, 100)), textureInfo);
        _gameObjects.Add(sampleRenderableObject);
    }

    public void ProcessFrame()
    {
        var renderableObject = (RenderableGameObject)_gameObjects.First();

        renderableObject.TextureSource = new Rectangle<int>(
            0, 0,
            renderableObject.TextureInformation.Width,
            renderableObject.TextureInformation.Height
        );

        var size = new Vector2D<int>(100, 100);
        renderableObject.TextureDestination = new Rectangle<int>(
            renderableObject.TextureDestination.Origin,
            size
        );
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

    public void MovePlayer((bool up, bool down, bool left, bool right) movementInput)
    {
        if (_gameObjects.Count == 0) return;

        var renderableObject = (RenderableGameObject)_gameObjects.First();
        var dest = renderableObject.TextureDestination;

        int deltaX = 0, deltaY = 0;
        int speed = 5;

        if (movementInput.up) deltaY -= speed;
        if (movementInput.down) deltaY += speed;
        if (movementInput.left) deltaX -= speed;
        if (movementInput.right) deltaX += speed;

        var newDest = new Rectangle<int>(
            new Vector2D<int>(dest.Origin.X + deltaX, dest.Origin.Y + deltaY),
            dest.Size
        );

        renderableObject.TextureDestination = newDest;
    }

    public void MovePlayerToPosition(int mouseX, int mouseY)
    {
        if (_gameObjects.Count == 0) return;

        var renderableObject = (RenderableGameObject)_gameObjects.First();
        var size = renderableObject.TextureDestination.Size;

        var newX = mouseX - size.X / 2;
        var newY = mouseY - size.Y / 2;

        renderableObject.TextureDestination = new Rectangle<int>(
            new Vector2D<int>(newX, newY),
            size
        );
    }
}
