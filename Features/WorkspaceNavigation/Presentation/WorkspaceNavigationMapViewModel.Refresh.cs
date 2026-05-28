#nullable enable

using AvaloniaEdit;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Composition.TraceFlow;
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Features.Editor;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.WorkspaceNavigation.Presentation;

/// <summary>Срез карты workspace: перезапрос refresh и сборка через <see cref="WorkspaceNavigationMapRefreshComposer"/>.</summary>
public sealed partial class WorkspaceNavigationMapViewModel
{
    internal void ScheduleWorkspaceNavigationMapRefresh()
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
        string? cfGraphNavigatePath = null;
        int? cfGraphNavigateLine = null;
        int? cfGraphNavigateColumn = null;
        CodeNavigationSettings? navSettings = null;
        string controlFlowGrain = CodeNavigationMapControlFlowGrainKind.Intent;
        var wantList = false;
        var wantGraph = false;
        var level = CodeNavigationMapLevelKind.File;
        CockpitSurfaceState? cockpitSurfaceCapturedOnUi = null;
        await UiScheduler.Default.InvokeAsync(() =>
        {
            rawPaths = McpSolutionTree.CollectFileEntries(_host.Workspace.SolutionRoots).Select(e => e.FullPath).ToList();
            openDocumentPaths = _host.Documents.OpenDocuments
                .Select(d => d.FilePath)
                .Where(fp => !string.IsNullOrEmpty(fp))
                .Select(fp => fp!)
                .ToList();
            currentPath = _host.CurrentFilePath;
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
            solutionPath = _host.Workspace.SolutionPath;
            editorText = _host.EditorText;
            caretOffset = TryCaptureLiveEditorCaretOffset(currentPath)
                ?? _editorCaretOffset
                ?? _host.EditorSelectionStart;
            if (TryCaptureLiveEditorText(currentPath) is { } liveText)
                editorText = liveText;
            navSettings = _host.Settings.CodeNavigation;
            var sm = _host.Settings.CodeNavigationMap;
            wantList = sm.WantsCodeNavigationMapList;
            wantGraph = sm.WantsCodeNavigationMapGraph;
            level = CodeNavigationMapLevelKind.Normalize(CodeNavigationMapLevel);
            controlFlowGrain = CodeNavigationMapControlFlowGrainKind.Normalize(sm.ControlFlowGrain);
            if (level == CodeNavigationMapLevelKind.ControlFlow)
            {
                cockpitSurfaceCapturedOnUi = CockpitSurfaceSnapshotBuilder.Build(_host.Shell);
                cfGraphNavigatePath = _controlFlowGraphNavigatePath;
                cfGraphNavigateLine = _controlFlowGraphNavigateLine;
                cfGraphNavigateColumn = _controlFlowGraphNavigateColumn;
            }
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
                if (WorkspaceTextFileReader.TryReadAllText(navigationPath, out var navText))
                    editorText = navText;
            }
            catch
            {
                // keep editor text as-is
            }
        }

