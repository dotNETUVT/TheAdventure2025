using Silk.NET.SDL;
using System.IO;
using System;
using TheAdventure;

namespace TheAdventure;

public unsafe class Input
{
    private readonly Sdl _sdl;
    private readonly GameRenderer _gameRenderer; // Added GameRenderer reference

    public EventHandler<(int x, int y)>? OnMouseClick;

    // Updated constructor to inject GameRenderer
    public Input(Sdl sdl, GameRenderer gameRenderer)
    {
        _sdl = sdl;
        _gameRenderer = gameRenderer;
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
                    if (ev.Window.Event == (byte)WindowEventID.TakeFocus)
                        _sdl.SetWindowInputFocus(_sdl.GetWindowFromID(ev.Window.WindowID));
                    break;

                case (uint)EventType.Mousebuttondown:
                    if (ev.Button.Button == (byte)MouseButton.Primary)
                        OnMouseClick?.Invoke(this, (ev.Button.X, ev.Button.Y));
                    break;

                case (uint)EventType.Keydown:
                    var key = ev.Key.Keysym.Sym;
                    if (key == (int)KeyCode.F12)
                    {
                        var screenshotDir = Path.Combine("Screenshots");
                        Directory.CreateDirectory(screenshotDir);

                        var filename = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                        var fullPath = Path.Combine(screenshotDir, filename);

                        _gameRenderer.CaptureScreenshot(fullPath);
                    }
                    break;
            }
        }

        return false;
    }
}
