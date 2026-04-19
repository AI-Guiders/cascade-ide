using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.ComponentModel;

namespace CascadeIDE.Views;

/// <summary>Второй <c>TopLevel</c> под зону Mfd: только <see cref="MfdShellView"/> (вторичный контур). Дерево и прочий UI — контент страниц внутри shell, не дублирование колонки главного окна в этом хосте — ADR 0017 п. 8.</summary>
public partial class MfdHostWindow : PointerTrackingWindow
{
    private INotifyPropertyChanged? _boundVm;
    private CockpitSkiaSceneRenderer? _renderer;

    public MfdHostWindow()
    {
        InitializeComponent();
        AddHandler(InputElement.KeyDownEvent, OnTunnelKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        DataContextChanged += OnDataContextChanged;
        Activated += OnActivated;
        Closed += (_, _) =>
        {
            if (_boundVm is not null)
                _boundVm.PropertyChanged -= OnVmPropertyChanged;
            _boundVm = null;
        };
    }

    private void OnTunnelKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        vm.CommandPaletteHost = ViewModels.CommandPaletteHost.MfdHost;
        MainWindowHotkeyService.LogTunnelEvent(nameof(MfdHostWindow), e, vm, "window-entry");
        MainWindowHotkeyService.TryHandleTunnelKeyDownForMainVm(e, vm);
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        if (DataContext is ViewModels.MainWindowViewModel vm)
            vm.CommandPaletteHost = ViewModels.CommandPaletteHost.MfdHost;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_boundVm is not null)
            _boundVm.PropertyChanged -= OnVmPropertyChanged;

        _boundVm = DataContext as INotifyPropertyChanged;
        if (_boundVm is not null)
            _boundVm.PropertyChanged += OnVmPropertyChanged;

        if (this.FindControl<SkiaHost>("MfdHostSkiaHost") is { } host)
        {
            _renderer ??= new CockpitSkiaSceneRenderer(
                () => DataContext as ViewModels.MainWindowViewModel,
                SkiaHostSurface.MfdHostWindow);
            host.Renderer = _renderer;
            host.InvalidateVisual();
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!MainWindow.IsSkiaHostRelatedProperty(e.PropertyName))
            return;

        if (this.FindControl<SkiaHost>("MfdHostSkiaHost") is { } host)
            host.InvalidateVisual();
    }
}
