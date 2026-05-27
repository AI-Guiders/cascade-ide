using CascadeIDE.Models;
using CascadeIDE.Services.CodeNavigation;

namespace CascadeIDE.Features.IdeMcp.Application;

internal sealed partial class MainWindowIdeMcpHost
{

    /// <inheritdoc />
    public Task<string> GetCodeNavigationContextAsync(
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
        if (!IdeMcpNavigationOrchestrator.TryNormalizeRequestedMode(mode, out var requestedMode))
            return Task.FromResult(IdeMcpNavigationOrchestrator.BuildInvalidModeJson());

        var (effectiveLevel, effectiveMode) = IdeMcpNavigationOrchestrator.ResolveEffectiveLevelAndMode(
            level,
            _host.McpSettings.CodeNavigationMap.Depth,
            requestedMode);

        if (effectiveLevel == CodeNavigationMapLevelKind.ControlFlow)
        {
            var (effectiveLine, effectiveColumn) = IdeMcpNavigationOrchestrator.ResolveControlFlowLineColumn(
                line,
                column,
                _host.EditorText,
                _host.McpEditorCaretOffset ?? _host.EditorSelectionStart);

            var maxN = maxNodes ?? CodeNavigationContextBuilder.DefaultControlFlowSubgraphMaxNodes;
            var maxE = maxEdges ?? CodeNavigationContextBuilder.DefaultControlFlowSubgraphMaxEdges;
            var path = string.IsNullOrWhiteSpace(filePath) ? _host.CurrentFilePath : filePath;
            var grain = CodeNavigationMapControlFlowGrainKind.Normalize(_host.McpSettings.CodeNavigationMap.ControlFlowGrain);

            return UiScheduler.Default.InvokeAsync(() =>
                CodeNavigationMapControlFlowGrainKind.IsDetailed(grain)
                    ? CodeNavigationControlFlowSubgraphBuilder.BuildJson(
                        path,
                        _host.EditorText,
                        effectiveLine,
                        effectiveColumn,
                        maxN,
                        maxE)
                    : CodeNavigationMethodIntentSubgraphBuilder.BuildJson(
                        path,
                        _host.EditorText,
                        effectiveLine,
                        effectiveColumn,
                        maxN,
                        maxE));
        }

        return UiScheduler.Default.InvokeAsync(() =>
            CodeNavigationContextBuilder.BuildJson(
                effectiveMode,
                filePath,
                _host.CurrentFilePath,
                _host.Workspace.SolutionRoots,
                _host.Workspace.SolutionPath,
                line,
                column,
                maxRelated ?? CodeNavigationContextBuilder.DefaultMaxRelated,
                maxNodes ?? CodeNavigationContextBuilder.DefaultMaxNodes,
                maxEdges ?? CodeNavigationContextBuilder.DefaultMaxEdges,
                includeKinds,
                excludeKinds,
                preset,
                _host.McpSettings.CodeNavigation));
    }

}
