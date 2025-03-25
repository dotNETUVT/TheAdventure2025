using Silk.NET.SDL;

namespace TheAdventure;

public unsafe class InputLogic
{
    private Sdl _sdl;
    private GameLogic _gameLogic;

    private DateTimeOffset _lastBombPlacedTime = DateTimeOffset.MinValue;
    private const int BOMB_PLACEMENT_COOLDOWN_MS = 500;

    public InputLogic(Sdl sdl, GameLogic gameLogic)
    {
        _sdl = sdl;
        _gameLogic = gameLogic;
    }

    public bool ProcessInput()
    {
        ReadOnlySpan<byte> _keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        Span<byte> mouseButtonStates = stackalloc byte[(int)MouseButton.Count];

        mouseButtonStates.Clear();

        var mouseX = 0;
        var mouseY = 0;

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
                        mouseX = ev.Button.X;
                        mouseY = ev.Button.Y;
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

        if (mouseButtonStates[(byte)MouseButton.Primary] == 1)
        {
            var currentTime = DateTimeOffset.UtcNow;
            if ((currentTime - _lastBombPlacedTime).TotalMilliseconds >= BOMB_PLACEMENT_COOLDOWN_MS)
            {
                _gameLogic.AddBomb(mouseX, mouseY);
                _lastBombPlacedTime = currentTime;
            }
        }

        return false;
    }
}