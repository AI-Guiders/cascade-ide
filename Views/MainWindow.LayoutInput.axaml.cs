using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private void UpdateSolutionColumnWidth(bool visible)
    {
        var grid = this.FindControl<Grid>("MainGrid");
        if (grid?.ColumnDefinitions.Count > 0 != true)
            return;
        grid.ColumnDefinitions[0] = new ColumnDefinition(visible ? new GridLength(220, GridUnitType.Pixel) : new GridLength(0, GridUnitType.Pixel));
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
        var grid = this.FindControl<Grid>("MainGrid");
        if (grid?.ColumnDefinitions.Count > 4)
            grid.ColumnDefinitions[4].Width = new GridLength(vm.IsChatPanelExpanded ? (vm.IsPowerMode ? 420 : 340) : 88);
    }

    private void UpdateMarkdownPreviewColumn(bool showPreview)
    {
        var grid = this.FindControl<Grid>("EditorContentGrid");
        if (grid?.ColumnDefinitions.Count > 1)
            grid.ColumnDefinitions[1].Width = showPreview ? new GridLength(1, GridUnitType.Star) : new GridLength(0, GridUnitType.Pixel);
    }

    /// <summary>Принудительно обновить контент панели превью справа от редактора (Markdown.Avalonia иногда не обновляет привязку при смене EditorText).</summary>
    private void UpdateInlineMarkdownPreview()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm || !vm.IsMarkdownFile)
            return;
        var viewer = this.FindControl<Markdown.Avalonia.MarkdownScrollViewer>("InlineMarkdownPreview");
        if (viewer is not null)
            viewer.Markdown = vm.EditorText ?? "";
    }
}
