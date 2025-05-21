using Silk.NET.SDL;
using System;

namespace TheAdventure;

public unsafe class Input
{
    private readonly Sdl _sdl;

    public EventHandler<(int x, int y)>? OnMouseClick;
    public event Action<int>? OnMouseWheel;

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

    public bool IsKeyRPressed()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)KeyCode.R] == 1;
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
                case (uint)EventType.Windowevent:
                    {
                        switch (ev.Window.Event)
                        {
                            case (byte)WindowEventID.Shown:
                            case (byte)WindowEventID.Exposed:
                            case (byte)WindowEventID.Hidden:
                            case (byte)WindowEventID.Moved:
                            case (byte)WindowEventID.SizeChanged:
                            case (byte)WindowEventID.Minimized:
                            case (byte)WindowEventID.Maximized:
                            case (byte)WindowEventID.Restored:
                            case (byte)WindowEventID.Enter:
                            case (byte)WindowEventID.Leave:
                            case (byte)WindowEventID.FocusGained:
                            case (byte)WindowEventID.FocusLost:
                            case (byte)WindowEventID.Close:
                                break;
                            case (byte)WindowEventID.TakeFocus:
                                _sdl.SetWindowInputFocus(_sdl.GetWindowFromID(ev.Window.WindowID));
                                break;
                        }
                        break;
                    }
                case (uint)EventType.Fingermotion:
                case (uint)EventType.Mousemotion:
                case (uint)EventType.Fingerdown:
                case (uint)EventType.Fingerup:
                case (uint)EventType.Mousebuttonup:
                case (uint)EventType.Keyup:
                case (uint)EventType.Keydown:
                    break; // No specific handling for these yet, but good to have cases
                case (uint)EventType.Mousebuttondown:
                    {
                        if (ev.Button.Button == (byte)MouseButton.Primary)
                        {
                            OnMouseClick?.Invoke(this, (ev.Button.X, ev.Button.Y));
                        }
                        break;
                    }
                case (uint)EventType.Mousewheel:
                    {
                        OnMouseWheel?.Invoke(ev.Wheel.Y);
                        break;
                    }
            }
        }
        return false;
    }
}