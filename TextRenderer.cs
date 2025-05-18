using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;



namespace TheAdventure;

public class TextRenderer
{
    private readonly Font _font;
    private readonly GameRenderer _renderer;

    public TextRenderer(GameRenderer renderer, string fontPath, float fontSize = 30f)
    {
        _renderer = renderer;
        var fontCollection = new FontCollection();
        var family = fontCollection.Add(fontPath);
        _font = family.CreateFont(fontSize);
    }

    public void DrawText(string text, int x, int y, Rgba32 color)
    {
        var textGraphics = new Image<Rgba32>(400, 100); // adjust size as needed
        textGraphics.Mutate(ctx => ctx.DrawText(text, _font, color, new PointF(0, 0)));

        using var stream = new MemoryStream();
        textGraphics.SaveAsPng(stream);
        stream.Seek(0, SeekOrigin.Begin);

        var textureId = _renderer.LoadTextureFromStream(stream, out var info);
        _renderer.RenderHUDTexture(textureId,
            new Silk.NET.Maths.Rectangle<int>(0, 0, info.Width, info.Height),
            new Silk.NET.Maths.Rectangle<int>(x, y, info.Width, info.Height));
    }
}