        if (level == CodeNavigationMapLevelKind.ControlFlow)
        {
            int? navigateLine = null;
            int? navigateColumn = null;
            if (cfGraphNavigateLine is > 0
                && EditorTextCoordinateUtilities.PathsReferToSameFile(cfGraphNavigatePath, navigationPath))
            {
                navigateLine = cfGraphNavigateLine;
                navigateColumn = cfGraphNavigateColumn ?? 1;
            }

            (cursorLine, cursorColumn) = WorkspaceNavigationMapOrchestrator.ResolveControlFlowCursorForRefresh(
                navigationPath,
                currentPath,
                editorText,
                caretOffset,
                navigateLine,
                navigateColumn);
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
                            navSettings,
                            controlFlowGrain)),
                    ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            await UiScheduler.Default.InvokeAsync(() =>
            {
                if (ct.IsCancellationRequested)
                    return;
                WorkspaceNavigationMapStatus = $"Не удалось построить контекст карты: {ex.Message}";
                CodeNavigationMapGraphScene = null;
                WorkspaceNavigationMapItems.Clear();
                WorkspaceNavigationMapRelatedCount = 0;
            });
            return;
        }

        if (ct.IsCancellationRequested)
            return;

        var deps = WorkspaceNavigationMapGraphComposition.CreateRefreshDependencies(
            _traceFlowChannelCoordinator,
            _traceFlowCdsRouter,
            _traceFlowSurfaceCompositor);
        var graphWidth = SanitizeMapViewportDimension(CodeNavigationMapGraphWidth, CodeNavigationMapCompositor.DefaultWidth);
        var graphHeight = SanitizeMapViewportDimension(CodeNavigationMapGraphHeight, CodeNavigationMapCompositor.DefaultHeightFile);

        var dry = WorkspaceNavigationMapRefreshComposer.Compose(
            deps,
            json,
            useSubgraphMode,
            wantList,
            navigationPath,
            solutionPath,
            level,
            graphWidth,
            graphHeight,
            _host.Settings.CodeNavigationMap.NormalizedDetailLevel,
            _host.Settings.CodeNavigationMap.NormalizedRelatedGraphLayout,
            _host.Settings.CodeNavigationMap.NormalizedControlFlowMainAxis,
            _host.Settings.CodeNavigationMap,
            new WorkspaceNavigationMapRefreshComposer.TraceSignals(_host.ImpactedTestsBadge, _host.LastTestSummary),
            cockpitSurfaceCapturedOnUi);

        var workspaceRoot = _host.GetWorkspacePath();
        SemanticMapHciOrientationSnapshot? hciSnap = null;
        if (!ct.IsCancellationRequested)
        {
            try
            {
                hciSnap = await SemanticMapHciOrientationAcquirer.TryAcquireAsync(
                        _host.HybridIndex,
                        _host.Settings.HybridIndex,
                        workspaceRoot,
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
        var (featureLine, featureDocPaths, docsCoverageLine, adrLine, adrFirstDocPath, adrDocPaths) =
            TryResolveWorkspaceCorrespondence(workspaceRoot, navigationPath);

        await UiScheduler.Default.InvokeAsync(() =>
        {
            try
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
                WorkspaceFeatureLine = featureLine;
                WorkspaceFeatureDocPaths = featureDocPaths;
                WorkspaceAdrCorrespondenceLine = adrLine;
                WorkspaceAdrCorrespondenceFirstDocPath = adrFirstDocPath;
                WorkspaceDocsCoverageLine = docsCoverageLine;
                ApplyWorkspaceCorrespondenceLayers(hciLine, featureLine, adrLine, dry);
                ApplyCorrespondenceDocAndReverseLists(workspaceRoot, navigationPath, adrDocPaths);
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
            }
            finally
            {
                if (cfGraphNavigateLine is > 0)
                    ClearControlFlowGraphNodeNavigationAnchor();
            }
        });
    }

    private void ApplyWorkspaceCorrespondenceLayers(
        string hciLine,
        string featureLine,
        string adrLine,
        WorkspaceNavigationMapRefreshComposer.DryResult dry)
    {
        var hasCodeGraph = dry.AccentCount > 0
            || dry.ListRows.Count > 0
            || dry.Scene?.Nodes.Count > 0;

        var signals = CorrespondenceLayersProjection.FromCorrespondence(
            hasHciOrientation: !string.IsNullOrWhiteSpace(hciLine),
            hasFeature: !string.IsNullOrWhiteSpace(featureLine),
            hasAdrDocs: !string.IsNullOrWhiteSpace(adrLine),
            hasCodeGraph: hasCodeGraph);

        WorkspaceCorrespondenceLayersLine = CorrespondenceLayersProjection.BuildLayersBadge(signals);
        WorkspaceCorrespondenceLayersTooltip = CorrespondenceLayersProjection.BuildLayersTooltip(signals);
    }

    private static (string FeatureLine, string[] FeatureDocPaths, string DocsCoverageLine, string AdrLine, string? AdrFirstDocPath, string[] AdrDocPaths)
        TryResolveWorkspaceCorrespondence(string? workspaceRoot, string? navigationPath)
    {
        var r = WorkspaceCorrespondenceResolver.Resolve(workspaceRoot, navigationPath);
        return (r.FeatureLine, r.FeatureDocPaths, r.DocsCoverageLine, r.AdrLine, r.AdrFirstDocPath, r.AdrDocPaths);
    }

    private int? TryCaptureLiveEditorCaretOffset(string? currentPath)
    {
        foreach (var editor in EnumerateEditorsForPath(currentPath))
            return editor.TextArea.Caret.Offset;

        return null;
    }

    private string? TryCaptureLiveEditorText(string? currentPath)
    {
        foreach (var editor in EnumerateEditorsForPath(currentPath))
            return editor.Document.Text;

        return null;
    }

    private static double SanitizeMapViewportDimension(double value, double fallback) =>
        double.IsFinite(value) && value > 0 ? value : fallback;

    internal IEnumerable<TextEditor> EnumerateEditorsForPath(string? currentPath)
    {
        if (string.IsNullOrWhiteSpace(currentPath))
            yield break;

        var direct = EditorActiveDockResolver.TryGetEditor(_host.Shell, currentPath);
        if (direct is not null)
        {
            yield return direct;
            yield break;
        }

        foreach (var doc in _host.Documents.OpenDocuments)
        {
            if (string.IsNullOrEmpty(doc.FilePath))
                continue;
            if (!EditorTextCoordinateUtilities.PathsReferToSameFile(doc.FilePath, currentPath))
                continue;

            var editor = EditorActiveDockResolver.TryGetEditor(_host.Shell, doc.FilePath);
            if (editor is not null)
                yield return editor;
        }
    }
}
