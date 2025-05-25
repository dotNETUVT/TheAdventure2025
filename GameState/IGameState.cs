namespace TheAdventure.GameState;

public delegate void UpdateCallback(float dt);
public delegate void DrawCallback();

public interface IGameState
{
    /// <summary>
    /// <para> Used for setting up the game state
    /// e.g loading resources, subscribing to events, transitions, etc </para>
    /// <para> Should only be called from GameStateManager. </para>
    /// </summary>
    void Enter();

    /// <summary>
    /// <para> Used for setting up the game state
    /// e.g unloading resources, unsubscribing to events, transitions, etc </para>
    /// <para> Should only be called from GameStateManager. </para>
    /// </summary>
    void Exit();

    /// <summary>
    /// <para> Used for updating game state logic
    /// e.g movement, collisions, input, etc. </para>
    /// <para> Should only be called from GameStateManager. </para>
    /// </summary>
    void Update(double deltaTime);

    /// <summary>
    /// <para> Used for filling up rendering buffer without presenting
    /// the buffer via <c>processFrame</c></para>
    /// <para> Should only be called from GameStateManager. </para>
    /// </summary>
    void Draw();

    /// <summary>
    /// <para> Used for rendering the buffer to the screen
    /// via the <c>Draw</c> and the <c>ProcessFrame</c> methods </para>
    /// <para> Further customization can be added to it if necessary. </para>
    /// <para> Should only be called from GameStateManager. </para>
    /// </summary>
    void Render();

    event Action<StateChangeRequest> OnStateChange;
    // IGameState? Parent { get; set; }

    UpdateCallback? UpdateCallback { get; set; }
    DrawCallback? DrawCallback { get; set; }
}