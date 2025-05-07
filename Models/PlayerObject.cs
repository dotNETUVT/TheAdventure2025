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
    
    public int Health { get; private set; } = 6; // max 6
    public int MaxHealth => 6;
    public bool IsAlive => Health > 0;

    public void Heal(int amount)
    {
        Health = Math.Min(MaxHealth, Health + amount);
    }

    public void TakeDamage(int amount)
    {
        Health = Math.Max(0, Health - amount);
    }

    public PlayerObject(GameRenderer renderer)
    {
        _textureId = renderer.LoadTexture(Path.Combine("Assets", "player.png"), out _);
        if (_textureId < 0)
        {
            throw new Exception("Failed to load player texture");
        }

        UpdateTarget();
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