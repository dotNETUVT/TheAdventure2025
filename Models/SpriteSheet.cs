using Silk.NET.Maths;
using Silk.NET.SDL;

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

        ActiveAnimation = animation;
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
            var totalFrames = (ActiveAnimation.EndFrame.Row - ActiveAnimation.StartFrame.Row) * ColumnCount +
                ActiveAnimation.EndFrame.Col - ActiveAnimation.StartFrame.Col;
            var currentFrame = (int)((DateTimeOffset.Now - _animationStart).TotalMilliseconds /
                                     (ActiveAnimation.DurationMs / (double)totalFrames));
            if (currentFrame > totalFrames)
            {
                if (ActiveAnimation.Loop)
                {
                    _animationStart = DateTimeOffset.Now;
                    currentFrame = 0;
                }
                else
                {
                    currentFrame = totalFrames;
                }
            }

            var currentRow = ActiveAnimation.StartFrame.Row + currentFrame / ColumnCount;
            var currentCol = ActiveAnimation.StartFrame.Col + currentFrame % ColumnCount;

            renderer.RenderTexture(_textureId,
                new Rectangle<int>(currentCol * FrameWidth, currentRow * FrameHeight, FrameWidth, FrameHeight),
                new Rectangle<int>(dest.X - FrameCenter.OffsetX, dest.Y - FrameCenter.OffsetY, FrameWidth, FrameHeight),
                ActiveAnimation.Flip, angle, rotationCenter);
        }
    }
}