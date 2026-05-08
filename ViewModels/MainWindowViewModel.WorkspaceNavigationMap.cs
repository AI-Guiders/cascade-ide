using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia.Controls;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Cockpit.Composition.TraceFlow;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using CascadeIDE.Services.CodeNavigation;
using CascadeIDE.Services.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Слот Pfd: <b>отображение</b> карты намерений / <see cref="CodeNavigationMapSubgraphDocument"/> (те же данные, что JSON MCP). Граф подграфа — не синоним <c>instrument_id</c>, см. ADR 0065.
/// По доменам: <b>карта намерений</b> (в т.ч. control flow) — CodeNavigation; <b>зависимости файлов</b> — WorkspaceNavigation; <b>submodules</b> — дерево/GitMap (ADR 0062).
/// </summary>
public partial class MainWindowViewModel
{
    private readonly CodeNavigationMapCompositor _codeNavigationMapCompositor = new();
    private readonly TraceFlowChannelCoordinator _traceFlowChannelCoordinator = new(
        [
            new CodeFlowTraceChannel(),
            new UnitTestTraceChannel()
        ]);
    private readonly ITraceFlowCdsRouter _traceFlowCdsRouter = new TraceFlowCdsRouter();
    private readonly ITraceFlowSurfaceCompositor _traceFlowSurfaceCompositor = new TraceFlowSurfaceCompositor();
    private int? _editorCaretOffset;

    private CancellationTokenSource? _workspaceNavigationMapRefreshCts;

    internal void UpdateCodeNavigationMapCaretOffset(int? offset)
    {
        _editorCaretOffset = offset;
        if (_settings.CodeNavigationMap.IsControlFlowDepth)
            ScheduleWorkspaceNavigationMapRefresh();
        ScheduleEditorHudBannerRefresh();
    }

    /// <summary>Связанные файлы для текущего якоря (режим списка).</summary>
    public ObservableCollection<WorkspaceNavigationMapItemVm> WorkspaceNavigationMapItems { get; } = new();

    /// <summary>Варианты <see cref="CodeNavigationMapPresentationKind"/> для ComboBox.</summary>
    public string[] CodeNavigationMapPresentationOptions { get; } =
        [CodeNavigationMapPresentationKind.List, CodeNavigationMapPresentationKind.Graph, CodeNavigationMapPresentationKind.Both];

    /// <summary>Варианты уровня карты: файловый и control flow.</summary>
    public string[] CodeNavigationMapLevelOptions { get; } =
        [CodeNavigationMapLevelKind.File, CodeNavigationMapLevelKind.ControlFlow];

    /// <summary>Сцена мини-карты (подграф + укладка по выбранному уровню карты).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WorkspaceNavigationMapHasRelated))]
    private CodeNavigationMapGraphSceneVm? _codeNavigationMapGraphScene;

    [ObservableProperty]
    private double _codeNavigationMapGraphHeight = CodeNavigationMapCompositor.DefaultHeightFile;

    /// <summary>Ширина области компоновки (совпадает с фактической шириной мини-карты в PFD).</summary>
    [ObservableProperty]
    private double _codeNavigationMapGraphWidth = CodeNavigationMapCompositor.DefaultWidth;

    /// <summary><c>list</c> | <c>graph</c> | <c>both</c> — синхронизируется с <c>[code_navigation_map]</c>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCodeNavigationMapList))]
    [NotifyPropertyChangedFor(nameof(ShowCodeNavigationMapGraph))]
    [NotifyPropertyChangedFor(nameof(WorkspaceNavigationMapHasRelated))]
    [NotifyPropertyChangedFor(nameof(CodeNavigationMapListAreaRowHeight))]
    [NotifyPropertyChangedFor(nameof(ShowCodeNavigationMapListOnPfd))]
    [NotifyPropertyChangedFor(nameof(ShowCodeNavigationMapGraphClickHint))]
    private string _codeNavigationMapPresentation = CodeNavigationMapPresentationKind.List;

    /// <summary><c>file</c> | <c>controlFlow</c> — уровень построения карты (секция <c>[code_navigation_map]</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCodeNavigationMapGraphClickHint))]
    private string _codeNavigationMapLevel = CodeNavigationMapLevelKind.File;

    /// <summary>Сообщение об ошибке или пустом состоянии (не null).</summary>
    [ObservableProperty]
    private string _workspaceNavigationMapStatus = "";

    /// <summary>Заголовок якоря: имя файла или «—».</summary>
    [ObservableProperty]
    private string _workspaceNavigationMapAnchorLabel = "—";

    /// <summary>Число связей для бейджа и Skia accent.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WorkspaceNavigationMapRelatedBadge))]
    [NotifyPropertyChangedFor(nameof(WorkspaceNavigationMapHasRelated))]
    private int _workspaceNavigationMapRelatedCount;

