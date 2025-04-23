using Silk.NET.SDL;
using TheAdventure.Audio;
using Thread = System.Threading.Thread;

namespace TheAdventure;

public static class Program
{
    public static void Main()
    {
        // ------------------------------ SDL bootstrap
        var sdl = new Sdl(new SdlContext());
        if (sdl.Init(
                Sdl.InitVideo | Sdl.InitAudio | Sdl.InitEvents | Sdl.InitTimer |
                Sdl.InitGamecontroller | Sdl.InitJoystick) < 0)
        {
            throw new InvalidOperationException($"SDL init failed: {sdl.GetErrorS()}");
        }

        // ------------------------------- engine objects
        var window   = new GameWindow(sdl);
        var renderer = new GameRenderer(sdl, window);
        var game     = new GameLogic(renderer);
        var input    = new InputLogic(sdl, game);

        game.InitializeGame();

        // ------------------------------- audio bootstrap
        var audioDir = Path.Combine("Assets", "Audio");
        AudioManager.I.LoadWav("step", Path.Combine(audioDir, "footstep.wav"));
        AudioManager.I.LoadWav("boom", Path.Combine(audioDir, "explosion.wav"));
        AudioManager.I.LoadWav("bgm",  Path.Combine(audioDir, "bgm.wav"));

        AudioManager.I.Play("bgm", 0.25f, loop: true);
        // -------------------------------------

        // --------------------------------- main loop
        bool quit = false;
        while (!quit)
        {
            quit = input.ProcessInput();
            if (quit) break;

            game.ProcessFrame();
            game.RenderFrame();

            Thread.Sleep(13); // ≈75 FPS
        }

        window.Destroy();
        sdl.Quit();
    }
}
