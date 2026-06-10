using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

public partial class TerminalMfdPageView : UserControl
{
    public TerminalMfdPageView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        TerminalHostChrome.PointerPressed += OnTerminalHostPointerPressed;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty && change.GetNewValue<bool>())
            ActivateTerminal();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e) => ActivateTerminal();

    private void OnTerminalHostPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(TerminalHostChrome).Properties.IsLeftButtonPressed)
            FocusTerminal();
    }

    private void ActivateTerminal()
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        // Явная привязка Model (как в sample AvaloniaTerminal) + фокус для KeyDown/TextInput.
        TerminalView.Model = vm.TerminalPanel.TerminalModel;
        vm.TerminalPanel.EnsureSessionStarted();
        FocusTerminal();
    }

    private void FocusTerminal()
    {
        if (TerminalView.Model is null)
            return;

        TerminalView.Focus(NavigationMethod.Tab);
    }
}
