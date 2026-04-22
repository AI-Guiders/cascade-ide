namespace CascadeIDE.ViewModels;

/// <summary>Строка списка карты кода (режим <c>related</c>, ADR 0039).</summary>
public sealed class WorkspaceNavigationMapItemVm
{
    public required string FullPath { get; init; }
    public required string RelativePath { get; init; }
    public required string Kind { get; init; }
    public required string Rationale { get; init; }

    public string KindRationaleLine =>
        string.IsNullOrEmpty(Rationale) ? Kind : $"{Kind} · {Rationale}";
}
