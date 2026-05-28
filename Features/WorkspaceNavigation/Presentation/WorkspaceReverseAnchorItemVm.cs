#nullable enable

namespace CascadeIDE.Features.WorkspaceNavigation.Presentation;

public sealed class WorkspaceReverseAnchorItemVm
{
    public required string DocPath { get; init; }

    public required string DisplayTitle { get; init; }

    public required string Excerpt { get; init; }

    public required string Provenance { get; init; }

    public int? DocLineHint { get; init; }

    public string DisplayLine
    {
        get
        {
            var line = DocLineHint is > 0 ? $" (line ~{DocLineHint})" : "";
            var prov = string.IsNullOrWhiteSpace(Provenance) ? "" : $" [{Provenance}]";
            var excerpt = string.IsNullOrWhiteSpace(Excerpt) ? "" : $" — «{Excerpt}»";
            return $"{DisplayTitle}{line}{excerpt}{prov}";
        }
    }
}
