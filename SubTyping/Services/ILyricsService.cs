namespace SubTyping;

public interface ILyricsService
{
    Task<LyricsResult?> FindSyncedLyricsAsync(
        string artist,
        string title,
        CancellationToken cancellationToken = default);
}
