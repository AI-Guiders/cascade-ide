namespace CascadeIDE.ViewModels;

/// <summary>Запись intent видимости панелей (семантика «хочу»); фактическая поверхность — <see cref="MainWindowShellSurfaceCompositor"/>.</summary>
public partial class MainWindowViewModel
{
    /// <summary>Предпочтение контента зоны PFD (не surface visibility).</summary>
    public void ApplySolutionExplorerVisible(bool desired) =>
        IsSolutionExplorerVisible = desired;

    /// <summary>Предпочтение контента зоны MFD (не surface visibility).</summary>
    public void ApplyChatPanelExpanded(bool desired) =>
        IsChatPanelExpanded = desired;
}
