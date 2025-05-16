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

    public void RenderText(string text, int x, int y, byte r = 255, byte g = 255, byte b = 255, byte a = 255)
    {
        var surface = _sdl.CreateRGBSurfaceWithFormat(0, 1, 1, 32, (uint)PixelFormatEnum.Rgba32);
        
        unsafe
        {
            // Create a solid colored surface for the text
            var tempSurface = _sdl.CreateRGBSurface(0, 200, 50, 32, 
                0x000000FF,  // Rmask
                0x0000FF00,  // Gmask
                0x00FF0000,  // Bmask
                0xFF000000); // Amask
            if (tempSurface == null)
            {
                _sdl.FreeSurface(surface);
                return;
            }

            // Set the color key for transparency
            _sdl.SetSurfaceColorMod(tempSurface, r, g, b);
            _sdl.SetSurfaceAlphaMod(tempSurface, a);

            // Convert text into a basic surface - This is a simplified approach
            // In a real game, you would want to use SDL_ttf for proper text rendering
            var width = text.Length * 8; // Approximate width based on character count
            var height = 16; // Fixed height for now
            var dstRect = new Rectangle<int>(0, 0, width, height);
            
            // Create texture from surface
            var texture = _sdl.CreateTextureFromSurface(_renderer, tempSurface);
            if (texture == null)
            {
                _sdl.FreeSurface(tempSurface);
                _sdl.FreeSurface(surface);
                return;
            }

            // Set blend mode for transparency
            _sdl.SetTextureBlendMode(texture, BlendMode.Blend);
            
            // Render the texture
            var renderRect = new Rectangle<int>(x, y, width, height);
            _sdl.RenderCopy(_renderer, texture, null, in renderRect);

            // Cleanup
            _sdl.DestroyTexture(texture);
            _sdl.FreeSurface(tempSurface);
            _sdl.FreeSurface(surface);
        }
    }

    public void DrawFilledRectangle(int x, int y, int width, int height, byte r, byte g, byte b, byte a)
    {
        var rect = new Rectangle<int>(x, y, width, height);
        SetDrawColor(r, g, b, a);
        _sdl.RenderFillRect(_renderer, &rect);
    }

    public void DrawRectangleOutline(int x, int y, int width, int height, byte r, byte g, byte b, byte a)
    {
        var rect = new Rectangle<int>(x, y, width, height);
        SetDrawColor(r, g, b, a);
        _sdl.RenderDrawRect(_renderer, &rect);
    }
}
