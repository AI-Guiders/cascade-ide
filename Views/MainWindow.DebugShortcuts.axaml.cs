using Avalonia.Input;
using Avalonia.Interactivity;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    /// <summary>
    /// Дублирует отладочные KeyBinding с Window: при фокусе в редакторе Avalonia не всегда
    /// доставляет жесты до команд — tunnel на окне срабатывает до дочерних контролов.
    /// </summary>
    private void OnDebugShortcutKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        if (e.Key == Key.F5 && e.KeyModifiers == KeyModifiers.None)
        {
            if (vm.DebugStartOrContinueCommand.CanExecute(null))
            {
                vm.DebugStartOrContinueCommand.Execute(null);
                e.Handled = true;
            }
            return;
        }

        if (e.Key == Key.F5 && e.KeyModifiers == KeyModifiers.Shift)
        {
            if (vm.DebugStopCommand.CanExecute(null))
            {
                vm.DebugStopCommand.Execute(null);
                e.Handled = true;
            }
            return;
        }

        if (e.Key == Key.F10 && e.KeyModifiers == KeyModifiers.None)
        {
            if (vm.DebugStepOverCommand.CanExecute(null))
            {
                vm.DebugStepOverCommand.Execute(null);
                e.Handled = true;
            }
            return;
        }

        if (e.Key == Key.F11 && e.KeyModifiers == KeyModifiers.None)
        {
            if (vm.DebugStepIntoCommand.CanExecute(null))
            {
                vm.DebugStepIntoCommand.Execute(null);
                e.Handled = true;
            }
            return;
        }

        if (e.Key == Key.F11 && e.KeyModifiers == KeyModifiers.Shift)
        {
            if (vm.DebugStepOutCommand.CanExecute(null))
            {
                vm.DebugStepOutCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
