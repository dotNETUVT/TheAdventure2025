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
    private bool _isImmortalVisible = false;
    
    private int _immortalIconTextureId = -1;
    private Rectangle<int> _immortalIconRect;
    
    private int _speedIconTextureId = -1;
    private Rectangle<int> _speedIconRect;
    private bool _isSpeedVisible = false;

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
        
        _immortalIconTextureId = renderer.LoadTexture(Path.Combine("Assets", "Immortal.png"), out var iconInfo);
        
        // Make immortality icon slightly larger
        int immortalIconSize = 45;
        int padding = 10;
        _immortalIconRect = new Rectangle<int>(
            windowSize.Width - immortalIconSize - padding,
            padding,
            immortalIconSize,
            immortalIconSize
        );
        
        // Load speed icon
        _speedIconTextureId = renderer.LoadTexture(Path.Combine("Assets", "Speed.png"), out var speedIconInfo);
        
        // Position speed icon to the left of immortal icon
        int speedIconSize = 40;
        _speedIconRect = new Rectangle<int>(
            _immortalIconRect.Origin.X - speedIconSize - padding,
            padding,
            speedIconSize,
            speedIconSize
        );
    }

    public void ShowGameOver()
    {
        _isGameOverVisible = true;
        _isImmortalVisible = false;
        _isSpeedVisible = false;
    }

    public void HideGameOver()
    {
        _isGameOverVisible = false;
    }
    
    public void SetImmortalVisible(bool visible)
    {
        _isImmortalVisible = visible;
    }
    
    public void SetSpeedVisible(bool visible)
    {
        _isSpeedVisible = visible;
    }

    public void Render()
    {
        if (_isGameOverVisible && _gameOverTextureId != -1)
        {
            _renderer.RenderUITexture(_gameOverTextureId, 
                new Rectangle<int>(0, 0, _gameOverRect.Size.X, _gameOverRect.Size.Y), 
                _gameOverRect);
        }
        
        if (_isImmortalVisible && _immortalIconTextureId != -1)
        {
            _renderer.RenderUITexture(
                _immortalIconTextureId,
                new Rectangle<int>(0, 0, _renderer.GetTextureData(_immortalIconTextureId).Width, _renderer.GetTextureData(_immortalIconTextureId).Height),
                _immortalIconRect
            );
        }
        
        if (_isSpeedVisible && _speedIconTextureId != -1)
        {
            _renderer.RenderUITexture(
                _speedIconTextureId,
                new Rectangle<int>(0, 0, _renderer.GetTextureData(_speedIconTextureId).Width, _renderer.GetTextureData(_speedIconTextureId).Height),
                _speedIconRect
            );
        }
    }
}
