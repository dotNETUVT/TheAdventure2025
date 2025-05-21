namespace TheAdventure.Models;

public enum PlayerBuffType
{
    SpeedBoost,      // Increases player movement speed
    DamageBoost,     // Increases damage/attack reach
    HealthRestore,   // Restores health if we implement health system
    ExtraBomb,       // Allows placing one additional bomb
    BombRadius       // Increases bomb explosion radius
}

public class PlayerBuff
{
    public PlayerBuffType Type { get; }
    public float Value { get; }
    public string Description { get; }
    public DateTimeOffset ApplicationTime { get; }

    // For permanent buffs (like extra bomb)
    public bool IsPermanent { get; }

    // For temporary buffs
    public double DurationSeconds { get; }
    public bool IsExpired => !IsPermanent &&
                              (DateTimeOffset.Now - ApplicationTime).TotalSeconds > DurationSeconds;

    public PlayerBuff(PlayerBuffType type, float value, string description, bool isPermanent = true, double durationSeconds = 0)
    {
        Type = type;
        Value = value;
        Description = description;
        ApplicationTime = DateTimeOffset.Now;
        IsPermanent = isPermanent;
        DurationSeconds = durationSeconds;
    }

    public static PlayerBuff CreateBuff(PlayerBuffType type)
    {
        return type switch
        {
            PlayerBuffType.SpeedBoost => new PlayerBuff(
                PlayerBuffType.SpeedBoost,
                1.2f,
                "Speed increased by 20%"),

            PlayerBuffType.DamageBoost => new PlayerBuff(
                PlayerBuffType.DamageBoost,
                1.25f,
                "Attack reach increased by 25%"),

            PlayerBuffType.HealthRestore => new PlayerBuff(
                PlayerBuffType.HealthRestore,
                1f,
                "Health restored"),

            PlayerBuffType.ExtraBomb => new PlayerBuff(
                PlayerBuffType.ExtraBomb,
                1f,
                "Can place an additional bomb"),

            PlayerBuffType.BombRadius => new PlayerBuff(
                PlayerBuffType.BombRadius,
                1.5f,
                "Bomb explosion radius increased by 50%"),

            _ => new PlayerBuff(
                PlayerBuffType.SpeedBoost,
                1.2f,
                "Speed increased by 20%")
        };
    }
}
