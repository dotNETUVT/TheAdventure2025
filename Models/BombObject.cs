namespace TheAdventure.Models;
using Silk.NET.Maths;

public class BombObject : TemporaryGameObject
{
    private readonly PlayerObject _player;
    private readonly List<Data.EnemyObject> _enemies;
    private readonly int explosionRadius = 24;
    private readonly double damageTriggerTime = 1.1;
    private bool _hasExploded = false;

    private DateTimeOffset _spawnTime;

    public BombObject(SpriteSheet spriteSheet, double ttl, (int X, int Y) position, PlayerObject player, List<Data.EnemyObject> enemies)
        : base(spriteSheet, ttl, position)
    {
        _player = player;
        _enemies = enemies;
        _spawnTime = DateTimeOffset.Now;
    }

    public override void Update(int elapsedMs)
    {
        base.Update(elapsedMs);

        if (_hasExploded || IsExpired)
            return;

        var timeSinceSpawn = (DateTimeOffset.Now - _spawnTime).TotalSeconds;
        if (timeSinceSpawn < damageTriggerTime)
            return;

        _hasExploded = true;

        var explosionArea = new Rectangle<int>(Position.X - explosionRadius, Position.Y - explosionRadius,
                                               explosionRadius * 2, explosionRadius * 2);
        
        var playerRect = new Rectangle<int>(_player.X - 24, _player.Y - 24, 48, 48);
        if (!_player.IsDead() && RectanglesIntersect(explosionArea, playerRect))
        {
            _player.TriggerDeath();
        }
        
        foreach (var enemy in _enemies)
        {
            var enemyRect = new Rectangle<int>(enemy.Position.X - 8, enemy.Position.Y - 12, 16, 24);
            if (RectanglesIntersect(explosionArea, enemyRect))
            {
                enemy.ForceDeath();
            }
        }
    }

    private static bool RectanglesIntersect(Rectangle<int> a, Rectangle<int> b)
    {
        return a.Origin.X < b.Origin.X + b.Size.X &&
               a.Origin.X + a.Size.X > b.Origin.X &&
               a.Origin.Y < b.Origin.Y + b.Size.Y &&
               a.Origin.Y + a.Size.Y > b.Origin.Y;
    }
}
