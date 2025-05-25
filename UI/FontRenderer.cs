using Silk.NET.Maths;
using Silk.NET.SDL;
using System.Runtime.InteropServices;

namespace TheAdventure.UI;

// Adding SDL_Surface struct definition to match SDL2
[StructLayout(LayoutKind.Sequential)]
public unsafe struct SDL_Surface
{
    public uint Flags;
    public IntPtr Format;
    public int W;
    public int H;
    public int Pitch;
    public IntPtr Pixels;
    public IntPtr UserData;
    public int Locked;
    public IntPtr LockData;
    public Rectangle<int> ClipRect;
    public IntPtr Map;
    public int RefCount;
}

public class FontRenderer
{
    private readonly Sdl _sdl;
    private IntPtr _font;
    private bool _initialized;

    public FontRenderer(Sdl sdl)
    {
        _sdl = sdl;
        _initialized = Ttf.TTF_Init() == 0;
    }

    public void LoadFont(string path, int fontSize)
    {
        if (!_initialized)
            return;

        _font = Ttf.TTF_OpenFont(path, fontSize);
        if (_font == IntPtr.Zero)
        {
            Console.WriteLine($"Failed to load font: {path}");
        }
    }

    public int RenderText(IntPtr renderer, string text, int x, int y, byte r, byte g, byte b, TextAlign align = TextAlign.Left)
    {
        if (!_initialized || _font == IntPtr.Zero)
            return 0;

        var color = new TheAdventure.SDL_Color { r = r, g = g, b = b, a = 255 };
        IntPtr surface = Ttf.TTF_RenderText_Blended(_font, text, color);
        if (surface == IntPtr.Zero)
            return 0;

        unsafe
        {
            SDL_Surface* sdlSurface = (SDL_Surface*)surface.ToPointer();
            Texture* texture = _sdl.CreateTextureFromSurface((Renderer*)renderer, (Surface*)surface);
            
            int width = sdlSurface->W;
            int height = sdlSurface->H;

            // Adjust x position based on alignment
            int adjustedX = x;
            if (align == TextAlign.Center)
                adjustedX = x - width / 2;
            else if (align == TextAlign.Right)
                adjustedX = x - width;

            var srcRect = new Rectangle<int>(0, 0, width, height);
            var destRect = new Rectangle<int>(adjustedX, y, width, height);
            
            _sdl.RenderCopy((Renderer*)renderer, texture, in srcRect, in destRect);
            
            _sdl.FreeSurface((Surface*)surface);
            _sdl.DestroyTexture(texture);
            
            return height;
        }
    }

    public (int Width, int Height) MeasureText(string text)
    {
        int width = 0;
        int height = 0;

        if (!_initialized || _font == IntPtr.Zero)
            return (width, height);

        unsafe
        {
            Ttf.TTF_SizeText(_font, text, &width, &height);
        }

        return (width, height);
    }

    public void Dispose()
    {
        if (_font != IntPtr.Zero)
        {
            Ttf.TTF_CloseFont(_font);
            _font = IntPtr.Zero;
        }

        if (_initialized)
        {
            Ttf.TTF_Quit();
            _initialized = false;
        }
    }
}

public enum TextAlign
{
    Left,
    Center,
    Right
} 