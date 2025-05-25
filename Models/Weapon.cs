
namespace TheAdventure.Models;

public class Weapon : Item 
{
    public string WeaponType { get; set; } = string.Empty;
    public int Damage { get; set; }
    public float AttackSpeed { get; set; }
    public float Range { get; set; }
    public string SpriteEquippedPath { get; set; } = string.Empty;
    public int EquippedTextureId { get; set; } = -1;
    public int EquippedSpriteWidth { get; set; } // ADDED (or ensure it's there)
    public int EquippedSpriteHeight { get; set; } // ADDED (or ensure it's there)
}