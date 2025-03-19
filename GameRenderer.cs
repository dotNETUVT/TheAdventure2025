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

    private static GameRenderer? _instance;
    private DateTimeOffset _lastFrameRenderedAt = DateTimeOffset.MinValue;

    public GameRenderer(Sdl sdl, GameWindow gameWindow, GameLogic gameLogic)
    {
        _sdl = sdl;
        _renderer = gameWindow.CreateRenderer();
        _gameLogic = gameLogic;

        _textureData = new();
        _texturePointers = new();

        _instance = this;
    }

    public void RenderGameObject(RenderableGameObject gameObject)
    {
        if (_texturePointers.TryGetValue(gameObject.TextureId, out var imageTexture))
        {
            var textureSrc = gameObject.TextureSource;
            var textureDest = gameObject.TextureDestination;
            var rotCenter = gameObject.TextureRotationCenter;
            _sdl.RenderCopyEx((Renderer*)_renderer, (Texture*)imageTexture, in textureSrc, in textureDest,
                gameObject.TextureRotation, in rotCenter, RendererFlip.None);
        }
    }

    public void Render()
    {
        var renderer = (Renderer*)_renderer;

        _sdl.RenderClear(renderer);

        var timeSinceLastFrame = 0;
        var now = DateTimeOffset.UtcNow;
        if (_lastFrameRenderedAt > DateTimeOffset.MinValue)
        {
            timeSinceLastFrame = (int)now.Subtract(_lastFrameRenderedAt).TotalMilliseconds;
        }

        _gameLogic.RenderAllObjects(timeSinceLastFrame, this);
        _lastFrameRenderedAt = now;

        _sdl.RenderPresent(renderer);
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
}