using Silk.NET.SDL;
using Thread = System.Threading.Thread;
using System.IO;

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

                    int gameOverTextureId = gameRenderer.LoadTexture(Path.Combine("Assets", "gameover.png"), out var textureInfo);
                    var windowSize = gameWindow.Size;
                    int imageWidth = textureInfo.Width;
                    int imageHeight = textureInfo.Height;

                    var destRect = new Silk.NET.Maths.Rectangle<int>(
                        (windowSize.Width - imageWidth) / 2,
                        (windowSize.Height - imageHeight) / 2,
                        imageWidth,
                        imageHeight);

                    gameRenderer.SetDrawColor(0, 0, 0, 255);
                    gameRenderer.ClearScreen();
                    gameRenderer.RenderTexture(gameOverTextureId,
                        new Silk.NET.Maths.Rectangle<int>(0, 0, imageWidth, imageHeight),
                        destRect);
                    gameRenderer.PresentFrame();

                    Thread.Sleep(3000);
                    break;
                }

                gameRenderer.RenderScoreWithoutTexture();
                Thread.Sleep(13);
            }
        }

        sdl.Quit();
    }
}
