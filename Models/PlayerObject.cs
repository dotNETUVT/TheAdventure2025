using System;
using Silk.NET.SDL;

namespace TheAdventure.Models
{
    public class PlayerObject : RenderableGameObject
    {
        private const int _speed = 128; // pixels per second

        public enum PlayerStateDirection { None = 0, Down, Up, Left, Right }
        public enum PlayerState { None = 0, Idle, Move, Attack, GameOver }

        public (PlayerState State, PlayerStateDirection Direction) State { get; private set; }

        // ← NEW: track health
        public int Health { get; private set; } = 3;

        public PlayerObject(SpriteSheet spriteSheet, int x, int y)
            : base(spriteSheet, (x, y))
        {
            SetState(PlayerState.Idle, PlayerStateDirection.Down);
        }

        // ← NEW: call this when the player takes damage
        public void TakeDamage()
        {
            if (Health <= 0) return;
            Health--;
            if (Health <= 0)
                GameOver();
        }

        public void SetState(PlayerState state)
            => SetState(state, State.Direction);

        public void SetState(PlayerState state, PlayerStateDirection direction)
        {
            if (State.State == PlayerState.GameOver) return;
            if (State.State == state && State.Direction == direction) return;

            if (state == PlayerState.None && direction == PlayerStateDirection.None)
                SpriteSheet.ActivateAnimation(null);
            else if (state == PlayerState.GameOver)
                SpriteSheet.ActivateAnimation(Enum.GetName(state));
            else
                SpriteSheet.ActivateAnimation(Enum.GetName(state) + Enum.GetName(direction));

            State = (state, direction);
        }

        public void GameOver()
        {
            SetState(PlayerState.GameOver, PlayerStateDirection.None);
        }

        public void Attack()
        {
            if (State.State == PlayerState.GameOver) return;
            SetState(PlayerState.Attack, State.Direction);
        }

        public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
        {
            if (State.State == PlayerState.GameOver) return;

            var pixelsToMove = _speed * (time / 1000.0);
            int x = Position.X + (int)((right - left)   * pixelsToMove);
            int y = Position.Y + (int)((down  - up)     * pixelsToMove);

            var newState = State.State;
            var newDir   = State.Direction;

            if (x == Position.X && y == Position.Y)
            {
                if (State.State == PlayerState.Attack && SpriteSheet.AnimationFinished)
                    newState = PlayerState.Idle;
                else if (State.State != PlayerState.Attack)
                    newState = PlayerState.Idle;
            }
            else
            {
                newState = PlayerState.Move;
                if (y < Position.Y) newDir = PlayerStateDirection.Up;
                else if (y > Position.Y) newDir = PlayerStateDirection.Down;
                if (x < Position.X) newDir = PlayerStateDirection.Left;
                else if (x > Position.X) newDir = PlayerStateDirection.Right;
            }

            if (newState != State.State || newDir != State.Direction)
                SetState(newState, newDir);

            Position = (x, y);
        }
    }
}
