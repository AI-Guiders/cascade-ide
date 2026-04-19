using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using Avalonia.Threading;
using CascadeIDE.Models;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Git;

/// <summary>
/// Вкладка «Git» нижней панели: статус, diff, submodule, коммит/push.
/// </summary>
public partial class GitPanelViewModel : ViewModelBase
{
    private readonly Services.IGitCommandRunner _gitRunner;
    private readonly Func<string> _getWorkspacePath;
    private readonly Services.IIdeMcpActions _ideActions;
    private readonly Action<string> _loadSolution;
    private readonly Func<Task> _refreshGitSummaryAsync;

    public GitPanelViewModel(
        Services.IGitCommandRunner gitRunner,
        Func<string> getWorkspacePath,
        Services.IIdeMcpActions ideActions,
        Action<string> loadSolution,
        Func<Task> refreshGitSummaryAsync)
    {
        _gitRunner = gitRunner;
        _getWorkspacePath = getWorkspacePath;
        _ideActions = ideActions;
        _loadSolution = loadSolution;
        _refreshGitSummaryAsync = refreshGitSummaryAsync;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CommitGitFromPanelCommand))]
    [NotifyCanExecuteChangedFor(nameof(PushGitFromPanelCommand))]
    private bool _isGitRepository;

    [ObservableProperty]
    private string _gitBranchLine = "";

    [ObservableProperty]
    private string _gitPanelStatusText = "";

    [ObservableProperty]
    private string _gitCommitMessage = "";

    [ObservableProperty]
    private string _gitDiffText = "";

    [ObservableProperty]
    private string _gitSubmoduleStatusText = "";

    [ObservableProperty]
    private string _gitRepositoryContextText = "";

    [ObservableProperty]
    private string _gitSubmoduleCommandOutput = "";

    [ObservableProperty]
    private GitStatusRow? _selectedGitStatusRow;

    public ObservableCollection<GitStatusRow> GitStatusRows { get; } = [];

    partial void OnSelectedGitStatusRowChanged(GitStatusRow? value)
    {
        _ = LoadGitDiffForSelectionAsync();
        StageGitSelectionCommand.NotifyCanExecuteChanged();
        UnstageGitSelectionCommand.NotifyCanExecuteChanged();
        OpenSubmoduleFolderCommand.NotifyCanExecuteChanged();
        OpenSubmoduleSolutionInIdeCommand.NotifyCanExecuteChanged();
    }

    partial void OnGitCommitMessageChanged(string value)
    {
        CommitGitFromPanelCommand.NotifyCanExecuteChanged();
        CommitGitStagedOnlyCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsGitRepositoryChanged(bool value)
    {
        CommitGitFromPanelCommand.NotifyCanExecuteChanged();
        PushGitFromPanelCommand.NotifyCanExecuteChanged();
        CommitGitStagedOnlyCommand.NotifyCanExecuteChanged();
        StageGitSelectionCommand.NotifyCanExecuteChanged();
        UnstageGitSelectionCommand.NotifyCanExecuteChanged();
        OpenSubmoduleFolderCommand.NotifyCanExecuteChanged();
        GitSubmoduleUpdateInitCommand.NotifyCanExecuteChanged();
        GitSubmoduleSyncCommand.NotifyCanExecuteChanged();
        OpenSubmoduleSolutionInIdeCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    public async Task RefreshGitPanelAsync()
    {
        var ws = _getWorkspacePath();
        if (string.IsNullOrWhiteSpace(ws) || !Directory.Exists(ws))
        {
            await UiScheduler.Default.InvokeAsync(() =>
            {
                GitBranchLine = "";
                GitStatusRows.Clear();
                GitSubmoduleStatusText = "";
                GitDiffText = "";
                GitRepositoryContextText = "";
                GitPanelStatusText = "Откройте решение (.sln / .slnx).";
                IsGitRepository = false;
            });
            return;
        }

        var inside = await RunGitCommandAsync(["rev-parse", "--is-inside-work-tree"]).ConfigureAwait(false);
        var inRepo = inside.Success && string.Equals(inside.Output.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        if (!inRepo)
        {
            await UiScheduler.Default.InvokeAsync(() =>
            {
                IsGitRepository = false;
                GitBranchLine = "";
                GitStatusRows.Clear();
                GitSubmoduleStatusText = "";
                GitDiffText = "";
                GitRepositoryContextText = "";
                GitPanelStatusText = "Каталог решения не в git-репозитории.";
            });
            return;
        }

        var status = await RunGitCommandAsync(["status", "--short", "--branch"]).ConfigureAwait(false);
        var submodule = await RunGitCommandAsync(["submodule", "status"]).ConfigureAwait(false);
        var subPaths = await LoadSubmodulePathsAsync(ws).ConfigureAwait(false);
        var toplevel = await RunGitCommandAsync(["rev-parse", "--show-toplevel"]).ConfigureAwait(false);

        await UiScheduler.Default.InvokeAsync(() =>
        {
            IsGitRepository = true;
            GitPanelStatusText = status.Success ? "" : status.Output;
            GitBranchLine = ExtractGitBranchLine(status.Output);
            FillGitStatusRows(status.Output, subPaths);
            GitSubmoduleStatusText = submodule.Success
                ? submodule.Output.Trim()
                : $"(submodule: {submodule.Output.Trim()})";
            GitRepositoryContextText = BuildGitRepositoryContext(ws, toplevel.Success ? toplevel.Output : "");
        });

        await LoadGitDiffForSelectionAsync().ConfigureAwait(false);
    }

    /// <summary>Обновить флаг репозитория при смене решения (без полного refresh панели).</summary>
    public async Task RefreshRepositoryFlagAsync()
    {
        var ws = _getWorkspacePath();
        if (string.IsNullOrWhiteSpace(ws) || !Directory.Exists(ws))
        {
            await UiScheduler.Default.InvokeAsync(() => IsGitRepository = false);
            return;
        }

        var inside = await RunGitCommandAsync(["rev-parse", "--is-inside-work-tree"]).ConfigureAwait(false);
        var ok = inside.Success && string.Equals(inside.Output.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        await UiScheduler.Default.InvokeAsync(() => IsGitRepository = ok);
    }

    private void FillGitStatusRows(string output, HashSet<string> submodulePaths)
    {
        GitStatusRows.Clear();
        foreach (var line in (output ?? "").Replace("\r\n", "\n").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("##", StringComparison.Ordinal))
                continue;
            var rel = ParseGitStatusPath(line);
            var path = rel ?? "";
            var norm = path.Replace('\\', '/').TrimEnd('/');
            var isUntracked = line.StartsWith("??", StringComparison.Ordinal);
            var hasStaged = !isUntracked && line.Length >= 1 && line[0] is not (' ' or '?');
            var isSub = !string.IsNullOrEmpty(norm) && submodulePaths.Contains(norm);
            GitStatusRows.Add(new GitStatusRow
            {
                RawLine = line,
                RelativePath = path,
                IsUntracked = isUntracked,
                HasStagedChanges = hasStaged,
                IsSubmodulePath = isSub
            });
        }
    }

    private async Task<HashSet<string>> LoadSubmodulePathsAsync(string workspace)
    {
        var result = await RunGitCommandAsync(["config", "-f", ".gitmodules", "--get-regexp", "path"]).ConfigureAwait(false);
        if (!result.Success)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in (result.Output ?? "").Replace("\r\n", "\n").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var tab = line.IndexOf('\t');
            if (tab < 0)
                continue;
            var p = line[(tab + 1)..].Trim().Replace('\\', '/').TrimEnd('/');
            if (!string.IsNullOrEmpty(p))
                set.Add(p);
        }

        return set;
    }

    private static string ExtractGitBranchLine(string output)
    {
        var first = (output ?? "").Replace("\r\n", "\n").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return first is { Length: >= 3 } && first.StartsWith("## ", StringComparison.Ordinal) ? first[3..].Trim() : "";
    }

    private static string? ParseGitStatusPath(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;
        if (line.StartsWith("##", StringComparison.Ordinal))
            return null;
        if (line.StartsWith("??", StringComparison.Ordinal))
            return line.Length > 2 ? line[2..].Trim() : null;
        if (line.Length < 4)
            return null;
        var trimmed = line[3..].Trim();
        if (trimmed.Contains(" -> ", StringComparison.Ordinal))
        {
            var parts = trimmed.Split([" -> "], StringSplitOptions.None);
            return parts.Length == 2 ? parts[1].Trim() : parts[0].Trim();
        }

        return trimmed;
    }

    private static string BuildGitRepositoryContext(string workspaceDir, string toplevelRaw)
    {
        if (string.IsNullOrWhiteSpace(workspaceDir))
            return "";
        try
        {
            var wsFull = Path.GetFullPath(workspaceDir);
            var topTrim = (toplevelRaw ?? "").Trim().Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrEmpty(topTrim))
                return $"Каталог решения: {wsFull}";
            var topFull = Path.GetFullPath(topTrim);
            var same = string.Equals(wsFull, topFull, StringComparison.OrdinalIgnoreCase);
            return same
                ? $"Корень git: {topFull}"
                : $"Корень git: {topFull} · каталог решения: {wsFull}";
        }
        catch
        {
            return (toplevelRaw ?? "").Trim();
        }
    }

    private static string? FindSolutionFileInDirectory(string submoduleRoot)
    {
        if (string.IsNullOrWhiteSpace(submoduleRoot) || !Directory.Exists(submoduleRoot))
            return null;

        static string? PickFirst(IEnumerable<string> files)
        {
            var arr = files as string[] ?? files.ToArray();
            return arr.Length == 0 ? null : arr.OrderBy(static f => f, StringComparer.OrdinalIgnoreCase).First();
        }

        var topSlnx = Directory.GetFiles(submoduleRoot, "*.slnx", SearchOption.TopDirectoryOnly);
        var topSln = Directory.GetFiles(submoduleRoot, "*.sln", SearchOption.TopDirectoryOnly);
        var p = PickFirst(topSlnx) ?? PickFirst(topSln);
        if (p is not null)
            return p;

        foreach (var sub in Directory.GetDirectories(submoduleRoot))
        {
            var innerSlnx = Directory.GetFiles(sub, "*.slnx", SearchOption.TopDirectoryOnly);
            var innerSln = Directory.GetFiles(sub, "*.sln", SearchOption.TopDirectoryOnly);
            p = PickFirst(innerSlnx) ?? PickFirst(innerSln);
            if (p is not null)
                return p;
        }

        return null;
    }

    private async Task LoadGitDiffForSelectionAsync()
    {
        if (SelectedGitStatusRow is not { HasPath: true } row || string.IsNullOrWhiteSpace(row.RelativePath))
        {
            await UiScheduler.Default.InvokeAsync(() => GitDiffText = "");
            return;
        }

        var unstaged = await RunGitCommandAsync(["diff", "--", row.RelativePath]).ConfigureAwait(false);
        var staged = await RunGitCommandAsync(["diff", "--staged", "--", row.RelativePath]).ConfigureAwait(false);
        var sb = new StringBuilder();
        if (staged.Success && !string.IsNullOrWhiteSpace(staged.Output))
        {
            sb.AppendLine("--- staged ---");
            sb.AppendLine(staged.Output);
            sb.AppendLine();
        }

        if (unstaged.Success && !string.IsNullOrWhiteSpace(unstaged.Output))
        {
            sb.AppendLine("--- unstaged ---");
            sb.AppendLine(unstaged.Output);
        }

        if (sb.Length == 0)
            sb.AppendLine("(нет diff для выбранного пути)");

        await UiScheduler.Default.InvokeAsync(() => GitDiffText = sb.ToString());
    }

    [RelayCommand(CanExecute = nameof(CanCommitGitFromPanel))]
    private async Task CommitGitFromPanelAsync()
    {
        var msg = GitCommitMessage.Trim();
        if (string.IsNullOrWhiteSpace(msg) || !IsGitRepository)
            return;

        GitPanelStatusText = "Коммит…";
        var json = await _ideActions.GitCommitAsync(msg, null).ConfigureAwait(false);
        await UiScheduler.Default.InvokeAsync(() =>
        {
            GitPanelStatusText = json;
            GitCommitMessage = "";
        });
        await RefreshGitPanelAsync().ConfigureAwait(false);
        await _refreshGitSummaryAsync().ConfigureAwait(false);
    }

    private bool CanCommitGitFromPanel() => IsGitRepository && !string.IsNullOrWhiteSpace(GitCommitMessage);

    [RelayCommand(CanExecute = nameof(CanPushGitFromPanel))]
    private async Task PushGitFromPanelAsync()
    {
        if (!IsGitRepository)
            return;

        GitPanelStatusText = "Push…";
        var json = await _ideActions.GitPushAsync(null, null).ConfigureAwait(false);
        await UiScheduler.Default.InvokeAsync(() => GitPanelStatusText = json);
        await RefreshGitPanelAsync().ConfigureAwait(false);
        await _refreshGitSummaryAsync().ConfigureAwait(false);
    }

    private bool CanPushGitFromPanel() => IsGitRepository;

    [RelayCommand(CanExecute = nameof(CanStageGitSelection))]
    private async Task StageGitSelectionAsync()
    {
        if (SelectedGitStatusRow is not { HasPath: true, RelativePath: var rel })
            return;

        GitPanelStatusText = "";
        var r = await RunGitCommandAsync(["add", "--", rel]).ConfigureAwait(false);
        await UiScheduler.Default.InvokeAsync(() => GitPanelStatusText = r.Success ? "" : r.Output);
        await RefreshGitPanelAsync().ConfigureAwait(false);
        await _refreshGitSummaryAsync().ConfigureAwait(false);
    }

    private bool CanStageGitSelection() => IsGitRepository && SelectedGitStatusRow is { HasPath: true };

    [RelayCommand(CanExecute = nameof(CanUnstageGitSelection))]
    private async Task UnstageGitSelectionAsync()
    {
        if (SelectedGitStatusRow is not { HasPath: true, RelativePath: var rel })
            return;

        GitPanelStatusText = "";
        var r = await RunGitCommandAsync(["restore", "--staged", "--", rel]).ConfigureAwait(false);
        await UiScheduler.Default.InvokeAsync(() => GitPanelStatusText = r.Success ? "" : r.Output);
        await RefreshGitPanelAsync().ConfigureAwait(false);
        await _refreshGitSummaryAsync().ConfigureAwait(false);
    }

    private bool CanUnstageGitSelection() =>
        IsGitRepository && SelectedGitStatusRow is { HasPath: true, HasStagedChanges: true };

    [RelayCommand(CanExecute = nameof(CanOpenSubmoduleFolder))]
    private void OpenSubmoduleFolder()
    {
        if (SelectedGitStatusRow is not { IsSubmodulePath: true, RelativePath: var rel })
            return;

        var ws = _getWorkspacePath();
        if (string.IsNullOrWhiteSpace(ws))
            return;

        var full = Path.GetFullPath(Path.Combine(ws, rel.Replace('/', Path.DirectorySeparatorChar)));
        if (!Directory.Exists(full))
            return;

        try
        {
            Process.Start(new ProcessStartInfo { FileName = full, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            GitPanelStatusText = ex.Message;
        }
    }

    private bool CanOpenSubmoduleFolder()
    {
        if (!IsGitRepository || SelectedGitStatusRow is not { IsSubmodulePath: true, RelativePath: var rel })
            return false;

        var ws = _getWorkspacePath();
        if (string.IsNullOrWhiteSpace(ws))
            return false;

        var full = Path.GetFullPath(Path.Combine(ws, rel.Replace('/', Path.DirectorySeparatorChar)));
        return Directory.Exists(full);
    }

    [RelayCommand(CanExecute = nameof(CanOpenSubmoduleSolutionInIde))]
    private void OpenSubmoduleSolutionInIde()
    {
        if (SelectedGitStatusRow is not { IsSubmodulePath: true, RelativePath: var rel })
            return;

        var ws = _getWorkspacePath();
        if (string.IsNullOrWhiteSpace(ws))
            return;

        var dir = Path.GetFullPath(Path.Combine(ws, rel.Replace('/', Path.DirectorySeparatorChar)));
        if (!Directory.Exists(dir))
            return;

        var sln = FindSolutionFileInDirectory(dir);
        if (string.IsNullOrEmpty(sln))
        {
            GitPanelStatusText =
                "В каталоге submodule не найден .sln / .slnx (корень и подпапки первого уровня).";
            return;
        }

        _loadSolution(sln);
    }

    private bool CanOpenSubmoduleSolutionInIde()
    {
        if (!IsGitRepository || SelectedGitStatusRow is not { IsSubmodulePath: true, RelativePath: var rel })
            return false;

        var ws = _getWorkspacePath();
        if (string.IsNullOrWhiteSpace(ws))
            return false;

        var full = Path.GetFullPath(Path.Combine(ws, rel.Replace('/', Path.DirectorySeparatorChar)));
        return Directory.Exists(full) && FindSolutionFileInDirectory(full) is not null;
    }

    [RelayCommand(CanExecute = nameof(CanRunGitSubmoduleCommands))]
    private async Task GitSubmoduleUpdateInitAsync()
    {
        GitSubmoduleCommandOutput = "git submodule update --init --recursive …\n";
        var r = await RunGitCommandAsync(["submodule", "update", "--init", "--recursive"]).ConfigureAwait(false);
        await UiScheduler.Default.InvokeAsync(() =>
        {
            GitSubmoduleCommandOutput = TruncateOutput(r.Output, 8000);
            GitPanelStatusText = r.Success ? "" : "Submodule update завершился с ошибкой (см. вывод ниже).";
        });
        await RefreshGitPanelAsync().ConfigureAwait(false);
        await _refreshGitSummaryAsync().ConfigureAwait(false);
    }

    [RelayCommand(CanExecute = nameof(CanRunGitSubmoduleCommands))]
    private async Task GitSubmoduleSyncAsync()
    {
        GitSubmoduleCommandOutput = "git submodule sync --recursive …\n";
        var r = await RunGitCommandAsync(["submodule", "sync", "--recursive"]).ConfigureAwait(false);
        await UiScheduler.Default.InvokeAsync(() =>
        {
            GitSubmoduleCommandOutput = TruncateOutput(r.Output, 8000);
            if (!r.Success)
                GitPanelStatusText = "Submodule sync завершился с ошибкой (см. вывод ниже).";
        });
        await RefreshGitPanelAsync().ConfigureAwait(false);
        await _refreshGitSummaryAsync().ConfigureAwait(false);
    }

    private bool CanRunGitSubmoduleCommands() => IsGitRepository;

    [RelayCommand(CanExecute = nameof(CanCommitGitStagedOnly))]
    private async Task CommitGitStagedOnlyAsync()
    {
        var msg = GitCommitMessage.Trim();
        if (string.IsNullOrWhiteSpace(msg) || !IsGitRepository)
            return;

        GitPanelStatusText = "Коммит (только staged)…";
        var result = await RunGitCommandAsync(["commit", "-m", msg]).ConfigureAwait(false);
        await UiScheduler.Default.InvokeAsync(() =>
        {
            GitPanelStatusText = result.Success ? "" : result.Output;
            if (result.Success)
                GitCommitMessage = "";
        });
        await RefreshGitPanelAsync().ConfigureAwait(false);
        await _refreshGitSummaryAsync().ConfigureAwait(false);
    }

    private bool CanCommitGitStagedOnly() => IsGitRepository && !string.IsNullOrWhiteSpace(GitCommitMessage);

    private async Task<(bool Success, int ExitCode, string Output)> RunGitCommandAsync(IReadOnlyList<string> args)
    {
        var workspace = _getWorkspacePath();
        if (string.IsNullOrWhiteSpace(workspace) || !Directory.Exists(workspace))
            return (false, -1, "Workspace path is not available.");
        return await _gitRunner.RunAsync(args, workspace).ConfigureAwait(false);
    }

    private static string TruncateOutput(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        return text.Length > maxChars ? text[..maxChars] + "\n... (output truncated)" : text;
    }
}
