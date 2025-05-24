// In TheAdventure2025/Systems/ItemDatabase.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TheAdventure.Models;

namespace TheAdventure.Systems;

public static class ItemDatabase
{
    public static List<Weapon> AllWeapons { get; private set; } = new List<Weapon>();
    // Add List<Clothing> later

    // Now requires GameRenderer to load textures
    public static void LoadAllItems(GameRenderer renderer, string assetsBasePath = "Assets")
    {
        LoadWeapons(renderer, Path.Combine(assetsBasePath, "Data", "weapons.json"));
        // Call LoadClothes(renderer, ...) here later
    }

    private static void LoadWeapons(GameRenderer renderer, string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                string jsonData = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }; 
                AllWeapons = JsonSerializer.Deserialize<List<Weapon>>(jsonData, options) ?? new List<Weapon>();

                Console.WriteLine($"Loaded {AllWeapons.Count} weapons:");
                foreach (var weapon in AllWeapons)
                {
                    // Load textures and store their IDs
                    if (!string.IsNullOrEmpty(weapon.SpriteIconPath))
                    {
                        weapon.IconTextureId = renderer.LoadTexture(weapon.SpriteIconPath, out _);
                    }
                    if (!string.IsNullOrEmpty(weapon.SpriteEquippedPath))
                    {
                        weapon.EquippedTextureId = renderer.LoadTexture(weapon.SpriteEquippedPath, out _);
                    }
                    if (!string.IsNullOrEmpty(weapon.SpriteEquippedPath))
{
    weapon.EquippedTextureId = renderer.LoadTexture(weapon.SpriteEquippedPath, out TextureData equippedTextureData);
    weapon.EquippedSpriteWidth = equippedTextureData.Width;   // Store width
    weapon.EquippedSpriteHeight = equippedTextureData.Height;  // Store height
}
                    Console.WriteLine($"- {weapon.Name} (IconID: {weapon.IconTextureId}, EquipID: {weapon.EquippedTextureId})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading weapon data from {filePath}: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"Error: Weapon data file not found at {filePath}");
        }
    }
}