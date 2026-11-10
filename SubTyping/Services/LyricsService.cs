using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SubTyping;

public sealed class LyricsService : ILyricsService
{
    private static readonly Regex TimestampRegex =
        new(@"\[(?<minutes>\d{1,3}):(?<seconds>\d{2})(?:\.(?<fraction>\d{1,3}))?\]",
            RegexOptions.Compiled);

    private readonly HttpClient httpClient;

    public LyricsService(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient();
        this.httpClient.Timeout = TimeSpan.FromSeconds(15);
        if (!this.httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SubTyping/1.0");
        }
    }

    public async Task<LyricsResult?> FindSyncedLyricsAsync(
        string artist,
        string title,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var directUrl =
            $"https://lrclib.net/api/get?artist_name={Uri.EscapeDataString(artist)}&track_name={Uri.EscapeDataString(title)}";
        var direct = await GetAsync<LrcLibTrack>(directUrl, cancellationToken);
        var result = ToResult(direct, artist, title);
        if (result is not null)
        {
            return result;
        }

        var searchUrl =
            $"https://lrclib.net/api/search?artist_name={Uri.EscapeDataString(artist)}&track_name={Uri.EscapeDataString(title)}";
        var searchResults = await GetAsync<List<LrcLibTrack>>(searchUrl, cancellationToken);
        if (searchResults is null)
        {
            return null;
        }

        foreach (var track in searchResults)
        {
            result = ToResult(track, artist, title);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private static LyricsResult? ToResult(LrcLibTrack? track, string artist, string title)
    {
        if (track is null || string.IsNullOrWhiteSpace(track.SyncedLyrics))
        {
            return null;
        }

        var lines = ParseLrc(track.SyncedLyrics);
        return lines.Count == 0 ? null : new LyricsResult(artist, title, lines, true);
    }

    private static IReadOnlyList<TimedLyricLine> ParseLrc(string lrc)
    {
        var lines = new List<TimedLyricLine>();
        foreach (var rawLine in lrc.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var matches = TimestampRegex.Matches(rawLine);
            var text = TimestampRegex.Replace(rawLine, string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            foreach (Match match in matches)
            {
                var minutes = int.Parse(match.Groups["minutes"].Value);
                var seconds = int.Parse(match.Groups["seconds"].Value);
                var fractionText = match.Groups["fraction"].Value;
                var milliseconds = fractionText.Length switch
                {
                    1 => int.Parse(fractionText) * 100,
                    2 => int.Parse(fractionText) * 10,
                    3 => int.Parse(fractionText),
                    _ => 0
                };
                lines.Add(new TimedLyricLine(
                    new TimeSpan(0, 0, minutes, seconds, milliseconds),
                    text));
            }
        }

        return lines.OrderBy(line => line.Timestamp).ToArray();
    }

    private sealed class LrcLibTrack
    {
        [JsonPropertyName("syncedLyrics")]
        public string? SyncedLyrics { get; set; }
    }
}
