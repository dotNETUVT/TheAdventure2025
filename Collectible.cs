using System;
using System.Numerics;

public class Collectible
{
    public Vector2 Position { get; private set; }
    public float Radius { get; private set; } = 0.2f;
    public bool Collected { get; private set; } = false;

    public Collectible(Vector2 position)
    {
        Position = position;
    }

    public void CheckCollision(Vector2 playerPosition, float playerRadius)
    {
        float distance = Vector2.Distance(Position, playerPosition);
        Console.WriteLine($"Distanta pana la moneda: {distance}, limita: {Radius + playerRadius}");

        if (distance < Radius + playerRadius && !Collected)
        {
            Collected = true;
            Console.WriteLine("Moneda colectata!");
        }
    }

    public void Draw(Action<Vector2, float> drawAction)
    {
        if (!Collected)
        {
            drawAction(Position, Radius);
        }
    }
}
