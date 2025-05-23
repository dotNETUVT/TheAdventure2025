using Silk.NET.SDL;

namespace TheAdventure;

public unsafe class Input
{
    private readonly Sdl _sdl;

    public EventHandler<(int x, int y)>? OnMouseClick;


    private bool _escapeHeld   = false;     // key is physically down
    private bool _escapePulse  = false;    


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
        ReadOnlySpan<byte> _keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return _keyboardState[(int)KeyCode.A] == 1;
    }

    public bool IsKeyBPressed()
    {
        ReadOnlySpan<byte> _keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return _keyboardState[(int)KeyCode.B] == 1;
    }

    public bool IsKeyEnterPressed()
    {
        ReadOnlySpan<byte> _keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return _keyboardState[(int)KeyCode.Return] == 1;
    }

    public bool IsKeyEscapePressed()
    {
        if (_escapePulse)
        {
            _escapePulse = false;           // consume the pulse
            return true;
        }
        return false;
    }



    public bool ProcessInput()
    {
        Event ev = new Event();
        while (_sdl.PollEvent(ref ev) != 0)
        {
            if (ev.Type == (uint)EventType.Quit)               return true;

            switch (ev.Type)
            {
                case (uint)EventType.Keydown:
                    if (ev.Key.Keysym.Sym == (int)KeyCode.Escape && !_escapeHeld)
                    {
                        _escapePulse = true;                   // 1-frame pulse
                        _escapeHeld  = true;
                    }
                    break;

                case (uint)EventType.Keyup:
                    if (ev.Key.Keysym.Sym == (int)KeyCode.Escape)
                        _escapeHeld = false;
                    break;

                case (uint)EventType.Mousebuttondown:
                    if (ev.Button.Button == (byte)MouseButton.Primary)
                        OnMouseClick?.Invoke(this, (ev.Button.X, ev.Button.Y));
                    break;
            }
        }
        return false;
    }
}

