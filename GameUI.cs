using Silk.NET.Maths;
using Silk.NET.SDL;
using TheAdventure.Models;

namespace TheAdventure;

public class GameUI
{
    private readonly GameRenderer _renderer;
    private int _gameOverTextureId = -1;
    private Rectangle<int> _gameOverRect;
    private bool _isGameOverVisible = false;

    public bool IsGameOverVisible => _isGameOverVisible;

    public GameUI(GameRenderer renderer)
    {
        _renderer = renderer;
        
        _gameOverTextureId = renderer.LoadTexture(Path.Combine("Assets", "GameOver.png"), out var textureInfo);
        
        var windowSize = renderer.WindowSize;
        _gameOverRect = new Rectangle<int>(
            windowSize.Width / 2 - textureInfo.Width / 2,
            windowSize.Height / 2 - textureInfo.Height / 2,
            textureInfo.Width,
            textureInfo.Height
        );
    }

    public void ShowGameOver()
    {
        _isGameOverVisible = true;
    }

    public void HideGameOver()
    {
        _isGameOverVisible = false;
    }

    public void Render()
    {
        if (_isGameOverVisible && _gameOverTextureId != -1)
        {
            _renderer.RenderUITexture(_gameOverTextureId, 
                new Rectangle<int>(0, 0, _gameOverRect.Size.X, _gameOverRect.Size.Y), 
                _gameOverRect);
        }
    }
}
