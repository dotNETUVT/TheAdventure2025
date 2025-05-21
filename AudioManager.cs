using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;

namespace TheAdventure;

public class AudioManagerNAudio : IDisposable
{
    private WaveOutEvent? _musicOutputDevice;
    private AudioFileReader? _musicFileReader;
    private string? _currentMusicName;

    private Dictionary<string, AudioFileReader?> _soundEffectReaders;
    private List<WaveOutEvent> _activeSoundEffectPlayers;

    public float MusicVolume { get; set; } = 0.5f;
    public float SoundEffectVolume { get; set; } = 0.7f;

    public AudioManagerNAudio()
    {
        _soundEffectReaders = new Dictionary<string, AudioFileReader?>();
        _activeSoundEffectPlayers = new List<WaveOutEvent>();
    }

    public bool LoadSoundEffect(string filePath, string name)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"AudioManagerNAudio: Sound effect file not found: {filePath}");
            return false;
        }
        try
        {
            if (_soundEffectReaders.ContainsKey(name))
            {
                _soundEffectReaders[name]?.Dispose();
            }
            _soundEffectReaders[name] = new AudioFileReader(filePath);
            Console.WriteLine($"AudioManagerNAudio: Sound effect '{name}' loaded from '{filePath}'.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AudioManagerNAudio: Failed to load sound effect '{name}'. Error: {ex.Message}");
            return false;
        }
    }

    public bool LoadMusic(string filePath, string name)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"AudioManagerNAudio: Music file not found: {filePath}");
            return false;
        }
        try
        {
            StopMusic();
            _musicFileReader?.Dispose();

            _musicFileReader = new AudioFileReader(filePath);
            _currentMusicName = name;
            Console.WriteLine($"AudioManagerNAudio: Music '{name}' loaded from '{filePath}'.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AudioManagerNAudio: Failed to load music '{name}'. Error: {ex.Message}");
            return false;
        }
    }

    public void PlaySoundEffect(string name)
    {
        if (_soundEffectReaders.TryGetValue(name, out AudioFileReader? soundReaderTemplate) && soundReaderTemplate != null)
        {
            try
            {
                var playbackReader = new AudioFileReader(soundReaderTemplate.FileName);
                playbackReader.Volume = SoundEffectVolume;

                var waveOut = new WaveOutEvent();
                _activeSoundEffectPlayers.Add(waveOut);

                waveOut.PlaybackStopped += (s, a) =>
                {
                    playbackReader.Dispose();
                    waveOut.Dispose();
                    _activeSoundEffectPlayers.Remove(waveOut);
                };

                waveOut.Init(playbackReader);
                waveOut.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AudioManagerNAudio: Error playing sound effect '{name}'. Error: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"AudioManagerNAudio: Sound effect '{name}' not loaded.");
        }
    }

    public void PlayMusic(bool loop = true)
    {
        if (_musicFileReader == null)
        {
            Console.WriteLine("AudioManagerNAudio: No music loaded to play.");
            return;
        }

        _musicOutputDevice ??= new WaveOutEvent();

        _musicOutputDevice.PlaybackStopped -= OnMusicPlaybackStopped;
        if (loop)
        {
            _musicOutputDevice.PlaybackStopped += OnMusicPlaybackStopped;
        }

        try
        {
            _musicFileReader.Position = 0;
            _musicFileReader.Volume = MusicVolume;
            _musicOutputDevice.Init(_musicFileReader);
            _musicOutputDevice.Play();
            Console.WriteLine($"AudioManagerNAudio: Playing music '{_currentMusicName}'. Loop: {loop}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AudioManagerNAudio: Error playing music '{_currentMusicName}'. Error: {ex.Message}");
        }
    }

    private void OnMusicPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (_musicFileReader != null && _musicOutputDevice != null && e.Exception == null)
        {
            Console.WriteLine($"AudioManagerNAudio: Music '{_currentMusicName}' finished, looping.");
            _musicFileReader.Position = 0;
            _musicOutputDevice.Play();
        }
        else if (e.Exception != null)
        {
            Console.WriteLine($"AudioManagerNAudio: Music playback stopped with error: {e.Exception.Message}");
        }
    }

    public void StopMusic()
    {
        _musicOutputDevice?.Stop();
        Console.WriteLine("AudioManagerNAudio: Music stopped.");
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Math.Clamp(volume, 0.0f, 1.0f);
        if (_musicFileReader != null)
        {
            _musicFileReader.Volume = MusicVolume;
        }
    }

    public void SetSoundEffectVolume(float volume)
    {
        SoundEffectVolume = Math.Clamp(volume, 0.0f, 1.0f);
    }

    public void Dispose()
    {
        Console.WriteLine("AudioManagerNAudio: Disposing all audio resources...");
        StopMusic();
        _musicOutputDevice?.Dispose();
        _musicOutputDevice = null;
        _musicFileReader?.Dispose();
        _musicFileReader = null;

        foreach (var player in _activeSoundEffectPlayers.ToList())
        {
            player.Dispose();
        }
        _activeSoundEffectPlayers.Clear();

        foreach (var reader in _soundEffectReaders.Values)
        {
            reader?.Dispose();
        }
        _soundEffectReaders.Clear();

        Console.WriteLine("AudioManagerNAudio: Disposed.");
        GC.SuppressFinalize(this);
    }

    ~AudioManagerNAudio()
    {
        Dispose();
    }
}