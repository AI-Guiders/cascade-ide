using CascadeIDE.Features.Settings.Application;
using CascadeIDE.Features.Shell.Application;

namespace CascadeIDE.ViewModels;

/// <summary>Settings reactions: workspace splitters + Hybrid Index (HCI).</summary>
public partial class MainWindowViewModel
{
    partial void OnWorkspaceSplittersLockedChanged(bool value)
    {
        _settings.Workspace.SplittersLocked = value;
        if (_lastSavedSettings is not null)
            SaveSettingsIfChanged();
    }

    partial void OnHciIntegrationEnabledChanged(bool value)
    {
        _settings.HybridIndex.Enabled = value;
        ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false);
        SaveSettingsIfChanged();
    }

    partial void OnHciIndexDirChanged(string value)
    {
        var normalized = ShellSettingsPresentationProjection.NormalizeHybridIndexDir(value);
        if (ShellSettingsPresentationProjection.ShouldRewriteWithNormalizedValue(value, normalized))
        {
            HciIndexDir = normalized;
            return;
        }

        ShellSettingsReactiveSideEffects.ApplyHybridIndexDirPersisted(
            normalized,
            _settings,
            _hybridIndex,
            () => ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false),
            SaveSettingsIfChanged,
            RaiseHybridIndexPresentationProperties);
    }

    partial void OnHciDebounceMsChanged(int value)
    {
        var v = Math.Clamp(value, 0, 60_000);
        if (v != value)
        {
            HciDebounceMs = v;
            return;
        }

        _settings.HybridIndex.DebounceMs = v;
        ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false);
        SaveSettingsIfChanged();
    }

    partial void OnHciAutoReindexOnSolutionOpenChanged(bool value)
    {
        _settings.HybridIndex.AutoReindexOnSolutionOpen = value;
        SaveSettingsIfChanged();
    }

    partial void OnHciWatchFilesChanged(bool value)
    {
        _settings.HybridIndex.WatchFiles = value;
        ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false);
        SaveSettingsIfChanged();
    }

    partial void OnHciScopeModeChanged(string value)
    {
        var n = ShellSettingsPresentationProjection.NormalizeHybridIndexScopeMode(value);
        if (ShellSettingsPresentationProjection.ShouldRewriteWithNormalizedValue(value, n))
        {
            HciScopeMode = n;
            return;
        }

        ShellSettingsReactiveSideEffects.ApplyHybridIndexScopeModePersisted(
            n,
            _settings,
            () => ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false),
            SaveSettingsIfChanged,
            RaiseHybridIndexPresentationProperties);
    }

    partial void OnHciPauseWhenMcpStdioHostChanged(bool value)
    {
        _settings.HybridIndex.PauseWhenMcpStdioHost = value;
        ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: false);
        SaveSettingsIfChanged();
    }
}
