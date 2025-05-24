namespace TheAdventure.GameState;

public class StateChangeRequest
{
    public enum ChangeTypeEnum { Push, Pop, Change, PopAll };
    public ChangeTypeEnum ChangeType { get; }
    public GameStateType? NewState { get; } // Nullable in case of Pop

    public StateChangeRequest(ChangeTypeEnum changeType, GameStateType? newState = null)
    {
        ChangeType = changeType;
        NewState = newState;
    }
}