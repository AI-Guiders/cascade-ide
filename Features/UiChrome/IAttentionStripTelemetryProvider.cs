namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Снимок строк телеметрии build/tests/debug/git для полосы внимания и композитора.
/// Реализация читает живое состояние (DAP, Git, тесты); <see cref="ViewModels.MainWindowViewModel"/> не форматирует строки сам.
/// </summary>
public interface IAttentionStripTelemetryProvider
{
    AttentionStripInputSnapshot GetSnapshot();
}
