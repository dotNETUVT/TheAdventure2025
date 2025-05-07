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
    
    private static readonly Dictionary<char, int[][]> _font = new()
    {
        ['A'] = new[] {
            new[] {0,1,1,1,0},
            new[] {1,0,0,0,1},
            new[] {1,1,1,1,1},
            new[] {1,0,0,0,1},
            new[] {1,0,0,0,1},
            new[] {0,0,0,0,0},
            new[] {0,0,0,0,0}
        },
        ['E'] = new[] {
            new[] {1,1,1,1,1},
            new[] {1,0,0,0,0},
            new[] {1,1,1,0,0},
            new[] {1,0,0,0,0},
            new[] {1,1,1,1,1},
            new[] {0,0,0,0,0},
            new[] {0,0,0,0,0}
        },
        ['G'] = new[] {
            new[] {0,1,1,1,0},
            new[] {1,0,0,0,0},
            new[] {1,0,0,1,1},
            new[] {1,0,0,0,1},
            new[] {0,1,1,1,1},
            new[] {0,0,0,0,0},
            new[] {0,0,0,0,0}
        },
        ['M'] = new[] {
            new[] {1,0,0,0,1},
            new[] {1,1,0,1,1},
            new[] {1,0,1,0,1},
            new[] {1,0,0,0,1},
            new[] {1,0,0,0,1},
            new[] {0,0,0,0,0},
            new[] {0,0,0,0,0}
        },
        ['O'] = new[] {
            new[] {0,1,1,1,0},
            new[] {1,0,0,0,1},
            new[] {1,0,0,0,1},
            new[] {1,0,0,0,1},
            new[] {0,1,1,1,0},
            new[] {0,0,0,0,0},
            new[] {0,0,0,0,0}
        },
        ['R'] = new[] {
            new[] {1,1,1,1,0},
            new[] {1,0,0,0,1},
            new[] {1,1,1,1,0},
            new[] {1,0,1,0,0},
            new[] {1,0,0,1,0},
            new[] {0,0,0,0,0},
            new[] {0,0,0,0,0}
        },
        ['V'] = new[] {
            new[] {1,0,0,0,1},
            new[] {1,0,0,0,1},
            new[] {1,0,0,0,1},
            new[] {0,1,0,1,0},
            new[] {0,0,1,0,0},
            new[] {0,0,0,0,0},
            new[] {0,0,0,0,0}
        },
        [' '] = new[] {
            new[] {0,0,0,0,0},
            new[] {0,0,0,0,0},
            new[] {0,0,0,0,0},
            new[] {0,0,0,0,0},
            new[] {0,0,0,0,0},
            new[] {0,0,0,0,0},
            new[] {0,0,0,0,0}
        }
    };

    private Dictionary<int, IntPtr> _texturePointers = new();
    private Dictionary<int, TextureData> _textureData = new();
    private int _textureId;
    public int CameraWidth => _camera.Width;
    public int CameraHeight => _camera.Height;

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
    
    public void DrawPixel(int x, int y, int size, bool highlight)
    {
        byte r = highlight ? (byte)34  : (byte)255;
        byte g = highlight ? (byte)139 : (byte)255;
        byte b = highlight ? (byte)34  : (byte)255;

        SetDrawColor(r, g, b, 255);

        var rect = new Rectangle<int>(x, y, size, size);
        _sdl.RenderFillRect(_renderer, in rect);
    }

    public void DrawTextCentered(string text, bool highlight = false)
    {
        const int charWidth = 5;
        const int charHeight = 7;
        const int pixelSize = 5;
        int spacing = 1;

        int totalWidth = (charWidth + spacing) * text.Length * pixelSize;
        int startX = (CameraWidth - totalWidth) / 2;
        int startY = (CameraHeight - charHeight * pixelSize) / 2;

        foreach (char c in text.ToUpperInvariant())
        {
            if (!_font.TryGetValue(c, out var matrix))
            {
                startX += (charWidth + spacing) * pixelSize;
                continue;
            }

            for (int row = 0; row < matrix.Length; row++)
            {
                for (int col = 0; col < matrix[row].Length; col++)
                {
                    if (matrix[row][col] == 1)
                    {
                        int x = startX + col * pixelSize;
                        int y = startY + row * pixelSize;
                        DrawPixel(x, y, pixelSize, highlight);
                    }
                }
            }

            startX += (charWidth + spacing) * pixelSize;
        }
    }
    
   public void RenderTextureNoCamera(int textureId, Rectangle<int> src, Rectangle<int> dst,
       RendererFlip flip = RendererFlip.None, double angle = 0.0, Point center = default)
   {
       if (_texturePointers.TryGetValue(textureId, out var imageTexture))
       {
           _sdl.RenderCopyEx(_renderer, (Texture*)imageTexture, in src, in dst, angle, in center, flip);
       }
   }


}
