using Silk.NET.SDL;

namespace TheAdventure;

public unsafe class Input
{
    private readonly Sdl _sdl;

    public event EventHandler<(int x, int y)>? OnMouseClick;

    public Input(Sdl sdl) => _sdl = sdl;

    private ReadOnlySpan<byte> KeyboardState => new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);

    private bool IsKeyDown(KeyCode key) => KeyboardState[(int)key] == 1;

    public bool IsLeftPressed()  => IsKeyDown(KeyCode.Left);
    public bool IsRightPressed() => IsKeyDown(KeyCode.Right);
    public bool IsUpPressed()    => IsKeyDown(KeyCode.Up);
    public bool IsDownPressed()  => IsKeyDown(KeyCode.Down);

    public bool IsKeyAPressed() => IsKeyDown(KeyCode.A);
    public bool IsKeyBPressed() => IsKeyDown(KeyCode.B);

    public bool IsDashPressed() =>
        IsKeyDown(KeyCode.Space) || IsKeyDown(KeyCode.LShift) || IsKeyDown(KeyCode.RShift);

    public (int X, int Y) GetMousePosition()
    {
        int x = 0, y = 0;
        _sdl.GetMouseState(&x, &y);
        return (x, y);
    }

    public bool ProcessInput()
    {
        Event ev = new Event();
        while (_sdl.PollEvent(ref ev) != 0)
        {
            if (ev.Type == (uint)EventType.Quit) return true;

            switch (ev.Type)
            {
                case (uint)EventType.Mousebuttondown:
                    if (ev.Button.Button == (byte)MouseButton.Primary)
                        OnMouseClick?.Invoke(this, (ev.Button.X, ev.Button.Y));
                    break;
            }
        }
        return false;
    }
}