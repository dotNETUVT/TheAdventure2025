using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Maths;

namespace TheAdventure.Models
{
    public class HealthBarRenderer
    {
        private readonly GameRenderer _renderer;
        private readonly PlayerObject _player;
        private readonly int _width;
        private readonly int _height;
        private readonly int _offsetY;
        private readonly int _offsetX;

        public HealthBarRenderer(GameRenderer renderer, PlayerObject player,
                               int width = 50,    // Reduced from 100
                               int height = 5,    // Reduced from 10
                               int offsetY = -15, // Adjusted upward
                               int offsetX = 0)   // New horizontal adjustment
        {
            _renderer = renderer;
            _player = player;
            _width = width;
            _height = height;
            _offsetY = offsetY;
            _offsetX = offsetX;
        }

        public void Render()
        {
            // Calculate position (centered above player)
            var barX = _player.X - (_width / 2) + _offsetX;
            var barY = _player.Y + _offsetY;

            // Background (empty health)
            _renderer.SetDrawColor(255, 0, 0, 255); // Red
            var bgRect = new Rectangle<int>(barX, barY, _width, _height);
            _renderer.RenderFillRectangle(bgRect);

            // Foreground (current health)
            var healthWidth = (int)(_width * ((float)_player.CurrentHealth / _player.MaxHealth));
            _renderer.SetDrawColor(0, 255, 0, 255); // Green
            var healthRect = new Rectangle<int>(barX, barY, healthWidth, _height);
            _renderer.RenderFillRectangle(healthRect);

            // Border
            _renderer.SetDrawColor(0, 0, 0, 255); // Black
            _renderer.RenderRectangle(bgRect);
        }
    }
}
