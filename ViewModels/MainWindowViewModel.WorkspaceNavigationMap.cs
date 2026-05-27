using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Cockpit.Composition.TraceFlow;
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Слот Pfd: <b>отображение</b> карты намерений / <see cref="CascadeIDE.Cockpit.Graph.GraphDocument"/> (те же данные, что JSON MCP). Граф подграфа — не синоним <c>instrument_id</c>, см. ADR 0065.
/// По доменам: <b>карта намерений</b> (в т.ч. control flow) — CodeNavigation; <b>зависимости файлов</b> — WorkspaceNavigation; <b>submodules</b> — дерево/GitMap (ADR 0062).
/// </summary>
public partial class MainWindowViewModel
{
    private readonly IGraphDataSource _codeNavigationMapGraphDataSource = new WorkspaceNavigationMapContextJsonDataSource();
    private readonly TraceFlowChannelCoordinator _traceFlowChannelCoordinator = new(
        [
            new CodeFlowTraceChannel(),
            new UnitTestTraceChannel()
        ]);
    private readonly ITraceFlowCdsRouter _traceFlowCdsRouter = new TraceFlowCdsRouter();
    private readonly ITraceFlowSurfaceCompositor _traceFlowSurfaceCompositor = new TraceFlowSurfaceCompositor();
    private int? _editorCaretOffset;

    /// <summary>Одноразовый якорь CF после клика по узлу графа (строка для refresh + reveal).</summary>
    private string? _controlFlowGraphNavigatePath;
    private int? _controlFlowGraphNavigateLine;
    private int? _controlFlowGraphNavigateColumn;

    private CancellationTokenSource? _workspaceNavigationMapRefreshCts;

    internal void UpdateCodeNavigationMapCaretOffset(int? offset)
    {
        _editorCaretOffset = offset;
        if (_settings.CodeNavigationMap.IsControlFlowDepth)
            ScheduleWorkspaceNavigationMapRefresh();
        ScheduleEditorHudBannerRefresh();
        RefreshEditorHudControlFlowStrip();
    }

    /// <summary>Связанные файлы для текущего якоря (режим списка).</summary>
    public ObservableCollection<WorkspaceNavigationMapItemVm> WorkspaceNavigationMapItems { get; } = new();

    /// <summary>Полоска control-flow в HUD над редактором (узлы карты без якорей в коде).</summary>
    public ObservableCollection<EditorHudControlFlowNodeVm> EditorHudControlFlowNodes { get; } = new();

    public bool ShowEditorHudControlFlowStrip => EditorHudControlFlowNodes.Count > 0;

    /// <summary>Варианты <see cref="CodeNavigationMapPresentationKind"/> для ComboBox.</summary>
    public string[] CodeNavigationMapPresentationOptions { get; } =
        [CodeNavigationMapPresentationKind.List, CodeNavigationMapPresentationKind.Graph, CodeNavigationMapPresentationKind.Both];

    /// <summary>Варианты уровня карты: файловый и control flow.</summary>
    public string[] CodeNavigationMapLevelOptions { get; } =
        [CodeNavigationMapLevelKind.File, CodeNavigationMapLevelKind.ControlFlow];

    /// <summary><c>auto</c> | <c>vertical</c> | <c>horizontal</c> для <c>[code_navigation_map].control_flow_main_axis</c>.</summary>
    public string[] CodeNavigationMapControlFlowMainAxisOptions { get; } =
    [
        CodeNavigationMapControlFlowMainAxisKind.Auto,
        CodeNavigationMapControlFlowMainAxisKind.Vertical,
        CodeNavigationMapControlFlowMainAxisKind.Horizontal,
    ];

    /// <summary>
    /// Полный путь якоря подграфа control-flow (как в JSON <c>anchor_path</c>); для кликов и подписей в редакторе.
    /// </summary>
    [ObservableProperty]
    private string? _workspaceNavigationMapCfAnchorFullPath;

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
    [NotifyPropertyChangedFor(nameof(ShowCodeNavigationMapGraph))]
    [NotifyPropertyChangedFor(nameof(ShowCodeNavigationMapGraphClickHint))]
    [NotifyPropertyChangedFor(nameof(CodeNavigationMapSettingsSummaryLine))]
    private string _codeNavigationMapLevel = CodeNavigationMapLevelKind.File;

