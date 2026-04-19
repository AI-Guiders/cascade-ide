using Avalonia.Input;
using Avalonia.Interactivity;

namespace CascadeIDE.Views;

/// <summary>Второй <c>TopLevel</c> под зону Pfd: дерево решения / semantic map — ADR 0017.</summary>
public partial class PfdHostWindow : PointerTrackingWindow
{
    public PfdHostWindow()
    {
        InitializeComponent();
        AddHandler(InputElement.KeyDownEvent, OnTunnelKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private void OnTunnelKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        MainWindowHotkeyService.LogTunnelEvent(nameof(PfdHostWindow), e, vm, "window-entry");
        MainWindowHotkeyService.TryHandleTunnelKeyDownForMainVm(e, vm);
    }
}
