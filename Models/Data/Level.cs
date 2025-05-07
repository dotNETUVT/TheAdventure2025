using System.Text.Json.Serialization;

namespace TheAdventure.Models.Data;

public class Level
{
    [JsonPropertyName("compressionlevel")]
    public int? CompressionLevel { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("infinite")]
    public bool? Infinite { get; set; }

    [JsonPropertyName("layers")]
    public List<Layer> Layers { get; set; } = new();

    [JsonPropertyName("nextlayerid")]
    public int? NextLayerId { get; set; }

    [JsonPropertyName("nextobjectid")]
    public int? NextObjectId { get; set; }

    [JsonPropertyName("orientation")]
    public string Orientation { get; set; } = "";

    [JsonPropertyName("renderorder")]
    public string RenderOrder { get; set; } = "";

    [JsonPropertyName("tiledversion")]
    public string TiledVersion { get; set; } = "";

    [JsonPropertyName("tileheight")]
    public int? TileHeight { get; set; }

    [JsonPropertyName("tilesets")]
    public List<TileSetReference> TileSets { get; set; } = new();

    [JsonPropertyName("tilewidth")]
    public int? TileWidth { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("width")]
    public int? Width { get; set; }
}

