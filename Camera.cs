using Silk.NET.Maths;

namespace TheAdventure;

public class Camera
{
    private int _x; // World X-coordinate of the camera's center
    private int _y; // World Y-coordinate of the camera's center
    private Rectangle<int> _worldBounds = new(); // The area the camera's center is allowed to be in

    public int X => _x;
    public int Y => _y;

    public readonly int Width;  // Screen width
    public readonly int Height; // Screen height

    public float Zoom { get; private set; } = 1.0f;
    private float _zoomSpeed = 0.1f;
    private float _minZoom = 0.5f;
    private float _maxZoom = 2.0f;

    private int _panSpeed = 5;
    private int _targetX;
    private int _targetY;


    public Camera(int width, int height)
    {
        Width = width;
        Height = height;
        _targetX = width / 2;
        _targetY = height / 2;
        _x = _targetX;
        _y = _targetY;
    }

    public void SetWorldBounds(Rectangle<int> bounds) // bounds is the total world size
    {
        // Margin allows the camera to move to the edge of the world
        // such that the edge of the world aligns with the edge of the screen.
        // If zoom is 1, half the screen width/height is the margin.
        // This logic might need adjustment depending on desired edge behavior with zoom.
        // For now, these margins are in world units if zoom was 1.
        var marginLeft = Width / 2;
        var marginTop = Height / 2;

        // Ensure margins are not larger than the bounds themselves
        if (marginLeft * 2 > bounds.Size.X)
        {
            // This implies the screen is wider than the world, adjust margin logic or clamp.
            // For simplicity, let's assume world is larger than screen.
            // If world is smaller, camera might be fixed or show areas outside world.
            marginLeft = bounds.Size.X / 4; // Example adjustment
        }

        if (marginTop * 2 > bounds.Size.Y)
        {
            marginTop = bounds.Size.Y / 4; // Example adjustment
        }

        // _worldBounds defines the min/max for the camera's *center* (_x, _y).
        // Origin of the world (bounds.Origin.X, bounds.Origin.Y) plus margin.
        int minCamX = bounds.Origin.X + marginLeft;
        int minCamY = bounds.Origin.Y + marginTop;
        // Max extent of world (bounds.Origin.X + bounds.Size.X) minus margin.
        int maxCamX = bounds.Origin.X + bounds.Size.X - marginLeft;
        int maxCamY = bounds.Origin.Y + bounds.Size.Y - marginTop;

        // The size of the area the camera center can move in.
        int camMoveWidth = maxCamX - minCamX;
        int camMoveHeight = maxCamY - minCamY;

        if (camMoveWidth < 0) camMoveWidth = 0; // Prevent negative size if world is too small
        if (camMoveHeight < 0) camMoveHeight = 0;

        _worldBounds = new Rectangle<int>(minCamX, minCamY, camMoveWidth, camMoveHeight);

        _x = Math.Clamp(_x, _worldBounds.Origin.X, _worldBounds.Origin.X + _worldBounds.Size.X);
        _y = Math.Clamp(_y, _worldBounds.Origin.Y, _worldBounds.Origin.Y + _worldBounds.Size.Y);
        _targetX = _x;
        _targetY = _y;
    }

    public void LookAt(int x, int y)
    {
        _targetX = x;
        _targetY = y;
        Update(); // Process panning immediately or ensure Update() is called in game loop
    }

    public void Update()
    {
        _x = (int)Lerp(_x, _targetX, _panSpeed * 0.01f);
        _y = (int)Lerp(_y, _targetY, _panSpeed * 0.01f);

        if (_worldBounds.Size.X > 0 || _worldBounds.Size.Y > 0) // Only clamp if bounds are valid
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