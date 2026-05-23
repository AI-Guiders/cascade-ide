using CascadeIDE.ViewModels;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;
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

        if (effectiveLevel == Models.CodeNavigationMapLevelKind.ControlFlow)
        {
            var (effectiveLine, effectiveColumn) = IdeMcpNavigationOrchestrator.ResolveControlFlowLineColumn(
                line,
                column,
                _host.EditorText,
                _host.McpEditorCaretOffset ?? _host.EditorSelectionStart);

            return UiScheduler.Default.InvokeAsync(() =>
                CodeNavigationControlFlowSubgraphBuilder.BuildJson(
                    string.IsNullOrWhiteSpace(filePath) ? _host.CurrentFilePath : filePath,
                    _host.EditorText,
                    effectiveLine,
                    effectiveColumn,
                    maxNodes ?? CodeNavigationContextBuilder.DefaultMaxNodes,
                    maxEdges ?? CodeNavigationContextBuilder.DefaultMaxEdges));
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
