using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

public partial class CommandPaletteView : UserControl
{
    public CommandPaletteView()
    {
        InitializeComponent();
        IsVisibleProperty.Changed.AddClassHandler<CommandPaletteView>(OnIsVisibleChanged);
        AddHandler(KeyDownEvent, OnPaletteTunnelKeyDown, RoutingStrategies.Tunnel);
    }

    private static void OnIsVisibleChanged(CommandPaletteView s, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            Dispatcher.UIThread.Post(() =>
            {
                s.SearchTextBox?.Focus();
                s.SearchTextBox?.SelectAll();
            }, DispatcherPriority.Loaded);
        }
    }

    /// <summary>Tunnel: срабатывает до TextBox/ListBox, чтобы Enter/стрелки работали при любом фокусе внутри палитры.</summary>
    private void OnPaletteTunnelKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsVisible || DataContext is not MainWindowViewModel vm)
            return;

        if (e.Key == Key.Q && e.KeyModifiers == KeyModifiers.Control)
        {
            SearchTextBox?.SelectAll();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            vm.CloseCommandPaletteCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            vm.ExecuteCommandPaletteSelectionCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down)
        {
            vm.CommandPaletteMoveSelectionCommand.Execute(1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up)
        {
            vm.CommandPaletteMoveSelectionCommand.Execute(-1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.PageDown)
        {
            vm.CommandPalettePageMoveCommand.Execute(1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.PageUp)
        {
            vm.CommandPalettePageMoveCommand.Execute(-1);
            e.Handled = true;
        }
    }

    private void OnDimmerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.CloseCommandPaletteCommand.Execute(null);
    }

    private void OnPanelPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }
}
