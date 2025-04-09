using Silk.NET.Maths;

namespace TheAdventure.Models;

public class AnimatedGameObject : RenderableGameObject
{
    private int _durationInSeconds;
    private int _numberOfColumns;
    private int _numberOfRows;
    private int _numberOfFrames;
    private double _timeSinceAnimationStart = 0;

    private int _currentRow = 0;
    private int _currentColumn = 0;
    private int _rowHeight = 0;
    private int _columnWidth = 0;
    private int _timePerFrame;

    public AnimatedGameObject(string fileName, GameRenderer renderer, int durationInSeconds, int numberOfFrames, int numberOfColumns,
        int numberOfRows, int x, int y) :
        base(fileName, renderer)
    {
        _durationInSeconds = durationInSeconds;
        _numberOfFrames = numberOfFrames;
        _numberOfColumns = numberOfColumns;
        _numberOfRows = numberOfRows;

        _rowHeight = TextureInformation.Height / numberOfRows;
        _columnWidth = TextureInformation.Width / numberOfColumns;

        var halfRow = _rowHeight / 2;
        var halfColumn = _columnWidth / 2;

        _timePerFrame = (durationInSeconds * 1000) / _numberOfFrames;

        TextureDestination =
            new Rectangle<int>(x - halfColumn, y - halfRow, _columnWidth, _rowHeight);
        TextureSource = new Rectangle<int>(_currentColumn * _columnWidth, _currentRow * _rowHeight, _columnWidth,
            _rowHeight);
    }

    public override bool Update(double msSinceLastFrame)
    {
        _timeSinceAnimationStart += msSinceLastFrame;

        var currentFrame = _timeSinceAnimationStart / _timePerFrame;

        if (_timeSinceAnimationStart > _durationInSeconds * 1000) return false;

        _currentRow = (int)(currentFrame / _numberOfColumns);
        _currentColumn = (int)(currentFrame % _numberOfColumns);

        TextureSource = new Rectangle<int>(_currentColumn * _columnWidth, _currentRow * _rowHeight, _columnWidth,
            _rowHeight);

        return true;
    }
}