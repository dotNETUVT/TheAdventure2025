using System.Text.Json.Serialization;

namespace TheAdventure.Models.Data;

public class TileSet
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("tilecount")]
    public int? TileCount { get; set; }

    [JsonPropertyName("tileheight")]
    public int? TileHeight { get; set; }

    [JsonPropertyName("tiles")]
    public List<Tile> Tiles { get; set; } = new(); 

    [JsonPropertyName("tilewidth")]
    public int? TileWidth { get; set; }
}

public class TileSetReference
{
    [JsonPropertyName("firstgid")]
    public int? FirstGID { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = "";
}

