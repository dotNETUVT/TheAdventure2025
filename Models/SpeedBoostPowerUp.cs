using Silk.NET.Maths;
using TheAdventure.Models;

namespace TheAdventure.Models
{
    public class SpeedBoostPowerUp : PowerUp
    {
        private const float SPEED_MULTIPLIER = 2.0f;
        private const double DURATION_MS = 3000;

        public SpeedBoostPowerUp(SpriteSheet spriteSheet, (int X, int Y) position)
            : base(spriteSheet, position)
        {
        }

        public override void ApplyEffect(PlayerObject player)
        {
            player.ApplySpeedBoost(SPEED_MULTIPLIER, DURATION_MS);
        }
    }
}