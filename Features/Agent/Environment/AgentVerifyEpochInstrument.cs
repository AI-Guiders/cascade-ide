using CascadeIDE.Cockpit.DataBus;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Live Verify Epoch state for PFD instrument + expand panel (ADR 0148 W3).</summary>
public sealed class AgentVerifyEpochInstrument
{
    private readonly object _gate = new();
    private readonly Dictionary<string, VerifyRungUiEntry> _rungs = new(StringComparer.Ordinal);

    private string? _runId;
    private string? _policy;
    private string? _snapshotId;
    private string? _maxRungReached;
    private string? _staleReason;
    private string? _activeTaskKind;
    private DateTimeOffset _activeSinceUtc;
    private bool _isActive;
    private bool _isStale;
    private bool _hostDied;
    private bool _completedGreen;
    private bool _frozen;
    private List<AgentTimeSlice> _timeSlices = [];

    public event Action? Changed;

    public bool IsVisible { get; private set; }

    public bool IsActive
    {
        get { lock (_gate) return _isActive; }
    }

    public bool IsStale
    {
        get { lock (_gate) return _isStale; }
    }

    public bool IsCaution
    {
        get { lock (_gate) return _isStale || _hostDied || (!_completedGreen && _frozen); }
    }

    public bool ShowCancel
    {
        get { lock (_gate) return _isActive && !_isStale; }
    }

    public bool ShowRetry
    {
        get { lock (_gate) return _hostDied || (_isStale && !_isActive); }
    }

    public bool DisplayGreen
    {
        get { lock (_gate) return AgentVerifyEpochFormatter.ShouldDisplayGreen(_completedGreen, _isStale, _maxRungReached ?? ""); }
    }

    public string CompactLine => Snapshot().CompactLine;

    public string ExpandedText => Snapshot().ExpandedText;

    public AgentVerifyEpochUiSnapshot Snapshot()
    {
        lock (_gate)
        {
            var rungList = BuildRungListLocked();
            var activeRung = ResolveActiveRungLocked();
            var activeSeconds = _isActive
                ? (DateTimeOffset.UtcNow - _activeSinceUtc).TotalSeconds
                : 0;

            return new AgentVerifyEpochUiSnapshot(
                IsVisible,
                CompactLine: AgentVerifyEpochFormatter.FormatCompactLine(
                    _isActive,
                    _isStale,
                    DisplayGreenLocked(),
                    _policy,
                    _runId,
                    activeRung,
                    activeSeconds,
                    _maxRungReached,
                    _hostDied),
                ExpandedText: AgentVerifyEpochFormatter.FormatExpandedBlock(
                    _policy,
                    _runId,
                    _snapshotId,
                    _isStale,
                    _staleReason,
                    rungList,
                    _timeSlices,
                    DisplayGreenLocked(),
                    _maxRungReached),
                IsCaution,
                ShowCancel: ShowCancel,
                ShowRetry: ShowRetry,
                DisplayGreen: DisplayGreenLocked());
        }
    }

    public void Reset()
    {
        lock (_gate)
        {
            _rungs.Clear();
            _runId = null;
            _policy = null;
            _snapshotId = null;
            _maxRungReached = null;
            _staleReason = null;
            _activeTaskKind = null;
            _isActive = false;
            _isStale = false;
            _hostDied = false;
            _completedGreen = false;
            _frozen = false;
            _timeSlices = [];
            IsVisible = false;
        }

        Changed?.Invoke();
    }

    public void OnRunStarted(AgentRunStarted evt)
    {
        lock (_gate)
        {
            _rungs.Clear();
            foreach (var rung in AgentVerifyEpochFormatter.OrderedRungs)
                _rungs[rung] = new VerifyRungUiEntry(rung, VerifyRungUiState.Pending, 0, null);

            _runId = evt.RunId;
            _policy = evt.VerifyPolicy;
            _snapshotId = evt.VerifySnapshotId;
            _maxRungReached = null;
            _staleReason = null;
            _activeTaskKind = VerifyRung.DiagnoseFiles;
            _activeSinceUtc = DateTimeOffset.UtcNow;
            _isActive = true;
            _isStale = false;
            _hostDied = false;
            _completedGreen = false;
            _frozen = false;
            _timeSlices = [];
            IsVisible = true;

            SetRungLocked(VerifyRung.DiagnoseFiles, VerifyRungUiState.Running, 0, null);
        }

        Changed?.Invoke();
    }

