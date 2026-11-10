using System.Diagnostics;

namespace SubTyping;

public sealed class TapAlongSyncService
{
    private readonly Stopwatch stopwatch = new();
    private readonly List<TimedLyricLine> lines = new();

    public IReadOnlyList<TimedLyricLine> Lines => lines;

    public void Start()
    {
        lines.Clear();
        stopwatch.Restart();
    }

    public TimedLyricLine RecordLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("A tap-along line cannot be empty.", nameof(text));
        }

        var line = new TimedLyricLine(stopwatch.Elapsed, text.Trim());
        lines.Add(line);
        return line;
    }

    public IReadOnlyList<TimedLyricLine> Complete()
    {
        stopwatch.Stop();
        return lines.ToArray();
    }
}
