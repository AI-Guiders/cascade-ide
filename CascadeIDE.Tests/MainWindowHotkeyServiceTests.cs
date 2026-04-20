using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using CascadeIDE.Views;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Регрессия: tunnel <see cref="MainWindowHotkeyService.TryHandleTunnelKeyDownForMainVm"/> — Ctrl+Q ↔ палитра, Esc ↔ закрыть.
/// Map подменяется явно, чтобы не зависеть от пользовательского overlay в <c>%LocalAppData%\CascadeIDE\hotkeys.toml</c>.
/// </summary>
public sealed class MainWindowHotkeyServiceTests
{
    private static Dictionary<string, string> MinimalTogglePaletteMap() =>
        new(StringComparer.OrdinalIgnoreCase) { ["toggle_command_palette"] = "Ctrl+Q" };

    [Fact]
    public void BundledHotkeysToml_FileNextToTestAssembly_DefinesToggleCommandPalette()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Hotkeys", "hotkeys.toml");
        Assert.True(File.Exists(path), $"Ожидался шипнутый hotkeys.toml: {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("toggle_command_palette", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Ctrl+Q", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryHandleTunnelKeyDownForMainVm_CtrlQ_TogglesCommandPaletteOpen()
    {
        MainWindowHotkeyService.ReplaceMergedMapForTests(MinimalTogglePaletteMap());
        try
        {
            var vm = new MainWindowViewModel();
            Assert.False(vm.IsCommandPaletteOpen);

            var e = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Q,
                KeyModifiers = KeyModifiers.Control,
                PhysicalKey = PhysicalKey.Q
            };

            var handled = MainWindowHotkeyService.TryHandleTunnelKeyDownForMainVm(e, vm);
            Assert.True(handled);
            Assert.True(e.Handled);
            Assert.True(vm.IsCommandPaletteOpen);

            var e2 = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Q,
                KeyModifiers = KeyModifiers.Control,
                PhysicalKey = PhysicalKey.Q
            };
            Assert.True(MainWindowHotkeyService.TryHandleTunnelKeyDownForMainVm(e2, vm));
            Assert.False(vm.IsCommandPaletteOpen);
        }
        finally
        {
            MainWindowHotkeyService.ReplaceMergedMapForTests(null);
        }
    }

    /// <summary>Как в <see cref="KeyGestureChordMatchingTests"/>: нелатинская раскладка даёт другой <see cref="KeyEventArgs.Key"/>.</summary>
    [Fact]
    public void TryHandleTunnelKeyDownForMainVm_CtrlQ_PhysicalQ_VirtualMismatch_StillOpensPalette()
    {
        MainWindowHotkeyService.ReplaceMergedMapForTests(MinimalTogglePaletteMap());
        try
        {
            var vm = new MainWindowViewModel();
            var e = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.W,
                KeyModifiers = KeyModifiers.Control,
                PhysicalKey = PhysicalKey.Q
            };

            Assert.True(MainWindowHotkeyService.TryHandleTunnelKeyDownForMainVm(e, vm));
            Assert.True(vm.IsCommandPaletteOpen);
        }
        finally
        {
            MainWindowHotkeyService.ReplaceMergedMapForTests(null);
        }
    }

    [Fact]
    public void TryHandleTunnelKeyDownForMainVm_Esc_ClosesPalette_WhenOpen()
    {
        MainWindowHotkeyService.ReplaceMergedMapForTests(MinimalTogglePaletteMap());
        try
        {
            var vm = new MainWindowViewModel { IsCommandPaletteOpen = true };

            var e = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Escape,
                KeyModifiers = KeyModifiers.None,
                PhysicalKey = PhysicalKey.Escape
            };

            Assert.True(MainWindowHotkeyService.TryHandleTunnelKeyDownForMainVm(e, vm));
            Assert.True(e.Handled);
            Assert.False(vm.IsCommandPaletteOpen);
        }
        finally
        {
            MainWindowHotkeyService.ReplaceMergedMapForTests(null);
        }
    }

    [AvaloniaFact]
    public async Task TryHandleTunnelKeyDownForMainVm_CtrlShiftO_ExecutesRegistryCommand_WhenPaletteOpen()
    {
        MainWindowHotkeyService.ReplaceMergedMapForTests(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["open_solution_dialog"] = "Ctrl+Shift+O"
            });

        try
        {
            var vm = new MainWindowViewModel
            {
                IsCommandPaletteOpen = true
            };

            var requested = false;
            vm.RequestOpenSolution = () => requested = true;

            var e = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.O,
                KeyModifiers = KeyModifiers.Control | KeyModifiers.Shift,
                PhysicalKey = PhysicalKey.O
            };

            var handled = MainWindowHotkeyService.TryHandleTunnelKeyDownForMainVm(e, vm);
            await Dispatcher.UIThread.InvokeAsync(() => { });

            Assert.True(handled);
            Assert.True(e.Handled);
            Assert.True(requested);
        }
        finally
        {
            MainWindowHotkeyService.ReplaceMergedMapForTests(null);
        }
    }

    [AvaloniaFact]
    public void ApplyAll_AddsWindowKeyBinding_ForToggleCommandPalette()
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

            MainWindowHotkeyService.ApplyAll(window, vm);

            var binding = Assert.Single(window.KeyBindings, kb => kb.Gesture is KeyGesture gesture
                && gesture.Key == Key.Q
                && gesture.KeyModifiers == KeyModifiers.Control);
            Assert.NotNull(binding.Command);
        }
        finally
        {
            MainWindowHotkeyService.ReplaceMergedMapForTests(null);
        }
    }
}
