#nullable enable
using CascadeIDE.Models;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Переключатель лобового якоря Intercom / Editor (ADR 0120).</summary>
public partial class MainWindowViewModel
{
    public PrimaryWorkSurfaceKind PrimaryWorkSurface
    {
        get => PrimaryWorkSurfaceKindExtensions.ParseTomlValue(_settings.Workspace.PrimaryWorkSurface);
        set
        {
            var normalized = value.ToTomlValue();
            if (string.Equals(_settings.Workspace.PrimaryWorkSurface, normalized, StringComparison.OrdinalIgnoreCase))
                return;
            _settings.Workspace.PrimaryWorkSurface = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsForwardEditorHostVisible));
            OnPropertyChanged(nameof(IsForwardIntercomHostVisible));
            try
            {
                SettingsService.Save(_settings);
            }
            catch
            {
                // ignore persistence errors during toggle
            }
        }
    }

    public bool IsForwardEditorHostVisible => PrimaryWorkSurface == PrimaryWorkSurfaceKind.Editor;

    public bool IsForwardIntercomHostVisible => PrimaryWorkSurface == PrimaryWorkSurfaceKind.Intercom;

    [RelayCommand]
    private void TogglePrimaryWorkSurface() =>
        PrimaryWorkSurface = PrimaryWorkSurface == PrimaryWorkSurfaceKind.Editor
            ? PrimaryWorkSurfaceKind.Intercom
            : PrimaryWorkSurfaceKind.Editor;
}
