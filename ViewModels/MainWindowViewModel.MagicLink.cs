#nullable enable

using CascadeIDE.Features.MagicLink;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>Magic link: pipe-сервер single-instance и обработка URI.</summary>
public partial class MainWindowViewModel
{
    private readonly object _magicLinkGate = new();
    private readonly Queue<string> _pendingMagicLinks = new();
    private CancellationTokenSource? _magicLinkCts;

    internal void StartMagicLinkListener()
    {
        _magicLinkCts?.Cancel();
        _magicLinkCts?.Dispose();
        _magicLinkCts = new CancellationTokenSource();
        var token = _magicLinkCts.Token;

        CideMagicLinkSingleInstance.RunPipeServerLoop(
            uri => UiScheduler.Default.InvokeAsync(() => ProcessMagicLinkAsync(uri)),
            token);
    }

    internal void StopMagicLinkListener()
    {
        _magicLinkCts?.Cancel();
        _magicLinkCts?.Dispose();
        _magicLinkCts = null;
    }

    public void EnqueueMagicLink(string uri)
    {
        lock (_magicLinkGate)
        {
            _pendingMagicLinks.Enqueue(uri);
        }
    }

    internal async Task FlushPendingMagicLinksAsync()
    {
        while (true)
        {
            string? uri;
            lock (_magicLinkGate)
            {
                if (_pendingMagicLinks.Count == 0)
                    return;
                uri = _pendingMagicLinks.Dequeue();
            }

            await ProcessMagicLinkAsync(uri).ConfigureAwait(false);
        }
    }

    private async Task ProcessMagicLinkAsync(string uri)
    {
        if (!CideMagicLinkUri.TryParse(uri, out var request, out _))
            return;

        await CideMagicLinkExecutor.TryExecuteAsync(this, request).ConfigureAwait(false);
    }
}
