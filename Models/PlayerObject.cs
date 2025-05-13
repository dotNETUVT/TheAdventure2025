using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : GameObject
{
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;

    private Rectangle<int> _source = new(0, 0, 48, 48);
    private Rectangle<int> _target = new(0, 0, 48, 48);

    private readonly int _textureId;

    private const int Speed = 128;

    public int Health { get; set; } = 2;

    private Direction _direction = Direction.Down;
    private int _frame = 0;
    private double _frameTimer = 0;
    private const int FrameWidth = 48;
    private const int FrameHeight = 48;
    private const int FrameCount = 4; 
    private const double FrameDuration = 0.2; 

    private bool _isDead = false;
    private int _deathFrame = 0;
    private double _deathAnimationTimer = 0;
    private const int DeathFrameCount = 3;
    private const double DeathFrameDuration = 0.3;



    public enum Direction
    {
        Down = 0,
        Left = 3,
        Right = 1,
        Up = 2
    }


    public PlayerObject(GameRenderer renderer)
    {
        _textureId = renderer.LoadTexture(Path.Combine("Assets", "player.png"), out _);
        if (_textureId < 0)
        {
            throw new Exception("Failed to load player texture");
        }

        UpdateTarget();
    }

    public void UpdatePosition(double up, double down, double left, double right, int time)
    {

        if (_isDead)
        {
            _deathAnimationTimer += time / 2000.0;
            if (_deathAnimationTimer >= DeathFrameDuration && _deathFrame < DeathFrameCount - 1)
            {
                _deathFrame++;
                _deathAnimationTimer = 0;
            }

            UpdateDeathSource();
            return;
        }

        var pixelsToMove = Speed * (time / 1000.0);

        bool moved = false;

        if (up > 0)
        {
            Y -= (int)(pixelsToMove * up);
            _direction = Direction.Up;
            moved = true;
        }
        else if (down > 0)
        {
            Y += (int)(pixelsToMove * down);
            _direction = Direction.Down;
            moved = true;
        }

        if (left > 0)
        {
            X -= (int)(pixelsToMove * left);
            _direction = Direction.Left;
            moved = true;
        }
        else if (right > 0)
        {
            X += (int)(pixelsToMove * right);
            _direction = Direction.Right;
            moved = true;
        }

        UpdateTarget();

        if (moved)
        {
            _frameTimer += time / 1000.0;
            if (_frameTimer >= FrameDuration)
            {
                _frame = (_frame + 1) % FrameCount;
                _frameTimer = 0;
            }
        }
        else
        {
            _frame = 0;
        }

        UpdateSource();
    }


    public void Render(GameRenderer renderer)
    {
        renderer.RenderTexture(_textureId, _source, _target);
    }

    private void UpdateTarget()
    {
        _target = new(X + 24, Y - 42, 48, 48);
    }

    private void UpdateSource()
    {
        _source = new Rectangle<int>(
            _frame * FrameWidth,
            (int)_direction * FrameHeight,
            FrameWidth,
            FrameHeight
        );
    }

    public void Die()
    {
        _isDead = true;
        _deathFrame = 0;
        _deathAnimationTimer = 0;
    }

    private static readonly (int X, int Y)[] DeathFrames = new[]
{
    (0, 9),
    (1, 9),
    (2, 9)
};


    private void UpdateDeathSource()
    {
        var (frameX, frameY) = DeathFrames[_deathFrame];
        _source = new Rectangle<int>(
            frameX * FrameWidth,
            frameY * FrameHeight,
            FrameWidth,
            FrameHeight
        );
    }




}