    /// <summary>Синхронизируется с <c>[code_navigation_map].control_flow_main_axis</c>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CodeNavigationMapSettingsSummaryLine))]
    private string _codeNavigationMapControlFlowMainAxis = CodeNavigationMapControlFlowMainAxisKind.Auto;

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

    partial void OnCodeNavigationMapGraphSceneChanged(CodeNavigationMapGraphSceneVm? value) =>
        RefreshEditorHudControlFlowStrip();

    partial void OnWorkspaceNavigationMapCfAnchorFullPathChanged(string? value) =>
        RefreshEditorHudControlFlowStrip();

    /// <summary>Настройка <c>list</c>/<c>both</c>: список связанных ренерится на странице MFD <see cref="MfdShellPage.RelatedFiles"/>, не в колонке PFD.</summary>
    public bool ShowCodeNavigationMapList =>
        CodeNavigationMapPresentationProjection.ShowCodeNavigationMapList(CodeNavigationMapPresentation);

    /// <summary>Списка related на PFD нет (см. <see cref="MfdShellPage.RelatedFiles"/>).</summary>
    public bool ShowCodeNavigationMapListOnPfd =>
        CodeNavigationMapPresentationProjection.ShowCodeNavigationMapListOnPfd;

    /// <summary>Показать мини-карту подграфа.</summary>
    public bool ShowCodeNavigationMapGraph =>
        CodeNavigationMapPresentationProjection.ShowCodeNavigationMapGraph(CodeNavigationMapPresentation, CodeNavigationMapLevel);

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

    [RelayCommand]
    private void NavigateEditorHudControlFlowNode(object? parameter)
    {
        if (parameter is EditorHudControlFlowNodeVm n)
            NavigateWorkspaceNavigationMapNode(n.ToNavigatePayload());
    }

    private void RefreshEditorHudControlFlowStrip()
    {
        var wantCf = string.Equals(
            CodeNavigationMapLevelKind.Normalize(CodeNavigationMapLevel),
            CodeNavigationMapLevelKind.ControlFlow,
            StringComparison.Ordinal);

        var anchor = WorkspaceNavigationMapCfAnchorFullPath;
        var current = CurrentFilePath;
        var scene = CodeNavigationMapGraphScene;
        if (!wantCf
            || string.IsNullOrEmpty(anchor)
            || string.IsNullOrEmpty(current)
            || !EditorTextCoordinateUtilities.PathsReferToSameFile(anchor, current)
            || scene is null
            || scene.IsEmpty
            || scene.Presentation != CodeNavigationMapGraphPresentationKind.CodeControlFlow)
        {
            if (EditorHudControlFlowNodes.Count > 0)
            {
                EditorHudControlFlowNodes.Clear();
                OnPropertyChanged(nameof(ShowEditorHudControlFlowStrip));
                OnPropertyChanged(nameof(IsEditorHudChromeVisible));
            }

            return;
        }

        var caretOffset = _editorCaretOffset ?? EditorSelectionStart;
        var (caretLine, _) = WorkspaceNavigationMapOrchestrator.ComputeLineColumn(EditorText, caretOffset);

        static int SortKey(CodeNavigationMapGraphNodeLayout n) =>
            n.IsAnchor ? int.MinValue :
            n.LegendIndex is > 0 ? n.LegendIndex.Value :
            string.Equals(n.Kind, "exit_step", StringComparison.OrdinalIgnoreCase) ? int.MaxValue :
            int.MaxValue - 1;

        var newNodes = scene.Nodes
            .OrderBy(SortKey)
            .Select(n =>
            {
                CodeNavigationControlFlowGlyphComposer.GetNodeVisual(n, scene, out var vk, out var text, out var exitArrow);

                var vm = new EditorHudControlFlowNodeVm
                {
                    NodeId = n.Id,
                    Kind = n.Kind,
                    FullPath = n.FullPath,
                    LineStart = n.LineStart,
                    LineEnd = n.LineEnd,
                    LegendLine = n.LegendLine,
                    Label = n.Label,
                    Glyph = text,
                    VisualKind = vk,
                    ShowExitArrow = exitArrow,
                    HudToolTip = CodeNavigationControlFlowGlyphComposer.TooltipForNode(n),
                };
                if (caretLine > 0)
                    vm.IsHighlighted = vm.IsCaretOnNode(caretLine);
                return vm;
            })
            .ToList();

        EditorHudControlFlowNodes.Clear();
        foreach (var n in newNodes)
            EditorHudControlFlowNodes.Add(n);
        OnPropertyChanged(nameof(ShowEditorHudControlFlowStrip));
        OnPropertyChanged(nameof(IsEditorHudChromeVisible));
    }

