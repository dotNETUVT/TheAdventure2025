using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheAdventure.Models;
using System.IO;
using System;
using System.Collections.Generic;
using Point = Silk.NET.SDL.Point;

namespace TheAdventure;

public unsafe class GameRenderer
{
    private Sdl _sdl;
    private Renderer* _renderer;
    private GameWindow _window;
    private Camera _camera;

    private Dictionary<int, IntPtr> _texturePointers = new();
    private Dictionary<int, TextureData> _textureData = new();
    private int _textureId; // Internal counter for generating texture IDs

    public GameRenderer(Sdl sdl, GameWindow window)
    {
        _sdl = sdl;
        _renderer = (Renderer*)window.CreateRenderer();
        if (_renderer == null) throw new Exception("Failed to create renderer: " + _sdl.GetErrorS());
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);
        _window = window;
        var windowSize = window.Size;
        _camera = new Camera(windowSize.Width, windowSize.Height);
    }

    public void ResetCamera(int targetX, int targetY)
    {
        _camera.Reset(targetX, targetY);
    }

    public void SetWorldBounds(Rectangle<int> bounds)
    {
        _camera.SetWorldBounds(bounds);
    }

    public void CameraLookAt(int x, int y)
    {
        _camera.LookAt(x, y);
    }

    public void AdjustCameraZoom(int scrollY)
    {
        _camera.AdjustZoom(scrollY);
    }

    public int LoadTexture(string fileName, out TextureData textureInfo)
    {
        if (!File.Exists(fileName))
        {
            textureInfo = default; // Set out param to default
            throw new FileNotFoundException($"Texture file not found: {fileName}");
        }
        using (var fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var image = Image.Load<Rgba32>(fStream);
            textureInfo = new TextureData()
            {
                Width = image.Width,
                Height = image.Height
            };
            var imageRAWData = new byte[textureInfo.Width * textureInfo.Height * 4];
            image.CopyPixelDataTo(imageRAWData.AsSpan());
            fixed (byte* data = imageRAWData)
            {
                var imageSurface = _sdl.CreateRGBSurfaceWithFormatFrom(data, textureInfo.Width,
                    textureInfo.Height, 32, textureInfo.Width * 4, (uint)PixelFormatEnum.Rgba32); // Assuming 32 bit depth for RGBA32
                if (imageSurface == null)
                {
                    throw new Exception("Failed to create surface from image data. SDL Error: " + _sdl.GetErrorS());
                }

                var imageTexture = _sdl.CreateTextureFromSurface(_renderer, imageSurface);
                _sdl.FreeSurface(imageSurface); // Free surface after texture creation
                if (imageTexture == null)
                {
                    throw new Exception("Failed to create texture from surface. SDL Error: " + _sdl.GetErrorS());
                }

                _textureData[_textureId] = textureInfo;
                _texturePointers[_textureId] = (IntPtr)imageTexture;
            }
        }
        return _textureId++;
    }

    public void RenderTexture(int textureId, Rectangle<int> src, Rectangle<int> dstWorldRect,
        RendererFlip flip = RendererFlip.None, double angle = 0.0, Point center = default)
    {
        if (_texturePointers.TryGetValue(textureId, out var imageTexture))
        {
            var translatedDstScreenRect = _camera.ToScreenCoordinates(dstWorldRect);
            _sdl.RenderCopyEx(_renderer, (Texture*)imageTexture, src,
                translatedDstScreenRect,
                angle,
                center, flip);
        }
    }

    public void RenderUITexture(int textureId, Rectangle<int> src, Rectangle<int> dstScreenRect,
        RendererFlip flip = RendererFlip.None, double angle = 0.0, Point center = default)
    {
        if (_texturePointers.TryGetValue(textureId, out var imageTexture))
        {
            _sdl.RenderCopyEx(_renderer, (Texture*)imageTexture, src,
                dstScreenRect,
                angle,
                center, flip);
        }
    }

    public Vector2D<int> ToWorldCoordinates(int x, int y)
    {
        return _camera.ToWorldCoordinates(new Vector2D<int>(x, y));
    }

    public void SetDrawColor(byte r, byte g, byte b, byte a)
    {
        _sdl.SetRenderDrawColor(_renderer, r, g, b, a);
    }

    public void ClearScreen()
    {
        _sdl.RenderClear(_renderer);
    }

    public void PresentFrame()
    {
        _sdl.RenderPresent(_renderer);
    }
}