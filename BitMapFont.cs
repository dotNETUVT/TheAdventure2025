using Silk.NET.Maths;
using TheAdventure.Models;

namespace TheAdventure;

public class BitmapFont
{
    private readonly int _charWidth = 32;
    private readonly int _charHeight = 48;

    private readonly int _textureId;
    private readonly GameRenderer _renderer;

    public BitmapFont(GameRenderer renderer)
    {
        _renderer = renderer;
        _textureId = renderer.LoadTexture("Assets/Font.png", out _);
    }

    public void RenderText(string text, int x, int y)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsDigit(c))
            {
                int charIndex = c - '0'; // 0 = poz. 0, 9 = poz. 9
                var src = new Rectangle<int>(charIndex * _charWidth, 0, _charWidth, _charHeight);
                var dst = new Rectangle<int>(x + i * _charWidth, y, _charWidth, _charHeight);
                _renderer.RenderTexture(_textureId, src, dst);
            }
        }
    }

}
