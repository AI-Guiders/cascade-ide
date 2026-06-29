namespace CascadeIDE.Features.Agent.Harness;

/// <summary>Debounce auto-verify after .cs writes (ADR 0166 P0.4).</summary>
public sealed class AgentVerifyCoalescer : IDisposable
{
    private readonly object _gate = new();
    private readonly Action _fire;
    private readonly int _windowMs;
    private CancellationTokenSource? _pending;
    private bool _disposed;

    public AgentVerifyCoalescer(int coalesceWindowMs, Action fire)
    {
        _windowMs = Math.Max(200, coalesceWindowMs);
        _fire = fire;
    }

    public void Schedule()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _pending?.Cancel();
            _pending?.Dispose();
            _pending = new CancellationTokenSource();
            var token = _pending.Token;
            _ = RunDelayedAsync(token);
        }
    }

    private async Task RunDelayedAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(_windowMs, token).ConfigureAwait(false);
            if (!token.IsCancellationRequested)
                _fire();
        }
        catch (OperationCanceledException)
        {
            // superseded
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            _pending?.Cancel();
            _pending?.Dispose();
            _pending = null;
        }
    }
}
