using NAudio.Wave;

namespace SubTyping;

public sealed class LocalAudioPlayer : IDisposable
{
    private AudioFileReader? reader;
    private WaveOutEvent? output;

    public TimeSpan Position => reader?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan Duration => reader?.TotalTime ?? TimeSpan.Zero;

    public void Open(string filePath)
    {
        DisposePlayback();
        reader = new AudioFileReader(filePath);
        output = new WaveOutEvent();
        output.Init(reader);
    }

    public void Play() => output?.Play();

    public void Pause() => output?.Pause();

    public void Stop()
    {
        output?.Stop();
        if (reader is not null)
        {
            reader.CurrentTime = TimeSpan.Zero;
        }
    }

    public void Seek(TimeSpan position)
    {
        if (reader is not null)
        {
            reader.CurrentTime = position < TimeSpan.Zero
                ? TimeSpan.Zero
                : position > reader.TotalTime
                    ? reader.TotalTime
                    : position;
        }
    }

    public void Dispose()
    {
        DisposePlayback();
        GC.SuppressFinalize(this);
    }

    private void DisposePlayback()
    {
        output?.Stop();
        output?.Dispose();
        output = null;
        reader?.Dispose();
        reader = null;
    }
}
