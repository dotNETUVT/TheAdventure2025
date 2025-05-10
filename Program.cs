using System;
using Thread = System.Threading.Thread;
using Silk.NET.SDL;

namespace TheAdventure
{
    public static class Program
    {
        public static void Main()
        {
            var sdl = new Sdl(new SdlContext());
            var flags = Sdl.InitVideo | Sdl.InitAudio | Sdl.InitEvents
                        | Sdl.InitTimer | Sdl.InitGamecontroller | Sdl.InitJoystick;
            if (sdl.Init(flags) < 0)
                throw new InvalidOperationException("SDL_Init failed.");

            using var window   = new GameWindow(sdl);
            var   renderer = new GameRenderer(sdl, window);
            var   input    = new Input(sdl);
            var   engine   = new Engine(renderer, input);

            engine.SetupWorld();

            bool quit = false;
            while (!quit)
            {
                quit = input.ProcessInput();
                if (quit) break;

                engine.ProcessFrame();
                engine.RenderFrame();

                Thread.Sleep(13);
            }

            sdl.Quit();
        }
    }
}