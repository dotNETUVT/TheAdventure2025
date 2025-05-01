using Silk.NET.Maths;
using Silk.NET.SDL;
using System;

namespace TheAdventure.Models;

public class SpriteSheet
{
    public class Animation
    {
        public (int Row, int Col) StartFrame { get; init; }
        public (int Row, int Col) EndFrame { get; init; }
        public RendererFlip Flip { get; init; } = RendererFlip.None;
        public int DurationMs { get; init; }
        public bool Loop { get; init; }
    }

    public int RowCount { get; init; }
    public int ColumnCount { get; init; }

    public int FrameWidth { get; init; }
    public int FrameHeight { get; init; }
    public (int OffsetX, int OffsetY) FrameCenter { get; init; }

    public Animation? ActiveAnimation { get; private set; }
    public Dictionary<string, Animation> Animations { get; init; } = new();

    private int _textureId;
    private DateTimeOffset _animationStart = DateTimeOffset.MinValue;
    private string _currentAnimationName = "";
    private bool _isPaused = false;

    public SpriteSheet(GameRenderer renderer, string fileName, int rowCount, int columnCount, int frameWidth,
        int frameHeight, (int OffsetX, int OffsetY) frameCenter)
    {
        _textureId = renderer.LoadTexture(fileName, out var textureData);

        RowCount = rowCount;
        ColumnCount = columnCount;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        FrameCenter = frameCenter;
    }

    public void ActivateAnimation(string name)
    {
        if (!Animations.TryGetValue(name, out var animation)) return;

        bool resetTimer = _currentAnimationName != name || _animationStart == DateTimeOffset.MinValue;
        
        ActiveAnimation = animation;
        _currentAnimationName = name;
        
        if (resetTimer)
        {
            _animationStart = DateTimeOffset.Now;
        }
    }

    public void ForceRestartAnimation()
    {
        if (ActiveAnimation != null)
        {
            _animationStart = DateTimeOffset.Now;
        }
    }

    public void PauseAnimation()
    {
        _isPaused = true;
    }
    
    public void ResumeAnimation()
    {
        _isPaused = false;
        _animationStart = DateTimeOffset.Now;
    }

    public void Render(GameRenderer renderer, (int X, int Y) dest, double angle = 0.0, Point rotationCenter = new())
    {
        if (ActiveAnimation == null)
        {
            renderer.RenderTexture(_textureId, new Rectangle<int>(0, 0, FrameWidth, FrameHeight),
                new Rectangle<int>(dest.X - FrameCenter.OffsetX, dest.Y - FrameCenter.OffsetY, FrameWidth, FrameHeight),
                RendererFlip.None, angle, rotationCenter);
        }
        else
        {
            var totalFramesInAnimation = (ActiveAnimation.EndFrame.Row * ColumnCount + ActiveAnimation.EndFrame.Col) -
                                         (ActiveAnimation.StartFrame.Row * ColumnCount + ActiveAnimation.StartFrame.Col) + 1;

            var frameDuration = ActiveAnimation.DurationMs / (double)totalFramesInAnimation;
            if (frameDuration <= 0) frameDuration = 100;

            int currentFrameIndex = 0;
            
            if (!_isPaused)
            {
                var elapsedMs = (DateTimeOffset.Now - _animationStart).TotalMilliseconds;
                currentFrameIndex = (int)(elapsedMs / frameDuration);

                if (currentFrameIndex >= totalFramesInAnimation)
                {
                    if (ActiveAnimation.Loop)
                    {
                        currentFrameIndex %= totalFramesInAnimation;
                        
                        double animationDuration = frameDuration * totalFramesInAnimation;
                        double overflowTime = elapsedMs % animationDuration;
                        _animationStart = DateTimeOffset.Now.AddMilliseconds(-overflowTime);
                    }
                    else
                    {
                        currentFrameIndex = totalFramesInAnimation - 1;
                    }
                }
            }

            var startFrameLinearIndex = ActiveAnimation.StartFrame.Row * ColumnCount + ActiveAnimation.StartFrame.Col;
            var currentFrameLinearIndex = startFrameLinearIndex + currentFrameIndex;

            var currentRow = currentFrameLinearIndex / ColumnCount;
            var currentCol = currentFrameLinearIndex % ColumnCount;

            renderer.RenderTexture(_textureId,
                new Rectangle<int>(currentCol * FrameWidth, currentRow * FrameHeight, FrameWidth, FrameHeight),
                new Rectangle<int>(dest.X - FrameCenter.OffsetX, dest.Y - FrameCenter.OffsetY, FrameWidth, FrameHeight),
                ActiveAnimation.Flip, angle, rotationCenter);
        }
    }
}