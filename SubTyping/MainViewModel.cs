using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SubTyping;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IYouTubeCaptionService captionService = new YouTubeCaptionService();
    private readonly LocalTrackService localTrackService = new();
    private readonly ILyricsService lyricsService = new LyricsService();
    private readonly TapAlongSyncService tapAlongSync = new();
    private IReadOnlyList<PracticeLine> loadedLines = Array.Empty<PracticeLine>();

    [ObservableProperty]
    private string youTubeUrl = string.Empty;

    [ObservableProperty]
    private YouTubeCaptionTrack? selectedCaptionTrack;

    [ObservableProperty]
    private bool isDarkTheme;

    [ObservableProperty]
    private string statusMessage = "Paste a YouTube link to load its available captions.";

    [ObservableProperty]
    private string localTrackText = "No MP3 selected.";

    [ObservableProperty]
    private bool isTapAlongMode;

    [ObservableProperty]
    private string tapLineText = string.Empty;

    [ObservableProperty]
    private bool isPracticing;

    public PracticeViewModel Practice { get; } = new();

    public ObservableCollection<YouTubeCaptionTrack> CaptionTracks { get; } = new();
    public ObservableCollection<string> TapAlongLines { get; } = new();

    public event EventHandler<string>? VideoLoadRequested;
    public event EventHandler? PlayRequested;
    public event EventHandler? PauseRequested;
    public event EventHandler? ContinueRequested;

    [RelayCommand]
    private async Task FetchCaptionsAsync()
    {
        if (string.IsNullOrWhiteSpace(YouTubeUrl))
        {
            StatusMessage = "Enter a YouTube URL first.";
            return;
        }

        try
        {
            StatusMessage = "Fetching available caption tracks...";
            CaptionTracks.Clear();
            loadedLines = Array.Empty<PracticeLine>();
            var tracks = await captionService.GetTracksAsync(YouTubeUrl);
            foreach (var track in tracks)
            {
                CaptionTracks.Add(track);
            }

            VideoLoadRequested?.Invoke(this, YouTubeUrl);
            StatusMessage = tracks.Count == 0
                ? "No captions, can't practice this one."
                : $"Found {tracks.Count} caption track(s). Select one to practice.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not load captions: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task StartPracticeAsync()
    {
        if (SelectedCaptionTrack is null && loadedLines.Count == 0)
        {
            StatusMessage = "Fetch YouTube captions or choose an MP3 with timed lyrics first.";
            return;
        }

        if (SelectedCaptionTrack is not null)
        {
            try
            {
                StatusMessage = "Loading selected captions...";
                loadedLines = await captionService.GetLinesAsync(SelectedCaptionTrack);
            }
            catch (Exception exception)
            {
                StatusMessage = $"Could not load the selected captions: {exception.Message}";
                return;
            }
        }

        Practice.LoadLines(loadedLines);
        IsPracticing = true;
        StatusMessage = "Practice started.";
        PlayRequested?.Invoke(this, EventArgs.Empty);
    }

    public void OnPlaybackTime(TimeSpan position)
    {
        if (IsPracticing && Practice.ShouldPauseAt(position))
        {
            PauseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ContinueAfterEnter()
    {
        if (Practice.AdvanceAfterEnter())
        {
            ContinueRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task LoadMp3Async(string filePath)
    {
        try
        {
            var metadata = localTrackService.ReadMetadata(filePath);
            SelectedCaptionTrack = null;
            LocalTrackText = $"{metadata.Artist} - {metadata.Title}";
            StatusMessage = "Looking up timed lyrics...";
            var lyrics = await lyricsService.FindSyncedLyricsAsync(metadata.Artist, metadata.Title);
            loadedLines = lyrics is null
                ? Array.Empty<PracticeLine>()
                : lyrics.Lines
                    .Select((line, index) => new PracticeLine(
                        line.Text,
                        line.Timestamp,
                        index + 1 < lyrics.Lines.Count
                            ? lyrics.Lines[index + 1].Timestamp
                            : null))
                    .ToArray();
            IsTapAlongMode = loadedLines.Count == 0;
            TapAlongLines.Clear();
            if (IsTapAlongMode)
            {
                tapAlongSync.Start();
            }
            StatusMessage = loadedLines.Count == 0
                ? "No timed lyrics found. Enter each lyric line and press Record at its start."
                : $"Loaded {loadedLines.Count} timed lyric line(s).";
            LocalTrackLoaded?.Invoke(this, filePath);
        }
        catch (Exception exception)
        {
            LocalTrackText = "No MP3 selected.";
            loadedLines = Array.Empty<PracticeLine>();
            StatusMessage = $"Could not load MP3: {exception.Message}";
        }
    }

    [RelayCommand]
    private void RecordTapLine()
    {
        if (!IsTapAlongMode || string.IsNullOrWhiteSpace(TapLineText))
        {
            StatusMessage = "Enter a lyric line before recording it.";
            return;
        }

        var line = tapAlongSync.RecordLine(TapLineText);
        TapAlongLines.Add($"{line.Timestamp:mm\\:ss}  {line.Text}");
        TapLineText = string.Empty;
    }

    [RelayCommand]
    private void FinishTapAlong()
    {
        if (TapAlongLines.Count == 0)
        {
            StatusMessage = "Record at least one lyric line first.";
            return;
        }

        var syncedLines = tapAlongSync.Complete();
        loadedLines = syncedLines
            .Select((line, index) => new PracticeLine(
                line.Text,
                line.Timestamp,
                index + 1 < syncedLines.Count ? syncedLines[index + 1].Timestamp : null))
            .ToArray();
        IsTapAlongMode = false;
        StatusMessage = $"Tap-along sync saved {loadedLines.Count} line(s).";
    }

    public event EventHandler<string>? LocalTrackLoaded;

    [RelayCommand]
    private void StopPractice()
    {
        IsPracticing = false;
        StatusMessage = "Practice stopped.";
        PauseRequested?.Invoke(this, EventArgs.Empty);
    }
}

