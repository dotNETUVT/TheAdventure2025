namespace TheAdventure.Models;

public class KeyBindings
{
    public KeyCode Up { get; init; }
    public KeyCode Down { get; init; }

    public KeyCode Left { get; init; }

    public KeyCode Right { get; init; }

    public static KeyBindings ArrowKeys => new()
    {
        Up = KeyCode.Up,
        Down = KeyCode.Down,
        Left = KeyCode.Left,
        Right = KeyCode.Right,
    };
    public static KeyBindings WASDKeys => new()
    {
        Up = KeyCode.W,
        Down = KeyCode.A,
        Left = KeyCode.S,
        Right = KeyCode.D,
    };
    // public static KeyBindings CustomKeys(KeyCode up, KeyCode down, KeyCode left, KeyCode right)
    // {
    //     Up = up;
    //     Down = down;
    //     Left = left;
    //     Right = right;
    // }

}