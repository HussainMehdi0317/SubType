namespace SubTyping;

public interface IYouTubeCaptionService
{
    Task<IReadOnlyList<YouTubeCaptionTrack>> GetTracksAsync(
        string url,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PracticeLine>> GetLinesAsync(
        YouTubeCaptionTrack track,
        CancellationToken cancellationToken = default);
}
