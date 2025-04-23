using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : GameObject
{
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;

    public int Health { get; set; } = 100;  // health
    public int Score { get; set; } = 0;    // score
    public int BombsPlaced { get; set; } = 0; //bombs placed
    
    private Rectangle<int> _source = new(0, 0, 48, 48);
    private Rectangle<int> _target = new(0, 0, 48, 48);

    private readonly int _textureId;

    private const int Speed = 128; // pixels per second

    public PlayerObject(GameRenderer renderer)
    {
        _textureId = renderer.LoadTexture(Path.Combine("Assets", "player.png"), out _);
        if (_textureId < 0)
        {
            throw new Exception("Failed to load player texture");
        }

        UpdateTarget();
    }

    public void IncrementBombsPlaced()
    {
         BombsPlaced++;
    }

    public void IncrementHealth(int amount)
    {
        Health += amount;
        if (Health > 100) // Max health
        {
            Health = 100;
        }
         if (Health < 0) // Min health
        {
            Health = 0;
        }
    }

    public void IncrementScore(int amount)
    {
        Score += amount;
    }

    public void UpdatePosition(double up, double down, double left, double right, int time)
    {
        var pixelsToMove = Speed * (time / 1000.0);

        Y -= (int)(pixelsToMove * up);
        Y += (int)(pixelsToMove * down);
        X -= (int)(pixelsToMove * left);
        X += (int)(pixelsToMove * right);

        UpdateTarget();
    }

    public void Render(GameRenderer renderer)
    {
        renderer.RenderTexture(_textureId, _source, _target);
    }

    private void UpdateTarget()
    {
        _target = new(X + 24, Y - 42, 48, 48);
    }
}