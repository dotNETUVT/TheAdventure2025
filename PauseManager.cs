namespace TheAdventure2025
{
    public static class PauseManager
    {
        private static bool _isPaused = false;

        public static bool IsPaused => _isPaused;

        public static void TogglePause()
        {
            _isPaused = !_isPaused;
        }

        public static void Pause()
        {
            _isPaused = true;
        }

        public static void Resume()
        {
            _isPaused = false;
        }
    }
}
