using Avalonia.Input;
using Avalonia.Interactivity;
using CascadeIDE.Models.Shell;

namespace CascadeIDE.Views;

/// <summary>Второй <c>TopLevel</c> под зону Pfd: дерево решения / semantic map — ADR 0017.</summary>
public partial class PfdHostWindow : PointerTrackingWindow
{
    public PfdHostWindow()
    {
        InitializeComponent();
        AddHandler(InputElement.KeyDownEvent, OnTunnelKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        Activated += OnActivated;
    }

    private void OnTunnelKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        vm.CommandPaletteHost = CommandPaletteHost.PfdHost;
        MainWindowHotkeyService.TryHandleTunnelKeyDownFromWindow(nameof(PfdHostWindow), e, vm);
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        if (DataContext is ViewModels.MainWindowViewModel vm)
            vm.CommandPaletteHost = CommandPaletteHost.PfdHost;
    }
}
