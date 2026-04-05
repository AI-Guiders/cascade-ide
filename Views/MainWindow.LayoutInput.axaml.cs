using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private void UpdateSolutionColumnWidth(bool visible)
    {
        if (this.FindControl<Grid>("MainGrid") is not { } grid)
            return;
        UiWorkspaceLayout.ApplySolutionExplorerVisible(grid, visible);
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
        var inner = this.FindControl<Grid>("WorkspaceTelemetryColumnsGrid");
        UiWorkspaceLayout.ApplyChatPanelColumns(main, inner, vm.ChatPanelColumnPixelWidth);
    }

    private void UpdateMarkdownPreviewColumn(bool showPreview)
    {
        if (this.FindControl<Grid>("EditorContentGrid") is not { } grid)
            return;
        UiWorkspaceLayout.ApplyMarkdownPreviewColumn(grid, showPreview);
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
