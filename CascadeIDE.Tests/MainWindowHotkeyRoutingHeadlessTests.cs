using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using CascadeIDE.Models.Shell;
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

    [AvaloniaFact]
    public void CommandPalette_IsVisibleOnlyInMatchingHostWindow()
    {
        var vm = new MainWindowViewModel();
        var mainWindow = new MainWindow { DataContext = vm };
        var pfdWindow = new PfdHostWindow { DataContext = vm };
        var mfdWindow = new MfdHostWindow { DataContext = vm };

        mainWindow.Show();
        pfdWindow.Show();
        mfdWindow.Show();

        var mainPalette = FindDescendant<CommandPaletteView>(mainWindow);
        var pfdPalette = FindDescendant<CommandPaletteView>(pfdWindow);
        var mfdPalette = FindDescendant<CommandPaletteView>(mfdWindow);

        Assert.NotNull(mainPalette);
        Assert.NotNull(pfdPalette);
        Assert.NotNull(mfdPalette);

        vm.IsCommandPaletteOpen = true;
        vm.CommandPaletteHost = CommandPaletteHost.MainWindow;
        Assert.True(mainPalette!.IsVisible);
        Assert.False(pfdPalette!.IsVisible);
        Assert.False(mfdPalette!.IsVisible);

        vm.CommandPaletteHost = CommandPaletteHost.PfdHost;
        Assert.False(mainPalette.IsVisible);
        Assert.True(pfdPalette.IsVisible);
        Assert.False(mfdPalette.IsVisible);

        vm.CommandPaletteHost = CommandPaletteHost.MfdHost;
        Assert.False(mainPalette.IsVisible);
        Assert.False(pfdPalette.IsVisible);
        Assert.True(mfdPalette.IsVisible);

        mainWindow.Close();
        pfdWindow.Close();
        mfdWindow.Close();
    }

    private static T? FindDescendant<T>(TopLevel root)
        where T : class
    {
        return root.GetVisualDescendants().OfType<T>().FirstOrDefault();
    }

}
