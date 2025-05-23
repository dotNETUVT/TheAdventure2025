using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TheAdventure.Models;
using Point = Silk.NET.SDL.Point;
using ImgColor = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;

namespace TheAdventure;

public unsafe class GameRenderer
{
    private readonly Sdl _sdl;
    private readonly Renderer* _renderer;
    private readonly GameWindow _window;
    private readonly Camera _camera;

    private readonly Dictionary<int, IntPtr> _texturePointers = new();
    private readonly Dictionary<int, TextureData> _textureData = new();
    private int _textureId;

    private readonly Font _font;
    private readonly Font _largeFont;

    private DateTime _startTime;
    private bool _timerRunning = true;
    private int _frozenSeconds;
    private readonly int _timerPosX = 650, _timerPosY = 340;

    private int _playX, _playY, _playW, _playH;

    public GameRenderer(Sdl sdl, GameWindow window)
    {
        _sdl = sdl;
        _renderer = (Renderer*)window.CreateRenderer();
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);

        _window = window;
        _camera = new Camera(window.Size.Width, window.Size.Height);

        var fonts = new FontCollection();
        var family = fonts.Add(Path.Combine("Assets", "ARCADECLASSIC.TTF"));
        _font = family.CreateFont(50);
        _largeFont = family.CreateFont(80);

        _startTime = DateTime.Now;
    }

    public bool IsPlayButtonClicked(int x, int y) =>
        !_timerRunning &&
        x >= _playX && x <= _playX + _playW &&
        y >= _playY && y <= _playY + _playH;

    public void StopTimer()
    {
        _frozenSeconds = (int)(DateTime.Now - _startTime).TotalSeconds;
        _timerRunning = false;
    }

    public void ResetTimer()
    {
        _startTime = DateTime.Now;
        _timerRunning = true;
        _frozenSeconds = 0;
    }

    public void SetWorldBounds(Rectangle<int> bounds) => _camera.SetWorldBounds(bounds);
    public void CameraLookAt(int x, int y) => _camera.LookAt(x, y);
    public Vector2D<int> ToWorldCoordinates(int x, int y)
        => _camera.ToWorldCoordinates(new Vector2D<int>(x, y));
    public void SetDrawColor(byte r, byte g, byte b, byte a)
        => _sdl.SetRenderDrawColor(_renderer, r, g, b, a);
    public void ClearScreen() => _sdl.RenderClear(_renderer);

    public int LoadTexture(string fileName, out TextureData texInfo)
    {
        using var fs = new FileStream(fileName, FileMode.Open);
        var img = Image.Load<Rgba32>(fs);
        texInfo = new TextureData { Width = img.Width, Height = img.Height };
        var raw = new byte[texInfo.Width * texInfo.Height * 4];
        img.CopyPixelDataTo(raw);

        fixed (byte* data = raw)
        {
            var surf = _sdl.CreateRGBSurfaceWithFormatFrom(
                data, texInfo.Width, texInfo.Height, 32,
                texInfo.Width * 4, (uint)PixelFormatEnum.Rgba32);
            var tex = _sdl.CreateTextureFromSurface(_renderer, surf);
            _sdl.FreeSurface(surf);
            _textureData[_textureId] = texInfo;
            _texturePointers[_textureId] = (IntPtr)tex;
        }

        return _textureId++;
    }

    public void RenderTexture(int id, Rectangle<int> src, Rectangle<int> dst,
        RendererFlip flip = RendererFlip.None, double angle = 0, Point center = default)
    {
        if (_texturePointers.TryGetValue(id, out var ptr))
        {
            var tex = (Texture*)ptr;
            var dest = _camera.ToScreenCoordinates(dst);
            _sdl.RenderCopyEx(_renderer, tex, in src, in dest, angle, in center, flip);
        }
    }

    public void PresentFrame()
    {
        DrawTime();
        if (!_timerRunning) DrawDeathUI();
        _sdl.RenderPresent(_renderer);
    }

    private Image<Rgba32> Blank(int w, int h)
    {
        var img = new Image<Rgba32>(w, h);
        img.Mutate(c => c.Clear(ImgColor.Transparent));
        return img;
    }

    private void DrawTextCentered(string text, int cx, int cy, Font font, ImgColor color)
    {
        var size = TextMeasurer.MeasureSize(text, new TextOptions(font));
        int w = (int)Math.Ceiling(size.Width);
        int h = (int)Math.Ceiling(size.Height);

        using var img = Blank(w, h);
        img.Mutate(c => c.DrawText(text, font, color, new PointF(0, 0)));
        Blit(img, cx - w / 2, cy - h / 2);
    }

    private void DrawButton(string text, int cx, int cy,
        out int bx, out int by, out int bw, out int bh)
    {
        var size = TextMeasurer.MeasureSize(text, new TextOptions(_font));
        bw = (int)Math.Ceiling(size.Width) + 20;
        bh = (int)Math.Ceiling(size.Height) + 20;
        bx = cx - bw / 2;
        by = cy - bh / 2;

        var rect = new Rectangle<int>(bx, by, bw, bh);
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);
        _sdl.SetRenderDrawColor(_renderer, 0, 0, 0, 200);
        _sdl.RenderFillRect(_renderer, in rect);
        _sdl.SetRenderDrawColor(_renderer, 255, 255, 255, 255);
        _sdl.RenderDrawRect(_renderer, in rect);

        DrawTextCentered(text, cx, cy, _font, ImgColor.White);
    }

    private void DrawTime()
    {
        var secs = _timerRunning
            ? (int)(DateTime.Now - _startTime).TotalSeconds
            : _frozenSeconds;

        var size = TextMeasurer.MeasureSize(secs.ToString(), new TextOptions(_font));
        int pad = 16;
        int w = (int)Math.Ceiling(size.Width) + pad * 2;
        int h = (int)Math.Ceiling(size.Height) + pad * 2;

        using var img = Blank(w, h);
        img.Mutate(c => c.DrawText(secs.ToString(), _font, ImgColor.White, new PointF(pad, pad)));
        Blit(img, _timerPosX - w, _timerPosY);
    }

    private void DrawDeathUI()
    {
        var ws = _window.Size;
        var overlay = new Rectangle<int>(0, 0, ws.Width, ws.Height);
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);
        _sdl.SetRenderDrawColor(_renderer, 0, 0, 0, 180);
        _sdl.RenderFillRect(_renderer, in overlay);

        DrawTextCentered("GAME OVER", ws.Width / 2, 100, _largeFont, ImgColor.White);
        DrawButton("PLAY AGAIN", ws.Width / 2, 240, out _playX, out _playY, out _playW, out _playH);
    }

    private void Blit(Image<Rgba32> img, int dx, int dy)
    {
        var raw = new byte[img.Width * img.Height * 4];
        img.CopyPixelDataTo(raw);
        fixed (byte* data = raw)
        {
            var surf = _sdl.CreateRGBSurfaceWithFormatFrom(
                data, img.Width, img.Height, 32, img.Width * 4, (uint)PixelFormatEnum.Rgba32);
            var tex = _sdl.CreateTextureFromSurface(_renderer, surf);
            _sdl.FreeSurface(surf);

            var dst = new Rectangle<int>(dx, dy, img.Width, img.Height);
            _sdl.RenderCopy(_renderer, tex, null, in dst);
            _sdl.DestroyTexture(tex);
        }
    }
}
