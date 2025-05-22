using Silk.NET.SDL;

namespace TheAdventure;

public unsafe class Input
{
    private readonly Sdl _sdl;

    public EventHandler<(int x, int y)>? OnMouseClick;

    public Input(Sdl sdl)
    {
        _sdl = sdl;
    }

    public bool IsLeftPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.Left] == 1;
    }

    public bool IsRightPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.Right] == 1;
    }

    public bool IsUpPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.Up] == 1;
    }

    public bool IsDownPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.Down] == 1;
    }

    public bool IsKeyAPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.A] == 1;
    }

    public bool IsKeyBPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.B] == 1;
    }

    public bool IsKeyCPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.C] == 1;
    }

    public bool IsKeyPPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.P] == 1;
    }

    public bool IsKeyRPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.R] == 1;
    }

    public bool IsKeyHPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.H] == 1;
    }

    public bool ProcessInput()
    {
        Event ev = new Event();
        while (_sdl.PollEvent(ref ev) != 0)
        {
            if (ev.Type == (uint)EventType.Quit)
            {
                return true;
            }

            switch (ev.Type)
            {
                case (uint)EventType.Mousebuttondown:
                    if (ev.Button.Button == (byte)MouseButton.Primary)
                    {
                        OnMouseClick?.Invoke(this, (ev.Button.X, ev.Button.Y));
                    }
                    break;

                case (uint)EventType.Windowevent:
                    if (ev.Window.Event == (byte)WindowEventID.TakeFocus)
                    {
                        _sdl.SetWindowInputFocus(_sdl.GetWindowFromID(ev.Window.WindowID));
                    }
                    break;
            }
        }

        return false;
    }
}