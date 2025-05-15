using Silk.NET.SDL;
using Thread = System.Threading.Thread;
using TheAdventure.Scripting;
namespace TheAdventure;

public static class Program
{
    public static void Main()
    {
        var sdl = new Sdl(new SdlContext());

        if (sdl.Init(Sdl.InitVideo | Sdl.InitAudio | Sdl.InitEvents | Sdl.InitTimer |
                     Sdl.InitGamecontroller | Sdl.InitJoystick) < 0)
            throw new InvalidOperationException("Failed to initialize SDL.");

        using var gameWindow = new GameWindow(sdl);
        var input        = new Input(sdl);
        var gameRenderer = new GameRenderer(sdl, gameWindow);
        var scriptEngine = new ScriptEngine();
        Engine CreateEngine()
        {
            var eng = new Engine(gameRenderer, input, scriptEngine);
            eng.SetupWorld();            
            return eng;
        }

        Engine engine = CreateEngine();

        bool quit = false;
        while (!quit)
        {
            quit = input.ProcessInput();
            if (quit) break;

            bool restart = engine.ProcessFrame();  

            if (restart)
            {
                engine = CreateEngine();            
                continue;              
            }

            engine.RenderFrame();
            Thread.Sleep(13);
        }

        sdl.Quit();
    }
}
