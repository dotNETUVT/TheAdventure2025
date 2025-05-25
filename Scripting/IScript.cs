using TheAdventure.GameState;

namespace TheAdventure.Scripting;

public interface IScript
{
    /// <summary>
    /// Method called by the game when the script is first "loaded".
    /// </summary>
    public void Initialize();
    /// <summary>
    /// <para> Method containing the actual script code. </para> 
    /// <para>Only call in child implementations of <see cref="IGameState.Update"/> </para>
    /// </summary>
    public void Execute(IGameState state);
}