    public void OnTaskChanged(AgentEnvironmentTaskChanged evt)
    {
        if (evt.State is not (AgentEnvironmentTaskState.Running or AgentEnvironmentTaskState.Queued))
            return;

        lock (_gate)
        {
            if (!_isActive || !string.Equals(_runId, evt.RunId, StringComparison.Ordinal))
                return;

            _activeTaskKind = evt.Kind;
            _activeSinceUtc = DateTimeOffset.UtcNow;
            var rung = AgentVerifyEpochFormatter.MapTaskKindToRung(evt.Kind);
            if (rung is not null)
                SetRungLocked(rung, VerifyRungUiState.Running, 0, evt.ProgressMessage);
        }

        Changed?.Invoke();
    }

    public void OnTaskCompleted(AgentEnvironmentTaskCompleted evt)
    {
        lock (_gate)
        {
            if (!string.Equals(_runId, evt.RunId, StringComparison.Ordinal))
                return;

            var rung = AgentVerifyEpochFormatter.MapTaskKindToRung(evt.Kind);
            if (rung is null)
                return;

            var failed = evt.ResultSummary.Contains("fail", StringComparison.OrdinalIgnoreCase)
                || evt.ResultSummary.Contains("error", StringComparison.OrdinalIgnoreCase);
            var state = failed ? VerifyRungUiState.Fail : VerifyRungUiState.Pass;
            SetRungLocked(
                rung,
                state,
                evt.DurationMs / 1000.0,
                evt.ResultSummary);
            _maxRungReached = rung;
        }

        Changed?.Invoke();
    }

    public void OnTaskDied(AgentEnvironmentTaskDied evt)
    {
        lock (_gate)
        {
            if (!string.Equals(_runId, evt.RunId, StringComparison.Ordinal))
                return;

            _hostDied = true;
            var rung = AgentVerifyEpochFormatter.MapTaskKindToRung(_activeTaskKind ?? "")
                ?? VerifyRung.BuildAffected;
            SetRungLocked(rung, VerifyRungUiState.Died, 0, evt.StderrTail);
        }

        Changed?.Invoke();
    }

    public void OnRunCompleted(AgentRunCompleted evt)
    {
        lock (_gate)
        {
            if (!string.Equals(_runId, evt.RunId, StringComparison.Ordinal))
                return;

            _isActive = false;
            _frozen = true;
            _completedGreen = evt.Green;
            _maxRungReached = evt.MaxRungReached;
            _timeSlices = evt.TimeSlices.ToList();

            foreach (var entry in AgentVerifyEpochFormatter.BuildEntriesFromTimeSlices(
                         evt.TimeSlices,
                         evt.Green,
                         evt.MaxRungReached))
            {
                _rungs[entry.RungId] = entry;
            }

            IsVisible = true;
        }

        Changed?.Invoke();
    }

    public void OnEpochStale(AgentVerifyEpochStale evt)
    {
        lock (_gate)
        {
            if (!string.Equals(_runId, evt.RunId, StringComparison.Ordinal))
                return;

            _isStale = true;
            _staleReason = evt.Reason;
            IsVisible = true;
        }

        Changed?.Invoke();
    }

    public void HideAfterIdle()
    {
        lock (_gate)
        {
            if (_isActive || _isStale)
                return;

            IsVisible = false;
        }

        Changed?.Invoke();
    }

    private bool DisplayGreenLocked() =>
        AgentVerifyEpochFormatter.ShouldDisplayGreen(_completedGreen, _isStale, _maxRungReached ?? "");

    private string? ResolveActiveRungLocked()
    {
        if (!_isActive)
            return _maxRungReached;

        var running = _rungs.Values.FirstOrDefault(r => r.State == VerifyRungUiState.Running);
        if (running is not null)
            return running.RungId;

        return AgentVerifyEpochFormatter.MapTaskKindToRung(_activeTaskKind ?? "")
            ?? VerifyRung.DiagnoseFiles;
    }

    private IReadOnlyList<VerifyRungUiEntry> BuildRungListLocked()
    {
        if (_frozen && _timeSlices.Count > 0)
            return AgentVerifyEpochFormatter.BuildEntriesFromTimeSlices(_timeSlices, _completedGreen, _maxRungReached ?? "");

        return AgentVerifyEpochFormatter.OrderedRungs
            .Select(rung => _rungs.TryGetValue(rung, out var entry)
                ? entry
                : new VerifyRungUiEntry(rung, VerifyRungUiState.Pending, 0, null))
            .ToList();
    }

    private void SetRungLocked(string rungId, VerifyRungUiState state, double durationSeconds, string? detail)
    {
        _rungs[rungId] = new VerifyRungUiEntry(rungId, state, durationSeconds, detail);
    }
}

public sealed record AgentVerifyEpochUiSnapshot(
    bool IsVisible,
    string CompactLine,
    string ExpandedText,
    bool IsCaution,
    bool ShowCancel,
    bool ShowRetry,
    bool DisplayGreen);
