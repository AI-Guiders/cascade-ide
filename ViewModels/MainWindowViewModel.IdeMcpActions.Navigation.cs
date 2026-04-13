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
        IReadOnlyList<string>? excludeKinds) =>
        UiScheduler.Default.InvokeAsync(() =>
            WorkspaceNavigationContextBuilder.BuildJson(
                mode,
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
