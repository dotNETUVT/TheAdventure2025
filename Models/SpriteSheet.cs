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
        
        if (!Animations.TryGetValue(name, out var animation))
        {
            ActiveAnimation = null; 
            AnimationFinished = true;
            Console.WriteLine($"Warning: Animation '{name}' not found for sprite sheet '{FileName}'. Rendering first frame.");
            return;
        }

        ActiveAnimation = animation;
        _animationStart = DateTimeOffset.Now;
        AnimationFinished = false;
    }

    public void Render(GameRenderer renderer, (int X, int Y) dest, double angle = 0.0, Point rotationCenter = new())
    {
        if (_textureId == -1) return;
        Rectangle<int> srcRect;
        RendererFlip flip = RendererFlip.None;

        if (ActiveAnimation == null)
        {
            srcRect = new Rectangle<int>(0, 0, FrameWidth, FrameHeight);
        }
        else
        {
            flip = ActiveAnimation.Flip;
            var totalFrames = (ActiveAnimation.EndFrame.Row - ActiveAnimation.StartFrame.Row) * ColumnCount +
                ActiveAnimation.EndFrame.Col - ActiveAnimation.StartFrame.Col + 1; 
            
            var elapsedMs = (DateTimeOffset.Now - _animationStart).TotalMilliseconds;
            var frameDurationMs = ActiveAnimation.DurationMs / (double)totalFrames;
            if (frameDurationMs <= 0) frameDurationMs = ActiveAnimation.DurationMs; 

            int currentFrameIndex = (int)(elapsedMs / frameDurationMs);

            if (currentFrameIndex >= totalFrames)
            {
                AnimationFinished = true;
                if (ActiveAnimation.Loop)
                {
                    _animationStart = DateTimeOffset.Now;
                    currentFrameIndex = 0;
                    AnimationFinished = false; // Reset for loop
                }
                else
                {
                    currentFrameIndex = totalFrames - 1; // Stay on the last frame
                }
            }

            var currentRow = ActiveAnimation.StartFrame.Row + (currentFrameIndex / ColumnCount);
            var currentCol = ActiveAnimation.StartFrame.Col + (currentFrameIndex % ColumnCount);

            srcRect = new Rectangle<int>(currentCol * FrameWidth, currentRow * FrameHeight, FrameWidth, FrameHeight);
        }
        
        var destRect = new Rectangle<int>(dest.X - FrameCenter.OffsetX, dest.Y - FrameCenter.OffsetY, FrameWidth, FrameHeight);
        renderer.RenderTexture(_textureId, srcRect, destRect, flip, angle, rotationCenter);
    }
}