using TheAdventure.Models;

namespace TheAdventure
{
    public class DogCompanion : RenderableGameObject
    {
        private PlayerObject _player;

        public DogCompanion(PlayerObject player, SpriteSheet spriteSheet)
            : base(spriteSheet, (player.Position.X - 30, player.Position.Y + 20))
        {
            _player = player;
        }

        public void Update()
    {
        var (playerX, playerY) = _player.Position;

        var offsetTarget = (playerX - 30, playerY + 20);
        var (currentX, currentY) = Position;
        int followSpeed = 2;

        int dx = offsetTarget.Item1 - currentX;
        int dy = offsetTarget.Item2 - currentY;

        if (Math.Abs(dx) > 2 || Math.Abs(dy) > 2)
        {
            int newX = currentX + Math.Sign(dx) * followSpeed;
            int newY = currentY + Math.Sign(dy) * followSpeed;
            Position = (newX, newY);
        }
    }


    }
}
