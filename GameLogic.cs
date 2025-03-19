using Silk.NET.Maths;
using TheAdventure.Models;

namespace TheAdventure;

public class GameLogic
{
    private readonly Dictionary<int, GameObject> _gameObjects = new();
    
    private int _bombIds = 100;

    public void InitializeGame()
    {
    }

    public void ProcessFrame()
    {
    }

    public void RenderAllObjects(int timeSinceLastFrame, GameRenderer renderer)
    {
        List<int> itemsToRemove = new List<int>();
        foreach (var gameObject in GetRenderables())
        {
            if (gameObject.Update(timeSinceLastFrame))
            {
                gameObject.Render(renderer);
            }
            else
            {
                itemsToRemove.Add(gameObject.Id);
            }
        }

        foreach (var item in itemsToRemove)
        {
            _gameObjects.Remove(item);
        }
    }

    public void AddBomb(int x, int y)
    {
        AnimatedGameObject bomb = new AnimatedGameObject("BombExploding.png", 2, _bombIds, 13, 13, 1, x, y);
        _gameObjects.Add(bomb.Id, bomb);
        ++_bombIds;
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
}