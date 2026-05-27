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
    private HashSet<string> _uiStalePaths = new(StringComparer.OrdinalIgnoreCase);
    private bool _writesInvalidatedVerifyEpoch;
    private bool _uiStale;

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

    /// <summary>UI stale: запись в epoch или отмена/supersede — до следующего verify.</summary>
    public bool IsUiStale
    {
        get
        {
            lock (_gate)
                return _uiStale;
        }
    }

    public bool IsPathUiStale(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        string full;
        try
        {
            full = Path.GetFullPath(filePath);
        }
        catch
        {
            return false;
        }

        lock (_gate)
            return _uiStale && _uiStalePaths.Contains(full);
    }

    public void Begin(string runId, string snapshotId, string solutionPath)
    {
        lock (_gate)
        {
            _runId = runId;
            _snapshotId = snapshotId;
            _watchedPaths = new(StringComparer.OrdinalIgnoreCase) { Path.GetFullPath(solutionPath) };
            _uiStalePaths.Clear();
            _writesInvalidatedVerifyEpoch = false;
            _uiStale = false;
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
            {
                _writesInvalidatedVerifyEpoch = true;
                MarkUiStaleLocked();
            }

            _dataBus.Publish(new AgentVerifyEpochStale(runId, snapshotId, "write_in_epoch"));
        }
    }

    public void End(string? reason = null)
    {
        lock (_gate)
        {
            if (_snapshotId is not null && reason is "superseded" or "cancel" && _runId is not null)
            {
                MarkUiStaleLocked();
                _dataBus.Publish(new AgentVerifyEpochStale(_runId, _snapshotId, reason));
            }

            _runId = null;
            _snapshotId = null;
            _watchedPaths.Clear();
            _writesInvalidatedVerifyEpoch = false;
        }
    }

    private void MarkUiStaleLocked()
    {
        _uiStale = true;
        _uiStalePaths = new HashSet<string>(_watchedPaths, StringComparer.OrdinalIgnoreCase);
    }
}
