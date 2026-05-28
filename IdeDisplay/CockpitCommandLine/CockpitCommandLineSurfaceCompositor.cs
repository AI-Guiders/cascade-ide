namespace CascadeIDE.IdeDisplay.CockpitCommandLine;

/// <summary>IDS: композитор оверлея Cockpit Command Line.</summary>
public sealed class CockpitCommandLineSurfaceCompositor
    : IIdsSurfaceCompositor<CockpitCommandLineSurfaceIntent, CockpitCommandLineSurfaceSnapshot>
{
    public CockpitCommandLineSurfaceSnapshot Compose(CockpitCommandLineSurfaceIntent intent)
    {
        if (!intent.ShowSuggestions || intent.Suggestions.Count == 0)
        {
            return new CockpitCommandLineSurfaceSnapshot(
                intent.DraftText,
                intent.CaretIndex,
                intent.SelectedSuggestionIndex,
                intent.Breadcrumb,
                false,
                []);
        }

        var items = new List<CockpitCommandLineSurfaceEntry>(intent.Suggestions.Count);
        var selected = intent.SelectedSuggestionIndex;
        for (var i = 0; i < intent.Suggestions.Count; i++)
        {
            var row = intent.Suggestions[i];
            items.Add(new CockpitCommandLineSurfaceEntry(row.Title, row.Subtitle, i == selected));
        }

        return new CockpitCommandLineSurfaceSnapshot(
            intent.DraftText,
            intent.CaretIndex,
            selected,
            intent.Breadcrumb,
            true,
            items);
    }
}
