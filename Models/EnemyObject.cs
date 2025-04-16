using System;
using System.Collections.Generic;

namespace TheAdventure.Models;

public class EnemyObject : RenderableGameObject
{
    public enum State { Patrol, Chase, Cooldown }

    public bool Dead  { get; private set; }
    public bool Alert { get; private set; }   // true once the enemy has spotted the player

    private readonly IReadOnlyList<(int X, int Y)> _waypoints;
    private int   _index;
    private State _state = State.Patrol;

    private const int   SpeedPxPerSec  = 96;
    private const int   SightRangePx   = 200;
    private const int   CaptureRadius  = 24;      // how close is “caught”
    private const float CooldownSeconds = 3f;

    private DateTimeOffset _cooldownUntil;

    public EnemyObject(SpriteSheet sheet,
                       IReadOnlyList<(int X, int Y)> waypoints,
                       (int X, int Y) startPos)
        : base(sheet, startPos)
    {
        _waypoints = waypoints.Count > 0 ? waypoints
                                         : throw new ArgumentException("Enemy needs waypoints");
    }

    // returns true if the player was caught this frame
    public bool Update(int ms, int playerX, int playerY)
    {
        if (Dead) return false;

        //------------------------------------------------ state machine
        switch (_state)
        {
            case State.Patrol:
                Patrol(ms, playerX, playerY);
                break;

            case State.Chase:
                Chase(ms, playerX, playerY);
                break;

            case State.Cooldown:
                if (DateTimeOffset.Now >= _cooldownUntil)
                    _state = State.Patrol;
                break;
        }

        // did we catch the player?
        double dist = Math.Sqrt(Math.Pow(playerX - Position.X, 2) +
                                Math.Pow(playerY - Position.Y, 2));

        return dist < CaptureRadius;
    }

    private void Patrol(int ms, int playerX, int playerY)
    {
        MoveTowards(_waypoints[_index], ms);

        // waypoint reached?
        if (DistanceTo(_waypoints[_index]) < 2)
            _index = (_index + 1) % _waypoints.Count;

        // spot player?
        if (DistanceTo((playerX, playerY)) < SightRangePx)
        {
            _state = State.Chase;
            Alert = true;
            Console.WriteLine("🚨 Enemy has spotted the player!");
        }
    }

    private void Chase(int ms, int playerX, int playerY)
    {
        MoveTowards((playerX, playerY), ms);

        // lost sight?
        if (DistanceTo((playerX, playerY)) > SightRangePx * 1.5)
        {
            _state = State.Cooldown;
            _cooldownUntil = DateTimeOffset.Now.AddSeconds(CooldownSeconds);
        }
    }

    private void MoveTowards((int X, int Y) target, int ms)
    {
        double dx = target.X - Position.X;
        double dy = target.Y - Position.Y;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 1) return;

        double nx = dx / dist;
        double ny = dy / dist;

        Position = (
            Position.X + (int)(nx * SpeedPxPerSec * ms / 1000.0),
            Position.Y + (int)(ny * SpeedPxPerSec * ms / 1000.0)
        );
    }

    private double DistanceTo((int X, int Y) p)
        => Math.Sqrt(Math.Pow(p.X - Position.X, 2) + Math.Pow(p.Y - Position.Y, 2));

    // called by engine when a bomb explodes
    public void Kill()
    {
        Dead  = true;
        Alert = false;
        // let the renderer ignore it by removing sprite sheet or moving off‑screen
        Position = (-10_000, -10_000);
    }
}
