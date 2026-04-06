using System.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Инвалидация производных высот <c>MainGrid</c> без длинных цепочек <c>NotifyPropertyChangedFor</c> в ShellState.</summary>
public partial class MainWindowViewModel
{
    /// <summary>Снимок высот нижней зоны для привязок строк Grid; обновляется при изменении флагов, влияющих на <see cref="MainWindowViewModel.ShowWorkspaceBottomChrome"/>.</summary>
    public MainGridRowHeightSet MainGridRowHeights =>
        MainGridRowHeightSet.ForWorkspaceBottomChrome(ShowWorkspaceBottomChrome);

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(ShowWorkspaceBottomChrome)
            or nameof(IsBottomPanelVisible)
            or nameof(ShowTelemetryStrip))
        {
            OnPropertyChanged(nameof(MainGridRowHeights));
        }

        if (e.PropertyName is nameof(TelemetryBuildText)
            or nameof(TelemetryTestsText)
            or nameof(TelemetryDebugText)
            or nameof(TelemetryBuildCockpitShort)
            or nameof(TelemetryTestsCockpitShort)
            or nameof(TelemetryDebugCockpitShort))
            RebuildAttentionStrip();
    }
}
