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
        _webView ??= this.FindControl<NativeWebView>("WebView");
        if (_webView is null)
            return;
        if (_webView.Source is null)
            _webView.Navigate(MonacoEditorAssetLocator.GetIndexUri());
    }

    private void OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
            _ready = false;
    }

    private void OnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        var msg = CideEditorBridgeJson.TryParseInbound(e.Body);
        if (msg is null)
            return;

        if (string.Equals(msg.Type, CideEditorBridgeTypes.Ready, StringComparison.Ordinal))
        {
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

    private async Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
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
