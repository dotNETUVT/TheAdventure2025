using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheAdventure.Models;
using NAudio.Wave;
using Point = Silk.NET.SDL.Point;
using System.IO;
using System.Collections.Generic;
using System;

namespace TheAdventure;

public unsafe class GameRenderer : IDisposable
{
    private Sdl _sdl;
    private Renderer* _renderer;
    private GameWindow _window;
    private Camera _camera;

    private Dictionary<int, IntPtr> _texturePointers = new();
    private Dictionary<int, TextureData> _textureData = new();
    private int _textureIdCounter = 1;

    private byte[] _explosionAudioData;

    private int _scoreLabelTextureId;
    private int _colonTextureId;
    private int[] _digitTextureIds = new int[10];

    private int _heartTextureId;
    private int _youLostTextureId;
    private int _youWinTextureId;


    private WaveOutEvent? _backgroundMusicPlayer;
    private MediaFoundationReader? _backgroundMusicReader;


    public GameRenderer(Sdl sdl, GameWindow window)
    {
        _sdl = sdl;
        _window = window;

        IntPtr rendererPtr = _window.CreateRenderer();
        if (rendererPtr == IntPtr.Zero)
        {
            var ex = _sdl.GetErrorAsException();
            if (ex != null) throw ex;
            throw new Exception("Failed to create renderer from GameWindow.");
        }
        _renderer = (Renderer*)rendererPtr;
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);

        var windowSize = _window.Size;
        _camera = new Camera(windowSize.Width, windowSize.Height);

        string explosionPath = Path.Combine("Assets", "game-explosion-sound-effect.wav");
        _explosionAudioData = File.ReadAllBytes(explosionPath);

