#nullable enable
using CascadeIDE.Services.CodeNavigation;
using CascadeIDE.Features.IdeMcp.Application;

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
        if (!IdeMcpNavigationOrchestrator.TryNormalizeRequestedMode(mode, out var requestedMode))
            return Task.FromResult(IdeMcpNavigationOrchestrator.BuildInvalidModeJson());

        var (effectiveLevel, effectiveMode) = IdeMcpNavigationOrchestrator.ResolveEffectiveLevelAndMode(
            level,
            _settings.CodeNavigationMap.Depth,
            requestedMode);

        if (effectiveLevel == Models.CodeNavigationMapLevelKind.ControlFlow)
        {
            var effectiveLine = line;
            var effectiveColumn = column;
            if (effectiveLine is null || effectiveLine <= 0 || effectiveColumn is null || effectiveColumn <= 0)
            {
                var (derivedLine, derivedColumn) = ComputeLineColumn(EditorText, _editorCaretOffset ?? EditorSelectionStart);
                (effectiveLine, effectiveColumn) = IdeMcpNavigationOrchestrator.ResolveLineColumnForControlFlow(
                    effectiveLine,
                    effectiveColumn,
                    derivedLine,
                    derivedColumn);
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
