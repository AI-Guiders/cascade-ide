using System.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Инвалидация производных высот <c>MainGrid</c> без длинных цепочек <c>NotifyPropertyChangedFor</c> в ShellState.</summary>
public partial class MainWindowViewModel
{
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(IdeHealthBuildText)
            or nameof(IdeHealthTestsText)
            or nameof(IdeHealthDebugText)
            or nameof(IdeHealthBuildCockpitShort)
            or nameof(IdeHealthTestsCockpitShort)
            or nameof(IdeHealthDebugCockpitShort))
            RebuildIdeHealth();
    }
}
