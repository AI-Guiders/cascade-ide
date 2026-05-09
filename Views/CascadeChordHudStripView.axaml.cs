using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CascadeIDE.Models.Shell;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

public partial class CascadeChordHudStripView : UserControl
{
    private MainWindowViewModel? _vm;
    private TextBox? _chordHudTextBox;

    public CascadeChordHudStripView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        DetachVm();
        _vm = DataContext as MainWindowViewModel;
        if (_vm is not null)
            _vm.CascadeChordHudFocusRequested += OnCascadeChordHudFocusRequested;
    }

    private void DetachVm()
    {
        if (_vm is not null)
            _vm.CascadeChordHudFocusRequested -= OnCascadeChordHudFocusRequested;
        _vm = null;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        DetachVm();
        if (_chordHudTextBox is not null)
        {
            _chordHudTextBox.GotFocus -= OnChordHudGotFocus;
            _chordHudTextBox.LostFocus -= OnChordHudLostFocus;
            _chordHudTextBox.RemoveHandler(InputElement.KeyDownEvent, OnChordHudPreviewKeyDown);
            _chordHudTextBox = null;
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_chordHudTextBox is not null)
            return;
        if (this.FindControl<TextBox>("ChordQueryBox") is not { } tb)
            return;
        _chordHudTextBox = tb;
        InputMethod.SetIsInputMethodEnabled(tb, false);
        tb.GotFocus += OnChordHudGotFocus;
        tb.LostFocus += OnChordHudLostFocus;
        tb.AddHandler(InputElement.KeyDownEvent, OnChordHudPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnCascadeChordHudFocusRequested()
    {
        Dispatcher.UIThread.Post(
            () =>
            {
                if (_chordHudTextBox is { IsVisible: true })
                {
                    _chordHudTextBox.Focus();
                    _chordHudTextBox.SelectAll();
                }
            },
            DispatcherPriority.Input);
    }

    private void OnChordHudGotFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.SetCascadeChordHudTextHasFocus(true);
    }

    private void OnChordHudLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.SetCascadeChordHudTextHasFocus(false);
    }

    private void OnChordHudPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        if (e.Key == Key.Escape)
        {
            vm.CancelCascadeChord();
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
            return;
        vm.OnCascadeChordHudEnter();
        e.Handled = true;
    }

    private void OnChordPopupClosed(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.NotifyCascadeChordDropdownDismissed();
    }

    private void OnChordSuggestionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 || sender is not ListBox lb || DataContext is not MainWindowViewModel vm)
            return;
        if (e.AddedItems[0] is CascadeChordOverlaySuggestion item)
        {
            vm.PickCascadeChordSuggestion(item);
            lb.SelectedItem = null;
        }
    }
}
