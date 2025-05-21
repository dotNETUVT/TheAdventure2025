using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheAdventure.Models;
using Point = Silk.NET.SDL.Point;

namespace TheAdventure;

public unsafe class GameRenderer
{
    private Sdl _sdl;
    private Renderer* _renderer;
    private GameWindow _window;
    private Camera _camera;

    private Dictionary<int, IntPtr> _texturePointers = new();
    private Dictionary<int, TextureData> _textureData = new();
    private int _textureId;
    private int _score;

    public GameRenderer(Sdl sdl, GameWindow window)
    {
        _sdl = sdl;

        _renderer = (Renderer*)window.CreateRenderer();
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);

        _window = window;
        var windowSize = window.Size;
        _camera = new Camera(windowSize.Width, windowSize.Height);
        _score = 0;
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
    public int GetScore()
    {
        return _score;
    }

    public void RenderTextCentered(string text, int centerX, int centerY, int fontSize)
    {

        Console.WriteLine($"Render Text: '{text}' at ({centerX}, {centerY}) with font size {fontSize}");
    }

    public void UpdateScore(int points)
    {
        _score += points;
    }

    public void RenderScoreWithoutTexture()
    {
        // Define the position and size of each digit
        int startX = 10; // Starting X position
        int startY = 10; // Starting Y position
        int digitWidth = 20; // Width of each digit
        int digitHeight = 40; // Height of each digit
        int spacing = 5; // Spacing between digits


        string scoreText = _score.ToString();

        foreach (char digit in scoreText)
        {

            int digitValue = digit - '0';
            RenderDigit(digitValue, startX, startY, digitWidth, digitHeight);

            startX += digitWidth + spacing;
        }
    }

    private void RenderDigit(int digit, int x, int y, int width, int height)
    {

        var segments = new[]
        {
        new Rectangle<int>(x, y, width, height / 5),
        new Rectangle<int>(x, y, width / 5, height / 2),
        new Rectangle<int>(x + width - width / 5, y, width / 5, height / 2),
        new Rectangle<int>(x, y + height / 2 - height / 10, width, height / 5),
        new Rectangle<int>(x, y + height / 2, width / 5, height / 2),
        new Rectangle<int>(x + width - width / 5, y + height / 2, width / 5, height / 2),
        new Rectangle<int>(x, y + height - height / 5, width, height / 5)
    };


        var digitSegments = new[]
        {
        new[] { true, true, true, false, true, true, true },
        new[] { false, false, true, false, false, true, false },
        new[] { true, false, true, true, true, false, true },
        new[] { true, false, true, true, false, true, true },
        new[] { false, true, true, true, false, true, false },
        new[] { true, true, false, true, false, true, true },
        new[] { true, true, false, true, true, true, true },
        new[] { true, false, true, false, false, true, false },
        new[] { true, true, true, true, true, true, true },
        new[] { true, true, true, true, false, true, true }
    };


        for (int i = 0; i < segments.Length; i++)
        {
            if (digitSegments[digit][i])
            {
                _sdl.SetRenderDrawColor(_renderer, 255, 255, 255, 255);
                _sdl.RenderFillRect(_renderer, in segments[i]);
            }
        }
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
}
