using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using CascadeIDE.Views;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MainWindowHotkeyRoutingHeadlessTests
{
    [AvaloniaFact]
    public void CtrlQ_FromFocusedChild_ReachesMainWindowTunnelHandler()
    {
        MainWindowHotkeyService.ReplaceMergedMapForTests(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["toggle_command_palette"] = "Ctrl+Q"
            });

        try
        {
            var vm = new MainWindowViewModel();
            var window = new MainWindow
            {
                DataContext = vm
            };

            window.Show();

            var mainGrid = window.FindControl<Grid>("MainGrid");
            Assert.NotNull(mainGrid);

            var e = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Source = mainGrid,
                Key = Key.Q,
                KeyModifiers = KeyModifiers.Control,
                PhysicalKey = PhysicalKey.Q
            };

            mainGrid.RaiseEvent(e);

            Assert.True(e.Handled);
            Assert.True(vm.IsCommandPaletteOpen);
        }
        finally
        {
            MainWindowHotkeyService.ReplaceMergedMapForTests(null);
        }
    }

}
