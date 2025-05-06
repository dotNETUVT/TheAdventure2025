namespace TheAdventure.Models.Data;

using TheAdventure.Models;
using TheAdventure.Models.Data;

public class SlimeObject : EnemyObject
{
    public SlimeObject(GameRenderer renderer, PlayerObject player, int startX, int startY)
        : base(renderer, player, startX, startY)
    {
        SpriteSheet = new SpriteSheet(renderer,
            Path.Combine("Assets", "Slime_Sheet.png"), 
            rowCount: 3, columnCount: 4, frameWidth: 26, frameHeight: 18, frameCenter: (13, 9));

        SpriteSheet.Animations["Fly"] = new SpriteSheet.Animation
        {
            StartFrame = (0, 0),
            EndFrame = (0, 1),
            DurationMs = 400,
            Loop = true
        };

        SpriteSheet.Animations["Damage"] = new SpriteSheet.Animation
        {
            StartFrame = (1, 0),
            EndFrame = (1, 3),
            DurationMs = 300,
            Loop = false
        };

        SpriteSheet.Animations["Die"] = new SpriteSheet.Animation
        {
            StartFrame = (2, 0),
            EndFrame = (2, 4),
            DurationMs = 400,
            Loop = false
        };

        SpriteSheet.ActivateAnimation("Fly");
    }
}
