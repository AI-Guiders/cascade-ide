using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private void UpdateSolutionColumnWidth(bool visible)
    {
        if (this.FindControl<Grid>("MainGrid") is not { } grid)
            return;
        UiWorkspaceLayout.ApplyPfdRegionExpanded(grid, visible);
    }

    private void SetupTerminalKeyHandler()
    {
        var box = this.FindControl<TextBox>("TerminalInputBox");
        if (box is null) return;
        box.KeyDown += (_, ev) =>
        {
            if (ev.Key != Key.Enter || DataContext is not ViewModels.MainWindowViewModel vm) return;
            var cmd = vm.TerminalPanel.RunTerminalCommandCommand;
            if (cmd.CanExecute(null))
            {
                cmd.Execute(null);
                ev.Handled = true;
            }
        };
    }

    private void SetupChatInputKeyHandler()
    {
        var box = this.FindControl<Avalonia.Controls.TextBox>("ChatInputBox");
        if (box is null) return;
        // Туннель: перехватываем Enter до того, как TextBox (AcceptsReturn=true) обработает его и вставит перевод строки.
        box.AddHandler(InputElement.KeyDownEvent, OnChatInputKeyDown, RoutingStrategies.Tunnel);

        void OnChatInputKeyDown(object? s, KeyEventArgs e)
        {
            if (DataContext is not ViewModels.MainWindowViewModel vm) return;
            var key = vm.SendMessageKey;
            var isEnter = e.Key == Key.Enter;
            var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            var match = key switch
            {
                "Enter" => isEnter && !ctrl && !shift,
                "Ctrl+Enter" => isEnter && ctrl && !shift,
                "Shift+Enter" => isEnter && !ctrl && shift,
                _ => false
            };
            if (match && (vm.ChatPanel.SendChatCommand as IRelayCommand)?.CanExecute(null) == true)
            {
                _ = vm.ChatPanel.SendChatCommand.ExecuteAsync(null);
                e.Handled = true;
            }
        }
    }

    private void UpdateChatColumnWidth(ViewModels.MainWindowViewModel vm)
    {
        if (this.FindControl<Grid>("MainGrid") is not { } main)
            return;
        var inner = this.FindControl<Grid>("WorkspaceHealthColumnsGrid");
        // MainGrid: сплиттер и MFD — см. UiWorkspaceLayoutDimensions.MainWindowMainGridColumns (индексы 3 и 4). Пока зона скрыта (в т.ч. Mfd на отдельном TopLevel),
        // не оставляем ширину по «чату» — иначе серая полоса без контента при пресете «P+F на первом дисплее».
        var w = vm.IsMfdColumnVisible ? vm.MfdRegionPixelWidth : 0;
        UiWorkspaceLayout.ApplyMfdRegionColumns(main, inner, w);
    }
}
