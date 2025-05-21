using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheAdventure.Models;
using Point = Silk.NET.SDL.Point;

#if WINDOWS || LINUX || OSX
using SDL2;
#endif

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

    public GameRenderer(Sdl sdl, GameWindow window)
    {
        _sdl = sdl;

        _renderer = (Renderer*)window.CreateRenderer();
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);

        _window = window;
        var windowSize = window.Size;
        _camera = new Camera(windowSize.Width, windowSize.Height);
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

    public void DrawPoint(int x, int y)
    {
        // Calculate screen coordinates from world coordinates
        int screenX = x - _camera.X + _camera.Width / 2;
        int screenY = y - _camera.Y + _camera.Height / 2;

        _sdl.RenderDrawPoint(_renderer, screenX, screenY);
    }

    public void DrawUIPoint(int x, int y)
    {
        // Draw a point directly to the screen (no camera translation)
        _sdl.RenderDrawPoint(_renderer, x, y);
    }

    // Use RenderAsciiText for all UI/game text. This draws a readable block font (10x16 per char).
    public void RenderAsciiText(string text, int x, int y, byte r, byte g, byte b)
    {
        int charWidth = 10;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            int charX = x + i * (charWidth + 2);
            DrawAsciiChar(c, charX, y, r, g, b);
        }
    }

    // Draws a single ASCII character in a readable block font (10x16)
    private void DrawAsciiChar(char c, int x, int y, byte r, byte g, byte b)
    {
        SetDrawColor(r, g, b, 255);
        int w = 10, h = 16;
        c = char.ToUpperInvariant(c);
        switch (c)
        {
            case 'A':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if ((py == 0 && px > 1 && px < w - 2) || // top bar
                            (py == h / 2 && px > 1 && px < w - 2) || // mid bar
                            (px == 1 && py > 0) || (px == w - 2 && py > 0)) // sides
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'B':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (px == 1 || // left
                            (py == 0 && px < w - 2) || // top
                            (py == h / 2 && px < w - 2) || // mid
                            (py == h - 1 && px < w - 2) || // bottom
                            (px == w - 2 && ((py > 0 && py < h / 2) || (py > h / 2 && py < h - 1))))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'C':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if ((px == 1 && py > 0 && py < h - 1) ||
                            (py == 0 && px > 1) || (py == h - 1 && px > 1))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'D':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (px == 1 ||
                            (py == 0 && px < w - 2) || (py == h - 1 && px < w - 2) ||
                            (px == w - 2 && py > 0 && py < h - 1))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'E':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (px == 1 || py == 0 || py == h - 1 || (py == h / 2 && px < w - 1))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'F':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (px == 1 || py == 0 || (py == h / 2 && px < w - 1))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'G':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if ((px == 1 && py > 0 && py < h - 1) ||
                            (py == 0 && px > 1) || (py == h - 1 && px > 1) ||
                            (py == h / 2 && px > w / 2) ||
                            (px == w - 2 && py > h / 2 && py < h - 1))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'H':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (px == 1 || px == w - 2 || (py == h / 2 && px > 1 && px < w - 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'I':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (py == 0 || py == h - 1 || px == w / 2)
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'J':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if ((py == 0 && px > 1 && px < w - 2) ||
                            (px == w / 2 && py < h - 1) ||
                            (py == h - 1 && px > 1 && px < w / 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'K':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (px == 1 || (px + py == w - 2 && py < h / 2) || (px - py == 1 - h / 2 && py >= h / 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'L':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (px == 1 || (py == h - 1 && px < w - 1))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'M':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (px == 1 || px == w - 2 || (py == px && py < h / 2) || (py == w - 1 - px && py < h / 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'N':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (px == 1 || px == w - 2 || (px == py && py > 0 && py < h - 1))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'O':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (((px == 1 || px == w - 2) && py > 0 && py < h - 1) ||
                            ((py == 0 || py == h - 1) && px > 1 && px < w - 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'P':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (px == 1 || (py == 0 && px < w - 2) || (py == h / 2 && px < w - 2) || (px == w - 2 && py > 0 && py < h / 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'Q':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (((px == 1 || px == w - 2) && py > 0 && py < h - 2) ||
                            ((py == 0 || py == h - 2) && px > 1 && px < w - 2) ||
                            (px == py && py > h / 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'R':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (px == 1 || (py == 0 && px < w - 2) || (py == h / 2 && px < w - 2) || (px == w - 2 && py > 0 && py < h / 2) || (px - py == 1 - h / 2 && py >= h / 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'S':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if ((py == 0 && px > 1) || (py == h / 2 && px > 1 && px < w - 2) || (py == h - 1 && px < w - 2) || (px == 1 && py > 0 && py < h / 2) || (px == w - 2 && py > h / 2 && py < h - 1))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'T':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (py == 0 || px == w / 2)
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'U':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if ((px == 1 && py < h - 1) || (px == w - 2 && py < h - 1) || (py == h - 1 && px > 1 && px < w - 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'V':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if ((px == 1 && py < h - 2) || (px == w - 2 && py < h - 2) || (py - px == h - w && px > 1 && px < w / 2) || (py + px == w - 1 + h - 2 && px > w / 2 && px < w - 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'W':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (px == 1 || px == w - 2 || (py == px && py > h / 2) || (py == w - 1 - px && py > h / 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'X':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (px == py || px == w - 1 - py)
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'Y':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if ((px == py && py < h / 2) || (px == w - 1 - py && py < h / 2) || (px == w / 2 && py >= h / 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case 'Z':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (py == 0 || py == h - 1 || px == w - 1 - py)
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            // Digits 0-9
            case '0':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (((px == 1 || px == w - 2) && py > 0 && py < h - 1) || ((py == 0 || py == h - 1) && px > 1 && px < w - 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case '1':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (px == w / 2 || (py == h - 1 && px > 1 && px < w - 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case '2':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if ((py == 0 && px > 1 && px < w - 2) || (py == h / 2 && px > 1 && px < w - 2) || (py == h - 1 && px > 1 && px < w - 2) || (px == w - 2 && py > 0 && py < h / 2) || (px == 1 && py > h / 2 && py < h - 1))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case '3':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if ((py == 0 && px > 1 && px < w - 2) || (py == h / 2 && px > 1 && px < w - 2) || (py == h - 1 && px > 1 && px < w - 2) || (px == w - 2 && py > 0 && py < h - 1))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case '4':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if ((px == 1 && py < h / 2) || (px == w - 2 && py > 0 && py < h - 1) || (py == h / 2 && px > 1 && px < w - 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case '5':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if ((py == 0 && px > 1 && px < w - 2) || (py == h / 2 && px > 1 && px < w - 2) || (py == h - 1 && px > 1 && px < w - 2) || (px == 1 && py > 0 && py < h / 2) || (px == w - 2 && py > h / 2 && py < h - 1))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case '6':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if ((px == 1 && py > 0 && py < h - 1) || (py == 0 && px > 1 && px < w - 2) || (py == h / 2 && px > 1 && px < w - 2) || (py == h - 1 && px > 1 && px < w - 2) || (px == w - 2 && py > h / 2 && py < h - 1))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case '7':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (py == 0 || (px == w - 2 && py > 0 && py < h - 1))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case '8':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (((px == 1 || px == w - 2) && py > 0 && py < h - 1) || ((py == 0 || py == h - 1) && px > 1 && px < w - 2) || (py == h / 2 && px > 1 && px < w - 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case '9':
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if ((px == w - 2 && py > 0 && py < h - 1) || (py == 0 && px > 1 && px < w - 2) || (py == h / 2 && px > 1 && px < w - 2) || (py == h - 1 && px > 1 && px < w - 2) || (px == 1 && py > 0 && py < h / 2))
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case ':':
                for (int py = 4; py < 6; py++)
                    for (int px = 4; px < 6; px++)
                        _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                for (int py = 10; py < 12; py++)
                    for (int px = 4; px < 6; px++)
                        _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case '!':
                for (int py = 2; py < 10; py++)
                    for (int px = 4; px < 6; px++)
                        _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                for (int py = 12; py < 14; py++)
                    for (int px = 4; px < 6; px++)
                        _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case '-':
                for (int px = 2; px < w - 2; px++)
                    _sdl.RenderDrawPoint(_renderer, x + px, y + h / 2);
                break;
            case '.':
                for (int py = h - 3; py < h - 1; py++)
                    for (int px = 4; px < 6; px++)
                        _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
            case ' ':
                break;
            default:
                // Draw a rectangle for truly unknown chars
                for (int py = 0; py < h; py++)
                    for (int px = 0; px < w; px++)
                        if (py == 0 || py == h - 1 || px == 0 || px == w - 1)
                            _sdl.RenderDrawPoint(_renderer, x + px, y + py);
                break;
        }
    }

    public int GetScreenWidth()
    {
        return _camera.Width;
    }

    public int GetScreenHeight()
    {
        return _camera.Height;
    }
}
