using NAudio.Wave;

namespace TheAdventure.Audio
{
    public class AudioPlayer
    {
        private readonly string _filePath;

        public AudioPlayer(string filePath)
        {
            _filePath = filePath;
        }

        public void Play()
        {
            var audioFile = new AudioFileReader(_filePath);
            var outputDevice = new WaveOutEvent();
            outputDevice.Init(audioFile);
            outputDevice.Play();

            outputDevice.PlaybackStopped += (s, e) =>
            {
                outputDevice.Dispose();
                audioFile.Dispose();
            };
        }
    }
}