    public bool IsControlFlowEditorVirtualSpacingActiveForFile(string? filePath) =>
        EditorControlFlowVirtualSpacing.ShouldReserveLane(
            CodeNavigationMapLevel,
            WorkspaceNavigationMapCfAnchorFullPath,
            filePath,
            CodeNavigationMapGraphScene);

    /// <summary>Список глифов CF для строки gutter активного документа (<c>null</c> если не CF / не совпадает якорь).</summary>
    public IReadOnlyList<ControlFlowLineVisual>? GetControlFlowGutterLineVisualsForFile(string? filePath)
    {
        if (!IsControlFlowEditorVirtualSpacingActiveForFile(filePath))
            return null;

        return CodeNavigationControlFlowGlyphComposer.BuildGutterLineVisuals(CodeNavigationMapGraphScene!);
    }

    /// <summary>Открыть связанный файл / code anchor из карты намерений.</summary>
    [RelayCommand]
    private void OpenWorkspaceNavigationRelated(object? parameter)
    {
        switch (parameter)
        {
            case CodeNavigationMapNodeNavigatePayload payload:
                NavigateWorkspaceNavigationMapNode(payload);
                return;
            case string path when !string.IsNullOrWhiteSpace(path):
                Documents.OpenOrActivateDocument(path);
                return;
        }
    }

    private void BeginControlFlowGraphNodeNavigation(string fullPath, int lineOneBased)
    {
        _controlFlowGraphNavigatePath = fullPath;
        _controlFlowGraphNavigateLine = lineOneBased;
        _controlFlowGraphNavigateColumn = 1;

        if (!EditorTextCoordinateUtilities.PathsReferToSameFile(CurrentFilePath, fullPath))
            return;

        var text = TryCaptureLiveEditorText(CurrentFilePath) ?? EditorText;
        if (WorkspaceNavigationMapOrchestrator.TryOffsetForLine(text, lineOneBased) is int offset)
            _editorCaretOffset = offset;
    }

    /// <summary>Не сбрасывать каретку при смене файла, если идёт навигация с CF-графа на ту же цель.</summary>
    internal bool TryPreserveControlFlowNavigateCaretOnFileChange()
    {
        if (_controlFlowGraphNavigateLine is not int line || line < 1
            || string.IsNullOrEmpty(_controlFlowGraphNavigatePath))
            return false;

        if (!EditorTextCoordinateUtilities.PathsReferToSameFile(CurrentFilePath, _controlFlowGraphNavigatePath))
            return false;

        foreach (var editor in EnumerateEditorsForPath(CurrentFilePath))
        {
            var docText = editor.Document?.Text;
            if (WorkspaceNavigationMapOrchestrator.TryOffsetForLine(docText, line) is int offset)
            {
                _editorCaretOffset = offset;
                return true;
            }
        }

        if (WorkspaceNavigationMapOrchestrator.TryOffsetForLine(EditorText, line) is int hostOffset)
        {
            _editorCaretOffset = hostOffset;
            return true;
        }

        return false;
    }

    private void ClearControlFlowGraphNodeNavigationAnchor()
    {
        _controlFlowGraphNavigatePath = null;
        _controlFlowGraphNavigateLine = null;
        _controlFlowGraphNavigateColumn = null;
    }

