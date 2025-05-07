using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAdventure.Models
{
    public class HealthPackObject : RenderableGameObject
    {
        public int HealAmount { get; }
        public bool IsCollected { get; private set; }

        public HealthPackObject(SpriteSheet spriteSheet, (int X, int Y) position, int healAmount = 25)
            : base(spriteSheet, position)
        {
            HealAmount = healAmount;
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
                player.Heal(HealAmount);
                IsCollected = true;
                Console.WriteLine($"Player healed by {HealAmount}. Health: {player.CurrentHealth}/{player.MaxHealth}");
            }
        }
    }
}
