using System.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Инвалидация производных высот <c>MainGrid</c> без длинных цепочек <c>NotifyPropertyChangedFor</c> в ShellState.</summary>
public partial class MainWindowViewModel
{
    /// <summary>Снимок высот строк MainGrid: нижний док перенесён в MFD — строки «сплиттер + низ» у корневой сетки не используются.</summary>
    public MainGridRowHeightSet MainGridRowHeights =>
        MainGridRowHeightSet.ForWorkspaceBottomChrome(false);

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(TelemetryBuildText)
            or nameof(TelemetryTestsText)
            or nameof(TelemetryDebugText)
            or nameof(TelemetryBuildCockpitShort)
            or nameof(TelemetryTestsCockpitShort)
            or nameof(TelemetryDebugCockpitShort))
            RebuildWorkspaceTelemetry();
    }
}
