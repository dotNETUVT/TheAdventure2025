using Silk.NET.Input;
using Silk.NET.Maths;

namespace TheAdventure.GameState;

public class PausedState : IGameState
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    
    public event Action<StateChangeRequest>? OnStateChange;
    // public IGameState? Parent { get; set; }

    // Callbacks to parent draw and update methods
    public UpdateCallback? UpdateCallback { get; set; }
    public DrawCallback? DrawCallback { get; set; }

    public PausedState(
        IGameState? parent, // TODO: Use concrete type to limit possible parent states
        GameRenderer renderer,
        Input input)
    {
        _renderer = renderer;
        _input = input;

        UpdateCallback = null;
        DrawCallback = parent != null ? parent.Draw : null;
    }
    
    public void Enter()
    {
        Console.WriteLine("Entering PausedState");
        GameTime.Instance.Pause();
    }

    public void Exit()
    {
        Console.WriteLine("Exiting PausedState");
        GameTime.Instance.Resume();
    }

    public void Update(double deltaTime)
    {
        if (_input.IsEscapePressed())
        {
            OnStateChange?.Invoke(new StateChangeRequest(
                StateChangeRequest.ChangeTypeEnum.Pop,
                GameStateType.Playing)); // TODO: Refactorize this
            return;
        }
    }

    public void Draw()
    {
        DrawCallback?.Invoke();
        _renderer.SetDrawColor(20, 20, 20, 128);
        var (width, height) = _renderer.GetWindowSize();
        var overlay = new Rectangle<int>(0, 0, width, height);
        _renderer.FillRect(overlay);
    }

    public void Render()
    {
        Draw();
        _renderer.PresentFrame();
    }
}