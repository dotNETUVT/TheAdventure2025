namespace TheAdventure.GameState;

public class GameStateManager
{
    private readonly Stack<IGameState> _gameStates = new();
    public GameStateType GameStateType { get; set; }
    public IGameState? TopGameState
    {
        get
        {
            if (_gameStates.Count == 0)
                return null;

            return _gameStates.Peek();
        }
    }

    // Possibly DEPRECATED
    public void ChangeState(IGameState state)
    {
        if (_gameStates.Count > 0)
            _gameStates.Pop().Exit();

        _gameStates.Push(state);
        state.Enter();
    }

    public void PopAllStates()
    {
        while (_gameStates.Count > 0)
        {
            _gameStates.Pop().Exit();
        }
    }

    public void OnlyPopTopState()
    {
        if (_gameStates.Count > 0)
            _gameStates.Pop().Exit();
    }

    public void PopState()
    {
        if (_gameStates.Count == 0)
            return;

        _gameStates.Pop().Exit();
        if (_gameStates.Count > 0)
            _gameStates.Peek().Enter();
    }

    public void PushState(IGameState state)
    {
        if (_gameStates.Count > 0)
        {
            if (_gameStates.Peek().GetType() == state.GetType())
                return;

            // _gameStates.Peek().Exit();
        }

        _gameStates.Push(state);
        state.Enter();
    }
    
    public void Update(double deltaTime)
    {
        if (_gameStates.Count > 0)
            _gameStates.Peek().Update(deltaTime);
    }

    public void Render()
    {
        if (_gameStates.Count > 0)
            _gameStates.Peek().Render();
    }
}