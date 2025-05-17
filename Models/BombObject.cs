using System;
using Silk.NET.Maths;
using TheAdventure;
using TheAdventure.Models;

namespace TheAdventure.Models
{
    public class BombObject : RenderableGameObject
    {
        private readonly RenderableGameObject _player;
        private readonly GameLogic _gameLogic;
        public bool HasExploded { get; private set; }

        public BombObject(string fileName, int id, int x, int y, RenderableGameObject player, GameLogic logic)
            : base(fileName, id)
        {
            _player = player;
            _gameLogic = logic;
            TextureDestination = new Rectangle<int>(new Vector2D<int>(x, y), new Vector2D<int>(TextureInformation.Width, TextureInformation.Height));
        }

        public override bool Update(int timeSinceLastFrame)
        {
            if (HasExploded)
                return false;

            int dx = Math.Abs(TextureDestination.Origin.X - _player.TextureDestination.Origin.X);
            int dy = Math.Abs(TextureDestination.Origin.Y - _player.TextureDestination.Origin.Y);

            if (dx < TextureDestination.Size.X && dy < TextureDestination.Size.Y)
            {
                _gameLogic.DecreasePlayerHealth();
                _gameLogic.PlayBombAnimation(new Vector2D<float>(TextureDestination.Origin.X, TextureDestination.Origin.Y));
                HasExploded = true;
                return false;
            }

            return true;
        }
    }
}
