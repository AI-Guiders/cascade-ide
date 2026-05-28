namespace CascadeIDE.IdeDisplay.CockpitCommandLine;

/// <summary>IDS v0: снимок оверлея Cockpit Command Line (editor host, ADR 0079).</summary>
public sealed record CockpitCommandLineSurfaceSnapshot(
    string DraftText,
    int CaretIndex,
    int SelectedSuggestionIndex,
    string? Breadcrumb,
    bool ShowSuggestions,
    IReadOnlyList<CockpitCommandLineSurfaceEntry> Suggestions)
{
    public static CockpitCommandLineSurfaceSnapshot Empty { get; } = new("", 0, -1, null, false, []);
}
