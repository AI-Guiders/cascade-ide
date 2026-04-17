using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia.Controls;
using CascadeIDE.Models;
using CascadeIDE.Services.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Semantic Map в слоте Pfd: тот же контракт, что <see cref="WorkspaceNavigationContextBuilder"/> / MCP.</summary>
public partial class MainWindowViewModel
{
    private readonly IWorkspaceNavigationGraphLayoutEngine _workspaceNavigationGraphLayout = new WorkspaceNavigationStarGraphLayoutEngine();

    private CancellationTokenSource? _workspaceNavigationMapRefreshCts;

    /// <summary>Связанные файлы для текущего якоря (режим списка).</summary>
    public ObservableCollection<WorkspaceNavigationMapItemVm> WorkspaceNavigationMapItems { get; } = new();

    /// <summary>Варианты <see cref="SemanticMapPresentationKind"/> для ComboBox.</summary>
    public string[] SemanticMapPresentationOptions { get; } =
        [SemanticMapPresentationKind.List, SemanticMapPresentationKind.Graph, SemanticMapPresentationKind.Both];

    /// <summary>Сцена мини-карты (подграф + укладка звездой).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WorkspaceNavigationMapHasRelated))]
    private SemanticMapGraphSceneVm? _semanticMapGraphScene;

    /// <summary><c>list</c> | <c>graph</c> | <c>both</c> — синхронизируется с <c>[semantic_map]</c>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSemanticMapList))]
    [NotifyPropertyChangedFor(nameof(ShowSemanticMapGraph))]
    [NotifyPropertyChangedFor(nameof(WorkspaceNavigationMapHasRelated))]
    [NotifyPropertyChangedFor(nameof(SemanticMapListAreaRowHeight))]
    [NotifyPropertyChangedFor(nameof(ShowSemanticMapGraphClickHint))]
    private string _semanticMapPresentation = SemanticMapPresentationKind.List;

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

    /// <summary>Показать список связанных файлов.</summary>
    public bool ShowSemanticMapList =>
        SemanticMapPresentation == SemanticMapPresentationKind.List
        || SemanticMapPresentation == SemanticMapPresentationKind.Both;

    /// <summary>Показать мини-карту подграфа.</summary>
    public bool ShowSemanticMapGraph =>
        SemanticMapPresentation == SemanticMapPresentationKind.Graph
        || SemanticMapPresentation == SemanticMapPresentationKind.Both;

    /// <summary>
    /// Высота нижней строки Grid под список: звезда только если список виден; иначе 0 —
    /// иначе строка <c>*</c> с невидимым <c>ScrollViewer</c> съедает всё место под графом.
    /// </summary>
    public GridLength SemanticMapListAreaRowHeight =>
        ShowSemanticMapList ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

    /// <summary>Режим только графа: подсказка, что узлы кликабельны (в списке кнопки скрыты).</summary>
    public bool ShowSemanticMapGraphClickHint => ShowSemanticMapGraph && !ShowSemanticMapList;

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
        || (SemanticMapGraphScene?.Nodes.Count > 1);

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
        WorkspaceNavigationContextSettings? navSettings = null;
        var presentation = SemanticMapPresentationKind.List;
        await UiScheduler.Default.InvokeAsync(() =>
        {
            rawPaths = McpSolutionTree.CollectFileEntries(Workspace.SolutionRoots).Select(e => e.FullPath).ToList();
            currentPath = CurrentFilePath;
            solutionPath = Workspace.SolutionPath;
            navSettings = _settings.WorkspaceNavigationContext;
            presentation = SemanticMapPresentationKind.Normalize(_settings.SemanticMap.Presentation);
        });

        if (ct.IsCancellationRequested)
            return;

        var wantList = presentation is SemanticMapPresentationKind.List or SemanticMapPresentationKind.Both;
        var wantGraph = presentation is SemanticMapPresentationKind.Graph or SemanticMapPresentationKind.Both;

        string json;
        try
        {
            json = await Task.Run(
                    () =>
                    {
                        if (wantGraph)
                        {
                            return WorkspaceNavigationContextBuilder.BuildJson(
                                "subgraph",
                                null,
                                currentPath,
                                rawPaths,
                                solutionPath,
                                null,
                                null,
                                WorkspaceNavigationContextBuilder.DefaultMaxRelated,
                                WorkspaceNavigationContextBuilder.DefaultMaxNodes,
                                WorkspaceNavigationContextBuilder.DefaultMaxEdges,
                                null,
                                null,
                                null,
                                navSettings ?? new WorkspaceNavigationContextSettings());
                        }

                        return WorkspaceNavigationContextBuilder.BuildJson(
                            "related",
                            null,
                            currentPath,
                            rawPaths,
                            solutionPath,
                            null,
                            null,
                            WorkspaceNavigationContextBuilder.DefaultMaxRelated,
                            WorkspaceNavigationContextBuilder.DefaultMaxNodes,
                            WorkspaceNavigationContextBuilder.DefaultMaxEdges,
                            null,
                            null,
                            null,
                            navSettings ?? new WorkspaceNavigationContextSettings());
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
        SemanticMapGraphSceneVm? scene = null;
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
            else if (wantGraph && WorkspaceNavigationSubgraphJson.TryParse(json, out var subgraph, out _))
            {
                scene = _workspaceNavigationGraphLayout.Layout(subgraph!, 280, 120);
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
            SemanticMapGraphScene = scene;
            WorkspaceNavigationMapItems.Clear();
            foreach (var r in rows)
                WorkspaceNavigationMapItems.Add(r);
        });
    }
}