    /// <summary>Настройка <c>list</c>/<c>both</c>: список связанных ренерится на странице MFD <see cref="MfdShellPage.RelatedFiles"/>, не в колонке PFD.</summary>
    public bool ShowCodeNavigationMapList =>
        CodeNavigationMapPresentationProjection.ShowCodeNavigationMapList(CodeNavigationMapPresentation);

    /// <summary>Списка related на PFD нет (см. <see cref="MfdShellPage.RelatedFiles"/>).</summary>
    public bool ShowCodeNavigationMapListOnPfd =>
        CodeNavigationMapPresentationProjection.ShowCodeNavigationMapListOnPfd;

    /// <summary>Показать мини-карту подграфа.</summary>
    public bool ShowCodeNavigationMapGraph =>
        CodeNavigationMapPresentationProjection.ShowCodeNavigationMapGraph(CodeNavigationMapPresentation);

    /// <summary>
    /// Высота нижней строки Grid под список на PFD: список перенесён в MFD — всегда 0.
    /// </summary>
    public GridLength CodeNavigationMapListAreaRowHeight =>
        CodeNavigationMapPresentationProjection.ListAreaRowUsesStar(ShowCodeNavigationMapList, ShowCodeNavigationMapListOnPfd)
            ? new GridLength(1, GridUnitType.Star)
            : new GridLength(0);

    /// <summary>
    /// Режим <c>file</c>: подсказка «открыть файл» (в control flow клик ведёт к строке, не к файлу).
    /// </summary>
    public bool ShowCodeNavigationMapGraphClickHint =>
        CodeNavigationMapPresentationProjection.ShowCodeNavigationMapGraphClickHint(
            ShowCodeNavigationMapGraph,
            CodeNavigationMapLevel,
            CodeNavigationMapPresentation);

    /// <summary>Короткая подпись к количеству связей для шапки SM.</summary>
    public string WorkspaceNavigationMapRelatedBadge =>
        CodeNavigationMapPresentationProjection.WorkspaceNavigationMapRelatedBadge(WorkspaceNavigationMapRelatedCount);

    /// <summary>Есть ли контекст для accent (список или подграф с соседями).</summary>
    public bool WorkspaceNavigationMapHasRelated =>
        CodeNavigationMapPresentationProjection.WorkspaceNavigationMapHasRelated(
            WorkspaceNavigationMapRelatedCount,
            CodeNavigationMapGraphScene?.Nodes.Count);

    /// <summary>Открыть связанный файл из карты намерений (список related / узел графа).</summary>
    [RelayCommand]
    private void OpenWorkspaceNavigationRelated(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        Documents.OpenOrActivateDocument(path);
    }

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
        if (double.IsNaN(width) || width < 40)
            return;
        var clamped = Math.Clamp(width, 80, 2400);
        if (Math.Abs(clamped - CodeNavigationMapGraphWidth) < 3)
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
        string? currentPath = null;
        string? solutionPath = null;
        string? editorText = null;
        int? cursorLine = null;
        int? cursorColumn = null;
        CodeNavigationSettings? navSettings = null;
        var wantList = false;
        var wantGraph = false;
        var level = CodeNavigationMapLevelKind.File;
        await UiScheduler.Default.InvokeAsync(() =>
        {
            rawPaths = McpSolutionTree.CollectFileEntries(Workspace.SolutionRoots).Select(e => e.FullPath).ToList();
            currentPath = CurrentFilePath;
            solutionPath = Workspace.SolutionPath;
            editorText = EditorText;
            var (line, column) = WorkspaceNavigationMapOrchestrator.ComputeLineColumn(EditorText, _editorCaretOffset ?? EditorSelectionStart);
            cursorLine = line;
            cursorColumn = column;
            navSettings = _settings.CodeNavigation;
            var sm = _settings.CodeNavigationMap;
            wantList = sm.WantsCodeNavigationMapList;
            wantGraph = sm.WantsCodeNavigationMapGraph;
            level = CodeNavigationMapLevelKind.Normalize(sm.Depth);
        });

        if (ct.IsCancellationRequested)
            return;

        var useSubgraphMode = level == CodeNavigationMapLevelKind.ControlFlow || wantGraph;

