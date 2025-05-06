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

        using (var gameWindow = new GameWindow(sdl))
        {
            var input = new Input(sdl);
            var gameRenderer = new GameRenderer(sdl, gameWindow);
            var engine = new Engine(gameRenderer, input);

            engine.SetupWorld();

            bool quit = false;
            while (!quit)
            {
                bool inputQuit = input.ProcessInput();
                bool engineQuit = engine.ProcessFrame();
                quit = inputQuit || engineQuit;

                if (quit) break;

                engine.RenderFrame();

                Thread.Sleep(13);
            }
        }

        sdl.Quit();
    }
}