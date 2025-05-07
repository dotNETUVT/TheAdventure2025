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
    
    private int _gameOverTextureId = -1;
    private bool _gameOverTextureLoaded;
    private TextureData _gameOverTextureData;

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
            var imageRawData = new byte[textureInfo.Width * textureInfo.Height * 4];
            image.CopyPixelDataTo(imageRawData.AsSpan());
            fixed (byte* data = imageRawData)
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
    
    public void Clear()
    { 
        _sdl.RenderClear(_renderer);
    }
    
    public void RenderGameOver()
    {
        if (!_gameOverTextureLoaded)
        {
            try
            {
                TextureData tempTextureInfo;
                _gameOverTextureId = LoadTexture(Path.Combine("Assets", "GameOver.png"), out tempTextureInfo);
                _gameOverTextureData = tempTextureInfo;
                _gameOverTextureLoaded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load GameOver.png: {ex.Message}");
                return;
            }
        }

        SetDrawColor(0, 0, 0, 255);
        ClearScreen();
        
        var windowSize = _window.Size;
        
        var sourceRect = new Rectangle<int>(0, 0, _gameOverTextureData.Width, _gameOverTextureData.Height);
        
        int displayWidth = Math.Min(windowSize.Width, _gameOverTextureData.Width);
        int displayHeight = Math.Min(windowSize.Height, _gameOverTextureData.Height);
        
        var destRect = new Rectangle<int>(
            (windowSize.Width - displayWidth) / 2,
            (windowSize.Height - displayHeight) / 2,
            displayWidth,
            displayHeight
        );
        
        if (_texturePointers.TryGetValue(_gameOverTextureId, out var imageTexture))
        {
            Point centerPoint = default;
            
            _sdl.RenderCopyEx(_renderer, (Texture*)imageTexture, in sourceRect,
                in destRect, 0.0, in centerPoint, RendererFlip.None);
        }
    }
}