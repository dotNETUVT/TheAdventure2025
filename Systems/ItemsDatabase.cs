
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TheAdventure.Models;

namespace TheAdventure.Systems;

public static class ItemDatabase
{
    public static List<Weapon> AllWeapons { get; private set; } = new List<Weapon>();
   
    public static void LoadAllItems(GameRenderer renderer, string assetsBasePath = "Assets")
    {
        LoadWeapons(renderer, Path.Combine(assetsBasePath, "Data", "weapons.json"));
        
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
    weapon.EquippedSpriteWidth = equippedTextureData.Width;   
    weapon.EquippedSpriteHeight = equippedTextureData.Height;  
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