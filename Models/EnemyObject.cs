using Silk.NET.Maths;
using System;

namespace TheAdventure.Models;

public class EnemyObject : GameObject
{
    private const int _speed = 30;
    private const int _detectionRadius = 10000;
    private const int _enemyRadius = 16;

    public bool IsAlive { get; private set; } = true;
    public bool IsDeathAnimationFinished { get; private set; } = false;
    public (int X, int Y) Position { get; private set; }

    private DateTimeOffset _deathTime;
    private const double _deathAnimationDuration = 0.8;

    public EnemyObject(int x, int y)
    {
        Position = (x, y);
    }

    public void UpdatePosition(PlayerObject player, double timeMs)
    {
        if (!IsAlive)
        {
            // Check if death animation is finished
            if (!IsDeathAnimationFinished &&
                (DateTimeOffset.Now - _deathTime).TotalSeconds >= _deathAnimationDuration)
            {
                IsDeathAnimationFinished = true;
            }
            return;
        }

        // Calculate distance to player
        double dx = player.Position.X - Position.X;
        double dy = player.Position.Y - Position.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        double speedMultiplier = 1.0;

        if (distance > 500)
        {
            speedMultiplier = 0.8;
        }
        else if (distance < _enemyRadius * 2)
        {
            speedMultiplier = 0.5;
        }

        if (distance > 0.1)
        {
            double dirX = dx / distance;
            double dirY = dy / distance;

            var pixelsToMove = _speed * speedMultiplier * (timeMs / 1000.0);

            int moveX = (int)(dirX * pixelsToMove);
            int moveY = (int)(dirY * pixelsToMove);

            if (moveX == 0 && Math.Abs(dx) > 0)
            {
                if (distance < 300)
                {
                    moveX = dx > 0 ? 1 : -1;
                }
            }
            if (moveY == 0 && Math.Abs(dy) > 0)
            {
                if (distance < 300)
                {
                    moveY = dy > 0 ? 1 : -1;
                }
            }

            Position = (Position.X + moveX, Position.Y + moveY);
        }
    }

    public bool CheckHit(PlayerObject player)
    {
        if (!IsAlive)
        {
            return false;
        }

        // Check if player's sword hits enemy
        if (player.State.State == PlayerObject.PlayerState.Attack)
        {
            // Use player's attack reach multiplier for more damage with buffs
            int hitRadius = (int)(60 * player.AttackReachMultiplier);
            int sideHitRadius = (int)(30 * player.AttackReachMultiplier);

            double dx = Position.X - player.Position.X;
            double dy = Position.Y - player.Position.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Check if enemy is in front of player based on direction
            bool isInAttackDirection = false;

            switch (player.State.Direction)
            {
                case PlayerObject.PlayerStateDirection.Up:
                    isInAttackDirection = dy < 0 && Math.Abs(dx) < sideHitRadius;
                    break;
                case PlayerObject.PlayerStateDirection.Down:
                    isInAttackDirection = dy > 0 && Math.Abs(dx) < sideHitRadius;
                    break;
                case PlayerObject.PlayerStateDirection.Left:
                    isInAttackDirection = dx < 0 && Math.Abs(dy) < sideHitRadius;
                    break;
                case PlayerObject.PlayerStateDirection.Right:
                    isInAttackDirection = dx > 0 && Math.Abs(dy) < sideHitRadius;
                    break;
            }

            if ((distance < hitRadius && isInAttackDirection) || distance < _enemyRadius + 15)
            {
                Die();
                return true;
            }
        }

        return false;
    }

    public bool CheckBombHit(int bombX, int bombY, float radiusMultiplier = 1.0f)
    {
        if (!IsAlive)
        {
            return false;
        }

        // Check if bomb explosion hits enemy
        const int baseBombRadius = 80;
        int bombRadius = (int)(baseBombRadius * radiusMultiplier);

        double dx = Position.X - bombX;
        double dy = Position.Y - bombY;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance < bombRadius)
        {
            Die();
            return true;
        }

        return false;
    }

    public bool CheckPlayerCollision(PlayerObject player)
    {
        if (!IsAlive)
        {
            return false;
        }

        double dx = Position.X - player.Position.X;
        double dy = Position.Y - player.Position.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        // Make collision detection more forgiving
        // Use a slightly smaller collision radius and ignore if player is attacking
        if (player.State.State == PlayerObject.PlayerState.Attack)
        {
            // During attack animation, player has temporary invincibility
            return false;
        }

        // Use a more forgiving collision radius (75% of visual radius)
        int collisionThreshold = _enemyRadius * 3 / 4;
        return distance < collisionThreshold;
    }

    public void Die()
    {
        if (IsAlive)
        {
            IsAlive = false;
            _deathTime = DateTimeOffset.Now;

            // Notify any listeners that the enemy has been defeated
            OnEnemyDefeated?.Invoke(this);
        }
    }

    // Event that gets fired when enemy dies - Engine will subscribe to this
    public static event Action<EnemyObject>? OnEnemyDefeated;

    public void Render(GameRenderer renderer)
    {
        if (IsDeathAnimationFinished)
        {
            return;
        }

        if (IsAlive)
        {
            renderer.SetDrawColor(255, 0, 0, 255);
        }
        else
        {
            // Fading out during death animation
            double progress = Math.Min(1.0, (DateTimeOffset.Now - _deathTime).TotalSeconds / _deathAnimationDuration);
            byte alpha = (byte)(255 * (1.0 - progress));
            renderer.SetDrawColor(255, 0, 0, alpha);
        }

        DrawFilledCircle(renderer, Position.X, Position.Y, _enemyRadius);
    }

    private void DrawFilledCircle(GameRenderer renderer, int centerX, int centerY, int radius)
    {
        for (int y = -radius; y <= radius; y++)
        {
            int width = (int)Math.Sqrt(radius * radius - y * y);

            for (int x = -width; x <= width; x++)
            {
                renderer.DrawPoint(centerX + x, centerY + y);
            }
        }
    }
}
