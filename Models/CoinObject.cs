using TheAdventure.Models;

public class CoinObject : RenderableGameObject
{
    public CoinObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y)) { }

    public void RandomizePosition(int mapWidth, int mapHeight)
    {
        var random = new Random();
        Position = (random.Next(0, mapWidth), random.Next(0, mapHeight));
    }
}
