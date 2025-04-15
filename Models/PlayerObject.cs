using Silk.NET.Maths;
using System;
using Silk.NET.SDL;

namespace TheAdventure.Models;

public enum Direction
{
    None,
    Down,
    Left,
    Right,
    Up
}

public class PlayerObject : GameObject
{
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;

    // The width and height of the player object.
    private const int PlayerWidth = 96;
    private const int PlayerHeight = 96;

    private const int FrameWidth = 48;
    private const int FrameHeight = 48;

    private Rectangle<int> _source = new(0, 0, 48, 48);
    private Rectangle<int> _target = new(0, 0, PlayerWidth, PlayerHeight);

    private readonly int _textureId;

    private const int DefaultSpeed = 128;
    private const int RunningSpeed = 256;

    private Direction _currentDirection = Direction.Down;
    private int _currentFrame = 0;
    private float _animationTimeAccumulator = 0.0f;
    private const float FrameDuration = 0.1f;
    private const int TotalFrames = 3;

    // Indicates whether the player is in running mode.
    private bool _isRunning = false;

    // Internal timer to track elapsed time between updates.
    private DateTime _lastUpdateTime;

    public PlayerObject(GameRenderer renderer)
    {
        _textureId = renderer.LoadTexture(System.IO.Path.Combine("Assets", "player.png"), out _);
        if (_textureId < 0)
        {
            throw new Exception("Failed to load player texture");
        }

        _lastUpdateTime = DateTime.Now;
        UpdateTarget();
    }

    public void UpdatePosition(double up, double down, double left, double right, int time, bool runKey)
    {
        var now = DateTime.Now;
        var elapsedSeconds = (float)(now - _lastUpdateTime).TotalSeconds;
        _lastUpdateTime = now;

        var currentSpeed = runKey ? RunningSpeed : DefaultSpeed;
        var pixelsToMove = currentSpeed * elapsedSeconds;
        var moving = false;
        var newDirection = Direction.None;

        if (up > 0)
        {
            Y -= (int)(pixelsToMove * up);
            newDirection = Direction.Up;
            moving = true;
        }

        if (down > 0)
        {
            Y += (int)(pixelsToMove * down);
            newDirection = Direction.Down;
            moving = true;
        }

        if (left > 0)
        {
            X -= (int)(pixelsToMove * left);
            newDirection = Direction.Left;
            moving = true;
        }

        if (right > 0)
        {
            X += (int)(pixelsToMove * right);
            newDirection = Direction.Right;
            moving = true;
        }

        if (moving)
        {
            _currentDirection = newDirection;
            _isRunning = runKey;

            // Update the animation frame.
            _animationTimeAccumulator += elapsedSeconds;
            if (_animationTimeAccumulator >= FrameDuration)
            {
                _currentFrame = (_currentFrame + 1) % (TotalFrames + 1);
                _animationTimeAccumulator -= FrameDuration;
            }
        }
        else
        {
            _currentFrame = 0;
            _animationTimeAccumulator = 0;
            _isRunning = false;
        }

        // Select the sprite row based on the current direction and running state.
        var row = _currentDirection switch
        {
            Direction.Down => _isRunning ? 3 : 0,
            Direction.Left => _isRunning ? 4 : 1,
            Direction.Right => _isRunning ? 4 : 1,
            Direction.Up => _isRunning ? 5 : 2,
            _ => 0,
        };

        _source = new Rectangle<int>(_currentFrame * FrameWidth, row * FrameHeight, FrameWidth, FrameHeight);
        UpdateTarget();
    }

    public void Render(GameRenderer renderer)
    {
        // Flip the player texture when moving left.
        var flip = _currentDirection == Direction.Left ? RendererFlip.Horizontal : RendererFlip.None;
        renderer.RenderTexture(_textureId, _source, _target, flip);
    }

    private void UpdateTarget()
    {
        _target = new Rectangle<int>(X - PlayerWidth / 2, Y - PlayerHeight / 2, PlayerWidth, PlayerHeight);
    }
}