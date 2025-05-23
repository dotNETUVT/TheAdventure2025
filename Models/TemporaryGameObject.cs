using Silk.NET.SDL;
using System;

namespace TheAdventure.Models;

public class TemporaryGameObject : RenderableGameObject
{
    private DateTimeOffset _spawnTime;
    private double _ttlSeconds;

    private bool _isPaused = false;
    private DateTimeOffset _pauseStart;
    private TimeSpan _pauseAccumulated = TimeSpan.Zero;



    public TemporaryGameObject(SpriteSheet spriteSheet, double ttlSeconds, (int X, int Y) position, double angle = 0.0, Point rotationCenter = new())
        : base(spriteSheet, position, angle, rotationCenter)
    {
        _ttlSeconds = ttlSeconds;
        _spawnTime = DateTimeOffset.Now;
    }

    public bool IsExpired
    {
        get
        {
            if (_isPaused) return false;

            var effectiveElapsed = DateTimeOffset.Now - _spawnTime - _pauseAccumulated;
            return effectiveElapsed.TotalSeconds >= _ttlSeconds;
        }
    }

    public void Pause()
    {
        if (_isPaused) return;

        _isPaused = true;
        _pauseStart = DateTimeOffset.Now;

        SpriteSheet?.Pause(); 
    }

    public void Resume()
    {
        if (!_isPaused) return;

        _pauseAccumulated += DateTimeOffset.Now - _pauseStart;
        _isPaused = false;

        SpriteSheet?.Resume(); 
    }
}
