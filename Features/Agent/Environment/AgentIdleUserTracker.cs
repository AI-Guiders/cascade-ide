using CascadeIDE.Cockpit.DataBus;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Отслеживание idle_user (ADR 0148 §8.1.4): потеря фокуса CIDE ≥ порога.</summary>
public sealed class AgentIdleUserTracker
{
    private readonly object _gate = new();
    private DateTimeOffset? _focusLostUtc;
    private bool _cideFocused = true;
    private readonly List<AgentTimeSlice> _slices = [];

    public void NotifyCideFocus(bool focused)
    {
        lock (_gate)
        {
            if (focused)
            {
                FlushIdleSliceLocked();
                _cideFocused = true;
                _focusLostUtc = null;
                return;
            }

            if (_cideFocused)
                _focusLostUtc = DateTimeOffset.UtcNow;
            _cideFocused = false;
        }
    }

    public void SampleWhileUnfocused(int thresholdMs)
    {
        if (thresholdMs <= 0)
            return;

        lock (_gate)
        {
            if (_cideFocused || _focusLostUtc is null)
                return;

            var elapsed = (DateTimeOffset.UtcNow - _focusLostUtc.Value).TotalMilliseconds;
            if (elapsed < thresholdMs)
                return;

            _slices.Add(new AgentTimeSlice(
                AgentRunPhaseKind.IdleUser,
                elapsed / 1000.0,
                $"idle_user ≥ {thresholdMs}ms"));
            _focusLostUtc = DateTimeOffset.UtcNow;
        }
    }

    public IReadOnlyList<AgentTimeSlice> DrainSlices()
    {
        lock (_gate)
        {
            FlushIdleSliceLocked();
            if (_slices.Count == 0)
                return [];

            var copy = _slices.ToArray();
            _slices.Clear();
            return copy;
        }
    }

    private void FlushIdleSliceLocked()
    {
        if (_focusLostUtc is null || _cideFocused)
            return;

        var elapsed = (DateTimeOffset.UtcNow - _focusLostUtc.Value).TotalSeconds;
        if (elapsed > 0.05)
            _slices.Add(new AgentTimeSlice(AgentRunPhaseKind.IdleUser, elapsed, "idle_user (focus returned)"));
        _focusLostUtc = null;
    }
}
