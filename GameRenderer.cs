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

        // texture storage
        private readonly Dictionary<int, IntPtr> _texturePointers = new();
        private readonly Dictionary<int, TextureData> _textureData = new();
        private int _textureId = 0;

        // digit‐sheet info
        private readonly int _digitsTexId;
        private readonly int _digitW;
        private readonly int _digitH;
        private const int DIGIT_SCALE = 2;

        public GameRenderer(Sdl sdl, GameWindow window)
        {
            _sdl    = sdl;
            _window = window;

            _renderer = (Renderer*)window.CreateRenderer();
            _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);

            var (w, h) = window.Size;
            _camera = new Camera(w, h);

            // load digit sprite‐sheet (10 cols in digits.png)
            _digitsTexId = LoadTexture(Path.Combine("Assets", "digits.png"), out var td);
            _digitW = td.Width  / 10;
            _digitH = td.Height;
        }

        public (int Width, int Height) WindowSize => _window.Size;

        public void SetWorldBounds(Rectangle<int> bounds)
            => _camera.SetWorldBounds(bounds);

        public void CameraLookAt(int x, int y)
            => _camera.LookAt(x, y);

        /// <summary>
        /// Loads a PNG via ImageSharp into an SDL_Texture.
        /// </summary>
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
                    8,                        // bit depth per channel
                    textureInfo.Width * 4,    // pitch
                    (uint)PixelFormatEnum.Rgba32
                );
                if (surf == null)
                    throw new Exception("Failed to create surface from image data.");

                var tex = _sdl.CreateTextureFromSurface(_renderer, surf);
                if (tex == null)
                {
                    _sdl.FreeSurface(surf);
                    throw new Exception("Failed to create texture from surface.");
                }

                _sdl.FreeSurface(surf);
                _textureData[_textureId]    = textureInfo;
                _texturePointers[_textureId] = (IntPtr)tex;
            }

            return _textureId++;
        }
        
        public void RenderTexture(
            int textureId,
            Rectangle<int> src,
            Rectangle<int> dst,
            RendererFlip flip = RendererFlip.None,
            double angle = 0.0,
            Point rotationCenter = default
        )
        {
            if (!_texturePointers.TryGetValue(textureId, out var texPtr))
                return;

            var screenDst = _camera.ToScreenCoordinates(dst);
            _sdl.RenderCopyEx(
                _renderer,
                (Texture*)texPtr,
                in src,
                in screenDst,
                angle,
                in rotationCenter,
                flip
            );
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
        
        public Vector2D<int> ToWorldCoordinates(Vector2D<int> screenPoint)
            => _camera.ToWorldCoordinates(screenPoint);
    }
}
