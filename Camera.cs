using Silk.NET.Maths;

namespace TheAdventure;

public class Camera
{
    private int _x;
    private int _y;
    private Rectangle<int> _worldBounds = new();

    public int X => _x;
    public int Y => _y;

    public readonly int Width;
    public readonly int Height;

    public float Zoom { get; private set; }
    private float _zoomSpeed = 0.1f;
    private float _minZoom = 0.5f;
    private float _maxZoom = 2.0f;
    private const float INITIAL_ZOOM = 1.0f;

    private int _panSpeed = 5;
    private int _targetX;
    private int _targetY;
    private (int X, int Y) _initialTargetPosition;

    public Camera(int width, int height)
    {
        Width = width;
        Height = height;
        _initialTargetPosition = (width / 2, height / 2);
        Reset(_initialTargetPosition.X, _initialTargetPosition.Y);
    }

    public void Reset(int targetX, int targetY)
    {
        _targetX = targetX;
        _targetY = targetY;
        _x = targetX;
        _y = targetY;
        Zoom = INITIAL_ZOOM;
    }

    public void SetWorldBounds(Rectangle<int> bounds)
    {
        var marginLeft = Width / 2;
        var marginTop = Height / 2;

        if (marginLeft * 2 > bounds.Size.X)
        {
            marginLeft = bounds.Size.X / 4;
        }
        if (marginTop * 2 > bounds.Size.Y)
        {
            marginTop = bounds.Size.Y / 4;
        }

        int minCamX = bounds.Origin.X + marginLeft;
        int minCamY = bounds.Origin.Y + marginTop;
        int maxCamX = bounds.Origin.X + bounds.Size.X - marginLeft;
        int maxCamY = bounds.Origin.Y + bounds.Size.Y - marginTop;
        int camMoveWidth = maxCamX - minCamX;
        int camMoveHeight = maxCamY - minCamY;

        if (camMoveWidth < 0) camMoveWidth = 0;
        if (camMoveHeight < 0) camMoveHeight = 0;

        _worldBounds = new Rectangle<int>(minCamX, minCamY, camMoveWidth, camMoveHeight);

        // After setting world bounds, ensure camera current pos is within these new valid bounds
        // This targets the center of the clampable area initially if player isn't set.
        _targetX = _worldBounds.Origin.X + _worldBounds.Size.X / 2;
        _targetY = _worldBounds.Origin.Y + _worldBounds.Size.Y / 2;

        _x = Math.Clamp(_targetX, _worldBounds.Origin.X, _worldBounds.Origin.X + _worldBounds.Size.X);
        _y = Math.Clamp(_targetY, _worldBounds.Origin.Y, _worldBounds.Origin.Y + _worldBounds.Size.Y);
    }

    public void LookAt(int x, int y)
    {
        _targetX = x;
        _targetY = y;
        Update();
    }

    public void Update()
    {
        _x = (int)Lerp(_x, _targetX, _panSpeed * 0.01f);
        _y = (int)Lerp(_y, _targetY, _panSpeed * 0.01f);

        if (_worldBounds.Size.X > 0 || _worldBounds.Size.Y > 0)
        {
            _x = Math.Clamp(_x, _worldBounds.Origin.X, _worldBounds.Origin.X + _worldBounds.Size.X);
            _y = Math.Clamp(_y, _worldBounds.Origin.Y, _worldBounds.Origin.Y + _worldBounds.Size.Y);
        }
    }

    public void AdjustZoom(float increment)
    {
        Zoom += increment * _zoomSpeed;
        Zoom = Math.Clamp(Zoom, _minZoom, _maxZoom);
    }

    public Rectangle<int> ToScreenCoordinates(Rectangle<int> worldRect)
    {
        int screenRectX = (int)((worldRect.Origin.X - _x) * Zoom + Width / 2.0f);
        int screenRectY = (int)((worldRect.Origin.Y - _y) * Zoom + Height / 2.0f);
        int screenRectWidth = (int)(worldRect.Size.X * Zoom);
        int screenRectHeight = (int)(worldRect.Size.Y * Zoom);
        return new Rectangle<int>(screenRectX, screenRectY, screenRectWidth, screenRectHeight);
    }

    public Vector2D<int> ToWorldCoordinates(Vector2D<int> screenPoint)
    {
        int worldX = (int)((screenPoint.X - Width / 2.0f) / Zoom + _x);
        int worldY = (int)((screenPoint.Y - Height / 2.0f) / Zoom + _y);
        return new Vector2D<int>(worldX, worldY);
    }

    private static float Lerp(float a, float b, float t)
    {
        t = Math.Clamp(t, 0.0f, 1.0f);
        return a + (b - a) * t;
    }
}