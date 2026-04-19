using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

#nullable enable

namespace CascadeIDE.Services.Lsp;

/// <summary>
/// Общий слой LSP по stdio: Content-Length framing, корреляция <c>id</c> запрос/ответ,
/// диспетчеризация <c>method</c> без <c>id</c> (нотификации). Процесс и stdin/stdout — снаружи.
/// </summary>
public sealed class LspStdioSession : IDisposable
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonDocument?>> _pending = new();
    private readonly Action<string, JsonElement> _onNotification;
    private Process? _process;
    private Stream? _stdIn;
    private CancellationTokenSource? _runCts;
    private Task? _readLoop;
    private int _nextRequestId = 1;
    private volatile bool _disposed;

    /// <param name="onNotification"><paramref name="method"/> LSP, <paramref name="params"/> — элемент params.</param>
    public LspStdioSession(Process process, Stream stdIn, Action<string, JsonElement> onNotification)
    {
        _process = process;
        _stdIn = stdIn;
        _onNotification = onNotification;
    }

    public Process? Process => _process;

    public Stream? StdIn => _stdIn;

    /// <summary>Следующий положительный id для <c>jsonrpc</c> request (потокобезопасно).</summary>
    public int AllocateRequestId() => Interlocked.Increment(ref _nextRequestId);

    public void StartReadLoop(CancellationToken parentCt)
    {
        if (_process is null || _stdIn is null)
            return;
        _runCts = CancellationTokenSource.CreateLinkedTokenSource(parentCt);
        var token = _runCts.Token;
        _ = Task.Run(() => DrainStdErrAsync(_process.StandardError, token), token);
        _readLoop = Task.Run(() => ReadLoopAsync(_process.StandardOutput.BaseStream, token), token);
    }

    public async Task<JsonDocument?> SendRequestAsync(JsonObject rpcRequest, int requestId, TimeSpan timeout, CancellationToken ct)
    {
        if (_stdIn is null || _disposed)
            return null;
        var tcs = new TaskCompletionSource<JsonDocument?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = tcs;
        try
        {
            var bytes = Encoding.UTF8.GetBytes(rpcRequest.ToJsonString());
            await LspStdioFraming.WriteMessageAsync(_stdIn, bytes, ct).ConfigureAwait(false);
            return await tcs.Task.WaitAsync(timeout, ct).ConfigureAwait(false);
        }
        catch
        {
            _pending.TryRemove(requestId, out _);
            return null;
        }
    }

    public async Task SendEnvelopeAsync(JsonObject rpcMessage, CancellationToken ct)
    {
        if (_stdIn is null || _disposed)
            return;
        var bytes = Encoding.UTF8.GetBytes(rpcMessage.ToJsonString());
        await LspStdioFraming.WriteMessageAsync(_stdIn, bytes, ct).ConfigureAwait(false);
    }

    public async Task SendRawUtf8Async(ReadOnlyMemory<byte> utf8Body, CancellationToken ct)
    {
        if (_stdIn is null || _disposed)
            return;
        await LspStdioFraming.WriteMessageAsync(_stdIn, utf8Body, ct).ConfigureAwait(false);
    }

    private async Task ReadLoopAsync(Stream stdout, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                JsonDocument? doc;
                try
                {
                    doc = await LspStdioFraming.ReadMessageAsync(stdout, ct).ConfigureAwait(false);
                }
                catch
                {
                    break;
                }

                if (doc is null)
                    break;

                using (doc)
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.Number)
                    {
                        var id = idEl.GetInt32();
                        if (root.TryGetProperty("result", out _))
                        {
                            if (_pending.TryRemove(id, out var tcs))
                            {
                                var copy = JsonDocument.Parse(root.GetRawText());
                                tcs.TrySetResult(copy);
                            }

                            continue;
                        }

                        if (root.TryGetProperty("error", out var errorEl))
                        {
                            if (_pending.TryRemove(id, out var tcs))
                                tcs.TrySetException(new InvalidOperationException(errorEl.GetRawText()));
                            continue;
                        }
                    }

                    if (root.TryGetProperty("method", out var methodEl))
                    {
                        var method = methodEl.GetString();
                        if (string.IsNullOrEmpty(method))
                            continue;
                        if (!root.TryGetProperty("params", out var p))
                            continue;
                        try
                        {
                            _onNotification(method, p);
                        }
                        catch
                        {
                            // не рвём read loop из-за обработчика
                        }
                    }
                }
            }
        }
        catch
        {
            // ignored
        }
        finally
        {
            foreach (var kv in _pending)
            {
                if (_pending.TryRemove(kv.Key, out var tcs))
                    tcs.TrySetCanceled(ct);
            }
        }
    }

    private static async Task DrainStdErrAsync(StreamReader err, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await err.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is null)
                    break;
                Debug.WriteLine("[lsp stderr] " + line);
            }
        }
        catch
        {
            // ignored
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        try
        {
            _runCts?.Cancel();
        }
        catch { }

        try
        {
            _stdIn?.Dispose();
        }
        catch { }

        _stdIn = null;

        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(2000);
            }
        }
        catch { }

        try
        {
            _process?.Dispose();
        }
        catch { }

        _process = null;
        _runCts?.Dispose();
        _runCts = null;
    }
}
