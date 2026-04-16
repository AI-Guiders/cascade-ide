using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Git-строки для полосы Workspace Health и вспышка смены UI-режима Focus/Balanced/Power (bloom overlay).
/// </summary>
public sealed partial class UiChromeViewModel : ObservableObject
{
    private CancellationTokenSource? _uiModeBloomCts;
    private string? _lastAppliedUiModeForBloomEffects;

    public static string NormalizeUiMode(string? mode) => UiModeCatalog.NormalizeUiMode(mode);

    public async Task RefreshGitSummaryAsync(
        Func<IReadOnlyList<string>, Task<(bool Success, int ExitCode, string Output)>> runGit)
    {
        var result = await runGit(["status", "--short", "--branch"]).ConfigureAwait(false);
        UiScheduler.Default.Post(() =>
        {
            if (!result.Success)
            {
                GitBranchSummary = "";
                GitStagedCount = 0;
                GitUnstagedCount = 0;
                GitUntrackedCount = 0;
                FilesChangedBadge = 0;
                return;
            }

            var parsed = ParseGitStatusShortBranch(result.Output);
            GitBranchSummary = parsed.BranchSummary;
            GitStagedCount = parsed.Staged;
            GitUnstagedCount = parsed.Unstaged;
            GitUntrackedCount = parsed.Untracked;
            FilesChangedBadge = parsed.ChangedPaths;
        });
    }

    /// <summary>Вызывается из <see cref="MainWindowViewModel"/> после нормализации и сохранения режима UI.</summary>
    public void NotifyUiModeChangedForBloom(string normalizedMode)
    {
        if (_lastAppliedUiModeForBloomEffects is not null
            && !string.Equals(_lastAppliedUiModeForBloomEffects, normalizedMode, StringComparison.OrdinalIgnoreCase))
            TriggerUiModeBloom(normalizedMode);
        _lastAppliedUiModeForBloomEffects = normalizedMode;
    }

    private void TriggerUiModeBloom(string normalizedMode)
    {
        _uiModeBloomCts?.Cancel();
        _uiModeBloomCts = new CancellationTokenSource();
        var ct = _uiModeBloomCts.Token;
        UiModeBloomBrush = PickUiModeBloomBrush(normalizedMode);
        UiModeBloomOpacity = 0;
        _ = RunUiModeBloomAsync(normalizedMode, ct);
    }

    private static IBrush PickUiModeBloomBrush(string mode)
    {
        if (string.Equals(mode, "Flight", StringComparison.OrdinalIgnoreCase))
            return new SolidColorBrush(Color.FromArgb(150, 60, 160, 220));
        return new SolidColorBrush(Color.FromArgb(120, 255, 235, 200));
    }

    private static double BloomPeakOpacity(string normalizedMode) =>
        string.Equals(normalizedMode, "Flight", StringComparison.OrdinalIgnoreCase) ? 0.14 : 0.11;

    private async Task RunUiModeBloomAsync(string normalizedMode, CancellationToken ct)
    {
        try
        {
            await Task.Delay(18, ct).ConfigureAwait(false);
            var peak = BloomPeakOpacity(normalizedMode);
            await UiScheduler.Default.InvokeAsync(() => UiModeBloomOpacity = peak);
            await Task.Delay(300, ct).ConfigureAwait(false);
            await UiScheduler.Default.InvokeAsync(() => UiModeBloomOpacity = 0);
        }
        catch (OperationCanceledException)
        {
            await UiScheduler.Default.InvokeAsync(() => UiModeBloomOpacity = 0);
        }
    }

    private static (string BranchSummary, int Staged, int Unstaged, int Untracked, int ChangedPaths)
        ParseGitStatusShortBranch(string output)
    {
        var branch = "";
        int staged = 0, unstaged = 0, untracked = 0, changedPaths = 0;
        var lines = (output ?? "")
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                branch = line[3..].Trim();
                continue;
            }

            changedPaths++;

            if (line.StartsWith("??", StringComparison.Ordinal))
            {
                untracked++;
                continue;
            }
            if (line.Length < 2)
                continue;
            var x = line[0];
            var y = line[1];
            if (x != ' ' && x != '?')
                staged++;
            if (y != ' ' && y != '?')
                unstaged++;
        }

        return (branch, staged, unstaged, untracked, changedPaths);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthGitText))]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthGitCockpitShort))]
    private string _gitBranchSummary = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthGitText))]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthGitCockpitShort))]
    private int _gitStagedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthGitText))]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthGitCockpitShort))]
    private int _gitUnstagedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthGitText))]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthGitCockpitShort))]
    private int _gitUntrackedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilesChangedBadgeVisible))]
    private int _filesChangedBadge;

    /// <summary>Короткая «кинематографичная» вспышка при смене режима UI (Opacity + кисть; разметка — MainWindow).</summary>
    [ObservableProperty]
    private double _uiModeBloomOpacity;

    [ObservableProperty]
    private IBrush _uiModeBloomBrush = Brushes.Transparent;

    public bool IsFilesChangedBadgeVisible => FilesChangedBadge > 0;

    public string WorkspaceHealthGitCockpitShort
    {
        get
        {
            var br = GitBranchSummary ?? "";
            if (br.Length > 16)
                br = string.Concat(br.AsSpan(0, 14), "…");
            var delta = GitStagedCount + GitUnstagedCount + GitUntrackedCount;
            return string.IsNullOrWhiteSpace(br) ? $"Δ{delta}" : $"{br} · Δ{delta}";
        }
    }

    public string WorkspaceHealthGitText
    {
        get
        {
            var branch = string.IsNullOrWhiteSpace(GitBranchSummary) ? "" : $" ({GitBranchSummary})";
            return $"Git: {GitStagedCount} staged, {GitUnstagedCount} unstaged, {GitUntrackedCount} untracked{branch}";
        }
    }
}
