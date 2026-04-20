namespace CascadeIDE.ViewModels;

/// <summary>Запись intent видимости панелей (семантика «хочу»); фактическая поверхность — <see cref="MainWindowShellSurfaceCompositor"/>.</summary>
public partial class MainWindowViewModel
{
    /// <summary>Предпочтение контента зоны PFD (не surface visibility).</summary>
    public void ApplyPfdRegionExpanded(bool desired) =>
        IsPfdRegionExpanded = desired;

    /// <summary>
    /// Intent ширины региона Mfd в <c>MainGrid</c> (развёрнут/свёрнут в смысле геометрии раскладки).
    /// Страница «Чат» — <see cref="Models.MfdShellPage.Chat"/> через <see cref="MainWindowViewModel.CurrentMfdShellPage"/>, отдельно.
    /// </summary>
    public void ApplyMfdRegionExpanded(bool desired) =>
        IsMfdRegionExpanded = desired;
}
