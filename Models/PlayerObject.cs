using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : GameObject
{
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;

    private Rectangle<int> _source = new(0, 0, 48, 48);
    private Rectangle<int> _target = new(0, 0, 48, 48);

    private readonly int _textureId;

    private const int Speed = 128; // pixels per second



    private double _jumpOffset = 0;
    private double _velocityY = 0;
    private const double Gravity = 0.0015;
    private bool _isOnGround = true;



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
        var pixelsToMove = Speed * (time / 1000.0);

        Y -= (int)(pixelsToMove * up);
        Y += (int)(pixelsToMove * down);
        X -= (int)(pixelsToMove * left);
        X += (int)(pixelsToMove * right);

        
        if (!_isOnGround)
        {
            _velocityY += Gravity * time;
            _jumpOffset += _velocityY * time;

            if (_jumpOffset >= 0)
            {
                _jumpOffset = 0;
                _velocityY = 0;
                _isOnGround = true;
            }
        }

        UpdateTarget();
    }

    public void Jump()
    {
        if (_isOnGround)
        {
            _velocityY = -0.5; 
            _isOnGround = false;
        }
    }

    public void Render(GameRenderer renderer)
    {
        renderer.RenderTexture(_textureId, _source, _target);
    }

    private void UpdateTarget()
    {
        _target = new(X + 24, (int)(Y - 42 + _jumpOffset), 48, 48);
    }

}