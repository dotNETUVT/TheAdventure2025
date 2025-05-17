using Silk.NET.SDL;

namespace TheAdventure;

public unsafe class InputLogic
{
    private Sdl _sdl;

    public InputLogic(Sdl sdl)
    {
        _sdl = sdl;
    }

    public bool ProcessInput()
    {
        ReadOnlySpan<byte> _keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        Span<byte> mouseButtonStates = stackalloc byte[(int)MouseButton.Count];
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
                        {
                            break;
                        }
                        case (byte)WindowEventID.Hidden:
                        {
                            break;
                        }
                        case (byte)WindowEventID.Moved:
                        {
                            break;
                        }
                        case (byte)WindowEventID.SizeChanged:
                        {
                            break;
                        }
                        case (byte)WindowEventID.Minimized:
                        case (byte)WindowEventID.Maximized:
                        case (byte)WindowEventID.Restored:
                            break;
                        case (byte)WindowEventID.Enter:
                        {
                            break;
                        }
                        case (byte)WindowEventID.Leave:
                        {
                            break;
                        }
                        case (byte)WindowEventID.FocusGained:
                        {
                            break;
                        }
                        case (byte)WindowEventID.FocusLost:
                        {
                            break;
                        }
                        case (byte)WindowEventID.Close:
                        {
                            break;
                        }
                        case (byte)WindowEventID.TakeFocus:
                        {
                            _sdl.SetWindowInputFocus(_sdl.GetWindowFromID(ev.Window.WindowID));
                            break;
                        }
                    }

                    break;
                }

                case (uint)EventType.Fingermotion:
                {
                    break;
                }

                case (uint)EventType.Mousemotion:
                {
                    break;
                }

                case (uint)EventType.Fingerdown:
                {
                    mouseButtonStates[(byte)MouseButton.Primary] = 1;
                    break;
                }
                case (uint)EventType.Mousebuttondown:
                {
                    mouseButtonStates[ev.Button.Button] = 1;
                    break;
                }

                case (uint)EventType.Fingerup:
                {
                    mouseButtonStates[(byte)MouseButton.Primary] = 0;
                    break;
                }

                case (uint)EventType.Mousebuttonup:
                {
                    mouseButtonStates[ev.Button.Button] = 0;
                    break;
                }

                case (uint)EventType.Mousewheel:
                {
                    break;
                }

                case (uint)EventType.Keyup:
                {
                    break;
                }

                case (uint)EventType.Keydown:
                {
                    break;
                }
            }
        }

        return false;
    }

    public (bool up, bool down, bool left, bool right) GetMovementKeys()
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);

        bool w = keyboardState[(int)KeyCode.W] != 0;
        bool s = keyboardState[(int)KeyCode.S] != 0;
        bool a = keyboardState[(int)KeyCode.A] != 0;
        bool d = keyboardState[(int)KeyCode.D] != 0;

        return (w, s, a, d);
    }

    public (bool clicked, int x, int y) GetMouseClick()
    {
        Event ev = new Event();
        while (_sdl.PollEvent(ref ev) != 0)
        {
            if (ev.Type == (uint)EventType.Mousebuttondown &&
                ev.Button.Button == (byte)MouseButton.Primary)
            {
                return (true, ev.Button.X, ev.Button.Y);
            }
        }

        return (false, 0, 0);
    }
}