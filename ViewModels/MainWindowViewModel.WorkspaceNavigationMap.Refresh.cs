using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Cockpit.Composition.TraceFlow;
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Services;
using CascadeIDE.Features.UiChrome;
using System.IO;
using System.Linq;

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
            caretOffset = TryCaptureLiveEditorCaretOffset(currentPath)
                ?? _editorCaretOffset
                ?? EditorSelectionStart;
            if (TryCaptureLiveEditorText(currentPath) is { } liveText)
                editorText = liveText;
            navSettings = _settings.CodeNavigation;
            var sm = _settings.CodeNavigationMap;
            wantList = sm.WantsCodeNavigationMapList;
            wantGraph = sm.WantsCodeNavigationMapGraph;
            level = CodeNavigationMapLevelKind.Normalize(CodeNavigationMapLevel);
            controlFlowGrain = CodeNavigationMapControlFlowGrainKind.Normalize(sm.ControlFlowGrain);
            if (level == CodeNavigationMapLevelKind.ControlFlow)
            {
                cockpitSurfaceCapturedOnUi = CockpitSurfaceSnapshotBuilder.Build(this);
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
            _settings.CodeNavigationMap.NormalizedDetailLevel,
            _settings.CodeNavigationMap.NormalizedRelatedGraphLayout,
            _settings.CodeNavigationMap.NormalizedControlFlowMainAxis,
            _settings.CodeNavigationMap,
            new WorkspaceNavigationMapRefreshComposer.TraceSignals(ImpactedTestsBadge, LastTestSummary),
            cockpitSurfaceCapturedOnUi);

        var workspaceRoot = GetWorkspacePath();
        SemanticMapHciOrientationSnapshot? hciSnap = null;
        if (!ct.IsCancellationRequested)
        {
            try
            {
                hciSnap = await SemanticMapHciOrientationAcquirer.TryAcquireAsync(
                        _hybridIndex,
                        _settings.HybridIndex,
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
        var (featureLine, featureDocPaths, docsCoverageLine, adrLine, adrFirstDocPath) =
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

    private static (string FeatureLine, string[] FeatureDocPaths, string DocsCoverageLine, string AdrLine, string? AdrFirstDocPath)
        TryResolveWorkspaceCorrespondence(string? workspaceRoot, string? navigationPath)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || string.IsNullOrWhiteSpace(navigationPath))
            return ("", [], "", "", null);

        try
        {
            var root = workspaceRoot.Trim();
            var path = Path.Combine(root, ".cascade", "workspace.toml");
            if (!File.Exists(path))
                return ("", [], "", "", null);

            var toml = File.ReadAllText(path);
            var parsed = CascadeTomlSerializer.Deserialize<UiWorkspaceToml>(toml);
            var feature = WorkspaceFeatureResolver.ResolveFeatureFromWorkspaceToml(parsed, root, navigationPath.Trim());
            var featureLine = WorkspaceFeatureResolver.BuildFeatureLine(feature);
            var featureDocs = feature?.Docs is { Count: > 0 }
                ? feature.Docs.Select(x => (x ?? "").Trim()).Where(x => x.Length > 0).ToArray()
                : [];

            var docPaths = new List<string>();
            if (feature?.Docs is { Count: > 0 })
                docPaths.AddRange(feature.Docs.Select(x => (x ?? "").Trim()).Where(x => x.Length > 0));

            var mapped = WorkspaceAdrMapResolver.ResolveAdrDocPathsFromWorkspaceToml(parsed, root, navigationPath.Trim());
            foreach (var m in mapped)
            {
                if (docPaths.Contains(m, StringComparer.OrdinalIgnoreCase))
                    continue;
                docPaths.Add(m);
            }

            // Optional auto-include of linked ADRs (one hop).
            var autoInclude = WorkspaceAdrMapResolver.NormalizeAutoInclude(parsed?.Workspace?.Adr?.AutoInclude);
            var maxRelated = parsed?.Workspace?.Adr?.MaxRelated is int mr && mr > 0 ? mr : 8;
            if (autoInclude == WorkspaceAdrMapResolver.AutoIncludeLinked && docPaths.Count > 0)
            {
                var primary = docPaths[0];
                var absPrimary = WorkspaceAdrMapResolver.TryResolveAbsoluteDocPath(root, primary);
                if (!string.IsNullOrWhiteSpace(absPrimary) && File.Exists(absPrimary))
                {
                    var md = File.ReadAllText(absPrimary);
                    var linked = WorkspaceAdrMapResolver.ExtractLinkedAdrDocPathsFromMarkdown(md, primary);
                    if (linked.Count > 0)
                    {
                        var merged = new List<string>(docPaths);
                        foreach (var l in linked)
                        {
                            if (merged.Count >= docPaths.Count + maxRelated)
                                break;
                            if (merged.Contains(l, StringComparer.OrdinalIgnoreCase))
                                continue;
                            merged.Add(l);
                        }
                        docPaths = merged;
                    }
                }
            }

            var adrLine = WorkspaceAdrMapResolver.BuildAdrIndicatorLine(docPaths);
            var firstRel = docPaths.Count > 0 ? docPaths[0] : null;
            var firstAbs = firstRel is null ? null : WorkspaceAdrMapResolver.TryResolveAbsoluteDocPath(root, firstRel);

            var docsCoverageLine =
                docPaths.Count > 0
                    ? ""
                    : feature is not null
                        ? "Docs: missing (feature has no ADRs) · template: docs/templates/feature.md"
                        : "Docs: missing (no correspondence) · template: docs/templates/module.md";

            return (featureLine, featureDocs, docsCoverageLine, adrLine, firstAbs);
        }
        catch
        {
            return ("", [], "", "", null);
        }
    }

    /// <summary>Каретка из видимого <see cref="AvaloniaEdit.TextEditor"/> (не только throttled <see cref="_editorCaretOffset"/>).</summary>
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

    private IEnumerable<AvaloniaEdit.TextEditor> EnumerateEditorsForPath(string? currentPath)
    {
        if (string.IsNullOrWhiteSpace(currentPath))
            yield break;

        var direct = EditorActiveDockResolver.TryGetEditor(this, currentPath);
        if (direct is not null)
        {
            yield return direct;
            yield break;
        }

        foreach (var doc in Documents.OpenDocuments)
        {
            if (string.IsNullOrEmpty(doc.FilePath))
                continue;
            if (!EditorTextCoordinateUtilities.PathsReferToSameFile(doc.FilePath, currentPath))
                continue;

            var editor = EditorActiveDockResolver.TryGetEditor(this, doc.FilePath);
            if (editor is not null)
                yield return editor;
        }
    }
}