        LoadUIAssets();
        InitializeBackgroundMusic(Path.Combine("Assets", "the-wandering-samurai-344699.mp3"));
    }

    private void LoadUIAssets()
    {
        _scoreLabelTextureId = LoadTexture(Path.Combine("Assets", "label-score.png"), out _);
        _colonTextureId = LoadTexture(Path.Combine("Assets", "colon.png"), out _);
        for (int i = 0; i < 10; ++i)
        {
            _digitTextureIds[i] = LoadTexture(Path.Combine("Assets", $"{i}.png"), out _);
        }
        _heartTextureId = LoadTexture(Path.Combine("Assets", "heart.png"), out _);
        _youLostTextureId = LoadTexture(Path.Combine("Assets", "you-lost.png"), out _);
        _youWinTextureId = LoadTexture(Path.Combine("Assets", "you-win.png"), out _);
    }

    private void InitializeBackgroundMusic(string filePath)
    {
        try
        {
            _backgroundMusicPlayer = new WaveOutEvent();
            _backgroundMusicReader = new MediaFoundationReader(filePath);
            _backgroundMusicPlayer.Init(_backgroundMusicReader);
            _backgroundMusicPlayer.PlaybackStopped += OnBackgroundMusicPlaybackStopped;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading background music: {ex.Message}");
            _backgroundMusicPlayer?.Dispose();
            _backgroundMusicReader?.Dispose();
            _backgroundMusicPlayer = null;
            _backgroundMusicReader = null;
        }
    }


    private void OnBackgroundMusicPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception == null && _backgroundMusicReader != null && _backgroundMusicPlayer != null)
        {
            _backgroundMusicReader.Position = 0;
            _backgroundMusicPlayer.Play();
        }
        else if (e.Exception != null)
        {
            Console.WriteLine($"Background music playback error: {e.Exception.Message}");
        }
    }

    public void PlayBackgroundMusic()
    {
        if (_backgroundMusicPlayer != null && _backgroundMusicReader != null)
        {

            _backgroundMusicPlayer.PlaybackStopped -= OnBackgroundMusicPlaybackStopped;
            _backgroundMusicPlayer.PlaybackStopped += OnBackgroundMusicPlaybackStopped;

            if (_backgroundMusicPlayer.PlaybackState != PlaybackState.Playing)
            {
                if (_backgroundMusicReader.Position >= _backgroundMusicReader.Length)
                {
                    _backgroundMusicReader.Position = 0;
                }
                _backgroundMusicPlayer.Play();
            }
        }
    }

    public void StopBackgroundMusic()
    {
        if (_backgroundMusicPlayer != null)
        {
            _backgroundMusicPlayer.PlaybackStopped -= OnBackgroundMusicPlaybackStopped;
            _backgroundMusicPlayer.Stop();
        }
    }

    public void SetWorldBounds(Rectangle<int> bounds) => _camera.SetWorldBounds(bounds);
    public void CameraLookAt(int x, int y) => _camera.LookAt(x, y);

    public int LoadTexture(string fileName, out TextureData textureInfo)
    {
        int currentTextureId = _textureIdCounter++;
        using (var fStream = new FileStream(fileName, FileMode.Open))
        {
            var image = Image.Load<Rgba32>(fStream);
            textureInfo = new TextureData() { Width = image.Width, Height = image.Height };
            var imageRAWData = new byte[textureInfo.Width * textureInfo.Height * 4];
            image.CopyPixelDataTo(imageRAWData.AsSpan());
            fixed (byte* data = imageRAWData)
            {
                var imageSurface = _sdl.CreateRGBSurfaceWithFormatFrom(data, textureInfo.Width, textureInfo.Height, 32, textureInfo.Width * 4, (uint)PixelFormatEnum.Rgba32);
                if (imageSurface == null) throw new Exception($"Failed to create surface for {fileName}. SDL Error: {_sdl.GetErrorS()}");
                var imageTexture = _sdl.CreateTextureFromSurface(_renderer, imageSurface);
                if (imageTexture == null)
                {
                    _sdl.FreeSurface(imageSurface);
                    throw new Exception($"Failed to create texture for {fileName}. SDL Error: {_sdl.GetErrorS()}");
                }
                _sdl.FreeSurface(imageSurface);
                _textureData[currentTextureId] = textureInfo;
                _texturePointers[currentTextureId] = (IntPtr)imageTexture;
            }
        }
        return currentTextureId;
    }

    public void RenderTexture(int textureId, Rectangle<int> src, Rectangle<int> dst, RendererFlip flip = RendererFlip.None, double angle = 0.0, Point center = default, bool isUiElement = false)
    {
        if (_texturePointers.TryGetValue(textureId, out var imageTexture) && imageTexture != IntPtr.Zero)
        {
            var finalDst = isUiElement ? dst : _camera.ToScreenCoordinates(dst);
            _sdl.RenderCopyEx(_renderer, (Texture*)imageTexture, in src, in finalDst, angle, in center, flip);
        }
    }

    public Vector2D<int> ToWorldCoordinates(int x, int y) => _camera.ToWorldCoordinates(new Vector2D<int>(x, y));
    public void SetDrawColor(byte r, byte g, byte b, byte a) => _sdl.SetRenderDrawColor(_renderer, r, g, b, a);
    public void ClearScreen() => _sdl.RenderClear(_renderer);
    public void PresentFrame() => _sdl.RenderPresent(_renderer);

    public void PlayExplosionSound()
    {
        var stream = new MemoryStream(_explosionAudioData);
        var reader = new WaveFileReader(stream);
        var outputDevice = new WaveOutEvent();
        outputDevice.Init(reader);
        outputDevice.Play();
        outputDevice.PlaybackStopped += (s, e) => { outputDevice.Dispose(); reader.Dispose(); stream.Dispose(); };
    }

    public void RenderScoreOnScreen(int score, int x, int y)
    {
        int currentX = x;
        TextureData labelData = _textureData[_scoreLabelTextureId];
        RenderTexture(_scoreLabelTextureId, new Rectangle<int>(0, 0, labelData.Width, labelData.Height), new Rectangle<int>(currentX, y, labelData.Width, labelData.Height), isUiElement: true);
        currentX += labelData.Width + 1;

        TextureData colonData = _textureData[_colonTextureId];
        RenderTexture(_colonTextureId, new Rectangle<int>(0, 0, colonData.Width, colonData.Height), new Rectangle<int>(currentX, y, colonData.Width, colonData.Height), isUiElement: true);
        currentX += colonData.Width + 2;

        string scoreString = score.ToString();
        if (string.IsNullOrEmpty(scoreString)) scoreString = "0";
        foreach (char digitChar in scoreString)
        {
            if (char.IsDigit(digitChar))
            {
                int digit = digitChar - '0';
                if (digit >= 0 && digit <= 9)
                {
                    int digitTextureId = _digitTextureIds[digit];
                    TextureData digitData = _textureData[digitTextureId];
                    RenderTexture(digitTextureId, new Rectangle<int>(0, 0, digitData.Width, digitData.Height), new Rectangle<int>(currentX, y, digitData.Width, digitData.Height), isUiElement: true);
                    currentX += digitData.Width + 1;
                }
            }
        }
    }

    public int CalculateHealthDisplayWidth(int health)
    {
        int totalWidth = 0;
        if (_textureData.TryGetValue(_heartTextureId, out var heartData)) totalWidth += heartData.Width + 1;
        if (_textureData.TryGetValue(_colonTextureId, out var colonData)) totalWidth += colonData.Width + 2;
        string healthString = health.ToString();
        if (string.IsNullOrEmpty(healthString)) healthString = "0";
        foreach (char digitChar in healthString)
        {
            if (char.IsDigit(digitChar) && (digitChar - '0') is int digit && digit >= 0 && digit <= 9)
            {
                if (_textureData.TryGetValue(_digitTextureIds[digit], out var digitData)) totalWidth += digitData.Width + 1;
            }
        }
        if (healthString.Length > 0) totalWidth -= 1;
        return totalWidth;
    }

    public void RenderHealthOnScreen(int health, int x, int y)
    {
        int currentX = x;
        TextureData heartData = _textureData[_heartTextureId];
        RenderTexture(_heartTextureId, new Rectangle<int>(0, 0, heartData.Width, heartData.Height), new Rectangle<int>(currentX, y, heartData.Width, heartData.Height), isUiElement: true);
        currentX += heartData.Width + 1;

        TextureData colonData = _textureData[_colonTextureId];
        RenderTexture(_colonTextureId, new Rectangle<int>(0, 0, colonData.Width, colonData.Height), new Rectangle<int>(currentX, y, colonData.Width, colonData.Height), isUiElement: true);
        currentX += colonData.Width + 2;

        string healthString = health.ToString();
        if (string.IsNullOrEmpty(healthString)) healthString = "0";
        foreach (char digitChar in healthString)
        {
            if (char.IsDigit(digitChar) && (digitChar - '0') is int digit && digit >= 0 && digit <= 9)
            {
                int digitTextureId = _digitTextureIds[digit];
                TextureData digitData = _textureData[digitTextureId];
                RenderTexture(digitTextureId, new Rectangle<int>(0, 0, digitData.Width, digitData.Height), new Rectangle<int>(currentX, y, digitData.Width, digitData.Height), isUiElement: true);
                currentX += digitData.Width + 1;
            }
        }
    }

    public void RenderYouLostScreen()
    {
        TextureData youLostData = _textureData[_youLostTextureId];
        var windowSize = _window.Size;
        int x = (windowSize.Width - youLostData.Width) / 2;
        int y = (windowSize.Height - youLostData.Height) / 2;
        RenderTexture(_youLostTextureId, new Rectangle<int>(0, 0, youLostData.Width, youLostData.Height), new Rectangle<int>(x, y, youLostData.Width, youLostData.Height), isUiElement: true);
    }

    public void RenderYouWinScreen()
    {
        TextureData youWinData = _textureData[_youWinTextureId];
        var windowSize = _window.Size;
        int x = (windowSize.Width - youWinData.Width) / 2;
        int y = (windowSize.Height - youWinData.Height) / 2;
        RenderTexture(_youWinTextureId, new Rectangle<int>(0, 0, youWinData.Width, youWinData.Height), new Rectangle<int>(x, y, youWinData.Width, youWinData.Height), isUiElement: true);
    }

    public int GetWindowWidth() => _window.Size.Width;

    public void Dispose()
    {
        StopBackgroundMusic();
        _backgroundMusicPlayer?.Dispose();
        _backgroundMusicPlayer = null;
        _backgroundMusicReader?.Dispose();
        _backgroundMusicReader = null;
    }
}