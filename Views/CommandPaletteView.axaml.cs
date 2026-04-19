using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

public partial class CommandPaletteView : UserControl
{
    private INotifyPropertyChanged? _boundVm;
    private Window? _hostWindow;
    private IInputElement? _focusBeforeOpen;
    private bool _wasVisible;

    public CommandPaletteView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
        DataContextChanged += OnDataContextChanged;
        AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private TextBox? SearchBox => this.FindControl<TextBox>("SearchTextBox");

    private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        AttachHostWindow();
        BindVm();
        UpdateOpenState();
    }

    private void OnDetachedFromVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        DetachVm();
        DetachHostWindow();
        _focusBeforeOpen = null;
        _wasVisible = false;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        BindVm();
        UpdateOpenState();
    }

    private void BindVm()
    {
        if (ReferenceEquals(_boundVm, DataContext))
            return;

        DetachVm();

        _boundVm = DataContext as INotifyPropertyChanged;
        if (_boundVm is not null)
            _boundVm.PropertyChanged += OnVmPropertyChanged;
    }

    private void DetachVm()
    {
        if (_boundVm is not null)
            _boundVm.PropertyChanged -= OnVmPropertyChanged;
        _boundVm = null;
    }

    private void AttachHostWindow()
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (ReferenceEquals(_hostWindow, window))
            return;

        DetachHostWindow();
        _hostWindow = window;

        if (_hostWindow is null)
            return;

        _hostWindow.Activated += OnHostWindowActivationChanged;
        _hostWindow.Deactivated += OnHostWindowActivationChanged;
    }

    private void DetachHostWindow()
    {
        if (_hostWindow is null)
            return;

        _hostWindow.Activated -= OnHostWindowActivationChanged;
        _hostWindow.Deactivated -= OnHostWindowActivationChanged;
        _hostWindow = null;
    }

    private void OnHostWindowActivationChanged(object? sender, EventArgs e) => UpdateOpenState();

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.IsCommandPaletteOpen)
            or nameof(MainWindowViewModel.CommandPaletteHost))
            UpdateOpenState();
    }

    private void UpdateOpenState()
    {
        var vm = ViewModel;
        var visible = vm?.IsCommandPaletteOpen == true
                      && vm.CommandPaletteHost == ResolveHost();
        IsVisible = visible;

        if (visible == _wasVisible)
            return;

        _wasVisible = visible;
        if (visible)
        {
            _focusBeforeOpen = _hostWindow?.FocusManager?.GetFocusedElement();
            Dispatcher.UIThread.Post(FocusSearchBox, DispatcherPriority.Input);
            return;
        }

        var prev = _focusBeforeOpen;
        _focusBeforeOpen = null;
        Dispatcher.UIThread.Post(() =>
        {
            if (prev is Control control)
                control.Focus();
        }, DispatcherPriority.Background);
    }

    private void FocusSearchBox()
    {
        if (!IsVisible)
            return;

        if (SearchBox is not { } searchBox)
            return;

        searchBox.Focus();
        searchBox.SelectAll();
    }

    private CommandPaletteHost ResolveHost() =>
        _hostWindow switch
        {
            MfdHostWindow => CommandPaletteHost.MfdHost,
            PfdHostWindow => CommandPaletteHost.PfdHost,
            _ => CommandPaletteHost.MainWindow,
        };

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        var vm = ViewModel;
        if (vm is null || !vm.IsCommandPaletteOpen)
            return;

        switch (e.Key)
        {
            case Key.Escape:
                if (vm.CloseCommandPaletteCommand.CanExecute(null))
                    vm.CloseCommandPaletteCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Enter:
                if (vm.ExecuteCommandPaletteSelectionCommand.CanExecute(null))
                    vm.ExecuteCommandPaletteSelectionCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Down:
                if (vm.CommandPaletteMoveSelectionCommand.CanExecute(1))
                    vm.CommandPaletteMoveSelectionCommand.Execute(1);
                e.Handled = true;
                break;
            case Key.Up:
                if (vm.CommandPaletteMoveSelectionCommand.CanExecute(-1))
                    vm.CommandPaletteMoveSelectionCommand.Execute(-1);
                e.Handled = true;
                break;
            case Key.PageDown:
                if (vm.CommandPalettePageMoveCommand.CanExecute(1))
                    vm.CommandPalettePageMoveCommand.Execute(1);
                e.Handled = true;
                break;
            case Key.PageUp:
                if (vm.CommandPalettePageMoveCommand.CanExecute(-1))
                    vm.CommandPalettePageMoveCommand.Execute(-1);
                e.Handled = true;
                break;
        }
    }

    private void OnDimmerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel?.CloseCommandPaletteCommand.CanExecute(null) == true)
            ViewModel.CloseCommandPaletteCommand.Execute(null);
        e.Handled = true;
    }

    private void OnPanelPressed(object? sender, PointerPressedEventArgs e)
    {
        FocusSearchBox();
        e.Handled = true;
    }
}
