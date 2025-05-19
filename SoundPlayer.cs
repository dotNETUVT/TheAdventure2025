using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class SoundPlayer
{
    private static IWavePlayer? _waveOut;
    private static AudioFileReader? _reader;

    public static void Play(string filePath)
    {
        _reader?.Dispose();
        _waveOut?.Dispose();

        _reader = new AudioFileReader(filePath);
        _waveOut = new WaveOutEvent();
        _waveOut.Init(_reader);
        _waveOut.Play();
    }
}