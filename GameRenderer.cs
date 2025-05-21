using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheAdventure.Models;
using System.IO;
using System;
using System.Collections.Generic;
using Point = Silk.NET.SDL.Point; // Explicitly use Silk.NET.SDL.Point

namespace TheAdventure;

public unsafe class GameRenderer
{
    private readonly Sdl _sdl; // Changed to readonly
    private readonly Renderer* _renderer; // Changed to readonly
    private readonly GameWindow _window; // Changed to readonly
    private readonly Camera _camera; // Changed to readonly

    private Dictionary<int, IntPtr> _texturePointers = new();
    private Dictionary<int, TextureData> _textureData = new();
    private int _textureId;

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

    // Pass Rectangle<int> by 'in' to avoid copying and satisfy warning CS9192/CS9195
    public void SetWorldBounds(in Rectangle<int> bounds)
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
            textureInfo = default;
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
                // For CreateRGBSurfaceWithFormatFrom, depth is bits per pixel. For RGBA32, it's 32.
                // Pitch is bytes per row: width * bytes_per_pixel. For RGBA32, bytes_per_pixel is 4.
                var imageSurface = _sdl.CreateRGBSurfaceWithFormatFrom((void*)data, textureInfo.Width,
                    textureInfo.Height, 32, textureInfo.Width * 4, (uint)PixelFormatEnum.Rgba32);
                if (imageSurface == null)
                {
                    throw new Exception("Failed to create surface from image data. SDL Error: " + _sdl.GetErrorS());
                }

                var imageTexture = _sdl.CreateTextureFromSurface(_renderer, imageSurface);
                _sdl.FreeSurface(imageSurface);
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

    // Use 'in' for struct parameters src and dstWorldRect
    public void RenderTexture(int textureId, in Rectangle<int> src, in Rectangle<int> dstWorldRect,
        RendererFlip flip = RendererFlip.None, double angle = 0.0, in Point center = default) // 'in Point center'
    {
        if (_texturePointers.TryGetValue(textureId, out var imageTexture))
        {
            var translatedDstScreenRect = _camera.ToScreenCoordinates(dstWorldRect);
            // The _sdl.RenderCopyEx function itself will take these.
            // If Silk.NET's wrapper expects refs for these struct parameters,
            // passing them directly as 'in' variables to this method is good.
            _sdl.RenderCopyEx(_renderer, (Texture*)imageTexture, src,
                translatedDstScreenRect, // This is a local struct variable, passed by value to RenderCopyEx wrapper
                angle,
                center, flip);
        }
    }

    // Use 'in' for struct parameters src and dstScreenRect
    public void RenderUITexture(int textureId, in Rectangle<int> src, in Rectangle<int> dstScreenRect,
        RendererFlip flip = RendererFlip.None, double angle = 0.0, in Point center = default) // 'in Point center'
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