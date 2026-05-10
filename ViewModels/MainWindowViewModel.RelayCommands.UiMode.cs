using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Relay: режим UI и уровень безопасности.</summary>
public partial class MainWindowViewModel
{
    /// <summary>Переключение режима по id из каталога (<see cref="UiModeCatalog.OrderedModeIds"/>), меню и MCP.</summary>
    [RelayCommand]
    private void SetUiModeById(string? modeId)
    {
        if (string.IsNullOrWhiteSpace(modeId))
            return;
        UiMode = NormalizeUiMode(modeId);
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
        var norm = NormalizeUiMode(UiMode);
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

    [RelayCommand]
    private void SetSafetyL1() => SafetyLevel = "L1";

    [RelayCommand]
    private void SetSafetyL2() => SafetyLevel = "L2";

    [RelayCommand]
    private void SetSafetyL3() => SafetyLevel = "L3";
}
