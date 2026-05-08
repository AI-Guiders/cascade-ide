using System.Collections.Concurrent;
using System.Threading.Channels;
using CascadeIDE.Cockpit.DataBus;
using HybridCodebaseIndex.Core;

namespace CascadeIDE.Features.HybridIndex.Application;

/// <summary>Оркестратор in-proc Hybrid Codebase Index (ADR 0106): watcher + debounce + публикация статуса в DataBus.</summary>
public sealed class HybridIndexOrchestrator : IDisposable
{
    private sealed record WatchKey(string WorkspaceRoot, string? SolutionPath)
    {
        public override string ToString() =>
            string.IsNullOrWhiteSpace(SolutionPath) ? WorkspaceRoot : $"{WorkspaceRoot} | {SolutionPath}";
    }

    private sealed class WatcherState : IDisposable
    {
        private readonly CodebaseIndexService _service;
        private readonly IDataBus _dataBus;
        private readonly string _workspaceRoot;
        private readonly string? _solutionPath;
        private readonly int _debounceMs;

        private readonly Channel<int> _poke;
        private readonly CancellationTokenSource _cts;
        private readonly Task _loop;
        private readonly FileSystemWatcher _fsw;

        public WatcherState(CodebaseIndexService service, IDataBus dataBus, string workspaceRoot, string? solutionPath, int debounceMs)
        {
            _service = service;
            _dataBus = dataBus;
            _workspaceRoot = workspaceRoot;
            _solutionPath = solutionPath;
            _debounceMs = Math.Clamp(debounceMs, 50, 60_000);

            _cts = new CancellationTokenSource();
            _poke = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            });

            _fsw = new FileSystemWatcher(_workspaceRoot)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter =
                    NotifyFilters.FileName
                    | NotifyFilters.DirectoryName
                    | NotifyFilters.LastWrite
                    | NotifyFilters.Size
                    | NotifyFilters.CreationTime,
            };

            _fsw.Changed += OnAny;
            _fsw.Created += OnAny;
            _fsw.Deleted += OnAny;
            _fsw.Renamed += OnAny;
            _fsw.Error += OnError;

            _loop = Task.Run(LoopAsync, _cts.Token);
        }

        private void OnAny(object sender, FileSystemEventArgs e)
        {
            // Best-effort: ignore self-induced churn from index directory.
            // A false-positive poke is ok because reindex is incremental and debounced.
            if (e.FullPath?.IndexOf(".hybrid-codebase-index", StringComparison.OrdinalIgnoreCase) >= 0)
                return;
            _poke.Writer.TryWrite(0);
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            // FileSystemWatcher can drop events when buffer overflows; trigger a catch-up reindex.
            _poke.Writer.TryWrite(0);
        }

        private async Task LoopAsync()
        {
            var ct = _cts.Token;

            // Proactively publish initial status (best-effort).
            await PublishStatusAsync(CancellationToken.None).ConfigureAwait(false);

            var nextDelay = Task.Delay(Timeout.Infinite, ct);
            var hasPending = false;

            while (!ct.IsCancellationRequested)
            {
                var read = _poke.Reader.ReadAsync(ct).AsTask();
                var completed = await Task.WhenAny(read, nextDelay).ConfigureAwait(false);

                if (completed == read)
                {
                    hasPending = true;
                    _ = await read.ConfigureAwait(false);
                    nextDelay = Task.Delay(_debounceMs, ct);
                    continue;
                }

                if (!hasPending)
                {
                    nextDelay = Task.Delay(Timeout.Infinite, ct);
                    continue;
                }

                hasPending = false;
                nextDelay = Task.Delay(Timeout.Infinite, ct);

                try
                {
                    await _service.FullReindexAsync(_workspaceRoot, _solutionPath, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch
                {
                    // best-effort background loop; status tool surfaces last error
                }

                await PublishStatusAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        private async Task PublishStatusAsync(CancellationToken ct)
        {
            try
            {
                var st = await _service.GetStatusAsync(_workspaceRoot, _solutionPath, ct).ConfigureAwait(false);
                _dataBus.Publish(new HybridIndexStateChanged(
                    WorkspaceRoot: st.WorkspaceRootNormalized ?? _workspaceRoot,
                    SolutionPath: _solutionPath,
                    DatabasePath: st.DatabasePath,
                    DocumentCount: st.DocumentCount,
                    IndexedAtIso: st.IndexedAtIso,
                    LastError: st.LastReindexError,
                    LastErrorAtIso: st.LastReindexErrorAtIso));
            }
            catch
            {
                // ignore
            }
        }

        public void Poke() => _poke.Writer.TryWrite(0);

        public void Dispose()
        {
            _fsw.EnableRaisingEvents = false;
            _fsw.Changed -= OnAny;
            _fsw.Created -= OnAny;
            _fsw.Deleted -= OnAny;
            _fsw.Renamed -= OnAny;
            _fsw.Error -= OnError;
            _fsw.Dispose();

            _cts.Cancel();
            _poke.Writer.TryComplete();
            try { _loop.GetAwaiter().GetResult(); } catch { /* ignore */ }
            _cts.Dispose();
        }
    }

    private readonly CodebaseIndexService _service;
    private readonly IDataBus _dataBus;
    private readonly ConcurrentDictionary<WatchKey, WatcherState> _watchers = new();

    public HybridIndexOrchestrator(IDataBus dataBus, string indexDirectoryRelative)
    {
        _dataBus = dataBus;
        _service = new CodebaseIndexService(indexDirectoryRelative: indexDirectoryRelative);
    }

    public void SetEnabled(string workspaceRoot, string? solutionPath, bool enabled, int debounceMs = 750)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return;

        var root = Path.GetFullPath(workspaceRoot.TrimEnd(Path.DirectorySeparatorChar));
        var key = new WatchKey(root, string.IsNullOrWhiteSpace(solutionPath) ? null : solutionPath.Trim());

        if (!enabled)
        {
            if (_watchers.TryRemove(key, out var st))
                st.Dispose();
            return;
        }

        _watchers.AddOrUpdate(
            key,
            static (k, arg) => new WatcherState(arg.service, arg.bus, arg.root, arg.solutionPath, arg.debounceMs),
            static (k, existing, arg) =>
            {
                existing.Dispose();
                return new WatcherState(arg.service, arg.bus, arg.root, arg.solutionPath, arg.debounceMs);
            },
            (service: _service, bus: _dataBus, root, solutionPath: key.SolutionPath, debounceMs));
    }

    public void Poke(string workspaceRoot, string? solutionPath)
    {
        var root = Path.GetFullPath((workspaceRoot ?? "").TrimEnd(Path.DirectorySeparatorChar));
        var key = new WatchKey(root, string.IsNullOrWhiteSpace(solutionPath) ? null : solutionPath.Trim());
        if (_watchers.TryGetValue(key, out var st))
            st.Poke();
    }

    public void Dispose()
    {
        foreach (var kv in _watchers)
        {
            if (_watchers.TryRemove(kv.Key, out var st))
                st.Dispose();
        }
    }
}

