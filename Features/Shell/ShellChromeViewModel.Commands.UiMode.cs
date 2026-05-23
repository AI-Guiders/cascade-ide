using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.UiChrome;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Shell;

/// <summary>Relay: режим UI (shell chrome).</summary>
public sealed partial class ShellChromeViewModel
{
    /// <summary>Переключение режима по id из каталога (<see cref="UiModeCatalog.OrderedModeIds"/>).</summary>
    [RelayCommand]
    private void SetUiModeById(string? modeId)
    {
        if (string.IsNullOrWhiteSpace(modeId))
            return;
        UiMode = UiChromeViewModel.NormalizeUiMode(modeId);
    }

    /// <summary>Alt+1…9: N-й режим в <see cref="UiModeCatalog.OrderedModeIds"/> (0-based).</summary>
    [RelayCommand]
    private void SetUiModeByIndex(object? parameter)
    {
        var idx = UiModeSelectionParameter.ParseIndex(parameter);
        if (idx < 0)
            return;
        var ids = UiModeCatalog.OrderedModeIds;
        if (idx >= ids.Count)
            return;
        UiMode = ids[idx];
    }

    [RelayCommand]
    private void CycleUiMode()
    {
        var norm = UiChromeViewModel.NormalizeUiMode(UiMode);
        var ids = UiModeCatalog.OrderedModeIds;
        var idx = -1;
        for (var i = 0; i < ids.Count; i++)
        {
            if (string.Equals(ids[i], norm, StringComparison.OrdinalIgnoreCase))
            {
                idx = i;
                break;
            }
        }

        if (idx < 0 || ids.Count == 0)
        {
            UiMode = "Flight";
            return;
        }

        UiMode = ids[(idx + 1) % ids.Count];
    }
}
