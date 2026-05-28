#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaEdit;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Cockpit.Composition.TraceFlow;
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.Editor;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Features.Markdown;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.WorkspaceNavigation.Presentation;

/// <summary>PFD/MFD workspace navigation map (ADR 0039).</summary>
public sealed partial class WorkspaceNavigationMapViewModel : ObservableObject
{
    private readonly IWorkspaceNavigationMapHost _host;

    public WorkspaceNavigationMapViewModel(IWorkspaceNavigationMapHost host)
    {
        _host = host;
        _host.Shell.PropertyChanged += OnShellPropertyChanged;
    }

    public bool IsPfdRegionExpanded => _host.IsPfdRegionExpanded;

    private void OnShellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is "IsPfdRegionExpanded" or null or "")
            OnPropertyChanged(nameof(IsPfdRegionExpanded));
    }

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

    internal int? EditorCaretOffset => _editorCaretOffset;

    internal void UpdateCodeNavigationMapCaretOffset(int? offset)
    {
        _editorCaretOffset = offset;
        if (_host.Settings.CodeNavigationMap.IsControlFlowDepth)
            ScheduleWorkspaceNavigationMapRefresh();
        _host.ScheduleEditorHudBannerRefresh();
    }

    /// <summary>Связанные файлы для текущего якоря (режим списка).</summary>
    public ObservableCollection<WorkspaceNavigationMapItemVm> WorkspaceNavigationMapItems { get; } = new();

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
                _host.Documents.OpenOrActivateDocument(path);
                return;
        }
    }

    [RelayCommand]
    private void OpenWorkspaceAdrCorrespondence()
    {
        var docPath = WorkspaceAdrCorrespondenceFirstDocPath;
        if (string.IsNullOrWhiteSpace(docPath))
            return;

        var wsRoot = _host.GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(wsRoot))
            return;

        if (!WorkspaceMarkdownPreviewOpener.TryOpenRepoDocument(
                wsRoot,
                docPath,
                (title, content, source) => _host.MarkdownPreviewTool.SetContent(title, content, source),
                out _))
            return;

        _host.ApplyMfdRegionExpanded(true);
        _host.TryNavigateToMfdShellPage(MfdShellPage.MarkdownPreview);
    }

    [RelayCommand]
    private async Task OpenWorkspaceFeatureDocsAsync()
    {
        var docs = WorkspaceFeatureDocPaths ?? [];
        if (docs.Length == 0)
            return;

        var pick = docs.Length == 1
            ? docs[0]
            : _host.RequestPickFeatureDocAsync is not null
                ? await _host.RequestPickFeatureDocAsync("Документация фичи", docs).ConfigureAwait(true)
                : docs[0];

        if (string.IsNullOrWhiteSpace(pick))
            return;

        var wsRoot = _host.GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(wsRoot))
            return;

        if (!WorkspaceMarkdownPreviewOpener.TryOpenRepoDocument(
                wsRoot,
                pick,
                (title, content, source) => _host.MarkdownPreviewTool.SetContent(title, content, source),
                out _))
            return;

        _host.ApplyMfdRegionExpanded(true);
        _host.TryNavigateToMfdShellPage(MfdShellPage.MarkdownPreview);
    }

    [RelayCommand]
    private async Task OpenDocsTemplateAsync(object? parameter)
    {
        var wsRoot = _host.GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(wsRoot))
            return;

        var workspaceToml = RepositoryWorkspaceTomlLoader.TryLoad(wsRoot);
        var templates = DocsTemplatesCatalogResolver.ResolveTemplatesFromWorkspaceToml(workspaceToml, wsRoot);
        if (templates.Count == 0)
            return;

        var requested = parameter as string;
        var selectedId = !string.IsNullOrWhiteSpace(requested) ? requested!.Trim() : null;
        if (string.IsNullOrWhiteSpace(selectedId))
        {
            var labels = templates.Select(t => $"{t.Id} — {t.Title}").ToArray();
            var pick = _host.RequestPickFeatureDocAsync is not null
                ? await _host.RequestPickFeatureDocAsync("Шаблон документации", labels).ConfigureAwait(true)
                : labels[0];

            if (string.IsNullOrWhiteSpace(pick))
                return;

            var dash = pick.IndexOf("—", StringComparison.Ordinal);
            selectedId = (dash > 0 ? pick[..dash] : pick).Trim();
        }

        if (string.IsNullOrWhiteSpace(selectedId))
            return;

        var entry = templates.FirstOrDefault(t => string.Equals(t.Id, selectedId, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return;

        if (entry.Source == "knowledge" && !string.IsNullOrWhiteSpace(entry.KnowledgeFilePath))
        {
            try
            {
                var args = new Dictionary<string, System.Text.Json.JsonElement>
                {
                    ["file_path"] = System.Text.Json.JsonDocument.Parse($"\"{entry.KnowledgeFilePath}\"").RootElement.Clone(),
                };
                if (!string.IsNullOrWhiteSpace(entry.KnowledgeRootId))
                    args["knowledge_root_id"] = System.Text.Json.JsonDocument.Parse($"\"{entry.KnowledgeRootId}\"").RootElement.Clone();

                var content = await _host.IdeMcp.ExecuteCommandAsync(Services.IdeCommands.ReadKnowledgeFile, args, CancellationToken.None)
                    .ConfigureAwait(true);

                _host.MarkdownPreviewTool.SetContent($"KB template: {entry.Title}", content ?? "", entry.KnowledgeFilePath);
                _host.ApplyMfdRegionExpanded(true);
                _host.TryNavigateToMfdShellPage(MfdShellPage.MarkdownPreview);
            }
            catch
            {
                // ignore
            }
            return;
        }

        if (!string.IsNullOrWhiteSpace(entry.RepoPath)
            && WorkspaceMarkdownPreviewOpener.TryOpenRepoDocument(
                wsRoot,
                entry.RepoPath,
                (title, content, source) => _host.MarkdownPreviewTool.SetContent(entry.Title, content, source),
                out _))
        {
            _host.ApplyMfdRegionExpanded(true);
            _host.TryNavigateToMfdShellPage(MfdShellPage.MarkdownPreview);
        }
    }

    private void BeginControlFlowGraphNodeNavigation(string fullPath, int lineOneBased)
    {
        _controlFlowGraphNavigatePath = fullPath;
        _controlFlowGraphNavigateLine = lineOneBased;
        _controlFlowGraphNavigateColumn = 1;

        if (!EditorTextCoordinateUtilities.PathsReferToSameFile(_host.CurrentFilePath, fullPath))
            return;

        var text = TryCaptureLiveEditorText(_host.CurrentFilePath) ?? _host.EditorText;
        if (WorkspaceNavigationMapOrchestrator.TryOffsetForLine(text, lineOneBased) is int offset)
            _editorCaretOffset = offset;
    }

    /// <summary>Не сбрасывать каретку при смене файла, если идёт навигация с CF-графа на ту же цель.</summary>
    internal bool TryPreserveControlFlowNavigateCaretOnFileChange()
    {
        if (_controlFlowGraphNavigateLine is not int line || line < 1
            || string.IsNullOrEmpty(_controlFlowGraphNavigatePath))
            return false;

        if (!EditorTextCoordinateUtilities.PathsReferToSameFile(_host.CurrentFilePath, _controlFlowGraphNavigatePath))
            return false;

        foreach (var editor in EnumerateEditorsForPath(_host.CurrentFilePath))
        {
            var docText = editor.Document?.Text;
            if (WorkspaceNavigationMapOrchestrator.TryOffsetForLine(docText, line) is int offset)
            {
                _editorCaretOffset = offset;
                return true;
            }
        }

        if (WorkspaceNavigationMapOrchestrator.TryOffsetForLine(_host.EditorText, line) is int hostOffset)
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

            _host.Documents.ActivateDocumentForReveal(path);

            if (payload.LineStart is > 0)
            {
                var start = payload.LineStart.Value;
                var end = payload.LineEnd is > 0 ? payload.LineEnd.Value : start;
                var revealPath = path;
                Avalonia.Threading.Dispatcher.UIThread.Post(
                    () => _host.RevealEditorRange(revealPath, start, end, null),
                    Avalonia.Threading.DispatcherPriority.Loaded);
                if (isCf)
                    ScheduleWorkspaceNavigationMapRefresh();
            }
            else if (isCf && string.Equals(payload.Kind, "anchor", StringComparison.OrdinalIgnoreCase))
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(
                    () => _host.RevealEditorRange(path, 1, 1, null),
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
            _host.Settings.CodeNavigationMap.DetailLevel,
            _host.Settings.CodeNavigationMap.RelatedGraphLayout,
            _host.Settings.CodeNavigationMap.NormalizedControlFlowMainAxis);

    /// <summary>Краткая строка ориентации HCI (слой B) рядом с картой; не влияет на Roslyn-граф (ADR 0106).</summary>
    [ObservableProperty]
    private string _workspaceNavigationMapHciOrientationLine = "";

    /// <summary>Doc correspondence (ADR 0061): какие ADR относятся к текущему файлу по <c>[_host.Workspace.adr.map]</c>.</summary>
    [ObservableProperty]
    private string _workspaceAdrCorrespondenceLine = "";

    [ObservableProperty]
    private string? _workspaceAdrCorrespondenceFirstDocPath;

    [ObservableProperty]
    private string _workspaceFeatureLine = "";

    /// <summary>Мягкий сигнал “нет доков” (не ошибка): пусто, если всё ок.</summary>
    [ObservableProperty]
    private string _workspaceDocsCoverageLine = "";

    [ObservableProperty]
    private string[] _workspaceFeatureDocPaths = [];

    /// <summary>Команда палитры / MCP: list → graph → both.</summary>
    public void CycleCodeNavigationMapPresentation() =>
        CodeNavigationMapPresentation = CodeNavigationMapPresentationProjection.NextPresentationViewAfter(CodeNavigationMapPresentation);

    /// <summary>Команда палитры / MCP: file ↔ controlFlow.</summary>
    public void CycleCodeNavigationMapLevel() =>
        CodeNavigationMapLevel = CodeNavigationMapPresentationProjection.ToggledMapLevel(CodeNavigationMapLevel);

    /// <summary>Команда палитры / MCP / слэш: явно <c>file</c> или <c>controlFlow</c>.</summary>
    public void SetCodeNavigationMapLevel(string level) =>
        CodeNavigationMapLevel = CodeNavigationMapLevelKind.Normalize(level);

    /// <summary>Команда палитры / MCP: radial → top_down → bottom_up.</summary>
    public void CycleCodeNavigationMapRelatedGraphLayout()
    {
        _host.Settings.CodeNavigationMap.RelatedGraphLayout =
            CodeNavigationMapPresentationProjection.NextRelatedGraphLayoutAfter(_host.Settings.CodeNavigationMap.RelatedGraphLayout);
        _host.SaveSettingsIfChanged();
        ScheduleWorkspaceNavigationMapRefresh();
        OnPropertyChanged(nameof(CodeNavigationMapSettingsSummaryLine));
    }

    /// <summary>Команда палитры / MCP: glance → normal → inspect.</summary>
    public void CycleCodeNavigationMapDetailLevel()
    {
        var (_, toml) = CodeNavigationMapPresentationProjection.NextDetailCycle(_host.Settings.CodeNavigationMap.NormalizedDetailLevel);
        _host.Settings.CodeNavigationMap.DetailLevel = toml;
        _host.SaveSettingsIfChanged();
        ScheduleWorkspaceNavigationMapRefresh();
        OnPropertyChanged(nameof(CodeNavigationMapSettingsSummaryLine));
    }

    partial void OnCodeNavigationMapPresentationChanged(string value)
    {
        var normalized = CodeNavigationMapPresentationKind.Normalize(value);
        if (ShellSettingsPresentationProjection.ShouldRewriteWithNormalizedValue(value, normalized))
        {
            CodeNavigationMapPresentation = normalized;
            return;
        }

        _host.Settings.CodeNavigationMap.View = normalized;
        _host.SaveSettingsIfChanged();
        ScheduleWorkspaceNavigationMapRefresh();
    }

    partial void OnCodeNavigationMapLevelChanged(string value)
    {
        var normalized = CodeNavigationMapLevelKind.Normalize(value);
        if (ShellSettingsPresentationProjection.ShouldRewriteWithNormalizedValue(value, normalized))
        {
            CodeNavigationMapLevel = normalized;
            return;
        }

        _host.Settings.CodeNavigationMap.Depth = normalized;
        _host.SaveSettingsIfChanged();
        ScheduleWorkspaceNavigationMapRefresh();
    }

    partial void OnCodeNavigationMapControlFlowMainAxisChanged(string value)
    {
        var normalized = CodeNavigationMapControlFlowMainAxisKind.Normalize(value);
        if (ShellSettingsPresentationProjection.ShouldRewriteWithNormalizedValue(value, normalized))
        {
            CodeNavigationMapControlFlowMainAxis = normalized;
            return;
        }

        _host.Settings.CodeNavigationMap.ControlFlowMainAxis = normalized;
        _host.SaveSettingsIfChanged();
        ScheduleWorkspaceNavigationMapRefresh();
    }
}


