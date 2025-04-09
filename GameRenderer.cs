using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheAdventure.Models;

namespace TheAdventure;

public unsafe partial class GameRenderer
{
    private readonly Sdl _sdl;
    private readonly IntPtr _renderer;
    private readonly GameLogic _gameLogic;

    private readonly Dictionary<int, IntPtr> _texturePointers;
    private readonly Dictionary<int, TextureData> _textureData;
    private int _index = 0;

    private readonly GameCamera _camera;

    private static GameRenderer? _instance;
    private DateTimeOffset _lastFrameRenderedAt = DateTimeOffset.MinValue;

    public GameRenderer(Sdl sdl, GameWindow gameWindow, GameLogic gameLogic)
    {
        _sdl = sdl;
        _renderer = gameWindow.CreateRenderer();
        _gameLogic = gameLogic;

        _textureData = new();
        _texturePointers = new();


        var windowSize = gameWindow.Size;
        _camera = new(windowSize.Width, windowSize.Height);

        _instance = this;
    }
    

    
    public void CameraLookAt(int x, int y)
    {
        _camera.LookAt(x, y);
    }

    public void RenderGameObject(RenderableGameObject gameObject)
    {
        if (_texturePointers.TryGetValue(gameObject.TextureId, out var imageTexture))
        {
            var textureSrc = gameObject.TextureSource;
            var textureDest = _camera.ToScreenCoordinates(gameObject.TextureDestination);
            var rotCenter = gameObject.TextureRotationCenter;
            _sdl.RenderCopyEx((Renderer*)_renderer, (Texture*)imageTexture, in textureSrc, in textureDest,
                gameObject.TextureRotation, in rotCenter, RendererFlip.None);
        }
    }

    public void Render()
    {
        var renderer = (Renderer*)_renderer;

        var playerPos = _gameLogic.GetPlayerPosition();
        _camera.LookAt(playerPos.X, playerPos.Y);

        _sdl.RenderClear(renderer);

        var timeSinceLastFrame = 0;
        var now = DateTimeOffset.UtcNow;
        if (_lastFrameRenderedAt > DateTimeOffset.MinValue)
        {
            timeSinceLastFrame = (int)now.Subtract(_lastFrameRenderedAt).TotalMilliseconds;
        }

        _gameLogic.RenderTerrain(this);
        _gameLogic.RenderAllObjects(timeSinceLastFrame, this);
        _lastFrameRenderedAt = now;

        _sdl.RenderPresent(renderer);
    }

    public void RenderTexture(int textureId, Rectangle<int> src, Rectangle<int> dst)
    {
        if (_texturePointers.TryGetValue(textureId, out var texture))
        {
            var translatedDst = _camera.ToScreenCoordinates(dst);
            _sdl.RenderCopy((Renderer*)_renderer, (Texture*)texture, in src, in translatedDst);
        }
    }
}

public unsafe partial class GameRenderer
{
    public static int LoadTexture(string fileName, out TextureData textureData)
    {
        using var fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

        var image = Image.Load<Rgba32>(fStream);
        textureData = new TextureData()
        {
            Width = image.Width,
            Height = image.Height,
        };
        var imageRawData = new byte[textureData.Width * textureData.Height * 4];
        image.CopyPixelDataTo(imageRawData.AsSpan());
        Texture* imageTexture = null;
        fixed (byte* data = imageRawData)
        {
            var imageSurface = _instance!._sdl.CreateRGBSurfaceWithFormatFrom(data, textureData.Width,
                textureData.Height, 8,
                textureData.Width * 4, (uint)PixelFormatEnum.Rgba32);
            imageTexture = _instance._sdl.CreateTextureFromSurface((Renderer*)_instance._renderer, imageSurface);
            _instance._sdl.FreeSurface(imageSurface);
        }

        if (imageTexture == null) return -1;

        _instance._texturePointers[_instance._index] = (IntPtr)imageTexture;
        _instance._textureData[_instance._index] = textureData;
        return _instance._index++;
    }
    
    public static void SetWorldBounds(Rectangle<int> bounds)
    {
        _instance!._camera.SetWorldBounds(bounds);
    }

    public static (int X, int Y) ToWorldCoordinates(int X, int Y)
    {
        if (_instance == null)
        {
            throw new InvalidOperationException("GameRenderer instance is not initialized.");
        }

        var worldCoords = _instance._camera.ToWorldCoordinates(new(X, Y));
        return (worldCoords.X, worldCoords.Y);
    }
}