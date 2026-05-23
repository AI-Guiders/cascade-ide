#nullable enable

using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.Chat;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Models;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.SolutionWarmup.Application;

/// <summary>Фоновый прогрев при смене solution (ADR 0141).</summary>
public sealed class SolutionWarmupOrchestrator : IDisposable
{
    private readonly IDataBus _dataBus;
    private readonly SolutionWarmupHostCallbacks _host;
    private readonly object _gate = new();
    private CancellationTokenSource? _runCts;
    private int _generation;
    private SolutionWarmupScope _currentScope;

    public SolutionWarmupOrchestrator(IDataBus dataBus, SolutionWarmupHostCallbacks host)
    {
        _dataBus = dataBus;
        _host = host;
    }

    public void OnSolutionScopeChanged(string workspaceRoot, string? solutionPath)
    {
        var scope = new SolutionWarmupScope(
            workspaceRoot.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            string.IsNullOrWhiteSpace(solutionPath) ? null : solutionPath.Trim());

        lock (_gate)
        {
            cancelLocked(SolutionWarmupLifecycle.Cancelled, "scope_changed");
            _currentScope = scope;
            if (scope.IsEmpty)
            {
                publish(scope, SolutionWarmupLifecycle.Idle, null);
                return;
            }

            var settings = _host.GetWarmupSettings();
            if (!settings.Enabled)
            {
                publish(scope, SolutionWarmupLifecycle.Idle, "disabled");
                return;
            }

            _generation++;
            var gen = _generation;
            _runCts = new CancellationTokenSource();
            var token = _runCts.Token;
            publish(scope, SolutionWarmupLifecycle.Running, null);
            _ = Task.Run(() => runPipelineAsync(scope, gen, token), token);
        }
    }

    private async Task runPipelineAsync(SolutionWarmupScope scope, int generation, CancellationToken cancellationToken)
    {
        var settings = _host.GetWarmupSettings();
        var hybrid = _host.GetHybridIndexSettings();
        var indexDir = HybridIndexIndexDirectoryRelative.ResolveOrDefault(hybrid.IndexDir);
        var partial = false;

        try
        {
            if (settings.WarmActiveFileOnSolutionOpen)
            {
                await runFileJobAsync(
                        () => warmBracketActiveFile(scope),
                        cancellationToken)
                    .ConfigureAwait(false);
                throwIfStale(scope, generation, cancellationToken);
            }

            if (hybrid.Enabled && hybrid.AutoReindexOnSolutionOpen)
            {
                var hciOk = await waitForHybridIndexAsync(scope, cancellationToken).ConfigureAwait(false);
                throwIfStale(scope, generation, cancellationToken);
                if (!hciOk)
                    partial = true;
            }
            else if (!hybrid.Enabled)
            {
                IntercomSymbolLineIndexCoordinator.ScheduleRebuildAfterHybridIndex(
                    scope.WorkspaceRoot,
                    scope.SolutionPath,
                    indexDir);
            }

            if (settings.WarmFeedAnchorsAfterSymbolSidecar)
            {
                postFeedAnchors();
                throwIfStale(scope, generation, cancellationToken);
            }

            if (settings.WarmOpenDocuments || settings.WarmRecentCsFiles)
            {
                var paths = collectOpenCsPaths(settings);
                await warmFilesParallelAsync(scope, paths, settings, cancellationToken).ConfigureAwait(false);
                throwIfStale(scope, generation, cancellationToken);
            }

            publishIfCurrent(
                scope,
                generation,
                partial ? SolutionWarmupLifecycle.Partial : SolutionWarmupLifecycle.Ready,
                partial ? "partial" : null);
        }
        catch (OperationCanceledException)
        {
            publishIfCurrent(scope, generation, SolutionWarmupLifecycle.Cancelled, "cancelled");
        }
        catch (Exception ex)
        {
            publishIfCurrent(scope, generation, SolutionWarmupLifecycle.Partial, ex.Message);
        }
    }

