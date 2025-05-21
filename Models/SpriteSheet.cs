using System.Text.Json;
using Silk.NET.Maths;
using Silk.NET.SDL;
using System.IO;        // For File, Path
using System.Collections.Generic; // For Dictionary
using System;           // For Exception, DateTimeOffset

namespace TheAdventure.Models;

public class SpriteSheet
{
    public struct Position
    {
        public int Row { get; set; }
        public int Col { get; set; }
    }

    public struct Offset
    {
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
    }

    public class Animation
    {
        public Position StartFrame { get; set; }
        public Position EndFrame { get; set; }
        public RendererFlip Flip { get; set; } = RendererFlip.None;
        public int DurationMs { get; set; }
        public bool Loop { get; set; }
    }

    public int RowCount { get; set; }
    public int ColumnCount { get; set; }

    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public Offset FrameCenter { get; set; }

    public string? FileName { get; set; } // Image file name

    public Animation? ActiveAnimation { get; set; }
    public Dictionary<string, Animation> Animations { get; set; } = new();

    public bool AnimationFinished { get; private set; }

    private int _textureId = -1;
    private DateTimeOffset _animationStart = DateTimeOffset.MinValue;

    public static SpriteSheet Load(GameRenderer renderer, string jsonFileName, string directory)
    {
        var json = File.ReadAllText(Path.Combine(directory, jsonFileName));
        var spriteSheet = JsonSerializer.Deserialize<SpriteSheet>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        if (spriteSheet == null)
        {
            throw new Exception($"Failed to load sprite sheet: {jsonFileName}");
        }
        if (string.IsNullOrEmpty(spriteSheet.FileName))
        {
            throw new Exception($"Sprite sheet {jsonFileName} does not have a file name for its texture.");
        }
        if (spriteSheet.FrameWidth <= 0 || spriteSheet.FrameHeight <= 0)
        {
            throw new Exception($"Sprite sheet {jsonFileName} has invalid frame dimensions.");
        }
        if (spriteSheet.RowCount <= 0 || spriteSheet.ColumnCount <= 0)
        {
            // This might not be strictly necessary if animations define frames absolutely,
            // but good for validation if calculations depend on it.
            // For now, let's assume it's good to have.
        }

        spriteSheet._textureId = renderer.LoadTexture(Path.Combine(directory, spriteSheet.FileName), out _);
        if (spriteSheet._textureId == -1) // Assuming -1 is an invalid ID from LoadTexture
        {
            throw new Exception($"Failed to load texture for sprite sheet: {spriteSheet.FileName}");
        }
        return spriteSheet;
    }

    public void ActivateAnimation(string? name)
    {
        AnimationFinished = false; // Reset this flag whenever a new animation is activated (or cleared)
        if (string.IsNullOrEmpty(name))
        {
            ActiveAnimation = null;
            AnimationFinished = true; // No animation, so it's "finished"
            return;
        }

        if (!Animations.TryGetValue(name, out var animation))
        {
            // Optionally, log a warning or throw if animation name not found
            Console.WriteLine($"Warning: Animation '{name}' not found in sprite sheet for {FileName}.");
            ActiveAnimation = null; // Fallback to no animation
            AnimationFinished = true;
            return;
        }

        ActiveAnimation = animation;
        _animationStart = DateTimeOffset.Now;
        // AnimationFinished is already set to false above
    }

    public void Render(GameRenderer renderer, (int X, int Y) dest, double angle = 0.0, Point rotationCenter = new())
    {
        if (_textureId == -1) return; // Texture not loaded

        if (ActiveAnimation == null)
        {
            // Default rendering: first frame or a defined "still" frame if no animation active
            var srcRect = new Rectangle<int>(0, 0, FrameWidth, FrameHeight);
            var dstRect = new Rectangle<int>(dest.X - FrameCenter.OffsetX, dest.Y - FrameCenter.OffsetY, FrameWidth, FrameHeight);
            renderer.RenderTexture(_textureId, srcRect, dstRect, RendererFlip.None, angle, rotationCenter);
        }
        else
        {
            // Animation logic
            var totalFramesInAnimation = (ActiveAnimation.EndFrame.Row - ActiveAnimation.StartFrame.Row) * ColumnCount +
                                         (ActiveAnimation.EndFrame.Col - ActiveAnimation.StartFrame.Col) + 1; // +1 because EndFrame is inclusive

            if (totalFramesInAnimation <= 0) totalFramesInAnimation = 1; // Avoid division by zero if animation is single frame

            var timePerFrame = ActiveAnimation.DurationMs / (double)totalFramesInAnimation;
            if (timePerFrame <= 0) timePerFrame = ActiveAnimation.DurationMs; // Handle single frame animations with duration

            var elapsedMs = (DateTimeOffset.Now - _animationStart).TotalMilliseconds;
            var currentFrameIndex = (int)(elapsedMs / timePerFrame);

            if (currentFrameIndex >= totalFramesInAnimation)
            {
                if (ActiveAnimation.Loop)
                {
                    _animationStart = DateTimeOffset.Now; // Restart animation
                    currentFrameIndex = 0;
                }
                else
                {
                    currentFrameIndex = totalFramesInAnimation - 1; // Clamp to last frame
                    AnimationFinished = true;
                }
            }

            var startFrameLinearIndex = ActiveAnimation.StartFrame.Row * ColumnCount + ActiveAnimation.StartFrame.Col;
            var currentLinearIndex = startFrameLinearIndex + currentFrameIndex;

            var currentRow = currentLinearIndex / ColumnCount;
            var currentCol = currentLinearIndex % ColumnCount;

            var srcRect = new Rectangle<int>(currentCol * FrameWidth, currentRow * FrameHeight, FrameWidth, FrameHeight);
            var dstRect = new Rectangle<int>(dest.X - FrameCenter.OffsetX, dest.Y - FrameCenter.OffsetY, FrameWidth, FrameHeight);

            renderer.RenderTexture(_textureId, srcRect, dstRect, ActiveAnimation.Flip, angle, rotationCenter);
        }
    }
}