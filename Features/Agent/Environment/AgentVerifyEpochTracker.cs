using CascadeIDE.Cockpit.DataBus;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>verify_snapshot_id epoch + stale on write (ADR 0148 §8.1).</summary>
public sealed class AgentVerifyEpochTracker
{
    private readonly IDataBus _dataBus;
    private readonly object _gate = new();
    private string? _runId;
    private string? _snapshotId;
    private HashSet<string> _watchedPaths = new(StringComparer.OrdinalIgnoreCase);
    private bool _writesInvalidatedVerifyEpoch;

    public AgentVerifyEpochTracker(IDataBus dataBus) => _dataBus = dataBus;

    /// <summary>Писали в workspace во время активного verify — результат лестницы может не соответствовать текущему дереву.</summary>
    public bool WritesInvalidatedVerifyEpoch
    {
        get
        {
            lock (_gate)
                return _writesInvalidatedVerifyEpoch;
        }
    }

    public void Begin(string runId, string snapshotId, string solutionPath)
    {
        lock (_gate)
        {
            _runId = runId;
            _snapshotId = snapshotId;
            _watchedPaths = new(StringComparer.OrdinalIgnoreCase) { Path.GetFullPath(solutionPath) };
            _writesInvalidatedVerifyEpoch = false;
        }
    }

    public void WatchPath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        lock (_gate)
        {
            if (_snapshotId is null)
                return;
            _watchedPaths.Add(Path.GetFullPath(filePath));
        }
    }

    public void NotifyWrite(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        string? runId;
        string? snapshotId;
        lock (_gate)
        {
            if (_snapshotId is null)
                return;

            var full = Path.GetFullPath(filePath);
            if (!_watchedPaths.Contains(full) && !full.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                return;

            runId = _runId;
            snapshotId = _snapshotId;
        }

        if (runId is not null && snapshotId is not null)
        {
            lock (_gate)
                _writesInvalidatedVerifyEpoch = true;
            _dataBus.Publish(new AgentVerifyEpochStale(runId, snapshotId, "write_in_epoch"));
        }
    }

    public void End(string? reason = null)
    {
        lock (_gate)
        {
            if (_snapshotId is not null && reason is "superseded" or "cancel" && _runId is not null)
                _dataBus.Publish(new AgentVerifyEpochStale(_runId, _snapshotId, reason));

            _runId = null;
            _snapshotId = null;
            _watchedPaths.Clear();
            _writesInvalidatedVerifyEpoch = false;
        }
    }
}
