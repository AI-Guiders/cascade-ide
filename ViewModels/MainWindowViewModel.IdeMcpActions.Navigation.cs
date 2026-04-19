#nullable enable
using CascadeIDE.Services;
using CascadeIDE.Services.CodeNavigation;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: семантическая навигация (ADR 0039).</summary>
public partial class MainWindowViewModel
{
    /// <inheritdoc />
    Task<string> Services.IIdeMcpActions.GetCodeNavigationContextAsync(
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

        var configuredLevel = _settings.SemanticMap.Depth;
        var effectiveLevel = Models.SemanticMapLevelKind.Normalize(string.IsNullOrWhiteSpace(level) ? configuredLevel : level);
        var effectiveMode = effectiveLevel == Models.SemanticMapLevelKind.ControlFlow ? "subgraph" : requestedMode;

        if (effectiveLevel == Models.SemanticMapLevelKind.ControlFlow)
        {
            var effectiveLine = line;
            var effectiveColumn = column;
            if (effectiveLine is null || effectiveLine <= 0 || effectiveColumn is null || effectiveColumn <= 0)
            {
                var (derivedLine, derivedColumn) = ComputeLineColumn(EditorText, _editorCaretOffset ?? EditorSelectionStart);
                if (effectiveLine is null || effectiveLine <= 0)
                    effectiveLine = derivedLine;
                if (effectiveColumn is null || effectiveColumn <= 0)
                    effectiveColumn = derivedColumn;
            }

            return UiScheduler.Default.InvokeAsync(() =>
                CodeNavigationControlFlowSubgraphBuilder.BuildJson(
                    string.IsNullOrWhiteSpace(filePath) ? CurrentFilePath : filePath,
                    EditorText,
                    effectiveLine,
                    effectiveColumn,
                    maxNodes ?? CodeNavigationContextBuilder.DefaultMaxNodes,
                    maxEdges ?? CodeNavigationContextBuilder.DefaultMaxEdges));
        }

        return UiScheduler.Default.InvokeAsync(() =>
            CodeNavigationContextBuilder.BuildJson(
                effectiveMode,
                filePath,
                CurrentFilePath,
                Workspace.SolutionRoots,
                Workspace.SolutionPath,
                line,
                column,
                maxRelated ?? CodeNavigationContextBuilder.DefaultMaxRelated,
                maxNodes ?? CodeNavigationContextBuilder.DefaultMaxNodes,
                maxEdges ?? CodeNavigationContextBuilder.DefaultMaxEdges,
                includeKinds,
                excludeKinds,
                preset,
                _settings.CodeNavigation));
    }
}
