namespace SubTyping;

public sealed record LocalTrackMetadata(
    string FilePath,
    string Artist,
    string Title,
    TimeSpan Duration);

public sealed record TimedLyricLine(TimeSpan Timestamp, string Text);

public sealed record LyricsResult(
    string Artist,
    string Title,
    IReadOnlyList<TimedLyricLine> Lines,
    bool IsSynced);
