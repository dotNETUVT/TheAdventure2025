using Silk.NET.OpenAL;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace TheAdventure.Audio;

/// <summary>
/// Wrapper around OpenAL. Supports PCM-WAV playback for SFX & BGM.
/// </summary>
public sealed class AudioManager : IDisposable
{
    private static AudioManager? _instance;
    public  static AudioManager  I => _instance ??= new AudioManager();

    private readonly ALContext _alc;
    private readonly AL        _al;
    private readonly nint      _device;
    private readonly nint      _context;

    private readonly ConcurrentDictionary<string,uint> _buffers = new();
    private readonly ConcurrentBag<uint>               _sources = new();

    private AudioManager()
    {
        _alc     = ALContext.GetApi();
        _al      = AL.GetApi();

        unsafe
        {
            _device  = (nint)_alc.OpenDevice(null);
            _context = (nint)_alc.CreateContext((Silk.NET.OpenAL.Device*)_device, null);
            _alc.MakeContextCurrent((Silk.NET.OpenAL.Context*)_context);
        }

        for (int i = 0; i < 16; i++)
            _sources.Add(_al.GenSource());
    }

    // ------------------------------------------------ WAV loader
    public void LoadWav(string key, string path)
    {
        if (_buffers.ContainsKey(key) || !File.Exists(path)) return;

        using var br = new BinaryReader(File.OpenRead(path));

        if (new string(br.ReadChars(4)) != "RIFF") throw new InvalidDataException("Not RIFF");
        br.ReadInt32();                                       // riff chunk size
        if (new string(br.ReadChars(4)) != "WAVE") throw new InvalidDataException("Not WAVE");

        ushort channels = 0, bitsPerSample = 0; int sampleRate = 0; byte[]? pcm = null;

        while (br.BaseStream.Position < br.BaseStream.Length)
        {
            string id  = new string(br.ReadChars(4));
            int    sz  = br.ReadInt32();

            switch (id)
            {
                case "fmt ":
                    ushort fmt = br.ReadUInt16();
                    channels   = br.ReadUInt16();
                    sampleRate = br.ReadInt32();
                    br.ReadInt32();            // byteRate
                    br.ReadInt16();            // blockAlign
                    bitsPerSample = br.ReadUInt16();
                    if (sz > 16) br.ReadBytes(sz - 16);
                    if (fmt != 1) throw new NotSupportedException("Only PCM WAV supported");
                    break;

                case "data":
                    pcm = br.ReadBytes(sz);
                    break;

                default:
                    br.ReadBytes(sz);          // skip extras
                    break;
            }
        }

        if (pcm is null) throw new InvalidDataException("No PCM data chunk");

        uint buf = _al.GenBuffer();
        _al.BufferData(
            buf,
            bitsPerSample == 16
               ? (channels == 2 ? BufferFormat.Stereo16 : BufferFormat.Mono16)
               : (channels == 2 ? BufferFormat.Stereo8  : BufferFormat.Mono8),
            pcm, sampleRate);

        _buffers[key] = buf;
    }

    // ------------------------------------------------ playback
    public void Play(string key, float gain = 1f, bool loop = false)
    {
        if (!_buffers.TryGetValue(key, out uint buf)) return;
        if (!_sources.TryTake(out uint src))           return;

        _al.SetSourceProperty(src, SourceInteger.Buffer,  (int)buf);
        _al.SetSourceProperty(src, SourceFloat.Gain,      gain);
        _al.SetSourceProperty(src, SourceBoolean.Looping, loop);
        _al.SourcePlay(src);

        if (!loop)
            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                _al.SourceStop(src);
                _sources.Add(src);
            });
    }

    // ------------------------------------------------ cleanup
    public void Dispose()
    {
        foreach (var s in _sources)         _al.DeleteSource(s);
        foreach (var b in _buffers.Values)  _al.DeleteBuffer(b);

        unsafe
        {
            _alc.DestroyContext((Silk.NET.OpenAL.Context*)_context);
            _alc.CloseDevice   ((Silk.NET.OpenAL.Device*) _device);
        }
    }
}
