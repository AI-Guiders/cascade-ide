namespace CascadeIDE.IdeDisplay.CockpitCommandLine;

public readonly record struct CockpitCommandLineSurfaceIntent(
    string DraftText,
    int CaretIndex,
    int SelectedSuggestionIndex,
    string? Breadcrumb,
    bool ShowSuggestions,
    IReadOnlyList<CockpitCommandLineSuggestionRow> Suggestions);

public readonly record struct CockpitCommandLineSuggestionRow(string Title, string? Subtitle);
