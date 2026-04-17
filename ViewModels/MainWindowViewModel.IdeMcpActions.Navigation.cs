namespace CascadeIDE.ViewModels;

/// <summary>MCP: семантическая навигация (ADR 0039).</summary>
public partial class MainWindowViewModel
{
    /// <inheritdoc />
    Task<string> Services.IIdeMcpActions.GetWorkspaceNavigationContextAsync(
        string mode,
        string? filePath,
        int? line,
        int? column,
        int? maxRelated,
        int? maxNodes,
        int? maxEdges,
        string? preset,
        IReadOnlyList<string>? includeKinds,
        IReadOnlyList<string>? excludeKinds,
        string? level)
    {
        var requestedMode = string.IsNullOrWhiteSpace(mode) ? "related" : mode.Trim().ToLowerInvariant();
        if (requestedMode is not ("related" or "subgraph"))
            return Task.FromResult("""{"error":"invalid_mode","message":"mode must be related or subgraph."}""");

        var configuredLevel = _settings.SemanticMap.Level;
        var effectiveLevel = Models.SemanticMapLevelKind.Normalize(string.IsNullOrWhiteSpace(level) ? configuredLevel : level);
        var effectiveMode = effectiveLevel == Models.SemanticMapLevelKind.ControlFlow ? "subgraph" : requestedMode;

        return UiScheduler.Default.InvokeAsync(() =>
            WorkspaceNavigationContextBuilder.BuildJson(
                effectiveMode,
                filePath,
                CurrentFilePath,
                Workspace.SolutionRoots,
                Workspace.SolutionPath,
                line,
                column,
                maxRelated ?? WorkspaceNavigationContextBuilder.DefaultMaxRelated,
                maxNodes ?? WorkspaceNavigationContextBuilder.DefaultMaxNodes,
                maxEdges ?? WorkspaceNavigationContextBuilder.DefaultMaxEdges,
                includeKinds,
                excludeKinds,
                preset,
                _settings.WorkspaceNavigationContext));
    }
}
