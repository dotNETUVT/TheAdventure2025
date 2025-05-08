using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int _speed = 128; // pixels per second
    private const int _sprintMultiplier = 2;
    private string _currentAnimation = "IdleDown";
    public bool IsDead { get; private set; } = false;

    public float MaxSprintEnergy { get; private set; } = 100f;
    public float CurrentSprintEnergy { get; private set; }
    public bool IsSprinting { get; private set; } = false;
    public float SprintRegenerationRate { get; private set; } = 15f; // energy per second
    public float SprintConsumptionRate { get; private set; } = 30f; // energy per second

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        SpriteSheet.ActivateAnimation(_currentAnimation);
        CurrentSprintEnergy = MaxSprintEnergy;
    }

    public void Die()
    {
        if (!IsDead)
        {
            IsDead = true;
            _currentAnimation = "Death";
            SpriteSheet.ActivateAnimation(_currentAnimation);
        }
    }

    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        if (IsDead)
            return;

        if (up + down + left + right == 0)
        {
            RegenerateSprint(time);
            return;
        }

        bool sprint = IsSprintActive(time);

        var speedMultiplier = sprint ? _sprintMultiplier : 1;
        var pixelsToMove = _speed * speedMultiplier * (time / 1000.0);

        var x = Position.X + (int)(right * pixelsToMove);
        x -= (int)(left * pixelsToMove);

        var y = Position.Y + (int)(down * pixelsToMove);
        y -= (int)(up * pixelsToMove);

        var newAnimation = _currentAnimation;

        if (y < Position.Y && _currentAnimation != "MoveUp")
        {
            newAnimation = "MoveUp";
        }

        if (y > Position.Y && newAnimation != "MoveDown")
        {
            newAnimation = "MoveDown";
        }

        if (x < Position.X && newAnimation != "MoveLeft")
        {
            newAnimation = "MoveLeft";
        }

        if (x > Position.X && newAnimation != "MoveRight")
        {
            newAnimation = "MoveRight";
        }

        if (x == Position.X && y == Position.Y && newAnimation != "IdleDown")
        {
            newAnimation = "IdleDown";
        }

        if (newAnimation != _currentAnimation)
        {
            _currentAnimation = newAnimation;
            SpriteSheet.ActivateAnimation(_currentAnimation);
        }

        Position = (x, y);
    }

    public bool IsDeathAnimationComplete()
    {
        if (!IsDead) return false;

        return SpriteSheet.IsAnimationComplete();
    }

    private void HandleSprint(bool sprintKeyPressed, double time)
    {
        float deltaTime = (float)(time / 1000.0);

        if (sprintKeyPressed && CurrentSprintEnergy > 0)
        {
            IsSprinting = true;

            CurrentSprintEnergy -= SprintConsumptionRate * deltaTime;

            if (CurrentSprintEnergy < 0)
                CurrentSprintEnergy = 0;
        }
        else
        {
            IsSprinting = false;

            RegenerateSprint(time);
        }
    }

    private void RegenerateSprint(double time)
    {
        if (CurrentSprintEnergy < MaxSprintEnergy)
        {
            float deltaTime = (float)(time / 1000.0);
            CurrentSprintEnergy += SprintRegenerationRate * deltaTime;

            if (CurrentSprintEnergy > MaxSprintEnergy)
                CurrentSprintEnergy = MaxSprintEnergy;
        }
    }

    public float GetSprintPercentage()
    {
        return CurrentSprintEnergy / MaxSprintEnergy;
    }

    private bool IsSprintActive(double time)
    {
        bool sprintKeyPressed = false;

        if (Engine.Instance != null)
        {
            sprintKeyPressed = Engine.Instance.Input.IsSprintPressed();
        }

        float deltaTime = (float)(time / 1000.0);

        if (sprintKeyPressed && CurrentSprintEnergy > 0)
        {
            IsSprinting = true;

            CurrentSprintEnergy -= SprintConsumptionRate * deltaTime;

            if (CurrentSprintEnergy < 0)
                CurrentSprintEnergy = 0;
        }
        else
        {
            IsSprinting = false;

            RegenerateSprint(time);
        }

        return IsSprinting;
    }
}