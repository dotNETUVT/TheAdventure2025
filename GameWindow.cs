using Silk.NET.SDL;

namespace TheAdventure;

public unsafe class GameWindow : IDisposable
{
    public (int Width, int Height) Size
    {
        get
        {
            int width = 0;
            int height = 0;
            _sdl.GetWindowSize((Window*)_window, ref width, ref height);

            return (width, height);
        }
    }

    private IntPtr _window;
    private readonly Sdl _sdl;

    public GameWindow(Sdl sdl)
    {
        _sdl = sdl;
        _window = (IntPtr)sdl.CreateWindow(
            "The Adventure", Sdl.WindowposUndefined, Sdl.WindowposUndefined, 640, 400,
            (uint)WindowFlags.Resizable | (uint)WindowFlags.AllowHighdpi
        );

        if (_window == IntPtr.Zero)
        {
            var ex = sdl.GetErrorAsException();
            if (ex != null)
            {
                throw ex;
            }

            throw new Exception("Failed to create window.");
        }
    }

    public IntPtr CreateRenderer()
    {
        var renderer = (IntPtr)_sdl.CreateRenderer((Window*)_window, -1, (uint)RendererFlags.Accelerated);
        if (renderer == IntPtr.Zero)
        {
            var ex = _sdl.GetErrorAsException();
            if (ex != null)
            {
                throw ex;
            }

            throw new Exception("Failed to create renderer.");
        }

        return renderer;
    }

    private void ReleaseUnmanagedResources()
    {
        if (_window != IntPtr.Zero)
        {
            _sdl.DestroyWindow((Window*)_window);
            _window = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~GameWindow()
    {
        ReleaseUnmanagedResources();
    }
}