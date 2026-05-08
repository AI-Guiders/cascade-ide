namespace CascadeIDE.Cockpit.DataBus;

/// <summary>
/// Сигнал IDE о состоянии Hybrid Codebase Index (ADR 0105/0106): готовность БД, число документов, время обновления.
/// </summary>
public sealed record HybridIndexStateChanged(
    string WorkspaceRoot,
    string? SolutionPath,
    string DatabasePath,
    int DocumentCount,
    string? IndexedAtIso,
    string? LastError,
    string? LastErrorAtIso);

