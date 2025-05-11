using System.Text.Json.Serialization;

namespace TheAdventure.Models.Data;

public class HighScore
{
    [JsonPropertyName("score")]
    public int Score { get; set; } = 0;
}