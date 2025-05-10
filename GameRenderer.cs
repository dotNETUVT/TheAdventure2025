using System;
using System.Collections.Generic;
using System.IO;
using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheAdventure.Models;
using Point = Silk.NET.SDL.Point;

namespace TheAdventure
{
    public unsafe class GameRenderer
    {
        private readonly Sdl _sdl;
        private readonly Renderer* _renderer;
        private readonly GameWindow _window;
        private readonly Camera _camera;

        private readonly Dictionary<int, IntPtr>      _texturePointers = new();
        private readonly Dictionary<int, TextureData> _textureData     = new();
        private int _textureId = 0;

        private readonly int _heartTexId;
        private readonly int _heartW;
        private readonly int _heartH;
        private const int HEART_SCALE = 1;

        private readonly int _digitsTexId;
        private readonly int _digitW;
        private readonly int _digitH;
        private const int DIGIT_SCALE = 2;

        public GameRenderer(Sdl sdl, GameWindow window)
        {
            _sdl      = sdl;
            _window   = window;
            _renderer = (Renderer*)window.CreateRenderer();
            _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);

            var (w, h) = window.Size;
            _camera = new Camera(w, h);

            _heartTexId = LoadTexture(Path.Combine("Assets", "heart.png"), out var hd);
            _heartW     = hd.Width;
            _heartH     = hd.Height;

            _digitsTexId = LoadTexture(Path.Combine("Assets", "digits.png"), out var dd);
            _digitW      = dd.Width  / 10;
            _digitH      = dd.Height;
        }

        public (int Width, int Height) WindowSize => _window.Size;

        public void SetWorldBounds(Rectangle<int> bounds)
            => _camera.SetWorldBounds(bounds);

        public void CameraLookAt(int x, int y)
            => _camera.LookAt(x, y);

        public int LoadTexture(string fileName, out TextureData textureInfo)
        {
            using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var image = Image.Load<Rgba32>(fs);
            textureInfo = new TextureData
            {
                Width  = image.Width,
                Height = image.Height
            };

            var raw = new byte[textureInfo.Width * textureInfo.Height * 4];
            image.CopyPixelDataTo(raw);

            fixed (byte* ptr = raw)
            {
                var surf = _sdl.CreateRGBSurfaceWithFormatFrom(
                    ptr,
                    textureInfo.Width,
                    textureInfo.Height,
                    8,
                    textureInfo.Width * 4,
                    (uint)PixelFormatEnum.Rgba32
                );
                if (surf == null)
                    throw new Exception("Failed to create surface from image data.");

                var tex = _sdl.CreateTextureFromSurface(_renderer, surf);
                _sdl.FreeSurface(surf);
                if (tex == null)
                    throw new Exception("Failed to create texture from surface.");

                _textureData[_textureId]    = textureInfo;
                _texturePointers[_textureId] = (IntPtr)tex;
            }

            return _textureId++;
        }

        public void RenderTexture(int textureId,
                                  Rectangle<int> src,
                                  Rectangle<int> dst,
                                  RendererFlip flip = RendererFlip.None,
                                  double angle = 0.0,
                                  Point rotationCenter = default)
        {
            if (!_texturePointers.TryGetValue(textureId, out var texPtr))
                return;

            var screenDst = _camera.ToScreenCoordinates(dst);
            _sdl.RenderCopyEx(_renderer,
                              (Texture*)texPtr,
                              in src,
                              in screenDst,
                              angle,
                              in rotationCenter,
                              flip);
        }

        public void RenderHearts(int health, int padding = 10)
        {
            for (int i = 0; i < health; i++)
            {
                var src = new Rectangle<int>(0, 0, 36, _heartH);
                var dst = new Rectangle<int>(
                    padding + i * (36 * HEART_SCALE + 4),
                    padding,
                    36 * HEART_SCALE,
                    _heartH * HEART_SCALE
                );
                RenderTexture(_heartTexId, src, dst);
            }
        }

        public void RenderScore(int score, int padding = 10)
        {
            var s = score.ToString();
            int totalW = s.Length * (_digitW * DIGIT_SCALE);
            int startX = WindowSize.Width - padding - totalW;
            int y = padding;

            for (int i = 0; i < s.Length; i++)
            {
                int d = s[i] - '0';
                var src = new Rectangle<int>(
                    d * _digitW, 0,
                    _digitW, _digitH
                );
                var dst = new Rectangle<int>(
                    startX + i * (_digitW * DIGIT_SCALE),
                    y,
                    _digitW * DIGIT_SCALE,
                    _digitH * DIGIT_SCALE
                );
                RenderTexture(_digitsTexId, src, dst);
            }
        }

        public void SetDrawColor(byte r, byte g, byte b, byte a)
            => _sdl.SetRenderDrawColor(_renderer, r, g, b, a);

        public void ClearScreen()
            => _sdl.RenderClear(_renderer);

        public void PresentFrame()
            => _sdl.RenderPresent(_renderer);
        
        public Vector2D<int> ToWorldCoordinates(int x, int y)
            => _camera.ToWorldCoordinates(new Vector2D<int>(x, y));
    }
}
