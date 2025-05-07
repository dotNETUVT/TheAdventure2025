using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAdventure.Models
{
    public class SpeedPackObject : RenderableGameObject
    {
        public int SpeedAmount { get; }
        public bool IsCollected { get; private set; }

        public SpeedPackObject(SpriteSheet spriteSheet, (int X, int Y) position, int SpeedAmount = 10)
            : base(spriteSheet, position)
        {
            this.SpeedAmount = SpeedAmount;
            IsCollected = false;
        }

        public bool CheckCollision((int X, int Y) playerPos)
        {
            const int pickupRadius = 25;

            var adjustedPlayerX = playerPos.X + 24;
            var adjustedPlayerY = playerPos.Y - 42 + 24;

            var dx = adjustedPlayerX - X;
            var dy = adjustedPlayerY - Y;
            var distanceSquared = dx * dx + dy * dy;

            return distanceSquared <= pickupRadius * pickupRadius;
        }


        public void Collect(PlayerObject player)
        {
            if (!IsCollected)
            {
                player.SpeedBoost(SpeedAmount);
                IsCollected = true;
                Console.WriteLine($"Player's speed increased by {SpeedAmount}.");
            }
        }
    }
}
