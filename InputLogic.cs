using Silk.NET.SDL;
using TheAdventure.Audio;

namespace TheAdventure;

public unsafe class InputLogic
{
    private readonly Sdl       _sdl;
    private readonly GameLogic _game;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;
    private int            _stepTimer  = 300;

    public InputLogic(Sdl sdl, GameLogic game)
    {
        _sdl  = sdl;
        _game = game;
    }

    // ------------------------------------------------ per-frame input
    public bool ProcessInput()
    {
        var now   = DateTimeOffset.Now;
        int delta = (int)(now - _lastUpdate).TotalMilliseconds;
        _lastUpdate = now;

        int numKeys;
        byte* keys = _sdl.GetKeyboardState(&numKeys);

        var ev = new Event();
        byte mouseDown = 0;
        int  mouseX = 0, mouseY = 0;

        while (_sdl.PollEvent(&ev) == 1)
        {
            if (ev.Type == (uint)EventType.Quit) return true;

            switch (ev.Type)
            {
                case (uint)EventType.Mousemotion:
                    mouseX = ev.Motion.X; mouseY = ev.Motion.Y;
                    break;

                case (uint)EventType.Mousebuttondown:
                    if (ev.Button.Button == (byte)MouseButton.Primary)
                        mouseDown = 1;
                    mouseX = ev.Button.X; mouseY = ev.Button.Y;
                    break;
            }
        }

        double up    = keys[(int)KeyCode.Up]    == 1 ? 1 : 0;
        double down  = keys[(int)KeyCode.Down]  == 1 ? 1 : 0;
        double left  = keys[(int)KeyCode.Left]  == 1 ? 1 : 0;
        double right = keys[(int)KeyCode.Right] == 1 ? 1 : 0;

        _game.UpdatePlayerPosition(up, down, left, right, delta);

        // ------------- foot-step SFX ----------------------------------
        bool moving = up + down + left + right > 0;
        if (moving)
        {
            _stepTimer += delta;
            if (_stepTimer >= 300)
            {
                AudioManager.I.Play("step", 1.0f);
                _stepTimer = 0;
            }
        }
        else _stepTimer = 300;

        // ------------- bombs & boom SFX --------------------------------
        if (mouseDown == 1)
        {
            _game.AddBomb(mouseX, mouseY);

            System.Threading.Tasks.Task.Delay(1500).ContinueWith(_ =>
            {
                AudioManager.I.Play("boom", 1.2f);
            });
        }

        return false;
    }
}