    private void warmBracketActiveFile(SolutionWarmupScope scope)
    {
        var active = _host.GetActiveCsFilePath();
        if (string.IsNullOrWhiteSpace(active))
            return;
        BracketMemberCompletionProvider.WarmIndex(active, scope.WorkspaceRoot);
    }

    private async Task<bool> waitForHybridIndexAsync(SolutionWarmupScope scope, CancellationToken cancellationToken)
    {
        var seed = _host.GetLatestHybridIndexState();
        if (seed is not null
            && scope.Matches(seed.WorkspaceRoot, seed.SolutionPath)
            && string.IsNullOrWhiteSpace(seed.LastError)
            && !string.IsNullOrWhiteSpace(seed.IndexedAtIso))
        {
            return true;
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = scopeSubscribe(scope, evt =>
        {
            if (string.IsNullOrWhiteSpace(evt.LastError))
                tcs.TrySetResult(true);
            else
                tcs.TrySetResult(false);
        });

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(30));
        try
        {
            return await tcs.Task.WaitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private IDisposable scopeSubscribe(SolutionWarmupScope scope, Action<HybridIndexStateChanged> handler) =>
        _dataBus.Subscribe<HybridIndexStateChanged>(evt =>
        {
            if (!scope.Matches(evt.WorkspaceRoot, evt.SolutionPath))
                return;
            handler(evt);
        });

    private IReadOnlyList<string> collectOpenCsPaths(SolutionWarmupSettings settings)
    {
        if (!settings.WarmOpenDocuments && !settings.WarmRecentCsFiles)
            return [];

        var list = _host.GetOpenCsFilePaths();
        var max = Math.Clamp(settings.MaxOpenDocumentFiles, 1, 32);
        return list
            .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .ToList();
    }

    private async Task warmFilesParallelAsync(
        SolutionWarmupScope scope,
        IReadOnlyList<string> absolutePaths,
        SolutionWarmupSettings settings,
        CancellationToken cancellationToken)
    {
        if (absolutePaths.Count == 0)
            return;

        var parallel = Math.Clamp(settings.MaxParallelFileJobs, 1, 8);
        using var gate = new SemaphoreSlim(parallel, parallel);
        var cache = IntercomAttachResolveCacheContext.From(
            scope.WorkspaceRoot,
            scope.SolutionPath,
            null,
            HybridIndexIndexDirectoryRelative.ResolveOrDefault(_host.GetHybridIndexSettings().IndexDir));

        var tasks = absolutePaths.Select(async path =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await Task.Run(
                        () =>
                        {
                            BracketMemberCompletionProvider.WarmIndex(path, scope.WorkspaceRoot);
                            IntercomRoslynL1Warmup.WarmFile(cache, path);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static async Task runFileJobAsync(Action work, CancellationToken cancellationToken) =>
        await Task.Run(work, cancellationToken).ConfigureAwait(false);

    private void postFeedAnchors() => _host.RunFeedAnchorsOnUi();

    private void throwIfStale(SolutionWarmupScope scope, int generation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (generation != _generation || !_currentScope.Equals(scope))
                throw new OperationCanceledException();
        }
    }

    private void publishIfCurrent(
        SolutionWarmupScope scope,
        int generation,
        SolutionWarmupLifecycle lifecycle,
        string? detail)
    {
        lock (_gate)
        {
            if (generation != _generation || !_currentScope.Equals(scope))
                return;
            publish(scope, lifecycle, detail);
        }
    }

    private void cancelLocked(SolutionWarmupLifecycle lifecycle, string? detail)
    {
        _runCts?.Cancel();
        _runCts?.Dispose();
        _runCts = null;
        if (!_currentScope.IsEmpty)
            publish(_currentScope, lifecycle, detail);
    }

    private void publish(SolutionWarmupScope scope, SolutionWarmupLifecycle lifecycle, string? detail) =>
        _dataBus.Publish(new SolutionWarmupStateChanged(
            scope.WorkspaceRoot,
            scope.SolutionPath,
            lifecycle,
            detail));

    public void Dispose()
    {
        lock (_gate)
        {
            _runCts?.Cancel();
            _runCts?.Dispose();
            _runCts = null;
        }
    }
}
