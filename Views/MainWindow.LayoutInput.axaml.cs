using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;
using CascadeIDE.Services;
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
        // Колонки 3–4 — сплиттер и зона MFD (чат/вторичный контур). Пока зона скрыта (в т.ч. Mfd на отдельном TopLevel),
        // не оставляем ширину по «чату» — иначе серая полоса без контента при пресете «P+F на первом дисплее».
        var w = vm.IsMfdColumnVisible ? vm.MfdRegionPixelWidth : 0;
        UiWorkspaceLayout.ApplyMfdRegionColumns(main, inner, w);
    }

    private void UpdateMarkdownPreviewColumn(bool showPreview)
    {
        var forwardSplit = MarkdownPreviewPlacementRuntime.Current == MarkdownPreviewPlacement.ForwardSplit;
        var showColumn = forwardSplit && showPreview;

        foreach (var v in EnumerateVisualDescendants(this))
        {
            if (v is not DockDocumentView dockView)
                continue;
            if (dockView.FindControl<Grid>("EditorContentGrid") is not { } grid)
                continue;

            var isActive = false;
            if (DataContext is ViewModels.MainWindowViewModel vm
                && dockView.DataContext is ViewModels.DockDocumentViewModel dv)
            {
                isActive = string.Equals(vm.CurrentFilePath, dv.Doc.FilePath, StringComparison.OrdinalIgnoreCase);
            }

            UiWorkspaceLayout.ApplyMarkdownPreviewColumn(grid, showColumn && isActive);
        }
    }

    /// <summary>Принудительно обновить контент панели превью справа от редактора (Markdown.Avalonia иногда не обновляет привязку при смене EditorText).</summary>
    private void UpdateInlineMarkdownPreview()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm || !vm.IsMarkdownFile)
        {
            _markdownDiagramPreviewCts?.Cancel();
            return;
        }

        if (MarkdownPreviewPlacementRuntime.Current != MarkdownPreviewPlacement.ForwardSplit)
            return;

        var dock = TryGetActiveDockDocumentView();
        var viewer = dock?.FindControl<Markdown.Avalonia.MarkdownScrollViewer>("InlineMarkdownPreview");
        if (viewer is null)
            return;

        var raw = vm.EditorText ?? "";
        viewer.Markdown = raw;

        if (!vm.MarkdownKrokiEnabled)
            return;

        _markdownDiagramPreviewCts?.Cancel();
        _markdownDiagramPreviewCts = new CancellationTokenSource();
        var token = _markdownDiagramPreviewCts.Token;
        var krokiSnapshot = new CascadeIdeSettings
        {
            MarkdownDiagrams = new MarkdownDiagramSettings
            {
                KrokiEnabled = vm.MarkdownKrokiEnabled,
                KrokiBaseUrl = string.IsNullOrWhiteSpace(vm.MarkdownKrokiBaseUrl)
                    ? "https://kroki.io"
                    : vm.MarkdownKrokiBaseUrl.Trim()
            }
        };

        _ = ExpandInlineMarkdownPreviewAsync(viewer, raw, krokiSnapshot, token);
    }

    private static async Task ExpandInlineMarkdownPreviewAsync(
        Markdown.Avalonia.MarkdownScrollViewer viewer,
        string rawMarkdown,
        CascadeIdeSettings krokiSettings,
        CancellationToken token)
    {
        try
        {
            await Task.Delay(400, token).ConfigureAwait(false);
            var expanded = await MarkdownDiagramExpansion.ExpandAsync(rawMarkdown, krokiSettings, token).ConfigureAwait(false);
            if (token.IsCancellationRequested)
                return;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!token.IsCancellationRequested)
                    viewer.Markdown = expanded;
            });
        }
        catch (OperationCanceledException)
        {
            // ожидаемо при новом вводе или смене файла
        }
    }
}
