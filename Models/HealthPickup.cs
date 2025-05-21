using Silk.NET.SDL;

namespace TheAdventure.Models;

public class HealthPickup : RenderableGameObject
{
    public int HealAmount { get; private set; }

    public HealthPickup(SpriteSheet spriteSheet, (int X, int Y) position, int healAmount)
        : base(spriteSheet, position)
    {
        HealAmount = healAmount;
        spriteSheet.ActivateAnimation("Idle");
    }
}