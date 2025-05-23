namespace TheAdventure.GameState;

public delegate void UpdateCallback(float dt);
public delegate void DrawCallback();

public interface IGameState
{
    void Enter();
    void Exit();
    void Update(double deltaTime);
    void Draw();
    void Render();

    event Action<StateChangeRequest> OnStateChange;
    // IGameState? Parent { get; set; }

    UpdateCallback? UpdateCallback { get; set; }
    DrawCallback? DrawCallback { get; set; }
}