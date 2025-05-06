namespace TheAdventure.Models;

public class SpawnObject : TemporaryGameObject
{
    public SpawnObject(GameRenderer renderer, (int X, int Y) position)
        : base(
            new SpriteSheet(renderer, Path.Combine("Assets", "Gravity-Sheet.png"), 4, 4, 96, 80, (48, 40)),
            ttl: 0.6, 
            position)
    {
        SpriteSheet.Animations["Spawn"] = new SpriteSheet.Animation
        {
            StartFrame = (2, 0),
            EndFrame = (2, 4),
            DurationMs = 600,
            Loop = false
        };

        SpriteSheet.ActivateAnimation("Spawn");
    }
}
