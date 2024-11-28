using System;
using System.IO;
using System.Threading;
using NAudio.Wave;

public class SoundPlayer : ISoundPlayer
{
    private readonly byte[] _tWinCache;
    private readonly byte[] _ctWinCache;

    public SoundPlayer()
    {
        _ctWinCache = LoadSoundIntoCache(@"./sounds/Counter Terrorists Win - CS GO - QuickSounds.com.mp3");
        _tWinCache = LoadSoundIntoCache(@"./sounds/Terrorists Win - CS GO - QuickSounds.com.mp3");
    }

    private byte[] LoadSoundIntoCache(string filePath)
    {
        using (var reader = new AudioFileReader(filePath))
        using (var memoryStream = new MemoryStream())
        {
            var waveProvider = new Wave32To16Stream(reader); // Convert to 16-bit PCM
            WaveFileWriter.WriteWavFileToStream(memoryStream, waveProvider);
            return memoryStream.ToArray();
        }
    }

    public void PlayTwin()
    {
        PlaySound(_tWinCache);
    }

    public void PlayCtWin()
    {
        PlaySound(_ctWinCache);
    }

    private void PlaySound(byte[] audioData)
    {
        using (var memoryStream = new MemoryStream(audioData))
        using (var waveStream = new WaveFileReader(memoryStream))
        using (var outputDevice = new WaveOutEvent())
        {
            outputDevice.Init(waveStream);
            outputDevice.Play();
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(100); // Ensure playback completes before disposing
            }
        }
    }
}
