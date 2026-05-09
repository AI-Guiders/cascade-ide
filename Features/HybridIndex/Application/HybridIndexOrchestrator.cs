using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Channels;
using CascadeIDE.Cockpit.ComputingUnits.HybridIndex;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Contracts;
using HybridCodebaseIndex.Core;

namespace CascadeIDE.Features.HybridIndex.Application;

/// <summary>Оркестратор in-proc Hybrid Codebase Index (ADR 0106): watcher + debounce + публикация статуса в DataBus.</summary>
[ApplicationOrchestrator("hybrid-index-in-proc")]
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
        private readonly string _indexDirRelativeMarker;

        private readonly Channel<int> _poke;
        private readonly CancellationTokenSource _cts;
        private readonly Task _loop;
        private readonly FileSystemWatcher _fsw;

        public WatcherState(
            CodebaseIndexService service,
            IDataBus dataBus,
            string workspaceRoot,
            string? solutionPath,
            int debounceMs,
            string indexDirectoryRelativeForNoiseFilter)
        {
            _service = service;
            _dataBus = dataBus;
            _workspaceRoot = workspaceRoot;
            _solutionPath = solutionPath;
            _debounceMs = Math.Clamp(debounceMs, 50, 60_000);
            _indexDirRelativeMarker = (indexDirectoryRelativeForNoiseFilter ?? "").Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

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
            // Best-effort: ignore self-induced churn from index directory (matches configured `index_dir`).
            // A false-positive poke is ok because reindex is incremental and debounced.
            if (IsIndexArtifactPath(e.FullPath, _indexDirRelativeMarker))
                return;
            _poke.Writer.TryWrite(0);
        }

        private static bool IsIndexArtifactPath(string? fullPath, string indexDirRelativeMarker)
        {
            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(indexDirRelativeMarker))
                return false;
            return fullPath.Contains(indexDirRelativeMarker, StringComparison.OrdinalIgnoreCase);
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
                _dataBus.Publish(HybridIndexStateChangedUnit.FromCoreStatus(st, _workspaceRoot, _solutionPath));
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

    private CodebaseIndexService _service;
    private readonly IDataBus _dataBus;
    private string _indexDirectoryRelative;
    private readonly ConcurrentDictionary<WatchKey, WatcherState> _watchers = new();

    public HybridIndexOrchestrator(IDataBus dataBus, string indexDirectoryRelative)
    {
        _dataBus = dataBus;
        _indexDirectoryRelative = HybridIndexIndexDirectoryRelative.ResolveOrDefault(indexDirectoryRelative);
        _service = new CodebaseIndexService(indexDirectoryRelative: _indexDirectoryRelative);
    }

    /// <summary>
    /// Recreates the in-proc <see cref="CodebaseIndexService"/> and drops active watchers.
    /// Callers should re-apply <see cref="SetEnabled"/> for the current workspace/solution.
    /// </summary>
    public void SetIndexDirectoryRelative(string indexDirectoryRelative)
    {
        var normalized = HybridIndexIndexDirectoryRelative.ResolveOrDefault(indexDirectoryRelative);
        if (string.Equals(_indexDirectoryRelative, normalized, StringComparison.Ordinal))
            return;

        foreach (var key in _watchers.Keys.ToArray())
        {
            if (_watchers.TryRemove(key, out var st))
                st.Dispose();
        }

        _indexDirectoryRelative = normalized;
        _service = new CodebaseIndexService(indexDirectoryRelative: normalized);
    }

    public void SetEnabled(string workspaceRoot, string? solutionPath, bool enabled, int debounceMs = 750)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return;

        var root = CanonicalFilePath.Normalize(workspaceRoot.TrimEnd(Path.DirectorySeparatorChar));
        var key = new WatchKey(root, string.IsNullOrWhiteSpace(solutionPath) ? null : solutionPath.Trim());

        if (!enabled)
        {
            if (_watchers.TryRemove(key, out var st))
                st.Dispose();
            return;
        }

        _watchers.AddOrUpdate(
            key,
            static (k, arg) => new WatcherState(
                arg.service,
                arg.bus,
                arg.root,
                arg.solutionPath,
                arg.debounceMs,
                arg.indexDir),
            static (k, existing, arg) =>
            {
                existing.Dispose();
                return new WatcherState(
                    arg.service,
                    arg.bus,
                    arg.root,
                    arg.solutionPath,
                    arg.debounceMs,
                    arg.indexDir);
            },
            (service: _service, bus: _dataBus, root, solutionPath: key.SolutionPath, debounceMs, indexDir: _indexDirectoryRelative));
    }

    public void Poke(string workspaceRoot, string? solutionPath)
    {
        var root = CanonicalFilePath.Normalize((workspaceRoot ?? "").TrimEnd(Path.DirectorySeparatorChar));
        var key = new WatchKey(root, string.IsNullOrWhiteSpace(solutionPath) ? null : solutionPath.Trim());
        if (_watchers.TryGetValue(key, out var st))
            st.Poke();
    }

    public Task<IndexStatus> GetIndexStatusAsync(string workspaceRoot, string? solutionPath, CancellationToken cancellationToken = default) =>
        _service.GetStatusAsync(workspaceRoot, solutionPath, cancellationToken);

    public Task<(SearchResponse Response, string? Error)> SearchHybridAsync(
        string workspaceRoot,
        string? solutionPath,
        string query,
        int topN,
        string? pathPrefix,
        IReadOnlyList<string>? excludePathPrefixes,
        IReadOnlyList<string>? extensions,
        bool semantic,
        double alpha,
        double beta,
        int vecTopK,
        CancellationToken cancellationToken = default) =>
        _service.SearchHybridAsync(workspaceRoot, solutionPath, query, topN, pathPrefix, excludePathPrefixes, extensions, semantic, alpha, beta, vecTopK, cancellationToken);

    public Task<ExplainHitResponse> ExplainHitAsync(string workspaceRoot, string? solutionPath, long hitId, CancellationToken cancellationToken = default) =>
        _service.ExplainHitAsync(workspaceRoot, solutionPath, hitId, cancellationToken);

    /// <summary>Инкрементальный или полный reindex через Core; затем снимок в DataBus (страница HIS).</summary>
    public async Task<ReindexSummary> RunReindexWithPublishAsync(
        string workspaceRoot,
        string? solutionPath,
        bool fullRebuild,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            throw new ArgumentException("workspace_root required", nameof(workspaceRoot));

        var root = CanonicalFilePath.Normalize(workspaceRoot.TrimEnd(Path.DirectorySeparatorChar));
        var sln = string.IsNullOrWhiteSpace(solutionPath) ? null : solutionPath.Trim();
        ReindexSummary summary = fullRebuild
            ? await _service.FullRebuildAsync(root, sln, cancellationToken).ConfigureAwait(false)
            : await _service.FullReindexAsync(root, sln, cancellationToken).ConfigureAwait(false);
        await PublishHybridIndexSnapshotAsync(root, sln, cancellationToken).ConfigureAwait(false);
        return summary;
    }

    private async Task PublishHybridIndexSnapshotAsync(string rootNormalized, string? solutionPathTrimmedOrNull, CancellationToken cancellationToken)
    {
        try
        {
            var st = await _service.GetStatusAsync(rootNormalized, solutionPathTrimmedOrNull, cancellationToken).ConfigureAwait(false);
            _dataBus.Publish(HybridIndexStateChangedUnit.FromCoreStatus(st, rootNormalized, solutionPathTrimmedOrNull));
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// Full reindex outside the watcher loop (например watcher выключен или master switch HCI off).
    /// Публикует <see cref="HybridIndexStateChanged"/> в DataBus по завершении.
    /// </summary>
    public async Task RunFullReindexAndPublishStatusAsync(
        string workspaceRoot,
        string? solutionPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return;

        var root = CanonicalFilePath.Normalize(workspaceRoot.TrimEnd(Path.DirectorySeparatorChar));
        var sln = string.IsNullOrWhiteSpace(solutionPath) ? null : solutionPath.Trim();
        try
        {
            await _service.FullReindexAsync(root, sln, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch
        {
            // surfaced via GetStatusAsync below
        }

        await PublishHybridIndexSnapshotAsync(root, sln, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        foreach (var key in _watchers.Keys.ToArray())
        {
            if (_watchers.TryRemove(key, out var st))
                st.Dispose();
        }
    }
}

