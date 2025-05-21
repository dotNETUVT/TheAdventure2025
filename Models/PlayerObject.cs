using System;
using System.Collections.Generic;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private int _baseSpeed = 128; // pixels per second

    // Player stats modifiable by buffs
    private float _speedMultiplier = 1.0f;
    private float _attackReachMultiplier = 1.0f;
    private int _maxBombs = 1;
    private float _bombRadiusMultiplier = 1.0f;

    // Tracking active buffs
    private readonly List<PlayerBuff> _activeBuffs = new();

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

    // New properties for wave system
    public int CurrentSpeed => (int)(_baseSpeed * _speedMultiplier);
    public float AttackReachMultiplier => _attackReachMultiplier;
    public int MaxBombs => _maxBombs;
    public float BombRadiusMultiplier => _bombRadiusMultiplier;
    public IReadOnlyList<PlayerBuff> ActiveBuffs => _activeBuffs;

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        SetState(PlayerState.Idle, PlayerStateDirection.Down);
    }

    public void ApplyBuff(PlayerBuffType buffType)
    {
        var buff = PlayerBuff.CreateBuff(buffType);
        _activeBuffs.Add(buff);

        // Apply buff effects
        switch (buffType)
        {
            case PlayerBuffType.SpeedBoost:
                _speedMultiplier *= buff.Value;
                break;
            case PlayerBuffType.DamageBoost:
                _attackReachMultiplier *= buff.Value;
                break;
            case PlayerBuffType.ExtraBomb:
                _maxBombs += 1;
                break;
            case PlayerBuffType.BombRadius:
                _bombRadiusMultiplier *= buff.Value;
                break;
            case PlayerBuffType.HealthRestore:
                break;
        }
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

        // Allow changing from Attack to another state even if direction is the same
        bool isChangingFromAttack = State.State == PlayerState.Attack && state != PlayerState.Attack;

        if (State.State == state && State.Direction == direction && !isChangingFromAttack)
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

    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        if (State.State == PlayerState.GameOver)
        {
            return;
        }

        var pixelsToMove = CurrentSpeed * (time / 1000.0);

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
            // Only change to Move state if not currently attacking
            if (State.State != PlayerState.Attack)
            {
                newState = PlayerState.Move;
            }

            if (y < Position.Y && newDirection != PlayerStateDirection.Up)
            {
                newDirection = PlayerStateDirection.Up;
            }

            if (y > Position.Y && newDirection != PlayerStateDirection.Down)
            {
                newDirection = PlayerStateDirection.Down;
            }

            if (x < Position.X && newDirection != PlayerStateDirection.Left)
            {
                newDirection = PlayerStateDirection.Left;
            }

            if (x > Position.X && newDirection != PlayerStateDirection.Right)
            {
                newDirection = PlayerStateDirection.Right;
            }
        }

        if (newState != State.State || newDirection != State.Direction)
        {
            SetState(newState, newDirection);
        }

        Position = (x, y);
    }
}