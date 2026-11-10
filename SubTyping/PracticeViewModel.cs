using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SubTyping;

public enum CharacterStatus
{
    NotTyped,
    Correct,
    Wrong
}

public sealed partial class CharacterViewModel : ObservableObject
{
    public CharacterViewModel(char character)
    {
        Character = character;
    }

    public char Character { get; }

    [ObservableProperty]
    private CharacterStatus status;
}

public sealed partial class PracticeViewModel : ObservableObject
{
    private readonly Stopwatch stopwatch = new();
    private List<PracticeLine> lines = new();
    private int lineIndex;
    private int correctCharacters;
    private int wrongCharacters;

    public ObservableCollection<CharacterViewModel> Characters { get; } = new();
    public ObservableCollection<string> NextLines { get; } = new();

    [ObservableProperty]
    private string inputText = string.Empty;

    [ObservableProperty]
    private string currentLineText = string.Empty;

    [ObservableProperty]
    private string progressText = "0 / 0";

    [ObservableProperty]
    private string accuracyText = "100%";

    [ObservableProperty]
    private string wpmText = "0";

    [ObservableProperty]
    private bool isComplete;

    [ObservableProperty]
    private bool isWaitingForEnter;

    public string? CurrentLineStart => lines.Count > lineIndex ? lines[lineIndex].Start.ToString() : null;

    public void LoadLines(IEnumerable<PracticeLine> sourceLines)
    {
        lines = sourceLines.Where(line => line.Text.Length > 0).ToList();
        lineIndex = 0;
        correctCharacters = 0;
        wrongCharacters = 0;
        IsComplete = false;
        stopwatch.Restart();
        LoadCurrentLine();
    }

    partial void OnInputTextChanged(string? oldValue, string newValue)
    {
        if (IsComplete || string.IsNullOrEmpty(CurrentLineText))
        {
            return;
        }

        for (var index = 0; index < Characters.Count; index++)
        {
            Characters[index].Status = index >= newValue.Length
                ? CharacterStatus.NotTyped
                : newValue[index] == CurrentLineText[index]
                    ? CharacterStatus.Correct
                    : CharacterStatus.Wrong;
        }

        correctCharacters = Characters.Count(character => character.Status == CharacterStatus.Correct);
        wrongCharacters = Characters.Count(character => character.Status == CharacterStatus.Wrong);
        UpdateMetrics();

        IsWaitingForEnter = newValue == CurrentLineText;
    }

    [RelayCommand]
    private void ClearInput()
    {
        InputText = string.Empty;
    }

    private void LoadCurrentLine()
    {
        if (lineIndex >= lines.Count)
        {
            CurrentLineText = string.Empty;
            NextLines.Clear();
            IsComplete = true;
            stopwatch.Stop();
            return;
        }

        CurrentLineText = lines[lineIndex].Text;
        InputText = string.Empty;
        IsWaitingForEnter = false;
        Characters.Clear();
        foreach (var character in CurrentLineText)
        {
            Characters.Add(new CharacterViewModel(character));
        }

        NextLines.Clear();
        foreach (var line in lines.Skip(lineIndex + 1).Take(3))
        {
            NextLines.Add(line.Text);
        }

        ProgressText = $"{lineIndex + 1} / {lines.Count}";
    }

    public bool ShouldPauseAt(TimeSpan position)
    {
        if (IsComplete || lines.Count == 0 || IsWaitingForEnter)
        {
            return false;
        }

        return position >= lines[lineIndex].EffectiveEnd;
    }

    public bool AdvanceAfterEnter()
    {
        if (IsComplete || !IsWaitingForEnter)
        {
            return false;
        }

        lineIndex++;
        LoadCurrentLine();
        UpdateMetrics();
        return true;
    }

    private void UpdateMetrics()
    {
        var minutes = Math.Max(stopwatch.Elapsed.TotalMinutes, 1d / 60d);
        WpmText = $"{Math.Round(correctCharacters / 5d / minutes):0}";
        var total = correctCharacters + wrongCharacters;
        AccuracyText = total == 0 ? "100%" : $"{Math.Round(correctCharacters * 100d / total):0}%";
    }
}
