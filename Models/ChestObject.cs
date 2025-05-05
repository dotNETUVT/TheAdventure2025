namespace TheAdventure.Models;

public class ChestObject : RenderableGameObject
{
    public bool IsOpened { get; private set; }
    public int BombReward { get; init; } = 1;
    public double InteractionRadius { get; init; } = 75.0;

    private DateTimeOffset? _openedAt;
    private int _openingDurationMs;

    public ChestObject(SpriteSheet spriteSheet, (int X, int Y) position)
        : base(spriteSheet, position)
    {
        SpriteSheet.Animations.TryGetValue("Closed", out _);
    }

    public bool CanInteract((int X, int Y) playerPosition)
    {
        if (IsOpened) return false;

        var dx = Position.X - playerPosition.X;
        var dy = Position.Y - playerPosition.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        return distance <= InteractionRadius;
    }

    public void Open(Action<int> onReward)
    {
        if (IsOpened) return;

        IsOpened = true;
        var anim = SpriteSheet.Animations["Opening"];
        _openingDurationMs = anim.DurationMs + 500;
        SpriteSheet.ActivateAnimation("Opening");
        _openedAt = DateTimeOffset.Now;

        // give the player their bombs right away
        onReward(BombReward);
    }

    public override void Render(GameRenderer renderer)
    {
        if (!IsOpened)
        {
            base.Render(renderer);
            return;
        }

        // if opened, check how long since then
        var elapsed = (DateTimeOffset.Now - _openedAt.Value).TotalMilliseconds;
        if (elapsed < _openingDurationMs)
        {
            // still in the middle of opening, draw the current frame:
            SpriteSheet.Render(renderer, Position, Angle, RotationCenter);
        }
    }
}