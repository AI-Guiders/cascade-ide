#nullable enable

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CascadeIDE.ViewModels;
using CascadeIDE.Views;

namespace CascadeIDE.Views;

public partial class ProblemsMfdPageView : UserControl
{
    private Point _problemDragStart;
    private bool _problemDragArmed;
    private PointerPressedEventArgs? _problemDragPress;

    public ProblemsMfdPageView()
    {
        InitializeComponent();
        AddHandler(PointerPressedEvent, OnProblemsPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnProblemsPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnProblemsPointerReleased, RoutingStrategies.Tunnel);
    }

    private void ProblemsList_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not ListBox lb || lb.DataContext is not ProblemsPanelViewModel panel)
            return;
        if (lb.SelectedItem is not ProblemListItem item)
            return;
        if (panel.NavigateCommand.CanExecute(item))
            panel.NavigateCommand.Execute(item);
    }

    private void ProblemsList_OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (sender is not ListBox lb || lb.DataContext is not ProblemsPanelViewModel panel)
            return;
        if (lb.SelectedItem is not ProblemListItem item)
            return;
        if (panel.AttachToIntercomCommand.CanExecute(item) != true)
            return;

        e.Handled = true;
        var menu = new ContextMenu();
        var attach = new MenuItem { Header = "Прикрепить к Intercom" };
        attach.Click += (_, _) => panel.AttachToIntercomCommand.Execute(item);
        menu.Items.Add(attach);
        menu.Open(lb);
    }

    private void OnProblemsPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not ProblemsMfdPageView || e.Source is not ListBox lb)
            return;
        if (lb.DataContext is not ProblemsPanelViewModel panel)
            return;
        if (lb.SelectedItem is not ProblemListItem item)
            return;
        if (!e.GetCurrentPoint(lb).Properties.IsLeftButtonPressed)
            return;

        _problemDragStart = e.GetPosition(lb);
        _problemDragPress = e;
        _problemDragArmed = panel.AttachToIntercomCommand.CanExecute(item);
    }

    private async void OnProblemsPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_problemDragArmed || _problemDragPress is null || e.Source is not ListBox lb || lb.SelectedItem is not ProblemListItem item)
            return;

        var point = e.GetPosition(lb);
        if (Math.Abs(point.X - _problemDragStart.X) < 6 && Math.Abs(point.Y - _problemDragStart.Y) < 6)
            return;

        _problemDragArmed = false;
        var press = _problemDragPress;
        _problemDragPress = null;
        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(
            DataFormat.Text,
            IntercomAttachDragPayload.ForProblem(item)));
        await DragDrop.DoDragDropAsync(press, data, DragDropEffects.Copy);
    }

    private void OnProblemsPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _problemDragArmed = false;
        _problemDragPress = null;
    }
}
