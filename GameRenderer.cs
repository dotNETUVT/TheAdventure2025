using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TheAdventure;

public unsafe class GameRenderer
{
    public readonly struct TextureInfo
    {
        public int Width { get; init; }
        public int Height { get; init; }

        public int PixelDataSize => Width * Height * 4;
    }

    private readonly Sdl _sdl;
    private readonly IntPtr _renderer;
    private readonly GameLogic _gameLogic;

    private readonly Dictionary<int, IntPtr> _texturePointers;
    private readonly Dictionary<int, TextureInfo> _textureInformation;
    private int _index = 0;

    public GameRenderer(Sdl sdl, GameWindow gameWindow, GameLogic gameLogic)
    {
        _sdl = sdl;
        _renderer = gameWindow.CreateRenderer();
        _gameLogic = gameLogic;

        _textureInformation = new();
        _texturePointers = new();
    }

    public void Render()
    {
        var renderer = (Renderer*)_renderer;

        _sdl.RenderClear(renderer);
        foreach (var renderable in _gameLogic.GetRenderables())
        {
            if (renderable.TextureId > -1 &&
                _texturePointers.TryGetValue(renderable.TextureId, out var texturePointer))
            {
                _sdl.RenderCopyEx(renderer, (Texture*)texturePointer, renderable.TextureSource,
                    renderable.TextureDestination, 0, new Silk.NET.SDL.Point(0, 0), RendererFlip.None);
            }
        }

        _sdl.RenderPresent(renderer);
    }

    public int LoadTexture(string fileName, out TextureInfo textureInfo)
    {
        using var fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

        var image = Image.Load<Rgba32>(fStream);
        textureInfo = new TextureInfo()
        {
            Width = image.Width,
            Height = image.Height,
        };
        var imageRawData = new byte[textureInfo.PixelDataSize];
        image.CopyPixelDataTo(imageRawData.AsSpan());
        Texture* imageTexture = null;
        fixed (byte* data = imageRawData)
        {
            var imageSurface = _sdl.CreateRGBSurfaceWithFormatFrom(data, textureInfo.Width, textureInfo.Height, 8,
                textureInfo.Width * 4, (uint)PixelFormatEnum.Rgba32);
            imageTexture = _sdl.CreateTextureFromSurface((Renderer*)_renderer, imageSurface);
            _sdl.FreeSurface(imageSurface);
        }

        if (imageTexture == null) return -1;

        _texturePointers[_index] = (IntPtr)imageTexture;
        _textureInformation[_index] = textureInfo;
        return _index++;
    }
}