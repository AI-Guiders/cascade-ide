using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia.Threading;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Semantic Map в слоте Pfd: связанные файлы через <see cref="WorkspaceNavigationContextBuilder"/>.</summary>
public partial class MainWindowViewModel
{
    private CancellationTokenSource? _workspaceNavigationMapRefreshCts;

    /// <summary>Связанные файлы для текущего якоря (режим <c>related</c>).</summary>
    public ObservableCollection<WorkspaceNavigationMapItemVm> WorkspaceNavigationMapItems { get; } = new();

    /// <summary>Сообщение об ошибке или пустом состоянии (не null).</summary>
    [ObservableProperty]
    private string _workspaceNavigationMapStatus = "";

    /// <summary>Заголовок якоря: имя файла или «—».</summary>
    [ObservableProperty]
    private string _workspaceNavigationMapAnchorLabel = "—";

    /// <summary>Число строк related в Semantic Map (для UI и SkiaHost accent).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WorkspaceNavigationMapRelatedBadge))]
    [NotifyPropertyChangedFor(nameof(WorkspaceNavigationMapHasRelated))]
    private int _workspaceNavigationMapRelatedCount;

    /// <summary>Короткая подпись к количеству связей для шапки SM.</summary>
    public string WorkspaceNavigationMapRelatedBadge =>
        WorkspaceNavigationMapRelatedCount switch
        {
            0 => "",
            1 => "1 связь",
            _ => $"{WorkspaceNavigationMapRelatedCount} связей"
        };

    /// <summary>Есть ли ненулевой список related (для видимости бейджа).</summary>
    public bool WorkspaceNavigationMapHasRelated => WorkspaceNavigationMapRelatedCount > 0;

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
        await UiScheduler.Default.InvokeAsync(() =>
        {
            rawPaths = McpSolutionTree.CollectFileEntries(Workspace.SolutionRoots).Select(e => e.FullPath).ToList();
            currentPath = CurrentFilePath;
            solutionPath = Workspace.SolutionPath;
            navSettings = _settings.WorkspaceNavigationContext;
        });

        if (ct.IsCancellationRequested)
            return;

        string json;
        try
        {
            json = await Task.Run(
                    () => WorkspaceNavigationContextBuilder.BuildJson(
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
                        navSettings ?? new WorkspaceNavigationContextSettings()),
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
            else
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
            WorkspaceNavigationMapRelatedCount = rows.Count;
            WorkspaceNavigationMapItems.Clear();
            foreach (var r in rows)
                WorkspaceNavigationMapItems.Add(r);
        });
    }
}
