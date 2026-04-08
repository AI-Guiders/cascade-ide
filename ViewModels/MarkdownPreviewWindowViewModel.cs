using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>ViewModel для отдельного окна превью Markdown. Поддерживает режим «контент от агента» и «живое» превью из редактора.</summary>
public sealed partial class MarkdownPreviewWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Превью";

    [ObservableProperty]
    private string _markdown = "";

    private MainWindowViewModel? _editorVm;
    private PropertyChangedEventHandler? _editorHandler;
    private CancellationTokenSource? _diagramPreviewCts;

    /// <summary>Подключить к редактору: превью обновляется при изменении EditorText/CurrentFilePath.</summary>
    public void AttachToEditor(MainWindowViewModel vm)
    {
        DetachFromEditor();
        _editorVm = vm;
        _editorHandler = (_, e) =>
        {
            if (e.PropertyName is nameof(MainWindowViewModel.EditorText) or nameof(MainWindowViewModel.CurrentFilePath)
                or nameof(MainWindowViewModel.MarkdownKrokiEnabled) or nameof(MainWindowViewModel.MarkdownKrokiBaseUrl))
                UpdateFromEditor();
        };
        vm.PropertyChanged += _editorHandler;
        UpdateFromEditor();
    }

    /// <summary>Отвязать от редактора (переход в режим контента от агента).</summary>
    public void DetachFromEditor()
    {
        _diagramPreviewCts?.Cancel();
        _diagramPreviewCts = null;
        if (_editorVm is null)
            return;
        if (_editorHandler is not null)
            _editorVm.PropertyChanged -= _editorHandler;
        _editorVm = null;
        _editorHandler = null;
    }

    /// <summary>Установить контент напрямую (режим агента).</summary>
    public void SetContent(string title, string content)
    {
        DetachFromEditor();
        Title = title;
        ApplyMarkdownWithDiagrams(content ?? "", SettingsService.Load());
    }

    private void UpdateFromEditor()
    {
        if (_editorVm is null)
            return;
        Title = string.IsNullOrEmpty(_editorVm.CurrentFilePath) ? "Превью" : _editorVm.CurrentFilePath;
        var raw = _editorVm.EditorText ?? "";
        Markdown = raw;

        var snapshot = new CascadeIdeSettings
        {
            MarkdownKrokiEnabled = _editorVm.MarkdownKrokiEnabled,
            MarkdownKrokiBaseUrl = string.IsNullOrWhiteSpace(_editorVm.MarkdownKrokiBaseUrl)
                ? "https://kroki.io"
                : _editorVm.MarkdownKrokiBaseUrl.Trim()
        };
        ApplyMarkdownWithDiagrams(raw, snapshot);
    }

    private void ApplyMarkdownWithDiagrams(string raw, CascadeIdeSettings settings)
    {
        _diagramPreviewCts?.Cancel();
        if (!settings.MarkdownKrokiEnabled)
            return;

        _diagramPreviewCts = new CancellationTokenSource();
        var token = _diagramPreviewCts.Token;
        _ = ExpandDiagramsAsync(raw, settings, token);
    }

    private async Task ExpandDiagramsAsync(string raw, CascadeIdeSettings settings, CancellationToken token)
    {
        try
        {
            await Task.Delay(400, token).ConfigureAwait(false);
            var expanded = await MarkdownDiagramExpansion.ExpandAsync(raw, settings, token).ConfigureAwait(false);
            if (token.IsCancellationRequested)
                return;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!token.IsCancellationRequested)
                    Markdown = expanded;
            });
        }
        catch (OperationCanceledException)
        {
            // ожидаемо
        }
    }
}
