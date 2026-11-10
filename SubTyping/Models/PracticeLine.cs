namespace SubTyping;

public sealed record PracticeLine(
    string Text,
    TimeSpan Start = default,
    TimeSpan? End = null)
{
    public TimeSpan EffectiveEnd => End ?? Start + TimeSpan.FromSeconds(5);
}
