using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia.Controls;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Cockpit.Composition.TraceFlow;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Services.CodeNavigation;
using CascadeIDE.Services.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Слот Pfd: <b>отображение</b> семантической карты (те же данные, что JSON MCP). Semantic Map — граф связей, не название прибора.
/// По доменам: <b>карта кода</b> (в т.ч. control flow) — CodeNavigation; <b>зависимости файлов</b> — WorkspaceNavigation; <b>submodules</b> — дерево/GitMap (ADR 0062).
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
        CodeNavigationMapPresentation == CodeNavigationMapPresentationKind.List
        || CodeNavigationMapPresentation == CodeNavigationMapPresentationKind.Both;

    /// <summary>Списка related на PFD нет (см. <see cref="MfdShellPage.RelatedFiles"/>).</summary>
    public bool ShowCodeNavigationMapListOnPfd => false;

    /// <summary>Показать мини-карту подграфа.</summary>
    public bool ShowCodeNavigationMapGraph =>
        CodeNavigationMapPresentation == CodeNavigationMapPresentationKind.Graph
        || CodeNavigationMapPresentation == CodeNavigationMapPresentationKind.Both;

    /// <summary>
    /// Высота нижней строки Grid под список на PFD: список перенесён в MFD — всегда 0.
    /// </summary>
    public GridLength CodeNavigationMapListAreaRowHeight =>
        (ShowCodeNavigationMapList && ShowCodeNavigationMapListOnPfd)
            ? new GridLength(1, GridUnitType.Star)
            : new GridLength(0);

    /// <summary>
    /// Режим <c>file</c>: подсказка «открыть файл» (в control flow клик ведёт к строке, не к файлу).
    /// </summary>
    public bool ShowCodeNavigationMapGraphClickHint =>
        ShowCodeNavigationMapGraph
        && string.Equals(CodeNavigationMapLevelKind.Normalize(CodeNavigationMapLevel), CodeNavigationMapLevelKind.File, StringComparison.Ordinal)
        && CodeNavigationMapPresentation != CodeNavigationMapPresentationKind.List;

    /// <summary>Короткая подпись к количеству связей для шапки SM.</summary>
    public string WorkspaceNavigationMapRelatedBadge =>
        WorkspaceNavigationMapRelatedCount switch
        {
            0 => "",
            1 => "1 связь",
            _ => $"{WorkspaceNavigationMapRelatedCount} связей"
        };

    /// <summary>Есть ли контекст для accent (список или подграф с соседями).</summary>
    public bool WorkspaceNavigationMapHasRelated =>
        WorkspaceNavigationMapRelatedCount > 0
        || (CodeNavigationMapGraphScene?.Nodes.Count > 1);

    /// <summary>Открыть связанный файл из Semantic Map.</summary>
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
            var (line, column) = ComputeLineColumn(EditorText, _editorCaretOffset ?? EditorSelectionStart);
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
            if (root.TryGetProperty("error", out var errEl))
            {
                var code = errEl.GetString() ?? "";
                var msg = root.TryGetProperty("message", out var mEl) ? mEl.GetString() ?? "" : "";
                status = string.IsNullOrEmpty(msg) ? code : msg;
                if (code == "no_file" && string.IsNullOrEmpty(currentPath))
                    status = "Откройте файл из дерева решения — здесь появятся связанные.";
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
                anchorLabel = string.IsNullOrEmpty(subgraph!.AnchorPath)
                    ? "—"
                    : Path.GetFileName(subgraph.AnchorPath);

                if (wantList)
                {
                    foreach (var n in subgraph.Nodes)
                    {
                        if (string.Equals(n.Kind, "anchor", StringComparison.OrdinalIgnoreCase))
                            continue;
                        var rel = string.IsNullOrEmpty(n.RelativePath)
                            ? McpSolutionTree.GetRelativePath(solutionPath, n.Path)
                            : n.RelativePath!;
                        rows.Add(new WorkspaceNavigationMapItemVm
                        {
                            FullPath = n.Path,
                            RelativePath = rel ?? n.Path,
                            Kind = n.Kind,
                            Rationale = n.Rationale ?? ""
                        });
                    }

                    accentCount = Math.Max(accentCount, rows.Count);
                }

                if (rows.Count == 0 && string.IsNullOrEmpty(status) && wantList)
                    status = "Нет связанных файлов по текущим эвристикам.";
            }
            else if (wantList)
            {
                if (root.TryGetProperty("anchor_path", out var ap) && ap.ValueKind == JsonValueKind.String)
                {
                    var apStr = ap.GetString();
                    if (!string.IsNullOrEmpty(apStr))
                        anchorLabel = Path.GetFileName(apStr);
                }

                if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in items.EnumerateArray())
                    {
                        var fp = el.TryGetProperty("path", out var pEl) ? pEl.GetString() ?? "" : "";
                        var rel = el.TryGetProperty("relative_path", out var rEl) ? rEl.GetString() ?? "" : "";
                        var kind = el.TryGetProperty("kind", out var kEl) ? kEl.GetString() ?? "" : "";
                        var rationale = el.TryGetProperty("rationale", out var raEl) ? raEl.GetString() ?? "" : "";
                        if (string.IsNullOrEmpty(fp))
                            continue;
                        rows.Add(new WorkspaceNavigationMapItemVm
                        {
                            FullPath = fp,
                            RelativePath = string.IsNullOrEmpty(rel) ? fp : rel,
                            Kind = kind,
                            Rationale = rationale
                        });
                    }
                }

                accentCount = rows.Count;

                if (rows.Count == 0 && string.IsNullOrEmpty(status))
                    status = "Нет связанных файлов по текущим эвристикам.";
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

    private static (int line, int column) ComputeLineColumn(string? text, int? offset)
    {
        var source = text ?? string.Empty;
        var pos = Math.Clamp(offset ?? 0, 0, source.Length);
        var line = 1;
        var col = 1;
        for (var i = 0; i < pos; i++)
        {
            if (source[i] == '\n')
            {
                line++;
                col = 1;
            }
            else
            {
                col++;
            }
        }

        return (line, col);
    }
}
