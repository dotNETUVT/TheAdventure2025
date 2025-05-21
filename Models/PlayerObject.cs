using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : GameObject
{
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;

    private Rectangle<int> _source = new(0, 0, 48, 48);
    private Rectangle<int> _target = new(0, 0, 48, 48);

    private readonly int _textureId;

    private const int Speed = 128; // pixels per second

    private int _distanceMoved; // Added to track the total distance moved

    public PlayerObject(GameRenderer renderer)
    {
        _textureId = renderer.LoadTexture(Path.Combine("Assets", "player.png"), out _);
        if (_textureId < 0)
        {
            throw new Exception("Failed to load player texture");
        }

        UpdateTarget();
    }

    public int UpdatePosition(double up, double down, double left, double right, int time)
    {
        var pixelsToMove = Speed * (time / 1000.0);

        // Store the previous position
        int previousX = X;
        int previousY = Y;

        // Update the player's position
        Y -= (int)(pixelsToMove * up);
        Y += (int)(pixelsToMove * down);
        X -= (int)(pixelsToMove * left);
        X += (int)(pixelsToMove * right);

        // Update the target rectangle
        UpdateTarget();

        // Calculate the distance moved
        int distanceMoved = Math.Abs(X - previousX) + Math.Abs(Y - previousY);

        // Accumulate the total distance moved
        _distanceMoved += distanceMoved;

        // Return the distance moved
        return distanceMoved;
    }

    public int GetDistanceMoved()
    {
        return _distanceMoved; // Added method to return the total distance moved
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
