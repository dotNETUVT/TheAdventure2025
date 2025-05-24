// In TheAdventure2025/Models/Item.cs
namespace TheAdventure.Models;

public class Item 
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SpriteIconPath { get; set; } = string.Empty;
    public int IconTextureId { get; set; } = -1; // ADD THIS
}