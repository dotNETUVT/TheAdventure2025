using System;
using System.Media;

public class SoundManager : IDisposable
{
    private readonly SoundPlayer _player;

    public SoundManager()
    {
        _player = new SoundPlayer("Assets/gameover.wav");
        _player.Load(); // Preload the file so it's ready instantly
    }

    public void PlayGameOverSound()
    {
        _player.Play(); // Use .PlaySync() if you want to block until it's done
    }

    public void Dispose()
    {
        _player.Dispose();
    }
}
