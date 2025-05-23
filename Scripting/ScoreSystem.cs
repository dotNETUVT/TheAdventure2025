namespace TheAdventure.Scripting
{
    public static class ScoreSystem
    {
        public static int Score { get; private set; } = 0;

        public static void AddPoints(int points)
        {
            Score += points;
            Console.WriteLine($"Score: {Score}");
        }

        public static void Reset()
        {
            Score = 0;
        }
    }
}
