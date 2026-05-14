#nullable enable
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Graph;

/// <summary>
/// Вход <see cref="IGraphDataSource"/> для карты намерений / workspace navigation (wire JSON). ADR 0115.
/// </summary>
public readonly record struct CodeNavigationMapJsonRequest(
    string NormalizedLevel,
    bool WantGraph,
    string? CurrentPath,
    string? EditorText,
    int? CursorLine,
    int? CursorColumn,
    IReadOnlyList<string> RawFilePathsFromSolution,
    string? SolutionPath,
    CodeNavigationSettings? NavSettings);
