using System.Threading.Channels;

namespace CascadeIDE.Features.Editor.Application;

/// <summary>
/// Hi-freq → bounded channel (capacity 1, drop-oldest) → минимальный интервал между
/// срабатываниями → UI thread. ADR 0103, не <see cref="IDataBus"/>.
/// </summary>
public sealed class EditorStabilizedInputThrottler : IAsyncDisposable
{
    private readonly IUiScheduler _ui;
    private readonly TimeSpan _minInterval;
    private readonly Channel<EditorInputDelta> _channel;
    private Task? _loop;
    private CancellationTokenSource? _runCts;

    public EditorStabilizedInputThrottler(IUiScheduler ui, TimeSpan minInterval)
    {
        _ui = ui;
        _minInterval = minInterval;
        _channel = Channel.CreateBounded<EditorInputDelta>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true,
        });
    }

    public bool TryPost(EditorInputDelta delta) =>
        _channel.Writer.TryWrite(delta);

    public void Start(Action<EditorInputDelta> onStabilized, CancellationToken appCancellation)
    {
        if (_loop is not null)
            return;

        _runCts = CancellationTokenSource.CreateLinkedTokenSource(appCancellation);
        var token = _runCts.Token;
        var reader = _channel.Reader;

        _loop = Task.Run(
            async () =>
            {
                var lastOut = DateTime.MinValue;
                while (!token.IsCancellationRequested)
                {
                    EditorInputDelta d;
                    try
                    {
                        d = await reader.ReadAsync(token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (ChannelClosedException)
                    {
                        break;
                    }

                    var now = DateTime.UtcNow;
                    var since = now - lastOut;
                    if (since < _minInterval && _minInterval > TimeSpan.Zero)
                    {
                        var wait = _minInterval - since;
                        try
                        {
                            await Task.Delay(wait, token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }

                    var copy = d;
                    _ui.Post(() =>
                    {
                        if (!token.IsCancellationRequested)
                            onStabilized(copy);
                    });
                    lastOut = DateTime.UtcNow;
                }
            },
            CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        _runCts?.Cancel();
        _channel.Writer.TryComplete();
        if (_loop is not null)
        {
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch
            {
                // loop cancelled
            }
        }
        _runCts?.Dispose();
        _runCts = null;
        _loop = null;
    }
}
