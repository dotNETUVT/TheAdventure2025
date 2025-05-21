using Silk.NET.Maths;
using System;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int _speed = 128;
    public int MaxHealth { get; private set; } = 100;
    public int CurrentHealth { get; private set; }
    private bool _isDead = false;
    private (int X, int Y) _initialPosition;

    public enum PlayerStateDirection { None = 0, Down, Up, Left, Right, }
    public enum PlayerState { None = 0, Idle, Move, Attack, GameOver }
    public (PlayerState State, PlayerStateDirection Direction) State { get; private set; }

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        _initialPosition = (x, y);
        CurrentHealth = MaxHealth;
        SetState(PlayerState.Idle, PlayerStateDirection.Down);
    }

    public void Reset()
    {
        CurrentHealth = MaxHealth;
        _isDead = false;
        Position = _initialPosition;
        SetState(PlayerState.Idle, PlayerStateDirection.Down);
    }

    public void TakeDamage(int amount)
    {
        if (_isDead) return;
        CurrentHealth -= amount;
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            GameOver();
        }
        Console.WriteLine($"Player took {amount} damage. Current Health: {CurrentHealth}/{MaxHealth}");
    }

    // New Heal method
    public void Heal(int amount)
    {
        if (_isDead) return; // Cannot heal if dead

        CurrentHealth += amount;
        if (CurrentHealth > MaxHealth)
        {
            CurrentHealth = MaxHealth;
        }
        Console.WriteLine($"Player healed by {amount}. Current Health: {CurrentHealth}/{MaxHealth}");
    }

    public void SetState(PlayerState newState, PlayerStateDirection newDirection)
    {
        // ... (SetState logic remains the same as the last correct version) ...
        if (_isDead && newState != PlayerState.GameOver)
        {
            return;
        }
        if (this.State.State == PlayerState.GameOver && newState != PlayerState.GameOver && _isDead)
        {
            return;
        }
        if (this.State.State == newState && this.State.Direction == newDirection)
        {
            return;
        }
        string? animationName = null;
        if (newState == PlayerState.None && newDirection == PlayerStateDirection.None)
        {
            animationName = null;
        }
        else if (newState == PlayerState.GameOver)
        {
            animationName = Enum.GetName(newState);
        }
        else
        {
            animationName = Enum.GetName(newState) + Enum.GetName(newDirection);
        }
        SpriteSheet.ActivateAnimation(animationName);
        this.State = (newState, newDirection);
    }

    public void GameOver()
    {
        // ... (GameOver logic remains the same) ...
        if (!_isDead)
        {
            Console.WriteLine("Player GameOver triggered.");
            _isDead = true;
            SetState(PlayerState.GameOver, PlayerStateDirection.None);
        }
    }

    public bool IsDead()
    {
        return _isDead;
    }

    public void Attack()
    {
        // ... (Attack logic remains the same) ...
        if (IsDead() || this.State.State == PlayerState.Attack)
        {
            return;
        }
        var direction = this.State.Direction;
        if (direction == PlayerStateDirection.None)
        {
            direction = PlayerStateDirection.Down;
        }
        SetState(PlayerState.Attack, direction);
    }

    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        // ... (UpdatePosition logic remains the same) ...
        if (IsDead())
        {
            if (this.State.State == PlayerState.GameOver && SpriteSheet.ActiveAnimation != null && !SpriteSheet.ActiveAnimation.Loop && SpriteSheet.AnimationFinished)
            {
            }
            return;
        }
        PlayerState currentStateBeforeInput = this.State.State;
        PlayerStateDirection currentDirectionBeforeInput = this.State.Direction;
        if (this.State.State == PlayerState.Attack && SpriteSheet.AnimationFinished)
        {
            SetState(PlayerState.Idle, currentDirectionBeforeInput);
            currentStateBeforeInput = PlayerState.Idle;
        }
        var pixelsToMove = _speed * (time / 1000.0);
        var newX = Position.X + (int)(right * pixelsToMove);
        newX -= (int)(left * pixelsToMove);
        var newY = Position.Y + (int)(down * pixelsToMove);
        newY -= (int)(up * pixelsToMove);
        PlayerState targetState = currentStateBeforeInput;
        PlayerStateDirection targetDirection = currentDirectionBeforeInput;
        if (newX == Position.X && newY == Position.Y)
        {
            if (targetState != PlayerState.Attack)
            {
                targetState = PlayerState.Idle;
            }
        }
        else
        {
            if (targetState != PlayerState.Attack)
            {
                targetState = PlayerState.Move;
            }
            if (newY < Position.Y) targetDirection = PlayerStateDirection.Up;
            else if (newY > Position.Y) targetDirection = PlayerStateDirection.Down;
            else if (newX < Position.X) targetDirection = PlayerStateDirection.Left;
            else if (newX > Position.X) targetDirection = PlayerStateDirection.Right;

            if (targetDirection == PlayerStateDirection.None && currentDirectionBeforeInput != PlayerStateDirection.None)
            {
                targetDirection = currentDirectionBeforeInput;
            }
            else if (targetDirection == PlayerStateDirection.None)
            {
                targetDirection = PlayerStateDirection.Down;
            }
        }
        SetState(targetState, targetDirection);
        Position = (newX, newY);
    }
}