using Silk.NET.SDL;


namespace TheAdventure;

public unsafe class InputLogic
{
    private Sdl _sdl;
    private GameLogic _gameLogic;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    public InputLogic(Sdl sdl, GameLogic gameLogic)
    {
        _sdl = sdl;
        _gameLogic = gameLogic;
    }

    public bool ProcessInput()
    {
        var currentTime = DateTimeOffset.Now;

        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        Span<byte> mouseButtonStates = stackalloc byte[(int)MouseButton.Count];

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
                    mouseX = ev.Motion.X;
                    mouseY = ev.Motion.Y;
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

        var timeSinceLastFrame = (int)currentTime.Subtract(_lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        var up = 0.0;
        var down = 0.0;
        var left = 0.0;
        var right = 0.0;

        if (keyboardState[(int)KeyCode.Up] == 1)
        {
            up = 1.0;
        }

        if (keyboardState[(int)KeyCode.Down] == 1)
        {
            down = 1.0;
        }

        if (keyboardState[(int)KeyCode.Left] == 1)
        {
            left = 1.0;
        }

        if (keyboardState[(int)KeyCode.Right] == 1)
        {
            right = 1.0;
        }

        //For Restart mechanism
        if (keyboardState[(int)KeyCode.R] == 1)  // Check for 'R' key press
        {
            _gameLogic.RestartGame();
        }

        if (keyboardState[(int)KeyCode.P] == 1)
        {
            _gameLogic.TogglePause();
            System.Threading.Thread.Sleep(200);
        }

        
        _gameLogic.UpdatePlayerPosition(up, down, left, right, timeSinceLastFrame);

        if (mouseButtonStates[(byte)MouseButton.Primary] == 1)
        {
            _gameLogic.AddBomb(mouseX, mouseY);
        }

        return false;
    }
}