        string json;
        try
        {
            json = await Task.Run(
                    () =>
                    {
                        if (level == CodeNavigationMapLevelKind.ControlFlow)
                        {
                            return CodeNavigationControlFlowSubgraphBuilder.BuildJson(
                                currentPath,
                                editorText,
                                cursorLine,
                                cursorColumn,
                                CodeNavigationContextBuilder.DefaultMaxNodes,
                                CodeNavigationContextBuilder.DefaultMaxEdges);
                        }

                        if (useSubgraphMode)
                        {
                            return CodeNavigationContextBuilder.BuildJson(
                                "subgraph",
                                null,
                                currentPath,
                                rawPaths,
                                solutionPath,
                                null,
                                null,
                                CodeNavigationContextBuilder.DefaultMaxRelated,
                                CodeNavigationContextBuilder.DefaultMaxNodes,
                                CodeNavigationContextBuilder.DefaultMaxEdges,
                                null,
                                null,
                                null,
                                navSettings ?? new CodeNavigationSettings());
                        }

                        return CodeNavigationContextBuilder.BuildJson(
                            "related",
                            null,
                            currentPath,
                            rawPaths,
                            solutionPath,
                            null,
                            null,
                            CodeNavigationContextBuilder.DefaultMaxRelated,
                            CodeNavigationContextBuilder.DefaultMaxNodes,
                            CodeNavigationContextBuilder.DefaultMaxEdges,
                            null,
                            null,
                            null,
                            navSettings ?? new CodeNavigationSettings());
                    },
                    ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (ct.IsCancellationRequested)
            return;

        List<WorkspaceNavigationMapItemVm> rows = [];
        string status = "";
        string anchorLabel = "—";
        CodeNavigationMapGraphSceneVm? scene = null;
        var graphHeight = CodeNavigationMapCompositor.DefaultHeightFile;
        var accentCount = 0;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out _))
            {
                status = WorkspaceNavigationMapOrchestrator.ResolveErrorStatus(root, currentPath);
            }
            else if (useSubgraphMode && CodeNavigationMapSubgraphJson.TryParse(json, out var subgraph, out _))
            {
                var composed = _codeNavigationMapCompositor.Compose(
                    new CodeNavigationMapCompositionIntent(
                        subgraph!,
                        level,
                        _settings.CodeNavigationMap.NormalizedDetailLevel),
                    new Services.SkiaInstruments.SkiaInstrumentViewport(CodeNavigationMapGraphWidth, CodeNavigationMapGraphHeight));
                if (level == CodeNavigationMapLevelKind.ControlFlow)
                {
                    var channelPayload = _traceFlowChannelCoordinator.Build(new TraceFlowChannelContext(
                        subgraph!,
                        ImpactedTestsBadge,
                        LastTestSummary));
                    var cds = CockpitSurfaceSnapshotBuilder.Build(this);
                    var cdsDecision = _traceFlowCdsRouter.Route(new TraceFlowCdsRouteInput(cds, level));
                    scene = _traceFlowSurfaceCompositor.Compose(composed.Scene, channelPayload, cdsDecision);
                }
                else
                {
                    scene = composed.Scene;
                }
                graphHeight = composed.PreferredHeight;
                var satCount = Math.Max(0, scene.Nodes.Count - 1);
                accentCount = satCount;
                anchorLabel = WorkspaceNavigationMapOrchestrator.ResolveAnchorLabelFromSubgraph(subgraph!);

                if (wantList)
                {
                    var parsedRows = WorkspaceNavigationMapOrchestrator.BuildRowsFromSubgraph(subgraph!, solutionPath);
                    foreach (var parsed in parsedRows)
                    {
                        rows.Add(new WorkspaceNavigationMapItemVm
                        {
                            FullPath = parsed.FullPath,
                            RelativePath = parsed.RelativePath,
                            Kind = parsed.Kind,
                            Rationale = parsed.Rationale
                        });
                    }

                    accentCount = Math.Max(accentCount, rows.Count);
                    status = WorkspaceNavigationMapOrchestrator.ResolveEmptyStatus(parsedRows, status, wantList: true);
                }
            }
            else if (wantList)
            {
                anchorLabel = WorkspaceNavigationMapOrchestrator.ResolveAnchorLabelFromRelatedRoot(root);
                var parsedRows = WorkspaceNavigationMapOrchestrator.BuildRowsFromRelatedRoot(root);
                foreach (var parsed in parsedRows)
                {
                    rows.Add(new WorkspaceNavigationMapItemVm
                    {
                        FullPath = parsed.FullPath,
                        RelativePath = parsed.RelativePath,
                        Kind = parsed.Kind,
                        Rationale = parsed.Rationale
                    });
                }

                accentCount = rows.Count;
                status = WorkspaceNavigationMapOrchestrator.ResolveEmptyStatus(parsedRows, status, wantList: true);
            }
        }
        catch
        {
            status = "Не удалось разобрать ответ навигации.";
        }

        await UiScheduler.Default.InvokeAsync(() =>
        {
            if (ct.IsCancellationRequested)
                return;
            WorkspaceNavigationMapAnchorLabel = anchorLabel;
            WorkspaceNavigationMapStatus = status;
            WorkspaceNavigationMapRelatedCount = accentCount;
            CodeNavigationMapGraphScene = scene;
            CodeNavigationMapGraphHeight = graphHeight;
            WorkspaceNavigationMapItems.Clear();
            foreach (var r in rows)
                WorkspaceNavigationMapItems.Add(r);
        });
    }

}
