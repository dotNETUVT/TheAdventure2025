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

        var gameWindow = new GameWindow(sdl);
        var gameRenderer = new GameRenderer(sdl, gameWindow);
        var gameLogic = new GameLogic(gameRenderer);
        var inputLogic = new InputLogic(sdl, gameLogic);

        gameLogic.InitializeGame();

        bool quit = false;
        while (!quit)
        {
            quit = inputLogic.ProcessInput();
            if (quit) break;

            gameLogic.ProcessFrame();
            gameLogic.RenderFrame();
            
            Thread.Sleep(13);
        }

        gameWindow.Destroy();

        sdl.Quit();
    }
}