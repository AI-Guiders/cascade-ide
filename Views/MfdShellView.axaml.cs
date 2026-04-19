#nullable enable
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

/// <summary>
/// Вторичный контур оболочки: одна активная страница (WORKSPACE / чат / терминал / сборка / …); v1 — колонка зоны Mfd по пресету.
/// Имя Border#BottomPanelShell на корне сохранено для снимков UI / MCP.
/// </summary>
public partial class MfdShellView : UserControl
{
    private bool _xamlInitialized;

    public MfdShellView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        if (_xamlInitialized)
            return;

        _xamlInitialized = true;
        AvaloniaXamlLoader.Load(this);
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
