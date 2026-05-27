using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Cockpit.Composition.TraceFlow;
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Services;
using System.IO;

namespace CascadeIDE.ViewModels;

/// <summary>Срез карты workspace: перезапрос refresh и сборка через <see cref="WorkspaceNavigationMapRefreshComposer"/>.</summary>
public partial class MainWindowViewModel
{
    private void ScheduleWorkspaceNavigationMapRefresh()
    {
        _workspaceNavigationMapRefreshCts?.Cancel();
        var cts = new CancellationTokenSource();
        _workspaceNavigationMapRefreshCts = cts;
        _ = RunWorkspaceNavigationMapRefreshAsync(cts.Token);
    }

    /// <summary>Вызывается из <c>WorkspaceNavigationMapView</c> при изменении ширины мини-карты.</summary>
    internal void NotifyCodeNavigationMapGraphViewportWidthChanged(double width)
    {
        if (!CodeNavigationMapViewportPolicy.ShouldApplyMeasuredWidth(width, CodeNavigationMapGraphWidth, out var clamped))
            return;
        CodeNavigationMapGraphWidth = clamped;
        ScheduleWorkspaceNavigationMapRefresh();
    }

    private async Task RunWorkspaceNavigationMapRefreshAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(280, ct).ConfigureAwait(false);
        }
        catch
        {
            return;
        }

        List<string> rawPaths = [];
        List<string> openDocumentPaths = [];
        string? currentPath = null;
        string? anchorPath = null;
        string? solutionPath = null;
        string? editorText = null;
        int? cursorLine = null;
        int? cursorColumn = null;
        int? caretOffset = null;
        CodeNavigationSettings? navSettings = null;
        var wantList = false;
        var wantGraph = false;
        var level = CodeNavigationMapLevelKind.File;
        CockpitSurfaceState? cockpitSurfaceCapturedOnUi = null;
        await UiScheduler.Default.InvokeAsync(() =>
        {
            rawPaths = McpSolutionTree.CollectFileEntries(Workspace.SolutionRoots).Select(e => e.FullPath).ToList();
            openDocumentPaths = Documents.OpenDocuments
                .Select(d => d.FilePath)
                .Where(fp => !string.IsNullOrEmpty(fp))
                .Select(fp => fp!)
                .ToList();
            currentPath = CurrentFilePath;
            var curOrder = currentPath ?? "";
            openDocumentPaths.Sort((a, b) =>
            {
                bool Match(string d) =>
                    !string.IsNullOrEmpty(curOrder) && EditorTextCoordinateUtilities.PathsReferToSameFile(d, curOrder);
                var am = Match(a);
                var bm = Match(b);
                if (am == bm)
                    return 0;
                return am ? -1 : 1;
            });
            anchorPath = WorkspaceNavigationMapAnchorResolver.Resolve(currentPath, openDocumentPaths, rawPaths);
            solutionPath = Workspace.SolutionPath;
            editorText = EditorText;
            caretOffset = _editorCaretOffset ?? EditorSelectionStart;
            var (line, column) = WorkspaceNavigationMapOrchestrator.ComputeLineColumn(EditorText, caretOffset);
            cursorLine = line;
            cursorColumn = column;
            navSettings = _settings.CodeNavigation;
            var sm = _settings.CodeNavigationMap;
            wantList = sm.WantsCodeNavigationMapList;
            wantGraph = sm.WantsCodeNavigationMapGraph;
            level = CodeNavigationMapLevelKind.Normalize(sm.Depth);
            if (level == CodeNavigationMapLevelKind.ControlFlow)
                cockpitSurfaceCapturedOnUi = CockpitSurfaceSnapshotBuilder.Build(this);
        });

        var navigationPath = WorkspaceNavigationMapOrchestrator.ResolveNavigationPathForGraphJson(
            level,
            currentPath,
            anchorPath,
            rawPaths);

        if (!string.IsNullOrEmpty(navigationPath)
            && !EditorTextCoordinateUtilities.PathsReferToSameFile(navigationPath, currentPath ?? ""))
        {
            try
            {
                if (File.Exists(navigationPath))
                    editorText = File.ReadAllText(navigationPath);
            }
            catch
            {
                // keep editor text as-is
            }
        }

        if (level == CodeNavigationMapLevelKind.ControlFlow)
        {
            (cursorLine, cursorColumn) = WorkspaceNavigationMapOrchestrator.ResolveControlFlowCursorForRefresh(
                navigationPath,
                currentPath,
                editorText,
                caretOffset);
        }

        if (ct.IsCancellationRequested)
            return;

        var useSubgraphMode = level == CodeNavigationMapLevelKind.ControlFlow || wantGraph;

        string json;
        try
        {
            json = await Task.Run(
                    () => _codeNavigationMapGraphDataSource.BuildNavigationJson(
                        new GraphNavigationJsonRequest(
                            level,
                            wantGraph,
                            navigationPath,
                            editorText,
                            cursorLine,
                            cursorColumn,
                            rawPaths,
                            solutionPath,
                            navSettings)),
                    ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (ct.IsCancellationRequested)
            return;

        var deps = WorkspaceNavigationMapGraphComposition.CreateRefreshDependencies(
            _traceFlowChannelCoordinator,
            _traceFlowCdsRouter,
            _traceFlowSurfaceCompositor);
        var dry = WorkspaceNavigationMapRefreshComposer.Compose(
            deps,
            json,
            useSubgraphMode,
            wantList,
            navigationPath,
            solutionPath,
            level,
            CodeNavigationMapGraphWidth,
            CodeNavigationMapGraphHeight,
            _settings.CodeNavigationMap.NormalizedDetailLevel,
            _settings.CodeNavigationMap.NormalizedRelatedGraphLayout,
            _settings.CodeNavigationMap.NormalizedControlFlowMainAxis,
            new WorkspaceNavigationMapRefreshComposer.TraceSignals(ImpactedTestsBadge, LastTestSummary),
            cockpitSurfaceCapturedOnUi);

        SemanticMapHciOrientationSnapshot? hciSnap = null;
        if (!ct.IsCancellationRequested)
        {
            try
            {
                var wsRoot = GetWorkspacePath();
                hciSnap = await SemanticMapHciOrientationAcquirer.TryAcquireAsync(
                        _hybridIndex,
                        _settings.HybridIndex,
                        wsRoot,
                        solutionPath,
                        navigationPath,
                        ct)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // refresh superseded
            }
            catch
            {
                hciSnap = new SemanticMapHciOrientationSnapshot([], "", "запрос HCI не выполнен");
            }
        }

        var hciLine = SemanticMapHciOrientationFormatting.ToStatusLine(hciSnap);

        await UiScheduler.Default.InvokeAsync(() =>
        {
            if (ct.IsCancellationRequested)
                return;
            WorkspaceNavigationMapAnchorLabel = dry.AnchorLabel;
            WorkspaceNavigationMapStatus = dry.Status;
            WorkspaceNavigationMapRelatedCount = dry.AccentCount;
            CodeNavigationMapGraphScene = dry.Scene;
            CodeNavigationMapGraphHeight = dry.GraphHeight;
            WorkspaceNavigationMapCfAnchorFullPath = dry.CfAnchorFullPath;
            WorkspaceNavigationMapHciOrientationLine = hciLine;
            WorkspaceNavigationMapItems.Clear();
            foreach (var parsed in dry.ListRows)
            {
                WorkspaceNavigationMapItems.Add(new WorkspaceNavigationMapItemVm
                {
                    FullPath = parsed.FullPath,
                    RelativePath = parsed.RelativePath,
                    Kind = parsed.Kind,
                    Rationale = parsed.Rationale
                });
            }
        });
    }
}
