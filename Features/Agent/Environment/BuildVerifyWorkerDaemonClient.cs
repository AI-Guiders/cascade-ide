using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>JSON-lines клиент к <c>BuildVerifyWorker serve</c> (long-lived daemon).</summary>
public sealed class BuildVerifyWorkerDaemonClient : IAsyncDisposable
{
    private readonly string _workerDllPath;
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pending = new();
    private readonly object _processGate = new();
    private Process? _process;
    private StreamWriter? _stdin;
    private Task? _stdoutPump;
    private int _nextRequestId;
    private bool _disposed;

    public BuildVerifyWorkerDaemonClient(string workerDllPath)
    {
        _workerDllPath = Path.GetFullPath(workerDllPath);
    }

    public bool IsRunning
    {
        get
        {
            lock (_processGate)
                return _process is { HasExited: false };
        }
    }

    public async Task EnsureStartedAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsRunning)
            return;

        lock (_processGate)
        {
            if (_process is { HasExited: false })
                return;

            if (!File.Exists(_workerDllPath))
                throw new FileNotFoundException("BuildVerifyWorker assembly not found.", _workerDllPath);

            var psi = new ProcessStartInfo("dotnet", $"exec \"{_workerDllPath}\" serve")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            _process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start BuildVerifyWorker serve process.");

            _stdin = new StreamWriter(_process.StandardInput.BaseStream) { AutoFlush = true, NewLine = "\n" };
            _stdoutPump = Task.Run(() => PumpStdoutAsync(_process.StandardOutput, _process));
        }

        await PingAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<JsonElement> SendAsync(
        IReadOnlyDictionary<string, object?> payload,
        CancellationToken cancellationToken = default)
    {
        await EnsureStartedAsync(cancellationToken).ConfigureAwait(false);

        var id = Interlocked.Increment(ref _nextRequestId).ToString();
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(id, tcs))
            throw new InvalidOperationException("Failed to register IPC request.");

        var body = new Dictionary<string, object?>(payload) { ["id"] = id };
        var line = JsonSerializer.Serialize(body);

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_stdin is null || _process is { HasExited: true })
                throw new InvalidOperationException("BuildVerifyWorker daemon is not running.");

            await _stdin.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(30));
        try
        {
            return await tcs.Task.WaitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch
        {
            _pending.TryRemove(id, out _);
            throw;
        }
    }

    public async Task PingAsync(CancellationToken cancellationToken = default)
    {
        var resp = await SendAsync(new Dictionary<string, object?> { ["op"] = "ping" }, cancellationToken)
            .ConfigureAwait(false);
        if (!resp.TryGetProperty("ok", out var ok) || ok.ValueKind != JsonValueKind.True)
            throw new InvalidOperationException("BuildVerifyWorker daemon ping failed.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            if (IsRunning)
            {
                await SendAsync(
                        new Dictionary<string, object?> { ["op"] = "shutdown" },
                        CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }
        catch
        {
            //
        }

        lock (_processGate)
        {
            try
            {
                if (_process is { HasExited: false })
                    _process.Kill(entireProcessTree: true);
            }
            catch
            {
                //
            }

            _process?.Dispose();
            _process = null;
            _stdin = null;
        }

        FailAllPending("daemon_disposed");
        _writeGate.Dispose();
    }

    private async Task PumpStdoutAsync(StreamReader reader, Process process)
    {
        try
        {
            while (true)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                    break;

                if (line.Length == 0)
                    continue;

                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(line);
                }
                catch (JsonException)
                {
                    continue;
                }

                using (doc)
                {
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("id", out var idEl))
                        continue;

                    var id = idEl.GetString();
                    if (string.IsNullOrEmpty(id))
                        continue;

                    if (id == "0")
                        continue;

                    if (_pending.TryRemove(id, out var tcs))
                        tcs.TrySetResult(root.Clone());
                }
            }
        }
        finally
        {
            if (!process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    //
                }
            }

            FailAllPending("daemon_exited");
        }
    }

    private void FailAllPending(string reason)
    {
        foreach (var pair in _pending.ToArray())
        {
            if (_pending.TryRemove(pair.Key, out var tcs))
                tcs.TrySetException(new InvalidOperationException(reason));
        }
    }
}
