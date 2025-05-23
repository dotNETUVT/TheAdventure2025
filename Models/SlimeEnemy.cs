using Silk.NET.Maths;
using Silk.NET.SDL;

namespace TheAdventure.Models;

public class SlimeEnemy : RenderableGameObject
{
    private const double _jumpSpeed = 64.0;
    private const double _jumpCooldownMs = 800.0;
    private const int _baseJumpDistance = 28;
    private const double _randomnessRange = 0.3;
    private const int _closeRangeDistance = 120;
    private const int _longRangeDistance = 300;
    private DateTimeOffset _lastJumpTime = DateTimeOffset.MinValue;
    private DateTimeOffset _spawnTime = DateTimeOffset.Now;
    private bool _isJumping = false;
    private (int X, int Y) _jumpTarget;
    private (int X, int Y) _jumpStart;
    private double _jumpProgress = 0.0;

    private readonly double _jumpCooldownVariation;
    private readonly double _jumpDistanceVariation;
    private readonly double _aggressionLevel;
    private readonly Random _random;

    private TrackingState _currentTrackingState = TrackingState.Searching;

    public bool IsAlive { get; private set; } = true;
    public SlimeType Type { get; private set; }

    public enum TrackingState
    {
        Searching,    // the slime knows general direction but moves more slowly
        Pursuing,     // the slime is actively chasing at normal speed
        Attacking     // the slime is very close and moves aggressively
    }

    public enum SlimeType
    {
        Normal,
        Fast,
        Heavy,
        Erratic
    }

    public SlimeEnemy(SpriteSheet spriteSheet, (int X, int Y) position, SlimeType type = SlimeType.Normal)
        : base(spriteSheet, position)
    {
        Type = type;
        _random = new Random(GetHashCode());

        _jumpCooldownVariation = 0.7 + (_random.NextDouble() * 0.6);
        _jumpDistanceVariation = 0.8 + (_random.NextDouble() * 0.4);
        _aggressionLevel = _random.NextDouble();

        SpriteSheet.ActivateAnimation("Idle");
    }

    public void Update(double deltaTimeMs, (int X, int Y) playerPosition, Rectangle<int> worldBounds)
    {
        if (!IsAlive) return;

        var currentTime = DateTimeOffset.Now;
        var timeSinceLastJump = (currentTime - _lastJumpTime).TotalMilliseconds;
        var timeSinceSpawn = (currentTime - _spawnTime).TotalMilliseconds;

        if (timeSinceSpawn < 500) return;

        var deltaX = playerPosition.X - Position.X;
        var deltaY = playerPosition.Y - Position.Y;
        var distanceToPlayer = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

        UpdateTrackingState(distanceToPlayer);

        if (_isJumping)
        {
            UpdateJumpMovement(deltaTimeMs);
        }
        else if (ShouldStartNewJump(timeSinceLastJump, distanceToPlayer))
        {
            StartJumpTowardsPlayer(playerPosition, worldBounds, distanceToPlayer);
        }
    }

    private void UpdateTrackingState(double distanceToPlayer)
    {
        if (distanceToPlayer <= _closeRangeDistance)
        {
            _currentTrackingState = TrackingState.Attacking;
        }
        else if (distanceToPlayer <= _longRangeDistance)
        {
            _currentTrackingState = TrackingState.Pursuing;
        }
        else
        {
            _currentTrackingState = TrackingState.Searching;
        }
    }

    private void UpdateJumpMovement(double deltaTimeMs)
    {
        var jumpDuration = GetJumpDuration();
        _jumpProgress += deltaTimeMs / jumpDuration;

        if (_jumpProgress >= 1.0)
        {
            _jumpProgress = 1.0;
            Position = _jumpTarget;
            _isJumping = false;
            SpriteSheet.ActivateAnimation("Idle");
        }
        else
        {
            var lerpX = (int)(_jumpStart.X + (_jumpTarget.X - _jumpStart.X) * _jumpProgress);
            var lerpY = (int)(_jumpStart.Y + (_jumpTarget.Y - _jumpStart.Y) * _jumpProgress);

            var jumpDistance = Math.Sqrt(
                Math.Pow(_jumpTarget.X - _jumpStart.X, 2) +
                Math.Pow(_jumpTarget.Y - _jumpStart.Y, 2));
            var maxArcHeight = (int)(jumpDistance * 0.3);
            var currentArcHeight = (int)(Math.Sin(_jumpProgress * Math.PI) * maxArcHeight);

            Position = (lerpX, lerpY - currentArcHeight);
        }
    }