    private void NavigateWorkspaceNavigationMapNode(CodeNavigationMapNodeNavigatePayload payload)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payload.FullPath))
                return;

            if (!Features.Documents.SolutionTreePath.TryGetFullPath(payload.FullPath, out var path))
            {
                WorkspaceNavigationMapStatus = "Не удалось нормализовать путь файла узла карты.";
                return;
            }

            var isCf = string.Equals(
                CodeNavigationMapLevelKind.Normalize(CodeNavigationMapLevel),
                CodeNavigationMapLevelKind.ControlFlow,
                StringComparison.Ordinal);

            if (isCf)
            {
                var anchorCf = WorkspaceNavigationMapCfAnchorFullPath;
                if (!string.IsNullOrEmpty(anchorCf)
                    && !EditorTextCoordinateUtilities.PathsReferToSameFile(path, anchorCf))
                {
                    WorkspaceNavigationMapStatus =
                        "Карта потока — другой файл; клик отменён (ожидается якорный .cs карты).";
                    return;
                }
            }

            if (isCf && payload.LineStart is > 0)
                BeginControlFlowGraphNodeNavigation(path, payload.LineStart.Value);

            Documents.ActivateDocumentForReveal(path);

            if (payload.LineStart is > 0)
            {
                var start = payload.LineStart.Value;
                var end = payload.LineEnd is > 0 ? payload.LineEnd.Value : start;
                var revealPath = path;
                Avalonia.Threading.Dispatcher.UIThread.Post(
                    () => _revealEditorRangeAction?.Invoke(revealPath, start, end, null),
                    Avalonia.Threading.DispatcherPriority.Loaded);
                if (isCf)
                    ScheduleWorkspaceNavigationMapRefresh();
            }
            else if (isCf && string.Equals(payload.Kind, "anchor", StringComparison.OrdinalIgnoreCase))
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(
                    () => _revealEditorRangeAction?.Invoke(path, 1, 1, null),
                    Avalonia.Threading.DispatcherPriority.Loaded);
            }

            if (!string.IsNullOrWhiteSpace(payload.LegendLine))
                WorkspaceNavigationMapStatus = payload.LegendLine.Trim();
        }
        catch (Exception ex)
        {
            WorkspaceNavigationMapStatus = $"Навигация с карты: {ex.Message}";
        }
    }

    /// <summary>Сводка настроек карты намерений (без ComboBox; смена через палитру или MCP).</summary>
    public string CodeNavigationMapSettingsSummaryLine =>
        CodeNavigationMapPresentationProjection.SettingsSummaryLine(
            CodeNavigationMapPresentation,
            CodeNavigationMapLevel,
            _settings.CodeNavigationMap.DetailLevel,
            _settings.CodeNavigationMap.RelatedGraphLayout,
            _settings.CodeNavigationMap.NormalizedControlFlowMainAxis);

    /// <summary>Краткая строка ориентации HCI (слой B) рядом с картой; не влияет на Roslyn-граф (ADR 0106).</summary>
    [ObservableProperty]
    private string _workspaceNavigationMapHciOrientationLine = "";

    /// <summary>Команда палитры / MCP: list → graph → both.</summary>
    public void CycleCodeNavigationMapPresentation() =>
        CodeNavigationMapPresentation = CodeNavigationMapPresentationProjection.NextPresentationViewAfter(CodeNavigationMapPresentation);

    /// <summary>Команда палитры / MCP: file ↔ controlFlow.</summary>
    public void CycleCodeNavigationMapLevel() =>
        CodeNavigationMapLevel = CodeNavigationMapPresentationProjection.ToggledMapLevel(CodeNavigationMapLevel);

    /// <summary>Команда палитры / MCP: radial → top_down → bottom_up.</summary>
    public void CycleCodeNavigationMapRelatedGraphLayout()
    {
        _settings.CodeNavigationMap.RelatedGraphLayout =
            CodeNavigationMapPresentationProjection.NextRelatedGraphLayoutAfter(_settings.CodeNavigationMap.RelatedGraphLayout);
        SaveSettingsIfChanged();
        ScheduleWorkspaceNavigationMapRefresh();
        OnPropertyChanged(nameof(CodeNavigationMapSettingsSummaryLine));
    }

    /// <summary>Команда палитры / MCP: glance → normal → inspect.</summary>
    public void CycleCodeNavigationMapDetailLevel()
    {
        var (_, toml) = CodeNavigationMapPresentationProjection.NextDetailCycle(_settings.CodeNavigationMap.NormalizedDetailLevel);
        _settings.CodeNavigationMap.DetailLevel = toml;
        SaveSettingsIfChanged();
        ScheduleWorkspaceNavigationMapRefresh();
        OnPropertyChanged(nameof(CodeNavigationMapSettingsSummaryLine));
    }
}

