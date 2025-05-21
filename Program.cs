using Silk.NET.SDL;
using System;
using Thread = System.Threading.Thread;

namespace TheAdventure;

public static class Program
{
    public static unsafe void Main()
    {
        var sdl = new Sdl(new SdlContext());

        // Initialize SDL for video, events, etc. Audio init for SDL itself can be kept or removed
        // if NAudio is the sole audio provider. Let's keep Sdl.InitAudio for now,
        // as other parts of SDL might expect it, though NAudio won't use it.
        if (sdl.Init(Sdl.InitVideo | Sdl.InitEvents | Sdl.InitTimer | Sdl.InitGamecontroller | Sdl.InitJoystick | Sdl.InitAudio) < 0)
        {
            Console.WriteLine($"Failed to initialize SDL. SDL Error: {sdl.GetErrorS()}");
            throw new InvalidOperationException("Failed to initialize SDL.");
        }

        // NO SDL_mixer specific initialization here anymore

        Engine? engine = null;
        try
        {
            using (var gameWindow = new GameWindow(sdl))
            {
                var input = new Input(sdl);
                var gameRenderer = new GameRenderer(sdl, gameWindow);
                // Engine no longer needs the Sdl instance passed for AudioManager
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
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
        finally
        {
            engine?.Dispose(); // Engine will dispose AudioManagerNAudio

            // NO SDL_mixer specific cleanup here anymore

            sdl.Quit();
            Console.WriteLine("SDL quit.");
        }
    }
}