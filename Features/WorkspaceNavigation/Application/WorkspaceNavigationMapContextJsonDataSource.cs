#nullable enable
using CascadeIDE.Cockpit.Graph;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>
/// Адаптер: текущая сборка JSON через <see cref="WorkspaceNavigationMapContextJsonBuilder"/> (Roslyn/контекст в одном процессе).
/// </summary>
public sealed class WorkspaceNavigationMapContextJsonDataSource : IGraphDataSource, IGraphDocumentSource
{
    public string BuildNavigationJson(GraphNavigationJsonRequest request) =>
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

    public bool TryBuildDocument(GraphNavigationJsonRequest request, out GraphDocument? document, out string? wireJson)
    {
        wireJson = BuildNavigationJson(request);
        if (GraphDocumentJson.TryParse(wireJson, out document, out _))
            return true;
        document = null;
        return false;
    }
}
