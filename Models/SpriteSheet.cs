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
        public int DurationMs { get; set; } = 1000;
        public bool Loop { get; set; } = true;
    }

    public int RowCount { get; set; }
    public int ColumnCount { get; set; }

    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public Offset FrameCenter { get; set; }

    public string? FileName { get; set; }

    public Dictionary<string, Animation> Animations { get; set; } = new();
    public Animation? ActiveAnimation { get; private set; }

    private int _textureId = -1;
    private DateTimeOffset _animationStart = DateTimeOffset.MinValue;

    // Constructor for hardcoded use
    public SpriteSheet(GameRenderer renderer, string filePath, int rowCount, int columnCount,
                       int frameWidth, int frameHeight, (int OffsetX, int OffsetY) frameCenter)
    {
        RowCount = rowCount;
        ColumnCount = columnCount;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        FrameCenter = new Offset { OffsetX = frameCenter.OffsetX, OffsetY = frameCenter.OffsetY };

        _textureId = renderer.LoadTexture(filePath, out _);
        if (_textureId == -1)
            throw new Exception($"Failed to load texture: {filePath}");
    }

    public SpriteSheet() { }

    public static SpriteSheet Load(GameRenderer renderer, string jsonFile, string directory)
    {
        var path = Path.Combine(directory, jsonFile);
        var json = File.ReadAllText(path);

        var spriteSheet = JsonSerializer.Deserialize<SpriteSheet>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new Exception($"Failed to parse JSON: {jsonFile}");

        if (spriteSheet.FileName == null)
            throw new Exception($"Missing 'FileName' in sprite sheet JSON: {jsonFile}");

        var fullTexturePath = Path.Combine(directory, spriteSheet.FileName);
        spriteSheet._textureId = renderer.LoadTexture(fullTexturePath, out _);
        if (spriteSheet._textureId == -1)
            throw new Exception($"Failed to load texture: {spriteSheet.FileName}");

        return spriteSheet;
    }

    public void ActivateAnimation(string name)
    {
        if (!Animations.TryGetValue(name, out var animation))
            throw new Exception($"Animation '{name}' not found.");

        ActiveAnimation = animation;
        _animationStart = DateTimeOffset.Now;
    }

    public void Render(GameRenderer renderer, (int X, int Y) dest, double angle = 0.0,
                       Point rotationCenter = new(), int? overrideWidth = null, int? overrideHeight = null)
    {
        int width = overrideWidth ?? FrameWidth;
        int height = overrideHeight ?? FrameHeight;

        int dstX = dest.X - FrameCenter.OffsetX;
        int dstY = dest.Y - FrameCenter.OffsetY;

        if (ActiveAnimation == null)
        {
            renderer.RenderTexture(_textureId,
                new Rectangle<int>(0, 0, FrameWidth, FrameHeight),
                new Rectangle<int>(dstX, dstY, width, height),
                RendererFlip.None, angle, rotationCenter);
            return;
        }

        var anim = ActiveAnimation;
        int totalFrames = (anim.EndFrame.Row - anim.StartFrame.Row) * ColumnCount +
                          (anim.EndFrame.Col - anim.StartFrame.Col);
        totalFrames = Math.Max(totalFrames, 1);

        double timeElapsed = (DateTimeOffset.Now - _animationStart).TotalMilliseconds;
        int currentFrame = (int)(timeElapsed / (anim.DurationMs / (double)(totalFrames + 1)));

        if (currentFrame > totalFrames)
        {
            if (anim.Loop)
            {
                _animationStart = DateTimeOffset.Now;
                currentFrame = 0;
            }
            else
            {
                currentFrame = totalFrames;
            }
        }

        int currentRow = anim.StartFrame.Row + currentFrame / ColumnCount;
        int currentCol = anim.StartFrame.Col + currentFrame % ColumnCount;

        renderer.RenderTexture(_textureId,
            new Rectangle<int>(currentCol * FrameWidth, currentRow * FrameHeight, FrameWidth, FrameHeight),
            new Rectangle<int>(dstX, dstY, width, height),
            anim.Flip, angle, rotationCenter);
    }
}
