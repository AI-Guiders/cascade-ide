#nullable enable
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Threading;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using CascadeIDE.Services.Intercom;
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
    private int? _pendingScrollLine;
    private string? _pendingScrollFragment;

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

    public void SetContent(string title, string content, string? sourcePath = null, int? scrollToLine = null)
    {
        if (scrollToLine is > 0)
            _pendingScrollLine = scrollToLine;

        QueueRefresh(
            new MarkdownPreviewSource(title, content ?? "", sourcePath),
            SettingsService.Load(),
            emptyMessage: "Markdown content is empty.");
    }

    /// <summary>Единый обработчик ссылок preview: http, .md, #fragment, code-anchor.</summary>
    public void TryOpenPreviewLink(string linkUrl, MarkdownPreviewAnchorRegistry? anchors)
    {
        if (string.IsNullOrWhiteSpace(linkUrl))
            return;

        var trimmed = linkUrl.Trim();
        if (trimmed.StartsWith(MarkdownCodeAnchorPreviewExpander.UriScheme, StringComparison.OrdinalIgnoreCase))
        {
            TryOpenCodeAnchor(trimmed);
            return;
        }

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            TryOpenExternalUrl(trimmed);
            return;
        }

        if (trimmed.StartsWith('#'))
        {
            anchors?.ScrollToFragment(trimmed[1..]);
            return;
        }

        var (path, fragment) = MarkdownPreviewRenderContext.SplitUrl(trimmed);
        if (!string.IsNullOrWhiteSpace(path))
            TryOpenLinkedDocument(path);

        if (!string.IsNullOrWhiteSpace(fragment))
            anchors?.ScrollToFragment(fragment);
    }

    /// <summary>Открыть связанный markdown (ADR/KB) в этом же preview.</summary>
    public void TryOpenLinkedDocument(string linkUrl)
    {
        var ws = TryGetWorkspaceRoot();
        if (string.IsNullOrWhiteSpace(ws))
            return;

        var (path, fragment) = MarkdownPreviewRenderContext.SplitUrl(linkUrl);
        if (string.IsNullOrWhiteSpace(path))
        {
            if (!string.IsNullOrWhiteSpace(fragment))
                _pendingScrollFragment = fragment;
            return;
        }

        var ctx = new MarkdownPreviewRenderContext(Payload?.SourcePath, ws);
        var target = ctx.ResolveNavigateTarget(path);
        if (string.IsNullOrWhiteSpace(target))
            return;

        if (!string.IsNullOrWhiteSpace(fragment))
            _pendingScrollFragment = fragment;

        if (!WorkspaceMarkdownPreviewOpener.TryOpenRepoDocument(
                ws,
                target,
                (title, content, source) => SetContent(title, content, source),
                out _))
            return;
    }

    internal int? ConsumePendingScrollLine()
    {
        var line = _pendingScrollLine;
        _pendingScrollLine = null;
        return line;
    }

    internal string? ConsumePendingScrollFragment()
    {
        var fragment = _pendingScrollFragment;
        _pendingScrollFragment = null;
        return fragment;
    }

    internal void TryOpenCodeAnchor(string codeAnchorUrl)
    {
        if (_editorVm is null)
            return;

        var inner = Uri.UnescapeDataString(
            codeAnchorUrl[MarkdownCodeAnchorPreviewExpander.UriScheme.Length..]);
        if (!BracketCodeReferenceParser.TryParse(inner, out var reference, out _))
            return;

        var ws = TryGetWorkspaceRoot();
        if (!BracketCodeReferenceParser.TryToAttachmentAnchor(
                reference,
                _editorVm.CurrentFilePath,
                ws,
                _editorVm.Workspace.SolutionPath,
                indexDirectoryRelative: null,
                out var anchor,
                out _))
        {
            return;
        }

        var absolute = ResolveAnchorAbsolutePath(anchor.File, ws);
        if (string.IsNullOrWhiteSpace(absolute))
            return;

        var line = anchor.LineStart is > 0 ? anchor.LineStart.Value : 1;
        var endLine = anchor.LineEnd is > 0 ? anchor.LineEnd.Value : line;
        _editorVm.IdeMcp.GoToPosition(absolute, line, 1, endLine, null);
    }

    internal void TryOpenExternalUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // ignore — preview must not crash on bad URL
        }
    }

    private static string? ResolveAnchorAbsolutePath(string? relativeFile, string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(relativeFile))
            return null;

        var normalized = relativeFile.Replace('\\', '/').Trim();
        if (Path.IsPathRooted(normalized))
            return normalized;

        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return null;

        var candidate = Path.GetFullPath(Path.Combine(workspaceRoot, normalized.Replace('/', Path.DirectorySeparatorChar)));
        return File.Exists(candidate) ? candidate : candidate;
    }

    internal string? TryGetWorkspaceRoot()
    {
        if (_editorVm is null)
            return null;

        return WorkspaceDirectoryFromSolutionPath.Resolve(_editorVm.Workspace.SolutionPath ?? "");
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
