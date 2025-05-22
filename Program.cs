using Silk.NET.SDL;
using Thread = System.Threading.Thread;

namespace TheAdventure;

public static class Program
{
    public static void Main()
    {
        var sdl = new Sdl(new SdlContext());

        var sdlInitResult = sdl.Init(Sdl.InitVideo | Sdl.InitAudio | Sdl.InitEvents | Sdl.InitTimer |
                                     Sdl.InitGamecontroller |
                                     Sdl.InitJoystick);
        if (sdlInitResult < 0)
        {
            throw new InvalidOperationException("Failed to initialize SDL.");
        }

        Engine? engine = null;

        try
        {
            using (var gameWindow = new GameWindow(sdl))
            {
                var input = new Input(sdl);
                using (var gameRenderer = new GameRenderer(sdl, gameWindow))
                {
                    engine = new Engine(gameRenderer, input);
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
                }
            }
        }
        finally
        {
            sdl.Quit();
        }
    }
}