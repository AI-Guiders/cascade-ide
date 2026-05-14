#nullable enable
using CascadeIDE.Cockpit.Graph;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>
/// Адаптер: текущая сборка JSON через <see cref="WorkspaceNavigationMapContextJsonBuilder"/> (Roslyn/контекст в одном процессе).
/// </summary>
public sealed class WorkspaceNavigationMapContextJsonDataSource : IGraphDataSource
{
    public string BuildNavigationJson(CodeNavigationMapJsonRequest request) =>
        WorkspaceNavigationMapContextJsonBuilder.Build(
            request.NormalizedLevel,
            request.WantGraph,
            request.CurrentPath,
            request.EditorText,
            request.CursorLine,
            request.CursorColumn,
            request.RawFilePathsFromSolution,
            request.SolutionPath,
            request.NavSettings);
}
