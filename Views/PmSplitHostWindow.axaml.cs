using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CascadeIDE.Models.Shell;

namespace CascadeIDE.Views;

/// <summary>TopLevel со сплитом PFD | MFD для пресета <c>(xP+yM)(F)</c> — ADR 0017.</summary>
public partial class PmSplitHostWindow : PointerTrackingWindow
{
    public PmSplitHostWindow()
    {
        InitializeComponent();
        AddHandler(InputElement.KeyDownEvent, OnTunnelKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        Loaded += OnLoaded;
        Activated += OnActivated;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (PmSplitRootGrid is not { } grid)
            return;
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        grid.ColumnDefinitions.Clear();
        foreach (var col in ColumnDefinitions.Parse(vm.PmSplitHostColumnDefinitions))
            grid.ColumnDefinitions.Add(col);
    }

    private void OnTunnelKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        vm.CommandPaletteHost = CommandPaletteHost.PmSplitHost;
        MainWindowHotkeyService.TryHandleTunnelKeyDownFromWindow(nameof(PmSplitHostWindow), e, vm);
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        if (DataContext is ViewModels.MainWindowViewModel vm)
            vm.CommandPaletteHost = CommandPaletteHost.PmSplitHost;
    }
}
