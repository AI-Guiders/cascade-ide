using Avalonia.Input;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    /// <summary>
    /// Глобальные жесты из hotkeys.toml: tunnel KeyDown на окне + <see cref="Services.KeyGestureChordMatching"/>
    /// (handledEventsToo: true — после фазы KeyBinding в Avalonia, если нужно тот же жест).
    /// </summary>
    private void OnDebugShortcutKeyDown(object? sender, KeyEventArgs e)
    {
        var vm = _boundMainVm ?? DataContext as ViewModels.MainWindowViewModel;
        if (vm is null)
            return;
        vm.CommandPaletteHost = ViewModels.CommandPaletteHost.MainWindow;
        Services.MainWindowHotkeyService.LogTunnelEvent(nameof(MainWindow), e, vm, "window-entry");
        Services.MainWindowHotkeyService.TryHandleTunnelKeyDownForMainVm(e, vm);
    }
}
