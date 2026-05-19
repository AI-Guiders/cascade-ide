namespace CascadeIDE.Services;

/// <summary>Длительность transient reveal (ADR 0130 фаза 2, паритет <see cref="UiAgentHighlight"/>).</summary>
public static class EditorRevealDuration
{
    public const int DefaultMs = 3000;
    public const int MinMs = 250;
    public const int MaxMs = 120_000;

    public static int? ClampOptional(int? rawMs)
    {
        if (rawMs is null)
            return null;
        return Math.Clamp(rawMs.Value, MinMs, MaxMs);
    }

    public static TimeSpan ToTimeSpan(int? durationMs) =>
        TimeSpan.FromMilliseconds(ClampOptional(durationMs) ?? DefaultMs);
}
