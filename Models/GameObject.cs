namespace TheAdventure.Models
{
    public abstract class GameObject
    {
        private static int _nextId = 0;

        public int Id { get; }
        public (int X, int Y) Position { get; set; }

        protected GameObject((int X, int Y) position)
        {
            Id = _nextId++;
            Position = position;
        }
    }
}