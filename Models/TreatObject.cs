using Silk.NET.Maths;

namespace TheAdventure.Models
{
    public class TreatObject : TemporaryGameObject
    {
        public int Points { get; } = 10;
        public bool IsCollected { get; private set; }


        public TreatObject(SpriteSheet spriteSheet, (int X, int Y) position)
            : base(spriteSheet, 6.5, position, "treat")
        {
        }
        
        public bool CheckCollision((int X, int Y) player)
        {
            if (IsCollected) return false;
            Vector2D<int> treatPosition = new Vector2D<int>(Position.X, Position.Y);
            Vector2D<int> playerPosition = new Vector2D<int>(player.X, player.Y);
            
            var distance = Vector2D.Distance(treatPosition, playerPosition);
            IsCollected = distance < 60; 
            return IsCollected;
        }
    }
}