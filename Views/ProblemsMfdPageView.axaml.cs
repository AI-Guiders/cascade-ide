#nullable enable
using Avalonia.Controls;
using Avalonia.Input;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

public partial class ProblemsMfdPageView : UserControl
{
    public ProblemsMfdPageView() => InitializeComponent();

    private void ProblemsList_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not ListBox lb || lb.DataContext is not ProblemsPanelViewModel panel)
            return;
        if (lb.SelectedItem is not ProblemListItem item)
            return;
        if (panel.NavigateCommand.CanExecute(item))
            panel.NavigateCommand.Execute(item);
    }
}
