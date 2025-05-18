using System;

public class GameTimeManager
{
    private const float DayDurationSeconds = 60f;
    private float elapsedTime = 0f;

    public float TimeOfDayNormalized => (elapsedTime % DayDurationSeconds) / DayDurationSeconds;

    public void Update(float deltaTime)
    {
        elapsedTime += deltaTime;
    }

    public float GetBrightnessFactor()
    {
        float t = TimeOfDayNormalized;
        return (float)(0.5 + 0.5 * Math.Sin(2 * Math.PI * t));
    }
}
