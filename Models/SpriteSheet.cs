using System.Text.Json;
using Silk.NET.Maths;
using Silk.NET.SDL;

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

    public string? FileName { get; set; }

    public Animation? ActiveAnimation { get; set; }
    public Dictionary<string, Animation> Animations { get; set; } = new();
    
    public bool AnimationFinished { get; private set; }

    private int _textureId = -1;
    private DateTimeOffset _animationStart = DateTimeOffset.MinValue;

    public static SpriteSheet Load(GameRenderer renderer, string fileName, string directory)
    {
        var json = File.ReadAllText(Path.Combine(directory, fileName));
        var spriteSheet = JsonSerializer.Deserialize<SpriteSheet>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        if (spriteSheet == null)
        {
            throw new Exception($"Failed to load sprite sheet: {fileName}");
        }

        if (spriteSheet.FileName == null)
        {
            throw new Exception($"Sprite sheet {fileName} does not have a file name.");
        }

        if (spriteSheet.FrameWidth <= 0 || spriteSheet.FrameHeight <= 0)
        {
            throw new Exception($"Sprite sheet {fileName} has invalid frame dimensions.");
        }

        if (spriteSheet.RowCount <= 0 || spriteSheet.ColumnCount <= 0)
        {
            throw new Exception($"Sprite sheet {fileName} has invalid row/column count.");
        }

        spriteSheet._textureId = renderer.LoadTexture(Path.Combine(directory, spriteSheet.FileName), out _);
        if (spriteSheet._textureId == -1)
        {
            throw new Exception($"Failed to load texture for sprite sheet: {spriteSheet.FileName}");
        }

        return spriteSheet;
    }

    public void ActivateAnimation(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            AnimationFinished = true;
            ActiveAnimation = null;
            return;
        }
        
        if (!Animations.TryGetValue(name, out var animation)) return;

        ActiveAnimation = animation;
        _animationStart = DateTimeOffset.Now;
        AnimationFinished = false;
    }

 public void Render(GameRenderer renderer, (int X, int Y) dest, double angle = 0.0, Point rotationCenter = new())
{
    const int SCALE = 2;
    var w = FrameWidth * SCALE;
    var h = FrameHeight * SCALE;
    var cx = FrameCenter.OffsetX * SCALE;
    var cy = FrameCenter.OffsetY * SCALE;

    if (ActiveAnimation == null)
    {
        renderer.RenderTexture(
            _textureId,
            new Rectangle<int>(0, 0, FrameWidth, FrameHeight),
            new Rectangle<int>(dest.X - cx,
                              dest.Y - cy,
                              w,
                              h),
            RendererFlip.None,
            angle,
            rotationCenter
        );
    }
    else
    {
        var totalFrames = (ActiveAnimation.EndFrame.Row - ActiveAnimation.StartFrame.Row) * ColumnCount +
                          ActiveAnimation.EndFrame.Col - ActiveAnimation.StartFrame.Col;
        var currentFrame = (int)((DateTimeOffset.Now - _animationStart).TotalMilliseconds /
                                 (ActiveAnimation.DurationMs / (double)totalFrames));
        if (currentFrame > totalFrames)
        {
            AnimationFinished = true;
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

        renderer.RenderTexture(
            _textureId,
            new Rectangle<int>(
                currentCol * FrameWidth,
                currentRow * FrameHeight,
                FrameWidth,
                FrameHeight
            ),
            new Rectangle<int>(
                dest.X - cx,
                dest.Y - cy,
                w,
                h
            ),
            ActiveAnimation.Flip,
            angle,
            rotationCenter
        );
    }
}
}