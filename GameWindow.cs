using System;
using Silk.NET.SDL;

namespace TheAdventure
{
    public unsafe class GameWindow : IDisposable
    {
        private readonly Sdl _sdl;
        private IntPtr _window;

        public (int Width, int Height) Size
        {
            get
            {
                int w = 0, h = 0;
                _sdl.GetWindowSize((Window*)_window, ref w, ref h);
                return (w, h);
            }
        }

        public GameWindow(Sdl sdl)
        {
            _sdl = sdl;
            _window = (IntPtr)sdl.CreateWindow(
                "The Adventure",
                Sdl.WindowposUndefined, Sdl.WindowposUndefined,
                480, 300,
                (uint)(WindowFlags.Resizable | WindowFlags.AllowHighdpi)
            );
            if (_window == IntPtr.Zero)
                throw _sdl.GetErrorAsException() ?? new Exception("Failed to create window.");
        }

        public IntPtr CreateRenderer()
        {
            var renderer = (IntPtr)_sdl.CreateRenderer(
                (Window*)_window, -1,
                (uint)RendererFlags.Accelerated
            );
            if (renderer == IntPtr.Zero)
                throw _sdl.GetErrorAsException() ?? new Exception("Failed to create renderer.");
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

        ~GameWindow() => ReleaseUnmanagedResources();
    }
}