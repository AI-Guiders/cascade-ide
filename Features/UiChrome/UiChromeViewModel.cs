using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Git-телеметрия для полосы задач и вспышка смены UI-режима Focus/Balanced/Power (bloom overlay).
/// </summary>
public sealed partial class UiChromeViewModel : ObservableObject
{
    private CancellationTokenSource? _uiModeBloomCts;
    private string? _lastAppliedUiModeForBloomEffects;

    public static string NormalizeUiMode(string? mode)
    {
        if (string.Equals(mode, "Focus", StringComparison.OrdinalIgnoreCase))
            return "Focus";
        if (string.Equals(mode, "Power", StringComparison.OrdinalIgnoreCase))
            return "Power";
        return "Balanced";
    }

    public async Task RefreshGitSummaryAsync(
        Func<IReadOnlyList<string>, Task<(bool Success, int ExitCode, string Output)>> runGit)
    {
        var result = await runGit(["status", "--short", "--branch"]).ConfigureAwait(false);
        Dispatcher.UIThread.Post(() =>
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
    public void NotifyUiModeChangedForBloom(string normalizedMode, bool isPowerMode, bool isFocusMode)
    {
        if (_lastAppliedUiModeForBloomEffects is not null
            && !string.Equals(_lastAppliedUiModeForBloomEffects, normalizedMode, StringComparison.OrdinalIgnoreCase))
            TriggerUiModeBloom(normalizedMode, isPowerMode, isFocusMode);
        _lastAppliedUiModeForBloomEffects = normalizedMode;
    }

    private void TriggerUiModeBloom(string normalizedMode, bool isPowerMode, bool isFocusMode)
    {
        _uiModeBloomCts?.Cancel();
        _uiModeBloomCts = new CancellationTokenSource();
        var ct = _uiModeBloomCts.Token;
        UiModeBloomBrush = PickUiModeBloomBrush(normalizedMode);
        UiModeBloomOpacity = 0;
        _ = RunUiModeBloomAsync(ct, isPowerMode, isFocusMode);
    }

    private static IBrush PickUiModeBloomBrush(string mode)
    {
        if (string.Equals(mode, "Power", StringComparison.OrdinalIgnoreCase))
            return new SolidColorBrush(Color.FromArgb(200, 110, 60, 210));
        if (string.Equals(mode, "Focus", StringComparison.OrdinalIgnoreCase))
            return new SolidColorBrush(Color.FromArgb(150, 25, 120, 185));
        return new SolidColorBrush(Color.FromArgb(120, 255, 235, 200));
    }

    private async Task RunUiModeBloomAsync(CancellationToken ct, bool isPowerMode, bool isFocusMode)
    {
        try
        {
            await Task.Delay(18, ct).ConfigureAwait(false);
            var peak = isPowerMode ? 0.2 : isFocusMode ? 0.13 : 0.11;
            await Dispatcher.UIThread.InvokeAsync(() => UiModeBloomOpacity = peak);
            await Task.Delay(300, ct).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() => UiModeBloomOpacity = 0);
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(() => UiModeBloomOpacity = 0);
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
    [NotifyPropertyChangedFor(nameof(TelemetryGitText))]
    [NotifyPropertyChangedFor(nameof(TelemetryGitCockpitShort))]
    private string _gitBranchSummary = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TelemetryGitText))]
    [NotifyPropertyChangedFor(nameof(TelemetryGitCockpitShort))]
    private int _gitStagedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TelemetryGitText))]
    [NotifyPropertyChangedFor(nameof(TelemetryGitCockpitShort))]
    private int _gitUnstagedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TelemetryGitText))]
    [NotifyPropertyChangedFor(nameof(TelemetryGitCockpitShort))]
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

    public string TelemetryGitCockpitShort
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

    public string TelemetryGitText
    {
        get
        {
            var branch = string.IsNullOrWhiteSpace(GitBranchSummary) ? "" : $" ({GitBranchSummary})";
            return $"Git: {GitStagedCount} staged, {GitUnstagedCount} unstaged, {GitUntrackedCount} untracked{branch}";
        }
    }
}
