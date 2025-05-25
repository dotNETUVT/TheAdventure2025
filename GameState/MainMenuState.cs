using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.SDL;
using TheAdventure.UI;
using Button = TheAdventure.UI.Button;

namespace TheAdventure.GameState;

public class MainMenuState : IGameState
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private FontRenderer _fontRenderer;
    private Button _playButton;
    private Button _quitButton;
    private bool _isButtonClicked = false;
    
    public event Action<StateChangeRequest>? OnStateChange;
    // public IGameState? Parent { get; set; }

    // Callbacks to parent draw and update methods
    public UpdateCallback? UpdateCallback { get; set; }
    public DrawCallback? DrawCallback { get; set; }

    public MainMenuState(
        IGameState? parent, // TODO: Use concrete type to limit possible parent states
        GameRenderer renderer,
        Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += OnMouseClick;
        
        UpdateCallback = null;
        DrawCallback = null;
        

        _fontRenderer = new FontRenderer(new Sdl(new SdlContext()));
        _fontRenderer.LoadFont("Assets/Fonts/Arial.ttf", 24);

        var (windowWidth, windowHeight) = _renderer.GetWindowSize();
        int buttonWidth = 200;
        int buttonHeight = 50;
        int buttonSpacing = 30;

        int centerX = windowWidth / 2;

        int resumeY = windowHeight / 2 - buttonHeight - buttonSpacing / 2;
        int quitY = windowHeight / 2 + buttonSpacing / 2;
        
        _playButton = new Button("Play", centerX - buttonWidth / 2, resumeY, buttonWidth, buttonHeight);
        _quitButton = new Button("Quit", centerX - buttonWidth / 2, quitY, buttonWidth, buttonHeight);
        
        _playButton.OnClick = () => 
        {
            OnStateChange?.Invoke(new StateChangeRequest(
                StateChangeRequest.ChangeTypeEnum.Push,
                GameStateType.Playing));
        };
        
        _quitButton.OnClick = () => 
        {
            // Quit the game
            Environment.Exit(0);
        };
    }
    
    private void OnMouseClick(object? sender, (int x, int y) mousePosition)
    {
        _isButtonClicked = true;
    }
    
    public void Enter()
    {
        Console.WriteLine("Entering MainMenuState");
    }

    public void Exit()
    {
        Console.WriteLine("Exiting MainMenuState");
        _input.OnMouseClick -= OnMouseClick;
    }

    public void Update(double deltaTime)
    {
        var mousePosition = _input.GetMousePosition();
        bool isClicked = _isButtonClicked;
        _isButtonClicked = false;
        
        // Update buttons
        _playButton.Update(mousePosition.x, mousePosition.y, isClicked);
        _quitButton.Update(mousePosition.x, mousePosition.y, isClicked);
    }

    public void Draw()
    {
        _renderer.SetDrawColor(102, 204, 255, 255);
        var (width, height) = _renderer.GetWindowSize();
        var overlay = new Rectangle<int>(0, 0, width, height);
        _renderer.FillRect(overlay);

        int titleY = height / 5;
        _fontRenderer.RenderText(_renderer.GetRawRenderer(), "The Adventure", width / 2, titleY, 255, 255, 255, TextAlign.Center);
        
        _playButton.Draw(_renderer, _fontRenderer);
        _quitButton.Draw(_renderer, _fontRenderer);
    }

    public void Render()
    {
        Draw();
        _renderer.PresentFrame();
    }
}