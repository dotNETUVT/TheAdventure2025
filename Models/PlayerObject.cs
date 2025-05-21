using Silk.NET.Maths;
using System.Collections.Generic;
using System.Linq;
using System; // Required for Enum.GetName

namespace TheAdventure.Models;

public enum EffectType
{
    SpeedBoost
}

public class ActivePlayerEffect
{
    public EffectType Type { get; }
    public double TimeRemainingMs { get; set; }
    public object Data { get; } // e.g., float for speed multiplier

    public ActivePlayerEffect(EffectType type, double durationMs, object data)
    {
        Type = type;
        TimeRemainingMs = durationMs;
        Data = data;
    }
}

public class PlayerObject : RenderableGameObject
{
    private const int BASE_SPEED = 128; // pixels per second
    public float CurrentSpeedMultiplier { get; private set; } = 1.0f;
    private List<ActivePlayerEffect> _activeEffects = new();

    public enum PlayerStateDirection
    {
        None = 0,
        Down,
        Up,
        Left,
        Right,
    }

    public enum PlayerState
    {
        None = 0,
        Idle,
        Move,
        Attack,
        GameOver
    }

    public (PlayerState State, PlayerStateDirection Direction) State { get; private set; }

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        SetState(PlayerState.Idle, PlayerStateDirection.Down);
    }

    public void SetState(PlayerState state)
    {
        SetState(state, State.Direction);
    }

    public void SetState(PlayerState state, PlayerStateDirection direction)
    {
        if (State.State == PlayerState.GameOver)
        {
            return;
        }

        if (State.State == state && State.Direction == direction)
        {
            return;
        }

        if (state == PlayerState.None && direction == PlayerStateDirection.None)
        {
            SpriteSheet.ActivateAnimation(null);
        }
        else if (state == PlayerState.GameOver)
        {
            SpriteSheet.ActivateAnimation(Enum.GetName(state));
        }
        else
        {
            var animationName = Enum.GetName(state) + Enum.GetName(direction);
            SpriteSheet.ActivateAnimation(animationName);
        }

        State = (state, direction);
    }

    public void GameOver()
    {
        SetState(PlayerState.GameOver, PlayerStateDirection.None);
    }

    public void Attack()
    {
        if (State.State == PlayerState.GameOver)
        {
            return;
        }

        var direction = State.Direction;
        SetState(PlayerState.Attack, direction);
    }

    private void UpdateEffects(double msSinceLastFrame)
    {
        bool effectsChanged = false;
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = _activeEffects[i];
            effect.TimeRemainingMs -= msSinceLastFrame;
            if (effect.TimeRemainingMs <= 0)
            {
                _activeEffects.RemoveAt(i);
                effectsChanged = true;
            }
        }

        if (effectsChanged)
        {
            RecalculateSpeedMultiplier();
        }
    }

    private void RecalculateSpeedMultiplier()
    {
        CurrentSpeedMultiplier = 1.0f; // Reset to base
        var speedBoostEffect = _activeEffects.FirstOrDefault(e => e.Type == EffectType.SpeedBoost);
        if (speedBoostEffect != null)
        {
            CurrentSpeedMultiplier = (float)speedBoostEffect.Data;
        }
        // In the future, if multiple effects can alter speed, logic here would combine them.
    }

    public void ApplySpeedBoost(float multiplier, double durationMs)
    {
        // Remove any existing speed boost to apply the new one (or decide on stacking logic)
        _activeEffects.RemoveAll(e => e.Type == EffectType.SpeedBoost);
        _activeEffects.Add(new ActivePlayerEffect(EffectType.SpeedBoost, durationMs, multiplier));
        RecalculateSpeedMultiplier(); // Immediately apply the new multiplier
    }

    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        UpdateEffects(time); // Update active effects and their durations

        if (State.State == PlayerState.GameOver)
        {
            return;
        }

        var pixelsToMove = BASE_SPEED * CurrentSpeedMultiplier * (time / 1000.0);

        var x = Position.X + (int)(right * pixelsToMove);
        x -= (int)(left * pixelsToMove);

        var y = Position.Y + (int)(down * pixelsToMove);
        y -= (int)(up * pixelsToMove);

        var newState = State.State;
        var newDirection = State.Direction;

        if (x == Position.X && y == Position.Y)
        {
            if (State.State == PlayerState.Attack)
            {
                if (SpriteSheet.AnimationFinished)
                {
                    newState = PlayerState.Idle;
                }
            }
            else
            {
                newState = PlayerState.Idle;
            }
        }
        else
        {
            newState = PlayerState.Move;
            
            if (y < Position.Y && (Math.Abs(y - Position.Y) > Math.Abs(x - Position.X) || right == 0.0 && left == 0.0))
            {
                newDirection = PlayerStateDirection.Up;
            }
            else if (y > Position.Y && (Math.Abs(y - Position.Y) > Math.Abs(x - Position.X) || right == 0.0 && left == 0.0))
            {
                newDirection = PlayerStateDirection.Down;
            }
            else if (x < Position.X && (Math.Abs(x - Position.X) >= Math.Abs(y - Position.Y) || up == 0.0 && down == 0.0))
            {
                newDirection = PlayerStateDirection.Left;
            }
            else if (x > Position.X && (Math.Abs(x - Position.X) >= Math.Abs(y - Position.Y) || up == 0.0 && down == 0.0))
            {
                newDirection = PlayerStateDirection.Right;
            }
        }
        
        // Fix for direction sticking when stopping diagonal movement
        if (newState == PlayerState.Idle)
        {
            // Keep current direction unless it was None
            if (State.Direction != PlayerStateDirection.None) {
                newDirection = State.Direction;
            } else {
                 // Default to down if no prior direction
                newDirection = PlayerStateDirection.Down;
            }
        }


        if (newState != State.State || newDirection != State.Direction)
        {
            SetState(newState, newDirection);
        }

        Position = (x, y);
    }
}