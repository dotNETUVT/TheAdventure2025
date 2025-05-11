using Silk.NET.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Processing;
using Color = System.Drawing.Color;

namespace TheAdventure;

public class GameOverScreen
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private int _highScore;
    private int _currentScore;
    private Font _font;

    public GameOverScreen(GameRenderer renderer, Input input, int currentScore, int highScore)
    {
        _renderer = renderer;
        _input = input;
        _currentScore = currentScore;
        _highScore = highScore;

        var fontCollection = new FontCollection();
        _font = fontCollection.Add("Assets/arial.ttf").CreateFont(32);
    }

    public bool Run()
    {
        bool retry = false;
        bool exit = false;

        var gameOverTex = CreateTextTexture("GAME OVER", Color.White);
        var retryTex = CreateTextTexture("PRESS 'R' TO RETRY", Color.White);
        var scoreTex = CreateTextTexture($"SCORE: {_currentScore}", Color.White);
        var highScoreTex = CreateTextTexture($"HIGH SCORE: {_highScore}", Color.White);

        while (!retry && !exit)
        {
            exit = _input.ProcessInput();

            if (_input.IsKeyRPressed())
                retry = true;

            _renderer.SetDrawColor(0, 0, 0, 255);
            _renderer.ClearScreen();

            DrawTexture(gameOverTex, 200, 120);
            DrawTexture(scoreTex, 200, 180);
            DrawTexture(highScoreTex, 200, 240);
            DrawTexture(retryTex, 200, 300);

            _renderer.PresentFrame();

            Thread.Sleep(16);
        }
        
        _renderer.FreeTexture(gameOverTex.textureId);
        _renderer.FreeTexture(scoreTex.textureId);
        _renderer.FreeTexture(highScoreTex.textureId);
        _renderer.FreeTexture(retryTex.textureId);
        
        return retry && !exit;
    }

    private (int textureId, int width, int height) CreateTextTexture(string text, Color color)
    {
        using var image = new Image<Rgba32>(400, 80);
        image.Mutate(ctx =>
        {
            ctx.Clear(new Rgba32(0, 0, 0, 0));
            ctx.DrawText(text, _font, new Rgba32(color.R, color.G, color.B, color.A), new PointF(0, 0));
        });

        byte[] pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);

        var textureId = _renderer.LoadTextureFromMemory(pixels, image.Width, image.Height);

        return (textureId, image.Width, image.Height);
    }
    
    private void DrawTexture((int textureId, int width, int height) tex, int x, int y)
    {
        _renderer.RenderUITexture(
            tex.textureId,
            new Rectangle<int>(0, 0, tex.width, tex.height),
            new Rectangle<int>(x, y, tex.width, tex.height));
    }
}
