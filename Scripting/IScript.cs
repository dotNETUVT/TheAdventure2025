namespace TheAdventure.Scripting;

public interface IScript
{
    /// <summary>
    /// Method called by the game when the script is first "loaded".
    /// </summary>
    public void Initialize();
    /// <summary>
    /// Method containing the actual script code, to be executed every time <see cref="Engine.ProcessFrame"/> is called.
    /// </summary>
    public void Execute(Engine engine);
}