    private bool ShouldStartNewJump(double timeSinceLastJump, double distanceToPlayer)
    {
        var adjustedCooldown = GetAdjustedJumpCooldown();
        var stateMultiplier = _currentTrackingState switch
        {
            TrackingState.Attacking => 0.7,
            TrackingState.Pursuing => 1.0,
            TrackingState.Searching => 1.5,
            _ => 1.0
        };

        adjustedCooldown *= stateMultiplier;

        switch (Type)
        {
            case SlimeType.Fast:
                return timeSinceLastJump >= adjustedCooldown * 0.7;

            case SlimeType.Heavy:
                return timeSinceLastJump >= adjustedCooldown * 1.4;

            case SlimeType.Erratic:
                var shouldRandomJump = _random.NextDouble() > 0.98 && timeSinceLastJump >= adjustedCooldown * 0.3;
                return shouldRandomJump || timeSinceLastJump >= adjustedCooldown;

            default:
                return timeSinceLastJump >= adjustedCooldown;
        }
    }

    private void StartJumpTowardsPlayer((int X, int Y) playerPosition, Rectangle<int> worldBounds, double distanceToPlayer)
    {
        var deltaX = playerPosition.X - Position.X;
        var deltaY = playerPosition.Y - Position.Y;
        var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

        if (distance < 5) return;

        var normalizedX = deltaX / distance;
        var normalizedY = deltaY / distance;

        var randomnessAmount = _randomnessRange * (1.0 - _aggressionLevel);

        var stateRandomnessMultiplier = _currentTrackingState switch
        {
            TrackingState.Attacking => 0.5,
            TrackingState.Pursuing => 1.0,
            TrackingState.Searching => 2.0,
            _ => 1.0
        };

        randomnessAmount *= stateRandomnessMultiplier;

        if (Type == SlimeType.Erratic) randomnessAmount *= 2.0;

        var randomAngle = (_random.NextDouble() - 0.5) * randomnessAmount * Math.PI;
        var cosAngle = Math.Cos(randomAngle);
        var sinAngle = Math.Sin(randomAngle);

        var adjustedX = normalizedX * cosAngle - normalizedY * sinAngle;
        var adjustedY = normalizedX * sinAngle + normalizedY * cosAngle;

        var jumpDistance = GetAdjustedJumpDistance();

        if (_currentTrackingState == TrackingState.Searching)
        {
            jumpDistance = (int)(jumpDistance * 1.3);
        }

        var jumpX = (int)(adjustedX * jumpDistance);
        var jumpY = (int)(adjustedY * jumpDistance);

        var targetX = Math.Max(worldBounds.Origin.X,
                      Math.Min(worldBounds.Origin.X + worldBounds.Size.X, Position.X + jumpX));
        var targetY = Math.Max(worldBounds.Origin.Y,
                      Math.Min(worldBounds.Origin.Y + worldBounds.Size.Y, Position.Y + jumpY));

        _jumpStart = Position;
        _jumpTarget = (targetX, targetY);
        _jumpProgress = 0.0;
        _isJumping = true;
        _lastJumpTime = DateTimeOffset.Now;

        SpriteSheet.ActivateAnimation("Jump");
    }
    private double GetAdjustedJumpCooldown()
    {
        return _jumpCooldownMs * _jumpCooldownVariation;
    }

    private int GetAdjustedJumpDistance()
    {
        var baseDistance = Type switch
        {
            SlimeType.Fast => _baseJumpDistance * 0.8,
            SlimeType.Heavy => _baseJumpDistance * 1.3,
            SlimeType.Erratic => _baseJumpDistance * (0.6 + _random.NextDouble() * 0.8),
            _ => _baseJumpDistance
        };

        return (int)(baseDistance * _jumpDistanceVariation);
    }

    private double GetJumpDuration()
    {
        return Type switch
        {
            SlimeType.Fast => _jumpCooldownMs * 0.3,
            SlimeType.Heavy => _jumpCooldownMs * 0.5,
            _ => _jumpCooldownMs * 0.4
        };
    }

    public bool CheckCollisionWithPlayer((int X, int Y) playerPosition, int playerSize = 28)
    {
        if (!IsAlive) return false;

        var deltaX = Position.X - playerPosition.X;
        var deltaY = Position.Y - playerPosition.Y;
        var distanceSquared = deltaX * deltaX + deltaY * deltaY;
        var collisionDistanceSquared = playerSize * playerSize;

        return distanceSquared <= collisionDistanceSquared;
    }

    public bool CheckCollisionWithBomb((int X, int Y) bombPosition, int explosionRadius = 32)
    {
        if (!IsAlive) return false;

        var deltaX = Position.X - bombPosition.X;
        var deltaY = Position.Y - bombPosition.Y;
        var distanceSquared = deltaX * deltaX + deltaY * deltaY;
        var explosionRadiusSquared = explosionRadius * explosionRadius;

        if (distanceSquared <= explosionRadiusSquared)
        {
            Die();
            return true;
        }

        return false;
    }

    public void Die()
    {
        if (!IsAlive) return;

        IsAlive = false;
        SpriteSheet.ActivateAnimation("Death");
        _isJumping = false;
    }

    public override void Render(GameRenderer renderer)
    {
        base.Render(renderer);
    }
}