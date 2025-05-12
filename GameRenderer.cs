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
    private int _oreIconId;


    private Dictionary<int, IntPtr> _texturePointers = new();
    private Dictionary<int, TextureData> _textureData = new();
    private int _textureId;

    private readonly Dictionary<string, IntPtr> _fontCache = new();
    
    
    public GameRenderer(Sdl sdl, GameWindow window)
    {
        _sdl = sdl;
        
        _renderer = (Renderer*)window.CreateRenderer();
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);
        
        _window = window;
        var windowSize = window.Size;
        _camera = new Camera(windowSize.Width, windowSize.Height);
        
        _oreIconId = LoadTexture("Assets/copper_ore.png", out _);

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
    
    public unsafe void RenderText(string text, int x, int y, byte r = 255, byte g = 255, byte b = 255)
{
    if (_textureData.TryGetValue(_oreIconId, out var textureInfo))
    {
        var iconSize = 16; 
        var iconSrc = new Rectangle<int>(0, 0, textureInfo.Width, textureInfo.Height);
        var iconDst = new Rectangle<int>(x, y, iconSize, iconSize);
        RenderTexture(_oreIconId, iconSrc, iconDst);
        
    
        x += iconSize + 4; 
    }

    const int CHAR_WIDTH = 8;
    const int CHAR_HEIGHT = 16;
    const int THICKNESS = 2;

    _sdl.SetRenderDrawColor(_renderer, r, g, b, 255);

    int currentX = x;
    foreach (char c in text)
    {
        if (char.IsDigit(c))
        {
            var segments = GetDigitSegments(c);
            foreach (var segment in segments)
            {
                var rect = new Rectangle<int>(
                    currentX + segment.X * THICKNESS, 
                    y + segment.Y * THICKNESS, 
                    segment.Width * THICKNESS, 
                    segment.Height * THICKNESS
                );
                _sdl.RenderFillRect(_renderer, &rect);
            }
        }
        else if (c == ':' || c == ' ')
        {
            if (c == ':')
            {
                var dot1 = new Rectangle<int>(currentX + 3, y + 5, 2, 2);
                var dot2 = new Rectangle<int>(currentX + 3, y + 9, 2, 2);
                _sdl.RenderFillRect(_renderer, &dot1);
                _sdl.RenderFillRect(_renderer, &dot2);
            }
        }
        currentX += CHAR_WIDTH;
    }
}

private struct Segment
{
    public int X, Y, Width, Height;
    public Segment(int x, int y, int w, int h)
    {
        X = x; Y = y; Width = w; Height = h;
    }
}

private Segment[] GetDigitSegments(char digit)
{
    return digit switch
    {
        '0' => new[] {
            new Segment(0, 0, 4, 1),  // top
            new Segment(0, 0, 1, 8),  // left
            new Segment(3, 0, 1, 8),  // right
            new Segment(0, 7, 4, 1)   // bottom
        },
        '1' => new[] {
            new Segment(3, 0, 1, 8)   // right
        },
        '2' => new[] {
            new Segment(0, 0, 4, 1),  // top
            new Segment(3, 0, 1, 4),  // right
            new Segment(0, 3, 4, 1),  // middle
            new Segment(0, 4, 1, 4),  // left
            new Segment(0, 7, 4, 1)   // bottom
        },
        '3' => new[] {
            new Segment(0, 0, 4, 1),  // top
            new Segment(3, 0, 1, 8),  // right
            new Segment(0, 3, 4, 1),  // middle
            new Segment(0, 7, 4, 1)   // bottom
        },
        '4' => new[] {
            new Segment(0, 0, 1, 4),  // left top
            new Segment(3, 0, 1, 8),  // right
            new Segment(0, 3, 4, 1)   // middle
        },
        '5' => new[] {
            new Segment(0, 0, 4, 1),  // top
            new Segment(0, 0, 1, 4),  // left
            new Segment(0, 3, 4, 1),  // middle
            new Segment(3, 4, 1, 4),  // right
            new Segment(0, 7, 4, 1)   // bottom
        },
        '6' => new[] {
            new Segment(0, 0, 4, 1),  // top
            new Segment(0, 0, 1, 8),  // left
            new Segment(0, 3, 4, 1),  // middle
            new Segment(3, 4, 1, 4),  // right
            new Segment(0, 7, 4, 1)   // bottom
        },
        '7' => new[] {
            new Segment(0, 0, 4, 1),  // top
            new Segment(3, 0, 1, 8)   // right
        },
        '8' => new[] {
            new Segment(0, 0, 4, 1),  // top
            new Segment(0, 0, 1, 8),  // left
            new Segment(3, 0, 1, 8),  // right
            new Segment(0, 3, 4, 1),  // middle
            new Segment(0, 7, 4, 1)   // bottom
        },
        '9' => new[] {
            new Segment(0, 0, 4, 1),  // top
            new Segment(0, 0, 1, 4),  // left
            new Segment(3, 0, 1, 8),  // right
            new Segment(0, 3, 4, 1),  // middle
            new Segment(0, 7, 4, 1)   // bottom
        },
        _ => new[] { new Segment(0, 0, 4, 1) }  // default
    };

}
}
