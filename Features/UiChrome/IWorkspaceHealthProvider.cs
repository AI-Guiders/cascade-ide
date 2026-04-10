namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Снимок строк build/tests/debug/git для полосы и композитора Workspace Health.
/// Реализация читает живое состояние (DAP, Git, тесты); <see cref="ViewModels.MainWindowViewModel"/> не форматирует строки сам.
/// </summary>
public interface IWorkspaceHealthProvider
{
    WorkspaceHealthInputSnapshot GetSnapshot();
}
