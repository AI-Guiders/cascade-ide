namespace CascadeIDE.Models;

/// <summary>UX обозревателя решения (ADR 0167). TOML: <c>[workspace.solution_explorer]</c>.</summary>
public sealed class SolutionExplorerSettings
{
    public bool TrackActiveItem { get; set; } = true;

    public bool CompactTree { get; set; } = true;
}
