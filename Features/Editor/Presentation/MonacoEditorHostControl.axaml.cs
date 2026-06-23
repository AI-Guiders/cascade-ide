using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Threading;
using CascadeIDE.Features.Editor.Application.Monaco;

namespace CascadeIDE.Features.Editor.Presentation;

/// <summary>Monaco Editor in WebView2 (ADR 0162 M0–M2).</summary>
public partial class MonacoEditorHostControl : UserControl
{
    private NativeWebView? _webView;
    private bool _ready;
    private bool _navigateRequested;
    private int _hostVersion;

    public MonacoEditorSessionState Session { get; } = new();

    public event EventHandler? Ready;
    public event EventHandler<CideEditorInboundMessage>? Inbound;

    public bool IsReady => _ready;

    public MonacoEditorHostControl()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        EnsureWebView();
    }

    private void EnsureWebView()
    {
        _webView ??= this.FindControl<NativeWebView>("WebView");
        if (_webView is null)
            return;

        _webView.AdapterCreated -= OnAdapterCreated;
        _webView.AdapterCreated += OnAdapterCreated;
        _webView.NavigationCompleted -= OnNavigationCompleted;
        _webView.NavigationCompleted += OnNavigationCompleted;
    }

    private void OnAdapterCreated(object? sender, EventArgs e)
    {
        if (_webView is null || _navigateRequested)
            return;

        if (!MonacoEditorWebViewBootstrap.TryMapVirtualHost(_webView))
        {
            System.Diagnostics.Debug.WriteLine("Monaco: virtual host mapping failed; falling back to file URI.");
            _webView.Navigate(MonacoEditorAssetLocator.GetFileIndexUri());
        }
        else
        {
            _webView.Navigate(MonacoEditorAssetLocator.GetIndexUri());
        }

        _navigateRequested = true;
    }

    private void OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            _ready = false;
            System.Diagnostics.Debug.WriteLine("Monaco navigation failed.");
        }
    }

    private void OnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        var msg = CideEditorBridgeJson.TryParseInbound(e.Body);
        if (msg is null)
            return;

        if (string.Equals(msg.Type, CideEditorBridgeTypes.Ready, StringComparison.Ordinal))
        {
            if (!string.IsNullOrEmpty(msg.Error))
            {
                _ready = false;
                System.Diagnostics.Debug.WriteLine("Monaco host error: " + msg.Error);
                return;
            }

            _ready = true;
            Dispatcher.UIThread.Post(() => Ready?.Invoke(this, EventArgs.Empty));
            return;
        }

        Session.ApplyInbound(msg);
        Inbound?.Invoke(this, msg);
    }

    public async Task PushSetModelAsync(string filePath, string text, CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        _hostVersion += 1;
        Session.Seed(text, _hostVersion);
        var payload = new CideEditorSetModelMessage(
            Uri: "file://" + filePath.Replace('\\', '/'),
            LanguageId: CideEditorLanguageIds.FromFilePath(filePath),
            Text: text,
            Version: _hostVersion);
        await DispatchAsync(CideEditorBridgeTypes.SetModel, payload, cancellationToken).ConfigureAwait(true);
    }

    public async Task PushApplyEditsAsync(
        IReadOnlyList<CideEditorApplyEdit> edits,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        var payload = new CideEditorApplyEditsMessage(edits, _hostVersion);
        await DispatchAsync(CideEditorBridgeTypes.ApplyEdits, payload, cancellationToken).ConfigureAwait(true);
    }

    public async Task PushDecorationsAsync(
        string setId,
        IReadOnlyList<CideEditorDecoration> decorations,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        var payload = new CideEditorSetDecorationsMessage(setId, decorations);
        await DispatchAsync(CideEditorBridgeTypes.SetDecorations, payload, cancellationToken).ConfigureAwait(true);
    }

    public async Task PushStickyScrollAsync(string? label, CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        await DispatchAsync(
            CideEditorBridgeTypes.SetStickyScroll,
            new CideEditorStickyScrollMessage(label),
            cancellationToken).ConfigureAwait(true);
    }

    public async Task PushGutterGlyphsAsync(
        IReadOnlyList<CideEditorGutterGlyph> glyphs,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        await DispatchAsync(
            CideEditorBridgeTypes.SetGutterGlyphs,
            new CideEditorSetGutterGlyphsMessage(glyphs),
            cancellationToken).ConfigureAwait(true);
    }

    public async Task PushIntelligenceEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        await DispatchAsync(
            CideEditorBridgeTypes.SetIntelligence,
            new CideEditorSetIntelligenceMessage(enabled),
            cancellationToken).ConfigureAwait(true);
    }

    public async Task PushRevealRangeAsync(
        int startLine,
        int endLine,
        int? column = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        await DispatchAsync(
            CideEditorBridgeTypes.RevealRange,
            new CideEditorRevealRangeMessage(startLine, endLine, column),
            cancellationToken).ConfigureAwait(true);
    }

    public async Task PushSetSelectionAsync(
        int selectionStart,
        int selectionLength,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        await DispatchAsync(
            CideEditorBridgeTypes.SetSelectionByOffset,
            new CideEditorSetSelectionMessage(selectionStart, selectionLength),
            cancellationToken).ConfigureAwait(true);
    }

    public async Task PushAgentRevealAsync(
        int startLine,
        int endLine,
        bool persistent,
        int? durationMs = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        await DispatchAsync(
            CideEditorBridgeTypes.SetAgentReveal,
            new CideEditorSetAgentRevealMessage(startLine, endLine, persistent, durationMs),
            cancellationToken).ConfigureAwait(true);
    }

    public async Task PushEpochDimAsync(bool dimmed, CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        await DispatchAsync(
            CideEditorBridgeTypes.SetEpochDim,
            new CideEditorSetEpochDimMessage(dimmed),
            cancellationToken).ConfigureAwait(true);
    }

    public async Task PushClearAgentRevealAsync(CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        await DispatchAsync(CideEditorBusManifest.Editor.ClearAgentReveal, new { }, cancellationToken).ConfigureAwait(true);
    }

    public async Task PushThemeAsync(bool isDark, CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        var themeId = MonacoEditorThemeMapper.ResolveThemeId(isDark);
        await DispatchAsync(
            CideEditorBusManifest.Editor.SetTheme,
            new
            {
                theme = themeId,
                defineTheme = new
                {
                    id = MonacoEditorThemeMapper.CascadeDarkThemeId,
                    data = MonacoEditorThemeMapper.BuildDefineThemePayload(isDark: true),
                },
            },
            cancellationToken).ConfigureAwait(true);
    }

    public Task PushCapabilityCompletionResultAsync(
        int requestId,
        IReadOnlyList<CideEditorCompletionItem> items,
        CancellationToken cancellationToken = default) =>
        PushCompletionResultAsync(requestId, items, cancellationToken);

    public Task PushCapabilityHoverResultAsync(
        int requestId,
        string? markdown,
        CancellationToken cancellationToken = default) =>
        PushHoverResultAsync(requestId, markdown, cancellationToken);

    public Task PushCapabilitySignatureResultAsync(
        int requestId,
        string? signature,
        CancellationToken cancellationToken = default) =>
        PushSignatureResultAsync(requestId, signature, cancellationToken);

    public async Task PushCapabilityDefinitionResultAsync(
        int requestId,
        CideEditorDefinitionLocation? location,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        await DispatchAsync(
            CideEditorBusManifest.Capabilities.DefinitionResult,
            new CideEditorDefinitionResultMessage(requestId, location),
            cancellationToken).ConfigureAwait(true);
    }

    public async Task PushCompletionResultAsync(
        int requestId,
        IReadOnlyList<CideEditorCompletionItem> items,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        await DispatchAsync(
            CideEditorBusManifest.Capabilities.CompletionResult,
            new CideEditorCompletionResultMessage(requestId, items),
            cancellationToken).ConfigureAwait(true);
    }

    public async Task PushHoverResultAsync(
        int requestId,
        string? markdown,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        await DispatchAsync(
            CideEditorBusManifest.Capabilities.HoverResult,
            new CideEditorHoverResultMessage(requestId, markdown),
            cancellationToken).ConfigureAwait(true);
    }

    public async Task PushSignatureResultAsync(
        int requestId,
        string? signature,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(true);
        await DispatchAsync(
            CideEditorBusManifest.Capabilities.SignatureResult,
            new CideEditorSignatureResultMessage(requestId, signature),
            cancellationToken).ConfigureAwait(true);
    }

    private async Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        EnsureWebView();
        if (_webView is not null && !_navigateRequested)
        {
            if (MonacoEditorWebViewBootstrap.TryMapVirtualHost(_webView))
                _webView.Navigate(MonacoEditorAssetLocator.GetIndexUri());
            else
                _webView.Navigate(MonacoEditorAssetLocator.GetFileIndexUri());
            _navigateRequested = true;
        }

        if (_ready)
            return;

        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!_ready && DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(50, cancellationToken).ConfigureAwait(true);
        }

        if (!_ready)
            throw new InvalidOperationException("Monaco editor host did not become ready.");
    }

    private async Task DispatchAsync(string type, object payload, CancellationToken cancellationToken)
    {
        _webView ??= this.FindControl<NativeWebView>("WebView");
        if (_webView is null)
            return;

        var json = CideEditorBridgeJson.WrapOutbound(type, payload);
        var script = "window.cideEditorHost.dispatchFromHost(JSON.parse("
                     + JsonSerializer.Serialize(json, CideEditorBridgeJson.Options)
                     + "));";
        await _webView.InvokeScript(script).ConfigureAwait(true);
    }
}
