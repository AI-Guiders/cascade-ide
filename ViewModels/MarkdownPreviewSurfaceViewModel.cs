#nullable enable
using System.ComponentModel;
using Avalonia.Threading;
using CascadeIDE.Models;
using CascadeIDE.Services.MarkdownPreview;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Общий state/payload host для MFD page и отдельного окна preview.</summary>
public abstract partial class MarkdownPreviewSurfaceViewModel : ObservableObject, IDisposable
{
    private readonly MarkdownPreviewPayloadBuilder _payloadBuilder = new();
    private MainWindowViewModel? _editorVm;
    private PropertyChangedEventHandler? _editorHandler;
    private CancellationTokenSource? _refreshCts;

    [ObservableProperty]
    private string _title = "Markdown Preview";

    [ObservableProperty]
    private string _statusText = "Open a Markdown document to preview.";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private MarkdownPreviewPayload? _payload;

    public void AttachToEditor(MainWindowViewModel vm)
    {
        if (ReferenceEquals(_editorVm, vm))
        {
            RefreshFromEditor();
            return;
        }

        DetachFromEditor();
        _editorVm = vm;
        _editorHandler = (_, e) =>
        {
            if (e.PropertyName is nameof(MainWindowViewModel.CurrentFilePath)
                or nameof(MainWindowViewModel.EditorText)
                or nameof(MainWindowViewModel.MarkdownKrokiEnabled)
                or nameof(MainWindowViewModel.MarkdownKrokiBaseUrl))
            {
                RefreshFromEditor();
            }
        };
        vm.PropertyChanged += _editorHandler;
        RefreshFromEditor();
    }

    public void DetachFromEditor()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = null;
        if (_editorVm is not null && _editorHandler is not null)
            _editorVm.PropertyChanged -= _editorHandler;
        _editorVm = null;
        _editorHandler = null;
    }

    public void SetContent(string title, string content, string? sourcePath = null)
    {
        QueueRefresh(
            new MarkdownPreviewSource(title, content ?? "", sourcePath),
            SettingsService.Load(),
            emptyMessage: "Markdown content is empty.");
    }

    public void RefreshFromEditor()
    {
        if (_editorVm is null)
        {
            SetEmptyState("Markdown Preview", "Preview is not attached to an editor.");
            return;
        }

        if (!_editorVm.IsMarkdownFile)
        {
            SetEmptyState("Markdown Preview", "Open a Markdown document in the editor to preview it here.");
            return;
        }

        var sourcePath = _editorVm.CurrentFilePath;
        var title = string.IsNullOrWhiteSpace(sourcePath) ? "Markdown Preview" : sourcePath!;
        var source = new MarkdownPreviewSource(title, _editorVm.EditorText ?? "", sourcePath);
        QueueRefresh(source, BuildEditorSettingsSnapshot(_editorVm), emptyMessage: "Markdown document is empty.");
    }

    protected void SetEmptyState(string title, string message)
    {
        _refreshCts?.Cancel();
        Title = title;
        StatusText = message;
        ErrorMessage = "";
        Payload = null;
        IsBusy = false;
    }

    private void QueueRefresh(MarkdownPreviewSource source, CascadeIdeSettings settings, string emptyMessage)
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = new CancellationTokenSource();
        var token = _refreshCts.Token;

        Title = source.Title;
        ErrorMessage = "";
        IsBusy = true;
        StatusText = string.IsNullOrWhiteSpace(source.Markdown) ? emptyMessage : "Refreshing preview…";

        _ = BuildAndApplyAsync(source, settings, emptyMessage, token);
    }

    private async Task BuildAndApplyAsync(
        MarkdownPreviewSource source,
        CascadeIdeSettings settings,
        string emptyMessage,
        CancellationToken token)
    {
        try
        {
            var payload = await _payloadBuilder.BuildAsync(source, settings, token).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                Title = payload.Title;
                Payload = payload;
                ErrorMessage = payload.ErrorMessage ?? "";
                StatusText = string.IsNullOrWhiteSpace(payload.RenderMarkdown)
                    ? emptyMessage
                    : payload.Notices.Count > 0 ? string.Join(" | ", payload.Notices) : "";
                IsBusy = false;
            });
        }
        catch (OperationCanceledException)
        {
            // expected when text changes quickly
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                Title = source.Title;
                Payload = null;
                ErrorMessage = ex.Message;
                StatusText = "Markdown preview failed to refresh.";
                IsBusy = false;
            });
        }
    }

    private static CascadeIdeSettings BuildEditorSettingsSnapshot(MainWindowViewModel vm)
    {
        return new CascadeIdeSettings
        {
            Markdown = new MarkdownSettings
            {
                Diagrams = new MarkdownDiagramSettings
                {
                    Kroki = vm.MarkdownKrokiEnabled,
                    KrokiUrl = string.IsNullOrWhiteSpace(vm.MarkdownKrokiBaseUrl)
                        ? "https://kroki.io"
                        : vm.MarkdownKrokiBaseUrl.Trim()
                }
            }
        };
    }

    public void Dispose()
    {
        DetachFromEditor();
        GC.SuppressFinalize(this);
    }
}
