#nullable enable
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Graph;

/// <summary>
/// Вход <see cref="IGraphDataSource"/> для graph-backed прибора workspace/code navigation (wire JSON). ADR 0115.
/// </summary>
public readonly record struct GraphNavigationJsonRequest(
    string NormalizedLevel,
    bool WantGraph,
    string? CurrentPath,
    string? EditorText,
    int? CursorLine,
    int? CursorColumn,
    IReadOnlyList<string> RawFilePathsFromSolution,
    string? SolutionPath,
    CodeNavigationSettings? NavSettings,
    /// <summary>Канон: <see cref="CodeNavigationMapControlFlowGrainKind"/> (TOML <c>[code_navigation_map].control_flow_grain</c>).</summary>
    string ControlFlowGrain = CodeNavigationMapControlFlowGrainKind.Intent);
