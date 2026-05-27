#nullable enable
using CascadeIDE.Contracts;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Services.CodeNavigation;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>
/// Сборка JSON контекста карты намерений для PFD (тяжёлая работа — вызывать из фона, см. refresh в <c>MainWindowViewModel.WorkspaceNavigationMap</c>).
/// </summary>
[ComputingUnit]
public static class WorkspaceNavigationMapContextJsonBuilder
{
    /// <summary>
    /// Должен совпадать с логикой refresh: control flow всегда отдельная ветка; иначе <c>subgraph</c> при <paramref name="wantGraph"/> или уровне control flow уже обработанным выше.
    /// </summary>
    /// <param name="normalizedLevel">Результат <see cref="CodeNavigationMapLevelKind.Normalize"/></param>
    /// <param name="rawFilePathsFromSolution">Полные пути из <see cref="McpSolutionTree.CollectFileEntries"/>.</param>
    /// <param name="controlFlowGrain">Канон <see cref="CodeNavigationMapControlFlowGrainKind"/>; null → intent.</param>
    public static string Build(
        string normalizedLevel,
        bool wantGraph,
        string? currentPath,
        string? editorText,
        int? cursorLine,
        int? cursorColumn,
        IEnumerable<string> rawFilePathsFromSolution,
        string? solutionPath,
        CodeNavigationSettings? navSettings,
        string? controlFlowGrain = null)
    {
        var settings = navSettings ?? new CodeNavigationSettings();
        var useSubgraphMode = normalizedLevel == CodeNavigationMapLevelKind.ControlFlow || wantGraph;

        if (normalizedLevel == CodeNavigationMapLevelKind.ControlFlow)
        {
            var grain = CodeNavigationMapControlFlowGrainKind.Normalize(controlFlowGrain);
            if (CodeNavigationMapControlFlowGrainKind.IsDetailed(grain))
            {
                return CodeNavigationControlFlowSubgraphBuilder.BuildJson(
                    currentPath,
                    editorText,
                    cursorLine,
                    cursorColumn,
                    CodeNavigationContextBuilder.DefaultControlFlowSubgraphMaxNodes,
                    CodeNavigationContextBuilder.DefaultControlFlowSubgraphMaxEdges);
            }

            return CodeNavigationMethodIntentSubgraphBuilder.BuildJson(
                currentPath,
                editorText,
                cursorLine,
                cursorColumn,
                CodeNavigationContextBuilder.DefaultControlFlowSubgraphMaxNodes,
                CodeNavigationContextBuilder.DefaultControlFlowSubgraphMaxEdges);
        }

        if (useSubgraphMode)
        {
            return CodeNavigationContextBuilder.BuildJson(
                "subgraph",
                null,
                currentPath,
                rawFilePathsFromSolution,
                solutionPath,
                null,
                null,
                CodeNavigationContextBuilder.DefaultMaxRelated,
                CodeNavigationContextBuilder.DefaultMaxNodes,
                CodeNavigationContextBuilder.DefaultMaxEdges,
                null,
                null,
                null,
                settings);
        }

        return CodeNavigationContextBuilder.BuildJson(
            "related",
            null,
            currentPath,
            rawFilePathsFromSolution,
            solutionPath,
            null,
            null,
            CodeNavigationContextBuilder.DefaultMaxRelated,
            CodeNavigationContextBuilder.DefaultMaxNodes,
            CodeNavigationContextBuilder.DefaultMaxEdges,
            null,
            null,
            null,
            settings);
    }
}
