namespace TheAdventure.Models;

public class ItemObject : RenderableGameObject
{
    private bool _isCollected;
    private const int COLLECTION_RADIUS = 32;

    public ItemObject(SpriteSheet spriteSheet, (int X, int Y) position) : base(spriteSheet, position)
    {
        _isCollected = false;
    }

    public bool CanBeCollected((int X, int Y) playerPosition)
    {
        if (_isCollected) return false;

        int deltaX = Position.X - playerPosition.X;
        int deltaY = Position.Y - playerPosition.Y;
        double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        
        return distance <= COLLECTION_RADIUS;
    }

    public void Collect()
    {
        _isCollected = true;
    }

    public bool IsCollected => _isCollected;

    public override void Render(GameRenderer renderer)
    {
        if (!_isCollected)
        {
            base.Render(renderer);
        }
    }
}