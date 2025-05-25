using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.SDL;
using TheAdventure.Models;
using TheAdventure.UI;
using Button = TheAdventure.UI.Button;

namespace TheAdventure.GameState;

public class GameOverState : IGameState
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private FontRenderer _fontRenderer;

    private Button _respawnButton;
    private Button _quitButton;

    private bool _isButtonClicked = false;
    public event Action<StateChangeRequest>? OnStateChange;

    // Callbacks to parent draw and update methods
    public UpdateCallback? UpdateCallback { get; set; }
    public DrawCallback? DrawCallback { get; set; }

    public GameOverState(
        IGameState? parent, // TODO: Use concrete type to limit possible parent states
        GameRenderer renderer,
        Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += OnMouseClick;
        
        UpdateCallback = null;
        DrawCallback = parent != null ? parent.Draw : null;
        
        // Create the font renderer
        _fontRenderer = new FontRenderer(new Sdl(new SdlContext()));
        _fontRenderer.LoadFont("Assets/Fonts/Arial.ttf", 24);
        
        // Set up UI
        var (windowWidth, windowHeight) = _renderer.GetWindowSize();
        int buttonWidth = 200;
        int buttonHeight = 50;
        int buttonSpacing = 30;
        
        // Center the buttons horizontally
        int centerX = windowWidth / 2;
        
        // Position buttons vertically
        int resumeY = windowHeight / 2 - buttonHeight - buttonSpacing / 2;
        int quitY = windowHeight / 2 + buttonSpacing / 2;
        
        _respawnButton = new Button("Respawn", centerX - buttonWidth / 2, resumeY, buttonWidth, buttonHeight);
        _quitButton = new Button("Quit", centerX - buttonWidth / 2, quitY, buttonWidth, buttonHeight);
        
        _respawnButton.OnClick = () => 
        {
            OnStateChange?.Invoke(new StateChangeRequest(
                StateChangeRequest.ChangeTypeEnum.Pop,
                GameStateType.Playing));
        };
        
        _quitButton.OnClick = () => 
        {
           OnStateChange?.Invoke(new StateChangeRequest(
                StateChangeRequest.ChangeTypeEnum.PopAll,
                GameStateType.MainMenu)); // TODO: Refactorize this
        };
    }
    
    private void OnMouseClick(object? sender, (int x, int y) mousePosition)
    {
        _isButtonClicked = true;
    }
    
    public void Enter()
    {
        Console.WriteLine("Entering GameOverState");
    }

    public void Exit()
    {
        Console.WriteLine("Exiting GameOverState");
        _input.OnMouseClick -= OnMouseClick;
    }

    public void Update(double deltaTime)
    { 
        var mousePosition = _input.GetMousePosition();
        bool isClicked = _isButtonClicked;
        _isButtonClicked = false;
        
        // Update buttons
        _respawnButton.Update(mousePosition.x, mousePosition.y, isClicked);
        _quitButton.Update(mousePosition.x, mousePosition.y, isClicked);
    }

    public void Draw()
    {
        DrawCallback?.Invoke();

        _renderer.SetDrawColor(20, 20, 20, 128);
        var (width, height) = _renderer.GetWindowSize();
        var overlay = new Rectangle<int>(0, 0, width, height);
        _renderer.FillRect(overlay);

        int titleY = height / 5;
        _fontRenderer.RenderText(_renderer.GetRawRenderer(), "GAME OVER", width / 2, titleY, 255, 255, 255, TextAlign.Center);

        _respawnButton.Draw(_renderer, _fontRenderer);
        _quitButton.Draw(_renderer, _fontRenderer);
    }

    public void Render()
    {
        Draw();
        _renderer.PresentFrame();
    }
}