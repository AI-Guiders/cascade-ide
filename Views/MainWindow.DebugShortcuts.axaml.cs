using Avalonia.Input;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    /// <summary>
    /// Дублирует жесты из hotkeys.toml (Window.KeyBindings): при фокусе в редакторе Avalonia не всегда
    /// доставляет их до команд — tunnel на окне срабатывает до дочерних контролов.
    /// </summary>
    private void OnDebugShortcutKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        if (vm.TryConsumeCascadeChordKeyDown(e))
            return;
        Services.MainWindowHotkeyService.TryHandleTunnelShortcuts(e, vm);
    }
}
