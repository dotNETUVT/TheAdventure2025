using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : GameObject
{
    private const int DefaultMaxHealth = 100;
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;

    private Rectangle<int> _source = new(0, 0, 48, 48);
    private Rectangle<int> _target = new(0, 0, 48, 48);

    private readonly int _textureId;

    private int Speed = 128; // pixels per second

    public event EventHandler OnPlayerDeath;

    public PlayerObject(GameRenderer renderer)
    {
        _textureId = renderer.LoadTexture(Path.Combine("Assets", "player.png"), out _);
        MaxHealth = DefaultMaxHealth;
        CurrentHealth = MaxHealth;
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

    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;

        CurrentHealth = Math.Max(0, CurrentHealth - damage);

        if (CurrentHealth <= 0)
        {
            OnDeath();
        }
    }

    public void Heal(int amount)
    {
        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
    }

    public void SpeedBoost(int amount)
    {
        Speed += amount;
    }
    public void RestoreFullHealth()
    {
        CurrentHealth = MaxHealth;
    }


    protected virtual void OnDeath()
    {
        OnPlayerDeath?.Invoke(this, EventArgs.Empty);
    }

}