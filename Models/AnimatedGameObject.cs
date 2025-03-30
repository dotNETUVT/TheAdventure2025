using Silk.NET.Maths;

namespace TheAdventure.Models
{
    public class AnimatedGameObject : RenderableGameObject
    {
        private int _durationInSeconds;
        private int _numberOfColumns;
        private int _numberOfRows;
        private int _numberOfFrames;
        private int _timeSinceAnimationStart = 0;
        private int _currentRow = 0;
        private int _currentColumn = 0;
        private int _rowHeight = 0;
        private int _columnWidth = 0;
        private int _timePerFrame;
        private float _scaleFactor = 1.0f;
        
        public int X { get; set; }
        public int Y { get; set; }
        
        public bool ForceEndNow { get; set; } = false;
    
        public Action<GameLogic>? OnDestroy { get; set; }

        public AnimatedGameObject(
            string fileName,
            int durationInSeconds,
            int id,
            int numberOfFrames,
            int numberOfColumns,
            int numberOfRows,
            int x,
            int y
        )
            : base(fileName, id)
        {
            _durationInSeconds = durationInSeconds;
            _numberOfFrames = numberOfFrames;
            _numberOfColumns = numberOfColumns;
            _numberOfRows = numberOfRows;

            X = x;
            Y = y;

            _rowHeight = TextureInformation.Height / numberOfRows;
            _columnWidth = TextureInformation.Width / numberOfColumns;
            var halfRow = _rowHeight / 2;
            var halfColumn = _columnWidth / 2;
            _timePerFrame = (durationInSeconds * 1000) / _numberOfFrames;
            TextureDestination = new Rectangle<int>(
                X - halfColumn,
                Y - halfRow,
                _columnWidth,
                _rowHeight
            );
            TextureSource = new Rectangle<int>(
                _currentColumn * _columnWidth,
                _currentRow * _rowHeight,
                _columnWidth,
                _rowHeight
            );
        }

        public override bool Update(int timeSinceLastFrame)
        {
            if (ForceEndNow)
            {
                _timeSinceAnimationStart += timeSinceLastFrame;
                _scaleFactor += (float)timeSinceLastFrame / 1000f * 1.0f;
                int newWidth = (int)(_columnWidth * _scaleFactor);
                int newHeight = (int)(_rowHeight * _scaleFactor);
                TextureDestination = new Rectangle<int>(
                    X - newWidth / 2,
                    Y - newHeight / 2,
                    newWidth,
                    newHeight
                );
                _currentRow = (_numberOfFrames - 1) / _numberOfColumns;
                _currentColumn = (_numberOfFrames - 1) % _numberOfColumns;
                TextureSource = new Rectangle<int>(
                    _currentColumn * _columnWidth,
                    _currentRow * _rowHeight,
                    _columnWidth,
                    _rowHeight
                );
                if (_timeSinceAnimationStart >= _durationInSeconds * 1000 + 1000)
                    return false;
                return true;
            }
            else
            {
                _timeSinceAnimationStart += timeSinceLastFrame;
                if (_timeSinceAnimationStart >= _durationInSeconds * 1000)
                    return false;

                var currentFrame = _timeSinceAnimationStart / _timePerFrame;
                _currentRow = currentFrame / _numberOfColumns;
                _currentColumn = currentFrame % _numberOfColumns;
                TextureSource = new Rectangle<int>(
                    _currentColumn * _columnWidth,
                    _currentRow * _rowHeight,
                    _columnWidth,
                    _rowHeight
                );
                var halfRow = _rowHeight / 2;
                var halfColumn = _columnWidth / 2;
                TextureDestination = new Rectangle<int>(
                    X - halfColumn,
                    Y - halfRow,
                    _columnWidth,
                    _rowHeight
                );
                return true;
            }
        }
    }
}
