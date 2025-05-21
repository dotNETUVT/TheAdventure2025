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
                quit = input.ProcessInput();
                if (quit) break;

                engine.ProcessFrame();
                engine.RenderFrame();

                if (engine.IsBombAdded())
                {
                    Console.WriteLine("Game Over: A bomb has appeared!");
                    Console.WriteLine($"Final Score: {gameRenderer.GetScore()}");
                    break;
                }

                gameRenderer.RenderScoreWithoutTexture();
                Thread.Sleep(13);
            }

        }

        static void DisplayGameOverMessage(GameRenderer renderer, GameWindow window, int score)
        {
            renderer.ClearScreen();
            renderer.SetDrawColor(255, 255, 255, 255); // White background


            renderer.PresentFrame();
            Thread.Sleep(3000);
        }

        sdl.Quit();
    }
}