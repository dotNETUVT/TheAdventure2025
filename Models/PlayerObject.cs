using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : GameObject
{
    private float _posX = 100f;
    private float _posY = 100f;
    private float _targetPosX = 100f;
    private float _targetPosY = 100f;

    public int X { get => (int)Math.Round(_posX); set => _posX = _targetPosX = value; }
    public int Y { get => (int)Math.Round(_posY); set => _posY = _targetPosY = value; }

    private float _velocityX = 0f;
    private float _velocityY = 0f;

    private Rectangle<int> _source = new(0, 0, 48, 48);
    private Rectangle<int> _target = new(0, 0, 48, 48);

    private readonly int _textureId;

    private const float MaxSpeed = 200f;
    private const float Acceleration = 600f;
    private const float Friction = 300f;
    private const float PositionSmoothing = 0.2f;

    public PlayerObject(int id) : base(id)
    {
        _textureId = GameRenderer.LoadTexture(Path.Combine("Assets", "player.png"), out _);
        if (_textureId < 0)
        {
            throw new Exception("Failed to load player texture");
        }
        UpdateTarget();
    }

    public void UpdatePosition(double up, double down, double left, double right, int time)
    {
        float deltaTime = time / 1000f;

        float inputX = (float)(right - left);
        float inputY = (float)(down - up);
        float inputMagnitude = (float)Math.Sqrt(inputX * inputX + inputY * inputY);
        if (inputMagnitude > 1f)
        {
            inputX /= inputMagnitude;
            inputY /= inputMagnitude;
        }

        _velocityX += inputX * Acceleration * deltaTime;
        _velocityY += inputY * Acceleration * deltaTime;

        float velocityMagnitude = (float)Math.Sqrt(_velocityX * _velocityX + _velocityY * _velocityY);
        if (velocityMagnitude > 0)
        {
            float frictionForce = Friction * deltaTime;
            float newMagnitude = Math.Max(0, velocityMagnitude - frictionForce);

            if (velocityMagnitude > 0.02f)
            {
                float scale = newMagnitude / velocityMagnitude;
                _velocityX *= scale;
                _velocityY *= scale;
            }
            else
            {
                _velocityX = 0;
                _velocityY = 0;
            }
        }

        if (velocityMagnitude > MaxSpeed)
        {
            float scale = MaxSpeed / velocityMagnitude;
            _velocityX *= scale;
            _velocityY *= scale;
        }

        _targetPosX += _velocityX * deltaTime;
        _targetPosY += _velocityY * deltaTime;

        _posX = Lerp(_posX, _targetPosX, PositionSmoothing);
        _posY = Lerp(_posY, _targetPosY, PositionSmoothing);

        UpdateTarget();
    }

    public void Render(GameRenderer renderer)
    {
        renderer.RenderTexture(_textureId, _source, _target);
    }

    private void UpdateTarget()
    {
        _target = new((int)_posX + 24, (int)_posY - 42, 48, 48);
    }

    private float Lerp(float start, float end, float amount)
    {
        return start + (end - start) * amount;
    }
}