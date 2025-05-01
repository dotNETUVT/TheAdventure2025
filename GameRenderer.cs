using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using TheAdventure.Models;
using Point = Silk.NET.SDL.Point;
using System.IO;

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
    
    private FontFamily _fontFamily;
    private Font _font;

    public GameRenderer(Sdl sdl, GameWindow window)
    {
        _sdl = sdl;
        
        _renderer = (Renderer*)window.CreateRenderer();
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);
        
        _window = window;
        var windowSize = window.Size;
        _camera = new Camera(windowSize.Width, windowSize.Height);
        
        var fontCollection = new FontCollection();
        _fontFamily = fontCollection.Add(Path.Combine("Assets", "ARIAL.TTF"));
        _font = _fontFamily.CreateFont(24, FontStyle.Regular);
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
    
    public void RenderRectangle(Rectangle<int> rect, byte r, byte g, byte b, byte a)
    {
        byte originalR, originalG, originalB, originalA;
        _sdl.GetRenderDrawColor(_renderer, &originalR, &originalG, &originalB, &originalA);
        
        _sdl.SetRenderDrawColor(_renderer, r, g, b, a);
        
        var translatedRect = _camera.ToScreenCoordinates(rect);
        
        _sdl.RenderFillRect(_renderer, &translatedRect);
        
        _sdl.SetRenderDrawColor(_renderer, originalR, originalG, originalB, originalA);
    }
    
    public void RenderRectangleOutline(Rectangle<int> rect, byte r, byte g, byte b, byte a)
    {
        byte originalR, originalG, originalB, originalA;
        _sdl.GetRenderDrawColor(_renderer, &originalR, &originalG, &originalB, &originalA);
        
        _sdl.SetRenderDrawColor(_renderer, r, g, b, a);
        
        var translatedRect = _camera.ToScreenCoordinates(rect);
        
        _sdl.RenderDrawRect(_renderer, &translatedRect);
        
        _sdl.SetRenderDrawColor(_renderer, originalR, originalG, originalB, originalA);
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

    public void RenderText(string text, int x, int y, byte r, byte g, byte b, bool isScreenCoordinates = false)
    {
        int padding = 2;
        var textSize = TextMeasurer.MeasureSize(text, new TextOptions(_font));
        int width = (int)textSize.Width + padding * 2;
        int height = (int)textSize.Height + padding * 2;
        
        using var image = new Image<Rgba32>(width, height);
        
        image.Mutate(ctx => ctx.DrawText(
            text,
            _font,
            SixLabors.ImageSharp.Color.FromRgb(r, g, b),
            new SixLabors.ImageSharp.PointF(padding, padding)
        ));
        
        var imageData = new byte[width * height * 4];
        image.CopyPixelDataTo(imageData.AsSpan());
        
        fixed (byte* data = imageData)
        {
            var surface = _sdl.CreateRGBSurfaceWithFormatFrom(
                data, width, height, 8, width * 4, (uint)PixelFormatEnum.Rgba32);
            
            if (surface == null)
            {
                throw new Exception("Failed to create surface for text rendering");
            }
            
            var texture = _sdl.CreateTextureFromSurface(_renderer, surface);
            if (texture == null)
            {
                _sdl.FreeSurface(surface);
                throw new Exception("Failed to create texture for text rendering");
            }
            
            _sdl.SetTextureBlendMode(texture, BlendMode.Blend);
            
            var srcRect = new Rectangle<int>(0, 0, width, height);
            var destRect = new Rectangle<int>(x, y, width, height);
            
            if (!isScreenCoordinates)
            {
                destRect = _camera.ToScreenCoordinates(destRect);
            }
            
            _sdl.RenderCopy(_renderer, texture, in srcRect, in destRect);
            
            _sdl.DestroyTexture(texture);
            _sdl.FreeSurface(surface);
        }
    }
    
    public void RenderUIText(string text, int x, int y, byte r, byte g, byte b)
    {
        RenderText(text, x, y, r, g, b, true);
    }
    
    public void RenderDirectionalArrow((int X, int Y) targetWorldPosition, byte r, byte g, byte b, byte a = 255)
    {
        var windowSize = _window.Size;
        int screenWidth = windowSize.Width;
        int screenHeight = windowSize.Height;
        
        var screenRect = new Rectangle<int>(targetWorldPosition.X, targetWorldPosition.Y, 1, 1);
        var translatedRect = _camera.ToScreenCoordinates(screenRect);
        int screenX = translatedRect.Origin.X;
        int screenY = translatedRect.Origin.Y;
        
        if (screenX >= 0 && screenX <= screenWidth && screenY >= 0 && screenY <= screenHeight)
        {
            return;
        }
        
        int centerX = screenWidth / 2;
        int centerY = screenHeight / 2;
        float dx = screenX - centerX;
        float dy = screenY - centerY;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        float ndx = dx / length;
        float ndy = dy / length;
        
        int arrowX, arrowY;
        int padding = 40;
        
        if (MathF.Abs(ndx) > MathF.Abs(ndy))
        {
            arrowX = ndx > 0 ? screenWidth - padding : padding;
            arrowY = centerY + (int)(ndy * (arrowX - centerX) / ndx);
            
            if (arrowY < padding) arrowY = padding;
            if (arrowY > screenHeight - padding) arrowY = screenHeight - padding;
        }
        else
        {
            arrowY = ndy > 0 ? screenHeight - padding : padding;
            arrowX = centerX + (int)(ndx * (arrowY - centerY) / ndy);
            
            if (arrowX < padding) arrowX = padding;
            if (arrowX > screenWidth - padding) arrowX = screenWidth - padding;
        }
        
        int arrowSize = 10;
        float angle = MathF.Atan2(ndy, ndx);
        
        var points = new[]
        {
            new SixLabors.ImageSharp.PointF(arrowX + arrowSize * MathF.Cos(angle), arrowY + arrowSize * MathF.Sin(angle)),
            new SixLabors.ImageSharp.PointF(arrowX + arrowSize * MathF.Cos(angle + 2.5f), arrowY + arrowSize * MathF.Sin(angle + 2.5f)),
            new SixLabors.ImageSharp.PointF(arrowX + arrowSize * MathF.Cos(angle - 2.5f), arrowY + arrowSize * MathF.Sin(angle - 2.5f))
        };
        
        int width = 30;
        int height = 30;
        using var image = new Image<Rgba32>(width, height);
        
        image.Mutate(ctx => ctx.FillPolygon(
            SixLabors.ImageSharp.Color.FromRgba(r, g, b, a),
            points.Select(p => new SixLabors.ImageSharp.PointF(p.X - (arrowX - width/2), p.Y - (arrowY - height/2))).ToArray()
        ));
        
        var imageData = new byte[width * height * 4];
        image.CopyPixelDataTo(imageData.AsSpan());
        
        fixed (byte* data = imageData)
        {
            var surface = _sdl.CreateRGBSurfaceWithFormatFrom(
                data, width, height, 8, width * 4, (uint)PixelFormatEnum.Rgba32);
            
            if (surface == null)
            {
                throw new Exception("Failed to create surface for arrow rendering");
            }
            
            var texture = _sdl.CreateTextureFromSurface(_renderer, surface);
            if (texture == null)
            {
                _sdl.FreeSurface(surface);
                throw new Exception("Failed to create texture for arrow rendering");
            }
            
            _sdl.SetTextureBlendMode(texture, BlendMode.Blend);
            
            var srcRect = new Rectangle<int>(0, 0, width, height);
            var destRect = new Rectangle<int>(arrowX - width/2, arrowY - height/2, width, height);
            
            _sdl.RenderCopy(_renderer, texture, in srcRect, in destRect);
            
            _sdl.DestroyTexture(texture);
            _sdl.FreeSurface(surface);
        }
    }

    public (int Width, int Height) GetWindowSize()
    {
        return _window.Size;
    }
}
