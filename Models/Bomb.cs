using TheAdventure.Models;
using Silk.NET.SDL;
using System;

namespace TheAdventure.Models;

/// <summary>
/// Wraps TemporaryGameObject so we can know when to detonate.
/// </summary>
public class Bomb : TemporaryGameObject
{
    public DateTimeOffset SpawnTime { get; }
    public bool Detonated { get; set; }    // only explode once

    public Bomb(SpriteSheet sheet, double ttl, (int X, int Y) pos, double angle = 0,
                Point center = new())
        : base(sheet, ttl, pos, angle, center)
    {
        SpawnTime = DateTimeOffset.Now;
    }
}
