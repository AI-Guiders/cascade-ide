using Avalonia.Controls;
using Avalonia.Input;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

/// <summary>
/// Зона MFD: одна полоса вкладок (WORKSPACE / чат / терминал / сборка / …), без наслоения отдельного «нижнего дока» под чатом.
/// Имя Border#BottomPanelShell на корне сохранено для снимков UI / MCP.
/// </summary>
public partial class MfdShellView : UserControl
{
    public MfdShellView()
    {
        InitializeComponent();
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
}
