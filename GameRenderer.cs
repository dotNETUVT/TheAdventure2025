using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheAdventure.Models;
using Point = Silk.NET.SDL.Point;
using SDL_Rect = Silk.NET.SDL.FRect;

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

    // Mini-map constants
    private const int MiniMapWidth = 150;
    private const int MiniMapHeight = 150;
    private const int MiniMapMargin = 10;

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

    public void RenderMiniMap(List<GameObject> gameObjects, GameObject player, int worldWidth, int worldHeight, float zoom)
    {
        int miniMapX = _window.Size.Width - MiniMapWidth - MiniMapMargin;
        int miniMapY = MiniMapMargin;

        DrawFilledRect(miniMapX, miniMapY, MiniMapWidth, MiniMapHeight, 80, 80, 80, 180);

        foreach (var obj in gameObjects)
        {
            if (obj is not RenderableGameObject renderable) continue;

            float relX = renderable.Position.X / (float)worldWidth;
            float relY = renderable.Position.Y / (float)worldHeight;

            relX /= zoom;
            relY /= zoom;

            int dotX = miniMapX + (int)(relX * MiniMapWidth);
            int dotY = miniMapY + (int)(relY * MiniMapHeight);

            byte r = 255, g = 255, b = 255;

            if (obj == player)
            {
                r = 0; g = 255; b = 0; // Green
            }
            else if (obj.GetType().Name.Contains("Enemy"))
            {
                r = 255; g = 0; b = 0; // Red
            }
            else if (obj is TemporaryGameObject)
            {
                r = 255; g = 255; b = 0; // Yellow
            }
            else if (obj.GetType().Name.Contains("Npc"))
            {
                r = 0; g = 0; b = 255; // Blue
            }

            DrawPixel(dotX, dotY, r, g, b);
        }
    }

    private void DrawFilledRect(int x, int y, int w, int h, byte r, byte g, byte b, byte a)
    {
        SetDrawColor(r, g, b, a);
        SDL_Rect rect = new SDL_Rect { X = x, Y = y, W = w, H = h };
        _sdl.RenderFillRect(_renderer, (Rectangle<int>*)&rect);
    }

    private void DrawPixel(int x, int y, byte r, byte g, byte b, byte a = 255)
    {
        SetDrawColor(r, g, b, a);
        _sdl.RenderDrawPoint(_renderer, x, y);
    }
}
