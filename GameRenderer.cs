using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing; // For mutate
using SixLabors.ImageSharp.Drawing.Processing; // Enables DrawText, Fill, etc.
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Fonts;
using TheAdventure.Models;
using Point = Silk.NET.SDL.Point;

namespace TheAdventure;

public unsafe class GameRenderer
{
    private Sdl _sdl;
    private Renderer* _renderer;
    private GameWindow _window;
    private GameCamera _camera;

    private Dictionary<int, IntPtr> _texturePointers = new();
    private Dictionary<int, TextureData> _textureData = new();
    private int _textureId;

    public GameRenderer(Sdl sdl, GameWindow window)
    {
        _sdl = sdl;
        
        _renderer = (Renderer*)window.CreateRenderer();
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);
        
        _window = window;
        var windowSize = window.Size;
        _camera = new GameCamera(windowSize.Width, windowSize.Height);
    }

    public void SetWorldBounds(Rectangle<int> bounds)
    {
        _camera.SetWorldBounds(bounds);
    }

    public void CameraLookAt(int x, int y)
    {
        _camera.LookAt(x, y);
    }

    public int LoadTexture(string fileName, out TextureData textureInfo)
    {
        using (var fStream = new FileStream(fileName, FileMode.Open))
        {
            var image = Image.Load<Rgba32>(fStream);
            textureInfo = new TextureData()
            {
                Width = image.Width,
                Height = image.Height
            };
            var imageRAWData = new byte[textureInfo.Width * textureInfo.Height * 4];
            image.CopyPixelDataTo(imageRAWData.AsSpan());
            fixed (byte* data = imageRAWData)
            {
                var imageSurface = _sdl.CreateRGBSurfaceWithFormatFrom(data, textureInfo.Width,
                    textureInfo.Height, 8, textureInfo.Width * 4, (uint)PixelFormatEnum.Rgba32);
                if (imageSurface == null)
                {
                    throw new Exception("Failed to create surface from image data.");
                }
                
                var imageTexture = _sdl.CreateTextureFromSurface(_renderer, imageSurface);
                if (imageTexture == null)
                {
                    _sdl.FreeSurface(imageSurface);
                    throw new Exception("Failed to create texture from surface.");
                }
                
                _sdl.FreeSurface(imageSurface);
                
                _textureData[_textureId] = textureInfo;
                _texturePointers[_textureId] = (IntPtr)imageTexture;
            }
        }

        return _textureId++;
    }

    public void RenderTexture(int textureId, Rectangle<int> src, Rectangle<int> dst,
        RendererFlip flip = RendererFlip.None, double angle = 0.0, Point center = default)
    {
        if (_texturePointers.TryGetValue(textureId, out var imageTexture))
        {
            var translatedDst = _camera.ToScreenCoordinates(dst);
            _sdl.RenderCopyEx(_renderer, (Texture*)imageTexture, in src,
                in translatedDst,
                angle,
                in center, flip);
        }
    }

    public Vector2D<int> ToWorldCoordinates(int x, int y)
    {
        return _camera.ToWorldCoordinates(new Vector2D<int>(x, y));
    }

    public void SetDrawColor(byte r, byte g, byte b, byte a)
    {
        _sdl.SetRenderDrawColor(_renderer, r, g, b, a);
    }

    public void ClearScreen()
    {
        _sdl.RenderClear(_renderer);
    }

    public void PresentFrame()
    {
        _sdl.RenderPresent(_renderer);
    }

    // Method to dynamically render text
    public IntPtr RenderTextAsTexture(string text, int fontSize = 24, string fontName = "Arial", int width = 400, int height = 50)
    {
    var font = SystemFonts.CreateFont(fontName, fontSize, FontStyle.Regular);

    using var image = new Image<Rgba32>(width, height);
    image.Mutate(ctx =>
    {
        ctx.Fill(SixLabors.ImageSharp.Color.Transparent);
        ctx.DrawText(text, font, SixLabors.ImageSharp.Color.White, new SixLabors.ImageSharp.PointF(5, 5));
    });

    var pixelData = new byte[width * height * 4];
    image.CopyPixelDataTo(pixelData);

    fixed (byte* data = pixelData)
    {
        var surface = _sdl.CreateRGBSurfaceWithFormatFrom(
            data, width, height, 32, width * 4, (uint)PixelFormatEnum.Rgba32);

        if (surface == null)
            throw new Exception("Failed to create surface from rendered text.");

        var texture = _sdl.CreateTextureFromSurface(_renderer, surface);
        _sdl.FreeSurface(surface);

        return (IntPtr)texture;
    }
    }

    // Method to render the stats of the player
    public void RenderStats(PlayerObject player)
    {
        string stats = $"Health: {player.Health}  Score: {player.Score}  Bombs: {player.BombsPlaced}";
        var texture = RenderTextAsTexture(stats);
        var dst = new Rectangle<int>(10, 10, 400, 50);
        _sdl.RenderCopy(_renderer, (Texture*)texture, null, &dst);
    }
}
